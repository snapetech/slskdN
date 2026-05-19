// <copyright file="Z05192026_TransferAudioMetadataMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds optional audio metadata columns (BitRate, SampleRate, BitDepth, Length) to transfer records.
/// </summary>
public class Z05192026_TransferAudioMetadataMigration : IMigration
{
    public Z05192026_TransferAudioMetadataMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Transfers];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05192026_TransferAudioMetadataMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        var columns = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transfers = columns["Transfers"];

        return !HasColumn(transfers, "BitRate") ||
               !HasColumn(transfers, "SampleRate") ||
               !HasColumn(transfers, "BitDepth") ||
               !HasColumn(transfers, "Length");
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05192026_TransferAudioMetadataMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var columns = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transfers = columns["Transfers"];

        if (!HasColumn(transfers, "BitRate"))
        {
            using var cmd = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN BitRate INTEGER NULL", connection);
            cmd.ExecuteNonQuery();
            Log.Information("> Added BitRate column to Transfers");
        }

        if (!HasColumn(transfers, "SampleRate"))
        {
            using var cmd = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN SampleRate INTEGER NULL", connection);
            cmd.ExecuteNonQuery();
            Log.Information("> Added SampleRate column to Transfers");
        }

        if (!HasColumn(transfers, "BitDepth"))
        {
            using var cmd = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN BitDepth INTEGER NULL", connection);
            cmd.ExecuteNonQuery();
            Log.Information("> Added BitDepth column to Transfers");
        }

        if (!HasColumn(transfers, "Length"))
        {
            using var cmd = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN Length INTEGER NULL", connection);
            cmd.ExecuteNonQuery();
            Log.Information("> Added Length column to Transfers");
        }
    }

    private static bool HasColumn(IEnumerable<SchemaInspector.ColumnInfo> columns, string name)
        => columns.Any(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
