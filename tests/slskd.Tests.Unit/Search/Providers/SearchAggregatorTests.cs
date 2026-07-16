// <copyright file="SearchAggregatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search.Providers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using slskd.Search;
using slskd.Search.Providers;
using Xunit;
using NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<slskd.Search.Providers.SearchAggregator>;

[Collection(AllocationTestCollection.Name)]
public class SearchAggregatorTests
{
    private readonly ILogger<SearchAggregator> _logger = NullLogger.Instance;

    private SearchAggregator CreateAggregator(string preferredPrimarySource = "pod")
    {
        var serilogLogger = Log.ForContext<SearchAggregator>();
        return new SearchAggregator(serilogLogger, preferredPrimarySource);
    }

    [Fact]
    public async Task AggregateAsync_MergesPodAndSceneResults_WithDeduplication()
    {
        // Arrange
        var aggregator = CreateAggregator();
        // Use same username for both to ensure deduplication works
        var providers = new List<ISearchProvider>
        {
            CreateMockProvider("pod", new List<SearchResult>
            {
                CreateSearchResult("pod", "test.flac", 1000, "pod", "same-user")
            }),
            CreateMockProvider("scene", new List<SearchResult>
            {
                CreateSearchResult("scene", "test.flac", 1000, "scene", "same-user")
            })
        };
        var request = new SearchRequest
        {
            SearchText = "test",
            TimeoutSeconds = 5,
            ResponseLimit = 100,
            FileLimit = 10000
        };

        // Act
        var results = await aggregator.AggregateAsync(providers, request, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var result = results.First();
        Assert.Contains("pod", result.SourceProviders);
        Assert.Contains("scene", result.SourceProviders);
        Assert.Equal(2, result.SourceProviders.Count);
        Assert.Equal("pod", result.PrimarySource); // Preferred source
    }

    [Fact]
    public async Task AggregateAsync_KeepsSeparateResults_WhenNoDeduplicationMatch()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var providers = new List<ISearchProvider>
        {
            CreateMockProvider("pod", new List<SearchResult>
            {
                CreateSearchResult("pod", "file1.flac", 1000, "pod")
            }),
            CreateMockProvider("scene", new List<SearchResult>
            {
                CreateSearchResult("scene", "file2.flac", 2000, "scene")
            })
        };
        var request = new SearchRequest
        {
            SearchText = "test",
            TimeoutSeconds = 5,
            ResponseLimit = 100,
            FileLimit = 10000
        };

        // Act
        var results = await aggregator.AggregateAsync(providers, request, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Single(r.SourceProviders));
    }

    [Fact]
    public async Task AggregateAsync_PrefersPod_WhenBothAvailable()
    {
        // Arrange
        var aggregator = CreateAggregator("pod");
        // Use same username for both to ensure deduplication works
        var providers = new List<ISearchProvider>
        {
            CreateMockProvider("pod", new List<SearchResult>
            {
                CreateSearchResult("pod", "test.flac", 1000, "pod", "same-user")
            }),
            CreateMockProvider("scene", new List<SearchResult>
            {
                CreateSearchResult("scene", "test.flac", 1000, "scene", "same-user")
            })
        };
        var request = new SearchRequest
        {
            SearchText = "test",
            TimeoutSeconds = 5,
            ResponseLimit = 100,
            FileLimit = 10000
        };

        // Act
        var results = await aggregator.AggregateAsync(providers, request, CancellationToken.None);

        // Assert
        Assert.Single(results); // Should be merged
        var result = results.First();
        Assert.Equal("pod", result.PrimarySource);
    }

    [Fact]
    public async Task AggregateAsync_PrefersScene_WhenConfigured()
    {
        // Arrange
        var aggregator = CreateAggregator("scene");
        // Use same username for both to ensure deduplication works
        var providers = new List<ISearchProvider>
        {
            CreateMockProvider("pod", new List<SearchResult>
            {
                CreateSearchResult("pod", "test.flac", 1000, "pod", "same-user")
            }),
            CreateMockProvider("scene", new List<SearchResult>
            {
                CreateSearchResult("scene", "test.flac", 1000, "scene", "same-user")
            })
        };
        var request = new SearchRequest
        {
            SearchText = "test",
            TimeoutSeconds = 5,
            ResponseLimit = 100,
            FileLimit = 10000
        };

        // Act
        var results = await aggregator.AggregateAsync(providers, request, CancellationToken.None);

        // Assert
        Assert.Single(results); // Should be merged
        var result = results.First();
        Assert.Equal("scene", result.PrimarySource);
    }

    [Fact]
    public async Task AggregateAsync_Continues_WhenAProviderFails()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var successfulProvider = CreateMockProvider("pod", new List<SearchResult>
        {
            CreateSearchResult("pod", "test.flac", 1000, "pod", "same-user")
        });

        var failingProvider = new Mock<ISearchProvider>();
        failingProvider.Setup(p => p.Name).Returns("scene");
        failingProvider.Setup(p => p.StartSearchAsync(
                It.IsAny<SearchRequest>(),
                It.IsAny<ISearchResultSink>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider failed"));

        var request = new SearchRequest
        {
            SearchText = "test",
            TimeoutSeconds = 5,
            ResponseLimit = 100,
            FileLimit = 10000
        };

        // Act
        var results = await aggregator.AggregateAsync(
            new[] { successfulProvider, failingProvider.Object },
            request,
            CancellationToken.None);

        // Assert
        var result = Assert.Single(results);
        Assert.Equal("pod", result.PrimarySource);
        Assert.Equal(new[] { "pod" }, result.SourceProviders);
    }

    [Fact]
    public void MergeResults_LargeUniqueInputAllocationBaseline()
    {
        const int resultCount = 100_000;
        var aggregator = CreateAggregator();
        var results = Enumerable.Range(0, resultCount)
            .Select(index => CreateSearchResult("pod", $"Artist/Album/Track-{index}.FLAC", index, "pod"))
            .ToArray();
        _ = aggregator.MergeResults([CreateSearchResult("pod", "warm.flac", 1, "pod")]);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var merged = aggregator.MergeResults(results);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(resultCount, merged.Count);
        Assert.Same(results[0], merged[0]);
        Assert.Same(results[^1], merged[^1]);
        Assert.True(
            allocatedBytes < 13_500_000,
            $"Expected streaming normalized aggregation below 13,500,000 allocated bytes, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void MergeResults_LargeDuplicateInputKeepsCapacityHintBounded()
    {
        const int resultCount = 100_000;
        var aggregator = CreateAggregator();
        var duplicate = CreateSearchResult("pod", "Artist/Album/Track.FLAC", 1234, "pod");
        var results = Enumerable.Repeat(duplicate, resultCount).ToArray();
        _ = aggregator.MergeResults([duplicate]);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var merged = aggregator.MergeResults(results);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Same(duplicate, Assert.Single(merged));
        Assert.True(
            allocatedBytes < 32_768,
            $"Expected duplicate aggregation below 32,768 allocated bytes, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void MergeResults_NormalizesCaseSlashesAndOuterWhitespace()
    {
        var aggregator = CreateAggregator();
        var first = CreateSearchResult("scene", "  ARTIST\\Album\\Track.FLAC  ", 1234, "scene");
        var second = CreateSearchResult("pod", "artist/album/track.flac", 1234, "pod");

        var result = Assert.Single(aggregator.MergeResults([first, second]));

        Assert.Same(first, result);
        Assert.Equal(["scene", "pod"], result.SourceProviders);
        Assert.Equal("pod", result.PrimarySource);
        Assert.NotNull(result.PodContentRef);
    }

    [Fact]
    public void MergeResults_NormalizationMatchesLegacyUnicodeTransformation()
    {
        const int sampleCount = 1024;
        var aggregator = CreateAggregator();
        string[] supplementarySamples = ["\U00010400", "\U00010428", "\U0001F600"];
        var expectedCount = sampleCount + supplementarySamples.Length;
        var results = new List<SearchResult>(expectedCount * 2);
        for (var index = 0; index < sampleCount; index++)
        {
            var character = (char)(index * char.MaxValue / (sampleCount - 1));
            var original = $" \tFolder\\{character}Track.FLAC \r\n";
            var normalized = original.ToLowerInvariant().Replace('\\', '/').Trim();
            results.Add(CreateSearchResult("scene", original, index, "scene"));
            results.Add(CreateSearchResult("pod", normalized, index, "pod"));
        }

        for (var index = 0; index < supplementarySamples.Length; index++)
        {
            var original = $" Folder\\{supplementarySamples[index]}Track.FLAC ";
            var normalized = original.ToLowerInvariant().Replace('\\', '/').Trim();
            var size = sampleCount + index;
            results.Add(CreateSearchResult("scene", original, size, "scene"));
            results.Add(CreateSearchResult("pod", normalized, size, "pod"));
        }

        var merged = aggregator.MergeResults(results);

        Assert.Equal(expectedCount, merged.Count);
        Assert.All(merged, result => Assert.Equal(2, result.SourceProviders.Count));
    }

    [Fact]
    public void MergeResults_TreatsNullAndEmptyFilenamesEqually()
    {
        var aggregator = CreateAggregator();
        var first = CreateSearchResult("scene", null, 1234, "scene");
        var second = CreateSearchResult("pod", string.Empty, 1234, "pod");

        var result = Assert.Single(aggregator.MergeResults([first, second]));

        Assert.Same(first, result);
        Assert.Equal(["scene", "pod"], result.SourceProviders);
    }

    [Fact]
    public void MergeResults_UnicodeLowercaseThatBecomesAsciiUsesAsciiKey()
    {
        var aggregator = CreateAggregator();
        var first = CreateSearchResult("scene", "\u212A.flac", 1234, "scene");
        var second = CreateSearchResult("pod", "K.FLAC", 1234, "pod");

        var result = Assert.Single(aggregator.MergeResults([first, second]));

        Assert.Same(first, result);
        Assert.Equal(["scene", "pod"], result.SourceProviders);
    }

    [Fact]
    public void MergeResults_PreservesLegacyLowercaseUnicodeDistinctions()
    {
        const string CapitalSigma = "\u03A3.flac";
        const string FinalSigma = "\u03C2.flac";
        Assert.NotEqual(CapitalSigma.ToLowerInvariant(), FinalSigma.ToLowerInvariant());
        var aggregator = CreateAggregator();

        var results = aggregator.MergeResults(
        [
            CreateSearchResult("scene", CapitalSigma, 1234, "scene"),
            CreateSearchResult("pod", FinalSigma, 1234, "pod"),
        ]);

        Assert.Equal(2, results.Count);
    }

    private ISearchProvider CreateMockProvider(string name, List<SearchResult> results)
    {
        var mock = new Mock<ISearchProvider>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.StartSearchAsync(
                It.IsAny<SearchRequest>(),
                It.IsAny<ISearchResultSink>(),
                It.IsAny<CancellationToken>()))
            .Returns<SearchRequest, ISearchResultSink, CancellationToken>((req, sink, ct) =>
            {
                foreach (var result in results)
                {
                    sink.AddResult(result);
                }
                return Task.CompletedTask;
            });
        return mock.Object;
    }

    private SearchResult CreateSearchResult(string provider, string filename, long size, string primarySource, string username = null)
    {
        var response = new Response
        {
            Username = username ?? (provider == "pod" ? "pod-peer" : "scene-user"),
            Files = new List<File>
            {
                new File { Filename = filename, Size = size }
            },
            FileCount = 1,
            SourceProviders = new List<string> { provider },
            PrimarySource = primarySource
        };

        PodContentRef? podRef = null;
        SceneContentRef? sceneRef = null;

        if (provider == "pod")
        {
            podRef = new PodContentRef
            {
                ContentId = $"content:{filename}",
                Hash = null
            };
            response.PodContentRef = podRef;
        }
        else
        {
            sceneRef = new SceneContentRef
            {
                Username = "scene-user",
                Filename = filename,
                Size = size
            };
            response.SceneContentRef = sceneRef;
        }

        return new SearchResult
        {
            Provider = provider,
            SourceProviders = new List<string> { provider },
            PrimarySource = primarySource,
            Response = response,
            PodContentRef = podRef,
            SceneContentRef = sceneRef
        };
    }
}
