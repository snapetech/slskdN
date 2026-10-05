// <copyright file="SqlitePodServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.PodCore;

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.PodCore;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class SqlitePodServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenStorageFails_EscapesExceptionInLogAndRollsBack()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var initializationOptions = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var initializationFactory = new TestDbContextFactory(initializationOptions);
        await using (var initializationContext = await initializationFactory.CreateDbContextAsync())
        {
            await initializationContext.Database.EnsureCreatedAsync();
        }

        var interceptor = new FailingPodInsertInterceptor();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        var factory = new TestDbContextFactory(options);
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            factory,
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            logger);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(new Pod
        {
            PodId = podId,
            Name = "Valid pod",
            Visibility = PodVisibility.Private,
        }));

        Assert.Equal("injected storage failure\r\nforged log entry", exception.InnerException?.Message);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("injected storage failure\\r\\nforged log entry", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);

        await using var verificationContext = await initializationFactory.CreateDbContextAsync();
        Assert.False(await verificationContext.Pods.AnyAsync(pod => pod.PodId == podId));
    }

    [Fact]
    public async Task CreateAsync_WhenCallerCancelsDuringInsert_PropagatesAndRollsBack()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var initializationOptions = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var initializationFactory = new TestDbContextFactory(initializationOptions);
        await using (var initializationContext = await initializationFactory.CreateDbContextAsync())
        {
            await initializationContext.Database.EnsureCreatedAsync();
        }

        using var cancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new CancelPodInsertInterceptor(cancellation))
            .Options;
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            new TestDbContextFactory(options),
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(new Pod
        {
            PodId = podId,
            Name = "Valid pod",
            Visibility = PodVisibility.Private,
        }, cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Empty(logger.Entries);
        await using var verificationContext = await initializationFactory.CreateDbContextAsync();
        Assert.False(await verificationContext.Pods.AnyAsync(pod => pod.PodId == podId));
    }

    [Fact]
    public async Task GetPodAsync_WhenCallerCancels_PropagatesCancellation()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            factory,
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetPodAsync(podId, cancellation.Token));

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task JoinAsync_WhenCallerCancelsDuringRead_PropagatesCancellationAndRollsBack()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var initializationOptions = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var initializationFactory = new TestDbContextFactory(initializationOptions);
        await using (var context = await initializationFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        using var cancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new CancelPodReadInterceptor(cancellation))
            .Options;
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            new TestDbContextFactory(options),
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.JoinAsync(podId, new PodMember { PeerId = "listener" }, cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Empty(logger.Entries);
        await using var verificationContext = await initializationFactory.CreateDbContextAsync();
        Assert.Empty(await verificationContext.Members.ToListAsync());
        Assert.Empty(await verificationContext.MembershipRecords.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerCancelsDuringRead_PropagatesAndRollsBack()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var initializationOptions = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var initializationFactory = new TestDbContextFactory(initializationOptions);
        await using (var context = await initializationFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        using var cancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new CancelPodReadInterceptor(cancellation))
            .Options;
        var publisher = new Mock<IPodPublisher>();
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            new TestDbContextFactory(options),
            publisher.Object,
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UpdateAsync(new Pod
        {
            PodId = podId,
            Name = "Updated pod",
            Visibility = PodVisibility.Private,
        }, cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Empty(logger.Entries);
        publisher.Verify(value => value.PublishAsync(It.IsAny<Pod>(), It.IsAny<CancellationToken>()), Times.Never);
        await using var verificationContext = await initializationFactory.CreateDbContextAsync();
        Assert.Equal(podId, (await verificationContext.Pods.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeletePodAsync_WhenCallerCancelsDuringRead_PropagatesAndKeepsPod()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var initializationOptions = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var initializationFactory = new TestDbContextFactory(initializationOptions);
        await using (var context = await initializationFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        using var cancellation = new CancellationTokenSource();
        var options = new DbContextOptionsBuilder<PodDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new CancelPodReadInterceptor(cancellation))
            .Options;
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            new TestDbContextFactory(options),
            Mock.Of<IPodPublisher>(),
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.DeletePodAsync(podId, cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Empty(logger.Entries);
        await using var verificationContext = await initializationFactory.CreateDbContextAsync();
        Assert.True(await verificationContext.Pods.AnyAsync(pod => pod.PodId == podId));
    }

    [Fact]
    public async Task UpdateAsync_WhenPublishIsCancelled_PropagatesAfterCommittedUpdate()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        using var cancellation = new CancellationTokenSource();
        var publisher = new Mock<IPodPublisher>();
        publisher
            .Setup(value => value.PublishAsync(It.IsAny<Pod>(), cancellation.Token))
            .Returns((Pod _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(token);
            });
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            factory,
            publisher.Object,
            Mock.Of<IPodMembershipSigner>(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UpdateAsync(new Pod
        {
            PodId = podId,
            Name = "Updated pod",
            Visibility = PodVisibility.Private,
        }, cancellation.Token));

        Assert.Empty(logger.Entries);
        await using var verificationContext = await factory.CreateDbContextAsync();
        Assert.Equal("Updated pod", (await verificationContext.Pods.SingleAsync()).Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenPublisherFails_EscapesExceptionAndKeepsCommittedUpdate()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Private));
            await context.SaveChangesAsync();
        }

        var publisher = new Mock<IPodPublisher>();
        publisher
            .Setup(value => value.PublishAsync(It.IsAny<Pod>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failure\r\nforged log entry"));
        var logger = new CapturingLogger<SqlitePodService>();
        var service = new SqlitePodService(
            factory,
            publisher.Object,
            Mock.Of<IPodMembershipSigner>(),
            logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(new Pod
        {
            PodId = podId,
            Name = "Updated pod",
            Visibility = PodVisibility.Private,
        }));

        Assert.Equal("publish failure\r\nforged log entry", exception.Message);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("publish failure\\r\\nforged log entry", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
        await using var verificationContext = await factory.CreateDbContextAsync();
        Assert.Equal("Updated pod", (await verificationContext.Pods.SingleAsync()).Name);
    }

    [Fact]
    public async Task LeaveAsync_ActiveMemberCanLeaveAndRejoin()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Listed));
            context.Members.Add(new PodMemberEntity { PodId = podId, PeerId = "listener" });
            await context.SaveChangesAsync();
        }
        var service = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance);
        Assert.True(await service.LeaveAsync(podId, "listener"));
        Assert.True(await service.JoinAsync(podId, new PodMember { PeerId = "listener" }));
        Assert.Single(await service.GetMembersAsync(podId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60_000)]
    public async Task MembershipActions_KeepOrderWhenClockStopsOrMovesBack(long clockOffset)
    {
        const string podId = "pod:00000000000000000000000000000001";
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(() => now);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Listed));
            context.Members.Add(new PodMemberEntity { PodId = podId, PeerId = "listener" });
            await context.SaveChangesAsync();
        }
        var service = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance, timeProvider: clock.Object);
        Assert.True(await service.LeaveAsync(podId, "listener"));
        now = now.AddMilliseconds(clockOffset);
        Assert.True(await service.JoinAsync(podId, new PodMember { PeerId = "listener" }));
        var restored = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance, timeProvider: clock.Object);
        Assert.True(await restored.BanAsync(podId, "listener"));
        Assert.False(await restored.LeaveAsync(podId, "listener"));
        Assert.False(await restored.JoinAsync(podId, new PodMember { PeerId = "listener" }));
        var history = await restored.GetMembershipHistoryAsync(podId);
        Assert.Equal(new[] { "leave", "join", "ban" }, history.Select(record => record.Action));
        Assert.Equal(new long[] { 1_700_000_000_000, 1_700_000_000_001, 1_700_000_000_002 }, history.Select(record => record.TimestampUnixMs));
        Assert.Empty(await restored.GetMembersAsync(podId));
    }

    [Fact]
    public async Task MembershipActions_AllocateHistoryPerPodAndPeer()
    {
        const string podId = "pod:00000000000000000000000000000001";
        const string secondPodId = "pod:00000000000000000000000000000002";
        const long timestamp = 1_700_000_000_000;
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(DateTimeOffset.FromUnixTimeMilliseconds(timestamp));
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new TestDbContextFactory(new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.AddRange(PodEntity(podId, PodVisibility.Listed), PodEntity(secondPodId, PodVisibility.Listed));
            context.Members.AddRange(
                new PodMemberEntity { PodId = podId, PeerId = "listener" },
                new PodMemberEntity { PodId = podId, PeerId = "other" },
                new PodMemberEntity { PodId = secondPodId, PeerId = "listener" });
            context.MembershipRecords.AddRange(
                MembershipRecord("listener", "join", timestamp + 5_000, podId),
                MembershipRecord("other", "join", timestamp + 80_000, podId),
                MembershipRecord("listener", "join", timestamp + 20_000, secondPodId));
            await context.SaveChangesAsync();
        }
        var service = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance, timeProvider: clock.Object);
        Assert.True(await service.LeaveAsync(podId, "listener"));
        Assert.True(await service.LeaveAsync(podId, "other"));
        Assert.True(await service.LeaveAsync(secondPodId, "listener"));
        var history = await service.GetMembershipHistoryAsync(podId);
        Assert.Equal(timestamp + 5_001, Assert.Single(history, record => record.PeerId == "listener" && record.Action == "leave").TimestampUnixMs);
        Assert.Equal(timestamp + 80_001, Assert.Single(history, record => record.PeerId == "other" && record.Action == "leave").TimestampUnixMs);
        Assert.Equal(timestamp + 20_001, Assert.Single((await service.GetMembershipHistoryAsync(secondPodId)), record => record.Action == "leave").TimestampUnixMs);
    }

    [Fact]
    public async Task ConcurrentJoins_CommitOneMemberAndOneHistoryRecord()
    {
        const string podId = "pod:00000000000000000000000000000001";
        var database = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"membership-{Guid.NewGuid():N}.db");
        try
        {
            var factory = new TestDbContextFactory(new DbContextOptionsBuilder<PodDbContext>()
                .UseSqlite($"Data Source={database};Pooling=False;Default Timeout=10").Options);
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.EnsureCreatedAsync();
                context.Pods.Add(PodEntity(podId, PodVisibility.Listed));
                await context.SaveChangesAsync();
            }
            var clock = new Mock<TimeProvider>();
            clock.Setup(provider => provider.GetUtcNow()).Returns(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000));
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
            {
                var service = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
                    NullLogger<SqlitePodService>.Instance, timeProvider: clock.Object);
                return await service.JoinAsync(podId, new PodMember { PeerId = "listener" });
            })));
            Assert.All(results, result => Assert.True(result));
            await using var verification = await factory.CreateDbContextAsync();
            Assert.Single(await verification.Members.ToListAsync());
            Assert.Equal("join", Assert.Single(await verification.MembershipRecords.ToListAsync()).Action);
        }
        finally
        {
            System.IO.File.Delete(database);
        }
    }

    [Fact]
    public async Task LeaveAsync_BannedMemberCannotEraseBanAndRejoin()
    {
        const string podId = "pod:00000000000000000000000000000001";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PodDbContext>().UseSqlite(connection).Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = await factory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.Pods.Add(PodEntity(podId, PodVisibility.Listed));
            context.Members.Add(new PodMemberEntity { PodId = podId, PeerId = "listener", IsBanned = true });
            await context.SaveChangesAsync();
        }
        var service = new SqlitePodService(factory, Mock.Of<IPodPublisher>(), Mock.Of<IPodMembershipSigner>(),
            NullLogger<SqlitePodService>.Instance);

        Assert.False(await service.LeaveAsync(podId, "listener"));
        Assert.False(await service.JoinAsync(podId, new PodMember { PeerId = "listener" }));
        await using var verification = await factory.CreateDbContextAsync();
        Assert.True((await verification.Members.SingleAsync()).IsBanned);
        Assert.Empty(await service.GetMembersAsync(podId));
    }

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

    private sealed class FailingPodInsertInterceptor : DbCommandInterceptor
    {
        private const string FailureMessage = "injected storage failure\r\nforged log entry";

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ThrowForPodInsert(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowForPodInsert(command);
            return ValueTask.FromResult(result);
        }

        private static void ThrowForPodInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO \"Pods\"", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(FailureMessage);
            }
        }
    }

    private sealed class CancelPodInsertInterceptor(CancellationTokenSource cancellation)
        : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            ThrowForPodInsert(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowForPodInsert(command);
            return ValueTask.FromResult(result);
        }

        private void ThrowForPodInsert(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO \"Pods\"", StringComparison.Ordinal))
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }
        }
    }

    private sealed class CancelPodReadInterceptor(CancellationTokenSource cancellation)
        : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Pods\"", StringComparison.OrdinalIgnoreCase))
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }

            return ValueTask.FromResult(result);
        }
    }
}
