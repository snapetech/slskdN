// <copyright file="TransfersDbContextTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using slskd.Migrations;
using slskd.Transfers;
using Soulseek;
using Xunit;
using SlskdTransfer = slskd.Transfers.Transfer;

public sealed class TransfersDbContextTests
{
    [Fact]
    public async Task SaveChanges_Stamps_Transfer_Updates_For_Sync_And_Async_Writes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new TransfersDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var transfer = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Direction = TransferDirection.Download,
            Filename = "Music/song.flac",
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UnixEpoch,
            Username = "listener",
        };
        context.Transfers.Add(transfer);

        context.SaveChanges();
        Assert.True(transfer.UpdatedAt > DateTime.UnixEpoch);

        transfer.BytesTransferred = 1;
        transfer.UpdatedAt = DateTime.UnixEpoch;
        await context.SaveChangesAsync();
        Assert.True(transfer.UpdatedAt > DateTime.UnixEpoch);
    }

    [Fact]
    public async Task Transfer_Change_Query_Uses_Direction_And_UpdatedAt_Index()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new TransfersDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            EXPLAIN QUERY PLAN
            SELECT *
            FROM Transfers
            WHERE Direction = 'Download' AND UpdatedAt > '2026-07-15 12:00:00'
            ORDER BY UpdatedAt
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var details = new List<string>();
        while (await reader.ReadAsync())
        {
            details.Add(reader.GetString(3));
        }

        Assert.Contains(details, detail =>
            detail.Contains("INDEX IDX_Transfers_Direction_UpdatedAt", StringComparison.Ordinal));
        Assert.DoesNotContain(details, detail =>
            detail.Contains("TEMP B-TREE", StringComparison.Ordinal));
    }

    [Fact]
    public void TransferUpdatedAtMigration_Adds_Backfills_And_Reuses_Ordered_Index()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"slskdn-transfers-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE Transfers (
                        Id TEXT PRIMARY KEY,
                        Direction TEXT NOT NULL,
                        RequestedAt TEXT NOT NULL,
                        EnqueuedAt TEXT NULL,
                        StartedAt TEXT NULL,
                        EndedAt TEXT NULL
                    );
                    INSERT INTO Transfers (Id, Direction, RequestedAt)
                    VALUES ('transfer-1', 'Download', '2026-07-15 12:00:00');
                    """;
                command.ExecuteNonQuery();
            }

            var migration = new Z07152026_TransferUpdatedAtMigration(
                new ConnectionStringDictionary(new()
                {
                    [Database.Transfers] = connectionString,
                }));

            Assert.True(migration.NeedsToBeApplied());
            migration.Apply();
            Assert.False(migration.NeedsToBeApplied());

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var timestamp = connection.CreateCommand();
                timestamp.CommandText = "SELECT UpdatedAt FROM Transfers WHERE Id = 'transfer-1'";
                Assert.Equal("2026-07-15 12:00:00", timestamp.ExecuteScalar());

                using var index = connection.CreateCommand();
                index.CommandText = "PRAGMA index_info('IDX_Transfers_Direction_UpdatedAt')";
                using var reader = index.ExecuteReader();
                var columns = new List<string>();
                while (reader.Read())
                {
                    columns.Add(reader.GetString(reader.GetOrdinal("name")));
                }

                Assert.Equal(["Direction", "UpdatedAt"], columns);
            }

            migration.Apply();
            Assert.False(migration.NeedsToBeApplied());
        }
        finally
        {
            System.IO.File.Delete(databasePath);
        }
    }

    [Fact]
    public void Removed_Is_Serialized_Only_For_Removed_Transfer_Deltas()
    {
        var visible = JsonSerializer.Serialize(new SlskdTransfer { Removed = false });
        var removed = JsonSerializer.Serialize(new SlskdTransfer { Removed = true });

        Assert.DoesNotContain("Removed", visible, StringComparison.Ordinal);
        Assert.Contains("\"Removed\":true", removed, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryingLegacyRows_AllowsNullTransferStrings()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new TransfersDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                INSERT INTO Transfers
                (Id, Username, Direction, Filename, Size, StartOffset, State, StateDescription, RequestedAt, EnqueuedAt, StartedAt, EndedAt, BytesTransferred, AverageSpeed, PlaceInQueue, Exception, Removed, Attempts)
                VALUES
                ($id, $username, $direction, $filename, $size, $startOffset, $state, NULL, $requestedAt, NULL, NULL, NULL, $bytesTransferred, $averageSpeed, NULL, NULL, 0, 1)
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid());
            command.Parameters.AddWithValue("$username", "legacy-user");
            command.Parameters.AddWithValue("$direction", TransferDirection.Upload.ToString());
            command.Parameters.AddWithValue("$filename", "legacy.flac");
            command.Parameters.AddWithValue("$size", 1024L);
            command.Parameters.AddWithValue("$startOffset", 0L);
            command.Parameters.AddWithValue("$state", TransferStates.None.ToString());
            command.Parameters.AddWithValue("$requestedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("$bytesTransferred", 0L);
            command.Parameters.AddWithValue("$averageSpeed", 0d);
            command.ExecuteNonQuery();
        }

        using var verificationContext = new TransfersDbContext(options);
        var transfer = verificationContext.Transfers.Single();

        Assert.Equal("legacy-user", transfer.Username);
        Assert.Null(transfer.StateDescription);
        Assert.Null(transfer.Exception);
    }
}
