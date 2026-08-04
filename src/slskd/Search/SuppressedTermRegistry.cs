// <copyright file="SuppressedTermRegistry.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Registry of known Soulseek network-suppressed search terms and their workarounds.
/// The Soulseek network operator(s) suppress certain terms from search results.
/// This registry tracks known blocked terms so fallback logic can prioritize
/// removing or substituting them rather than blindly removing leading terms.
/// </summary>
internal static class SuppressedTermRegistry
{
    /// <summary>
    /// Known suppressed terms mapped to their preferred alternate (if any).
    /// A null alternate means removal is the only workaround.
    /// Add new terms here as they are discovered through user reports or testing.
    /// </summary>
    private static readonly Dictionary<string, string?> KnownSuppressedTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        // "Linkin Park" → search "Park" instead
        ["Linkin"] = null,

        // Single-word artist; removal only
        ["Metallica"] = null,

        // Add more as discovered through user reports or network testing
    };

    /// <summary>
    /// Gets all known suppressed terms.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllSuppressedTerms()
        => KnownSuppressedTerms.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Checks if a term is known to be suppressed by the Soulseek network.
    /// </summary>
    public static bool IsSuppressed(string term)
        => !string.IsNullOrWhiteSpace(term) && KnownSuppressedTerms.ContainsKey(term);

    /// <summary>
    /// Gets the preferred alternate for a suppressed term, or null if removal is the only workaround.
    /// </summary>
    public static string? GetAlternate(string term)
        => !string.IsNullOrWhiteSpace(term) && KnownSuppressedTerms.TryGetValue(term, out var alternate)
            ? alternate
            : null;

    /// <summary>
    /// Finds all suppressed terms in a search query.
    /// </summary>
    public static IReadOnlyList<string> FindSuppressedTermsInQuery(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<string>();
        }

        var terms = searchText.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var found = new List<string>();

        foreach (var term in terms)
        {
            if (IsSuppressed(term))
            {
                found.Add(term);
            }
        }

        return found;
    }
}
