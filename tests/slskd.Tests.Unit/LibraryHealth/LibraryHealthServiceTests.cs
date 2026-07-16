// <copyright file="LibraryHealthServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.LibraryHealth
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using slskd.Audio;
    using slskd.HashDb;
    using slskd.Integrations.MetadataFacade;
    using slskd.Integrations.MusicBrainz;
    using slskd.LibraryHealth;
    using slskd.LibraryHealth.Remediation;
    using slskd.Tests.Unit;
    using Xunit;

    public class LibraryHealthServiceTests
    {
        [Fact]
        public async Task GetScanStatusAsync_WhenScanMissing_ReturnsNull()
        {
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(m => m.GetLibraryHealthScanAsync("missing-scan", It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(LibraryHealthScan));

            var service = new LibraryHealthService(
                hashDb.Object,
                Mock.Of<ILibraryHealthRemediationService>(),
                Mock.Of<IMetadataFacade>(),
                Mock.Of<ICanonicalStatsService>(),
                Mock.Of<IMusicBrainzClient>(),
                Mock.Of<IOptionsMonitor<slskd.Options>>(),
                NullLogger<LibraryHealthService>.Instance);

            var scan = await service.GetScanStatusAsync("missing-scan");

            Assert.Null(scan);
        }

        [Fact]
        public async Task StartScanAsync_WhenBackgroundScanFails_ReturnsSanitizedErrorMessage()
        {
            var shareRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(shareRoot);

            try
            {
                var persistedScans = new ConcurrentDictionary<string, LibraryHealthScan>();
                var hashDb = new Mock<IHashDbService>();
                hashDb
                    .Setup(m => m.UpsertLibraryHealthScanAsync(It.IsAny<LibraryHealthScan>(), It.IsAny<CancellationToken>()))
                    .Returns<LibraryHealthScan, CancellationToken>((scan, _) =>
                    {
                        persistedScans[scan.ScanId] = Clone(scan);
                        return Task.CompletedTask;
                    });
                hashDb
                    .Setup(m => m.GetLibraryHealthScanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns<string, CancellationToken>((scanId, _) =>
                    {
                        persistedScans.TryGetValue(scanId, out var scan);
                        return Task.FromResult(scan);
                    });

                var options = new slskd.Options
                {
                    Shares = new slskd.Options.SharesOptions
                    {
                        Directories = new[] { shareRoot },
                    },
                };

                var service = new LibraryHealthService(
                    hashDb.Object,
                    Mock.Of<ILibraryHealthRemediationService>(),
                    Mock.Of<IMetadataFacade>(),
                    Mock.Of<ICanonicalStatsService>(),
                    Mock.Of<IMusicBrainzClient>(),
                    new TestOptionsMonitor<slskd.Options>(options),
                    NullLogger<LibraryHealthService>.Instance);

                var scanId = await service.StartScanAsync(
                    new LibraryHealthScanRequest
                    {
                        LibraryPath = Path.Combine(shareRoot, "missing"),
                    },
                    CancellationToken.None);

                LibraryHealthScan? status = null;
                for (var attempt = 0; attempt < 50; attempt++)
                {
                    status = await service.GetScanStatusAsync(scanId, CancellationToken.None);
                    if (status?.Status == ScanStatus.Failed)
                    {
                        break;
                    }

                    await Task.Delay(20);
                }

                Assert.NotNull(status);
                Assert.Equal(ScanStatus.Failed, status!.Status);
                Assert.Equal("Library health scan failed", status.ErrorMessage);
                Assert.DoesNotContain("missing", status.ErrorMessage, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (Directory.Exists(shareRoot))
                {
                    Directory.Delete(shareRoot, true);
                }
            }
        }

        [Fact]
        public void CreateLibraryEnumerationOptions_WhenRecursive_SkipsReparsePoints()
        {
            var options = LibraryHealthService.CreateLibraryEnumerationOptions(includeSubdirectories: true);

            Assert.True(options.RecurseSubdirectories);
            Assert.True(options.AttributesToSkip.HasFlag(FileAttributes.ReparsePoint));
            Assert.True(options.AttributesToSkip.HasFlag(FileAttributes.System));
        }

        [Fact]
        public async Task StartScanAsync_WhileScanRunning_RejectsOverlap()
        {
            var shareRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(shareRoot);
            var file = Path.Combine(shareRoot, "track.flac");
            await File.WriteAllBytesAsync(file, new byte[] { 1 });
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                var hashDb = new Mock<IHashDbService>();
                hashDb.Setup(service => service.UpsertLibraryHealthScanAsync(It.IsAny<LibraryHealthScan>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                var metadata = new Mock<IMetadataFacade>();
                metadata.Setup(service => service.GetByFileAsync(file, It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                    {
                        entered.TrySetResult();
                        await release.Task;
                        return null;
                    });
                var options = new slskd.Options
                {
                    Shares = new slskd.Options.SharesOptions { Directories = new[] { shareRoot } },
                };
                var service = new LibraryHealthService(
                    hashDb.Object,
                    Mock.Of<ILibraryHealthRemediationService>(),
                    metadata.Object,
                    Mock.Of<ICanonicalStatsService>(),
                    Mock.Of<IMusicBrainzClient>(),
                    new TestOptionsMonitor<slskd.Options>(options),
                    NullLogger<LibraryHealthService>.Instance);

                var firstScanId = await service.StartScanAsync(
                    new LibraryHealthScanRequest { LibraryPath = shareRoot, MaxConcurrentFiles = 100 },
                    CancellationToken.None);
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

                var exception = await Assert.ThrowsAsync<LibraryHealthScanAlreadyRunningException>(() =>
                    service.StartScanAsync(new LibraryHealthScanRequest { LibraryPath = shareRoot }, CancellationToken.None));

                Assert.Equal(firstScanId, exception.ScanId);
            }
            finally
            {
                release.TrySetResult();
                Directory.Delete(shareRoot, recursive: true);
            }
        }

        [Fact]
        public async Task StartScanAsync_CoalescesReleaseChecksAndBatchesRecordingPresence()
        {
            var shareRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(shareRoot);
            var recordingIds = Enumerable.Range(1, 10)
                .Select(index => $"recording-{index}")
                .ToArray();
            foreach (var recordingId in recordingIds)
            {
                WriteWaveFile(Path.Combine(shareRoot, $"{recordingId}.wav"));
            }

            try
            {
                var scanCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var hashDb = new Mock<IHashDbService>();
                hashDb
                    .Setup(service => service.UpsertLibraryHealthScanAsync(
                        It.IsAny<LibraryHealthScan>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<LibraryHealthScan, CancellationToken>((scan, _) =>
                    {
                        if (scan.Status == ScanStatus.Completed)
                        {
                            scanCompleted.TrySetResult();
                        }
                    })
                    .Returns(Task.CompletedTask);
                hashDb
                    .Setup(service => service.GetAlbumTargetAsync("release-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new global::slskd.HashDb.Models.AlbumTargetEntry
                    {
                        Artist = "Fixture Artist",
                        ReleaseId = "release-1",
                        Title = "Fixture Album",
                    });
                hashDb
                    .Setup(service => service.GetAlbumTracksAsync("release-1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(recordingIds.Select((recordingId, index) =>
                        new global::slskd.HashDb.Models.AlbumTargetTrackEntry
                        {
                            Position = index + 1,
                            RecordingId = recordingId,
                            ReleaseId = "release-1",
                            Title = $"Track {index + 1}",
                        }).ToArray());
                hashDb
                    .Setup(service => service.GetRecordingIdsWithHashesAsync(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns<IEnumerable<string>, CancellationToken>((ids, _) =>
                        Task.FromResult(new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase)));
                var metadata = new Mock<IMetadataFacade>();
                metadata
                    .Setup(service => service.GetByFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns<string, CancellationToken>((path, _) =>
                        Task.FromResult<MetadataResult?>(new MetadataResult(
                            Artist: "Fixture Artist",
                            Title: Path.GetFileNameWithoutExtension(path),
                            Album: "Fixture Album",
                            MusicBrainzRecordingId: Path.GetFileNameWithoutExtension(path),
                            MusicBrainzReleaseId: "release-1",
                            MusicBrainzArtistId: null,
                            Isrc: null,
                            Year: null,
                            Genre: null,
                            Source: MetadataResult.SourceFileTags)));
                var canonicalStats = new Mock<ICanonicalStatsService>();
                canonicalStats
                    .Setup(service => service.GetCanonicalVariantCandidatesAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<AudioVariant>());
                var options = new slskd.Options
                {
                    Shares = new slskd.Options.SharesOptions { Directories = new[] { shareRoot } },
                };
                var service = new LibraryHealthService(
                    hashDb.Object,
                    Mock.Of<ILibraryHealthRemediationService>(),
                    metadata.Object,
                    canonicalStats.Object,
                    Mock.Of<IMusicBrainzClient>(),
                    new TestOptionsMonitor<slskd.Options>(options),
                    NullLogger<LibraryHealthService>.Instance);

                await service.StartScanAsync(new LibraryHealthScanRequest
                {
                    FileExtensions = new List<string> { ".wav" },
                    LibraryPath = shareRoot,
                    MaxConcurrentFiles = 8,
                });
                await scanCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

                hashDb.Verify(
                    value => value.GetAlbumTargetAsync("release-1", It.IsAny<CancellationToken>()),
                    Times.Once);
                hashDb.Verify(
                    value => value.GetAlbumTracksAsync("release-1", It.IsAny<CancellationToken>()),
                    Times.Once);
                hashDb.Verify(
                    value => value.GetRecordingIdsWithHashesAsync(
                        It.Is<IEnumerable<string>>(ids => ids.Count() == 10),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
                hashDb.Verify(
                    value => value.LookupHashesByRecordingIdAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }
            finally
            {
                Directory.Delete(shareRoot, recursive: true);
            }
        }

        private static LibraryHealthScan Clone(LibraryHealthScan scan)
        {
            return new LibraryHealthScan
            {
                ScanId = scan.ScanId,
                LibraryPath = scan.LibraryPath,
                StartedAt = scan.StartedAt,
                CompletedAt = scan.CompletedAt,
                Status = scan.Status,
                FilesScanned = scan.FilesScanned,
                IssuesDetected = scan.IssuesDetected,
                ErrorMessage = scan.ErrorMessage,
            };
        }

        private static void WriteWaveFile(string path)
        {
            const int sampleRate = 8_000;
            const short channels = 1;
            const short bitsPerSample = 16;
            var audio = new byte[32];
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + audio.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(audio.Length);
            writer.Write(audio);
        }
    }
}
