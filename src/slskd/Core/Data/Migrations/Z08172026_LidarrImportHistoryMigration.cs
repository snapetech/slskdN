// <copyright file="Z08172026_LidarrImportHistoryMigration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Migrations;

using Microsoft.Data.Sqlite;
using Serilog;

public sealed class Z08172026_LidarrImportHistoryMigration
{
    public Z08172026_LidarrImportHistoryMigration(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private string ConnectionString { get; }

    private ILogger Log { get; } = Serilog.Log.ForContext<Z08172026_LidarrImportHistoryMigration>();

    public void Apply()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS LidarrImportHistory (
                    Id TEXT NOT NULL CONSTRAINT PK_LidarrImportHistory PRIMARY KEY,
                    SourceDirectory TEXT NOT NULL,
                    Directory TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    ErrorMessage TEXT NOT NULL,
                    SkippedReason TEXT NOT NULL,
                    CandidateCount INTEGER NOT NULL DEFAULT 0,
                    SafeCandidateCount INTEGER NOT NULL DEFAULT 0,
                    RejectedCandidateCount INTEGER NOT NULL DEFAULT 0,
                    CommandId INTEGER NULL,
                    ImportMode TEXT NOT NULL,
                    StartedAt TEXT NOT NULL,
                    CompletedAt TEXT NULL,
                    RetryOfId TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_LidarrImportHistory_StartedAt
                    ON LidarrImportHistory (StartedAt DESC);
                CREATE INDEX IF NOT EXISTS IX_LidarrImportHistory_Status_CommandId
                    ON LidarrImportHistory (Status, CommandId);
                """;
            command.ExecuteNonQuery();
            transaction.Commit();
            Log.Information("> Lidarr import history schema is ready.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
