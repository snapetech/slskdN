// <copyright file="Z05182026_SearchSourceAndWishlistItemIdMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds Source and WishlistItemId columns to the Searches table.
/// </summary>
public class Z05182026_SearchSourceAndWishlistItemIdMigration : IMigration
{
    public Z05182026_SearchSourceAndWishlistItemIdMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Search];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05182026_SearchSourceAndWishlistItemIdMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        try
        {
            var schema = SchemaInspector.GetDatabaseSchema(ConnectionString);

            if (!schema.ContainsKey("Searches"))
            {
                return false;
            }

            var columns = schema["Searches"].Select(c => c.Name.ToLowerInvariant()).ToHashSet();
            return !columns.Contains("source") || !columns.Contains("wishlistitemid") || HasNullSources();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to check if search columns migration is needed");
            return false;
        }
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05182026_SearchSourceAndWishlistItemIdMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var schema = SchemaInspector.GetDatabaseSchema(ConnectionString);
            var columns = schema.ContainsKey("Searches")
                ? schema["Searches"].Select(c => c.Name.ToLowerInvariant()).ToHashSet()
                : new System.Collections.Generic.HashSet<string>();

            if (!columns.Contains("source"))
            {
                Log.Information("> Adding Source column to Searches table...");
                using var cmd1 = new SqliteCommand(
                    "ALTER TABLE Searches ADD COLUMN Source TEXT",
                    connection,
                    transaction);
                cmd1.ExecuteNonQuery();
                Log.Information("> Source column added");
            }

            if (!columns.Contains("wishlistitemid"))
            {
                Log.Information("> Adding WishlistItemId column to Searches table...");
                using var cmd2 = new SqliteCommand(
                    "ALTER TABLE Searches ADD COLUMN WishlistItemId TEXT",
                    connection,
                    transaction);
                cmd2.ExecuteNonQuery();
                Log.Information("> WishlistItemId column added");
            }

            Log.Information("> Backfilling missing Search Source values...");
            using var cmd3 = new SqliteCommand(
                "UPDATE Searches SET Source = 'manual' WHERE Source IS NULL OR trim(Source) = ''",
                connection,
                transaction);
            cmd3.ExecuteNonQuery();

            transaction.Commit();
            Log.Information("> Done!");
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    private bool HasNullSources()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM Searches WHERE Source IS NULL OR trim(Source) = ''",
            connection);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
