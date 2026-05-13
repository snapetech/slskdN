// <copyright file="IMeshStreamTicketService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

/// <summary>
/// Creates short-lived opaque tickets for manual mesh preview streams.
/// </summary>
public interface IMeshStreamTicketService
{
    MeshStreamTicket Create(MeshStreamTicketRequest request, string ownerKey, TimeSpan lifetime);

    MeshStreamTicket? Validate(string ticket);
}

public sealed record MeshStreamTicketRequest(
    string ContentId,
    string Filename,
    string? PeerId,
    long? ExpectedSize,
    string? ExpectedHash);

public sealed record MeshStreamTicket(
    string Ticket,
    string ContentId,
    string Filename,
    string? PeerId,
    long? ExpectedSize,
    string? ExpectedHash,
    string OwnerKey,
    DateTimeOffset ExpiresAtUtc,
    string ContentType);
