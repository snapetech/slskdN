// <copyright file="Z05222026_TransferFileDetailsMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds completed local path and embedded audio tag columns to transfer records.
/// </summary>
public class Z05222026_TransferFileDetailsMigration : IMigration
{
    public Z05222026_TransferFileDetailsMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Transfers];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05222026_TransferFileDetailsMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        var columns = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transfers = columns["Transfers"];

        return !HasColumn(transfers, "LocalFilename") ||
               !HasColumn(transfers, "Artist") ||
               !HasColumn(transfers, "Album") ||
               !HasColumn(transfers, "Title") ||
               !HasColumn(transfers, "TrackNumber") ||
               !HasColumn(transfers, "Year");
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05222026_TransferFileDetailsMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var columns = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transfers = columns["Transfers"];

        AddColumnIfMissing(connection, transfers, "LocalFilename", "TEXT NULL");
        AddColumnIfMissing(connection, transfers, "Artist", "TEXT NULL");
        AddColumnIfMissing(connection, transfers, "Album", "TEXT NULL");
        AddColumnIfMissing(connection, transfers, "Title", "TEXT NULL");
        AddColumnIfMissing(connection, transfers, "TrackNumber", "INTEGER NULL");
        AddColumnIfMissing(connection, transfers, "Year", "INTEGER NULL");
    }

    private void AddColumnIfMissing(
        SqliteConnection connection,
        IEnumerable<SchemaInspector.ColumnInfo> columns,
        string name,
        string definition)
    {
        if (HasColumn(columns, name))
        {
            return;
        }

        using var cmd = new SqliteCommand($"ALTER TABLE Transfers ADD COLUMN {name} {definition}", connection);
        cmd.ExecuteNonQuery();
        Log.Information("> Added {Column} column to Transfers", name);
    }

    private static bool HasColumn(IEnumerable<SchemaInspector.ColumnInfo> columns, string name)
        => columns.Any(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
