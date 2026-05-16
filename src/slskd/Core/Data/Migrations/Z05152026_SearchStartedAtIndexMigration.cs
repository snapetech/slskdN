// <copyright file="Z05152026_SearchStartedAtIndexMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds an index for recent-first search history listing.
/// </summary>
public class Z05152026_SearchStartedAtIndexMigration : IMigration
{
    public Z05152026_SearchStartedAtIndexMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Search];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05152026_SearchStartedAtIndexMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        try
        {
            var idxes = SchemaInspector.GetDatabaseIndexes(ConnectionString);

            if (!idxes.ContainsKey("Searches"))
            {
                return false;
            }

            return !idxes["Searches"].Any(c => c.Name.Equals("IDX_Searches_StartedAt", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to check if search index migration is needed");
            return false;
        }
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05152026_SearchStartedAtIndexMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            Log.Information("> Adding missing index on the Searches table...");

            using var command = new SqliteCommand(
                "CREATE INDEX IF NOT EXISTS IDX_Searches_StartedAt ON Searches (StartedAt DESC)",
                connection,
                transaction);
            command.ExecuteNonQuery();

            Log.Information("> Index created");
            transaction.Commit();
            Log.Information("> Done!");
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
}
