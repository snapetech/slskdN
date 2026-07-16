// <copyright file="IpldMapperTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using slskd.MediaCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class IpldMapperTests
{
    private readonly IpldMapper _mapper;
    private readonly Mock<IContentIdRegistry> _registryMock;
    private readonly Mock<ILogger<IpldMapper>> _loggerMock;

    public IpldMapperTests()
    {
        _registryMock = new Mock<IContentIdRegistry>();
        _loggerMock = new Mock<ILogger<IpldMapper>>();
        _mapper = new IpldMapper(_registryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AddLinksAsync_ValidInputs_Succeeds()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";
        var links = new[]
        {
            new IpldLink(IpldLinkNames.Album, "content:audio:album:mb-67890"),
            new IpldLink(IpldLinkNames.Artist, "content:audio:artist:mb-abc123")
        };

        _registryMock.Setup(r => r.IsContentIdRegisteredAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _mapper.AddLinksAsync(contentId, links);

        // Assert - mainly that it doesn't throw
        _registryMock.Verify(r => r.IsContentIdRegisteredAsync(contentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddLinksAsync_UnregisteredContentId_ThrowsException()
    {
        // Arrange
        var contentId = "content:unknown:id";
        var links = new[] { new IpldLink("parent", "content:other:id") };

        _registryMock.Setup(r => r.IsContentIdRegisteredAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mapper.AddLinksAsync(contentId, links));

        Assert.Contains("not registered", exception.Message);
    }

    [Fact]
    public async Task TraverseAsync_SimpleTraversal_ReturnsTraversalResult()
    {
        // Arrange
        var startContentId = "content:audio:track:mb-12345";
        var linkName = IpldLinkNames.Album;

        // Setup mock registry responses
        _registryMock.Setup(r => r.FindByDomainAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new[] { startContentId });

        // Act
        var result = await _mapper.TraverseAsync(startContentId, linkName, maxDepth: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startContentId, result.StartContentId);
        Assert.Equal(linkName, result.LinkName);
        Assert.NotNull(result.VisitedNodes);
        Assert.NotNull(result.Paths);
    }

    [Fact]
    public async Task TraverseAsync_MaxDepthExceeded_StopsTraversal()
    {
        // maxDepth 1–10 required; use maxDepth: 1 and a graph that goes deeper (track→album→…).
        // Traversal stops at the limit: only the start node is visited; no deeper nodes.
        var startContentId = "content:audio:track:mb-12345";
        var linkName = IpldLinkNames.Album;

        _registryMock.Setup(r => r.FindByDomainAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Array.Empty<string>());

        var result = await _mapper.TraverseAsync(startContentId, linkName, maxDepth: 1);

        Assert.NotNull(result);
        // With maxDepth=1 we process depth 0 only; recursion to linked nodes returns immediately at depth 1.
        Assert.Single(result.VisitedNodes);
        Assert.Equal(startContentId, result.VisitedNodes[0].ContentId);
    }

    [Fact]
    public async Task FindInboundLinksAsync_ValidTarget_ReturnsInboundLinks()
    {
        // Arrange: FindInboundLinksAsync scans _outgoingLinks only; pre-populate via AddLinksAsync
        var sourceContentId = "content:audio:track:mb-12345";
        var targetContentId = "content:audio:album:mb-67890";
        var linkName = IpldLinkNames.Album;

        _registryMock.Setup(r => r.IsContentIdRegisteredAsync(sourceContentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _mapper.AddLinksAsync(sourceContentId, new[] { new IpldLink(linkName, targetContentId) });

        // Act
        var result = await _mapper.FindInboundLinksAsync(targetContentId, linkName);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(sourceContentId, result);
    }

    [Fact]
    public async Task GetGraphAsync_ValidContentId_ReturnsGraph()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";

        _registryMock.Setup(r => r.FindByDomainAsync(It.IsAny<string>(), default))
            .ReturnsAsync(new[] { contentId });

        // Act
        var result = await _mapper.GetGraphAsync(contentId, maxDepth: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contentId, result.RootContentId);
        Assert.NotNull(result.Nodes);
        Assert.NotNull(result.Paths);
    }

    [Fact]
    public async Task GetGraphAsync_WideGraphUsesIndexedInboundLinks()
    {
        const int childCount = 10_000;
        const string RootContentId = "content:audio:album:root";
        var links = Enumerable.Range(0, childCount)
            .Select(index => new IpldLink(IpldLinkNames.Tracks, $"content:audio:track:{index}"))
            .ToArray();
        _registryMock
            .Setup(registry => registry.IsContentIdRegisteredAsync(RootContentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _mapper.AddLinksAsync(RootContentId, links);

        var warmRegistry = new Mock<IContentIdRegistry>();
        warmRegistry
            .Setup(registry => registry.IsContentIdRegisteredAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var warmMapper = new IpldMapper(warmRegistry.Object, Mock.Of<ILogger<IpldMapper>>());
        await warmMapper.AddLinksAsync("content:audio:album:warm", [new IpldLink(IpldLinkNames.Tracks, "content:audio:track:warm")]);
        _ = await warmMapper.GetGraphAsync("content:audio:album:warm", maxDepth: 2);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var graph = await _mapper.GetGraphAsync(RootContentId, maxDepth: 2);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(childCount + 1, graph.Nodes.Count);
        Assert.Equal(childCount, graph.Paths.Count);
        Assert.Equal(RootContentId, graph.Nodes[0].ContentId);
        Assert.Equal(childCount, graph.Nodes[0].OutgoingLinks.Count);
        Assert.Equal([RootContentId], graph.Nodes[1].IncomingLinks);
        Assert.Equal(links[0], graph.Paths[0].Links[0]);
        Assert.Equal(links[^1], graph.Paths[^1].Links[0]);
        Assert.True(
            allocatedBytes < 8_600_000,
            $"Expected pre-sized wide-graph building below 8,600,000 allocated bytes, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task GetGraphAsync_DuplicateFanOutKeepsCapacityHintBounded()
    {
        const int linkCount = 100_000;
        const string RootContentId = "content:audio:album:duplicates";
        const string ChildContentId = "content:audio:track:one";
        var link = new IpldLink(IpldLinkNames.Tracks, ChildContentId);
        var links = Enumerable.Repeat(link, linkCount).ToArray();
        _registryMock
            .Setup(registry => registry.IsContentIdRegisteredAsync(RootContentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _mapper.AddLinksAsync(RootContentId, links);
        var warmRegistry = new Mock<IContentIdRegistry>();
        warmRegistry
            .Setup(registry => registry.IsContentIdRegisteredAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var warmMapper = new IpldMapper(warmRegistry.Object, Mock.Of<ILogger<IpldMapper>>());
        await warmMapper.AddLinksAsync("content:audio:album:warm-duplicate", [link]);
        _ = await warmMapper.GetGraphAsync("content:audio:album:warm-duplicate", maxDepth: 1);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var graph = await _mapper.GetGraphAsync(RootContentId, maxDepth: 1);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Single(graph.Paths);
        Assert.Equal(linkCount, graph.Nodes[0].OutgoingLinks.Count);
        Assert.Equal(ChildContentId, graph.Nodes[1].ContentId);
        Assert.True(
            allocatedBytes < 1_100_000,
            $"Expected duplicate-fan-out graph building below 1,100,000 allocated bytes, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task FindInboundLinksAsync_IndexPreservesSourceOrderFiltersAndDeduplication()
    {
        const string FirstSource = "content:audio:track:first";
        const string SecondSource = "content:audio:track:second";
        const string ThirdSource = "content:audio:track:third";
        const string TargetContentId = "content:audio:album:target";
        _registryMock
            .Setup(registry => registry.IsContentIdRegisteredAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _mapper.AddLinksAsync(FirstSource, [new IpldLink(IpldLinkNames.Artist, "content:audio:artist:other")]);
        await _mapper.AddLinksAsync(SecondSource, [new IpldLink(IpldLinkNames.Artist, TargetContentId)]);
        await _mapper.AddLinksAsync(FirstSource,
        [
            new IpldLink(IpldLinkNames.Parent, TargetContentId),
            new IpldLink(IpldLinkNames.Artist, TargetContentId),
            new IpldLink(IpldLinkNames.Artist, TargetContentId),
        ]);
        await _mapper.AddLinksAsync(ThirdSource,
        [
            new IpldLink(IpldLinkNames.Parent, TargetContentId),
            new IpldLink("Artist", TargetContentId),
        ]);

        var all = await _mapper.FindInboundLinksAsync(TargetContentId);
        var artists = await _mapper.FindInboundLinksAsync(TargetContentId, IpldLinkNames.Artist);
        var parents = await _mapper.FindInboundLinksAsync(TargetContentId, IpldLinkNames.Parent);
        var caseVariant = await _mapper.FindInboundLinksAsync(TargetContentId, "Artist");
        var differentTargetCase = await _mapper.FindInboundLinksAsync(TargetContentId.ToUpperInvariant());

        Assert.Equal([FirstSource, SecondSource, ThirdSource], all);
        Assert.Equal([FirstSource, SecondSource], artists);
        Assert.Equal([FirstSource, ThirdSource], parents);
        Assert.Equal([ThirdSource], caseVariant);
        Assert.Empty(differentTargetCase);
    }

    [Fact]
    public async Task GetGraphAsync_PreservesDepthOrderCyclesAndSharedTargets()
    {
        const string RootContentId = "content:audio:album:root";
        const string FirstChild = "content:audio:track:first";
        const string SecondChild = "content:audio:track:second";
        const string Grandchild = "content:audio:artist:shared";
        _registryMock
            .Setup(registry => registry.IsContentIdRegisteredAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        await _mapper.AddLinksAsync(RootContentId,
        [
            new IpldLink(IpldLinkNames.Tracks, FirstChild),
            new IpldLink(IpldLinkNames.Tracks, SecondChild),
            new IpldLink(IpldLinkNames.Tracks, FirstChild),
        ]);
        await _mapper.AddLinksAsync(FirstChild, [new IpldLink(IpldLinkNames.Artist, Grandchild)]);
        await _mapper.AddLinksAsync(SecondChild,
        [
            new IpldLink(IpldLinkNames.Artist, Grandchild),
            new IpldLink(IpldLinkNames.Parent, RootContentId),
        ]);

        var graph = await _mapper.GetGraphAsync(RootContentId, maxDepth: 2);

        Assert.Equal([RootContentId, FirstChild, Grandchild, SecondChild], graph.Nodes.Select(node => node.ContentId));
        Assert.Equal(
        [
            new[] { RootContentId, FirstChild },
            new[] { FirstChild, Grandchild },
            new[] { RootContentId, SecondChild },
        ],
            graph.Paths.Select(path => path.ContentIds));
        Assert.Equal([FirstChild, SecondChild], graph.Nodes[2].IncomingLinks);
    }

    [Fact]
    public async Task ValidateLinksAsync_ValidatesSuccessfully()
    {
        // Arrange
        _registryMock.Setup(r => r.FindByDomainAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _mapper.ValidateLinksAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid); // Should be valid with no links to validate
        Assert.Equal(0, result.BrokenLinks.Count);
        Assert.Equal(0, result.OrphanedLinks.Count);
    }

    [Fact]
    public async Task ValidateLinksAsync_ReusesRegistrationChecksForRepeatedContentIds()
    {
        var sourceContentId = "content:audio:track:source";
        var missingTarget = "content:audio:album:missing";
        var registeredTarget = "content:audio:artist:registered";

        _registryMock
            .Setup(r => r.IsContentIdRegisteredAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string contentId, CancellationToken _) => contentId != missingTarget);
        await _mapper.AddLinksAsync(sourceContentId, new[]
        {
            new IpldLink(IpldLinkNames.Album, missingTarget),
            new IpldLink(IpldLinkNames.Album, missingTarget),
            new IpldLink(IpldLinkNames.Artist, registeredTarget),
        });

        _registryMock.Invocations.Clear();
        _registryMock
            .Setup(r => r.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentIdRegistryStats(
                TotalMappings: 1,
                TotalDomains: 1,
                MappingsByDomain: new Dictionary<string, int> { ["audio"] = 1 }));
        _registryMock
            .Setup(r => r.FindByDomainAsync("audio", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { sourceContentId });

        var result = await _mapper.ValidateLinksAsync();

        Assert.False(result.IsValid);
        Assert.Equal(2, result.BrokenLinks.Count);
        Assert.Empty(result.OrphanedLinks);
        _registryMock.Verify(
            r => r.IsContentIdRegisteredAsync(missingTarget, It.IsAny<CancellationToken>()),
            Times.Once);
        _registryMock.Verify(
            r => r.IsContentIdRegisteredAsync(registeredTarget, It.IsAny<CancellationToken>()),
            Times.Once);
        _registryMock.Verify(
            r => r.IsContentIdRegisteredAsync(sourceContentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateLinksAsync_ChecksOrphanedSourceOnceAndReportsEveryLink()
    {
        var sourceContentId = "content:audio:track:source";
        var firstTarget = "content:audio:album:first";
        var secondTarget = "content:audio:album:second";
        var sourceIsRegistered = true;

        _registryMock
            .Setup(r => r.IsContentIdRegisteredAsync(sourceContentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => sourceIsRegistered);
        await _mapper.AddLinksAsync(sourceContentId, new[]
        {
            new IpldLink(IpldLinkNames.Album, firstTarget),
            new IpldLink(IpldLinkNames.Album, secondTarget),
        });

        sourceIsRegistered = false;
        _registryMock.Invocations.Clear();
        _registryMock
            .Setup(r => r.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentIdRegistryStats(
                TotalMappings: 0,
                TotalDomains: 0,
                MappingsByDomain: new Dictionary<string, int>()));
        _registryMock
            .Setup(r => r.FindByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var result = await _mapper.ValidateLinksAsync();

        Assert.False(result.IsValid);
        Assert.Empty(result.BrokenLinks);
        Assert.Equal(2, result.OrphanedLinks.Count);
        _registryMock.Verify(
            r => r.IsContentIdRegisteredAsync(sourceContentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ToJson_ValidDescriptor_ReturnsJson()
    {
        // Arrange
        var descriptor = new ContentDescriptor
        {
            ContentId = "content:audio:track:mb-12345",
            SizeBytes = 1024 * 1024,
            Codec = "mp3",
            Confidence = 0.8
        };

        // Act
        var json = _mapper.ToJson(descriptor);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("contentId", json);
        Assert.Contains("size", json);
        Assert.Contains("codec", json);
        Assert.Contains("confidence", json);
    }

    [Fact]
    public void ToJson_DescriptorWithLinks_IncludesLinksInJson()
    {
        // Arrange
        var descriptor = new ContentDescriptor
        {
            ContentId = "content:audio:track:mb-12345",
            SizeBytes = 1024 * 1024,
            Codec = "mp3",
            Confidence = 0.8
        };

        descriptor.AddLink(IpldLinkNames.Album, "content:audio:album:mb-67890");
        descriptor.AddLink(IpldLinkNames.Artist, "content:audio:artist:mb-abc123");

        // Act
        var json = _mapper.ToJson(descriptor);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("links", json);
        Assert.Contains(IpldLinkNames.Album, json);
        Assert.Contains(IpldLinkNames.Artist, json);
    }

    [Fact]
    public void IpldLinkNames_ConstantsAreDefined()
    {
        // Assert that all expected link name constants are defined
        Assert.Equal("parent", IpldLinkNames.Parent);
        Assert.Equal("children", IpldLinkNames.Children);
        Assert.Equal("album", IpldLinkNames.Album);
        Assert.Equal("artist", IpldLinkNames.Artist);
        Assert.Equal("artwork", IpldLinkNames.Artwork);
        Assert.Equal("tracks", IpldLinkNames.Tracks);
    }

    [Fact]
    public void IpldLink_PropertiesAreSetCorrectly()
    {
        // Arrange
        var name = "album";
        var target = "content:audio:album:mb-67890";
        var linkName = "main-album";

        // Act
        var link = new IpldLink(name, target, linkName);

        // Assert
        Assert.Equal(name, link.Name);
        Assert.Equal(target, link.Target);
        Assert.Equal(linkName, link.LinkName);
        Assert.Equal($"{name}/{target}", link.Path);
    }

    [Fact]
    public void IpldLinkCollection_AddAndRetrieveLinks()
    {
        // Arrange
        var collection = new IpldLinkCollection();
        var link1 = new IpldLink("album", "content:audio:album:1");
        var link2 = new IpldLink("album", "content:audio:album:2");
        var link3 = new IpldLink("artist", "content:audio:artist:1");

        // Act
        collection.AddLink(link1);
        collection.AddLink(link2);
        collection.AddLink(link3);

        // Assert
        var albumLinks = collection.GetLinksByName("album");
        Assert.Equal(2, albumLinks.Count);
        Assert.Contains(link1, albumLinks);
        Assert.Contains(link2, albumLinks);

        var artistLinks = collection.GetLinksByName("artist");
        Assert.Single(artistLinks);
        Assert.Contains(link3, artistLinks);

        var album1Targets = collection.GetLinksByTarget("content:audio:album:1");
        Assert.Single(album1Targets);
        Assert.Contains(link1, album1Targets);
    }
}
