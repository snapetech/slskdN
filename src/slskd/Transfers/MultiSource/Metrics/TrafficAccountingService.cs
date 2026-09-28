// <copyright file="TrafficAccountingService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource.Metrics
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.HashDb;

    public interface ITrafficAccountingService
    {
        Task AddOverlayDownloadAsync(long bytes, CancellationToken ct = default);
        Task AddOverlayUploadAsync(long bytes, CancellationToken ct = default);
        Task AddSoulseekDownloadAsync(long bytes, CancellationToken ct = default);
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
        private readonly SemaphoreSlim soulseekUploadSync = new(1, 1);
        private long pendingSoulseekUploadBytes;

        public TrafficAccountingService(IHashDbService hashDb)
        {
            this.hashDb = hashDb;
        }

        public void Dispose()
        {
            soulseekUploadSync.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task AddOverlayDownloadAsync(long bytes, CancellationToken ct = default) =>
            hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: bytes, soulseekUpload: 0, soulseekDownload: 0, ct);

        public Task AddOverlayUploadAsync(long bytes, CancellationToken ct = default) =>
            hashDb.AddTrafficAsync(overlayUpload: bytes, overlayDownload: 0, soulseekUpload: 0, soulseekDownload: 0, ct);

        public Task AddSoulseekDownloadAsync(long bytes, CancellationToken ct = default) =>
            hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: 0, soulseekUpload: 0, soulseekDownload: bytes, ct);

        public void RecordSoulseekUploadProgress(int bytes) =>
            Interlocked.Add(ref pendingSoulseekUploadBytes, bytes);

        public async Task CommitSoulseekUploadAsync(long bytes, CancellationToken ct = default)
        {
            await soulseekUploadSync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await hashDb.AddTrafficAsync(overlayUpload: 0, overlayDownload: 0, soulseekUpload: bytes, soulseekDownload: 0, ct).ConfigureAwait(false);
                Interlocked.Add(ref pendingSoulseekUploadBytes, -bytes);
            }
            finally
            {
                soulseekUploadSync.Release();
            }
        }

        public async Task<HashDb.Models.TrafficTotals> GetTotalsAsync(CancellationToken ct = default)
        {
            await soulseekUploadSync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var persistedTotals = await hashDb.GetTrafficTotalsAsync(ct).ConfigureAwait(false);
                return new HashDb.Models.TrafficTotals
                {
                    OverlayUploadBytes = persistedTotals.OverlayUploadBytes,
                    OverlayDownloadBytes = persistedTotals.OverlayDownloadBytes,
                    SoulseekUploadBytes = persistedTotals.SoulseekUploadBytes + Interlocked.Read(ref pendingSoulseekUploadBytes),
                    SoulseekDownloadBytes = persistedTotals.SoulseekDownloadBytes,
                };
            }
            finally
            {
                soulseekUploadSync.Release();
            }
        }
    }
}
