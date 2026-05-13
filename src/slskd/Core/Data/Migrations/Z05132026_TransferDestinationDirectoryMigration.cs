// <copyright file="Z05132026_TransferDestinationDirectoryMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Migrations;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Serilog;

/// <summary>
///     Adds optional completed-file destination metadata to transfer history.
/// </summary>
public class Z05132026_TransferDestinationDirectoryMigration : IMigration
{
    public Z05132026_TransferDestinationDirectoryMigration(ConnectionStringDictionary connectionStrings)
    {
        ConnectionString = connectionStrings[Database.Transfers];
    }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z05132026_TransferDestinationDirectoryMigration>();
    private string ConnectionString { get; }

    public bool NeedsToBeApplied()
    {
        var columns = SchemaInspector.GetDatabaseSchema(ConnectionString);
        var transfers = columns["Transfers"];

        return !HasColumn(transfers, "DestinationDirectory");
    }

    public void Apply()
    {
        if (!NeedsToBeApplied())
        {
            Log.Information("> Migration {Name} is not necessary or has already been applied", nameof(Z05132026_TransferDestinationDirectoryMigration));
            return;
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = new SqliteCommand("ALTER TABLE Transfers ADD COLUMN DestinationDirectory TEXT NULL", connection);
        command.ExecuteNonQuery();

        Log.Information("> Added transfer destination directory metadata");
    }

    private static bool HasColumn(IEnumerable<SchemaInspector.ColumnInfo> columns, string name)
        => columns.Any(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
