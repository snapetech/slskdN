// <copyright file="MusicContentDomainProviderTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Core.Music
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using slskd.Audio;
    using slskd.Common.Moderation;
    using slskd.HashDb;
    using slskd.HashDb.Models;
    using slskd.VirtualSoulfind.Core.Music;
    using Xunit;

    /// <summary>
    ///     Tests for T-VC02: Music Domain Provider implementation.
    /// </summary>
    public class MusicContentDomainProviderTests
    {
        private readonly Mock<ILogger<MusicContentDomainProvider>> _loggerMock;
        private readonly Mock<IHashDbService> _hashDbMock;

        public MusicContentDomainProviderTests()
        {
            _loggerMock = new Mock<ILogger<MusicContentDomainProvider>>();
            _hashDbMock = new Mock<IHashDbService>();
        }

        [Fact]
        public async Task TryGetWorkByReleaseIdAsync_WithValidReleaseId_ReturnsMusicWork()
        {
            // Arrange
            var releaseId = "12345678-1234-1234-1234-123456789abc";
            var albumEntry = new AlbumTargetEntry
            {
                ReleaseId = releaseId,
                Title = "Test Album",
                Artist = "Test Artist"
            };

            _hashDbMock.Setup(h => h.GetAlbumTargetAsync(releaseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(albumEntry);

            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetWorkByReleaseIdAsync(releaseId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Album", result.Title);
            Assert.Equal("Test Artist", result.Creator);
        }

        [Fact]
        public async Task TryGetWorkByReleaseIdAsync_WithInvalidReleaseId_ReturnsNull()
        {
            // Arrange
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetWorkByReleaseIdAsync(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetWorkByReleaseIdAsync_WhenAlbumNotFound_ReturnsNull()
        {
            // Arrange
            var releaseId = "12345678-1234-1234-1234-123456789abc";

            _hashDbMock.Setup(h => h.GetAlbumTargetAsync(releaseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AlbumTargetEntry?)null);

            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetWorkByReleaseIdAsync(releaseId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetWorkByTitleArtistAsync_ReturnsExactAlbumMatch()
        {
            // Arrange
            _hashDbMock.Setup(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new AlbumTargetEntry
                    {
                        ReleaseId = "12345678-1234-1234-1234-123456789abc",
                        Title = "Test Album",
                        Artist = "Test Artist",
                        ReleaseDate = "2020-01-01"
                    }
                });
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetWorkByTitleArtistAsync("Test Album", "Test Artist", 2020);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Album", result.Title);
            Assert.Equal("Test Artist", result.Creator);
        }

        [Fact]
        public async Task TryGetItemByRecordingIdAsync_ReturnsTrack()
        {
            // Arrange
            var recordingId = "12345678-1234-1234-1234-123456789abc";
            var releaseId = "22345678-1234-1234-1234-123456789abc";
            _hashDbMock.Setup(h => h.LookupHashesByRecordingIdAsync(recordingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new HashDbEntry { MusicBrainzId = recordingId } });
            _hashDbMock.Setup(h => h.GetAlbumTrackByRecordingIdAsync(recordingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AlbumTargetTrackEntry
                {
                    ReleaseId = releaseId,
                    RecordingId = recordingId,
                    Title = "Track",
                    Artist = "Artist",
                    Position = 1,
                });
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetItemByRecordingIdAsync(recordingId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Track", result.Title);
            Assert.True(result.IsAdvertisable);
            _hashDbMock.Verify(h => h.GetAlbumTrackByRecordingIdAsync(recordingId, It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetVariantsByRecordingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _hashDbMock.Verify(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()), Times.Never);
            _hashDbMock.Verify(h => h.GetAlbumTracksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TryGetItemByLocalMetadataAsync_ReturnsExactTagMatch()
        {
            // Arrange
            var fileMetadata = new LocalFileMetadata { Id = "test.flac", SizeBytes = 1024L };
            var tags = new AudioTags("Test Track", "Test Artist", "Test Album", null, null, null, null, null, null, null, null, null, null, null);
            var releaseId = "32345678-1234-1234-1234-123456789abc";
            var recordingId = "42345678-1234-1234-1234-123456789abc";
            _hashDbMock.Setup(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new AlbumTargetEntry { ReleaseId = releaseId, Title = "Test Album", Artist = "Test Artist" } });
            _hashDbMock.Setup(h => h.GetAlbumTracksAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new AlbumTargetTrackEntry { ReleaseId = releaseId, RecordingId = recordingId, Title = "Test Track", Artist = "Test Artist", Position = 1 }
                });
            _hashDbMock.Setup(h => h.GetRecordingIdsWithHashesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<string>(new[] { recordingId }, StringComparer.OrdinalIgnoreCase));

            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryGetItemByLocalMetadataAsync(fileMetadata, tags);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Track", result.Title);
            _hashDbMock.Verify(h => h.GetAlbumTracksAsync(
                It.Is<IEnumerable<string>>(releaseIds => releaseIds.SequenceEqual(new[] { releaseId })),
                It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetRecordingIdsWithHashesAsync(
                It.Is<IEnumerable<string>>(recordingIds => recordingIds.SequenceEqual(new[] { recordingId })),
                It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetAlbumTracksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _hashDbMock.Verify(h => h.LookupHashesByRecordingIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TryGetItemByLocalMetadataAsync_BatchesVariantFallback()
        {
            var fileMetadata = new LocalFileMetadata { Id = "test.flac", SizeBytes = 1024L };
            var tags = new AudioTags("Fallback Track", "Artist", null, null, null, null, null, null, null, null, null, null, null, null);
            var recordingIds = Enumerable.Range(1, 256)
                .Select(index => $"30000000-0000-0000-0000-{index:D12}")
                .ToList();
            var matchingRecordingId = recordingIds[^1];
            _hashDbMock.Setup(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AlbumTargetEntry>());
            _hashDbMock.Setup(h => h.GetAlbumTracksAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AlbumTargetTrackEntry>());
            _hashDbMock.Setup(h => h.GetRecordingIdsWithVariantsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(recordingIds);
            _hashDbMock.Setup(h => h.GetVariantsByRecordingsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AudioVariant>
                {
                    new()
                    {
                        MusicBrainzRecordingId = matchingRecordingId,
                        VariantId = "Fallback Track",
                        QualityScore = 0.9,
                    },
                });
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            var result = await provider.TryGetItemByLocalMetadataAsync(fileMetadata, tags);

            Assert.NotNull(result);
            Assert.Equal("Fallback Track", result.Title);
            _hashDbMock.Verify(h => h.GetVariantsByRecordingsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(recordingIds)),
                It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetVariantsByRecordingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetRecentItemsAsync_UsesBoundedTracksAndBatchedPresence()
        {
            var recordingIds = Enumerable.Range(1, 3)
                .Select(index => $"20000000-0000-0000-0000-{index:D12}")
                .ToArray();
            var tracks = Enumerable.Range(1, 3)
                .Select(index => new AlbumTargetTrackEntry
                {
                    ReleaseId = "10000000-0000-0000-0000-000000000000",
                    RecordingId = recordingIds[index - 1],
                    Title = $"Track {index}",
                    Artist = "Artist",
                    Position = index,
                })
                .ToArray();
            _hashDbMock.Setup(h => h.GetRecentAlbumTracksAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tracks);
            _hashDbMock.Setup(h => h.GetRecordingIdsWithHashesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<string>(new[] { recordingIds[0], recordingIds[2] }, StringComparer.OrdinalIgnoreCase));
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            var items = await provider.GetRecentItemsAsync(3);

            Assert.Equal(new[] { "Track 1", "Track 2", "Track 3" }, items.Select(item => item.Title));
            Assert.Equal(new[] { true, false, true }, items.Select(item => item.IsAdvertisable));
            _hashDbMock.Verify(h => h.GetRecentAlbumTracksAsync(3, It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetRecordingIdsWithHashesAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(recordingIds)),
                It.IsAny<CancellationToken>()), Times.Once);
            _hashDbMock.Verify(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()), Times.Never);
            _hashDbMock.Verify(h => h.GetAlbumTracksAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _hashDbMock.Verify(h => h.LookupHashesByRecordingIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TryMatchTrackByFingerprintAsync_ReturnsClosestDurationMatch()
        {
            // Arrange
            var recordingId = "52345678-1234-1234-1234-123456789abc";
            _hashDbMock.Setup(h => h.LookupHashesByAudioFingerprintAsync("fingerprint123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new HashDbEntry { MusicBrainzId = recordingId, DurationMs = 200_000, QualityScore = 1.0, UseCount = 5 }
                });
            _hashDbMock.Setup(h => h.LookupHashesByRecordingIdAsync(recordingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new HashDbEntry { MusicBrainzId = recordingId } });
            _hashDbMock.Setup(h => h.GetVariantsByRecordingAsync(recordingId, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<slskd.Audio.AudioVariant>
                {
                    new() { MusicBrainzRecordingId = recordingId, VariantId = "Fingerprint Track", DurationMs = 200_000, QualityScore = 1.0 }
                }));
            _hashDbMock.Setup(h => h.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AlbumTargetEntry>());
            var provider = new MusicContentDomainProvider(_loggerMock.Object, _hashDbMock.Object);

            // Act
            var result = await provider.TryMatchTrackByFingerprintAsync("fingerprint123", 200);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Fingerprint Track", result.Title);
        }
    }
}
