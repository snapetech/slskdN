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
        long timestampUnixMs) => new()
        {
            Action = action,
            PeerId = peerId,
            PodId = "pod-one",
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
    }
}
