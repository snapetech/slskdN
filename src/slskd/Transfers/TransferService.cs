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
    using System.Linq;
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
            var activeTransfers = context.Transfers
                .AsNoTracking()
                .Where(transfer => !transfer.Removed && transfer.State == TransferStates.InProgress)
                .Select(transfer => new
                {
                    transfer.AverageSpeed,
                    transfer.BytesTransferred,
                    transfer.Direction,
                    transfer.StartedAt,
                })
                .ToList();
            var byteTotals = context.Transfers
                .AsNoTracking()
                .GroupBy(transfer => transfer.Direction)
                .Select(group => new
                {
                    Direction = group.Key,
                    BytesTransferred = group.Sum(transfer => transfer.BytesTransferred),
                })
                .ToList();

            double GetLiveSpeed(double averageSpeed, long bytesTransferred, DateTime? startedAt)
            {
                if (averageSpeed > 0)
                {
                    return averageSpeed;
                }

                var elapsed = startedAt.HasValue ? now - startedAt.Value : TimeSpan.Zero;
                return elapsed.TotalSeconds > 0 && bytesTransferred > 0
                    ? bytesTransferred / elapsed.TotalSeconds
                    : 0;
            }

            var downloadSpeed = activeTransfers
                .Where(transfer => transfer.Direction == TransferDirection.Download)
                .Sum(transfer => GetLiveSpeed(
                    transfer.AverageSpeed,
                    transfer.BytesTransferred,
                    transfer.StartedAt));
            var uploadSpeed = activeTransfers
                .Where(transfer => transfer.Direction == TransferDirection.Upload)
                .Sum(transfer => GetLiveSpeed(
                    transfer.AverageSpeed,
                    transfer.BytesTransferred,
                    transfer.StartedAt));
            var downloadedBytes = byteTotals
                .Where(total => total.Direction == TransferDirection.Download)
                .Select(total => total.BytesTransferred)
                .SingleOrDefault();
            var uploadedBytes = byteTotals
                .Where(total => total.Direction == TransferDirection.Upload)
                .Select(total => total.BytesTransferred)
                .SingleOrDefault();

            return (downloadSpeed, uploadSpeed, downloadedBytes, uploadedBytes);
        }
    }
}
