// <copyright file="IPeerStreamTicketService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

/// <summary>
/// Creates short-lived opaque tickets for manual Soulseek peer preview streams.
/// </summary>
public interface IPeerStreamTicketService
{
    PeerStreamTicket Create(PeerStreamTicketRequest request, string ownerKey, TimeSpan lifetime);

    PeerStreamTicket? Validate(string ticket);
}

public sealed record PeerStreamTicketRequest(string Username, string Filename, long? Size);

public sealed record PeerStreamTicket(string Ticket, string Username, string Filename, long? Size, string OwnerKey, DateTimeOffset ExpiresAtUtc, string ContentType);
