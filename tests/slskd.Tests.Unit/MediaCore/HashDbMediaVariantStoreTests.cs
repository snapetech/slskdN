// <copyright file="HashDbMediaVariantStoreTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Audio;
using slskd.HashDb;
using slskd.MediaCore;
using slskd.VirtualSoulfind.Core;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public sealed class HashDbMediaVariantStoreTests
{
    [Fact]
    public async Task GetByVariantIdAsync_RecordingFallbackUsesOneBestVariantRead()
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(db => db.GetAudioVariantByFlacKeyAsync("recording-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AudioVariant?)null);
        hashDb.Setup(db => db.GetBestVariantByRecordingAsync("recording-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioVariant
            {
                FlacKey = "best-key",
                VariantId = "best-variant",
                MusicBrainzRecordingId = "recording-1",
                QualityScore = 0.9,
                SeenCount = 7,
            });
        var store = new HashDbMediaVariantStore(hashDb.Object, NullLogger<HashDbMediaVariantStore>.Instance);

        var result = await store.GetByVariantIdAsync("recording-1");

        Assert.NotNull(result);
        Assert.Equal("best-variant", result.VariantId);
        hashDb.Verify(db => db.GetBestVariantByRecordingAsync(
            "recording-1",
            It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(db => db.GetVariantsByRecordingAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByDomainAsync_MusicUsesOneBoundedVariantRead()
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(db => db.GetRecentVariantsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 100)
                .Select(index => new AudioVariant
                {
                    VariantId = $"variant-{index}",
                    FlacKey = $"key-{index}",
                    MusicBrainzRecordingId = $"recording-{index}",
                })
                .ToList());
        var store = new HashDbMediaVariantStore(hashDb.Object, NullLogger<HashDbMediaVariantStore>.Instance);

        var variants = await store.GetByDomainAsync(ContentDomain.Music);

        Assert.Equal(100, variants.Count);
        hashDb.Verify(db => db.GetRecentVariantsAsync(100, It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(db => db.GetRecordingIdsWithVariantsAsync(It.IsAny<CancellationToken>()), Times.Never);
        hashDb.Verify(db => db.GetVariantsByRecordingAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByDomainAsync_DuplicateHeavyMusicAvoidsDiscardedVariantAllocations()
    {
        var audioVariants = Enumerable.Range(0, 100_000)
            .Select(index => new AudioVariant
            {
                VariantId = $"variant-{index % 10}",
                FlacKey = $"key-{index}",
                MusicBrainzRecordingId = $"recording-{index}",
            })
            .ToList();
        var hashDb = new Mock<IHashDbService>();
        hashDb
            .Setup(db => db.GetRecentVariantsAsync(100_000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(audioVariants);
        var store = new HashDbMediaVariantStore(hashDb.Object, NullLogger<HashDbMediaVariantStore>.Instance);
        _ = await CreateStore([new AudioVariant { VariantId = "warm" }])
            .GetByDomainAsync(ContentDomain.Music, 1);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var variants = await store.GetByDomainAsync(ContentDomain.Music, 100_000);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(10, variants.Count);
        Assert.Equal(Enumerable.Range(0, 10).Select(index => $"variant-{index}"), variants.Select(variant => variant.VariantId));
        for (var index = 0; index < variants.Count; index++)
        {
            Assert.Same(audioVariants[index], variants[index].Audio);
        }

        Assert.True(
            allocatedBytes < 16 * 1024,
            $"Expected duplicate-heavy music projection below 16 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task GetByDomainAsync_MusicPreservesVariantIdFallbackAndFirstOccurrence()
    {
        var firstFallback = new AudioVariant { VariantId = null!, FlacKey = "fallback" };
        var firstEmpty = new AudioVariant { VariantId = string.Empty, FlacKey = "ignored" };
        var firstExplicit = new AudioVariant { VariantId = "explicit", FlacKey = "key" };
        var caseVariant = new AudioVariant { VariantId = "EXPLICIT" };
        var store = CreateStore(
        [
            firstFallback,
            new AudioVariant { VariantId = null!, FlacKey = "fallback" },
            firstEmpty,
            new AudioVariant(),
            firstExplicit,
            new AudioVariant { VariantId = "explicit" },
            caseVariant,
        ]);

        var variants = await store.GetByDomainAsync(ContentDomain.Music, 7);

        Assert.Equal(["fallback", string.Empty, "explicit", "EXPLICIT"], variants.Select(variant => variant.VariantId));
        Assert.Same(firstFallback, variants[0].Audio);
        Assert.Same(firstEmpty, variants[1].Audio);
        Assert.Same(firstExplicit, variants[2].Audio);
        Assert.Same(caseVariant, variants[3].Audio);
    }

    private static HashDbMediaVariantStore CreateStore(List<AudioVariant> variants)
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb
            .Setup(db => db.GetRecentVariantsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        return new HashDbMediaVariantStore(hashDb.Object, NullLogger<HashDbMediaVariantStore>.Instance);
    }
}
