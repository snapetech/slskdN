// <copyright file="SuppressedTermRegistryTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search;

using slskd.Search;
using Xunit;

public sealed class SuppressedTermRegistryTests
{
    [Fact]
    public void IsSuppressed_DetectsKnownTerms()
    {
        Assert.True(SuppressedTermRegistry.IsSuppressed("Linkin"));
        Assert.True(SuppressedTermRegistry.IsSuppressed("linkin"));
        Assert.True(SuppressedTermRegistry.IsSuppressed("LINKIN"));
        Assert.True(SuppressedTermRegistry.IsSuppressed("Metallica"));
    }

    [Fact]
    public void IsSuppressed_RejectsUnknownTerms()
    {
        Assert.False(SuppressedTermRegistry.IsSuppressed("Park"));
        Assert.False(SuppressedTermRegistry.IsSuppressed("Meteora"));
        Assert.False(SuppressedTermRegistry.IsSuppressed("Tate"));
        Assert.False(SuppressedTermRegistry.IsSuppressed("McRae"));
    }

    [Fact]
    public void IsSuppressed_RejectsEmptyOrNull()
    {
        Assert.False(SuppressedTermRegistry.IsSuppressed(string.Empty));
        Assert.False(SuppressedTermRegistry.IsSuppressed("   "));
        Assert.False(SuppressedTermRegistry.IsSuppressed(null!));
    }

    [Fact]
    public void GetAlternate_ReturnsNullForRemovalOnly()
    {
        Assert.Null(SuppressedTermRegistry.GetAlternate("Linkin"));
        Assert.Null(SuppressedTermRegistry.GetAlternate("Metallica"));
    }

    [Fact]
    public void GetAlternate_ReturnsNullForUnknownTerms()
    {
        Assert.Null(SuppressedTermRegistry.GetAlternate("Park"));
        Assert.Null(SuppressedTermRegistry.GetAlternate("Unknown"));
    }

    [Fact]
    public void FindSuppressedTermsInQuery_FindsKnownTerms()
    {
        var found = SuppressedTermRegistry.FindSuppressedTermsInQuery("Linkin Park Meteora");

        Assert.Single(found);
        Assert.Contains("Linkin", found);
    }

    [Fact]
    public void FindSuppressedTermsInQuery_FindsMultipleSuppressedTerms()
    {
        var found = SuppressedTermRegistry.FindSuppressedTermsInQuery("Linkin Metallica Mashup");

        Assert.Equal(2, found.Count);
        Assert.Contains("Linkin", found);
        Assert.Contains("Metallica", found);
    }

    [Fact]
    public void FindSuppressedTermsInQuery_ReturnsEmptyForNoSuppressedTerms()
    {
        var found = SuppressedTermRegistry.FindSuppressedTermsInQuery("Park Meteora Album");

        Assert.Empty(found);
    }

    [Fact]
    public void FindSuppressedTermsInQuery_HandlesEmptyInput()
    {
        Assert.Empty(SuppressedTermRegistry.FindSuppressedTermsInQuery(string.Empty));
        Assert.Empty(SuppressedTermRegistry.FindSuppressedTermsInQuery("   "));
        Assert.Empty(SuppressedTermRegistry.FindSuppressedTermsInQuery(null!));
    }

    [Fact]
    public void GetAllSuppressedTerms_ReturnsAllKnownTerms()
    {
        var all = SuppressedTermRegistry.GetAllSuppressedTerms();

        Assert.Contains("Linkin", all);
        Assert.Contains("Metallica", all);
        Assert.True(all.Count >= 2);
    }
}
