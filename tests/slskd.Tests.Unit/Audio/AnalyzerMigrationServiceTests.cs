// <copyright file="AnalyzerMigrationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Audio;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Audio;
using slskd.HashDb;
using Xunit;

public sealed class AnalyzerMigrationServiceTests
{
    [Fact]
    public async Task MigrateAsync_WithOneThousandRecordings_UsesPagedBatchCalls()
    {
        const int recordingCount = 1000;
        const int variantsPerRecording = 3;
        var recordingIds = Enumerable.Range(0, recordingCount)
            .Select(index => $"recording-{index:D4}")
            .ToList();
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(
                null,
                500,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordingIds.Take(500).ToList());
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(
                "recording-0499",
                500,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordingIds.Skip(500).ToList());
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(
                "recording-0999",
                500,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        hashDb.Setup(service => service.GetVariantsByRecordingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> ids, CancellationToken _) => ids
                .SelectMany(recordingId => Enumerable.Range(1, variantsPerRecording)
                    .Select(index => new AudioVariant
                    {
                        FlacKey = $"{recordingId}-variant-{index}",
                        VariantId = $"{recordingId}-variant-{index}",
                        MusicBrainzRecordingId = recordingId,
                        Codec = "FLAC",
                        SampleRateHz = 44100,
                        BitDepth = 16,
                        Channels = 2,
                        BitrateKbps = 900,
                        AnalyzerVersion = "old",
                    }))
                .ToList());
        hashDb.Setup(service => service.UpdateVariantAnalysisAsync(
                It.IsAny<IEnumerable<AudioVariant>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new AnalyzerMigrationService(
            hashDb.Object,
            NullLogger<AnalyzerMigrationService>.Instance);

        var updated = await service.MigrateAsync("audioqa-2");

        Assert.Equal(recordingCount * variantsPerRecording, updated);
        hashDb.Verify(service => service.GetRecordingIdsWithVariantsPageAsync(
            It.IsAny<string?>(),
            500,
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        hashDb.Verify(service => service.GetVariantsByRecordingsAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 500),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        hashDb.Verify(service => service.UpdateVariantAnalysisAsync(
            It.Is<IEnumerable<AudioVariant>>(variants => variants.Count() == 1500 &&
                variants.All(variant => variant.AnalyzerVersion == "audioqa-2")),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        hashDb.Verify(service => service.GetRecordingIdsWithVariantsAsync(
            It.IsAny<CancellationToken>()), Times.Never);
        hashDb.Verify(service => service.GetVariantsByRecordingAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        hashDb.Verify(service => service.UpdateVariantMetadataAsync(
            It.IsAny<string>(),
            It.IsAny<AudioVariant>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
