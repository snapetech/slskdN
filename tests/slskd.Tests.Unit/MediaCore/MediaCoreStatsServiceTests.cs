// <copyright file="MediaCoreStatsServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.MediaCore;
using Xunit;

public class MediaCoreStatsServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_UsesOneRegistrySnapshotForContentAndIpldStats()
    {
        var registry = new Mock<IContentIdRegistry>();
        var descriptorRetriever = new Mock<IDescriptorRetriever>();
        var ipldMapper = new Mock<IIpldMapper>();
        var contentPublisher = new Mock<IContentDescriptorPublisher>();
        var mappingsByDomain = new Dictionary<string, int>
        {
            ["audio"] = 1,
            ["video"] = 1,
            ["image"] = 1,
        };
        var contentIdsByDomain = new Dictionary<string, IReadOnlyList<string>>
        {
            ["audio"] = new[] { "content:audio:track:one" },
            ["video"] = new[] { "content:video:movie:two" },
            ["image"] = new[] { "content:image:artwork:three" },
        };

        registry
            .Setup(r => r.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentIdRegistryStats(3, 3, mappingsByDomain));
        registry
            .Setup(r => r.FindByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string domain, CancellationToken _) => contentIdsByDomain[domain]);
        descriptorRetriever
            .Setup(r => r.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetrievalStats(0, 0, 0, 0, TimeSpan.Zero, 0, 0, default));
        ipldMapper
            .Setup(m => m.GetGraphAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string contentId, int _, CancellationToken _) => new ContentGraph(
                contentId,
                new[] { new ContentGraphNode(contentId, Array.Empty<IpldLink>(), Array.Empty<string>()) },
                Array.Empty<ContentGraphPath>()));
        ipldMapper
            .Setup(m => m.ValidateLinksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IpldValidationResult(true, Array.Empty<string>(), Array.Empty<string>(), 0));
        contentPublisher
            .Setup(p => p.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishingStats(0, 0, 0, default, new Dictionary<string, int>(), 0, 0));

        var service = new MediaCoreStatsService(
            Mock.Of<ILogger<MediaCoreStatsService>>(),
            registry.Object,
            descriptorRetriever.Object,
            Mock.Of<IFuzzyMatcher>(),
            ipldMapper.Object,
            Mock.Of<IPerceptualHasher>(),
            Mock.Of<IMetadataPortability>(),
            contentPublisher.Object);

        var dashboard = await service.GetDashboardAsync();

        Assert.Equal(3, dashboard.ContentRegistry.TotalMappings);
        Assert.Equal(3, dashboard.ContentRegistry.MappingsByType.Count);
        Assert.Equal(3, dashboard.IpldMapping.TotalGraphs);
        registry.Verify(r => r.GetStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
        foreach (var domain in mappingsByDomain.Keys)
        {
            registry.Verify(
                r => r.FindByDomainAsync(domain, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
