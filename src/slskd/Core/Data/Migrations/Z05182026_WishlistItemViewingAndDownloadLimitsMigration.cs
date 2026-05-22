// <copyright file="Z05182026_WishlistItemViewingAndDownloadLimitsMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds LastViewedAt and MaxDownloads columns to the WishlistItems table.
///     This runs against the wishlist.db in AppDirectory (not managed by the main Migrator).
/// </summary>
public class Z05182026_WishlistItemViewingAndDownloadLimitsMigration
{
    public Z05182026_WishlistItemViewingAndDownloadLimitsMigration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05182026_WishlistItemViewingAndDownloadLimitsMigration>();
    private string ConnectionString { get; }

    public void Apply()
    {
        try
        {
            var columns = GetExistingColumns();

            if (columns.Contains("lastviewedat") &&
                columns.Contains("maxdownloads") &&
                columns.Contains("lastvisiblehitcount") &&
                columns.Contains("lasthiddenlockedhitcount") &&
                columns.Contains("lastfilteredouthitcount") &&
                columns.Contains("lastresponsecount"))
            {
                Log.Information("> Wishlist viewing, download limit, and hit statistic columns already exist; no migration needed.");
                return;
            }

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                if (!columns.Contains("lastviewedat"))
                {
                    Log.Information("> Adding LastViewedAt column to WishlistItems table...");
                    using var cmd1 = new SqliteCommand(
                        "ALTER TABLE WishlistItems ADD COLUMN LastViewedAt TEXT",
                        connection,
                        transaction);
                    cmd1.ExecuteNonQuery();
                    Log.Information("> LastViewedAt column added");
                }

                if (!columns.Contains("maxdownloads"))
                {
                    Log.Information("> Adding MaxDownloads column to WishlistItems table...");
                    using var cmd2 = new SqliteCommand(
                        "ALTER TABLE WishlistItems ADD COLUMN MaxDownloads INTEGER",
                        connection,
                        transaction);
                    cmd2.ExecuteNonQuery();
                    Log.Information("> MaxDownloads column added");
                }

                AddIntegerColumnIfMissing(connection, transaction, columns, "lastvisiblehitcount", "LastVisibleHitCount", "LastMatchCount");
                AddIntegerColumnIfMissing(connection, transaction, columns, "lasthiddenlockedhitcount", "LastHiddenLockedHitCount", "0");
                AddIntegerColumnIfMissing(connection, transaction, columns, "lastfilteredouthitcount", "LastFilteredOutHitCount", "0");
                AddIntegerColumnIfMissing(connection, transaction, columns, "lastresponsecount", "LastResponseCount", "LastMatchCount");

                transaction.Commit();
                Log.Information("> Wishlist schema migration complete!");
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Wishlist schema migration failed (may be a fresh database): {Message}", ex.Message);
        }
    }

    private void AddIntegerColumnIfMissing(
        SqliteConnection connection,
        SqliteTransaction transaction,
        HashSet<string> columns,
        string normalizedName,
        string columnName,
        string defaultExpression)
    {
        if (columns.Contains(normalizedName))
        {
            return;
        }

        Log.Information("> Adding {Column} column to WishlistItems table...", columnName);
        using var add = new SqliteCommand(
            $"ALTER TABLE WishlistItems ADD COLUMN {columnName} INTEGER NOT NULL DEFAULT 0",
            connection,
            transaction);
        add.ExecuteNonQuery();

        using var backfill = new SqliteCommand(
            $"UPDATE WishlistItems SET {columnName} = COALESCE({defaultExpression}, 0)",
            connection,
            transaction);
        backfill.ExecuteNonQuery();
        Log.Information("> {Column} column added", columnName);
    }

    private HashSet<string> GetExistingColumns()
    {
        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Check if the table exists
            using var tableCmd = new SqliteCommand(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='WishlistItems';",
                connection);
            var tableExists = tableCmd.ExecuteScalar() != null;

            if (!tableExists)
            {
                // Table doesn't exist yet - EnsureCreated will handle it
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            using var cmd = new SqliteCommand("PRAGMA table_info(WishlistItems);", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }

            return columns;
        }
        catch
        {
            // Fresh database or error - let EnsureCreated handle it
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
