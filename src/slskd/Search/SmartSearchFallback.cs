// <copyright file="SmartSearchFallback.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds tightly bounded fallback queries for Wishlist searches whose exact
/// Soulseek query appears to have been suppressed by network-side term rules.
/// </summary>
internal static class SmartSearchFallback
{
    public const int MinimumResultThreshold = 10;
    public const int MaximumFallbackQueries = 2;
    public const int FallbackTimeoutMilliseconds = 5_000;

    public static int MaximumAdditionalWaitSeconds
        => MaximumFallbackQueries * (FallbackTimeoutMilliseconds / 1_000);

    public static bool IsEnabledForSource(string source)
        => string.Equals(source, "wishlist", StringComparison.OrdinalIgnoreCase);

    public static bool NeedsFallback(
        int responseCount,
        int fileCount,
        int responseLimit = int.MaxValue,
        int fileLimit = int.MaxValue)
    {
        var responseThreshold = Math.Min(MinimumResultThreshold, responseLimit);
        var fileThreshold = Math.Min(MinimumResultThreshold, fileLimit);

        return responseCount < responseThreshold && fileCount < fileThreshold;
    }

    public static IReadOnlyList<string> CreateQueries(string searchText)
    {
        var terms = searchText
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (terms.Length < 3 || ContainsExplicitQuerySyntax(searchText, terms))
        {
            return Array.Empty<string>();
        }

        var queries = new List<string>(MaximumFallbackQueries);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidateCount = Math.Min(MaximumFallbackQueries, terms.Length - 1);

        // Wishlist/Lidarr search text is deliberately ordered as artist first,
        // followed by album/track context. Only relax the leading artist terms;
        // removing title or album terms would make a low-result query noisier.
        for (var removedIndex = 0; removedIndex < candidateCount; removedIndex++)
        {
            var fallback = string.Join(
                ' ',
                terms.Where((_, index) => index != removedIndex));

            if (seen.Add(fallback))
            {
                queries.Add(fallback);
            }
        }

        return queries;
    }

    private static bool ContainsExplicitQuerySyntax(string searchText, IReadOnlyList<string> terms)
    {
        if (searchText.Contains('"') || searchText.Contains('\'') || searchText.Contains('|'))
        {
            return true;
        }

        return terms.Any(term =>
            string.Equals(term, "OR", StringComparison.OrdinalIgnoreCase) ||
            term.StartsWith("-", StringComparison.Ordinal));
    }
}
