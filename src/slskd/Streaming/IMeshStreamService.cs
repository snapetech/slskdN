// <copyright file="IMeshStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IMeshStreamService
{
    Task<MeshStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken);
}

public sealed record MeshStreamLease(Stream Stream, string ContentType, string OwnerKey);
