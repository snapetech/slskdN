// <copyright file="ScopedApiKeyDenyByDefaultFilter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Authentication;

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
///     Denies non-wildcard scoped principals on actions without an explicit
///     <see cref="RequireScopeAttribute"/> mapping.
/// </summary>
public sealed class ScopedApiKeyDenyByDefaultFilter : IAuthorizationFilter, IOrderedFilter
{
    public int Order => int.MinValue + 100;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var heldScopes = user.FindAll(SlskdClaims.ScopeClaim)
            .Select(claim => claim.Value)
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (heldScopes.Length == 0 || heldScopes.Contains(SlskdClaims.ScopeAll, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var requiredScopes = context.Filters
            .OfType<RequireScopeAttribute>()
            .Select(attribute => attribute.Scope)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requiredScopes.Length > 0 && requiredScopes.Any(required => heldScopes.Contains(required, StringComparer.OrdinalIgnoreCase)))
        {
            return;
        }

        context.Result = new ObjectResult(new
        {
            error = requiredScopes.Length == 0 ? "scope_mapping_required" : "insufficient_scope",
            required_scopes = requiredScopes,
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
