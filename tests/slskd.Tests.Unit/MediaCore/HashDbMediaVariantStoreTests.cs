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
}
