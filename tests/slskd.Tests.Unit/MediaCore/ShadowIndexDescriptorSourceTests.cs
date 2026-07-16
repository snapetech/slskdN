// <copyright file="ShadowIndexDescriptorSourceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.MediaCore;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class ShadowIndexDescriptorSourceTests
{
    [Fact]
    public async Task BuildForAsync_PreservesStableRankAndDistinctHashOrder()
    {
        var result = new ShadowIndexQueryResult
        {
            CanonicalVariants =
            [
                null!,
                new VariantHint { Codec = "nan", SizeBytes = long.MaxValue, QualityScore = double.NaN, HashPrefix = [0x0A] },
                new VariantHint { Codec = "two-old", SizeBytes = 10, QualityScore = 1, HashPrefix = [0x02] },
                new VariantHint { Codec = "one-old", SizeBytes = 20, QualityScore = 1, HashPrefix = [0x01] },
                new VariantHint { Codec = "three", SizeBytes = 20, QualityScore = 1, HashPrefix = [0x03] },
                new VariantHint { Codec = "two-best", SizeBytes = 1, QualityScore = 2, HashPrefix = [0x02] },
                new VariantHint { Codec = "one-best", SizeBytes = 30, QualityScore = 1, HashPrefix = [0x01] },
                new VariantHint { Codec = "four", SizeBytes = 20, QualityScore = 1, HashPrefix = [0x04] },
                new VariantHint { Codec = "overall", SizeBytes = 40, QualityScore = 5, HashPrefix = [] },
                new VariantHint { Codec = "overall-later", SizeBytes = 40, QualityScore = 5, HashPrefix = null! },
            ],
        };

        var descriptor = await CreateSource(result).BuildForAsync("content:mb:recording:order");

        Assert.NotNull(descriptor);
        Assert.Equal("overall", descriptor.Codec);
        Assert.Equal(40, descriptor.SizeBytes);
        Assert.Equal(["02", "01", "03", "04", "0a"], descriptor.Hashes.Select(hash => hash.Hex));
    }

    [Fact]
    public async Task BuildForAsync_NoUsableVariantsReturnsNull()
    {
        var result = new ShadowIndexQueryResult { CanonicalVariants = [null!] };

        var descriptor = await CreateSource(result).BuildForAsync("content:mb:recording:empty");

        Assert.Null(descriptor);
    }

    [Fact]
    public async Task BuildForAsync_DuplicateHeavyVariantsAvoidPerVariantSortAndHashAllocations()
    {
        var result = new ShadowIndexQueryResult
        {
            TotalPeerCount = 10,
            CanonicalVariants = Enumerable.Range(0, 100_000)
                .Select(index => new VariantHint
                {
                    Codec = $"codec-{index}",
                    SizeBytes = index + 1,
                    QualityScore = index % 10,
                    HashPrefix = [(byte)(index % 10)],
                })
                .ToList(),
        };
        var source = CreateSource(result);
        _ = await CreateSource(new ShadowIndexQueryResult
        {
            CanonicalVariants =
            [
                new VariantHint
                {
                    Codec = "warm",
                    SizeBytes = 1,
                    QualityScore = 1,
                    HashPrefix = [1],
                },
            ],
        }).BuildForAsync("content:mb:recording:warm");

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var descriptor = await source.BuildForAsync("content:mb:recording:large");
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.NotNull(descriptor);
        Assert.Equal(100_000, descriptor.SizeBytes);
        Assert.Equal("codec-99999", descriptor.Codec);
        Assert.Equal(0.88, descriptor.Confidence!.Value, precision: 10);
        Assert.Equal(
            Enumerable.Range(0, 10).Reverse().Select(value => value.ToString("x2")),
            descriptor.Hashes.Select(hash => hash.Hex));
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected duplicate-heavy descriptor building below 8 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    private static ShadowIndexDescriptorSource CreateSource(ShadowIndexQueryResult result)
    {
        var shadowIndex = new Mock<IShadowIndexQuery>();
        shadowIndex
            .Setup(index => index.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return new ShadowIndexDescriptorSource(
            NullLogger<ShadowIndexDescriptorSource>.Instance,
            shadowIndex.Object);
    }
}
