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

    public ListeningPartyHub(IPodService pods)
    {
        _pods = pods;
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

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(podId, channelId), Context.ConnectionAborted);
    }

    public Task LeaveParty(string podId, string channelId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(podId, channelId));
    }

    internal static string GroupName(string podId, string channelId)
    {
        return $"party:{podId?.Trim()}:{channelId?.Trim()}";
    }
}
