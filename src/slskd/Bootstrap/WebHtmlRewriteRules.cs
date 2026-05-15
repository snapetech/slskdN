// <copyright file="WebHtmlRewriteRules.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

public static class WebHtmlRewriteRules
{
    public static IReadOnlyList<(string Pattern, string Replacement)> Create(string urlBase)
    {
        var normalizedUrlBase = string.IsNullOrWhiteSpace(urlBase) || urlBase == "/"
            ? string.Empty
            : (urlBase.StartsWith("/") ? urlBase : "/" + urlBase).TrimEnd('/');

        string Prefix(string path) => string.IsNullOrEmpty(normalizedUrlBase) ? path : $"{normalizedUrlBase}{path}";
        string BaseTag() => $"<head><base href=\"{(string.IsNullOrEmpty(normalizedUrlBase) ? "/" : $"{normalizedUrlBase}/")}\" />";

        return new List<(string Pattern, string Replacement)>
        {
            ("<head>", BaseTag()),
            ("((?:src|href)=\")/assets/", $"$1{Prefix("/assets/")}"),
            ("((?:src|href)=\")/manifest\\.json", $"$1{Prefix("/manifest.json")}"),
            ("((?:src|href)=\")/logo192\\.png", $"$1{Prefix("/logo192.png")}"),
            ("((?:src|href)=\")/logo512\\.png", $"$1{Prefix("/logo512.png")}"),
        };
    }
}
