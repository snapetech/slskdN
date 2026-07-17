// <copyright file="DedupeServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Audio;

using Microsoft.Extensions.Logging;
using Moq;
using slskd.Audio;
using slskd.HashDb;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class DedupeServiceTests
{
    [Fact]
    public async Task GetDedupeAsync_PreservesGroupDuplicateSetAndVariantOrder()
    {
        var variants = new List<AudioVariant>
        {
            CreateVariant("z-1", "sketch-z", "stream-z1"),
            CreateVariant("a-1", "sketch-a", "stream-a"),
            CreateVariant("b-1", "sketch-b", "stream-c"),
            CreateVariant("z-2", "sketch-z", "stream-z2"),
            CreateVariant("a-2", "sketch-a", "stream-b"),
            CreateVariant("b-2", "sketch-b", "stream-c"),
            CreateVariant("z-3", "sketch-z", "stream-z3"),
            CreateVariant("a-3", "sketch-a", "stream-b"),
            CreateVariant("a-4", "sketch-a", "stream-a"),
            CreateVariant("b-3", "sketch-b", "stream-d"),
            CreateVariant("z-4", "sketch-z", "stream-z4"),
            CreateVariant("b-4", "sketch-b", "stream-d"),
            CreateVariant("z-5", "sketch-z", "stream-z5"),
        };
        var service = CreateService(variants);

        var result = await service.GetDedupeAsync("recording-1");

        Assert.Equal("recording-1", result.RecordingId);
        Assert.Collection(
            result.Groups,
            group =>
            {
                Assert.Equal("sketch-z", group.AudioSketchHash);
                Assert.Empty(group.DuplicateSets);
            },
            group =>
            {
                Assert.Equal("sketch-a", group.AudioSketchHash);
                Assert.Equal(new[] { "a-1", "a-2", "a-3", "a-4" }, group.Variants.Select(variant => variant.VariantId));
                Assert.Collection(
                    group.DuplicateSets,
                    set =>
                    {
                        Assert.Equal("stream-a", set.StreamHash);
                        Assert.Equal(new[] { "a-1", "a-4" }, set.Variants.Select(variant => variant.VariantId));
                    },
                    set =>
                    {
                        Assert.Equal("stream-b", set.StreamHash);
                        Assert.Equal(new[] { "a-2", "a-3" }, set.Variants.Select(variant => variant.VariantId));
                    });
            },
            group =>
            {
                Assert.Equal("sketch-b", group.AudioSketchHash);
                Assert.Equal(new[] { "stream-c", "stream-d" }, group.DuplicateSets.Select(set => set.StreamHash));
            });
    }

    [Fact]
    public async Task GetDedupeAsync_DuplicateHeavyInputHasBoundedAllocation()
    {
        const int variantCount = 10_000;
        const int groupCount = 100;
        const int streamCountPerGroup = 10;
        var variants = Enumerable.Range(0, variantCount)
            .Select(index => CreateVariant(
                $"variant-{index}",
                $"sketch-{index % groupCount:D3}",
                $"stream-{(index / groupCount) % streamCountPerGroup:D2}"))
            .ToList();
        var service = CreateService(variants);
        _ = await service.GetDedupeAsync("recording-wide");

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await service.GetDedupeAsync("recording-wide");
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(groupCount, result.Groups.Count);
        Assert.Equal(groupCount * streamCountPerGroup, result.Groups.Sum(group => group.DuplicateSets.Count));
        Assert.True(
            allocatedBytes < 2_000_000,
            $"Expected dedupe construction below 2 MB allocated, got {allocatedBytes:N0} bytes.");
    }

    private static DedupeService CreateService(List<AudioVariant> variants)
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetVariantsByRecordingAsync("recording-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        hashDb.Setup(service => service.GetVariantsByRecordingAsync("recording-wide", It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        return new DedupeService(hashDb.Object, Mock.Of<ILogger<DedupeService>>());
    }

    private static AudioVariant CreateVariant(string variantId, string sketchHash, string streamHash)
    {
        return new AudioVariant
        {
            VariantId = variantId,
            FlacKey = $"flac-{variantId}",
            Codec = "FLAC",
            Container = "flac",
            DurationMs = 180_000,
            BitrateKbps = 900,
            QualityScore = 0.9,
            FlacStreamInfoHash42 = streamHash,
            AudioSketchHash = sketchHash,
        };
    }
}
