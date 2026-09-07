// <copyright file="Z09062026_WishlistBlockedHitCountMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds the blocked-user hit statistic to the wishlist database.
/// </summary>
public sealed class Z09062026_WishlistBlockedHitCountMigration
{
    public Z09062026_WishlistBlockedHitCountMigration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private string ConnectionString { get; }
    private ILogger Log { get; } = Serilog.Log.ForContext<Z09062026_WishlistBlockedHitCountMigration>();

    public void Apply()
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            if (!WishlistTableExists(connection))
            {
                return;
            }

            var columns = GetExistingColumns(connection);
            if (columns.Contains("lastblockedhitcount"))
            {
                return;
            }

            using var transaction = connection.BeginTransaction();
            using var command = new SqliteCommand(
                "ALTER TABLE WishlistItems ADD COLUMN LastBlockedHitCount INTEGER NOT NULL DEFAULT 0",
                connection,
                transaction);
            command.ExecuteNonQuery();
            transaction.Commit();
            Log.Information("> Added LastBlockedHitCount column to WishlistItems.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Wishlist blocked-hit-count migration failed: {Message}", ex.Message);
        }
    }

    private static bool WishlistTableExists(SqliteConnection connection)
    {
        using var command = new SqliteCommand(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='WishlistItems'",
            connection);
        return command.ExecuteScalar() != null;
    }

    private static HashSet<string> GetExistingColumns(SqliteConnection connection)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = new SqliteCommand("PRAGMA table_info(WishlistItems)", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
