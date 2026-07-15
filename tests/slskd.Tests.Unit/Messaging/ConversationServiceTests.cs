// <copyright file="ConversationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Messaging;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using slskd.Events;
using slskd.Messaging;
using slskd.Migrations;
using slskd.PodCore;
using Soulseek;
using Xunit;

public class ConversationServiceTests
{
    [Fact]
    public void AcknowledgementIndexMigration_Is_Idempotent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"slskdn-messaging-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE PrivateMessages (
                        Username TEXT NOT NULL,
                        Id INTEGER NOT NULL,
                        Timestamp TEXT NOT NULL,
                        IsAcknowledged INTEGER NOT NULL,
                        PRIMARY KEY (Username, Id, Timestamp)
                    );
                    """;
                command.ExecuteNonQuery();
            }

            var connectionStrings = new ConnectionStringDictionary(new()
            {
                [Database.Messaging] = connectionString,
            });
            var migration = new Z07152026_PrivateMessageAcknowledgementIndexMigration(connectionStrings);

            Assert.True(migration.NeedsToBeApplied());
            migration.Apply();
            Assert.False(migration.NeedsToBeApplied());

            migration.Apply();
            Assert.False(migration.NeedsToBeApplied());
        }
        finally
        {
            System.IO.File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task HasUnAcknowledgedMessagesAsync_Returns_Existence_Without_Loading_Conversations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseSqlite(connection)
            .Options;
        var factory = new TestDbContextFactory(options);

        await using (var context = factory.CreateDbContext())
        {
            await context.Database.EnsureCreatedAsync();
            context.PrivateMessages.AddRange(
                new PrivateMessage
                {
                    Id = 1,
                    Username = "acknowledged",
                    IsAcknowledged = true,
                },
                new PrivateMessage
                {
                    Id = 2,
                    Username = "unacknowledged",
                    IsAcknowledged = false,
                });
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);

        Assert.True(await service.HasUnAcknowledgedMessagesAsync());

        await using (var context = factory.CreateDbContext())
        {
            await context.PrivateMessages.ExecuteUpdateAsync(update => update.SetProperty(message => message.IsAcknowledged, true));
        }

        Assert.False(await service.HasUnAcknowledgedMessagesAsync());
    }

    [Fact]
    public async Task MessagingSchema_Uses_Acknowledgement_Index_For_Existence_Query()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new MessagingDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN SELECT EXISTS (SELECT 1 FROM PrivateMessages WHERE IsAcknowledged = 0)";
        await using var reader = await command.ExecuteReaderAsync();

        var details = new List<string>();
        while (await reader.ReadAsync())
        {
            details.Add(reader.GetString(3));
        }

        Assert.Contains(details, detail => detail.Contains("IDX_PrivateMessages_IsAcknowledged", StringComparison.Ordinal));
    }

    private static ConversationService CreateService(IDbContextFactory<MessagingDbContext> contextFactory)
    {
        return new ConversationService(
            Mock.Of<ISoulseekClient>(),
            new EventBus(new EventService(Mock.Of<IDbContextFactory<EventsDbContext>>())),
            contextFactory,
            Mock.Of<IPodService>());
    }

    private sealed class TestDbContextFactory : IDbContextFactory<MessagingDbContext>
    {
        public TestDbContextFactory(DbContextOptions<MessagingDbContext> options)
        {
            Options = options;
        }

        private DbContextOptions<MessagingDbContext> Options { get; }

        public MessagingDbContext CreateDbContext()
        {
            return new MessagingDbContext(Options);
        }
    }
}
