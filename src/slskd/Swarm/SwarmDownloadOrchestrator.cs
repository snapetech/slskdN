// <copyright file="SwarmDownloadOrchestrator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soulseek;
using slskd.Common.Security;
using slskd.Transfers.Downloads;
using slskd.Transfers.MultiSource;
using slskd.Transfers.MultiSource.Scheduling;
using IODirectory = System.IO.Directory;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;
using LimitedWriteStream = slskd.Transfers.MultiSource.LimitedWriteStream;

namespace slskd.Swarm;

/// <summary>
/// Background orchestrator for swarm downloads.
/// </summary>
public class SwarmDownloadOrchestrator : BackgroundService
{
    private readonly ILogger<SwarmDownloadOrchestrator> logger;
    private readonly IVerificationEngine verifier;
    private readonly IChunkScheduler chunkScheduler;
    private readonly ISoulseekClient soulseekClient;
    private readonly IOptionsMonitor<slskd.Options>? optionsMonitor;
    private readonly string _tempRoot;
    private readonly Channel<SwarmJob> jobs = Channel.CreateBounded<SwarmJob>(new BoundedChannelOptions(1024)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
    });
    private readonly ConcurrentDictionary<string, SwarmJobStatus> activeJobs = new();

    public SwarmDownloadOrchestrator(
        ILogger<SwarmDownloadOrchestrator> logger,
        IVerificationEngine verifier,
        IChunkScheduler chunkScheduler,
        ISoulseekClient soulseekClient,
        IOptionsMonitor<slskd.Options>? optionsMonitor = null)
        : this(logger, verifier, chunkScheduler, soulseekClient, optionsMonitor, IOPath.GetTempPath())
    {
    }

    internal SwarmDownloadOrchestrator(
        ILogger<SwarmDownloadOrchestrator> logger,
        IVerificationEngine verifier,
        IChunkScheduler chunkScheduler,
        ISoulseekClient soulseekClient,
        IOptionsMonitor<slskd.Options>? optionsMonitor,
        string tempRoot)
    {
        this.logger = logger;
        this.verifier = verifier;
        this.chunkScheduler = chunkScheduler;
        this.soulseekClient = soulseekClient;
        this.optionsMonitor = optionsMonitor;
        _tempRoot = tempRoot;
    }

    public bool Enqueue(SwarmJob job)
    {
        logger.LogDebug("[SwarmOrchestrator] Enqueue {JobId} ({ContentId})",
            LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
            LoggingSanitizer.SanitizeExternalIdentifier(job.File.ContentId));
        return jobs.Writer.TryWrite(job);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        await Task.Yield();

        await foreach (var job in jobs.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("[SwarmOrchestrator] Start {JobId} ({ContentId})",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                    LoggingSanitizer.SanitizeExternalIdentifier(job.File.ContentId));
                await ProcessJob(job, stoppingToken);
                logger.LogInformation("[SwarmOrchestrator] Completed {JobId}",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                logger.LogError("[SwarmOrchestrator] Failed {JobId}: {Exception}",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                    LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            }
        }
    }

    private async Task ProcessJob(SwarmJob job, CancellationToken ct)
    {
        var remoteFilename = string.IsNullOrWhiteSpace(job.File.Filename)
            ? job.File.ContentId
            : job.File.Filename;
        var policyExclusion = DownloadFilter.GetMatchingExclusion(
            remoteFilename,
            optionsMonitor?.CurrentValue?.Filters.Download.Exclude);
        if (policyExclusion is not null)
        {
            logger.LogInformation(
                "[SwarmOrchestrator] Skipping job {JobId} for {Filename}; blocked by global exclusion {Exclusion}",
                LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                LoggingSanitizer.SanitizeFilePath(remoteFilename),
                LoggingSanitizer.SanitizeExternalIdentifier(policyExclusion));
            return;
        }

        logger.LogInformation("[SwarmOrchestrator] Processing job {JobId}: {ContentId} ({Size} bytes) from {SourceCount} sources",
            LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
            LoggingSanitizer.SanitizeExternalIdentifier(job.File.ContentId),
            job.File.SizeBytes,
            job.Sources.Count);

        if (job.Sources.Count == 0)
        {
            logger.LogWarning("[SwarmOrchestrator] Job {JobId} has no sources",
                LoggingSanitizer.SanitizeExternalIdentifier(job.JobId));
            return;
        }

        var status = new SwarmJobStatus
        {
            JobId = job.JobId,
            State = SwarmJobState.Downloading,
            TotalChunks = 0,
            CompletedChunks = 0,
        };
        var storageId = Guid.NewGuid().ToString("N");
        var tempRoot = IOPath.Combine(_tempRoot, "slskdn-swarm");
        var tempDir = PathGuard.NormalizeAbsolutePathWithinRoots(
            IOPath.GetFullPath(IOPath.Combine(tempRoot, storageId)),
            new[] { tempRoot })
            ?? throw new IOException("Swarm temporary directory escaped its root");
        var downloadTasks = new List<Task>();
        activeJobs[job.JobId] = status;

        try
        {
            // Calculate chunks
            const int chunkSize = 512 * 1024; // 512KB chunks
            var chunks = CalculateChunks(job.File.SizeBytes, chunkSize);
            status.TotalChunks = chunks.Count;

            logger.LogInformation("[SwarmOrchestrator] Job {JobId}: {ChunkCount} chunks of {ChunkSize} bytes each",
                LoggingSanitizer.SanitizeExternalIdentifier(job.JobId), chunks.Count, chunkSize);

            // Create temp directory for chunks
            IODirectory.CreateDirectory(tempDir);

            // Convert SwarmSource to peer usernames for chunk scheduler
            var availablePeers = job.Sources
                .Where(s => s.Transport == "soulseek") // For now, only handle Soulseek peers
                .Select(s => s.PeerId)
                .ToList();

            if (availablePeers.Count == 0)
            {
                logger.LogWarning("[SwarmOrchestrator] Job {JobId}: No Soulseek peers available",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId));
                status.State = SwarmJobState.Failed;
                status.Error = "No Soulseek peers available";
                return;
            }

            // Shared work queue for chunks
            var chunkQueue = new ConcurrentQueue<ChunkInfo>();
            foreach (var chunk in chunks)
            {
                chunkQueue.Enqueue(chunk);
            }

            var completedChunks = new ConcurrentDictionary<int, ChunkResult>();
            var chunkAssignments = new ConcurrentDictionary<int, ChunkAssignment>();
            var activeDownloadTasks = new ConcurrentDictionary<int, Task>(); // Track active downloads by chunk index
            var chunkAttempts = new ConcurrentDictionary<int, int>();
            var failedChunks = new ConcurrentDictionary<int, string>();
            const int maxAttemptsPerChunk = 3;

            void RetryOrFail(ChunkInfo failedChunk, string reason)
            {
                var attempts = chunkAttempts.GetValueOrDefault(failedChunk.Index);
                if (attempts < maxAttemptsPerChunk)
                {
                    chunkQueue.Enqueue(failedChunk);
                    return;
                }

                failedChunks[failedChunk.Index] = reason;
                logger.LogWarning(
                    "[SwarmOrchestrator] Job {JobId}: Chunk {ChunkIndex} exhausted {Attempts} attempts: {Reason}",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                    failedChunk.Index,
                    attempts,
                    LoggingSanitizer.SanitizeExternalIdentifier(reason));
            }

            // T-1405: Subscribe to peer degradation events for reassignment
            var degradedPeers = new ConcurrentDictionary<string, bool>();

            // Process chunks using chunk scheduler
            while (!chunkQueue.IsEmpty
                || completedChunks.Count + failedChunks.Count < chunks.Count
                || downloadTasks.Any(task => !task.IsCompleted))
            {
                // T-1405: Check for peer degradation and reassign chunks
                foreach (var degradedPeer in degradedPeers.Keys.ToList())
                {
                    if (degradedPeers.TryRemove(degradedPeer, out _))
                    {
                        var chunksToReassign = await chunkScheduler.HandlePeerDegradationAsync(
                            degradedPeer,
                            slskd.Transfers.MultiSource.Scheduling.DegradationReason.HighErrorRate,
                            ct);

                        if (chunksToReassign != null && chunksToReassign.Count > 0)
                        {
                            logger.LogInformation(
                                "[SwarmOrchestrator] Reassigning {Count} chunks from degraded peer {PeerId}",
                                chunksToReassign.Count,
                                LoggingSanitizer.SanitizeExternalIdentifier(degradedPeer));

                            // Cancel and re-queue chunks assigned to degraded peer
                            foreach (var chunkIndex in chunksToReassign)
                            {
                                // Cancel active download task if exists
                                if (activeDownloadTasks.TryRemove(chunkIndex, out var downloadTask))
                                {
                                    // Task will handle cancellation and re-queue the chunk.
                                    _ = downloadTask;
                                }

                                // Re-queue chunk for reassignment
                                var chunkToRequeue = chunks.Find(c => c.Index == chunkIndex);
                                if (chunkToRequeue != null && !completedChunks.ContainsKey(chunkIndex))
                                {
                                    chunkQueue.Enqueue(chunkToRequeue);
                                    chunkAssignments.TryRemove(chunkIndex, out _);
                                    chunkScheduler.UnregisterAssignment(chunkIndex);
                                }
                            }
                        }
                    }
                }

                if (chunkQueue.TryDequeue(out var chunk))
                {
                    chunkAttempts.AddOrUpdate(chunk.Index, 1, (_, attempts) => attempts + 1);

                    // Get chunk assignment from scheduler
                    var assignment = await chunkScheduler.AssignChunkAsync(
                        new ChunkRequest
                        {
                            ChunkIndex = chunk.Index,
                            Size = chunk.EndOffset - chunk.StartOffset,
                        },
                        availablePeers.Where(p => !degradedPeers.ContainsKey(p)).ToList(), // Exclude degraded peers
                        ct);

                    if (assignment.Success && !string.IsNullOrEmpty(assignment.AssignedPeer))
                    {
                        chunkAssignments[chunk.Index] = assignment;

                        // T-1405: Track active download task for reassignment
                        var downloadTask = Task.Run(async () =>
                        {
                            try
                            {
                                var chunkResult = await DownloadChunkAsync(
                                    job,
                                    chunk,
                                    assignment.AssignedPeer,
                                    tempDir,
                                    ct);

                                if (chunkResult.Success)
                                {
                                    // Verify chunk
                                    var verified = await verifier.VerifyChunkAsync(
                                        job.File.ContentId,
                                        chunk.Index,
                                        chunkResult.Data ?? Array.Empty<byte>(),
                                        ct);

                                    if (verified)
                                    {
                                        chunkResult.Data = null;
                                        completedChunks[chunk.Index] = chunkResult;
                                        chunkScheduler.UnregisterAssignment(chunk.Index);
                                        var completed = Interlocked.Increment(ref status.CompletedChunks);
                                        logger.LogDebug("[SwarmOrchestrator] Job {JobId}: Chunk {ChunkIndex} completed and verified ({Completed}/{Total})",
                                            LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                                            chunk.Index, completed, status.TotalChunks);
                                    }
                                    else
                                    {
                                        logger.LogWarning("[SwarmOrchestrator] Job {JobId}: Chunk {ChunkIndex} verification failed",
                                            LoggingSanitizer.SanitizeExternalIdentifier(job.JobId), chunk.Index);
                                        chunkScheduler.UnregisterAssignment(chunk.Index);

                                        RetryOrFail(chunk, "Chunk verification failed");
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("[SwarmOrchestrator] Job {JobId}: Chunk {ChunkIndex} download failed: {Error}",
                                        LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                                        chunk.Index,
                                        LoggingSanitizer.SanitizeExternalIdentifier(chunkResult.Error));
                                    chunkScheduler.UnregisterAssignment(chunk.Index);

                                    RetryOrFail(chunk, chunkResult.Error ?? "Chunk download failed");
                                }
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError("[SwarmOrchestrator] Job {JobId}: Error processing chunk {ChunkIndex}: {Exception}",
                                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                                    chunk.Index,
                                    LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                                chunkScheduler.UnregisterAssignment(chunk.Index);

                                RetryOrFail(chunk, "Chunk processing failed");
                            }
                            finally
                            {
                                activeDownloadTasks.TryRemove(chunk.Index, out _);
                            }
                        }, ct);

                        activeDownloadTasks[chunk.Index] = downloadTask;
                        downloadTasks.Add(downloadTask);
                    }
                    else
                    {
                        logger.LogWarning("[SwarmOrchestrator] Job {JobId}: Failed to assign chunk {ChunkIndex}: {Reason}",
                            LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                            chunk.Index,
                            LoggingSanitizer.SanitizeExternalIdentifier(assignment.Reason));

                        RetryOrFail(chunk, assignment.Reason ?? "Peer assignment failed");
                    }
                }

                // Limit concurrent downloads
                if (downloadTasks.Count >= availablePeers.Count * 2)
                {
                    await Task.WhenAny(downloadTasks);
                    downloadTasks.RemoveAll(t => t.IsCompleted);
                }

                // Check if we're done
                if (completedChunks.Count >= chunks.Count)
                {
                    break;
                }

                await Task.Delay(100, ct); // Small delay to prevent tight loop
            }

            // Wait for remaining downloads
            await Task.WhenAll(downloadTasks);

            // Check completion
            if (completedChunks.Count < chunks.Count)
            {
                logger.LogWarning("[SwarmOrchestrator] Job {JobId}: Incomplete - {Completed}/{Total} chunks",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId), completedChunks.Count, chunks.Count);
                status.State = SwarmJobState.Failed;
                status.Error = $"Only {completedChunks.Count}/{chunks.Count} chunks completed after up to {maxAttemptsPerChunk} attempts per chunk";
                return;
            }

            // Assemble final file
            var outputRoot = IOPath.Combine(_tempRoot, "slskdn-swarm-output");
            var outputFilename = PathGuard.SanitizeFilename($"{storageId}_{IOPath.GetFileName(job.File.ContentId)}");
            var outputPath = PathGuard.NormalizeAbsolutePathWithinRoots(
                IOPath.GetFullPath(IOPath.Combine(outputRoot, outputFilename)),
                new[] { outputRoot })
                ?? throw new IOException("Swarm output path escaped its root");
            var outputDirectory = IOPath.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                IODirectory.CreateDirectory(outputDirectory);
            }

            var stagingPath = ContentSafety.CreateStagingPath(outputPath, outputRoot);
            var stagingDirectory = IOPath.GetDirectoryName(stagingPath)
                ?? throw new IOException("Swarm staging path has no parent directory");
            IODirectory.CreateDirectory(stagingDirectory);
            try
            {
                await AssembleFileAsync(chunks, completedChunks, tempDir, stagingPath, ct);
                ContentSafety.PublishStagedFile(stagingPath, outputPath, outputRoot);
            }
            finally
            {
                try
                {
                    ContentSafety.DeleteStagedFile(stagingPath, outputRoot);
                }
                catch (IOException ex)
                {
                    logger.LogWarning("[SwarmOrchestrator] Failed to remove partial output for job {JobId}: {Exception}",
                        LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                        LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning("[SwarmOrchestrator] Failed to remove partial output for job {JobId}: {Exception}",
                        LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                        LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                }
            }

            status.State = SwarmJobState.Completed;
            status.OutputPath = outputPath;
            logger.LogInformation("[SwarmOrchestrator] Job {JobId}: Completed successfully - {OutputPath}",
                LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                LoggingSanitizer.SanitizeFilePath(outputPath));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("[SwarmOrchestrator] Job {JobId}: Failed with exception {Exception}",
                LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            status.State = SwarmJobState.Failed;
            status.Error = "Swarm download failed";
        }
        finally
        {
            try
            {
                await Task.WhenAll(downloadTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Active chunk workers observe the job cancellation before temporary files are removed.
            }
            catch (Exception ex)
            {
                logger.LogWarning("[SwarmOrchestrator] Failed while draining chunks for job {JobId}: {Exception}",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                    LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            }

            try
            {
                if (IODirectory.Exists(tempDir))
                {
                    IODirectory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("[SwarmOrchestrator] Failed to remove temporary chunk directory for job {JobId}: {Exception}",
                    LoggingSanitizer.SanitizeExternalIdentifier(job.JobId),
                    LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            }

            activeJobs.TryRemove(job.JobId, out _);
        }
    }

    private List<ChunkInfo> CalculateChunks(long fileSize, int chunkSize)
    {
        var chunks = new List<ChunkInfo>();
        var chunkIndex = 0;

        for (long offset = 0; offset < fileSize; offset += chunkSize)
        {
            var endOffset = Math.Min(offset + chunkSize, fileSize);
            chunks.Add(new ChunkInfo
            {
                Index = chunkIndex++,
                StartOffset = offset,
                EndOffset = endOffset,
            });
        }

        return chunks;
    }

    private async Task<ChunkResult> DownloadChunkAsync(
        SwarmJob job,
        ChunkInfo chunk,
        string peerId,
        string tempDir,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var chunkSize = chunk.EndOffset - chunk.StartOffset;
        var tempFile = IOPath.Combine(tempDir, $"chunk_{chunk.Index}.tmp");

        try
        {
            // Find the source for this peer
            var source = job.Sources.FirstOrDefault(s => s.PeerId == peerId);
            if (source == null)
            {
                return new ChunkResult
                {
                    ChunkIndex = chunk.Index,
                    Success = false,
                    Error = "Chunk source not found",
                };
            }

            // For Soulseek transport, use Soulseek client with LimitedWriteStream
            if (source.Transport == "soulseek")
            {
                var remoteFilename = string.IsNullOrWhiteSpace(job.File.Filename)
                    ? job.File.ContentId
                    : job.File.Filename;
                var policyExclusion = DownloadFilter.GetMatchingExclusion(
                    remoteFilename,
                    optionsMonitor?.CurrentValue?.Filters.Download.Exclude);
                if (policyExclusion is not null)
                {
                    return new ChunkResult
                    {
                        ChunkIndex = chunk.Index,
                        Success = false,
                        Error = $"Blocked by global download exclusion '{policyExclusion}'",
                    };
                }

                logger.LogDebug("[SwarmOrchestrator] Downloading chunk {ChunkIndex} from {PeerId} (offset {Start}-{End}, size {Size})",
                    chunk.Index,
                    LoggingSanitizer.SanitizeExternalIdentifier(peerId),
                    chunk.StartOffset,
                    chunk.EndOffset,
                    chunkSize);

                // Use LimitedWriteStream to download only the chunk range
                // Soulseek doesn't support range requests, but we can start at the offset
                // and limit the stream to only write the chunk size
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30s timeout per chunk

                long bytesDownloaded;
                using (var fileStream = IOFile.Create(tempFile))
                using (var limitedStream = new LimitedWriteStream(fileStream, chunkSize, cts))
                {
                    try
                    {
                        // Download from the start offset, limited stream will stop after chunkSize bytes
                        // Use ContentId as filename if source doesn't have a specific path
                        await soulseekClient.DownloadAsync(
                            username: peerId,
                            remoteFilename: remoteFilename,
                            outputStreamFactory: () => Task.FromResult<System.IO.Stream>(limitedStream),
                            size: job.File.SizeBytes,
                            startOffset: chunk.StartOffset,
                            cancellationToken: cts.Token,
                            options: new Soulseek.TransferOptions(
                                maximumLingerTime: 3000,
                                disposeOutputStreamOnCompletion: false));
                    }
                    catch (OperationCanceledException) when (limitedStream.LimitReached && !ct.IsCancellationRequested)
                    {
                        // Expected - we cancelled after getting our chunk
                        logger.LogDebug("[SwarmOrchestrator] Chunk {ChunkIndex} complete (cancelled remaining) from {PeerId}",
                            chunk.Index, LoggingSanitizer.SanitizeExternalIdentifier(peerId));
                    }

                    bytesDownloaded = limitedStream.BytesWritten;
                }

                stopwatch.Stop();

                var success = bytesDownloaded >= chunkSize;

                if (success)
                {
                    logger.LogInformation("[SwarmOrchestrator] ✓ Chunk {ChunkIndex} from {PeerId}: {Size}KB in {Time}ms @ {Speed:F0}KB/s",
                        chunk.Index, LoggingSanitizer.SanitizeExternalIdentifier(peerId), chunkSize / 1024, stopwatch.ElapsedMilliseconds,
                        (chunkSize * 1000.0 / stopwatch.ElapsedMilliseconds) / 1024.0);

                    // Read chunk data into memory for assembly
                    var chunkData = await IOFile.ReadAllBytesAsync(tempFile, ct);

                    return new ChunkResult
                    {
                        ChunkIndex = chunk.Index,
                        Success = true,
                        Data = chunkData,
                    };
                }
                else
                {
                    // Clean up partial file
                    try
                    {
                        IOFile.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug("[SwarmOrchestrator] Failed to delete partial chunk file {Path}: {Exception}",
                            LoggingSanitizer.SanitizeFilePath(tempFile),
                            LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                    }

                    return new ChunkResult
                    {
                        ChunkIndex = chunk.Index,
                        Success = false,
                        Error = "Incomplete chunk download",
                    };
                }
            }
            else if (source.Transport == "mesh" || source.Transport == "overlay")
            {
                // Mesh/overlay chunk range reads require the mesh data plane to advertise range support.
                logger.LogWarning("[SwarmOrchestrator] Mesh transport chunk download unavailable: range reads are not advertised");
                return new ChunkResult
                {
                    ChunkIndex = chunk.Index,
                    Success = false,
                    Error = "Mesh transport chunk download is unavailable",
                };
            }
            else
            {
                return new ChunkResult
                {
                    ChunkIndex = chunk.Index,
                    Success = false,
                    Error = "Unsupported chunk transport",
                };
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError("[SwarmOrchestrator] Error downloading chunk {ChunkIndex} from {PeerId}: {Exception}",
                chunk.Index,
                LoggingSanitizer.SanitizeExternalIdentifier(peerId),
                LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));

            // Clean up on error
            try
            {
                if (IOFile.Exists(tempFile))
                {
                    IOFile.Delete(tempFile);
                }
            }
            catch (Exception cleanupEx)
            {
                logger.LogDebug("[SwarmOrchestrator] Failed to cleanup chunk file {Path}: {Exception}",
                    LoggingSanitizer.SanitizeFilePath(tempFile),
                    LoggingSanitizer.SanitizeExternalIdentifier(cleanupEx.ToString()));
            }

            return new ChunkResult
            {
                ChunkIndex = chunk.Index,
                Success = false,
                Error = "Chunk download failed",
            };
        }
    }

    private async Task AssembleFileAsync(
        List<ChunkInfo> chunks,
        ConcurrentDictionary<int, ChunkResult> completedChunks,
        string tempDir,
        string outputPath,
        CancellationToken ct)
    {
        await using var outputStream = new FileStream(
            outputPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        foreach (var chunk in chunks.OrderBy(c => c.Index))
        {
            if (completedChunks.TryGetValue(chunk.Index, out var chunkResult) && chunkResult.Success)
            {
                var chunkPath = PathGuard.NormalizeAbsolutePathWithinRoots(
                    IOPath.Combine(tempDir, $"chunk_{chunk.Index}.tmp"),
                    new[] { tempDir })
                    ?? throw new IOException($"Chunk {chunk.Index} path escaped its temporary directory");
                await using var chunkStream = new FileStream(
                    chunkPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await chunkStream.CopyToAsync(outputStream, 81920, ct);
            }
            else
            {
                throw new InvalidOperationException($"Missing chunk {chunk.Index}");
            }
        }

        await outputStream.FlushAsync(ct);
    }
}

/// <summary>
/// Status of a swarm job.
/// </summary>
public class SwarmJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public SwarmJobState State { get; set; }
    public int TotalChunks { get; set; }

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Interlocked requires a field reference.")]
    public int CompletedChunks; // Field, not property, for Interlocked operations

    public string? OutputPath { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// State of a swarm job.
/// </summary>
public enum SwarmJobState
{
    Pending,
    Downloading,
    Completed,
    Failed,
}

/// <summary>
/// Information about a chunk.
/// </summary>
public class ChunkInfo
{
    public int Index { get; set; }
    public long StartOffset { get; set; }
    public long EndOffset { get; set; }
}

/// <summary>
/// Result of downloading a chunk.
/// </summary>
public class ChunkResult
{
    public int ChunkIndex { get; set; }
    public bool Success { get; set; }
    public byte[]? Data { get; set; }
    public string? Error { get; set; }
}
