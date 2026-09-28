// <copyright file="IListeningPartyService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.ListeningParty;

using System.Security.Claims;

public interface IListeningPartyService
{
    bool Subscribe(string connectionId, string podId, string channelId, ClaimsPrincipal user);

    void Unsubscribe(string connectionId, string podId, string channelId);

    void Disconnect(string connectionId);

    Task<ListeningPartyEvent?> GetStateAsync(string podId, string channelId, CancellationToken cancellationToken = default);

    Task<ListeningPartyEvent?> GetStateByPartyIdAsync(string partyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListeningPartyAnnouncement>> ListDirectoryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListeningPartyAnnouncement>> RefreshDirectoryAsync(CancellationToken cancellationToken = default);

    Task<ListeningPartyEvent> PublishAsync(ListeningPartyEvent partyEvent, CancellationToken cancellationToken = default);
}
