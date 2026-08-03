// <copyright file="Z08032026_AutoReplaceAttemptsMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds persisted auto-replace cycle tracking to transfer records.
/// </summary>
public class Z08032026_AutoReplaceAttemptsMigration : IMigration
{
    public Z08032026_AutoReplaceAttemptsMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Transfers];
    }

    private string ConnectionString { get; }
    private ILogger Log { get; } = Serilog.Log.ForContext<Z08032026_AutoReplaceAttemptsMigration>();

    public bool NeedsToBeApplied()
    {
        var schema = SchemaInspector.GetDatabaseSchema(ConnectionString);
        return schema.TryGetValue("Transfers", out var transfers) &&
            !HasColumn(transfers, "AutoReplaceAttempts");
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z08032026_AutoReplaceAttemptsMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using var command = new SqliteCommand(
                "ALTER TABLE Transfers ADD COLUMN AutoReplaceAttempts INTEGER NOT NULL DEFAULT 0",
                connection,
                transaction);
            command.ExecuteNonQuery();

            transaction.Commit();
            Log.Information("> Added persisted auto-replace attempt tracking");
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    private static bool HasColumn(IEnumerable<SchemaInspector.ColumnInfo> columns, string name)
        => columns.Any(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
