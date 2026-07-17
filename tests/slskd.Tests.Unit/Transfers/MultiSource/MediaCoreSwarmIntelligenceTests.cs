// <copyright file="MediaCoreSwarmIntelligenceTests.cs" company="slskdN Team">
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

public sealed class MediaCoreSwarmIntelligenceTests
{
    [Fact]
    public async Task PredictOptimalConfigurationAsync_EnumeratesPeersOnceAndPreservesCapabilityAnalysis()
    {
        const string ContentId = "content:audio:track:target";
        var enumerationCount = 0;
        var peers = new[]
        {
            new PeerCapability("excellent", 100, new[] { ContentId }, PeerReliability.Excellent, TimeSpan.FromSeconds(1)),
            new PeerCapability("good", 200, new[] { ContentId }, PeerReliability.Good, TimeSpan.FromSeconds(1)),
            new PeerCapability("fair-fast", 1_000, new[] { ContentId }, PeerReliability.Fair, TimeSpan.FromSeconds(1)),
            new PeerCapability("poor", 500, new[] { ContentId }, PeerReliability.Poor, TimeSpan.FromSeconds(1)),
            new PeerCapability("video-only", 10_000, new[] { "content:video:movie:other" }, PeerReliability.Unreliable, TimeSpan.FromSeconds(1)),
        };
        IEnumerable<PeerCapability> EnumeratePeers()
        {
            enumerationCount++;
            foreach (var peer in peers)
            {
                yield return peer;
            }
        }

        var result = await CreateService(ContentId).PredictOptimalConfigurationAsync(ContentId, EnumeratePeers());

        Assert.Equal(1, enumerationCount);
        Assert.Equal(SwarmStrategy.QualityOptimized, result.RecommendedStrategy);
        Assert.Equal(4, result.OptimalPeerCount);
        Assert.Contains("4 compatible peers", result.Reasoning, StringComparison.Ordinal);
        Assert.Contains("1 fast peers", result.Reasoning, StringComparison.Ordinal);
        Assert.Contains("3 reliable peers", result.Reasoning, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PredictOptimalConfigurationAsync_RepeatedContentIdsBoundAllocation()
    {
        const string ContentId = "content:audio:track:allocation";
        var service = CreateService(ContentId);
        var peers = Enumerable.Range(0, 10_000)
            .Select(index => new PeerCapability(
                Username: $"peer-{index:D5}",
                AverageSpeed: index + 1,
                SupportedContentIds: new[] { ContentId },
                Reliability: (PeerReliability)(index % 5),
                AverageResponseTime: TimeSpan.FromMilliseconds(index % 1_000)))
            .ToList();

        for (var iteration = 0; iteration < 5; iteration++)
        {
            await service.PredictOptimalConfigurationAsync(ContentId, peers);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await service.PredictOptimalConfigurationAsync(ContentId, peers);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.NotEmpty(result.RecommendedPeers);
        Assert.True(allocatedBytes < 1_200_000, $"Allocated {allocatedBytes:N0} bytes.");
    }

    private static MediaCoreSwarmIntelligence CreateService(string contentId)
    {
        var descriptorRetriever = new Mock<IDescriptorRetriever>();
        descriptorRetriever
            .Setup(retriever => retriever.RetrieveAsync(contentId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescriptorRetrievalResult(
                Found: true,
                Descriptor: new ContentDescriptor { ContentId = contentId, Codec = "flac" },
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: true,
                Verification: null));
        return new MediaCoreSwarmIntelligence(
            NullLogger<MediaCoreSwarmIntelligence>.Instance,
            descriptorRetriever.Object,
            Mock.Of<IContentIdRegistry>());
    }
}
