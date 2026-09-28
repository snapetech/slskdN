// <copyright file="IMeshStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IMeshStreamService
{
    Task<MeshStreamLease?> OpenRangeAsync(string ticket, long offset, long endExclusive, CancellationToken cancellationToken);

    Task<MeshStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken);
}

public sealed record MeshStreamLease(Stream Stream, string ContentType, string OwnerKey)
{
    public CancellationToken Superseded { get; init; }
}
