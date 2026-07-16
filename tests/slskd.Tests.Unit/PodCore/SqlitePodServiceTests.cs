// <copyright file="SqlitePodServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.PodCore;

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.PodCore;
using Xunit;

public sealed class SqlitePodServiceTests
{
    [Fact]
    public async Task DeletePodAsync_UsesBoundedSetBasedDeletes()
    {
        const string podId = "pod:00000000000000000000000000000001";
        const string retainedPodId = "pod:00000000000000000000000000000002";
        const string missingPodId = "pod:00000000000000000000000000000003";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.AddRange(
                PodEntity(podId, PodVisibility.Private),
                PodEntity(retainedPodId, PodVisibility.Private));
            context.Members.AddRange(
                Enumerable.Range(0, 10).Select(index => new PodMemberEntity
                {
                    PodId = podId,
                    PeerId = $"peer-{index}",
                }));
            context.Messages.AddRange(
                Enumerable.Range(0, 501).Select(index => new PodMessageEntity
                {
                    PodId = podId,
                    ChannelId = "general",
                    SenderPeerId = "peer-0",
                    TimestampUnixMs = index,
                }));
            context.Messages.Add(new PodMessageEntity
            {
                PodId = missingPodId,
                ChannelId = "general",
                SenderPeerId = "orphan",
                TimestampUnixMs = 1,
            });
            context.MembershipRecords.AddRange(
                Enumerable.Range(0, 501).Select(index => MembershipRecord("peer-0", "join", index, podId)));
            await context.SaveChangesAsync();
        }
        commandCapture.Commands.Clear();
        var service = new SqlitePodService(
            contextFactory,
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance);

        var deleted = await service.DeletePodAsync(podId);

        Assert.True(deleted);
        var deleteCommands = commandCapture.Commands
            .Where(command => command.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Equal(4, deleteCommands.Count);
        Assert.All(deleteCommands, command => Assert.Contains("WHERE", command, StringComparison.OrdinalIgnoreCase));
        await using var verificationContext = await contextFactory.CreateDbContextAsync();
        Assert.False(await verificationContext.Pods.AnyAsync(pod => pod.PodId == podId));
        Assert.True(await verificationContext.Pods.AnyAsync(pod => pod.PodId == retainedPodId));
        Assert.False(await verificationContext.Messages.AnyAsync(message => message.PodId == podId));
        Assert.False(await verificationContext.Members.AnyAsync(member => member.PodId == podId));
        Assert.False(await verificationContext.MembershipRecords.AnyAsync(record => record.PodId == podId));
        Assert.False(await service.DeletePodAsync(missingPodId));
        Assert.True(await verificationContext.Messages.AnyAsync(message => message.PodId == missingPodId));
    }

    [Fact]
    public async Task ListListedAsync_FiltersPodsInSql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.AddRange(
                PodEntity("listed", PodVisibility.Listed),
                PodEntity("private", PodVisibility.Private));
            await context.SaveChangesAsync();
        }
        commandCapture.Commands.Clear();
        var service = new SqlitePodService(
            contextFactory,
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance);

        var pods = await service.ListListedAsync();

        var pod = Assert.Single(pods);
        Assert.Equal("listed", pod.PodId);
        var command = Assert.Single(commandCapture.Commands);
        Assert.Contains("WHERE", command, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Visibility", command, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMembersAsync_AggregatesMembershipHistoryInSql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Members.Add(new PodMemberEntity
            {
                PeerId = "peer-one",
                PodId = "pod-one",
                PublicKey = "public-key",
                Role = "member",
            });
            context.MembershipRecords.AddRange(
                MembershipRecord(" PEER-ONE ", "JOIN", 1_000),
                MembershipRecord("peer-one", "leave", 2_000),
                MembershipRecord("Peer-One", "join", 3_000));
            await context.SaveChangesAsync();
        }
        commandCapture.Commands.Clear();
        var service = new SqlitePodService(
            contextFactory,
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance);

        var members = await service.GetMembersAsync("pod-one");

        var member = Assert.Single(members);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1_000), member.JoinedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(3_000), member.LastSeen);
        Assert.Equal(2, commandCapture.Commands.Count);
        var historyCommand = Assert.Single(commandCapture.Commands, command =>
            command.Contains("MembershipRecords", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("GROUP BY", historyCommand, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Signature", historyCommand, StringComparison.OrdinalIgnoreCase);
    }

    private static SignedMembershipRecordEntity MembershipRecord(
        string peerId,
        string action,
        long timestampUnixMs,
        string podId = "pod-one") => new()
        {
            Action = action,
            PeerId = peerId,
            PodId = podId,
            Signature = "signature",
            TimestampUnixMs = timestampUnixMs,
        };

    private static PodEntity PodEntity(string podId, PodVisibility visibility) => new()
    {
        PodId = podId,
        Name = podId,
        Visibility = visibility,
        Tags = "[]",
        Channels = "[]",
        ExternalBindings = "[]",
    };

    private sealed class TestDbContextFactory(DbContextOptions<PodDbContext> options)
        : IDbContextFactory<PodDbContext>
    {
        public PodDbContext CreateDbContext() => new(options);

        public ValueTask<PodDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
