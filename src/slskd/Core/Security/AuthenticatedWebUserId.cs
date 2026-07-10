// <copyright file="AuthenticatedWebUserId.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Core.Security;

using System.Security.Claims;

/// <summary>
/// Resolves the stable web-account identity established by the authentication layer.
/// </summary>
public static class AuthenticatedWebUserId
{
    public static string? Resolve(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = user.FindFirstValue(ClaimTypes.Name)?.Trim();
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
