// <copyright file="EventsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Events;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using slskd.Events;
using Soulseek;
using Xunit;

public class EventsTests
{
    [Fact]
    public void SearchResponsesReceivedEvent_HasCorrectType()
    {
        // Arrange
        var evt = new SearchResponsesReceivedEvent
        {
            Responses = new List<SearchResponse>(),
        };

        // Assert
        Assert.Equal(EventType.SearchResponsesReceived, evt.Type);
    }

    [Fact]
    public void SearchResponsesReceivedEvent_HasUniqueId()
    {
        // Arrange
        var evt1 = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };
        var evt2 = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };

        // Assert
        Assert.NotEqual(evt1.Id, evt2.Id);
    }

    [Fact]
    public void SearchResponsesReceivedEvent_HasTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var evt = new SearchResponsesReceivedEvent { Responses = new List<SearchResponse>() };
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
    }

    [Fact]
    public void PeerSearchedUsEvent_HasCorrectType()
    {
        // Arrange
        var evt = new PeerSearchedUsEvent
        {
            Username = "testuser",
            SearchText = "test query",
            HadResults = true,
        };

        // Assert
        Assert.Equal(EventType.PeerSearchedUs, evt.Type);
    }

    [Fact]
    public void PeerSearchedUsEvent_StoresProperties()
    {
        // Arrange & Act
        var evt = new PeerSearchedUsEvent
        {
            Username = "testuser",
            SearchText = "test query",
            HadResults = true,
        };

        // Assert
        Assert.Equal("testuser", evt.Username);
        Assert.Equal("test query", evt.SearchText);
        Assert.True(evt.HadResults);
    }

    [Fact]
    public void PeerDownloadedFromUsEvent_HasCorrectType()
    {
        // Arrange
        var evt = new PeerDownloadedFromUsEvent
        {
            Username = "testuser",
            Filename = "/music/test.flac",
        };

        // Assert
        Assert.Equal(EventType.PeerDownloadedFromUs, evt.Type);
    }

    [Fact]
    public void PeerDownloadedFromUsEvent_StoresProperties()
    {
        // Arrange & Act
        var evt = new PeerDownloadedFromUsEvent
        {
            Username = "testuser",
            Filename = "/music/test.flac",
        };

        // Assert
        Assert.Equal("testuser", evt.Username);
        Assert.Equal("/music/test.flac", evt.Filename);
    }

    [Fact]
    public void DownloadFileCompleteEvent_HasCorrectType()
    {
        // Arrange
        var evt = new DownloadFileCompleteEvent
        {
            LocalFilename = "/local/file.flac",
            RemoteFilename = "/remote/file.flac",
            Transfer = new slskd.Transfers.Transfer(),
        };

        // Assert
        Assert.Equal(EventType.DownloadFileComplete, evt.Type);
    }

    [Fact]
    public void PruneAsync_DeletesExpiredEventsWithoutSelectingPayloads()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var commandRecorder = new RecordingCommandInterceptor();
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandRecorder)
            .Options;

        using (var context = new EventsDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Events.AddRange(
                EventRecord.From<NoopEvent>(new NoopEvent { Timestamp = DateTime.UtcNow.AddDays(-30) }),
                EventRecord.From<NoopEvent>(new NoopEvent { Timestamp = DateTime.UtcNow.AddDays(-14) }),
                EventRecord.From<NoopEvent>(new NoopEvent { Timestamp = DateTime.UtcNow }));
            context.SaveChanges();
        }

        commandRecorder.Commands.Clear();
        var service = new EventService(new TestEventsDbContextFactory(options));

        var pruned = service.PruneAsync(7);
        var pruneCommands = commandRecorder.Commands.ToList();

        Assert.Equal(2, pruned);
        Assert.Contains(pruneCommands, command => command.Contains("DELETE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(pruneCommands, command => command.Contains("SELECT", StringComparison.OrdinalIgnoreCase));

        using var verificationContext = new EventsDbContext(options);
        var remaining = verificationContext.Events.Single();
        Assert.True(remaining.Timestamp > DateTime.UtcNow.AddDays(-7));
    }

    private sealed class TestEventsDbContextFactory : IDbContextFactory<EventsDbContext>
    {
        private readonly DbContextOptions<EventsDbContext> _options;

        public TestEventsDbContextFactory(DbContextOptions<EventsDbContext> options)
        {
            _options = options;
        }

        public EventsDbContext CreateDbContext()
        {
            return new EventsDbContext(_options);
        }
    }

    private sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            Commands.Add(command.CommandText);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }
    }
}

// Note: EventBus tests require full database setup and are covered by integration tests.
// The event type tests above verify the event data structures work correctly.
