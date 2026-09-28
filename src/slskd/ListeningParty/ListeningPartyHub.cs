// <copyright file="ListeningPartyHub.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.ListeningParty;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using slskd.Authentication;
using slskd.PodCore;
using slskd.PodCore.API;

/// <summary>
///     SignalR fan-out for pod listen-along state.
/// </summary>
[Authorize(Policy = AuthPolicy.Any)]
public sealed class ListeningPartyHub : Hub
{
    private readonly IPodService _pods;

    private readonly IListeningPartyService _parties;

    public ListeningPartyHub(IPodService pods, IListeningPartyService parties)
    {
        _pods = pods;
        _parties = parties;
    }

    public async Task JoinParty(string podId, string channelId)
    {
        podId = podId?.Trim() ?? string.Empty;
        channelId = channelId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId) || string.IsNullOrWhiteSpace(channelId) || podId.Length > 512 || channelId.Length > 512)
        {
            throw new HubException("Pod and channel are required.");
        }

        var access = await PodApiAuthorizer.GetAccessAsync(Context.User!, _pods, podId, Context.ConnectionAborted);
        if (!access.IsMember)
        {
            throw new HubException("Pod membership is required to join listen-along.");
        }

        Context.ConnectionAborted.ThrowIfCancellationRequested();
        if (!_parties.Subscribe(Context.ConnectionId, podId, channelId, Context.User!))
        {
            throw new HubException("Too many listen-along subscriptions.");
        }
    }

    public Task LeaveParty(string podId, string channelId)
    {
        _parties.Unsubscribe(Context.ConnectionId, podId?.Trim() ?? string.Empty, channelId?.Trim() ?? string.Empty);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _parties.Disconnect(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
