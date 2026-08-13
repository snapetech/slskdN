// <copyright file="DownloadFilterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Transfers.Downloads;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using slskd.Transfers.Downloads;
using Xunit;

public sealed class DownloadFilterTests
{
    [Theory]
    [InlineData(@"Music\A Cappella\Track.flac", "a cappella", "a cappella")]
    [InlineData(@"Music/Instrumental/Track.flac", "instrumental", "instrumental")]
    [InlineData(@"Music/Live.Track.flac", ".", ".")]
    public void GetMatchingExclusion_IsCaseInsensitiveLiteralAndPathSeparatorAgnostic(
        string filename,
        string exclusion,
        string expected)
    {
        var result = DownloadFilter.GetMatchingExclusion(filename, new[] { exclusion });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetMatchingExclusion_IgnoresBlankTermsAndReturnsConfiguredTerm()
    {
        var result = DownloadFilter.GetMatchingExclusion(
            @"Music\Instrumental\Track.flac",
            new[] { "  ", " INSTRUMENTAL " });

        Assert.Equal("INSTRUMENTAL", result);
    }

    [Fact]
    public void DownloadFilterOptions_RejectsBlankAndOverlongTerms()
    {
        var options = new slskd.Options.FiltersOptions.DownloadFilterOptions
        {
            Exclude = new[] { "  ", new string('x', 257) },
        };

        var results = options.Validate(new ValidationContext(options)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, result => result.ErrorMessage!.Contains("must not be blank", StringComparison.Ordinal));
        Assert.Contains(results, result => result.ErrorMessage!.Contains("maximum length", StringComparison.Ordinal));
    }
}
