// <copyright file="AntiforgeryCookieRecovery.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Security;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

public static class AntiforgeryCookieRecovery
{
    public static AntiforgeryTokenSet? TryGetAndStoreTokens(
        HttpContext context,
        IAntiforgery antiforgery,
        int webPort,
        Action<PathString> onStaleCookiesCleared)
    {
        try
        {
            return antiforgery.GetAndStoreTokens(context);
        }
        catch (Exception ex) when (IsStaleTokenException(ex))
        {
            ClearKnownCookies(context, webPort);
            onStaleCookiesCleared(context.Request.Path);
            return antiforgery.GetAndStoreTokens(context);
        }
    }

    public static bool IsStaleTokenException(Exception exception)
    {
        return FlattenExceptions(exception).Any(innerException =>
            innerException is CryptographicException ||
            innerException.Message.Contains("could not be decrypted", StringComparison.OrdinalIgnoreCase) ||
            innerException.Message.Contains("key ring", StringComparison.OrdinalIgnoreCase));
    }

    public static bool StripKnownCookiesFromRequest(HttpContext context, int webPort)
    {
        var filteredSegments = new List<string>();
        var removed = false;

        foreach (var headerValue in context.Request.Headers.Cookie)
        {
            if (headerValue is null)
            {
                continue;
            }

            foreach (var segment in headerValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = segment.IndexOf('=');
                var cookieName = separatorIndex >= 0 ? segment[..separatorIndex].Trim() : segment.Trim();

                if (IsKnownCookieName(cookieName, webPort))
                {
                    removed = true;
                    continue;
                }

                filteredSegments.Add(segment);
            }
        }

        if (!removed)
        {
            return false;
        }

        if (filteredSegments.Count == 0)
        {
            context.Request.Headers.Remove("Cookie");
        }
        else
        {
            context.Request.Headers.Cookie = string.Join("; ", filteredSegments);
        }

        context.Features.Set<IRequestCookiesFeature>(new RequestCookiesFeature(context.Features));

        return true;
    }

    public static void ClearKnownCookies(HttpContext context, int webPort)
    {
        var options = new CookieOptions
        {
            Path = "/",
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
        };

        context.Response.Cookies.Delete($"XSRF-COOKIE-{webPort}", options);
        context.Response.Cookies.Delete($"XSRF-TOKEN-{webPort}", options);
        context.Response.Cookies.Delete("XSRF-COOKIE", options);
        context.Response.Cookies.Delete("XSRF-TOKEN", options);
    }

    private static bool IsKnownCookieName(string cookieName, int webPort)
    {
        return string.Equals(cookieName, $"XSRF-COOKIE-{webPort}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cookieName, $"XSRF-TOKEN-{webPort}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cookieName, "XSRF-COOKIE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(cookieName, "XSRF-TOKEN", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<Exception> FlattenExceptions(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
            {
                foreach (var flattenedInnerException in FlattenExceptions(innerException))
                {
                    yield return flattenedInnerException;
                }
            }

            yield break;
        }

        yield return exception;

        if (exception.InnerException is not null)
        {
            foreach (var innerException in FlattenExceptions(exception.InnerException))
            {
                yield return innerException;
            }
        }
    }
}
