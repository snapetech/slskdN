// <copyright file="IPeerStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IPeerStreamService
{
    Task<PeerStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken);
}

public sealed record PeerStreamLease(Stream Stream, string ContentType, string OwnerKey);
