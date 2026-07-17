// <copyright file="MediaCoreSwarmServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.MediaCore;
using slskd.Transfers.MultiSource;
using Xunit;

public sealed class MediaCoreSwarmServiceTests
{
    [Fact]
    public async Task GroupSourcesByContentIdAsync_EmptyGroupsSkipDiscovery()
    {
        var contentRegistry = new Mock<IContentIdRegistry>();
        var service = new MediaCoreSwarmService(
            NullLogger<MediaCoreSwarmService>.Instance,
            contentRegistry.Object,
            Mock.Of<IFuzzyMatcher>(),
            Mock.Of<IDescriptorRetriever>(),
            Mock.Of<IMediaCoreSwarmIntelligence>());
        var verification = new ContentVerificationResult
        {
            Filename = "empty.flac",
            FileSize = 123,
        };

        var grouping = await service.GroupSourcesByContentIdAsync(verification);

        Assert.Empty(grouping.GroupsByContentId[grouping.PrimaryContentId].Sources);
        contentRegistry.Verify(
            registry => registry.FindByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GroupSourcesByContentIdAsync_PreservesDiscoveredVariantGrouping()
    {
        const string contentId = "content:audio:track:track";
        var descriptor = new ContentDescriptor { ContentId = contentId, Codec = "flac" };
        var contentRegistry = new Mock<IContentIdRegistry>();
        contentRegistry
            .Setup(registry => registry.FindByDomainAsync("audio", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { contentId });
        contentRegistry
            .Setup(registry => registry.FindByDomainAsync("video", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var fuzzyMatcher = new Mock<IFuzzyMatcher>();
        fuzzyMatcher
            .Setup(matcher => matcher.Score(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(0.9);
        var descriptorRetriever = new Mock<IDescriptorRetriever>();
        descriptorRetriever
            .Setup(retriever => retriever.RetrieveAsync(contentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescriptorRetrievalResult(
                Found: true,
                Descriptor: descriptor,
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: true,
                Verification: null));
        var service = new MediaCoreSwarmService(
            NullLogger<MediaCoreSwarmService>.Instance,
            contentRegistry.Object,
            fuzzyMatcher.Object,
            descriptorRetriever.Object,
            Mock.Of<IMediaCoreSwarmIntelligence>());
        var firstSource = new VerifiedSource { Username = "first-peer", FullPath = "music/track.flac" };
        var verification = new ContentVerificationResult
        {
            Filename = "track.flac",
            FileSize = 123,
            SourcesByHash = new Dictionary<string, List<VerifiedSource>>
            {
                ["first-hash"] = new() { firstSource },
                ["second-hash"] = new()
                {
                    new VerifiedSource { Username = "second-peer", FullPath = "other/track.flac" },
                },
            },
        };

        var grouping = await service.GroupSourcesByContentIdAsync(verification);

        Assert.Equal(contentId, grouping.PrimaryContentId);
        Assert.Equal(new[] { contentId }, grouping.RecommendedContentIds);
        var group = Assert.Single(grouping.GroupsByContentId).Value;
        Assert.Equal(1.0, group.QualityScore);
        Assert.Equal(new[] { firstSource }, group.Sources);
        descriptorRetriever.Verify(
            retriever => retriever.RetrieveAsync(contentId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GroupSourcesByContentIdAsync_DiscoversSharedTargetOnce()
    {
        const int hashGroupCount = 100;
        var contentRegistry = new Mock<IContentIdRegistry>();
        contentRegistry
            .Setup(registry => registry.FindByDomainAsync("audio", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        contentRegistry
            .Setup(registry => registry.FindByDomainAsync("video", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var service = new MediaCoreSwarmService(
            NullLogger<MediaCoreSwarmService>.Instance,
            contentRegistry.Object,
            Mock.Of<IFuzzyMatcher>(),
            Mock.Of<IDescriptorRetriever>(),
            Mock.Of<IMediaCoreSwarmIntelligence>());
        var verification = new ContentVerificationResult
        {
            Filename = "shared-target.flac",
            FileSize = 123,
            SourcesByHash = Enumerable.Range(0, hashGroupCount)
                .ToDictionary(
                    index => $"hash-{index:D3}",
                    index => new List<VerifiedSource>
                    {
                        new() { Username = $"peer-{index:D3}", FullPath = "shared-target.flac" },
                    }),
        };

        var grouping = await service.GroupSourcesByContentIdAsync(verification);

        Assert.Equal(hashGroupCount, grouping.GroupsByContentId[grouping.PrimaryContentId].Sources.Count);
        contentRegistry.Verify(
            registry => registry.FindByDomainAsync("audio", It.IsAny<CancellationToken>()),
            Times.Once);
        contentRegistry.Verify(
            registry => registry.FindByDomainAsync("video", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
