// <copyright file="WishlistFilterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Wishlist;

using slskd.Wishlist;
using Xunit;

public class WishlistFilterTests
{
    [Theory]
    [InlineData("flac -\"inner space vol. 2\"", "Music/Inner Space Vol. 1/01.flac", true)]
    [InlineData("flac -\"inner space vol. 2\"", "Music/Inner Space Vol. 2/01.flac", false)]
    public void Filter_supports_quoted_exclusion_phrases(string filter, string filename, bool expected)
    {
        Assert.Equal(expected, WishlistService.CreateSearchFileFilter(filter)(filename));
    }

    [Fact]
    public void Ignore_rule_matches_only_the_same_peer_and_normalized_directory()
    {
        WishlistIgnoredResult[] rules =
        [
            new() { Username = "peer", Directory = "Music/Artist/Album" },
        ];

        Assert.True(WishlistService.IsIgnored(rules, "PEER", @"Music\Artist\Album\01.flac"));
        Assert.False(WishlistService.IsIgnored(rules, "peer", @"Music\Artist\Other\01.flac"));
        Assert.False(WishlistService.IsIgnored(rules, "other-peer", @"Music\Artist\Album\01.flac"));
    }
}
