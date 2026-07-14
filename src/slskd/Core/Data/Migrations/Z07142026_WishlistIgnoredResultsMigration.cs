// <copyright file="Z07142026_WishlistIgnoredResultsMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Migrations;

using Microsoft.Data.Sqlite;
using Serilog;

public class Z07142026_WishlistIgnoredResultsMigration
{
    public Z07142026_WishlistIgnoredResultsMigration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private string ConnectionString { get; }
    private ILogger Log { get; } = Serilog.Log.ForContext<Z07142026_WishlistIgnoredResultsMigration>();

    public void Apply()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS WishlistIgnoredResults (
                Id TEXT NOT NULL CONSTRAINT PK_WishlistIgnoredResults PRIMARY KEY,
                WishlistItemId TEXT NOT NULL,
                Username TEXT COLLATE NOCASE NOT NULL,
                Directory TEXT COLLATE NOCASE NOT NULL,
                CreatedAt TEXT NOT NULL,
                CONSTRAINT FK_WishlistIgnoredResults_WishlistItems_WishlistItemId
                    FOREIGN KEY (WishlistItemId) REFERENCES WishlistItems (Id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_WishlistIgnoredResults_WishlistItemId_Username_Directory
                ON WishlistIgnoredResults (WishlistItemId, Username, Directory);
            """;
        command.ExecuteNonQuery();

        var hasIgnoredCount = false;
        {
            using var columns = connection.CreateCommand();
            columns.CommandText = "PRAGMA table_info(WishlistItems);";
            using var reader = columns.ExecuteReader();
            while (reader.Read())
            {
                hasIgnoredCount |= reader.GetString(1).Equals("LastIgnoredResultHitCount", System.StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!hasIgnoredCount)
        {
            using var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE WishlistItems ADD COLUMN LastIgnoredResultHitCount INTEGER NOT NULL DEFAULT 0;";
            addColumn.ExecuteNonQuery();
        }

        Log.Information("> Wishlist ignored-result schema is ready.");
    }
}
