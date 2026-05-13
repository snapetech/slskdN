// <copyright file="DownloadBatchResponse.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Transfers.API;

using System;
using System.Collections.Generic;

public record DownloadBatchResponse
{
    public Guid Id { get; init; }
    public int TransferCount { get; init; }
    public int CompletedCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public IEnumerable<Transfers.Transfer> Transfers { get; init; } = Array.Empty<Transfers.Transfer>();
}
