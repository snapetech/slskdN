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
        // Strategy: stage all distinct (BatchId, Filename) groups in a TEMP table with pre-generated
        // request ids and computed display name, then do a single bulk INSERT into DownloadRequests and
        // a single bulk UPDATE on Transfers driven off the temp table. A throwaway index on
        // Transfers(Filename, BatchId, Direction) makes the correlated subquery a PK lookup.
        // On a database with ~134k transfer rows and ~70k groups this finishes in a few seconds;
        // the prior per-group loop took 15-25 minutes because each UPDATE scanned the full Transfers table.
        using var tx = connection.BeginTransaction();

        using (var create = new SqliteCommand(
            @"CREATE TEMP TABLE backfill_groups (
                request_id TEXT NOT NULL,
                name TEXT NOT NULL,
                original_filename TEXT NOT NULL,
                group_batch TEXT NOT NULL,
                batch_id TEXT NULL,
                destination_directory TEXT NULL,
                size INTEGER NULL,
                state TEXT NOT NULL,
                first_requested_at TEXT NOT NULL,
                bit_rate INTEGER NULL,
                sample_rate INTEGER NULL,
                bit_depth INTEGER NULL,
                length INTEGER NULL,
                artist TEXT NULL,
                album TEXT NULL,
                title TEXT NULL,
                track_number INTEGER NULL,
                year INTEGER NULL,
                PRIMARY KEY (group_batch, original_filename)
            )",
            connection,
            tx))
        {
            create.ExecuteNonQuery();
        }

        var staged = StageGroups(connection, tx);
        Log.Information("> Staged {Count} backfill group(s)", staged);

        if (staged == 0)
        {
            tx.Commit();
            return;
        }

        // Helper index for the bulk UPDATE join; dropped at end of the migration.
        using (var idx = new SqliteCommand(
            "CREATE INDEX IF NOT EXISTS idx_z05292026_transfers_backfill ON Transfers(Filename, BatchId, Direction)",
            connection,
            tx))
        {
            idx.ExecuteNonQuery();
        }

        int inserted;
        using (var insertRequests = new SqliteCommand(
            @"INSERT INTO DownloadRequests
                (Id, Name, OriginalFilename, Size, BatchId, DestinationDirectory,
                 State, StateDescription, CreatedAt,
                 BitRate, SampleRate, BitDepth, Length,
                 Artist, Album, Title, TrackNumber, Year)
              SELECT
                request_id, name, original_filename, size, batch_id, destination_directory,
                state, state, first_requested_at,
                bit_rate, sample_rate, bit_depth, length,
                artist, album, title, track_number, year
              FROM backfill_groups",
            connection,
            tx))
        {
            inserted = insertRequests.ExecuteNonQuery();
        }

        int updated;
        using (var updateTransfers = new SqliteCommand(
            @"UPDATE Transfers
              SET RequestId = (
                  SELECT request_id FROM backfill_groups
                  WHERE COALESCE(Transfers.BatchId, '') = backfill_groups.group_batch
                    AND Transfers.Filename = backfill_groups.original_filename
              )
              WHERE Direction = 'Download' AND (RequestId IS NULL OR RequestId = '')",
            connection,
            tx))
        {
            updated = updateTransfers.ExecuteNonQuery();
        }

        using (var dropIdx = new SqliteCommand(
            "DROP INDEX IF EXISTS idx_z05292026_transfers_backfill",
            connection,
            tx))
        {
            dropIdx.ExecuteNonQuery();
        }

        tx.Commit();

        Log.Information(
            "> Backfilled {RequestCount} DownloadRequest row(s) and stamped {TransferCount} Transfer row(s)",
            inserted,
            updated);
    }

    private int StageGroups(SqliteConnection connection, SqliteTransaction tx)
    {
        using var select = new SqliteCommand(
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
                SUM(CASE WHEN State LIKE '%Completed%Succeeded%' OR State LIKE '%Succeeded%Completed%' THEN 1 ELSE 0 END) AS SucceededCount
              FROM Transfers
              WHERE Direction = 'Download' AND (RequestId IS NULL OR RequestId = '')
              GROUP BY GroupBatchId, Filename",
            connection,
            tx);

        using var insert = new SqliteCommand(
            @"INSERT INTO backfill_groups
                (request_id, name, original_filename, group_batch, batch_id, destination_directory,
                 size, state, first_requested_at,
                 bit_rate, sample_rate, bit_depth, length,
                 artist, album, title, track_number, year)
              VALUES
                ($rid, $name, $orig, $gb, $batch, $dest,
                 $size, $state, $created,
                 $br, $sr, $bd, $len,
                 $artist, $album, $title, $track, $year)",
            connection,
            tx);

        var pRid = insert.Parameters.Add("$rid", Microsoft.Data.Sqlite.SqliteType.Text);
        var pName = insert.Parameters.Add("$name", Microsoft.Data.Sqlite.SqliteType.Text);
        var pOrig = insert.Parameters.Add("$orig", Microsoft.Data.Sqlite.SqliteType.Text);
        var pGb = insert.Parameters.Add("$gb", Microsoft.Data.Sqlite.SqliteType.Text);
        var pBatch = insert.Parameters.Add("$batch", Microsoft.Data.Sqlite.SqliteType.Text);
        var pDest = insert.Parameters.Add("$dest", Microsoft.Data.Sqlite.SqliteType.Text);
        var pSize = insert.Parameters.Add("$size", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pState = insert.Parameters.Add("$state", Microsoft.Data.Sqlite.SqliteType.Text);
        var pCreated = insert.Parameters.Add("$created", Microsoft.Data.Sqlite.SqliteType.Text);
        var pBr = insert.Parameters.Add("$br", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pSr = insert.Parameters.Add("$sr", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pBd = insert.Parameters.Add("$bd", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pLen = insert.Parameters.Add("$len", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pArtist = insert.Parameters.Add("$artist", Microsoft.Data.Sqlite.SqliteType.Text);
        var pAlbum = insert.Parameters.Add("$album", Microsoft.Data.Sqlite.SqliteType.Text);
        var pTitle = insert.Parameters.Add("$title", Microsoft.Data.Sqlite.SqliteType.Text);
        var pTrack = insert.Parameters.Add("$track", Microsoft.Data.Sqlite.SqliteType.Integer);
        var pYear = insert.Parameters.Add("$year", Microsoft.Data.Sqlite.SqliteType.Integer);
        insert.Prepare();

        var count = 0;
        using var reader = select.ExecuteReader();
        while (reader.Read())
        {
            var groupBatch = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var filename = reader.GetString(1);
            var firstRequestedAt = reader.IsDBNull(2) ? DateTime.UtcNow.ToString("o") : reader.GetString(2);
            var batchId = reader.IsDBNull(3) ? null : reader.GetString(3);
            var destDir = reader.IsDBNull(4) ? null : reader.GetString(4);
            object size = reader.IsDBNull(5) ? DBNull.Value : reader.GetInt64(5);
            object bitRate = reader.IsDBNull(6) ? DBNull.Value : reader.GetInt32(6);
            object sampleRate = reader.IsDBNull(7) ? DBNull.Value : reader.GetInt32(7);
            object bitDepth = reader.IsDBNull(8) ? DBNull.Value : reader.GetInt32(8);
            object length = reader.IsDBNull(9) ? DBNull.Value : reader.GetInt32(9);
            object artist = reader.IsDBNull(10) ? DBNull.Value : reader.GetString(10);
            object album = reader.IsDBNull(11) ? DBNull.Value : reader.GetString(11);
            object title = reader.IsDBNull(12) ? DBNull.Value : reader.GetString(12);
            object track = reader.IsDBNull(13) ? DBNull.Value : reader.GetInt32(13);
            object year = reader.IsDBNull(14) ? DBNull.Value : reader.GetInt32(14);
            var succeededCount = reader.IsDBNull(15) ? 0 : reader.GetInt32(15);

            var name = System.IO.Path.GetFileName(filename.Replace('\\', '/').TrimEnd('/'));
            if (string.IsNullOrEmpty(name))
            {
                name = filename;
            }

            pRid.Value = Guid.NewGuid().ToString();
            pName.Value = name;
            pOrig.Value = filename;
            pGb.Value = groupBatch;
            pBatch.Value = (object?)batchId ?? DBNull.Value;
            pDest.Value = (object?)destDir ?? DBNull.Value;
            pSize.Value = size;
            pState.Value = succeededCount > 0 ? "Completed" : "Active";
            pCreated.Value = firstRequestedAt;
            pBr.Value = bitRate;
            pSr.Value = sampleRate;
            pBd.Value = bitDepth;
            pLen.Value = length;
            pArtist.Value = artist;
            pAlbum.Value = album;
            pTitle.Value = title;
            pTrack.Value = track;
            pYear.Value = year;
            insert.ExecuteNonQuery();
            count++;
        }

        return count;
    }
}
