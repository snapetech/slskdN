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
    public void CreateQueries_RelaxesOnlyLeadingTerms()
    {
        var queries = SmartSearchFallback.CreateQueries("Linkin Park Meteora");

        Assert.Equal(["Park Meteora", "Linkin Meteora"], queries);
    }

    [Fact]
    public void CreateQueries_NormalizesWhitespaceAndBoundsCandidates()
    {
        var queries = SmartSearchFallback.CreateQueries("  Tate   McRae   greedy  ");

        Assert.Equal(["McRae greedy", "Tate greedy"], queries);
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
