// <copyright file="PodApiAuthorizer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.PodCore.API;

using System.Security.Claims;
using slskd.Core.Security;

/// <summary>
/// Resolves the authenticated web identity and maps it to pod membership.
/// </summary>
public static class PodApiAuthorizer
{
    public static string? GetAuthenticatedPeerId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identity = user.FindFirstValue(ClaimTypes.Name)?.Trim();
        return string.IsNullOrWhiteSpace(identity) ? null : identity;
    }

    public static async Task<PodApiAccess> GetAccessAsync(
        ClaimsPrincipal user,
        IPodService podService,
        string podId,
        CancellationToken cancellationToken)
    {
        if (user.IsInRole(AuthRole.AdministratorOnly))
        {
            return new PodApiAccess(GetAuthenticatedPeerId(user), IsMember: true, CanModerate: true, IsAdministrator: true);
        }

        var peerId = GetAuthenticatedPeerId(user);
        if (peerId is null)
        {
            return PodApiAccess.Denied;
        }

        var members = await podService.GetMembersAsync(podId, cancellationToken);
        var member = members.FirstOrDefault(candidate =>
            !candidate.IsBanned &&
            string.Equals(candidate.PeerId, peerId, StringComparison.Ordinal));
        if (member is null)
        {
            return new PodApiAccess(peerId, IsMember: false, CanModerate: false, IsAdministrator: false)
            {
                Members = members,
            };
        }

        var canModerate = member.Role.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
                          member.Role.Equals("mod", StringComparison.OrdinalIgnoreCase);
        return new PodApiAccess(peerId, IsMember: true, CanModerate: canModerate, IsAdministrator: false)
        {
            Members = members,
        };
    }
}

public sealed record PodApiAccess(string? PeerId, bool IsMember, bool CanModerate, bool IsAdministrator)
{
    public static PodApiAccess Denied { get; } = new(null, false, false, false);

    public IReadOnlyList<PodMember>? Members { get; init; }
}
