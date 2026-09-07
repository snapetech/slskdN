// <copyright file="SmartSearchFallbackTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search;

using System;
using slskd.Search;
using Xunit;

public sealed class SmartSearchFallbackTests
{
    [Fact]
    public void CreateQueries_DoesNotRemoveUnknownTerms()
    {
        var queries = SmartSearchFallback.CreateQueries("Park Meteora Album");

        Assert.Empty(queries);
    }

    [Fact]
    public void CreateQueries_PrioritizesKnownSuppressedTerms()
    {
        // "Linkin" is a known suppressed term, so it should be removed first
        var queries = SmartSearchFallback.CreateQueries("Linkin Park Meteora");

        // First fallback should remove "Linkin" (known suppressed), not "Park"
        Assert.Equal(["Park Meteora"], queries);
    }

    [Fact]
    public void CreateQueries_RemovesSuppressedTermFromAnyPosition()
    {
        // Even if the suppressed term is not leading, it should be targeted
        var queries = SmartSearchFallback.CreateQueries("Meteora Linkin Park");

        // A known suppressed term may be removed regardless of its position.
        Assert.Contains("Meteora Park", queries);
        Assert.Single(queries);
    }

    [Fact]
    public void CreateQueries_HandlesMultipleSuppressedTerms()
    {
        // If multiple suppressed terms exist, remove them in order
        var queries = SmartSearchFallback.CreateQueries("Linkin Metallica Mashup");

        // Should have fallbacks removing each suppressed term
        Assert.True(queries.Count >= 1);
        Assert.Contains("Metallica Mashup", queries); // Removed "Linkin"
        Assert.Contains("Linkin Mashup", queries); // Removed "Metallica"
    }

    [Fact]
    public void CreateQueries_DoesNotBroadenNormalWhitespaceSeparatedQueries()
    {
        var queries = SmartSearchFallback.CreateQueries("  Tate   McRae   greedy  ");

        Assert.Empty(queries);
    }

    [Theory]
    [InlineData("Artist Album")]
    [InlineData("\"Linkin Park\" Meteora")]
    [InlineData("Linkin OR Park Meteora")]
    [InlineData("Linkin -Park Meteora")]
    public void CreateQueries_SkipsAmbiguousOrShortSyntax(string searchText)
    {
        Assert.Empty(SmartSearchFallback.CreateQueries(searchText));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(9, 10, false)]
    [InlineData(10, 0, false)]
    [InlineData(25, 25, false)]
    public void NeedsFallback_RequiresBothResponseAndFileCountsToBeLow(
        int responseCount,
        int fileCount,
        bool expected)
    {
        Assert.Equal(expected, SmartSearchFallback.NeedsFallback(responseCount, fileCount));
    }

    [Fact]
    public void NeedsFallback_DoesNotExceedConfiguredSearchLimits()
    {
        Assert.False(SmartSearchFallback.NeedsFallback(1, 1, responseLimit: 1, fileLimit: 1));
        Assert.True(SmartSearchFallback.NeedsFallback(0, 0, responseLimit: 1, fileLimit: 1));
    }

    [Fact]
    public void IsEnabledForSource_IsRestrictedToWishlist()
    {
        Assert.True(SmartSearchFallback.IsEnabledForSource("wishlist"));
        Assert.True(SmartSearchFallback.IsEnabledForSource("WISHLIST"));
        Assert.False(SmartSearchFallback.IsEnabledForSource("user"));
        Assert.False(SmartSearchFallback.IsEnabledForSource("auto-replace"));
    }
}
