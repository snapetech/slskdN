// <copyright file="Z07152026_PrivateMessageAcknowledgementIndexMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds an index for private-message acknowledgement existence queries.
/// </summary>
public class Z07152026_PrivateMessageAcknowledgementIndexMigration : IMigration
{
    private const string IndexName = "IDX_PrivateMessages_IsAcknowledged";

    public Z07152026_PrivateMessageAcknowledgementIndexMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Messaging];
    }

    private string ConnectionString { get; }
    private ILogger Log { get; } = Serilog.Log.ForContext<Z07152026_PrivateMessageAcknowledgementIndexMigration>();

    public bool NeedsToBeApplied()
    {
        try
        {
            var indexes = SchemaInspector.GetDatabaseIndexes(ConnectionString);
            if (!indexes.TryGetValue("PrivateMessages", out var privateMessageIndexes))
            {
                return false;
            }

            return !privateMessageIndexes.Any(index => index.Name.Equals(IndexName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to check if the private-message acknowledgement index migration is needed");
            return false;
        }
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z07152026_PrivateMessageAcknowledgementIndexMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            Log.Information("> Adding the private-message acknowledgement index...");

            using var command = new SqliteCommand(
                $"CREATE INDEX IF NOT EXISTS {IndexName} ON PrivateMessages (IsAcknowledged)",
                connection,
                transaction);
            command.ExecuteNonQuery();

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
