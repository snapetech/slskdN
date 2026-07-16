// <copyright file="Z07162026_WishlistSearchTextIndexMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Migrations;

using Microsoft.Data.Sqlite;
using Serilog;

public sealed class Z07162026_WishlistSearchTextIndexMigration
{
    public Z07162026_WishlistSearchTextIndexMigration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private string ConnectionString { get; }
    private ILogger Log { get; } = Serilog.Log.ForContext<Z07162026_WishlistSearchTextIndexMigration>();

    public void Apply()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE INDEX IF NOT EXISTS IX_WishlistItems_SearchText_NoCase
                ON WishlistItems (SearchText COLLATE NOCASE)
            """;
        command.ExecuteNonQuery();

        Log.Information("> Wishlist case-insensitive search-text index is ready.");
    }
}
