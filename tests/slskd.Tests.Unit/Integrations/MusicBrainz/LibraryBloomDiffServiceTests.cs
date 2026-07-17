// <copyright file="LibraryBloomDiffServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.MusicBrainz;

using Moq;
using slskd.HashDb;
using slskd.HashDb.Models;
using slskd.Integrations.MusicBrainz.Bloom;
using slskd.PodCore;
using slskd.Wishlist;

public sealed class LibraryBloomDiffServiceTests
{
    [Fact]
    public void BloomFilter_FromBase64_RestoresMembership()
    {
        var filter = new BloomFilter(16, 0.01);
        filter.Add("salt\u001fmusicbrainz:recording\u001frec-1");

        var restored = BloomFilter.FromBase64(16, 0.01, filter.ToBase64(), filter.ItemCount);

        Assert.True(restored.Contains("salt\u001fmusicbrainz:recording\u001frec-1"));
        Assert.False(restored.Contains("salt\u001fmusicbrainz:recording\u001frec-2"));
    }

    [Fact]
    public async Task CreateSnapshotAsync_ContainsOnlyBloomMetadata()
    {
        var hashDb = CreateHashDb(
            heldRecordings: new[] { "rec-local" },
            targets: new[]
            {
                new AlbumTargetEntry { ReleaseId = "release-local", Title = "Local Release", Artist = "Artist" },
            },
            tracksByRelease: new Dictionary<string, IEnumerable<AlbumTargetTrackEntry>>
            {
                ["release-local"] = new[]
                {
                    new AlbumTargetTrackEntry
                    {
                        ReleaseId = "release-local",
                        RecordingId = "rec-local",
                        Title = "Local Track",
                        Artist = "Artist",
                    },
                },
            });
        var service = CreateService(hashDb);

        var snapshot = await service.CreateSnapshotAsync(new LibraryBloomSnapshotRequest
        {
            SaltId = "salt-1",
            ExpectedItems = 32,
        });

        Assert.Equal(1, snapshot.Version);
        Assert.Equal("salt-1", snapshot.SaltId);
        Assert.True(snapshot.ItemCount >= 1);
        Assert.True(snapshot.NamespaceItemCounts["musicbrainz:recording"] >= 1);
        Assert.True(snapshot.NamespaceItemCounts["musicbrainz:release"] >= 1);
        Assert.DoesNotContain("rec-local", snapshot.BitsBase64, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snapshot.PrivacyNotes, note => note.Contains("does not include filenames", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateSnapshotAsync_BatchesAlbumTrackReads()
    {
        var targets = Enumerable.Range(1, 100)
            .Select(index => new AlbumTargetEntry
            {
                ReleaseId = $"release-{index}",
                Title = $"Release {index}",
                Artist = "Artist",
            })
            .ToArray();
        var tracksByRelease = targets.ToDictionary(
            target => target.ReleaseId,
            target => (IEnumerable<AlbumTargetTrackEntry>)new[]
            {
                new AlbumTargetTrackEntry
                {
                    ReleaseId = target.ReleaseId,
                    RecordingId = $"recording-{target.ReleaseId}",
                    Title = "Track",
                    Artist = "Artist",
                },
            });
        var hashDb = CreateHashDb(
            heldRecordings: tracksByRelease.Values.SelectMany(tracks => tracks).Select(track => track.RecordingId),
            targets: targets,
            tracksByRelease: tracksByRelease);

        await CreateService(hashDb).CreateSnapshotAsync(new LibraryBloomSnapshotRequest
        {
            SaltId = "salt",
            ExpectedItems = 256,
        });

        hashDb.Verify(service => service.GetAlbumTracksAsync(
            It.Is<IEnumerable<string>>(releaseIds => releaseIds.Count() == 100),
            It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(service => service.GetAlbumTracksAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSnapshotAsync_PreservesNormalizedItemDeduplicationAndMembership()
    {
        var targets = new[]
        {
            new AlbumTargetEntry { ReleaseId = " Release-A ", Title = "Release", Artist = "Artist" },
            new AlbumTargetEntry { ReleaseId = "release-a", Title = "Duplicate", Artist = "Artist" },
            new AlbumTargetEntry { ReleaseId = " ", Title = "Blank", Artist = "Artist" },
        };
        var tracksByRelease = targets.ToDictionary(
            target => target.ReleaseId,
            target => (IEnumerable<AlbumTargetTrackEntry>)new[]
            {
                new AlbumTargetTrackEntry
                {
                    ReleaseId = target.ReleaseId,
                    RecordingId = "rec-1",
                    Title = "Track",
                    Artist = "Artist",
                },
            });
        var service = CreateService(CreateHashDb(
            heldRecordings: new[] { " REC-1 ", "rec-1", "REC-2", " " },
            targets: targets,
            tracksByRelease: tracksByRelease));

        var snapshot = await service.CreateSnapshotAsync(new LibraryBloomSnapshotRequest
        {
            SaltId = "salt",
            ExpectedItems = 16,
        });

        Assert.Equal(2, snapshot.NamespaceItemCounts["musicbrainz:recording"]);
        Assert.Equal(1, snapshot.NamespaceItemCounts["musicbrainz:release"]);
        var filter = BloomFilter.FromBase64(
            snapshot.ExpectedItems,
            snapshot.FalsePositiveRate,
            snapshot.BitsBase64,
            snapshot.ItemCount);
        Assert.True(filter.Contains("salt\u001fmusicbrainz:recording\u001frec-1"));
        Assert.True(filter.Contains("salt\u001fmusicbrainz:recording\u001frec-2"));
        Assert.True(filter.Contains("salt\u001fmusicbrainz:release\u001frelease-a"));
    }

    [Fact]
    public async Task CreateSnapshotAsync_WideHeldRecordingSetBoundsAllocation()
    {
        const int recordingCount = 10_000;
        var service = CreateService(CreateHashDb(
            heldRecordings: Enumerable.Range(0, recordingCount)
                .Select(index => $"recording-{index:D5}")));
        var request = new LibraryBloomSnapshotRequest
        {
            SaltId = "allocation-salt",
            ExpectedItems = recordingCount,
        };

        for (var iteration = 0; iteration < 4; iteration++)
        {
            await service.CreateSnapshotAsync(request);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var snapshot = await service.CreateSnapshotAsync(request);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.InRange(snapshot.ItemCount, 1, recordingCount);
        Assert.Equal(recordingCount, snapshot.NamespaceItemCounts["musicbrainz:recording"]);
        Assert.False(snapshot.NamespaceItemCounts.ContainsKey("musicbrainz:release"));
        Assert.True(allocatedBytes < 2_600_000, $"Allocated {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task CompareAsync_ReturnsLikelyMissingSuggestionFromInboundRecordingBloom()
    {
        var remoteSnapshot = await CreateRemoteSnapshotAsync("rec-missing");
        var localHashDb = CreateHashDb(
            heldRecordings: Array.Empty<string>(),
            targets: new[]
            {
                new AlbumTargetEntry { ReleaseId = "release-1", Title = "Remote Release", Artist = "Remote Artist" },
            },
            tracksByRelease: new Dictionary<string, IEnumerable<AlbumTargetTrackEntry>>
            {
                ["release-1"] = new[]
                {
                    new AlbumTargetTrackEntry
                    {
                        ReleaseId = "release-1",
                        RecordingId = "rec-missing",
                        Title = "Missing Track",
                        Artist = "Remote Artist",
                    },
                },
            });
        var service = CreateService(localHashDb);

        var diff = await service.CompareAsync(new LibraryBloomDiffRequest
        {
            Snapshot = remoteSnapshot,
            Limit = 10,
        });

        Assert.True(diff.IsCompatible);
        var suggestion = Assert.Single(diff.Suggestions);
        Assert.Equal("rec-missing", suggestion.Mbid);
        Assert.Equal("Remote Artist Missing Track", suggestion.SearchText);
        Assert.True(suggestion.Confidence < 1);
        Assert.Contains("false positives", suggestion.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompareAsync_PreservesFirstDuplicateMetadataAndStableSortTies()
    {
        var recordingIds = new[] { "recording-b", "recording-duplicate", "recording-a" };
        var targets = new[]
        {
            new AlbumTargetEntry { ReleaseId = "release-z", Title = "Same Release", Artist = "Artist" },
            new AlbumTargetEntry { ReleaseId = "release-a", Title = "Same Release", Artist = "Artist" },
        };
        var tracksByRelease = new Dictionary<string, IEnumerable<AlbumTargetTrackEntry>>
        {
            ["release-z"] = new[]
            {
                new AlbumTargetTrackEntry
                {
                    ReleaseId = "release-z",
                    RecordingId = "recording-b",
                    Title = "Same Track",
                    Artist = "Artist",
                },
                new AlbumTargetTrackEntry
                {
                    ReleaseId = "release-z",
                    RecordingId = "recording-duplicate",
                    Title = "Same Track",
                    Artist = "Artist",
                },
            },
            ["release-a"] = new[]
            {
                new AlbumTargetTrackEntry
                {
                    ReleaseId = "release-a",
                    RecordingId = "recording-a",
                    Title = "Same Track",
                    Artist = "Artist",
                },
                new AlbumTargetTrackEntry
                {
                    ReleaseId = "release-a",
                    RecordingId = "RECORDING-DUPLICATE",
                    Title = "Discarded Duplicate",
                    Artist = "Other Artist",
                },
            },
        };
        var service = CreateService(CreateHashDb(
            heldRecordings: Array.Empty<string>(),
            targets: targets,
            tracksByRelease: tracksByRelease));

        var result = await service.CompareAsync(new LibraryBloomDiffRequest
        {
            Snapshot = await CreateRemoteSnapshotAsync(recordingIds),
            Limit = 10,
        });

        Assert.Equal(recordingIds, result.Suggestions.Select(suggestion => suggestion.Mbid));
        Assert.Equal("release-z", result.Suggestions[1].ReleaseId);
        Assert.Equal("Same Track", result.Suggestions[1].TrackTitle);
    }

    [Fact]
    public async Task CompareAsync_DuplicateHeavyCandidatesBoundAllocation()
    {
        const int releaseCount = 100;
        const int recordingCount = 100;
        var recordingIds = Enumerable.Range(0, recordingCount)
            .Select(index => $"recording-{index:D3}")
            .ToArray();
        var targets = Enumerable.Range(0, releaseCount)
            .Select(index => new AlbumTargetEntry
            {
                ReleaseId = $"release-{index:D3}",
                Title = $"Release {index:D3}",
                Artist = "Artist",
            })
            .ToArray();
        var tracksByRelease = targets.ToDictionary(
            target => target.ReleaseId,
            target => (IEnumerable<AlbumTargetTrackEntry>)recordingIds.Select((recordingId, index) =>
                new AlbumTargetTrackEntry
                {
                    ReleaseId = target.ReleaseId,
                    RecordingId = recordingId,
                    Title = $"Track {index:D3}",
                    Artist = "Artist",
                }).ToArray());
        var service = CreateService(CreateHashDb(
            heldRecordings: Array.Empty<string>(),
            targets: targets,
            tracksByRelease: tracksByRelease));
        var request = new LibraryBloomDiffRequest
        {
            Snapshot = await CreateRemoteSnapshotAsync(recordingIds),
            Limit = recordingCount,
        };

        for (var iteration = 0; iteration < 4; iteration++)
        {
            await service.CompareAsync(request);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await service.CompareAsync(request);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(recordingCount, result.Suggestions.Count);
        Assert.Equal("recording-000", result.Suggestions[0].Mbid);
        Assert.Equal("recording-099", result.Suggestions[^1].Mbid);
        Assert.True(allocatedBytes < 400_000, $"Allocated {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task CompareAsync_RejectsUnsupportedSnapshotVersion()
    {
        var service = CreateService(CreateHashDb());

        var diff = await service.CompareAsync(new LibraryBloomDiffRequest
        {
            Snapshot = new LibraryBloomSnapshot
            {
                Version = 99,
                SnapshotId = "future",
                SaltId = "salt",
                BitsBase64 = Convert.ToBase64String(new byte[] { 1 }),
                ExpectedItems = 16,
                FalsePositiveRate = 0.01,
            },
        });

        Assert.False(diff.IsCompatible);
        Assert.Empty(diff.Suggestions);
        Assert.Contains("Unsupported library Bloom snapshot version.", diff.Warnings);
    }

    [Fact]
    public async Task PromoteSuggestionsToWishlistAsync_CreatesReviewOnlyWishlistSeeds()
    {
        var remoteSnapshot = await CreateRemoteSnapshotAsync("rec-missing");
        var localHashDb = CreateHashDb(
            heldRecordings: Array.Empty<string>(),
            targets: new[]
            {
                new AlbumTargetEntry { ReleaseId = "release-1", Title = "Remote Release", Artist = "Remote Artist" },
            },
            tracksByRelease: new Dictionary<string, IEnumerable<AlbumTargetTrackEntry>>
            {
                ["release-1"] = new[]
                {
                    new AlbumTargetTrackEntry
                    {
                        ReleaseId = "release-1",
                        RecordingId = "rec-missing",
                        Title = "Missing Track",
                        Artist = "Remote Artist",
                    },
                },
            });
        var wishlist = new Mock<IWishlistService>();
        wishlist.Setup(service => service.ListAsync()).ReturnsAsync(new List<WishlistItem>());
        wishlist.Setup(service => service.CreateManyAsync(
                It.IsAny<IEnumerable<WishlistItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<WishlistItem> items, CancellationToken _) => items
                .Select(item =>
                {
                    item.Id = Guid.NewGuid();
                    return item;
                })
                .ToList());
        var service = CreateService(localHashDb, wishlist);

        var result = await service.PromoteSuggestionsToWishlistAsync(new LibraryBloomWishlistPromotionRequest
        {
            DiffRequest = new LibraryBloomDiffRequest { Snapshot = remoteSnapshot },
            Filter = "flac",
            MaxResults = 25,
        });

        Assert.Equal(1, result.CreatedCount);
        Assert.Empty(result.CreatedItemIds.Where(id => id == Guid.Empty));
        wishlist.Verify(service => service.CreateManyAsync(
            It.Is<IEnumerable<WishlistItem>>(items => items.Count() == 1 &&
                items.Single().SearchText == "Remote Artist Missing Track" &&
                items.Single().Filter == "flac" &&
                items.Single().AutoDownload == false &&
                items.Single().MaxResults == 25),
            It.IsAny<CancellationToken>()), Times.Once);
        wishlist.Verify(service => service.CreateAsync(It.IsAny<WishlistItem>()), Times.Never);
    }

    [Fact]
    public async Task PromoteSuggestionsToWishlistAsync_WithMaximumSuggestions_UsesOneBulkCreate()
    {
        const int suggestionCount = 250;
        var recordingIds = Enumerable.Range(1, suggestionCount)
            .Select(index => $"rec-{index}")
            .ToArray();
        var remoteSnapshot = await CreateRemoteSnapshotAsync(recordingIds);
        var localHashDb = CreateHashDb(
            heldRecordings: Array.Empty<string>(),
            targets: new[]
            {
                new AlbumTargetEntry { ReleaseId = "release-1", Title = "Remote Release", Artist = "Remote Artist" },
            },
            tracksByRelease: new Dictionary<string, IEnumerable<AlbumTargetTrackEntry>>
            {
                ["release-1"] = recordingIds.Select((recordingId, index) => new AlbumTargetTrackEntry
                {
                    ReleaseId = "release-1",
                    Position = index + 1,
                    RecordingId = recordingId,
                    Title = $"Missing Track {index + 1}",
                    Artist = "Remote Artist",
                }),
            });
        var wishlist = new Mock<IWishlistService>();
        wishlist.Setup(service => service.ListAsync()).ReturnsAsync(
            Enumerable.Range(1, 9_999)
                .Select(index => new WishlistItem
                {
                    SearchText = $"Unrelated {index}",
                    Filter = "flac",
                })
                .Append(new WishlistItem
                {
                    SearchText = "Remote Artist Missing Track 250",
                    Filter = "mp3",
                })
                .ToList());
        wishlist.Setup(service => service.CreateManyAsync(
                It.IsAny<IEnumerable<WishlistItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<WishlistItem> items, CancellationToken _) => items
                .Select(item =>
                {
                    item.Id = Guid.NewGuid();
                    return item;
                })
                .ToList());
        var service = CreateService(localHashDb, wishlist);

        var result = await service.PromoteSuggestionsToWishlistAsync(new LibraryBloomWishlistPromotionRequest
        {
            DiffRequest = new LibraryBloomDiffRequest
            {
                Snapshot = remoteSnapshot,
                Limit = suggestionCount,
            },
        });

        Assert.Equal(suggestionCount - 1, result.CreatedCount);
        Assert.Equal(1, result.AlreadySeededCount);
        Assert.Equal(suggestionCount - 1, result.CreatedItemIds.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, result.CreatedItemIds);
        wishlist.Verify(service => service.CreateManyAsync(
            It.Is<IEnumerable<WishlistItem>>(items => items.Count() == suggestionCount - 1 &&
                items.All(item => item.SearchText != "Remote Artist Missing Track 250")),
            It.IsAny<CancellationToken>()), Times.Once);
        wishlist.Verify(service => service.CreateAsync(It.IsAny<WishlistItem>()), Times.Never);
        wishlist.Verify(service => service.ListAsync(), Times.Once);
    }

    private static async Task<LibraryBloomSnapshot> CreateRemoteSnapshotAsync(string recordingId)
        => await CreateRemoteSnapshotAsync(new[] { recordingId });

    private static async Task<LibraryBloomSnapshot> CreateRemoteSnapshotAsync(IEnumerable<string> recordingIds)
    {
        var remoteHashDb = CreateHashDb(heldRecordings: recordingIds);
        return await CreateService(remoteHashDb).CreateSnapshotAsync(new LibraryBloomSnapshotRequest
        {
            SaltId = "remote-salt",
            ExpectedItems = 32,
        });
    }

    private static LibraryBloomDiffService CreateService(
        Mock<IHashDbService> hashDb,
        Mock<IWishlistService>? wishlist = null)
    {
        if (wishlist == null)
        {
            wishlist = new Mock<IWishlistService>();
            wishlist.Setup(service => service.ListAsync()).ReturnsAsync(new List<WishlistItem>());
        }

        return new LibraryBloomDiffService(hashDb.Object, wishlist.Object);
    }

    private static Mock<IHashDbService> CreateHashDb(
        IEnumerable<string>? heldRecordings = null,
        IEnumerable<AlbumTargetEntry>? targets = null,
        IReadOnlyDictionary<string, IEnumerable<AlbumTargetTrackEntry>>? tracksByRelease = null)
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((heldRecordings ?? Array.Empty<string>()).ToList());
        hashDb.Setup(service => service.GetAlbumTargetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(targets ?? Array.Empty<AlbumTargetEntry>());
        hashDb.Setup(service => service.GetAlbumTracksAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> releaseIds, CancellationToken _) =>
                releaseIds.SelectMany(releaseId =>
                    tracksByRelease != null && tracksByRelease.TryGetValue(releaseId, out var tracks)
                        ? tracks
                        : Array.Empty<AlbumTargetTrackEntry>()));
        return hashDb;
    }
}
