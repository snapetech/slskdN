// <copyright file="TransferChangesResponse.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Transfers.API;

using System.Collections.Generic;

/// <summary>
///     A server-watermarked transfer snapshot or incremental change set.
/// </summary>
public class TransferChangesResponse
{
    /// <summary>
    ///     Gets the Unix-millisecond cursor for the next request.
    /// </summary>
    public long Cursor { get; set; }

    /// <summary>
    ///     Gets non-removed transfers for an initial request, or all changed records for a cursor request.
    /// </summary>
    public IEnumerable<global::slskd.Transfers.Transfer> Transfers { get; set; } = [];
}
