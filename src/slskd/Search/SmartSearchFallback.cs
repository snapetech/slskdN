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

        // Only relax terms whose suppression has been observed and recorded.
        // Removing an unknown artist, album, or year creates plausible-looking
        // false positives and was the source of broad Wishlist mismatches.
        var suppressedTerms = SuppressedTermRegistry.FindSuppressedTermsInQuery(searchText);
        foreach (var suppressedTerm in suppressedTerms)
        {
            if (queries.Count >= MaximumFallbackQueries)
            {
                break;
            }

            var fallback = string.Join(
                ' ',
                terms.Where(t => !string.Equals(t, suppressedTerm, StringComparison.OrdinalIgnoreCase)));

            if (fallback.Length > 0 && seen.Add(fallback))
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
