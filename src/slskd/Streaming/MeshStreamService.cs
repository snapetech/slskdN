// <copyright file="MeshStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Mesh;
using slskd.Transfers.MultiSource.Metrics;

/// <summary>
/// Opens manual mesh preview streams without saving content locally.
/// </summary>
public sealed class MeshStreamService : IMeshStreamService
{
    private const int MaxConcurrentMeshStreamsPerOwner = 1;
    private const int MeshStreamChunkBytes = 2048;
    private const long PipePauseWriterThreshold = 512 * 1024;
    private const long PipeResumeWriterThreshold = 128 * 1024;

    private readonly IMeshStreamTicketService _tickets;
    private readonly IStreamSessionLimiter _limiter;
    private readonly IMeshDirectory _meshDirectory;
    private readonly IMeshContentFetcher _contentFetcher;
    private readonly IFairnessGuard? _fairnessGuard;
    private readonly ITrafficAccountingService? _trafficAccounting;
    private readonly ILogger<MeshStreamService> _logger;

    public MeshStreamService(
        IMeshStreamTicketService tickets,
        IStreamSessionLimiter limiter,
        IMeshDirectory meshDirectory,
        IMeshContentFetcher contentFetcher,
        ILogger<MeshStreamService> logger,
        IFairnessGuard? fairnessGuard = null,
        ITrafficAccountingService? trafficAccounting = null)
    {
        _tickets = tickets;
        _limiter = limiter;
        _meshDirectory = meshDirectory;
        _contentFetcher = contentFetcher;
        _logger = logger;
        _fairnessGuard = fairnessGuard;
        _trafficAccounting = trafficAccounting;
    }

    public Task<MeshStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken)
        => OpenCoreAsync(ticket, 0, null, cancellationToken);

    public Task<MeshStreamLease?> OpenRangeAsync(string ticket, long offset, long endExclusive, CancellationToken cancellationToken)
        => OpenCoreAsync(ticket, offset, endExclusive, cancellationToken);

    private async Task<MeshStreamLease?> OpenCoreAsync(string ticket, long offset, long? endExclusive, CancellationToken cancellationToken)
    {
        var claims = _tickets.Validate(ticket);
        if (claims == null)
        {
            return null;
        }

        if (offset < 0 || (endExclusive.HasValue && (claims.Radio == null || !claims.ExpectedSize.HasValue || endExclusive > claims.ExpectedSize || endExclusive <= offset)))
        {
            throw new ArgumentException("Invalid radio range.");
        }

        if (endExclusive.HasValue)
        {
            claims = claims with { ExpectedSize = endExclusive.Value };
        }

        if (_fairnessGuard != null)
        {
            var fairness = await _fairnessGuard.EvaluateAsync(cancellationToken).ConfigureAwait(false);
            if (!fairness.Allowed)
            {
                throw new MeshStreamLimitException($"Mesh preview stream blocked by fairness policy: {fairness.Reason}");
            }
        }

        if (!_limiter.TryAcquire(claims.OwnerKey, MaxConcurrentMeshStreamsPerOwner))
        {
            throw new MeshStreamLimitException("Too many concurrent mesh preview streams.");
        }

        var hostLimiterKey = claims.Radio == null ? null : $"mesh-radio-host:{claims.PeerId}";
        if (hostLimiterKey != null && !_limiter.TryAcquire(hostLimiterKey, 1))
        {
            _limiter.Release(claims.OwnerKey);
            throw new MeshStreamLimitException("A radio stream to this host is already active.");
        }

        var pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: PipePauseWriterThreshold,
            resumeWriterThreshold: PipeResumeWriterThreshold));
#pragma warning disable CA2000 // Ownership is transferred to ReleaseOnDisposeStream, which disposes it when the HTTP response stream is disposed.
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#pragma warning restore CA2000

        _ = ProduceAsync(claims, pipe.Writer, cancellationTokenSource.Token, offset);

        var stream = new ReleaseOnDisposeStream(
            pipe.Reader.AsStream(),
            () =>
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                _limiter.Release(claims.OwnerKey);
                if (hostLimiterKey != null)
                {
                    _limiter.Release(hostLimiterKey);
                }
            });

        return new MeshStreamLease(stream, claims.ContentType, claims.OwnerKey);
    }

    private async Task ProduceAsync(MeshStreamTicket claims, PipeWriter writer, CancellationToken cancellationToken, long offset)
    {
        Exception? failure = null;
        try
        {
            var peerId = await ResolvePeerIdAsync(claims, cancellationToken).ConfigureAwait(false);
            if (peerId == null)
            {
                throw new MeshStreamException("No fresh mesh peer is advertising this content.");
            }

            await using var output = writer.AsStream(leaveOpen: true);
            if (!string.IsNullOrWhiteSpace(claims.ExpectedHash))
            {
                await FetchVerifiedThenCopyAsync(claims, peerId, output, cancellationToken).ConfigureAwait(false);
                return;
            }

            await FetchAndCopyAsync(claims, peerId, output, cancellationToken, startOffset: offset).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            failure = ex;
            _logger.LogDebug("Mesh preview stream of {ContentId} was cancelled.", claims.ContentId);
        }
        catch (Exception ex) when (IsExpectedMeshStreamFailure(ex))
        {
            failure = ex;
            _logger.LogWarning("Mesh preview stream of {ContentId} ended because the mesh peer is unavailable: {Message}", claims.ContentId, ex.Message);
        }
        catch (Exception ex)
        {
            failure = ex;
            _logger.LogError(ex, "Mesh preview stream of {ContentId} failed: {Message}", claims.ContentId, ex.Message);
        }
        finally
        {
            await writer.CompleteAsync(failure).ConfigureAwait(false);
        }
    }

    private async Task FetchVerifiedThenCopyAsync(MeshStreamTicket claims, string peerId, Stream output, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"slskdn-mesh-preview-{Guid.NewGuid():N}.tmp");
        await using var temp = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            MeshStreamChunkBytes,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await FetchAndCopyAsync(claims, peerId, temp, cancellationToken, hash).ConfigureAwait(false);

        var actualHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        if (!string.Equals(actualHash, claims.ExpectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new MeshStreamException("Mesh content hash validation failed.");
        }

        temp.Position = 0;
        await temp.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    private async Task FetchAndCopyAsync(
        MeshStreamTicket claims,
        string peerId,
        Stream output,
        CancellationToken cancellationToken,
        IncrementalHash? hash = null,
        long startOffset = 0)
    {
        var expectedSize = claims.ExpectedSize;
        long offset = startOffset;
        var chunkBytes = claims.Radio == null ? MeshStreamChunkBytes : slskd.Mesh.ServiceFabric.Services.ListedRadioMeshService.MaxChunkBytes;
        var buffer = new byte[chunkBytes];
        while (!expectedSize.HasValue || offset < expectedSize.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = expectedSize.HasValue ? expectedSize.Value - offset : MeshStreamChunkBytes;
            var length = (int)Math.Min(chunkBytes, remaining);
            if (length <= 0)
            {
                break;
            }

            var result = claims.Radio != null
                ? await _contentFetcher.FetchRadioAsync(peerId, claims.ContentId, claims.Radio, offset, length, cancellationToken).ConfigureAwait(false)
                : await _contentFetcher.FetchAsync(
                peerId,
                claims.ContentId,
                expectedSize: expectedSize.HasValue ? length : null,
                expectedHash: null,
                offset: offset,
                length: length,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.Error != null || result.Data == null || !result.SizeValid)
            {
                throw new MeshStreamException(result.Error ?? "Mesh content chunk validation failed.");
            }

            using (result.Data)
            {
                int read;
                while ((read = await result.Data.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    hash?.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }

            offset += result.Size;
            if (_trafficAccounting != null && result.Size > 0)
            {
                await _trafficAccounting.AddOverlayDownloadAsync(result.Size, cancellationToken).ConfigureAwait(false);
            }

            if (claims.Radio != null && (!expectedSize.HasValue || offset < expectedSize.Value))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
            }

            if (!expectedSize.HasValue && result.Size < chunkBytes)
            {
                break;
            }
        }
    }

    private async Task<string?> ResolvePeerIdAsync(MeshStreamTicket claims, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(claims.PeerId))
        {
            return claims.PeerId;
        }

        var peers = await _meshDirectory.FindPeersByContentAsync(claims.ContentId, cancellationToken).ConfigureAwait(false);
        return peers.FirstOrDefault()?.PeerId;
    }

    private static bool IsExpectedMeshStreamFailure(Exception ex)
    {
        return ex is MeshStreamException ||
            ex is TimeoutException ||
            ex is IOException ||
            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class MeshStreamLimitException : Exception
{
    public MeshStreamLimitException(string message)
        : base(message)
    {
    }
}

public sealed class MeshStreamException : Exception
{
    public MeshStreamException(string message)
        : base(message)
    {
    }
}
