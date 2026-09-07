// <copyright file="WishlistSearchPolicyTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Search;

using System.Collections.Generic;
using slskd.Search;
using Xunit;

public sealed class WishlistSearchPolicyTests
{
    [Theory]
    [InlineData("2005 Asteroid", "Music/2005 Asteroid - Original Mix.mp3", true)]
    [InlineData("2005 Asteroid", "Music/Asteroid - Original Mix.mp3", false)]
    [InlineData("Aglo 2023 Into the Maze", "Aglo/2023/Into the Maze.flac", true)]
    [InlineData("Aglo 2023 Into the Maze", "2023/Into the Maze.flac", false)]
    public void CreateFilenameFilterRequiresEveryPositiveQueryTerm(
        string searchText,
        string filename,
        bool expected)
    {
        var filter = WishlistSearchPolicy.CreateFilenameFilter(searchText);

        Assert.Equal(expected, filter(filename));
    }

    [Fact]
    public void CreateFilenameFilterPreservesExplicitOrAndExclusions()
    {
        var filter = WishlistSearchPolicy.CreateFilenameFilter("2005 Asteroid OR 2006 Comet -demo");

        Assert.True(filter("2005 Asteroid.mp3"));
        Assert.True(filter("2006 Comet.flac"));
        Assert.False(filter("2005 Asteroid demo.mp3"));
        Assert.False(filter("2007 Comet.mp3"));
    }

    [Fact]
    public void FilterResponsesRemovesMismatchingFilesAndEmptyPeers()
    {
        var responses = new[]
        {
            new Response
            {
                Username = "wanted-peer",
                Files = new List<slskd.Search.File>
                {
                    new() { Filename = "2005 Asteroid.mp3", Size = 1 },
                    new() { Filename = "Asteroid.mp3", Size = 2 },
                },
            },
            new Response
            {
                Username = "mismatch-only-peer",
                Files = new List<slskd.Search.File>
                {
                    new() { Filename = "Asteroid.mp3", Size = 3 },
                },
            },
        };

        var filtered = WishlistSearchPolicy.FilterResponses(responses, "2005 Asteroid");

        var response = Assert.Single(filtered);
        var file = Assert.Single(response.Files);
        Assert.Equal("2005 Asteroid.mp3", file.Filename);
        Assert.Equal(1, response.FileCount);
    }
}
