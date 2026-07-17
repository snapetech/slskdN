// <copyright file="CanonicalStatsServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Audio
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using slskd.Audio;
    using slskd.HashDb;
    using Xunit;

    [Collection(AllocationTestCollection.Name)]
    public class CanonicalStatsServiceTests
    {
        [Fact]
        public async Task AggregateStats_Should_SelectBestVariant_ByQualityThenSeen()
        {
            // Arrange
            var variants = new List<AudioVariant>
            {
                new() { VariantId = "v1", MusicBrainzRecordingId = "rec1", Codec = "FLAC", SampleRateHz = 44100, BitDepth = 16, Channels = 2, BitrateKbps = 900, QualityScore = 0.95, SeenCount = 5, TranscodeSuspect = false, FlacPcmMd5 = "md5a", DurationMs = 180000 },
                new() { VariantId = "v2", MusicBrainzRecordingId = "rec1", Codec = "FLAC", SampleRateHz = 44100, BitDepth = 16, Channels = 2, BitrateKbps = 800, QualityScore = 0.90, SeenCount = 20, TranscodeSuspect = false, FlacPcmMd5 = "md5b", DurationMs = 180500 },
                new() { VariantId = "v3", MusicBrainzRecordingId = "rec1", Codec = "MP3", SampleRateHz = 44100, BitDepth = null, Channels = 2, BitrateKbps = 320, QualityScore = 0.75, SeenCount = 30, TranscodeSuspect = false, Mp3StreamHash = "mp3hash", DurationMs = 181000 },
            };

            var mockDb = new Mock<IHashDbService>();
            mockDb.Setup(m => m.GetVariantsByRecordingAndProfileAsync("rec1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string profile, CancellationToken _) =>
                {
                    if (profile.Contains("FLAC")) return variants.GetRange(0, 2);
                    if (profile.Contains("MP3")) return new List<AudioVariant> { variants[2] };
                    return new List<AudioVariant>();
                });
            mockDb.Setup(m => m.UpsertCanonicalStatsAsync(It.IsAny<CanonicalStats>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockDb.Setup(m => m.GetVariantsByRecordingAsync("rec1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(variants);
            mockDb.Setup(m => m.GetCanonicalStatsForRecordingAsync("rec1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CanonicalStats>());
            mockDb.Setup(m => m.UpsertCanonicalStatsAsync(It.IsAny<IEnumerable<CanonicalStats>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var svc = new CanonicalStatsService(mockDb.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<CanonicalStatsService>>());

            // Act
            var candidates = await svc.GetCanonicalVariantCandidatesAsync("rec1");

            // Assert
            Assert.Equal(3, candidates.Count);
            Assert.Equal("v1", candidates[0].VariantId); // Lossless preferred, then quality
        }

        [Fact]
        public async Task AggregateStats_WithNoVariants_ReturnsNull()
        {
            var mockDb = new Mock<IHashDbService>();
            mockDb.Setup(m => m.GetVariantsByRecordingAndProfileAsync("rec-empty", "FLAC", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AudioVariant>());

            var svc = new CanonicalStatsService(mockDb.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<CanonicalStatsService>>());

            var stats = await svc.AggregateStatsAsync("rec-empty", "FLAC");

            Assert.Null(stats);
            mockDb.Verify(m => m.UpsertCanonicalStatsAsync(It.IsAny<CanonicalStats>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCanonicalVariantCandidatesAsync_WithOneHundredProfiles_UsesThreeDatabaseCalls()
        {
            const int profileCount = 100;
            var variants = Enumerable.Range(1, profileCount)
                .Select(index => new AudioVariant
                {
                    VariantId = $"variant-{index}",
                    MusicBrainzRecordingId = "recording-1",
                    Codec = "FLAC",
                    SampleRateHz = 44000 + index,
                    BitDepth = 16,
                    Channels = 2,
                    FlacPcmMd5 = $"stream-{index}",
                    QualityScore = index / 100.0,
                    SeenCount = index,
                })
                .ToList();
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(service => service.GetVariantsByRecordingAsync("recording-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(variants);
            hashDb.Setup(service => service.GetCanonicalStatsForRecordingAsync("recording-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CanonicalStats>());
            hashDb.Setup(service => service.UpsertCanonicalStatsAsync(
                    It.IsAny<IEnumerable<CanonicalStats>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var service = new CanonicalStatsService(
                hashDb.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CanonicalStatsService>>());

            var candidates = await service.GetCanonicalVariantCandidatesAsync("recording-1");

            Assert.Equal(profileCount, candidates.Count);
            hashDb.Verify(service => service.GetVariantsByRecordingAsync(
                "recording-1",
                It.IsAny<CancellationToken>()), Times.Once);
            hashDb.Verify(service => service.GetCanonicalStatsForRecordingAsync(
                "recording-1",
                It.IsAny<CancellationToken>()), Times.Once);
            hashDb.Verify(service => service.UpsertCanonicalStatsAsync(
                It.Is<IEnumerable<CanonicalStats>>(stats => stats.Count() == profileCount),
                It.IsAny<CancellationToken>()), Times.Once);
            hashDb.Verify(service => service.GetCanonicalStatsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            hashDb.Verify(service => service.GetVariantsByRecordingAndProfileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            hashDb.Verify(service => service.UpsertCanonicalStatsAsync(
                It.IsAny<CanonicalStats>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetCanonicalVariantCandidatesAsync_WideInputHasBoundedAllocation()
        {
            const int variantCount = 10_000;
            const int profileCount = 100;
            var variants = Enumerable.Range(0, variantCount)
                .Select(index => new AudioVariant
                {
                    VariantId = $"variant-{index}",
                    MusicBrainzRecordingId = "recording-wide",
                    Codec = "FLAC",
                    SampleRateHz = 44_100 + (index % profileCount),
                    BitDepth = 16,
                    Channels = 2,
                    FlacPcmMd5 = $"stream-{index}",
                    AudioSketchHash = "shared-sketch",
                    QualityScore = index / (double)variantCount,
                    SeenCount = index + 1,
                })
                .ToList();
            var stats = variants
                .Take(profileCount)
                .Select((variant, index) => new CanonicalStats
                {
                    CodecProfileKey = CodecProfile.FromVariant(variant).ToKey(),
                    CanonicalityScore = index / (double)profileCount,
                })
                .ToList();
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(service => service.GetVariantsByRecordingAsync(
                    "recording-wide",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(variants);
            hashDb.Setup(service => service.GetCanonicalStatsForRecordingAsync(
                    "recording-wide",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(stats);
            var service = new CanonicalStatsService(
                hashDb.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CanonicalStatsService>>());
            _ = await service.GetCanonicalVariantCandidatesAsync("recording-wide");

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var candidates = await service.GetCanonicalVariantCandidatesAsync("recording-wide");
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Equal(variantCount, candidates.Count);
            Assert.Equal($"variant-{variantCount - 1}", candidates[0].VariantId);
            Assert.True(
                allocatedBytes < 3_600_000,
                $"Expected canonical candidate selection below 3.6 MB allocated, got {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public async Task RecomputeAllStatsAsync_WithOneThousandRecordings_UsesPagedBatchCalls()
        {
            const int recordingCount = 1000;
            const int profilesPerRecording = 3;
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
                    .SelectMany(recordingId => new[]
                    {
                        CreateVariant(recordingId, "FLAC", 44100, 16),
                        CreateVariant(recordingId, "FLAC", 48000, 24),
                        CreateVariant(recordingId, "MP3", 44100, null),
                    })
                    .ToList());
            var persistedStats = new List<CanonicalStats>();
            hashDb.Setup(service => service.UpsertCanonicalStatsAsync(
                    It.IsAny<IEnumerable<CanonicalStats>>(),
                    It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<CanonicalStats> stats, CancellationToken _) => persistedStats.AddRange(stats))
                .Returns(Task.CompletedTask);
            var service = new CanonicalStatsService(
                hashDb.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CanonicalStatsService>>());

            await service.RecomputeAllStatsAsync();

            Assert.Equal(recordingCount * profilesPerRecording, persistedStats.Count);
            Assert.Equal(recordingCount * profilesPerRecording, persistedStats.Select(stats => stats.Id).Distinct().Count());
            hashDb.Verify(service => service.GetRecordingIdsWithVariantsPageAsync(
                It.IsAny<string?>(),
                500,
                It.IsAny<CancellationToken>()), Times.Exactly(3));
            hashDb.Verify(service => service.GetVariantsByRecordingsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Count() == 500),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            hashDb.Verify(service => service.UpsertCanonicalStatsAsync(
                It.Is<IEnumerable<CanonicalStats>>(stats => stats.Count() == 1500),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            hashDb.Verify(service => service.GetRecordingIdsWithVariantsAsync(
                It.IsAny<CancellationToken>()), Times.Never);
            hashDb.Verify(service => service.GetCodecProfilesForRecordingAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            hashDb.Verify(service => service.GetVariantsByRecordingAndProfileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            hashDb.Verify(service => service.UpsertCanonicalStatsAsync(
                It.IsAny<CanonicalStats>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void DeduplicateStreams_PreservesGroupOrderAndWinnerPrecedence()
        {
            var firstA = CreateDedupVariant("a-first", "stream-a", quality: 0.80, seenCount: 5);
            var firstB = CreateDedupVariant("b-first", "stream-b", quality: 0.85, seenCount: 3);
            var betterA = CreateDedupVariant("a-better", "stream-a", quality: 0.90, seenCount: 1);
            var moreSeenB = CreateDedupVariant("b-more-seen", "stream-b", quality: 0.85, seenCount: 8);
            var tiedA = CreateDedupVariant("a-tied", "stream-a", quality: 0.90, seenCount: 1);

            var result = CanonicalStatsService.DeduplicateStreams(
                new List<AudioVariant> { firstA, firstB, betterA, moreSeenB, tiedA });

            Assert.Collection(
                result,
                variant => Assert.Same(betterA, variant),
                variant => Assert.Same(moreSeenB, variant));
        }

        [Fact]
        public void DeduplicateStreams_CrossCodecFlagPreservesCodecPartitioning()
        {
            var flac = CreateDedupVariant("flac", "shared-stream", quality: 0.80, seenCount: 5);
            var mp3 = CreateDedupVariant("mp3", "shared-stream", quality: 0.90, seenCount: 2);
            mp3.Codec = "MP3";
            mp3.FlacPcmMd5 = null;
            mp3.Mp3StreamHash = "shared-stream";

            Assert.Equal(2, CanonicalStatsService.DeduplicateStreams(new List<AudioVariant> { flac, mp3 }).Count);
            Assert.Same(
                mp3,
                Assert.Single(CanonicalStatsService.DeduplicateStreams(
                    new List<AudioVariant> { flac, mp3 },
                    crossCodec: true)));
        }

        [Fact]
        public void DeduplicateStreams_DuplicateHeavyInputHasBoundedAllocation()
        {
            const int variantCount = 10_000;
            const int distinctStreamCount = 100;
            var variants = Enumerable.Range(0, variantCount)
                .Select(index => CreateDedupVariant(
                    $"variant-{index}",
                    $"stream-{index % distinctStreamCount}",
                    quality: index / (double)variantCount,
                    seenCount: index))
                .ToList();
            _ = CanonicalStatsService.DeduplicateStreams(variants.Take(10).ToList());

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var result = CanonicalStatsService.DeduplicateStreams(variants);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Equal(distinctStreamCount, result.Count);
            Assert.All(result, variant => Assert.True(variant.VariantId!.StartsWith("variant-99", StringComparison.Ordinal)));
            Assert.True(
                allocatedBytes < 1_200_000,
                $"Expected canonical stream deduplication below 1.2 MB allocated, got {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public void BuildCanonicalStats_PreservesDedupedAggregatesDistributionsAndBestVariant()
        {
            var duplicateA = CreateDedupVariant("a-old", "stream-a", quality: 0.80, seenCount: 20);
            duplicateA.BitrateKbps = 900;
            duplicateA.SampleRateHz = 44_100;
            var bestA = CreateDedupVariant("a-best", "stream-a", quality: 0.90, seenCount: 5);
            bestA.BitrateKbps = 900;
            bestA.SampleRateHz = 44_100;
            var bestB = CreateDedupVariant("b-best", "stream-b", quality: 0.90, seenCount: 10);
            bestB.BitrateKbps = 900;
            bestB.SampleRateHz = 44_100;
            var mp3 = CreateDedupVariant("mp3", "stream-c", quality: 0.70, seenCount: -2);
            mp3.Codec = "MP3";
            mp3.FlacPcmMd5 = null;
            mp3.Mp3StreamHash = "stream-c";
            mp3.BitrateKbps = 319;
            mp3.SampleRateHz = 48_000;
            mp3.TranscodeSuspect = true;

            var stats = CanonicalStatsService.BuildCanonicalStats(
                "recording",
                "profile",
                new List<AudioVariant> { duplicateA, bestA, bestB, mp3 });

            Assert.Equal(3, stats.VariantCount);
            Assert.Equal(16, stats.TotalSeenCount);
            Assert.Equal(2.5 / 3.0, stats.AvgQualityScore, precision: 10);
            Assert.Equal(0.90, stats.MaxQualityScore);
            Assert.Equal(100.0 / 3.0, stats.PercentTranscodeSuspect, precision: 10);
            Assert.Equal(new Dictionary<string, int> { ["FLAC"] = 2, ["MP3"] = 1 }, stats.CodecDistribution);
            Assert.Equal(new Dictionary<int, int> { [896] = 2, [320] = 1 }, stats.BitrateDistribution);
            Assert.Equal(new Dictionary<int, int> { [44_100] = 2, [48_000] = 1 }, stats.SampleRateDistribution);
            Assert.Equal("b-best", stats.BestVariantId);
        }

        [Fact]
        public void BuildCanonicalStats_WideProfileHasBoundedAllocation()
        {
            const int variantCount = 10_000;
            var variants = Enumerable.Range(0, variantCount)
                .Select(index => CreateDedupVariant(
                    $"variant-{index}",
                    $"stream-{index}",
                    quality: index / (double)variantCount,
                    seenCount: index + 1))
                .ToList();
            _ = CanonicalStatsService.BuildCanonicalStats("warm", "profile", variants.Take(10).ToList());

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stats = CanonicalStatsService.BuildCanonicalStats("recording", "profile", variants);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Equal(variantCount, stats.VariantCount);
            Assert.Equal($"variant-{variantCount - 1}", stats.BestVariantId);
            Assert.True(
                allocatedBytes < 2_600_000,
                $"Expected canonical-stat aggregation below 2.6 MB allocated, got {allocatedBytes:N0} bytes.");
        }

        private static AudioVariant CreateDedupVariant(
            string variantId,
            string streamHash,
            double quality,
            int seenCount)
        {
            return new AudioVariant
            {
                VariantId = variantId,
                Codec = "FLAC",
                FlacPcmMd5 = streamHash,
                AudioSketchHash = "shared-sketch",
                DurationMs = 180_000,
                QualityScore = quality,
                SeenCount = seenCount,
            };
        }

        private static AudioVariant CreateVariant(
            string recordingId,
            string codec,
            int sampleRate,
            int? bitDepth)
        {
            var profile = $"{codec}-{sampleRate}-{bitDepth}";
            return new AudioVariant
            {
                VariantId = $"{recordingId}-{profile}",
                MusicBrainzRecordingId = recordingId,
                Codec = codec,
                SampleRateHz = sampleRate,
                BitDepth = bitDepth,
                Channels = 2,
                FlacPcmMd5 = $"{recordingId}-{profile}",
                Mp3StreamHash = $"{recordingId}-{profile}",
                QualityScore = 0.8,
                SeenCount = 1,
            };
        }
    }
}
