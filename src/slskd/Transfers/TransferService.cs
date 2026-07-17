// <copyright file="TransferService.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

// <copyright file="TransferService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Soulseek;
    using slskd.Transfers.Downloads;
    using slskd.Transfers.Uploads;

    /// <summary>
    ///     Manages transfers.
    /// </summary>
    public interface ITransferService
    {
        /// <summary>
        ///     Gets the upload service.
        /// </summary>
        IUploadService Uploads { get; }

        /// <summary>
        ///     Gets the download service.
        /// </summary>
        IDownloadService Downloads { get; }

        /// <summary>
        ///     Gets current transfer speeds and retained byte totals without materializing transfer history.
        /// </summary>
        /// <returns>Current directional speeds and retained directional byte totals.</returns>
        (double DownloadSpeed, double UploadSpeed, long DownloadedBytes, long UploadedBytes) GetSpeedSnapshot();

        /// <summary>
        ///     Gets retained download statistics grouped by username without materializing transfer history.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Download statistics keyed by username.</returns>
        Task<IReadOnlyDictionary<string, UserDownloadStats>> GetUserDownloadStatsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Manages transfers.
    /// </summary>
    public class TransferService : ITransferService
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferService"/> class.
        /// </summary>
        public TransferService(
            IUploadService uploadService,
            IDownloadService downloadService,
            IDbContextFactory<TransfersDbContext> contextFactory)
        {
            Uploads = uploadService;
            Downloads = downloadService;
            ContextFactory = contextFactory;
        }

        private IDbContextFactory<TransfersDbContext> ContextFactory { get; }

        /// <summary>
        ///     Gets the upload service.
        /// </summary>
        public IUploadService Uploads { get; init; }

        /// <summary>
        ///     Gets the download service.
        /// </summary>
        public IDownloadService Downloads { get; init; }

        /// <inheritdoc />
        public (double DownloadSpeed, double UploadSpeed, long DownloadedBytes, long UploadedBytes) GetSpeedSnapshot()
        {
            using var context = ContextFactory.CreateDbContext();
            var now = DateTime.UtcNow;
            var directionTotals = context.Transfers
                .AsNoTracking()
                .GroupBy(transfer => transfer.Direction)
                .Select(group => new
                {
                    Direction = group.Key,
                    BytesTransferred = group.Sum(transfer => transfer.BytesTransferred),
                    RecordedSpeed = group.Sum(transfer =>
                        !transfer.Removed &&
                        transfer.State == TransferStates.InProgress &&
                        transfer.AverageSpeed > 0
                            ? transfer.AverageSpeed
                            : 0),
                })
                .ToList();

            double downloadSpeed = 0;
            double uploadSpeed = 0;
            long downloadedBytes = 0;
            long uploadedBytes = 0;
            foreach (var total in directionTotals)
            {
                if (total.Direction == TransferDirection.Download)
                {
                    downloadSpeed = total.RecordedSpeed;
                    downloadedBytes = total.BytesTransferred;
                }
                else if (total.Direction == TransferDirection.Upload)
                {
                    uploadSpeed = total.RecordedSpeed;
                    uploadedBytes = total.BytesTransferred;
                }
            }

            var fallbackTransfers = context.Transfers
                .AsNoTracking()
                .Where(transfer =>
                    !transfer.Removed &&
                    transfer.State == TransferStates.InProgress &&
                    !(transfer.AverageSpeed > 0) &&
                    transfer.BytesTransferred > 0 &&
                    transfer.StartedAt.HasValue)
                .Select(transfer => new
                {
                    transfer.BytesTransferred,
                    transfer.Direction,
                    transfer.StartedAt,
                });

            foreach (var transfer in fallbackTransfers)
            {
                var elapsed = now - transfer.StartedAt!.Value;
                if (elapsed.TotalSeconds <= 0)
                {
                    continue;
                }

                var fallbackSpeed = transfer.BytesTransferred / elapsed.TotalSeconds;
                if (transfer.Direction == TransferDirection.Download)
                {
                    downloadSpeed += fallbackSpeed;
                }
                else if (transfer.Direction == TransferDirection.Upload)
                {
                    uploadSpeed += fallbackSpeed;
                }
            }

            return (downloadSpeed, uploadSpeed, downloadedBytes, uploadedBytes);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, UserDownloadStats>> GetUserDownloadStatsAsync(
            CancellationToken cancellationToken = default)
        {
            await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
            var stats = await context.Transfers
                .AsNoTracking()
                .Where(transfer =>
                    transfer.Direction == TransferDirection.Download &&
                    !transfer.Removed)
                .GroupBy(transfer => transfer.Username)
                .Select(group => new UserDownloadStats
                {
                    Username = group.Key,
                    TotalDownloads = group.Count(),
                    SuccessfulDownloads = group.Count(transfer =>
                        (transfer.State & TransferStates.Completed) == TransferStates.Completed &&
                        (transfer.State & TransferStates.Succeeded) == TransferStates.Succeeded),
                    FailedDownloads = group.Count(transfer =>
                        (transfer.State & TransferStates.Completed) == TransferStates.Completed &&
                        (transfer.State & TransferStates.Succeeded) != TransferStates.Succeeded),
                    TotalBytes = group.Sum(transfer =>
                        (transfer.State & TransferStates.Succeeded) == TransferStates.Succeeded
                            ? transfer.BytesTransferred
                            : 0),
                    LastDownloadAt = group.Max(transfer => transfer.EndedAt),
                })
                .ToListAsync(cancellationToken);

            return stats.ToDictionary(stat => stat.Username, StringComparer.Ordinal);
        }
    }
}
