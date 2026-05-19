// <copyright file="TransfersHub.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.API
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Soulseek;

    public static class TransferHubMethods
    {
        /// <summary>
        ///     A transfer changed state (e.g. Queued => InProgress => Completed).
        /// </summary>
        public static readonly string Activity = "ACTIVITY";

        /// <summary>
        ///     A coalesced progress/speed sample for an in-progress transfer.
        /// </summary>
        public static readonly string Progress = "PROGRESS";

        /// <summary>
        ///     A transfer record was removed (cancel-with-remove or clear-completed).
        /// </summary>
        public static readonly string Removed = "REMOVED";
    }

    /// <summary>
    ///     Identifies a transfer record that was removed so clients can drop the row
    ///     without waiting for the next reconcile.
    /// </summary>
    public class TransferRemoved
    {
        public Guid Id { get; set; }

        public TransferDirection Direction { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Filename { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Extension methods for the transfers SignalR hub.
    /// </summary>
    public static class TransferHubExtensions
    {
        /// <summary>
        ///     Broadcast a transfer state change.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="activity">The transfer activity to broadcast.</param>
        /// <returns>The operation context.</returns>
        public static Task EmitTransferActivityAsync(this IHubContext<TransfersHub> hub, TransferActivity activity)
        {
            return hub.Clients.All.SendAsync(TransferHubMethods.Activity, activity);
        }

        /// <summary>
        ///     Broadcast a coalesced progress sample for an in-progress transfer.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="activity">The progress sample to broadcast.</param>
        /// <returns>The operation context.</returns>
        public static Task EmitTransferProgressAsync(this IHubContext<TransfersHub> hub, TransferActivity activity)
        {
            return hub.Clients.All.SendAsync(TransferHubMethods.Progress, activity);
        }

        /// <summary>
        ///     Broadcast that a transfer record was removed.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="removed">The removed transfer descriptor.</param>
        /// <returns>The operation context.</returns>
        public static Task EmitTransferRemovedAsync(this IHubContext<TransfersHub> hub, TransferRemoved removed)
        {
            return hub.Clients.All.SendAsync(TransferHubMethods.Removed, removed);
        }
    }

    /// <summary>
    ///     The transfers SignalR hub.
    /// </summary>
    [Authorize(Policy = AuthPolicy.Any)]
    public class TransfersHub : Hub
    {
        // Hub for broadcasting transfer activity events
    }
}
