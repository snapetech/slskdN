// <copyright file="LibraryReconciliationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.v2.Reconciliation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using slskd.VirtualSoulfind.v2.Catalogue;
    using slskd.VirtualSoulfind.v2.Reconciliation;
    using Xunit;

    /// <summary>
    ///     Tests for <see cref="LibraryReconciliationService"/>.
    /// </summary>
    public class LibraryReconciliationServiceTests
    {
        [Fact]
        public async Task FindMissingTracksForRelease_EmptyRelease_ReturnsEmpty()
        {
            // Arrange
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var releaseId = Guid.NewGuid().ToString();

            // Act
            var missing = await service.FindMissingTracksForReleaseAsync(releaseId);

            // Assert
            Assert.Empty(missing);
        }

        [Fact]
        public async Task FindMissingTracksForRelease_AllTracksHaveLocalFiles_ReturnsEmpty()
        {
            // Arrange
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var (release, tracks) = await CreateTestReleaseWithTracks(catalogue, 3);

            // Add local files for all tracks
            foreach (var track in tracks)
            {
                var localFile = CreateLocalFile(track.TrackId);
                await catalogue.UpsertLocalFileAsync(localFile);
            }

            // Act
            var missing = await service.FindMissingTracksForReleaseAsync(release.ReleaseId);

            // Assert
            Assert.Empty(missing);
        }

        [Fact]
        public async Task FindMissingTracksForRelease_SomeTracksMissing_ReturnsOnlyMissing()
        {
            // Arrange
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var (release, tracks) = await CreateTestReleaseWithTracks(catalogue, 5);

            // Add local files for tracks 0, 1, 2 only
            for (int i = 0; i < 3; i++)
            {
                var localFile = CreateLocalFile(tracks[i].TrackId);
                await catalogue.UpsertLocalFileAsync(localFile);
            }

            // Act
            var missing = await service.FindMissingTracksForReleaseAsync(release.ReleaseId);

            // Assert
            Assert.Equal(2, missing.Count);
            Assert.Contains(tracks[3].TrackId, missing);
            Assert.Contains(tracks[4].TrackId, missing);
        }

        [Fact]
        public async Task FindMissingTracksForRelease_TracksWithVerifiedCopiesNotMissing()
        {
            // Arrange
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var (release, tracks) = await CreateTestReleaseWithTracks(catalogue, 3);

            // Add verified copy for track 0
            var localFile = CreateLocalFile(tracks[0].TrackId);
            await catalogue.UpsertLocalFileAsync(localFile);

            var verifiedCopy = new VerifiedCopy
            {
                VerifiedCopyId = Guid.NewGuid().ToString(),
                TrackId = tracks[0].TrackId,
                LocalFileId = localFile.LocalFileId,
                HashPrimary = localFile.HashPrimary,
                DurationSeconds = localFile.DurationSeconds,
                VerificationSource = VerificationSource.Manual,
                VerifiedAt = DateTimeOffset.UtcNow,
            };
            await catalogue.UpsertVerifiedCopyAsync(verifiedCopy);

            // Act
            var missing = await service.FindMissingTracksForReleaseAsync(release.ReleaseId);

            // Assert
            Assert.Equal(2, missing.Count);
            Assert.DoesNotContain(tracks[0].TrackId, missing);
        }

        [Fact]
        public async Task FindMissingTracksForRelease_AllTracksMissing_ReturnsAll()
        {
            // Arrange
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var (release, tracks) = await CreateTestReleaseWithTracks(catalogue, 4);

            // Act
            var missing = await service.FindMissingTracksForReleaseAsync(release.ReleaseId);

            // Assert
            Assert.Equal(4, missing.Count);
            Assert.All(tracks, track => Assert.Contains(track.TrackId, missing));
        }

        [Fact]
        public async Task FindMissingTracksForRelease_OneThousandTracks_LoadsCopyStatesOnce()
        {
            var tracks = Enumerable.Range(0, 1_000)
                .Select(index => new Track
                {
                    TrackId = $"track-{index:D4}",
                    ReleaseId = "release",
                    DiscNumber = 1,
                    TrackNumber = index + 1,
                    Title = $"Track {index}",
                })
                .ToList();
            IReadOnlyCollection<string>? requestedTrackIds = null;
            var catalogue = new Mock<ICatalogueStore>(MockBehavior.Strict);
            catalogue
                .Setup(store => store.ListTracksForReleaseAsync("release", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tracks);
            catalogue
                .Setup(store => store.GetTrackCopyStatesAsync(
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<string>, CancellationToken>((trackIds, _) => requestedTrackIds = trackIds)
                .ReturnsAsync(new Dictionary<string, TrackCopyState>
                {
                    [tracks[0].TrackId] = new(HasLocalFile: true, HasVerifiedCopy: false),
                    [tracks[1].TrackId] = new(HasLocalFile: false, HasVerifiedCopy: true),
                });
            var service = new LibraryReconciliationService(catalogue.Object);

            var missing = await service.FindMissingTracksForReleaseAsync("release");

            Assert.Equal(tracks.Skip(2).Select(track => track.TrackId), missing);
            Assert.Equal(tracks.Select(track => track.TrackId), requestedTrackIds);
            catalogue.Verify(store => store.GetTrackCopyStatesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.ListLocalFilesForTrackAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            catalogue.Verify(store => store.FindVerifiedCopyForTrackAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FindTracksWithoutLocalCopies_NoTracks_ReturnsEmpty()
        {
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);

            // Act
            var result = await service.FindTracksWithoutLocalCopiesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task FindTracksWithoutLocalCopies_FullPage_LoadsCopyStatesOnce()
        {
            var tracks = Enumerable.Range(0, 250)
                .Select(index => new Track
                {
                    TrackId = $"track-{index:D3}",
                    ReleaseId = "release",
                    DiscNumber = 1,
                    TrackNumber = index + 1,
                    Title = $"Track {index}",
                })
                .ToList();
            IReadOnlyDictionary<string, TrackCopyState> states = tracks
                .Where((_, index) => index % 2 == 0)
                .ToDictionary(
                    track => track.TrackId,
                    _ => new TrackCopyState(HasLocalFile: true, HasVerifiedCopy: false));
            var catalogue = new Mock<ICatalogueStore>(MockBehavior.Strict);
            catalogue
                .Setup(store => store.CountTracksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tracks.Count);
            catalogue
                .Setup(store => store.ListTracksAsync(0, 250, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tracks);
            catalogue
                .Setup(store => store.GetTrackCopyStatesAsync(
                    It.Is<IReadOnlyCollection<string>>(trackIds => trackIds.Count == tracks.Count),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(states);
            var service = new LibraryReconciliationService(catalogue.Object);

            var missing = await service.FindTracksWithoutLocalCopiesAsync(limit: 250);

            Assert.Equal(tracks.Where((_, index) => index % 2 != 0).Select(track => track.TrackId), missing);
            catalogue.Verify(store => store.GetTrackCopyStatesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.ListLocalFilesForTrackAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FindOrphanedLocalFiles_NoFiles_ReturnsEmpty()
        {
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);

            // Act
            var result = await service.FindOrphanedLocalFilesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AnalyzeAllReleases_NoReleases_ReturnsEmpty()
        {
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);

            // Act
            var result = await service.AnalyzeAllReleasesAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AnalyzeAllReleases_PartialRelease_PreservesCopyCounts()
        {
            using var catalogue = new InMemoryCatalogueStore();
            var service = new LibraryReconciliationService(catalogue);
            var (release, tracks) = await CreateTestReleaseWithTracks(catalogue, 3);
            await catalogue.UpsertLocalFileAsync(CreateLocalFile(tracks[0].TrackId));
            var verifiedFile = CreateLocalFile();
            await catalogue.UpsertLocalFileAsync(verifiedFile);
            await catalogue.UpsertVerifiedCopyAsync(new VerifiedCopy
            {
                VerifiedCopyId = Guid.NewGuid().ToString(),
                TrackId = tracks[1].TrackId,
                LocalFileId = verifiedFile.LocalFileId,
                HashPrimary = verifiedFile.HashPrimary,
                DurationSeconds = verifiedFile.DurationSeconds,
                VerificationSource = VerificationSource.Manual,
                VerifiedAt = DateTimeOffset.UtcNow,
            });

            var results = await service.AnalyzeAllReleasesAsync();

            var analysis = Assert.Single(results);
            Assert.Equal(release.ReleaseId, analysis.ReleaseId);
            Assert.Equal(2, analysis.TracksWithLocalCopies);
            Assert.Equal(1, analysis.TracksWithVerifiedCopies);
            Assert.Equal(new[] { tracks[2].TrackId }, analysis.MissingTrackIds);
        }

        [Fact]
        public async Task AnalyzeAllReleases_FullPage_LoadsEvidenceInBatches()
        {
            const int releaseCount = 250;
            const int tracksPerRelease = 10;
            var artists = Enumerable.Range(0, releaseCount)
                .ToDictionary(
                    index => $"artist-{index:D3}",
                    index => new Artist
                    {
                        ArtistId = $"artist-{index:D3}",
                        Name = $"Artist {index:D3}",
                    });
            var releaseGroups = Enumerable.Range(0, releaseCount)
                .ToDictionary(
                    index => $"group-{index:D3}",
                    index => new ReleaseGroup
                    {
                        ReleaseGroupId = $"group-{index:D3}",
                        ArtistId = $"artist-{index:D3}",
                        Title = $"Group {index:D3}",
                    });
            var releases = Enumerable.Range(0, releaseCount)
                .Select(index => new Release
                {
                    ReleaseId = $"release-{index:D3}",
                    ReleaseGroupId = $"group-{index:D3}",
                    Title = $"Release {index:D3}",
                })
                .ToList();
            IReadOnlyDictionary<string, IReadOnlyList<Track>> tracksByRelease = releases
                .ToDictionary(
                    release => release.ReleaseId,
                    release => (IReadOnlyList<Track>)Enumerable.Range(0, tracksPerRelease)
                        .Select(index => new Track
                        {
                            TrackId = $"{release.ReleaseId}-track-{index:D2}",
                            ReleaseId = release.ReleaseId,
                            DiscNumber = 1,
                            TrackNumber = index + 1,
                            Title = $"Track {index:D2}",
                        })
                        .ToList());
            var copyStates = tracksByRelease.Values
                .Select(tracks => tracks[0])
                .ToDictionary(
                    track => track.TrackId,
                    _ => new TrackCopyState(HasLocalFile: true, HasVerifiedCopy: false));
            var catalogue = new Mock<ICatalogueStore>(MockBehavior.Strict);
            catalogue
                .Setup(store => store.CountReleasesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(releaseCount);
            catalogue
                .Setup(store => store.ListReleasesAsync(0, 250, It.IsAny<CancellationToken>()))
                .ReturnsAsync(releases);
            catalogue
                .Setup(store => store.GetTracksByReleaseIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Count == releaseCount),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(tracksByRelease);
            catalogue
                .Setup(store => store.GetReleaseGroupsByIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Count == releaseCount),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(releaseGroups);
            catalogue
                .Setup(store => store.GetArtistsByIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Count == releaseCount),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(artists);
            catalogue
                .Setup(store => store.GetTrackCopyStatesAsync(
                    It.Is<IReadOnlyCollection<string>>(ids => ids.Count == releaseCount * tracksPerRelease),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(copyStates);
            var service = new LibraryReconciliationService(catalogue.Object);

            var results = await service.AnalyzeAllReleasesAsync();

            Assert.Equal(releaseCount, results.Count);
            Assert.Equal(releases.Select(release => release.ReleaseId), results.Select(result => result.ReleaseId));
            Assert.Equal("Artist 000", results[0].ArtistName);
            Assert.Equal(tracksPerRelease - 1, results[0].MissingTrackIds.Count);
            catalogue.Verify(store => store.GetTracksByReleaseIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.GetReleaseGroupsByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.GetArtistsByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.GetTrackCopyStatesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.ListTracksForReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            catalogue.Verify(store => store.FindReleaseGroupByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            catalogue.Verify(store => store.FindArtistByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FindUpgradeOpportunities_FullPage_LoadsTracksOnce()
        {
            var localFiles = Enumerable.Range(0, 250)
                .Select(index => CreateLocalFile($"track-{index:D3}", codec: "MP3", bitrate: 128))
                .ToList();
            var tracks = localFiles
                .Take(localFiles.Count - 1)
                .ToDictionary(
                    file => file.InferredTrackId!,
                    file => new Track
                    {
                        TrackId = file.InferredTrackId!,
                        ReleaseId = "release",
                        DiscNumber = 1,
                        TrackNumber = 1,
                        Title = $"Title {file.InferredTrackId}",
                    });
            IReadOnlyCollection<string>? requestedTrackIds = null;
            var catalogue = new Mock<ICatalogueStore>(MockBehavior.Strict);
            catalogue
                .Setup(store => store.CountLocalFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(localFiles.Count);
            catalogue
                .Setup(store => store.CountVerifiedCopiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            catalogue
                .Setup(store => store.ListLocalFilesAsync(0, 250, It.IsAny<CancellationToken>()))
                .ReturnsAsync(localFiles);
            catalogue
                .Setup(store => store.GetTracksByIdsAsync(
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<string>, CancellationToken>((trackIds, _) => requestedTrackIds = trackIds)
                .ReturnsAsync(tracks);
            var service = new LibraryReconciliationService(catalogue.Object);

            var suggestions = await service.FindUpgradeOpportunitiesAsync();

            Assert.Equal(localFiles.Count, suggestions.Count);
            Assert.Equal(localFiles.Select(file => file.InferredTrackId), requestedTrackIds);
            Assert.Equal("Title track-000", suggestions[0].TrackTitle);
            Assert.Equal("track-249", suggestions[^1].TrackTitle);
            catalogue.Verify(store => store.GetTracksByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            catalogue.Verify(store => store.FindTrackByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReleaseGapAnalysis_CompletionPercentage_CalculatesCorrectly()
        {
            // Arrange
            var analysis = new ReleaseGapAnalysis
            {
                ReleaseId = "test",
                ReleaseTitle = "Test Album",
                ArtistName = "Test Artist",
                TotalTracks = 10,
                TracksWithLocalCopies = 7,
                TracksWithVerifiedCopies = 5,
                MissingTrackIds = new[] { "1", "2", "3" },
            };

            // Act
            var percentage = analysis.CompletionPercentage;

            // Assert
            Assert.Equal(0.7f, percentage);
        }

        [Fact]
        public async Task ReleaseGapAnalysis_IsPartial_TrueWhenSomeTracksPresent()
        {
            // Arrange
            var analysis = new ReleaseGapAnalysis
            {
                ReleaseId = "test",
                ReleaseTitle = "Test Album",
                ArtistName = "Test Artist",
                TotalTracks = 10,
                TracksWithLocalCopies = 7,
                TracksWithVerifiedCopies = 5,
                MissingTrackIds = new[] { "1", "2", "3" },
            };

            // Act & Assert
            Assert.True(analysis.IsPartial);
        }

        [Fact]
        public async Task ReleaseGapAnalysis_IsPartial_FalseWhenComplete()
        {
            // Arrange
            var analysis = new ReleaseGapAnalysis
            {
                ReleaseId = "test",
                ReleaseTitle = "Test Album",
                ArtistName = "Test Artist",
                TotalTracks = 10,
                TracksWithLocalCopies = 10,
                TracksWithVerifiedCopies = 10,
                MissingTrackIds = Array.Empty<string>(),
            };

            // Act & Assert
            Assert.False(analysis.IsPartial);
        }

        [Fact]
        public async Task ReleaseGapAnalysis_IsPartial_FalseWhenEmpty()
        {
            // Arrange
            var analysis = new ReleaseGapAnalysis
            {
                ReleaseId = "test",
                ReleaseTitle = "Test Album",
                ArtistName = "Test Artist",
                TotalTracks = 10,
                TracksWithLocalCopies = 0,
                TracksWithVerifiedCopies = 0,
                MissingTrackIds = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" },
            };

            // Act & Assert
            Assert.False(analysis.IsPartial);
        }

        [Fact]
        public async Task UpgradeSuggestion_HasAllRequiredProperties()
        {
            // Arrange
            var suggestion = new UpgradeSuggestion
            {
                TrackId = "track1",
                TrackTitle = "Test Track",
                LocalFileId = "file1",
                CurrentQuality = 0.6f,
                TargetQuality = "FLAC",
                QualityImprovement = 0.4f,
                CurrentCodec = "MP3",
                CurrentBitrate = 128,
            };

            // Act & Assert
            Assert.Equal("track1", suggestion.TrackId);
            Assert.Equal("Test Track", suggestion.TrackTitle);
            Assert.Equal(0.6f, suggestion.CurrentQuality);
            Assert.Equal("FLAC", suggestion.TargetQuality);
            Assert.Equal(0.4f, suggestion.QualityImprovement);
            Assert.Equal("MP3", suggestion.CurrentCodec);
            Assert.Equal(128, suggestion.CurrentBitrate);
        }

        // Helper methods

        private static async Task<(Release release, Track[] tracks)> CreateTestReleaseWithTracks(
            InMemoryCatalogueStore catalogue,
            int trackCount)
        {
            // Create artist
            var artist = new Artist
            {
                ArtistId = Guid.NewGuid().ToString(),
                MusicBrainzId = null,
                Name = "Test Artist",
                SortName = "Test Artist",
                Tags = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            await catalogue.UpsertArtistAsync(artist);

            // Create release group
            var releaseGroup = new ReleaseGroup
            {
                ReleaseGroupId = Guid.NewGuid().ToString(),
                MusicBrainzId = null,
                ArtistId = artist.ArtistId,
                Title = "Test Album",
                PrimaryType = ReleaseGroupPrimaryType.Album,
                Year = 2024,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            await catalogue.UpsertReleaseGroupAsync(releaseGroup);

            // Create release
            var release = new Release
            {
                ReleaseId = Guid.NewGuid().ToString(),
                MusicBrainzId = null,
                ReleaseGroupId = releaseGroup.ReleaseGroupId,
                Title = "Test Album",
                Year = 2024,
                Country = "US",
                Label = "Test Label",
                CatalogNumber = "TEST001",
                MediaCount = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            await catalogue.UpsertReleaseAsync(release);

            // Create tracks
            var tracks = new Track[trackCount];
            for (int i = 0; i < trackCount; i++)
            {
                tracks[i] = new Track
                {
                    TrackId = Guid.NewGuid().ToString(),
                    MusicBrainzRecordingId = null,
                    ReleaseId = release.ReleaseId,
                    DiscNumber = 1,
                    TrackNumber = i + 1,
                    Title = $"Track {i + 1}",
                    DurationSeconds = 180,
                    Isrc = null,
                    Tags = null,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                await catalogue.UpsertTrackAsync(tracks[i]);
            }

            return (release, tracks);
        }

        private LocalFile CreateLocalFile(
            string? inferredTrackId = null,
            string codec = "FLAC",
            int bitrate = 1411)
        {
            return new LocalFile
            {
                LocalFileId = Guid.NewGuid().ToString(),
                Path = $"/music/test/{Guid.NewGuid()}.flac",
                SizeBytes = 25_000_000,
                DurationSeconds = 180,
                Codec = codec,
                Bitrate = bitrate,
                Channels = 2,
                HashPrimary = Guid.NewGuid().ToString("N"),
                HashSecondary = Guid.NewGuid().ToString("N"),
                AudioFingerprintId = null,
                InferredTrackId = inferredTrackId,
                AddedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }
    }
}
