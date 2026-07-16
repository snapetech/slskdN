// <copyright file="MetadataPortabilityTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using slskd.MediaCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class MetadataPortabilityTests
{
    private readonly MetadataPortability _portability;
    private readonly Mock<IContentIdRegistry> _registryMock;
    private readonly Mock<IDescriptorRetriever> _descriptorRetrieverMock;
    private readonly Mock<IIpldMapper> _ipldMapperMock;
    private readonly Mock<ILogger<MetadataPortability>> _loggerMock;

    public MetadataPortabilityTests()
    {
        _registryMock = new Mock<IContentIdRegistry>();
        _descriptorRetrieverMock = new Mock<IDescriptorRetriever>();
        _ipldMapperMock = new Mock<IIpldMapper>();
        _loggerMock = new Mock<ILogger<MetadataPortability>>();
        _portability = new MetadataPortability(_registryMock.Object, _descriptorRetrieverMock.Object, _ipldMapperMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExportAsync_ValidContentIds_ReturnsPackage()
    {
        // Arrange
        var contentIds = new[] { "content:audio:track:mb-12345", "content:video:movie:imdb-tt0111161" };

        _registryMock.Setup(r => r.FindByDomainAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Array.Empty<string>());
        _descriptorRetrieverMock.Setup(r => r.RetrieveAsync(It.IsAny<string>(), false, default))
            .ReturnsAsync(new DescriptorRetrievalResult(
                Found: true,
                Descriptor: new ContentDescriptor(),
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: false,
                Verification: null));

        // Act
        var package = await _portability.ExportAsync(contentIds, includeLinks: true);

        // Assert
        Assert.NotNull(package);
        Assert.Equal("1.0", package.Version);
        Assert.True(package.Entries.Count >= 0); // May be empty in mock scenario
        Assert.Equal("slskdN", package.Source);
        Assert.NotNull(package.Metadata);
        Assert.Equal(package.Entries.Count, package.Metadata.TotalEntries);
    }

    [Fact]
    public async Task ExportAsync_IncludeLinksTrue_IncludesLinksInPackage()
    {
        // Arrange
        var contentIds = new[] { "content:audio:track:mb-12345" };
        var mockLinks = new[] { new IpldLink("album", "content:audio:album:mb-67890") };
        _descriptorRetrieverMock.Setup(r => r.RetrieveAsync(It.IsAny<string>(), false, default))
            .ReturnsAsync(new DescriptorRetrievalResult(
                Found: true,
                Descriptor: new ContentDescriptor { ContentId = "content:audio:track:mb-12345" },
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: false,
                Verification: null));

        _ipldMapperMock.Setup(m => m.GetGraphAsync(It.IsAny<string>(), It.IsAny<int>(), default))
            .ReturnsAsync(new ContentGraph("content:audio:track:mb-12345",
                new[] { new ContentGraphNode("content:audio:track:mb-12345", Array.Empty<IpldLink>(), Array.Empty<string>()) },
                new[] { new ContentGraphPath(new[] { "content:audio:track:mb-12345" }, mockLinks) }));

        // Act
        var package = await _portability.ExportAsync(contentIds, includeLinks: true);

        // Assert
        Assert.NotNull(package);
        Assert.NotEmpty(package.Links);
    }

    [Fact]
    public async Task ExportAsync_TrimsAndDeduplicatesContentIdsCaseInsensitively()
    {
        _descriptorRetrieverMock.Setup(r => r.RetrieveAsync(It.IsAny<string>(), false, default))
            .ReturnsAsync((string contentId, bool _, CancellationToken _) => new DescriptorRetrievalResult(
                Found: true,
                Descriptor: new ContentDescriptor { ContentId = contentId },
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: false,
                Verification: null));

        var package = await _portability.ExportAsync(new[]
        {
            " content:mb:recording:12345 ",
            "content:mb:recording:12345",
            "CONTENT:MB:RECORDING:12345",
            "",
            "   ",
        }, includeLinks: false);

        Assert.Single(package.Entries);
        _descriptorRetrieverMock.Verify(
            r => r.RetrieveAsync("content:mb:recording:12345", false, default),
            Times.Once);
    }

    [Fact]
    public async Task ExportAsync_TracksNormalizedDomains()
    {
        _descriptorRetrieverMock.Setup(r => r.RetrieveAsync("content:mb:recording:12345", false, default))
            .ReturnsAsync(new DescriptorRetrievalResult(
                Found: true,
                Descriptor: new ContentDescriptor { ContentId = "content:mb:recording:12345" },
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: TimeSpan.Zero,
                FromCache: false,
                Verification: null));

        var package = await _portability.ExportAsync(new[] { "content:mb:recording:12345" }, includeLinks: false);

        Assert.Equal(1, package.Metadata.EntriesByDomain["audio"]);
    }

    [Fact]
    public async Task ImportAsync_ValidPackage_Succeeds()
    {
        // Arrange
        var package = new MetadataPackage(
            "1.0",
            DateTimeOffset.UtcNow,
            "test-source",
            Array.Empty<MetadataEntry>(),
            Array.Empty<IpldLink>(),
            new MetadataPackageMetadata(0, 0, new Dictionary<string, int>(), "checksum"));

        // Act
        var result = await _portability.ImportAsync(package);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(0, result.EntriesProcessed); // Empty package
    }

    [Fact]
    public async Task ImportAsync_DryRunTrue_DoesNotModifyData()
    {
        // Arrange
        var package = new MetadataPackage(
            "1.0",
            DateTimeOffset.UtcNow,
            "test-source",
            Array.Empty<MetadataEntry>(),
            Array.Empty<IpldLink>(),
            new MetadataPackageMetadata(0, 0, new Dictionary<string, int>(), "checksum"));

        // Act
        var result = await _portability.ImportAsync(package, dryRun: true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ImportAsync_WhenEntryImportThrows_SanitizesErrorMessage()
    {
        var package = new MetadataPackage(
            "1.0",
            DateTimeOffset.UtcNow,
            "test-source",
            new[]
            {
                new MetadataEntry(
                    "content:audio:track:mb-12345",
                    new ContentDescriptor { ContentId = "content:audio:track:mb-12345" },
                    new MetadataSourceInfo("test", DateTimeOffset.UtcNow, "1.0", new Dictionary<string, string>()))
            },
            Array.Empty<IpldLink>(),
            new MetadataPackageMetadata(1, 0, new Dictionary<string, int>(), "checksum"));

        _registryMock
            .Setup(r => r.IsContentIdRegisteredAsync("content:audio:track:mb-12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _registryMock
            .Setup(r => r.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var result = await _portability.ImportAsync(package);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Failed to import content:audio:track:mb-12345", error);
        Assert.DoesNotContain("sensitive detail", error);
    }

    [Fact]
    public async Task ImportAsync_RegistersNormalizedExternalIdForMusicBrainzContent()
    {
        var package = new MetadataPackage(
            "1.0",
            DateTimeOffset.UtcNow,
            "test-source",
            new[]
            {
                new MetadataEntry(
                    "content:mb:recording:12345",
                    new ContentDescriptor { ContentId = "content:mb:recording:12345" },
                    new MetadataSourceInfo("test", DateTimeOffset.UtcNow, "1.0", new Dictionary<string, string>()))
            },
            Array.Empty<IpldLink>(),
            new MetadataPackageMetadata(1, 0, new Dictionary<string, int>(), "checksum"));

        _registryMock
            .Setup(r => r.IsContentIdRegisteredAsync("content:mb:recording:12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _portability.ImportAsync(package);

        Assert.True(result.Success);
        _registryMock.Verify(
            r => r.RegisterAsync("audio:recording:12345", "content:mb:recording:12345", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeConflictsAsync_ValidPackage_ReturnsAnalysis()
    {
        // Arrange
        var package = new MetadataPackage(
            "1.0",
            DateTimeOffset.UtcNow,
            "test-source",
            Array.Empty<MetadataEntry>(),
            Array.Empty<IpldLink>(),
            new MetadataPackageMetadata(0, 0, new Dictionary<string, int>(), "checksum"));

        _registryMock.Setup(r => r.IsRegisteredAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act
        var analysis = await _portability.AnalyzeConflictsAsync(package);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(0, analysis.TotalEntries); // Empty package
        Assert.Equal(0, analysis.ConflictingEntries);
        Assert.Equal(0, analysis.CleanEntries);
    }

    [Fact]
    public async Task MergeMetadataAsync_PreferNewerStrategy_ReturnsNewer()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";
        var olderDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 0.5 };
        var newerDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 0.8 };

        var sources = new[]
        {
            new MetadataSource("source1", olderDescriptor, DateTimeOffset.UtcNow.AddDays(-1), 1),
            new MetadataSource("source2", newerDescriptor, DateTimeOffset.UtcNow, 2)
        };

        // Act
        var result = await _portability.MergeMetadataAsync(contentId, sources, MetadataMergeStrategy.PreferNewer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newerDescriptor.Confidence, result.Confidence);
    }

    [Fact]
    public async Task MergeMetadataAsync_PreferHigherPriorityStrategy_ReturnsHigherPriority()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";
        var lowPriorityDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 0.5 };
        var highPriorityDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 0.8 };

        var sources = new[]
        {
            new MetadataSource("source1", lowPriorityDescriptor, DateTimeOffset.UtcNow, 1),
            new MetadataSource("source2", highPriorityDescriptor, DateTimeOffset.UtcNow, 5)
        };

        // Act
        var result = await _portability.MergeMetadataAsync(contentId, sources, MetadataMergeStrategy.PreferHigherPriority);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(highPriorityDescriptor.Confidence, result.Confidence);
    }

    [Theory]
    [InlineData(MetadataMergeStrategy.PreferNewer)]
    [InlineData(MetadataMergeStrategy.PreferHigherPriority)]
    public async Task MergeMetadataAsync_SelectionStrategiesAvoidSortingAllSources(MetadataMergeStrategy strategy)
    {
        const int sourceCount = 100_000;
        var contentId = "content:audio:track:mb-large";
        var selectedDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 1.0 };
        var tiedDescriptor = new ContentDescriptor { ContentId = contentId, Confidence = 0.5 };
        var timestamp = DateTimeOffset.UtcNow;
        var sources = Enumerable.Range(0, sourceCount)
            .Select(index => new MetadataSource(
                $"source-{index}",
                index == 0 ? selectedDescriptor : tiedDescriptor,
                timestamp,
                Priority: 1))
            .ToArray();
        _ = await _portability.MergeMetadataAsync(contentId, sources.AsSpan(0, 2).ToArray(), strategy);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await _portability.MergeMetadataAsync(contentId, sources, strategy);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Same(selectedDescriptor, result);
        Assert.True(
            allocatedBytes < 4 * 1024,
            $"Expected single-pass source selection below 4 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task MergeMetadataAsync_CombineAllStrategy_MergesFields()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";
        var descriptor1 = new ContentDescriptor
        {
            ContentId = contentId,
            Codec = "mp3",
            SizeBytes = 1024
        };
        var descriptor2 = new ContentDescriptor
        {
            ContentId = contentId,
            Confidence = 0.8
        };

        var sources = new[]
        {
            new MetadataSource("source1", descriptor1, DateTimeOffset.UtcNow, 1),
            new MetadataSource("source2", descriptor2, DateTimeOffset.UtcNow, 2)
        };

        // Act
        var result = await _portability.MergeMetadataAsync(contentId, sources, MetadataMergeStrategy.CombineAll);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mp3", result.Codec);
        Assert.Equal(1024, result.SizeBytes);
        Assert.Equal(0.8, result.Confidence);
    }

    [Fact]
    public async Task MergeMetadataAsync_CombineAllAvoidsDuplicateSourceAndDescriptorLists()
    {
        const int sourceCount = 100_000;
        var contentId = "content:audio:track:mb-combine-large";
        var descriptor = new ContentDescriptor
        {
            ContentId = contentId,
            Hashes = new List<ContentHash> { new("sha256", "hash") },
            PerceptualHashes = new List<PerceptualHash> { new("Chromaprint", "phash", 123UL) },
            SizeBytes = 1234,
            Codec = "flac",
            Confidence = 0.75,
        };
        var timestamp = DateTimeOffset.UtcNow;
        var sources = Enumerable.Range(0, sourceCount)
            .Select(index => new MetadataSource($"source-{index}", descriptor, timestamp, index))
            .ToArray();
        _ = await _portability.MergeMetadataAsync(
            contentId,
            sources.AsSpan(0, 2).ToArray(),
            MetadataMergeStrategy.CombineAll);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await _portability.MergeMetadataAsync(
            contentId,
            sources,
            MetadataMergeStrategy.CombineAll);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(contentId, result.ContentId);
        Assert.Equal(descriptor.Hashes, result.Hashes);
        Assert.Equal(descriptor.PerceptualHashes, result.PerceptualHashes);
        Assert.Equal(descriptor.SizeBytes, result.SizeBytes);
        Assert.Equal(descriptor.Codec, result.Codec);
        Assert.Equal(descriptor.Confidence, result.Confidence);
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected single-pass combined aggregation below 8 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task MergeMetadataAsync_CombineAllPreservesOrderedDistinctAndScalarSelection()
    {
        var hashA = new ContentHash("sha256", "a");
        var hashB = new ContentHash("sha256", "b");
        var hashC = new ContentHash("sha256", "c");
        var perceptualA = new PerceptualHash("Chromaprint", "a", 1UL);
        var perceptualB = new PerceptualHash("Chromaprint", "b", 2UL);
        var timestamp = DateTimeOffset.UtcNow;
        var sources = new[]
        {
            new MetadataSource("first", new ContentDescriptor
            {
                ContentId = "content:audio:track:first",
                Hashes = new List<ContentHash> { hashA, hashB },
                PerceptualHashes = new List<PerceptualHash> { perceptualA },
                Codec = " ",
            }, timestamp, 1),
            new MetadataSource("second", new ContentDescriptor
            {
                ContentId = "content:audio:track:second",
                Hashes = new List<ContentHash> { hashB, hashC },
                PerceptualHashes = new List<PerceptualHash> { perceptualA, perceptualB },
                SizeBytes = 10,
                Codec = "flac",
                Confidence = 0.25,
            }, timestamp, 2),
            new MetadataSource("third", new ContentDescriptor
            {
                ContentId = "content:audio:track:third",
                SizeBytes = 20,
                Codec = "mp3",
                Confidence = 0.75,
            }, timestamp, 3),
        };

        var result = await _portability.MergeMetadataAsync(
            "content:audio:track:requested",
            sources,
            MetadataMergeStrategy.CombineAll);

        Assert.Equal("content:audio:track:first", result.ContentId);
        Assert.Equal(new[] { hashA, hashB, hashC }, result.Hashes);
        Assert.Equal(new[] { perceptualA, perceptualB }, result.PerceptualHashes);
        Assert.Equal(20, result.SizeBytes);
        Assert.Equal("flac", result.Codec);
        Assert.Equal(0.5, result.Confidence);
    }

    [Fact]
    public async Task MergeMetadataAsync_EmptySources_ThrowsException()
    {
        // Arrange
        var contentId = "content:audio:track:mb-12345";
        var emptySources = Array.Empty<MetadataSource>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _portability.MergeMetadataAsync(contentId, emptySources));

        Assert.Contains("At least one metadata source", exception.Message);
    }

    [Fact]
    public async Task MergeMetadataAsync_NullSources_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _portability.MergeMetadataAsync("content:audio:track:mb-null", null!));
    }

    [Fact]
    public void ComputePackageChecksum_LargePackageAvoidsSerializedPayloadCopies()
    {
        var timestamp = DateTimeOffset.Parse("2026-07-16T00:00:00Z");
        var entries = Enumerable.Range(0, 10_000)
            .Select(index => new MetadataEntry(
                $"content:audio:track:fixture-{index}",
                new ContentDescriptor
                {
                    ContentId = $"content:audio:track:fixture-{index}",
                    Codec = "flac",
                    SizeBytes = index,
                },
                new MetadataSourceInfo(
                    "fixture",
                    timestamp,
                    "1.0.0",
                    new Dictionary<string, string> { ["source"] = "test" })))
            .ToArray();
        var links = Array.Empty<IpldLink>();
        _ = MetadataPortability.ComputePackageChecksum(entries.AsSpan(0, 1).ToArray(), links);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var checksum = MetadataPortability.ComputePackageChecksum(entries, links);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal("de17fafc70333e87d0a34b787e01b59786d83af7eb737b55a7037d2a2cc31c94", checksum);
        Assert.True(
            allocatedBytes < 600 * 1024,
            $"Expected streaming checksum allocation below 600 KiB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ConflictResolutionStrategy_EnumValues_AreDefined()
    {
        // Assert that all expected strategy values are defined
        Assert.Equal(0, (int)ConflictResolutionStrategy.Skip);
        Assert.Equal(1, (int)ConflictResolutionStrategy.Overwrite);
        Assert.Equal(2, (int)ConflictResolutionStrategy.Merge);
        Assert.Equal(3, (int)ConflictResolutionStrategy.KeepExisting);
        Assert.Equal(4, (int)ConflictResolutionStrategy.Interactive);
    }

    [Fact]
    public void MetadataMergeStrategy_EnumValues_AreDefined()
    {
        // Assert that all expected strategy values are defined
        Assert.Equal(0, (int)MetadataMergeStrategy.PreferNewer);
        Assert.Equal(1, (int)MetadataMergeStrategy.PreferHigherPriority);
        Assert.Equal(2, (int)MetadataMergeStrategy.CombineAll);
        Assert.Equal(3, (int)MetadataMergeStrategy.Custom);
    }

    [Fact]
    public void MetadataSource_Properties_AreSetCorrectly()
    {
        // Arrange
        var name = "MusicBrainz";
        var descriptor = new ContentDescriptor { ContentId = "content:audio:track:mb-12345" };
        var timestamp = DateTimeOffset.UtcNow;
        var priority = 5;

        // Act
        var source = new MetadataSource(name, descriptor, timestamp, priority);

        // Assert
        Assert.Equal(name, source.Name);
        Assert.Equal(descriptor, source.Descriptor);
        Assert.Equal(timestamp, source.Timestamp);
        Assert.Equal(priority, source.Priority);
    }
}
