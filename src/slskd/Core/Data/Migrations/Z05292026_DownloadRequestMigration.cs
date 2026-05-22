// <copyright file="Z05292026_DownloadRequestMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds the DownloadRequests table and RequestId column on Transfers,
///     backfilling one DownloadRequest per existing non-removed download transfer.
///     Removed/superseded transfers covering the same (BatchId, Filename) are collapsed
///     into a single request as attempt history.
/// </summary>
public class Z05292026_DownloadRequestMigration : IMigration
{
    public Z05292026_DownloadRequestMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Transfers];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05292026_DownloadRequestMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        var schema = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var hasRequestsTable = schema.ContainsKey("DownloadRequests");
        var hasRequestIdColumn = schema.TryGetValue("Transfers", out var transferCols)
            && transferCols.Any(c => c.Name.Equals("RequestId", StringComparison.OrdinalIgnoreCase));

        return !hasRequestsTable || !hasRequestIdColumn;
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05292026_DownloadRequestMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        CreateDownloadRequestsTable(connection);
        AddRequestIdColumn(connection);
        Backfill(connection);
    }

    private void CreateDownloadRequestsTable(SqliteConnection connection)
    {
        using var cmd = new SqliteCommand(
            @"CREATE TABLE IF NOT EXISTS DownloadRequests (
                Id TEXT PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL DEFAULT '',
                OriginalFilename TEXT NOT NULL DEFAULT '',
                Size INTEGER NULL,
                BatchId TEXT NULL,
                DestinationDirectory TEXT NULL,
                State TEXT NOT NULL DEFAULT 'Active',
                StateDescription TEXT NULL,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT NULL,
                WishlistItemId TEXT NULL,
                SearchResponseId TEXT NULL,
                BitRate INTEGER NULL,
                SampleRate INTEGER NULL,
                BitDepth INTEGER NULL,
                Length INTEGER NULL,
                Artist TEXT NULL,
                Album TEXT NULL,
                Title TEXT NULL,
                TrackNumber INTEGER NULL,
                Year INTEGER NULL
            )",
            connection);
        cmd.ExecuteNonQuery();

        using var idxState = new SqliteCommand("CREATE INDEX IF NOT EXISTS IDX_DownloadRequests_State ON DownloadRequests(State)", connection);
        idxState.ExecuteNonQuery();

        using var idxWish = new SqliteCommand("CREATE INDEX IF NOT EXISTS IDX_DownloadRequests_WishlistItemId ON DownloadRequests(WishlistItemId)", connection);
        idxWish.ExecuteNonQuery();

        Log.Information("> Created DownloadRequests table");
    }

    private void AddRequestIdColumn(SqliteConnection connection)
    {
        var schema = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transferCols = schema.TryGetValue("Transfers", out var cols) ? cols : Enumerable.Empty<SchemaInspector.ColumnInfo>();

        if (!transferCols.Any(c => c.Name.Equals("RequestId", StringComparison.OrdinalIgnoreCase)))
        {
            using var cmd = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN RequestId TEXT NULL", connection);
            cmd.ExecuteNonQuery();
            Log.Information("> Added RequestId column to Transfers");
        }

        using var idx = new SqliteCommand("CREATE INDEX IF NOT EXISTS IDX_Transfers_RequestId ON Transfers(RequestId)", connection);
        idx.ExecuteNonQuery();
    }

    private void Backfill(SqliteConnection connection)
    {
        // Collapse all download Transfers that share (BatchId, Filename) into one DownloadRequest.
        // Use the earliest RequestedAt as CreatedAt and copy metadata from the most recent record
        // that actually has it.

        using var tx = connection.BeginTransaction();

        // Identify groups by (BatchId, Filename) where Direction = 'Download'.
        // SQLite treats NULL != NULL in GROUP BY, so coalesce to a literal for grouping.
        using var selectGroups = new SqliteCommand(
            @"SELECT
                COALESCE(BatchId, '') AS GroupBatchId,
                Filename,
                MIN(RequestedAt) AS FirstRequestedAt,
                MAX(BatchId) AS BatchId,
                MAX(DestinationDirectory) AS DestinationDirectory,
                MAX(Size) AS Size,
                MAX(BitRate) AS BitRate,
                MAX(SampleRate) AS SampleRate,
                MAX(BitDepth) AS BitDepth,
                MAX(Length) AS Length,
                MAX(Artist) AS Artist,
                MAX(Album) AS Album,
                MAX(Title) AS Title,
                MAX(TrackNumber) AS TrackNumber,
                MAX(Year) AS Year,
                SUM(CASE WHEN State LIKE '%Completed%Succeeded%' OR State LIKE '%Succeeded%Completed%' THEN 1 ELSE 0 END) AS SucceededCount,
                COUNT(*) AS TotalCount
              FROM Transfers
              WHERE Direction = 'Download' AND (RequestId IS NULL OR RequestId = '')
              GROUP BY GroupBatchId, Filename",
            connection,
            tx);

        var groups = new List<BackfillGroup>();
        using (var reader = selectGroups.ExecuteReader())
        {
            while (reader.Read())
            {
                groups.Add(new BackfillGroup
                {
                    GroupBatchId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Filename = reader.GetString(1),
                    FirstRequestedAt = reader.IsDBNull(2) ? DateTime.UtcNow.ToString("o") : reader.GetString(2),
                    BatchId = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DestinationDirectory = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Size = reader.IsDBNull(5) ? (long?)null : reader.GetInt64(5),
                    BitRate = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                    SampleRate = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                    BitDepth = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                    Length = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                    Artist = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Album = reader.IsDBNull(11) ? null : reader.GetString(11),
                    Title = reader.IsDBNull(12) ? null : reader.GetString(12),
                    TrackNumber = reader.IsDBNull(13) ? (int?)null : reader.GetInt32(13),
                    Year = reader.IsDBNull(14) ? (int?)null : reader.GetInt32(14),
                    SucceededCount = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
                });
            }
        }

        var created = 0;
        foreach (var g in groups)
        {
            var requestId = Guid.NewGuid().ToString();
            var name = System.IO.Path.GetFileName(g.Filename.Replace('\\', '/').TrimEnd('/'));
            var state = g.SucceededCount > 0 ? "Completed" : "Active";

            using (var insert = new SqliteCommand(
                @"INSERT INTO DownloadRequests
                  (Id, Name, OriginalFilename, Size, BatchId, DestinationDirectory, State, StateDescription, CreatedAt,
                   BitRate, SampleRate, BitDepth, Length, Artist, Album, Title, TrackNumber, Year)
                  VALUES
                  ($id, $name, $orig, $size, $batch, $dest, $state, $state, $created,
                   $bitrate, $samplerate, $bitdepth, $length, $artist, $album, $title, $track, $year)",
                connection,
                tx))
            {
                insert.Parameters.AddWithValue("$id", requestId);
                insert.Parameters.AddWithValue("$name", string.IsNullOrEmpty(name) ? g.Filename : name);
                insert.Parameters.AddWithValue("$orig", g.Filename);
                insert.Parameters.AddWithValue("$size", (object?)g.Size ?? DBNull.Value);
                insert.Parameters.AddWithValue("$batch", (object?)g.BatchId ?? DBNull.Value);
                insert.Parameters.AddWithValue("$dest", (object?)g.DestinationDirectory ?? DBNull.Value);
                insert.Parameters.AddWithValue("$state", state);
                insert.Parameters.AddWithValue("$created", g.FirstRequestedAt);
                insert.Parameters.AddWithValue("$bitrate", (object?)g.BitRate ?? DBNull.Value);
                insert.Parameters.AddWithValue("$samplerate", (object?)g.SampleRate ?? DBNull.Value);
                insert.Parameters.AddWithValue("$bitdepth", (object?)g.BitDepth ?? DBNull.Value);
                insert.Parameters.AddWithValue("$length", (object?)g.Length ?? DBNull.Value);
                insert.Parameters.AddWithValue("$artist", (object?)g.Artist ?? DBNull.Value);
                insert.Parameters.AddWithValue("$album", (object?)g.Album ?? DBNull.Value);
                insert.Parameters.AddWithValue("$title", (object?)g.Title ?? DBNull.Value);
                insert.Parameters.AddWithValue("$track", (object?)g.TrackNumber ?? DBNull.Value);
                insert.Parameters.AddWithValue("$year", (object?)g.Year ?? DBNull.Value);
                insert.ExecuteNonQuery();
            }

            using (var update = new SqliteCommand(
                g.BatchId == null
                    ? "UPDATE Transfers SET RequestId = $rid WHERE Direction = 'Download' AND BatchId IS NULL AND Filename = $fn AND (RequestId IS NULL OR RequestId = '')"
                    : "UPDATE Transfers SET RequestId = $rid WHERE Direction = 'Download' AND BatchId = $batch AND Filename = $fn AND (RequestId IS NULL OR RequestId = '')",
                connection,
                tx))
            {
                update.Parameters.AddWithValue("$rid", requestId);
                update.Parameters.AddWithValue("$fn", g.Filename);
                if (g.BatchId != null)
                {
                    update.Parameters.AddWithValue("$batch", g.BatchId);
                }

                update.ExecuteNonQuery();
            }

            created++;
        }

        tx.Commit();

        Log.Information("> Backfilled {Count} DownloadRequest(s) from existing Transfers", created);
    }

    private sealed class BackfillGroup
    {
        public string GroupBatchId { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string FirstRequestedAt { get; set; } = string.Empty;
        public string? BatchId { get; set; }
        public string? DestinationDirectory { get; set; }
        public long? Size { get; set; }
        public int? BitRate { get; set; }
        public int? SampleRate { get; set; }
        public int? BitDepth { get; set; }
        public int? Length { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Title { get; set; }
        public int? TrackNumber { get; set; }
        public int? Year { get; set; }
        public int SucceededCount { get; set; }
    }
}
