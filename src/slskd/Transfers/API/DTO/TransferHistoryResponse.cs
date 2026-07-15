// <copyright file="TransferHistoryResponse.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Transfers.API;

using System.Collections.Generic;

/// <summary>
///     A stable page from successful transfer history.
/// </summary>
public class TransferHistoryResponse
{
    /// <summary>
    ///     Gets the Unix-millisecond watermark shared by every page in this history snapshot.
    /// </summary>
    public long AsOf { get; set; }

    /// <summary>
    ///     Gets a value indicating whether another page remains.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    ///     Gets the offset to use for the next page.
    /// </summary>
    public int NextOffset { get; set; }

    /// <summary>
    ///     Gets successful transfer records in descending completion order.
    /// </summary>
    public IEnumerable<global::slskd.Transfers.Transfer> Transfers { get; set; } = [];
}
