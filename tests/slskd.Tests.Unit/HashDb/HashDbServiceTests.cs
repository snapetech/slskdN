// <copyright file="HashDbServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.HashDb;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using slskd.Events;
using slskd.Audio;
using slskd.HashDb;
using slskd.HashDb.Models;
using slskd.Integrations.MusicBrainz.Models;
using slskd.Jobs;
using slskd.LibraryHealth;
using Xunit;

public class HashDbServiceTests : IDisposable
{
    private readonly string testDir;
    private readonly HashDbService service;

    public HashDbServiceTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"hashdb-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);
        service = new HashDbService(testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_InitializesDatabase()
    {
        var dbPath = Path.Combine(testDir, "hashdb.db");
        Assert.True(File.Exists(dbPath));
    }

    [Fact]
    public void Constructor_InitializesSeqIdToZero()
    {
        Assert.Equal(0, service.CurrentSeqId);
    }

    [Fact]
    public async Task Constructor_IndexesNormalizedRecordingIdPages()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT DISTINCT TRIM(musicbrainz_id) COLLATE NOCASE AS recording_id
            FROM HashDb
            WHERE musicbrainz_id IS NOT NULL AND TRIM(musicbrainz_id) <> ''
              AND TRIM(musicbrainz_id) COLLATE NOCASE > 'm-recording'
            ORDER BY recording_id COLLATE NOCASE
            LIMIT 100
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH HashDb USING INDEX idx_hashdb_recording_normalized", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesBatchedRecordingPresenceQueryWithoutTemporarySort()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT TRIM(musicbrainz_id)
            FROM HashDb
            WHERE musicbrainz_id IS NOT NULL
              AND TRIM(musicbrainz_id) <> ''
              AND TRIM(musicbrainz_id) COLLATE NOCASE IN ('recording-a', 'recording-b')
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH HashDb USING INDEX idx_hashdb_recording_normalized", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesBatchedAlbumTrackQueryWithoutTemporarySort()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT release_id, track_position, recording_id, title, artist, duration_ms, isrc
            FROM AlbumTargetTracks
            WHERE release_id IN ('release-1', 'release-2')
            ORDER BY release_id ASC, track_position ASC
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH AlbumTargetTracks USING INDEX", StringComparison.Ordinal) &&
            detail.Contains("release_id=?", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesCaseInsensitiveRecordingTrackLookup()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT track.release_id, track.track_position, track.recording_id, track.title, track.artist, track.duration_ms, track.isrc
            FROM AlbumTargetTracks AS track
            INNER JOIN AlbumTargets AS album ON album.release_id = track.release_id
            WHERE track.recording_id IS NOT NULL
              AND track.recording_id <> ''
              AND track.recording_id = 'RECORDING-1' COLLATE NOCASE
            ORDER BY album.created_at DESC, track.track_position ASC
            LIMIT 1
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH track USING INDEX idx_album_tracks_recording_nocase", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("SCAN track", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesBoundedRecentAlbumTrackQuery()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT track.release_id, track.track_position, track.recording_id, track.title, track.artist, track.duration_ms, track.isrc
            FROM AlbumTargets AS album INDEXED BY idx_album_targets_created
            INNER JOIN AlbumTargetTracks AS track ON track.release_id = album.release_id
            ORDER BY album.created_at DESC, track.track_position ASC
            LIMIT 50
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SCAN album USING INDEX idx_album_targets_created", StringComparison.Ordinal));
        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH track USING INDEX", StringComparison.Ordinal) &&
            detail.Contains("release_id=?", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("SCAN track", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesBatchedHashEvidenceQueryWithoutTemporarySort()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT *
            FROM HashDb
            WHERE musicbrainz_id IN ('recording-1', 'recording-2')
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH HashDb USING INDEX idx_hashdb_musicbrainz_id", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Constructor_IndexesPeerCapabilityStatsQuery()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN SELECT COUNT(*) FROM Peers WHERE caps > 0";

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var plan = reader.GetString(3);

        Assert.Contains("idx_peers_caps", plan, StringComparison.Ordinal);
    }

    [Fact]
    public void GetStats_ReturnsZeroCountsForEmptyDb()
    {
        // Act
        var stats = service.GetStats();

        // Assert
        Assert.Equal(0, stats.TotalPeers);
        Assert.Equal(0, stats.SlskdnPeers);
        Assert.Equal(0, stats.TotalFlacEntries);
        Assert.Equal(0, stats.HashedFlacEntries);
        Assert.Equal(0, stats.TotalHashEntries);
        Assert.True(stats.DatabaseSizeBytes > 0); // SQLite creates some base tables
    }

    // ========== Peer Management Tests ==========

    [Fact]
    public async Task GetOrCreatePeerAsync_CreatesNewPeer()
    {
        // Act
        var peer = await service.GetOrCreatePeerAsync("testuser");

        // Assert
        Assert.NotNull(peer);
        Assert.Equal("testuser", peer.PeerId);
        Assert.True(peer.LastSeen > 0);
    }

    [Fact]
    public async Task GetOrCreatePeerAsync_ReturnsExistingPeer()
    {
        // Arrange
        await service.GetOrCreatePeerAsync("testuser");

        // Act
        var peer = await service.GetOrCreatePeerAsync("testuser");

        // Assert
        Assert.NotNull(peer);
        Assert.Equal("testuser", peer.PeerId);
    }

    [Fact]
    public async Task GetOrCreatePeerAsync_WithConcurrentSamePeer_ReturnsSinglePeer()
    {
        const string username = "testuser";
        using var start = new ManualResetEventSlim();

        var tasks = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(async () =>
            {
                start.Wait();
                return await service.GetOrCreatePeerAsync(username);
            }))
            .ToArray();

        start.Set();
        var peers = await Task.WhenAll(tasks);

        Assert.All(peers, peer => Assert.Equal(username, peer.PeerId));

        var stats = service.GetStats();
        Assert.Equal(1, stats.TotalPeers);
    }

    [Fact]
    public async Task TouchPeerAsync_NormalizesCreatesAndPreservesCapabilities()
    {
        await service.UpdatePeerCapabilitiesAsync(
            "testuser",
            slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync,
            "slskdn/1.0");
        await using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}"))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Peers SET last_seen = 1, backfills_today = 3, backfill_reset_date = 2 WHERE peer_id = 'testuser'";
            await cmd.ExecuteNonQueryAsync();
        }

        await service.TouchPeerAsync(" testuser ");
        await service.TouchPeerAsync(" new-peer ");

        var peer = Assert.Single(await service.GetSlskdnPeersAsync());
        Assert.Equal("testuser", peer.PeerId);
        Assert.Equal((int)slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync, peer.Caps);
        Assert.Equal("slskdn/1.0", peer.ClientVersion);
        Assert.True(peer.LastSeen > 1);
        Assert.Equal(3, peer.BackfillsToday);
        Assert.Equal(2, peer.BackfillResetDate);
        Assert.Equal("new-peer", (await service.GetOrCreatePeerAsync("new-peer")).PeerId);
    }

    [Fact]
    public async Task UpdatePeerCapabilitiesAsync_CreatesAndPreservesVersionWhenOmitted()
    {
        await service.UpdatePeerCapabilitiesAsync(" testuser ", slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync, " slskdn/1.0 ");
        await service.UpdatePeerCapabilitiesAsync("testuser", slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync);

        var peers = await service.GetSlskdnPeersAsync();
        var peer = Assert.Single(peers);
        Assert.Equal("testuser", peer.PeerId);
        Assert.Equal((int)slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync, peer.Caps);
        Assert.Equal("slskdn/1.0", peer.ClientVersion);
        Assert.NotNull(peer.LastCapCheck);
    }

    // ========== FLAC Inventory Tests ==========

    [Fact]
    public async Task UpsertFlacEntryAsync_InsertsNewEntry()
    {
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        };

        await service.UpsertFlacEntryAsync(entry);
        var stats = service.GetStats();
        Assert.Equal(1, stats.TotalFlacEntries);
    }

    [Fact]
    public async Task UpsertFlacEntryAsync_GeneratesFileId()
    {
        // Arrange
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
        };

        // Act
        await service.UpsertFlacEntryAsync(entry);

        // Assert
        Assert.NotNull(entry.FileId);
        Assert.NotEmpty(entry.FileId);
    }

    [Fact]
    public async Task UpsertFlacEntryAsync_UpdatesExistingEntry()
    {
        // Arrange
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        };
        await service.UpsertFlacEntryAsync(entry);

        // Act - Update with hash
        entry.HashStatusStr = "known";
        entry.HashValue = "abc123";
        await service.UpsertFlacEntryAsync(entry);

        // Assert
        var retrieved = await service.GetFlacEntryAsync(entry.FileId);
        Assert.NotNull(retrieved);
        Assert.Equal("known", retrieved.HashStatusStr);
    }

    [Fact]
    public async Task PassiveFlacBatchHelpers_BoundInventoryAndPeerCommands()
    {
        var entries = Enumerable.Range(1, 201)
            .Select(index => new FlacInventoryEntry
            {
                PeerId = $"peer-{index}",
                Path = $"/music/track-{index}.flac",
                Size = 50_000_000 + index,
                HashStatusStr = "none",
            })
            .ToList();
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        var ingestion = await HashDbService.UpsertFlacEntriesInBatchesAsync(
            conn,
            transaction,
            entries,
            CancellationToken.None);
        var peerCommands = await HashDbService.UpsertPeersInBatchesAsync(
            conn,
            transaction,
            entries.Select(entry => entry.PeerId),
            CancellationToken.None);
        transaction.Commit();

        Assert.Equal(201, ingestion.AffectedRows);
        Assert.Equal(3, ingestion.CommandCount);
        Assert.Equal(1, peerCommands);
        Assert.Equal(201, service.GetStats().TotalFlacEntries);
        await using var peerCountCommand = conn.CreateCommand();
        peerCountCommand.CommandText = "SELECT COUNT(*) FROM Peers";
        Assert.Equal(201L, (long)(await peerCountCommand.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task BackfillFromSearchResponsesAsync_PersistsBoundedFilesAndPeers()
    {
        var responses = Enumerable.Range(1, 100)
            .Select(index => new slskd.Search.Response
            {
                Username = $"peer-{index}",
                Files = new[]
                {
                    new slskd.Search.File
                    {
                        Filename = $"/music/track-{index}.flac",
                        Size = 50_000_000 + index,
                    },
                    new slskd.Search.File
                    {
                        Filename = $"/music/cover-{index}.jpg",
                        Size = 100_000,
                    },
                },
            })
            .ToList();

        var count = await service.BackfillFromSearchResponsesAsync(responses);

        Assert.Equal(100, count);
        Assert.Equal(100, service.GetStats().TotalFlacEntries);
        await service.UpdatePeerCapabilitiesAsync(
            "peer-100",
            slskd.Capabilities.PeerCapabilityFlags.SupportsMeshSync);
        Assert.Contains(await service.GetSlskdnPeersAsync(), peer => peer.PeerId == "peer-100");
    }

    [Fact]
    public async Task GetAlbumTargetsAsync_ReturnsStoredTarget()
    {
        var target = new AlbumTarget
        {
            MusicBrainzReleaseId = "mb:abc",
            Title = "Test Album",
            Artist = "Test Artist",
            Metadata = new ReleaseMetadata { ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) },
            Tracks = new[]
            {
                new TrackTarget
                {
                    MusicBrainzRecordingId = "rec:1",
                    Position = 1,
                    Title = "Track One",
                    Artist = "Test Artist",
                    Duration = TimeSpan.FromSeconds(180),
                }
            }
        };

        await service.UpsertAlbumTargetAsync(target);
        var stored = (await service.GetAlbumTargetsAsync()).Single();

        Assert.Equal("Test Album", stored.Title);
        Assert.Equal("Test Artist", stored.Artist);
        Assert.Equal(target.Metadata.ReleaseDate?.ToString("yyyy-MM-dd"), stored.ReleaseDate);
    }

    [Fact]
    public async Task UpsertAlbumTargetAsync_TrimsStoredAlbumAndTrackFields()
    {
        var target = new AlbumTarget
        {
            MusicBrainzReleaseId = " mb:release-1 ",
            DiscogsReleaseId = " discogs-1 ",
            Title = " Test Album ",
            Artist = " Test Artist ",
            Metadata = new ReleaseMetadata
            {
                Country = " US ",
                Label = " Test Label ",
                Status = " Official ",
            },
            Tracks = new[]
            {
                new TrackTarget
                {
                    MusicBrainzRecordingId = " rec:1 ",
                    Position = 1,
                    Title = " Track One ",
                    Artist = " Test Artist ",
                    Isrc = " US-AAA-01 ",
                }
            }
        };

        await service.UpsertAlbumTargetAsync(target);

        var album = await service.GetAlbumTargetAsync("mb:release-1");
        var track = (await service.GetAlbumTracksAsync("mb:release-1")).Single();

        Assert.NotNull(album);
        Assert.Equal("mb:release-1", album!.ReleaseId);
        Assert.Equal("discogs-1", album.DiscogsReleaseId);
        Assert.Equal("Test Album", album.Title);
        Assert.Equal("Test Artist", album.Artist);
        Assert.Equal("US", album.Country);
        Assert.Equal("Test Label", album.Label);
        Assert.Equal("Official", album.Status);
        Assert.Equal("mb:release-1", track.ReleaseId);
        Assert.Equal("rec:1", track.RecordingId);
        Assert.Equal("Track One", track.Title);
        Assert.Equal("Test Artist", track.Artist);
        Assert.Equal("US-AAA-01", track.Isrc);
    }

    [Fact]
    public async Task GetAlbumTracksAsync_WithReleaseBatch_ReturnsRequestedTracks()
    {
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-1",
            Title = "Album One",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "recording-1", Title = "One" },
            },
        });
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-2",
            Title = "Album Two",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "recording-2", Title = "Two" },
            },
        });

        var tracks = (await service.GetAlbumTracksAsync(new[]
        {
            " release-1 ",
            "release-1",
            "release-2",
            "missing",
        })).ToList();

        Assert.Equal(2, tracks.Count);
        Assert.Equal(new[] { "release-1", "release-2" }, tracks.Select(track => track.ReleaseId));
        Assert.Equal(new[] { "recording-1", "recording-2" }, tracks.Select(track => track.RecordingId));
    }

    [Fact]
    public async Task GetAlbumTrackByRecordingIdAsync_ReturnsNewestCaseInsensitiveMatch()
    {
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-old",
            Title = "Old Album",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "recording-1", Title = "Old Track" },
            },
        });
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-new",
            Title = "New Album",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "recording-1", Title = "New Track" },
            },
        });
        await using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}"))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                UPDATE AlbumTargets
                SET created_at = CASE release_id WHEN 'release-old' THEN 1 ELSE 2 END
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var track = await service.GetAlbumTrackByRecordingIdAsync(" RECORDING-1 ");

        Assert.NotNull(track);
        Assert.Equal("release-new", track!.ReleaseId);
        Assert.Equal("New Track", track.Title);
    }

    [Fact]
    public async Task GetRecentAlbumTracksAsync_AppliesGlobalLimitInAlbumOrder()
    {
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-old",
            Title = "Old Album",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "old-1", Title = "Old One" },
            },
        });
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-new",
            Title = "New Album",
            Artist = "Artist",
            Tracks = new[]
            {
                new TrackTarget { Position = 1, MusicBrainzRecordingId = "new-1", Title = "New One" },
                new TrackTarget { Position = 2, MusicBrainzRecordingId = "new-2", Title = "New Two" },
            },
        });
        await using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}"))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                UPDATE AlbumTargets
                SET created_at = CASE release_id WHEN 'release-old' THEN 1 ELSE 2 END
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var tracks = (await service.GetRecentAlbumTracksAsync(2)).ToList();

        Assert.Equal(2, tracks.Count);
        Assert.All(tracks, track => Assert.Equal("release-new", track.ReleaseId));
        Assert.Equal(new[] { 1, 2 }, tracks.Select(track => track.Position));
    }

    [Fact]
    public async Task UpsertCanonicalStatsAsync_TrimsKeysBeforePersisting()
    {
        var stats = new CanonicalStats
        {
            Id = " stats-1 ",
            MusicBrainzRecordingId = " rec-1 ",
            CodecProfileKey = " FLAC_44100_16_2 ",
            BestVariantId = " variant-1 ",
            VariantCount = 2,
            TotalSeenCount = 3,
            AvgQualityScore = 0.8,
            MaxQualityScore = 0.9,
            PercentTranscodeSuspect = 0.1,
            CodecDistribution = new Dictionary<string, int> { ["FLAC"] = 2 },
            BitrateDistribution = new Dictionary<int, int> { [1000] = 2 },
            SampleRateDistribution = new Dictionary<int, int> { [44100] = 2 },
            CanonicalityScore = 0.85,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        await service.UpsertCanonicalStatsAsync(stats);

        var stored = await service.GetCanonicalStatsAsync("rec-1", "FLAC_44100_16_2");

        Assert.NotNull(stored);
        Assert.Equal("stats-1", stored!.Id);
        Assert.Equal("rec-1", stored.MusicBrainzRecordingId);
        Assert.Equal("FLAC_44100_16_2", stored.CodecProfileKey);
        Assert.Equal("variant-1", stored.BestVariantId);
    }

    [Fact]
    public async Task LookupHashesByRecordingIdAsync_ReturnsMatches()
    {
        var entry = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("test.flac", 123456),
            ByteHash = "abcdef",
            Size = 123456,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };

        await service.StoreHashAsync(entry);
        await service.UpdateHashRecordingIdAsync(entry.FlacKey, "mb:rec1");

        var matches = await service.LookupHashesByRecordingIdAsync("mb:rec1");
        var match = Assert.Single(matches);
        Assert.Equal(entry.FlacKey, match.FlacKey);
    }

    [Fact]
    public async Task LookupHashesByRecordingIdAsync_TrimsInput()
    {
        var entry = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("test.flac", 123456),
            ByteHash = "abcdef",
            Size = 123456,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };

        await service.StoreHashAsync(entry);
        await service.UpdateHashRecordingIdAsync($" {entry.FlacKey} ", " mb:trimmed ", CancellationToken.None);

        var matches = await service.LookupHashesByRecordingIdAsync("  mb:trimmed  ");

        var match = Assert.Single(matches);
        Assert.Equal(entry.FlacKey, match.FlacKey);
    }

    [Fact]
    public async Task LookupHashesByRecordingIdsAsync_ReturnsRequestedMatches()
    {
        var first = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("first.flac", 123),
            ByteHash = "first-hash",
            Size = 123,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };
        var second = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("second.flac", 456),
            ByteHash = "second-hash",
            Size = 456,
            FirstSeenAt = 1,
            LastUpdatedAt = 2,
            SeqId = 2,
            UseCount = 1,
        };
        await service.StoreHashAsync(first);
        await service.StoreHashAsync(second);
        await service.UpdateHashRecordingIdAsync(first.FlacKey, "recording-1");
        await service.UpdateHashRecordingIdAsync(second.FlacKey, "recording-2");

        var matches = (await service.LookupHashesByRecordingIdsAsync(new[]
        {
            " recording-1 ",
            "recording-1",
            "recording-2",
            "missing",
        })).ToList();

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, match => match.FlacKey == first.FlacKey && match.MusicBrainzId == "recording-1");
        Assert.Contains(matches, match => match.FlacKey == second.FlacKey && match.MusicBrainzId == "recording-2");
    }

    [Fact]
    public async Task GetVariantsByRecordingsAsync_ReturnsRequestedVariants()
    {
        var first = new HashDbEntry
        {
            FlacKey = "variant-key-1",
            ByteHash = "variant-hash-1",
            Size = 123,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };
        var second = new HashDbEntry
        {
            FlacKey = "variant-key-2",
            ByteHash = "variant-hash-2",
            Size = 456,
            FirstSeenAt = 1,
            LastUpdatedAt = 2,
            SeqId = 2,
            UseCount = 1,
        };
        await service.StoreHashAsync(first);
        await service.StoreHashAsync(second);
        await service.UpdateHashRecordingIdAsync(first.FlacKey, "recording-1");
        await service.UpdateHashRecordingIdAsync(second.FlacKey, "recording-2");
        await service.UpdateVariantMetadataAsync(first.FlacKey, new AudioVariant
        {
            VariantId = "variant-1",
            MusicBrainzRecordingId = "recording-1",
            QualityScore = 0.8,
        });
        await service.UpdateVariantMetadataAsync(second.FlacKey, new AudioVariant
        {
            VariantId = "variant-2",
            MusicBrainzRecordingId = "recording-2",
            QualityScore = 0.9,
        });

        var variants = await service.GetVariantsByRecordingsAsync(new[]
        {
            " recording-1 ",
            "recording-1",
            "recording-2",
            "missing",
        });

        Assert.Equal(2, variants.Count);
        Assert.Contains(variants, variant => variant.VariantId == "variant-1" && variant.MusicBrainzRecordingId == "recording-1");
        Assert.Contains(variants, variant => variant.VariantId == "variant-2" && variant.MusicBrainzRecordingId == "recording-2");
    }

    [Fact]
    public async Task GetRecordingIdsWithHashesAsync_ReturnsRequestedMatchesInOneBatchShape()
    {
        var first = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("first.flac", 123),
            ByteHash = "first-hash",
            Size = 123,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };
        var second = new HashDbEntry
        {
            FlacKey = HashDbEntry.GenerateFlacKey("second.flac", 456),
            ByteHash = "second-hash",
            Size = 456,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 2,
            UseCount = 1,
        };
        await service.StoreHashAsync(first);
        await service.StoreHashAsync(second);
        await service.UpdateHashRecordingIdAsync(first.FlacKey, "mb:first");
        await service.UpdateHashRecordingIdAsync(second.FlacKey, "MB:SECOND");

        var matches = await service.GetRecordingIdsWithHashesAsync(new[]
        {
            " mb:first ",
            "MB:FIRST",
            "mb:second",
            "mb:missing",
        });

        Assert.Equal(2, matches.Count);
        Assert.Contains("MB:FIRST", matches);
        Assert.Contains("mb:second", matches);
    }

    [Fact]
    public async Task GetDiscographyJobAsync_TrimsStoredAndLookupJobId()
    {
        await service.UpsertDiscographyJobAsync(new slskd.Jobs.DiscographyJob
        {
            JobId = "  job-1  ",
            ArtistId = " artist-1 ",
            ArtistName = " Artist ",
            TargetDirectory = " /tmp/test ",
        });

        var job = await service.GetDiscographyJobAsync(" job-1 ");

        Assert.NotNull(job);
        Assert.Equal("job-1", job!.JobId);
        Assert.Equal("artist-1", job.ArtistId);
        Assert.Equal("Artist", job.ArtistName);
        Assert.Equal("/tmp/test", job.TargetDirectory);
    }

    [Fact]
    public async Task GetDiscographyJobAsync_NormalizesDeserializedJsonPayload()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO DiscographyJobs (job_id, artist_id, artist_name, profile, target_directory, total_releases, completed_releases, failed_releases, status, created_at, json_data)
            VALUES (@job_id, @artist_id, @artist_name, @profile, @target_directory, @total_releases, @completed_releases, @failed_releases, @status, @created_at, @json_data)";
        cmd.Parameters.AddWithValue("@job_id", "job-json");
        cmd.Parameters.AddWithValue("@artist_id", "artist-json");
        cmd.Parameters.AddWithValue("@artist_name", "Artist Json");
        cmd.Parameters.AddWithValue("@profile", "CoreDiscography");
        cmd.Parameters.AddWithValue("@target_directory", "/tmp/json");
        cmd.Parameters.AddWithValue("@total_releases", 0);
        cmd.Parameters.AddWithValue("@completed_releases", 0);
        cmd.Parameters.AddWithValue("@failed_releases", 0);
        cmd.Parameters.AddWithValue("@status", "Pending");
        cmd.Parameters.AddWithValue("@created_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("@json_data", "{\"JobId\":\" job-json \",\"ArtistId\":\" artist-json \",\"ArtistName\":\" Artist Json \",\"TargetDirectory\":\" /tmp/json \",\"Status\":\"Pending\"}");
        await cmd.ExecuteNonQueryAsync();

        var job = await service.GetDiscographyJobAsync("job-json");

        Assert.NotNull(job);
        Assert.Equal("job-json", job!.JobId);
        Assert.Equal("artist-json", job.ArtistId);
        Assert.Equal("Artist Json", job.ArtistName);
        Assert.Equal("/tmp/json", job.TargetDirectory);
    }

    [Fact]
    public async Task GetWarmCacheEntryAndJobFallbacks_ReturnTrimmedValues()
    {
        await service.UpsertWarmCacheEntryAsync(new slskd.HashDb.Models.WarmCacheEntry
        {
            ContentId = "  content:mb:recording:1  ",
            Path = "  /tmp/media.flac  ",
            SizeBytes = 123,
            Pinned = true,
            LastAccessed = 456,
        });

        var warmEntry = await service.GetWarmCacheEntryAsync(" content:mb:recording:1 ");
        Assert.NotNull(warmEntry);
        Assert.Equal("content:mb:recording:1", warmEntry!.ContentId);
        Assert.Equal("/tmp/media.flac", warmEntry.Path);

        await service.UpsertDiscographyJobAsync(new slskd.Jobs.DiscographyJob
        {
            JobId = "  job-row  ",
            ArtistId = " artist-row ",
            ArtistName = " Artist Row ",
            TargetDirectory = " /tmp/row ",
        });
        var discographyJob = await service.GetDiscographyJobAsync(" job-row ");
        Assert.NotNull(discographyJob);
        Assert.Equal("job-row", discographyJob!.JobId);
        Assert.Equal("artist-row", discographyJob.ArtistId);
        Assert.Equal("Artist Row", discographyJob.ArtistName);
        Assert.Equal("/tmp/row", discographyJob.TargetDirectory);

        await service.UpsertLabelCrateJobAsync(new slskd.Jobs.LabelCrateJob
        {
            JobId = "  label-row  ",
            LabelId = " label-row-id ",
            LabelName = " Label Row ",
        });
        var labelJob = await service.GetLabelCrateJobAsync(" label-row ");
        Assert.NotNull(labelJob);
        Assert.Equal("label-row", labelJob!.JobId);
        Assert.Equal("label-row-id", labelJob.LabelId);
        Assert.Equal("Label Row", labelJob.LabelName);
    }

    [Fact]
    public async Task GetLabelCrateJobAsync_NormalizesDeserializedJsonPayload()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO LabelCrateJobs (job_id, label_id, label_name, limit_count, total_releases, completed_releases, failed_releases, status, created_at, json_data)
            VALUES (@job_id, @label_id, @label_name, @limit_count, @total_releases, @completed_releases, @failed_releases, @status, @created_at, @json_data)";
        cmd.Parameters.AddWithValue("@job_id", "label-json");
        cmd.Parameters.AddWithValue("@label_id", "label-id");
        cmd.Parameters.AddWithValue("@label_name", "Label Json");
        cmd.Parameters.AddWithValue("@limit_count", 0);
        cmd.Parameters.AddWithValue("@total_releases", 0);
        cmd.Parameters.AddWithValue("@completed_releases", 0);
        cmd.Parameters.AddWithValue("@failed_releases", 0);
        cmd.Parameters.AddWithValue("@status", "Pending");
        cmd.Parameters.AddWithValue("@created_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("@json_data", "{\"JobId\":\" label-json \",\"LabelId\":\" label-id \",\"LabelName\":\" Label Json \",\"Status\":\"Pending\"}");
        await cmd.ExecuteNonQueryAsync();

        var job = await service.GetLabelCrateJobAsync("label-json");

        Assert.NotNull(job);
        Assert.Equal("label-json", job!.JobId);
        Assert.Equal("label-id", job.LabelId);
        Assert.Equal("Label Json", job.LabelName);
    }

    [Fact]
    public async Task GetLabelCrateJobAsync_TrimsStoredAndLookupJobId()
    {
        await service.UpsertLabelCrateJobAsync(new slskd.Jobs.LabelCrateJob
        {
            JobId = "  label-job  ",
            LabelId = " label-1 ",
            LabelName = " Label Name ",
            ReleaseIds = new List<string> { " rel-1 ", "rel-1", "  " },
        });

        var job = await service.GetLabelCrateJobAsync(" label-job ");

        Assert.NotNull(job);
        Assert.Equal("label-job", job!.JobId);
        Assert.Equal("label-1", job.LabelId);
        Assert.Equal("Label Name", job.LabelName);
        Assert.Single(job.ReleaseIds);
        Assert.Equal("rel-1", job.ReleaseIds[0]);
    }

    [Fact]
    public async Task GetFlacEntriesBySizeAsync_FindsMatchingEntries()
    {
        // Arrange
        var size = 50000000L;
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "user1",
            Path = "/music/test1.flac",
            Size = size,
        });
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "user2",
            Path = "/music/test2.flac",
            Size = size,
        });
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "user3",
            Path = "/music/different.flac",
            Size = 60000000,
        });

        // Act
        var entries = await service.GetFlacEntriesBySizeAsync(size);

        // Assert
        Assert.Equal(2, ((List<FlacInventoryEntry>)entries).Count);
    }

    [Fact]
    public async Task GetUnhashedFlacFilesAsync_ReturnsOnlyUnhashed()
    {
        // Arrange
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "user1",
            Path = "/music/unhashed.flac",
            Size = 50000000,
            HashStatusStr = "none",
        });
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "user2",
            Path = "/music/hashed.flac",
            Size = 50000000,
            HashStatusStr = "known",
            HashValue = "abc123",
        });

        // Act
        var unhashed = await service.GetUnhashedFlacFilesAsync();

        // Assert
        var list = (List<FlacInventoryEntry>)unhashed;
        Assert.Single(list);
        Assert.Equal("user1", list[0].PeerId);
    }

    [Fact]
    public async Task UpdateFlacHashAsync_SetsHashAndStatus()
    {
        // Arrange
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        };
        await service.UpsertFlacEntryAsync(entry);

        // Act
        await service.UpdateFlacHashAsync(entry.FileId, "newhash123", HashSource.LocalScan);

        // Assert
        var updated = await service.GetFlacEntryAsync(entry.FileId);
        Assert.Equal("known", updated.HashStatusStr);
        Assert.Equal("newhash123", updated.HashValue);
    }

    [Fact]
    public async Task UpdateFlacHashAsync_TrimsFileIdAndHashValue()
    {
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        };
        await service.UpsertFlacEntryAsync(entry);

        await service.UpdateFlacHashAsync($" {entry.FileId} ", " trimmedhash ", HashSource.LocalScan);

        var updated = await service.GetFlacEntryAsync(entry.FileId);
        Assert.Equal("known", updated.HashStatusStr);
        Assert.Equal("trimmedhash", updated.HashValue);
    }

    [Fact]
    public async Task MarkFlacHashFailedAsync_SetsFailedStatus()
    {
        // Arrange
        var entry = new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        };
        await service.UpsertFlacEntryAsync(entry);

        // Act
        await service.MarkFlacHashFailedAsync(entry.FileId);

        // Assert
        var updated = await service.GetFlacEntryAsync(entry.FileId);
        Assert.Equal("failed", updated.HashStatusStr);
    }

    [Fact]
    public async Task GetRecordingIdsWithVariantsAsync_TrimsAndDeduplicatesIds()
    {
        var entry1 = new HashDbEntry
        {
            FlacKey = "flac-key-1",
            ByteHash = "hash-1",
            Size = 123,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
        };

        var entry2 = new HashDbEntry
        {
            FlacKey = "flac-key-2",
            ByteHash = "hash-2",
            Size = 456,
            FirstSeenAt = 2,
            LastUpdatedAt = 2,
            SeqId = 2,
            UseCount = 1,
        };

        await service.StoreHashAsync(entry1);
        await service.StoreHashAsync(entry2);
        await service.UpdateHashRecordingIdAsync(entry1.FlacKey, " mb:rec1 ");
        await service.UpdateHashRecordingIdAsync(entry2.FlacKey, "mb:rec1");

        var ids = await service.GetRecordingIdsWithVariantsAsync();

        var id = Assert.Single(ids);
        Assert.Equal("mb:rec1", id);
    }

    [Fact]
    public async Task GetRecordingIdsWithVariantsPageAsync_NormalizesDeduplicatesAndUsesKeysetCursor()
    {
        foreach (var (key, recordingId, sequence) in new[]
        {
            ("flac-page-z", " z-recording ", 1L),
            ("flac-page-a-upper", "A-recording", 2L),
            ("flac-page-a-lower", " a-recording ", 3L),
            ("flac-page-m", "m-recording", 4L),
        })
        {
            await service.StoreHashAsync(new HashDbEntry
            {
                FlacKey = key,
                ByteHash = $"hash-{key}",
                Size = 123,
                FirstSeenAt = sequence,
                LastUpdatedAt = sequence,
                SeqId = sequence,
                UseCount = 1,
            });
            await service.UpdateHashRecordingIdAsync(key, recordingId);
        }

        var firstPage = await service.GetRecordingIdsWithVariantsPageAsync(afterRecordingId: null, limit: 2);
        var secondPage = await service.GetRecordingIdsWithVariantsPageAsync(firstPage[^1], limit: 2);
        var emptyPage = await service.GetRecordingIdsWithVariantsPageAsync(afterRecordingId: null, limit: 0);

        Assert.Equal(new[] { "a-recording", "m-recording" }, firstPage.Select(id => id.ToLowerInvariant()));
        Assert.Equal(new[] { "z-recording" }, secondPage.Select(id => id.ToLowerInvariant()));
        Assert.Empty(emptyPage);
    }

    [Fact]
    public async Task GetCodecProfilesForRecordingAsync_TrimsRecordingId()
    {
        var entry = new HashDbEntry
        {
            FlacKey = "flac-key-codec",
            ByteHash = "hash-codec",
            Size = 321,
            FirstSeenAt = 1,
            LastUpdatedAt = 1,
            SeqId = 1,
            UseCount = 1,
            Codec = "flac",
            SampleRateHz = 44100,
            BitDepth = 16,
            Channels = 2,
        };

        await service.StoreHashAsync(entry);
        await service.UpdateHashRecordingIdAsync(entry.FlacKey, "mb:codec1");

        var profiles = await service.GetCodecProfilesForRecordingAsync(" mb:codec1 ");

        Assert.Single(profiles);
    }

    [Fact]
    public async Task GetLabelCrateReleaseJobsAsync_SkipsBlankReleaseIds()
    {
        await service.UpsertLabelCrateJobAsync(new slskd.Jobs.LabelCrateJob
        {
            JobId = "job-1",
            LabelId = "label-1",
            LabelName = "Label",
            Status = JobStatus.Pending
        });

        await service.UpsertLabelCrateReleaseJobsAsync("job-1", new[]
        {
            new DiscographyReleaseJobStatus { ReleaseId = " rel-1 ", Status = JobStatus.Pending },
            new DiscographyReleaseJobStatus { ReleaseId = "   ", Status = JobStatus.Failed },
        });

        var jobs = await service.GetLabelCrateReleaseJobsAsync(" job-1 ");

        var job = Assert.Single(jobs);
        Assert.Equal("rel-1", job.ReleaseId);
    }

    // ========== Hash Database Tests ==========

    [Fact]
    public async Task StoreHashAsync_StoresAndIncrementsSeqId()
    {
        // Arrange
        var entry = new HashDbEntry
        {
            FlacKey = "testkey",
            ByteHash = "testhash",
            Size = 50000000,
        };

        // Act
        await service.StoreHashAsync(entry);

        // Assert
        Assert.Equal(1, service.CurrentSeqId);
        var stats = service.GetStats();
        Assert.Equal(1, stats.TotalHashEntries);
    }

    [Fact]
    public async Task LookupHashAsync_FindsStoredHash()
    {
        // Arrange
        var entry = new HashDbEntry
        {
            FlacKey = "testkey",
            ByteHash = "testhash",
            Size = 50000000,
        };
        await service.StoreHashAsync(entry);

        // Act
        var found = await service.LookupHashAsync("testkey");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("testhash", found.ByteHash);
        Assert.Equal(50000000, found.Size);
    }

    [Fact]
    public async Task LookupHashAsync_ReturnsNullForMissingKey()
    {
        // Act
        var found = await service.LookupHashAsync("nonexistent");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public async Task LookupHashesBySizeAsync_FindsMatchingHashes()
    {
        // Arrange
        var size = 50000000L;
        await service.StoreHashAsync(new HashDbEntry
        {
            FlacKey = "key1",
            ByteHash = "hash1",
            Size = size,
        });
        await service.StoreHashAsync(new HashDbEntry
        {
            FlacKey = "key2",
            ByteHash = "hash2",
            Size = size,
        });

        // Act
        var hashes = await service.LookupHashesBySizeAsync(size);

        // Assert
        Assert.Equal(2, ((List<HashDbEntry>)hashes).Count);
    }

    [Fact]
    public async Task StoreHashFromVerificationAsync_CreatesCorrectKey()
    {
        // Arrange - hash needs to be at least 16 chars for logging substring
        var hash = "0123456789abcdef0123456789abcdef";

        // Act
        await service.StoreHashFromVerificationAsync("/music/test.flac", 50000000, hash);

        // Assert
        var expectedKey = HashDbEntry.GenerateFlacKey("/music/test.flac", 50000000);
        var found = await service.LookupHashAsync(expectedKey);
        Assert.NotNull(found);
        Assert.Equal(hash, found.ByteHash);
    }

    [Fact]
    public async Task DownloadCompleteAsync_SkipsNonAudioSidecar()
    {
        var sidecar = Path.Combine(testDir, "booklet.pdf");
        await File.WriteAllBytesAsync(sidecar, new byte[32769]);

        await InvokeDownloadCompleteAsync(new DownloadFileCompleteEvent
        {
            LocalFilename = sidecar,
            RemoteFilename = @"Album\booklet.pdf",
            Transfer = new slskd.Transfers.Transfer
            {
                Size = 32769,
            },
        });

        var stats = service.GetStats();
        Assert.Equal(0, stats.TotalHashEntries);
    }

    [Fact]
    public async Task IncrementHashUseCountAsync_IncrementsCount()
    {
        // Arrange
        var entry = new HashDbEntry
        {
            FlacKey = "testkey",
            ByteHash = "testhash",
            Size = 50000000,
        };
        await service.StoreHashAsync(entry);

        // Act
        await service.IncrementHashUseCountAsync("testkey");
        await service.IncrementHashUseCountAsync("testkey");

        // Assert
        var found = await service.LookupHashAsync("testkey");
        Assert.Equal(3, found.UseCount); // Initial 1 + 2 increments
    }

    private async Task InvokeDownloadCompleteAsync(DownloadFileCompleteEvent evt)
    {
        var method = typeof(HashDbService).GetMethod(
            "OnDownloadCompleteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var task = Assert.IsAssignableFrom<Task>(method?.Invoke(service, new object[] { evt }));
        await task;
    }

    [Fact]
    public async Task IncrementHashUseCountAsync_TrimsKeyAndIgnoresBlank()
    {
        await service.StoreHashAsync(new HashDbEntry
        {
            FlacKey = "trim-key",
            ByteHash = "trim-hash",
            Size = 123,
        });

        await service.IncrementHashUseCountAsync(" trim-key ");
        await service.IncrementHashUseCountAsync("   ");

        var found = await service.LookupHashAsync("trim-key");
        Assert.Equal(2, found!.UseCount);
    }

    // ========== Mesh Sync Tests ==========

    [Fact]
    public async Task GetEntriesSinceSeqAsync_ReturnsEntriesAfterSeq()
    {
        // Arrange
        await service.StoreHashAsync(new HashDbEntry { FlacKey = "key1", ByteHash = "hash1", Size = 100 });
        await service.StoreHashAsync(new HashDbEntry { FlacKey = "key2", ByteHash = "hash2", Size = 200 });
        await service.StoreHashAsync(new HashDbEntry { FlacKey = "key3", ByteHash = "hash3", Size = 300 });

        // Act
        var entries = await service.GetEntriesSinceSeqAsync(1);

        // Assert
        var list = (List<HashDbEntry>)entries;
        Assert.Equal(2, list.Count);
        Assert.Contains(list, e => e.FlacKey == "key2");
        Assert.Contains(list, e => e.FlacKey == "key3");
    }

    [Fact]
    public async Task MergeEntriesFromMeshAsync_MergesNewEntries()
    {
        // Arrange
        var entries = new List<HashDbEntry>
        {
            new HashDbEntry { FlacKey = "mesh1", ByteHash = "meshhash1", Size = 100 },
            new HashDbEntry { FlacKey = "mesh2", ByteHash = "meshhash2", Size = 200 },
        };

        // Act
        var merged = await service.MergeEntriesFromMeshAsync(entries);

        // Assert
        Assert.Equal(2, merged);
        Assert.NotNull(await service.LookupHashAsync("mesh1"));
        Assert.NotNull(await service.LookupHashAsync("mesh2"));
    }

    [Fact]
    public async Task MergeEntriesFromMeshAsync_SkipsExistingEntries()
    {
        // Arrange
        await service.StoreHashAsync(new HashDbEntry { FlacKey = "existing", ByteHash = "localhash", Size = 100 });

        var entries = new List<HashDbEntry>
        {
            new HashDbEntry { FlacKey = "existing", ByteHash = "localhash", Size = 100 }, // Same hash
            new HashDbEntry { FlacKey = "new", ByteHash = "newhash", Size = 200 },
        };

        // Act
        var merged = await service.MergeEntriesFromMeshAsync(entries);

        // Assert
        Assert.Equal(1, merged); // Only the new one should be merged
    }

    [Fact]
    public async Task UpdatePeerLastSeqSeenAsync_TracksSeqPerPeer()
    {
        // Act
        await service.UpdatePeerLastSeqSeenAsync("peer1", 100);
        await service.UpdatePeerLastSeqSeenAsync("peer2", 200);

        // Assert
        Assert.Equal(100, await service.GetPeerLastSeqSeenAsync("peer1"));
        Assert.Equal(200, await service.GetPeerLastSeqSeenAsync("peer2"));
    }

    [Fact]
    public async Task UpdatePeerLastSeqSeenAsync_TrimsPeerIdAndBlankLookupsReturnZero()
    {
        await service.UpdatePeerLastSeqSeenAsync(" peer-trim ", 321);

        Assert.Equal(321, await service.GetPeerLastSeqSeenAsync(" peer-trim "));
        Assert.Equal(0, await service.GetPeerLastSeqSeenAsync("   "));
    }

    // ========== Backfill Tests ==========

    [Fact]
    public async Task GetBackfillCandidatesAsync_ReturnsUnhashedFiles()
    {
        // Arrange
        await service.GetOrCreatePeerAsync("testuser");
        await service.UpsertFlacEntryAsync(new FlacInventoryEntry
        {
            PeerId = "testuser",
            Path = "/music/test.flac",
            Size = 50000000,
            HashStatusStr = "none",
        });

        // Act
        var candidates = await service.GetBackfillCandidatesAsync();

        // Assert
        Assert.Single(candidates);
    }

    [Fact]
    public async Task GetBackfillCandidatesAsync_NonPositiveLimitReturnsEmpty()
    {
        var candidates = await service.GetBackfillCandidatesAsync(0);

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task IncrementPeerBackfillCountAsync_IncrementsCount()
    {
        // Arrange
        await service.GetOrCreatePeerAsync("testuser");

        // Act
        await service.IncrementPeerBackfillCountAsync("testuser");
        await service.IncrementPeerBackfillCountAsync("testuser");

        // Assert
        var count = await service.GetPeerBackfillCountTodayAsync("testuser");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task IncrementPeerBackfillCountAsync_TrimsPeerId()
    {
        await service.GetOrCreatePeerAsync("testuser");

        await service.IncrementPeerBackfillCountAsync(" testuser ");

        var count = await service.GetPeerBackfillCountTodayAsync(" testuser ");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetPeerBackfillCountsTodayAsync_ReturnsAllRequestedCountsInOneSnapshot()
    {
        await service.GetOrCreatePeerAsync("alice");
        await service.GetOrCreatePeerAsync("bob");
        await service.IncrementPeerBackfillCountAsync("alice");
        await service.IncrementPeerBackfillCountAsync("alice");
        await service.IncrementPeerBackfillCountAsync("bob");

        var counts = await service.GetPeerBackfillCountsTodayAsync(
            new[] { " alice ", "BOB", "missing", "alice" });

        Assert.Equal(2, counts["alice"]);
        Assert.Equal(1, counts["bob"]);
        Assert.False(counts.ContainsKey("missing"));
        Assert.Equal(2, counts.Count);
    }

    [Fact]
    public async Task GetLabelPresenceAndReleaseIds_NormalizeTrimAndDeduplicate()
    {
        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = " release-1 ",
            DiscogsReleaseId = " discogs-1 ",
            Title = "Album 1",
            Artist = "Artist 1",
            Metadata = new ReleaseMetadata
            {
                Label = " Label One ",
            },
        });

        await service.UpsertAlbumTargetAsync(new AlbumTarget
        {
            MusicBrainzReleaseId = "release-1",
            DiscogsReleaseId = "discogs-1",
            Title = "Album 1 Dup",
            Artist = "Artist 1",
            Metadata = new ReleaseMetadata
            {
                Label = "label one",
            },
        });

        var labels = await service.GetLabelPresenceAsync();
        var releasesByLabel = await service.GetReleaseIdsByLabelAsync("  LABEL ONE  ", 10);

        Assert.Single(labels);
        Assert.Equal("label one", labels[0].Label, ignoreCase: true);
        Assert.Single(releasesByLabel);
        Assert.Equal("release-1", releasesByLabel[0]);
    }

    [Fact]
    public async Task GetEntriesSinceSeqAsync_NonPositiveLimitReturnsEmpty()
    {
        await service.StoreHashAsync(new HashDbEntry { FlacKey = "seq-key", ByteHash = "seq-hash", Size = 1 });

        var entries = await service.GetEntriesSinceSeqAsync(0, 0);

        Assert.Empty(entries);
    }

    [Fact]
    public async Task PeerMetrics_NormalizePeerIdOnWriteAndRead()
    {
        await service.UpsertPeerMetricsAsync(new slskd.Transfers.MultiSource.Metrics.PeerPerformanceMetrics
        {
            PeerId = " peer-metrics ",
            Source = slskd.Transfers.MultiSource.Metrics.PeerSource.Soulseek,
            FirstSeen = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
        });

        var metric = await service.GetPeerMetricsAsync(" peer-metrics ");
        var all = await service.GetAllPeerMetricsAsync();

        Assert.NotNull(metric);
        Assert.Equal("peer-metrics", metric!.PeerId);
        var single = Assert.Single(all);
        Assert.Equal("peer-metrics", single.PeerId);
    }

    [Fact]
    public async Task LibraryHealthDashboard_AggregatesBeyondDefaultPageAndBoundsDetails()
    {
        for (var index = 0; index < 157; index++)
        {
            var inSelectedLibrary = index < 150;
            await service.InsertLibraryIssueAsync(new LibraryIssue
            {
                IssueId = $"issue-{index:D3}",
                Type = index % 2 == 0 ? LibraryIssueType.CorruptedFile : LibraryIssueType.MissingMetadata,
                Severity = index % 3 == 0 ? LibraryIssueSeverity.High : LibraryIssueSeverity.Medium,
                FilePath = inSelectedLibrary ? $"/music/track-{index:D3}.flac" : $"/other/track-{index:D3}.flac",
                Artist = index % 2 == 0 ? "Artist A" : "Artist B",
                Album = "Fixture Album",
                MusicBrainzReleaseId = "fixture-release",
                Status = index < 120
                    ? LibraryIssueStatus.Detected
                    : index < 140
                        ? LibraryIssueStatus.Resolved
                        : LibraryIssueStatus.Ignored,
                DetectedAt = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000 + index),
                Metadata = new Dictionary<string, object>
                {
                    ["codec"] = index % 2 == 0 ? "flac" : "mp3",
                    ["payload"] = new string('x', 256),
                    ["transcode_suspect"] = index % 10 == 0,
                },
            });
        }

        var dashboard = await service.GetLibraryHealthDashboardAsync("/music", artistLimit: 1, issueLimit: 25);

        Assert.Equal(150, dashboard.Summary.TotalIssues);
        Assert.Equal(120, dashboard.Summary.IssuesOpen);
        Assert.Equal(20, dashboard.Summary.IssuesResolved);
        Assert.Equal(150, dashboard.IssuesByType.Sum(group => group.Count));
        var artist = Assert.Single(dashboard.IssuesByArtist);
        Assert.Equal("Artist A", artist.Artist);
        Assert.Equal(75, artist.Count);
        Assert.Equal(25, dashboard.Issues.Count);
        Assert.Equal("issue-149", dashboard.Issues[0].IssueId);
        Assert.All(dashboard.Issues, issue => Assert.StartsWith("/music/", issue.FilePath, StringComparison.Ordinal));

        var releases = await service.GetLibraryIssueReleaseSummariesAsync("/music", limit: 2);
        Assert.Equal(2, releases.Count);
        Assert.All(releases, release => Assert.Equal(75, release.Count));
        Assert.Equal(150, releases.Sum(release => release.ByType.Values.Sum()));

        var codecs = await service.GetLibraryIssueCodecSummariesAsync("/music");
        Assert.Equal(150, codecs.Sum(group => group.Count));
        Assert.Contains(codecs, group => group.Codec == "FLAC" && group.Count == 75);
        Assert.Contains(codecs, group => group.Codec == "MP3" && group.Count == 75);
    }

    [Fact]
    public async Task LibraryHealthIssuePage_ReturnsFullFilteredCountWithBoundedRows()
    {
        for (var index = 0; index < 120; index++)
        {
            await service.InsertLibraryIssueAsync(new LibraryIssue
            {
                IssueId = $"page-issue-{index:D3}",
                FilePath = $"/music/track-{index:D3}.flac",
                DetectedAt = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000 + index),
            });
        }

        var page = await service.GetLibraryIssuePageAsync(new LibraryHealthIssueFilter
        {
            LibraryPath = "/music",
            Limit = 20,
        });

        Assert.Equal(120, page.TotalCount);
        Assert.Equal(20, page.Issues.Count);
    }

    [Fact]
    public async Task Constructor_IndexesLibraryHealthRecentIssueQuery()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN SELECT * FROM LibraryHealthIssues WHERE file_path LIKE @path ORDER BY detected_at DESC LIMIT 100";
        cmd.Parameters.AddWithValue("@path", "/music/%");

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.Contains("idx_issues_detected", reader.GetString(3), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Constructor_IndexesRemediationJobIssueQueryWithoutTemporarySort()
    {
        await using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(testDir, "hashdb.db")}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            EXPLAIN QUERY PLAN
            SELECT *
            FROM LibraryHealthIssues
            WHERE remediation_job_id IS NOT NULL
              AND remediation_job_id <> ''
              AND remediation_job_id = 'job-1'
              AND status = 'fixing'
            ORDER BY detected_at DESC
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        var plan = new List<string>();
        while (await reader.ReadAsync())
        {
            plan.Add(reader.GetString(3));
        }

        Assert.Contains(plan, detail =>
            detail.Contains("SEARCH LibraryHealthIssues USING INDEX idx_issues_remediation_status", StringComparison.Ordinal));
        Assert.DoesNotContain(plan, detail => detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LibraryHealthRemediationBatch_RoundTripsLinkAndStatus()
    {
        for (var index = 1; index <= 3; index++)
        {
            await service.InsertLibraryIssueAsync(new LibraryIssue
            {
                IssueId = $"remediation-{index}",
                FilePath = $"/music/{index}.flac",
                Status = LibraryIssueStatus.Detected,
                DetectedAt = DateTimeOffset.FromUnixTimeSeconds(index),
            });
        }

        var requested = await service.GetLibraryIssuesByIdsAsync(new[]
        {
            "remediation-2",
            " remediation-1 ",
            "remediation-2",
            "missing",
        });
        var linkedCount = await service.UpdateLibraryIssueStatusesAsync(
            requested.Select(issue => issue.IssueId),
            LibraryIssueStatus.Fixing,
            " job-1 ");
        var linked = await service.GetLibraryIssuesByRemediationJobAsync(" job-1 ", LibraryIssueStatus.Fixing);
        var resolvedCount = await service.UpdateLibraryIssueStatusesAsync(
            linked.Select(issue => issue.IssueId),
            LibraryIssueStatus.Resolved);
        var resolved = await service.GetLibraryIssuesByIdsAsync(linked.Select(issue => issue.IssueId));

        Assert.Equal(2, requested.Count);
        Assert.Equal(new[] { "remediation-2", "remediation-1" }, requested.Select(issue => issue.IssueId));
        Assert.Equal(2, linkedCount);
        Assert.Equal(2, linked.Count);
        Assert.All(linked, issue =>
        {
            Assert.Equal("job-1", issue.RemediationJobId);
            Assert.Equal(LibraryIssueStatus.Fixing, issue.Status);
        });
        Assert.Equal(2, resolvedCount);
        Assert.All(resolved, issue =>
        {
            Assert.Equal("job-1", issue.RemediationJobId);
            Assert.Equal(LibraryIssueStatus.Resolved, issue.Status);
            Assert.NotNull(issue.ResolvedAt);
        });
        var untouched = Assert.Single(await service.GetLibraryIssuesByIdsAsync(new[] { "remediation-3" }));
        Assert.Equal(LibraryIssueStatus.Detected, untouched.Status);
        Assert.Empty(untouched.RemediationJobId);
    }

    // ========== FlacInventoryEntry Tests ==========

    [Fact]
    public void FlacInventoryEntry_GenerateFileId_IsConsistent()
    {
        // Act
        var id1 = FlacInventoryEntry.GenerateFileId("user", "/path/file.flac", 12345);
        var id2 = FlacInventoryEntry.GenerateFileId("user", "/path/file.flac", 12345);

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void FlacInventoryEntry_GenerateFileId_DifferentInputsProduceDifferentIds()
    {
        // Act
        var id1 = FlacInventoryEntry.GenerateFileId("user1", "/path/file.flac", 12345);
        var id2 = FlacInventoryEntry.GenerateFileId("user2", "/path/file.flac", 12345);
        var id3 = FlacInventoryEntry.GenerateFileId("user1", "/path/other.flac", 12345);
        var id4 = FlacInventoryEntry.GenerateFileId("user1", "/path/file.flac", 99999);

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.NotEqual(id1, id4);
    }

    // ========== HashDbEntry Tests ==========

    [Fact]
    public void HashDbEntry_GenerateFlacKey_IsConsistent()
    {
        // Act
        var key1 = HashDbEntry.GenerateFlacKey("/path/file.flac", 12345);
        var key2 = HashDbEntry.GenerateFlacKey("/path/file.flac", 12345);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void HashDbEntry_PackUnpackMetaFlags_RoundTrips()
    {
        // Arrange
        var sampleRate = 44100;
        var channels = 2;
        var bitDepth = 16;

        // Act
        var packed = HashDbEntry.PackMetaFlags(sampleRate, channels, bitDepth);
        var (unpackedRate, unpackedChannels, unpackedDepth) = HashDbEntry.UnpackMetaFlags(packed);

        // Assert
        Assert.Equal(sampleRate, unpackedRate);
        Assert.Equal(channels, unpackedChannels);
        Assert.Equal(bitDepth, unpackedDepth);
    }
}
