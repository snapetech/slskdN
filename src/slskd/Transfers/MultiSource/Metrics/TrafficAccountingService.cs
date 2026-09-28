// <copyright file="TrafficAccountingService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using slskd.HashDb;

    public interface ITrafficAccountingService
    {
        Task AddOverlayDownloadAsync(long bytes, CancellationToken ct = default);
        Task AddOverlayUploadAsync(long bytes, CancellationToken ct = default);
        Task AddSoulseekDownloadAsync(long bytes, CancellationToken ct = default);
        void RecordSoulseekDownloadProgress(int transferToken, long bytesTransferred, long startOffset);
        Task CommitSoulseekDownloadAsync(int transferToken, long bytesTransferred, long startOffset, CancellationToken ct = default);
        void RecordSoulseekUploadProgress(int bytes);
        Task CommitSoulseekUploadAsync(long bytes, CancellationToken ct = default);
        Task<HashDb.Models.TrafficTotals> GetTotalsAsync(CancellationToken ct = default);
    }

    /// <summary>
    ///     Centralized traffic accounting for overlay/Soulseek.
    /// </summary>
    public sealed class TrafficAccountingService : ITrafficAccountingService, IDisposable
    {
        private readonly IHashDbService hashDb;
        private readonly ILogger<TrafficAccountingService> logger;
        private readonly SemaphoreSlim soulseekAccountingSync = new(1, 1);
        private readonly object soulseekDownloadGate = new();
        private readonly Dictionary<int, SoulseekDownloadAttempt> soulseekDownloadAttempts = new();
        private readonly HashSet<int> completedSoulseekDownloadTokens = new();
        private long pendingSoulseekUploadBytes;
        private long pendingSoulseekDownloadBytes;
        private long queuedSoulseekDownloadBytes;
        private Task? soulseekDownloadFlushTask;

        public TrafficAccountingService(IHashDbService hashDb, ILogger<TrafficAccountingService> logger)
        {
            this.hashDb = hashDb;
            this.logger = logger;
        }

        public void Dispose()
        {
            soulseekAccountingSync.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task AddOverlayDownloadAsync(long bytes, CancellationToken ct = default) =>
            hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: bytes, soulseekUpload: 0, soulseekDownload: 0, ct);

        public Task AddOverlayUploadAsync(long bytes, CancellationToken ct = default) =>
            hashDb.AddTrafficAsync(overlayUpload: bytes, overlayDownload: 0, soulseekUpload: 0, soulseekDownload: 0, ct);

        public async Task AddSoulseekDownloadAsync(long bytes, CancellationToken ct = default)
        {
            await soulseekAccountingSync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: 0, soulseekUpload: 0, soulseekDownload: bytes, ct).ConfigureAwait(false);
            }
            finally
            {
                soulseekAccountingSync.Release();
            }
        }

        public void RecordSoulseekDownloadProgress(int transferToken, long bytesTransferred, long startOffset)
        {
            lock (soulseekDownloadGate)
            {
                var attempt = GetOrCreateSoulseekDownloadAttempt(transferToken, startOffset);
                if (attempt.IsTerminal)
                {
                    return;
                }

                AddSoulseekDownloadDelta(attempt, bytesTransferred, startOffset);
            }
        }

        public async Task CommitSoulseekDownloadAsync(
            int transferToken,
            long bytesTransferred,
            long startOffset,
            CancellationToken ct = default)
        {
            Task flushTask;
            lock (soulseekDownloadGate)
            {
                var attempt = GetOrCreateSoulseekDownloadAttempt(transferToken, startOffset);
                if (!attempt.IsTerminal)
                {
                    // The transfer-state event carries the final cumulative value, even when
                    // throttled progress events skipped its last network writes.
                    AddSoulseekDownloadDelta(attempt, bytesTransferred, startOffset);
                    attempt.IsTerminal = true;

                    if (attempt.AccountedBytes > 0)
                    {
                        queuedSoulseekDownloadBytes += attempt.AccountedBytes;
                        completedSoulseekDownloadTokens.Add(transferToken);
                    }
                    else
                    {
                        soulseekDownloadAttempts.Remove(transferToken);
                    }
                }

                flushTask = ScheduleSoulseekDownloadFlushLocked();
            }

            await flushTask.WaitAsync(ct).ConfigureAwait(false);
        }

        private Task ScheduleSoulseekDownloadFlushLocked()
        {
            if (queuedSoulseekDownloadBytes == 0)
            {
                return Task.CompletedTask;
            }

            soulseekDownloadFlushTask ??= FlushSoulseekDownloadBatchesAsync();
            return soulseekDownloadFlushTask;
        }

        private async Task FlushSoulseekDownloadBatchesAsync()
        {
            var persistenceFailures = 0;
            try
            {
                while (true)
                {
                    // Multi-source downloads finish in small chunks. Coalesce nearby terminal
                    // events so chunk size does not become database write frequency.
                    await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                    await soulseekAccountingSync.WaitAsync().ConfigureAwait(false);

                    long batchBytes = 0;
                    int[] completedTokens = [];
                    try
                    {
                        lock (soulseekDownloadGate)
                        {
                            if (queuedSoulseekDownloadBytes == 0)
                            {
                                soulseekDownloadFlushTask = null;
                                return;
                            }

                            batchBytes = queuedSoulseekDownloadBytes;
                            queuedSoulseekDownloadBytes = 0;
                            completedTokens = [.. completedSoulseekDownloadTokens];
                            completedSoulseekDownloadTokens.Clear();
                        }

                        try
                        {
                            await hashDb.AddTrafficAsync(
                                overlayUpload: 0,
                                overlayDownload: 0,
                                soulseekUpload: 0,
                                soulseekDownload: batchBytes).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            lock (soulseekDownloadGate)
                            {
                                queuedSoulseekDownloadBytes += batchBytes;
                                foreach (var token in completedTokens)
                                {
                                    completedSoulseekDownloadTokens.Add(token);
                                }
                            }

                            logger.LogError(ex, "Failed to persist a batch of {Bytes} Soulseek download bytes", batchBytes);
                            if (++persistenceFailures < 3)
                            {
                                continue;
                            }

                            lock (soulseekDownloadGate)
                            {
                                soulseekDownloadFlushTask = null;
                            }

                            return;
                        }

                        persistenceFailures = 0;
                        lock (soulseekDownloadGate)
                        {
                            pendingSoulseekDownloadBytes -= batchBytes;
                            foreach (var token in completedTokens)
                            {
                                soulseekDownloadAttempts.Remove(token);
                            }

                            if (queuedSoulseekDownloadBytes == 0)
                            {
                                soulseekDownloadFlushTask = null;
                                return;
                            }
                        }
                    }
                    finally
                    {
                        soulseekAccountingSync.Release();
                    }
                }
            }
            catch
            {
                lock (soulseekDownloadGate)
                {
                    soulseekDownloadFlushTask = null;
                }

                throw;
            }
        }

        public void RecordSoulseekUploadProgress(int bytes) =>
            Interlocked.Add(ref pendingSoulseekUploadBytes, bytes);

        public async Task CommitSoulseekUploadAsync(long bytes, CancellationToken ct = default)
        {
            await soulseekAccountingSync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: 0, soulseekUpload: bytes, soulseekDownload: 0, ct).ConfigureAwait(false);
                Interlocked.Add(ref pendingSoulseekUploadBytes, -bytes);
            }
            finally
            {
                soulseekAccountingSync.Release();
            }
        }

        public async Task<HashDb.Models.TrafficTotals> GetTotalsAsync(CancellationToken ct = default)
        {
            await soulseekAccountingSync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var persistedTotals = await hashDb.GetTrafficTotalsAsync(ct).ConfigureAwait(false);
                long pendingDownloadBytes;
                lock (soulseekDownloadGate)
                {
                    pendingDownloadBytes = pendingSoulseekDownloadBytes;
                }

                return new HashDb.Models.TrafficTotals
                {
                    OverlayUploadBytes = persistedTotals.OverlayUploadBytes,
                    OverlayDownloadBytes = persistedTotals.OverlayDownloadBytes,
                    SoulseekUploadBytes = persistedTotals.SoulseekUploadBytes + Interlocked.Read(ref pendingSoulseekUploadBytes),
                    SoulseekDownloadBytes = persistedTotals.SoulseekDownloadBytes + pendingDownloadBytes,
                };
            }
            finally
            {
                soulseekAccountingSync.Release();
            }
        }

        private SoulseekDownloadAttempt GetOrCreateSoulseekDownloadAttempt(int transferToken, long startOffset)
        {
            if (!soulseekDownloadAttempts.TryGetValue(transferToken, out var attempt))
            {
                attempt = new SoulseekDownloadAttempt(Math.Max(0, startOffset));
                soulseekDownloadAttempts.Add(transferToken, attempt);
            }

            return attempt;
        }

        private void AddSoulseekDownloadDelta(SoulseekDownloadAttempt attempt, long bytesTransferred, long startOffset)
        {
            var currentBytes = Math.Max(Math.Max(0, startOffset), bytesTransferred);
            if (currentBytes <= attempt.LastBytesTransferred)
            {
                return;
            }

            var delta = currentBytes - attempt.LastBytesTransferred;
            attempt.LastBytesTransferred = currentBytes;
            attempt.AccountedBytes += delta;
            pendingSoulseekDownloadBytes += delta;
        }

        private sealed class SoulseekDownloadAttempt(long startOffset)
        {
            public long LastBytesTransferred { get; set; } = startOffset;

            public long AccountedBytes { get; set; }

            public bool IsTerminal { get; set; }
        }
    }
}
