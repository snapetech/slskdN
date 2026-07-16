// <copyright file="MultiSourceDownloadServiceCanonicalTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource;

using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Soulseek;
using slskd.Audio;
using slskd.HashDb;
using slskd.Transfers.MultiSource;
using Xunit;

public sealed class MultiSourceDownloadServiceCanonicalTests
{
    [Theory]
    [InlineData(0.90, 0.95, true)]
    [InlineData(0.90, 1.01, false)]
    [InlineData(0.80, 0.80, false)]
    public async Task ShouldSkipDownloadAsync_UsesOnlyBestLocalVariant(
        double localQuality,
        double proposedQuality,
        bool expected)
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetBestVariantByRecordingAsync(
                "recording-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioVariant
            {
                FlacKey = "best-key",
                VariantId = "best-variant",
                MusicBrainzRecordingId = "recording-1",
                QualityScore = localQuality,
            });
        var service = new MultiSourceDownloadService(
            NullLogger<MultiSourceDownloadService>.Instance,
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IContentVerificationService>(),
            hashDb.Object);

        var result = await service.ShouldSkipDownloadAsync(
            "recording-1",
            new AudioVariant { QualityScore = proposedQuality });

        Assert.Equal(expected, result);
        hashDb.Verify(database => database.GetBestVariantByRecordingAsync(
            "recording-1",
            It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(database => database.GetVariantsByRecordingAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldSkipDownloadAsync_MissingRecordingPerformsNoDatabaseRead()
    {
        var hashDb = new Mock<IHashDbService>();
        var service = new MultiSourceDownloadService(
            NullLogger<MultiSourceDownloadService>.Instance,
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IContentVerificationService>(),
            hashDb.Object);

        Assert.False(await service.ShouldSkipDownloadAsync(string.Empty, new AudioVariant()));
        hashDb.VerifyNoOtherCalls();
    }
}
