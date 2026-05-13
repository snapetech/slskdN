// <copyright file="DownloadServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.Downloads;

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Events;
using slskd.Files;
using slskd.Integrations.FTP;
using slskd.Relay;
using slskd.Tests.Unit;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using Soulseek;
using Xunit;

[Collection(StaticEventCollection.Name)]
public class DownloadServiceTests
{
    [Fact]
    public async Task EnqueueAsync_ExistingInProgressTransfer_IsRejectedWithoutStartingDownload()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.Add(new slskd.Transfers.Transfer
            {
                Id = Guid.NewGuid(),
                Username = "alice",
                Direction = TransferDirection.Download,
                Filename = @"Music\track.flac",
                Size = 1234,
                RequestedAt = DateTime.UtcNow.AddMinutes(-1),
                State = TransferStates.InProgress,
            });
            await context.SaveChangesAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Empty(enqueued);
            Assert.Equal(new[] { @"Music\track.flac" }, failed);
            soulseekClient.Verify(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()), Times.Never);
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task EnqueueAsync_CompletedExistingTransfer_IsSupersededByNewRecord()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var existingId = Guid.NewGuid();

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.Add(new slskd.Transfers.Transfer
            {
                Id = existingId,
                Username = "alice",
                Direction = TransferDirection.Download,
                Filename = @"Music\track.flac",
                Size = 1234,
                RequestedAt = DateTime.UtcNow.AddHours(-1),
                EndedAt = DateTime.UtcNow.AddMinutes(-30),
                State = TransferStates.Completed | TransferStates.Succeeded,
            });
            await context.SaveChangesAsync();
        }

        var soulseekClient = CreateHangingSoulseekClient();
        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            await using var context = new TransfersDbContext(options);
            var existing = await context.Transfers.SingleAsync(t => t.Id == existingId);
            var replacement = await context.Transfers.SingleAsync(t => t.Id == enqueued.Single().Id);

            Assert.True(existing.Removed);
            Assert.False(replacement.Removed);
            Assert.Equal(TransferStates.Queued | TransferStates.Locally, replacement.State);

            Assert.True(service.TryCancel(replacement.Id));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task EnqueueAsync_BackgroundDownloadStartFailure_MarksTransferTerminalFailed()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("synthetic enqueue failure"));

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            var failedTransfer = await WaitForTransferAsync(
                () => service.Find(t => t.Id == enqueued.Single().Id && t.State.HasFlag(TransferStates.Completed)),
                TimeSpan.FromSeconds(5));

            Assert.True(failedTransfer.State.HasFlag(TransferStates.Errored));
            Assert.Contains("synthetic enqueue failure", failedTransfer.Exception, StringComparison.Ordinal);
            Assert.False(service.TryCancel(failedTransfer.Id));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task TryFail_AggregateTimeout_MarksTransferTimedOut()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var transferId = Guid.NewGuid();

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.Add(new slskd.Transfers.Transfer
            {
                Id = transferId,
                Username = "alice",
                Direction = TransferDirection.Download,
                Filename = @"Music\slow.flac",
                Size = 1234,
                RequestedAt = DateTime.UtcNow,
                State = TransferStates.InProgress,
            });
            await context.SaveChangesAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var exception = new AggregateException(new TimeoutException("The wait timed out after 15000 milliseconds"));

            Assert.True(service.TryFail(transferId, exception));

            await using var context = new TransfersDbContext(options);
            var failedTransfer = await context.Transfers.SingleAsync(t => t.Id == transferId);
            Assert.True(failedTransfer.State.HasFlag(TransferStates.Completed));
            Assert.True(failedTransfer.State.HasFlag(TransferStates.TimedOut));
            Assert.False(failedTransfer.State.HasFlag(TransferStates.Errored));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task TryFail_TransferExceptionTimeoutMessage_MarksTransferTimedOut()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var transferId = Guid.NewGuid();

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.Add(new slskd.Transfers.Transfer
            {
                Id = transferId,
                Username = "alice",
                Direction = TransferDirection.Download,
                Filename = @"Music\slow.flac",
                Size = 1234,
                RequestedAt = DateTime.UtcNow,
                State = TransferStates.InProgress,
            });
            await context.SaveChangesAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var exception = new TransferException("The wait timed out after 15000 milliseconds");

            Assert.True(service.TryFail(transferId, exception));

            await using var context = new TransfersDbContext(options);
            var failedTransfer = await context.Transfers.SingleAsync(t => t.Id == transferId);
            Assert.True(failedTransfer.State.HasFlag(TransferStates.Completed));
            Assert.True(failedTransfer.State.HasFlag(TransferStates.TimedOut));
            Assert.False(failedTransfer.State.HasFlag(TransferStates.Errored));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task EnqueueAsync_BackgroundAggregateTimeout_MarksTransferTimedOut()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new AggregateException(new TimeoutException("The wait timed out after 15000 milliseconds")));

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\slow.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            var failedTransfer = await WaitForTransferAsync(
                () => service.Find(t => t.Id == enqueued.Single().Id && t.State.HasFlag(TransferStates.Completed)),
                TimeSpan.FromSeconds(5));

            Assert.True(failedTransfer.State.HasFlag(TransferStates.TimedOut));
            Assert.False(failedTransfer.State.HasFlag(TransferStates.Errored));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Theory]
    [InlineData("reported")]
    [InlineData("size")]
    [InlineData("rejected")]
    public async Task EnqueueAsync_BackgroundExpectedRemoteFailure_MarksTransferTerminalFailed(string failureKind)
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new AggregateException(CreateExpectedRemoteFailure(failureKind)));

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\remote-failed.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            var failedTransfer = await WaitForTransferAsync(
                () => service.Find(t => t.Id == enqueued.Single().Id && t.State.HasFlag(TransferStates.Completed)),
                TimeSpan.FromSeconds(5));

            Assert.True(failedTransfer.State.HasFlag(TransferStates.Errored));
            Assert.False(failedTransfer.State.HasFlag(TransferStates.TimedOut));
        }
        finally
        {
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task EnqueueAsync_SameUserRequests_AreSerializedByUserSemaphore()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var blockingFactory = new BlockingFirstDbContextFactory(options);
        var soulseekClient = CreateHangingSoulseekClient();
        var service = CreateDownloadService(blockingFactory, soulseekClient);

        try
        {
            var first = Task.Run(() => service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\first.flac", Size: 1234L) },
                CancellationToken.None));

            await blockingFactory.FirstCreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var second = Task.Run(() => service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\second.flac", Size: 1234L) },
                CancellationToken.None));

            await Task.Delay(200);

            Assert.False(second.IsCompleted);
            Assert.Equal(1, blockingFactory.CreateCount);

            blockingFactory.ReleaseFirstCreate();

            var (firstEnqueued, firstFailed) = await first.WaitAsync(TimeSpan.FromSeconds(5));
            var (secondEnqueued, secondFailed) = await second.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Single(firstEnqueued);
            Assert.Empty(firstFailed);
            Assert.Single(secondEnqueued);
            Assert.Empty(secondFailed);
            Assert.True(blockingFactory.CreateCount >= 2);

            Assert.True(service.TryCancel(firstEnqueued.Single().Id));
            Assert.True(service.TryCancel(secondEnqueued.Single().Id));
        }
        finally
        {
            blockingFactory.ReleaseFirstCreate();
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task EnqueueAsync_DifferentUsers_CanEnterCriticalSectionConcurrently()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var blockingFactory = new BlockingFirstDbContextFactory(options);
        var soulseekClient = CreateHangingSoulseekClient();
        var service = CreateDownloadService(blockingFactory, soulseekClient);

        try
        {
            var first = Task.Run(() => service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\first.flac", Size: 1234L) },
                CancellationToken.None));

            await blockingFactory.FirstCreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var second = Task.Run(() => service.EnqueueAsync(
                "bob",
                new[] { (Filename: @"Music\second.flac", Size: 1234L) },
                CancellationToken.None));

            await WaitUntilAsync(() => blockingFactory.CreateCount >= 2, TimeSpan.FromSeconds(5));

            blockingFactory.ReleaseFirstCreate();

            var (firstEnqueued, firstFailed) = await first.WaitAsync(TimeSpan.FromSeconds(5));
            var (secondEnqueued, secondFailed) = await second.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Single(firstEnqueued);
            Assert.Empty(firstFailed);
            Assert.Single(secondEnqueued);
            Assert.Empty(secondFailed);

            Assert.True(service.TryCancel(firstEnqueued.Single().Id));
            Assert.True(service.TryCancel(secondEnqueued.Single().Id));
        }
        finally
        {
            blockingFactory.ReleaseFirstCreate();
            service.Dispose();
            DeleteDatabase(databasePath);
        }
    }


    [Fact]
    public async Task EnqueueAsync_DoesNotRequirePeerPreflightConnection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.ConnectToUserAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("peer preflight should not run"));
        soulseekClient
            .Setup(client => client.GetUserEndPointAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("endpoint preflight should not run"));
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long? size,
                long startOffset,
                int? token,
                TransferOptions transferOptions,
                CancellationToken? cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken ?? CancellationToken.None);
                return null!;
            });

        var service = new DownloadService(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            soulseekClient.Object,
            new TestDbContextFactory(options),
            new FileService(new TestOptionsMonitor<slskd.Options>(new slskd.Options())),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            Assert.True(service.TryCancel(enqueued.Single().Id));

            soulseekClient.Verify(client => client.ConnectToUserAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken?>()), Times.Never);
            soulseekClient.Verify(client => client.GetUserEndPointAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken?>()), Times.Never);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task EnqueueAsync_CancelledTransfer_DoesNotFailFromDisposedBatchSemaphore()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var downloadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long? size,
                long startOffset,
                int? token,
                TransferOptions transferOptions,
                CancellationToken? cancellationToken) =>
            {
                downloadStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken ?? CancellationToken.None);
                return null!;
            });

        var service = new DownloadService(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            soulseekClient.Object,
            new TestDbContextFactory(options),
            new FileService(new TestOptionsMonitor<slskd.Options>(new slskd.Options())),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            var transferId = enqueued.Single().Id;
            await downloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitForTransferAsync(
                () => service.Find(t => t.Id == transferId),
                TimeSpan.FromSeconds(5));

            Assert.True(service.TryCancel(transferId));

            var cancelledTransfer = await WaitForTransferAsync(
                () => service.Find(t => t.Id == transferId && t.EndedAt != null),
                TimeSpan.FromSeconds(5));

            Assert.True(cancelledTransfer.State.HasFlag(TransferStates.Completed));
            Assert.DoesNotContain("SemaphoreSlim", cancelledTransfer.Exception ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("disposed object", cancelledTransfer.Exception ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            service.Dispose();

            if (System.IO.File.Exists(databasePath))
            {
                System.IO.File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task Dispose_WhenApplicationIsShuttingDown_DoesNotMarkActiveDownloadFailed()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long? size,
                long startOffset,
                int? token,
                TransferOptions transferOptions,
                CancellationToken? cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken ?? CancellationToken.None);
                return null!;
            });

        var service = new DownloadService(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            soulseekClient.Object,
            new TestDbContextFactory(options),
            new FileService(new TestOptionsMonitor<slskd.Options>(new slskd.Options())),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            SetApplicationShuttingDown(true);
            service.Dispose();
            await Task.Delay(250);

            await using var context = new TransfersDbContext(options);
            var transfer = await context.Transfers.SingleAsync(t => t.Id == enqueued.Single().Id);
            Assert.Null(transfer.EndedAt);
            Assert.False(transfer.State.HasFlag(TransferStates.Completed));
        }
        finally
        {
            SetApplicationShuttingDown(false);
            service.Dispose();

            if (System.IO.File.Exists(databasePath))
            {
                System.IO.File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task ShutdownAsync_WaitsForCancelledDownloadsToDrain()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var downloadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowDrainCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long? size,
                long startOffset,
                int? token,
                TransferOptions transferOptions,
                CancellationToken? cancellationToken) =>
            {
                try
                {
                    downloadStarted.TrySetResult();
                    await Task.Delay(Timeout.Infinite, cancellationToken ?? CancellationToken.None);
                    return null!;
                }
                catch (OperationCanceledException)
                {
                    cancellationObserved.TrySetResult();
                    await allowDrainCompletion.Task;
                    throw;
                }
            });

        var service = new DownloadService(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            soulseekClient.Object,
            new TestDbContextFactory(options),
            new FileService(new TestOptionsMonitor<slskd.Options>(new slskd.Options())),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            await downloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            SetApplicationShuttingDown(true);
            var shutdownTask = service.ShutdownAsync(CancellationToken.None);

            await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(shutdownTask.IsCompleted);

            allowDrainCompletion.TrySetResult();
            await shutdownTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            SetApplicationShuttingDown(false);
            service.Dispose();

            if (System.IO.File.Exists(databasePath))
            {
                System.IO.File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public void Dispose_UnsubscribesClockMinuteHandler()
    {
        var optionsMonitor = new TestOptionsMonitor<slskd.Options>(new slskd.Options());
        var clockEveryMinuteListenersBefore = GetStaticEventInvocationCount(typeof(Clock), "EveryMinute");
        var service = new DownloadService(
            optionsMonitor,
            Mock.Of<ISoulseekClient>(),
            Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<TransfersDbContext>>(),
            new FileService(optionsMonitor),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));

        Assert.Equal(clockEveryMinuteListenersBefore + 1, GetStaticEventInvocationCount(typeof(Clock), "EveryMinute"));

        service.Dispose();

        Assert.Equal(clockEveryMinuteListenersBefore, GetStaticEventInvocationCount(typeof(Clock), "EveryMinute"));
    }

    private static async Task<slskd.Transfers.Transfer> WaitForTransferAsync(Func<slskd.Transfers.Transfer?> finder, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt < timeout)
        {
            var transfer = finder();

            if (transfer is not null)
            {
                return transfer;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Timed out waiting {timeout.TotalSeconds} seconds for transfer state update");
    }

    private static int GetStaticEventInvocationCount(Type type, string eventName)
    {
        var field = type.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{type.FullName}.{eventName} backing field was not found.");

        return (field.GetValue(null) as MulticastDelegate)?.GetInvocationList().Length ?? 0;
    }

    private static void SetApplicationShuttingDown(bool value)
    {
        var property = typeof(slskd.Application).GetProperty("ShuttingDown", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Application.ShuttingDown property was not found.");

        property.SetValue(null, value);
    }

    private static DownloadService CreateDownloadService(
        DbContextOptions<TransfersDbContext> options,
        Mock<ISoulseekClient> soulseekClient)
    {
        var optionsMonitor = new TestOptionsMonitor<slskd.Options>(new slskd.Options());
        var eventService = new Mock<EventService>(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>());
        eventService.Setup(service => service.Add(It.IsAny<EventRecord>()));

        return new DownloadService(
            optionsMonitor,
            soulseekClient.Object,
            new TestDbContextFactory(options),
            new FileService(optionsMonitor),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(eventService.Object));
    }

    private static DownloadService CreateDownloadService(
        Microsoft.EntityFrameworkCore.IDbContextFactory<TransfersDbContext> contextFactory,
        Mock<ISoulseekClient> soulseekClient)
    {
        var optionsMonitor = new TestOptionsMonitor<slskd.Options>(new slskd.Options());
        var eventService = new Mock<EventService>(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>());
        eventService.Setup(service => service.Add(It.IsAny<EventRecord>()));

        return new DownloadService(
            optionsMonitor,
            soulseekClient.Object,
            contextFactory,
            new FileService(optionsMonitor),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(eventService.Object));
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt < timeout)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"Timed out waiting {timeout.TotalSeconds} seconds for predicate");
    }

    private static Exception CreateExpectedRemoteFailure(string failureKind)
        => failureKind switch
        {
            "reported" => new SoulseekClientException(
                "Failed to download file Music\\remote-failed.flac from user alice: Download reported as failed by remote client",
                new TransferReportedFailedException("Download reported as failed by remote client")),
            "size" => new TransferSizeMismatchException("Transfer aborted: the remote size of 2000 does not match expected size 1234", 1234, 2000),
            "rejected" => new TransferRejectedException("Transfer rejected: File not shared."),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind)),
        };

    private static Mock<ISoulseekClient> CreateHangingSoulseekClient()
    {
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(Array.Empty<Soulseek.Transfer>());
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long?>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long? size,
                long startOffset,
                int? token,
                TransferOptions transferOptions,
                CancellationToken? cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken ?? CancellationToken.None);
                return null!;
            });

        return soulseekClient;
    }

    private static void DeleteDatabase(string databasePath)
    {
        if (System.IO.File.Exists(databasePath))
        {
            System.IO.File.Delete(databasePath);
        }
    }

    private sealed class TestDbContextFactory : Microsoft.EntityFrameworkCore.IDbContextFactory<TransfersDbContext>
    {
        private readonly DbContextOptions<TransfersDbContext> _options;

        public TestDbContextFactory(DbContextOptions<TransfersDbContext> options)
        {
            _options = options;
        }

        public TransfersDbContext CreateDbContext() => new(_options);
    }

    private sealed class BlockingFirstDbContextFactory : Microsoft.EntityFrameworkCore.IDbContextFactory<TransfersDbContext>
    {
        private readonly DbContextOptions<TransfersDbContext> _options;
        private readonly ManualResetEventSlim _releaseFirstCreate = new(initialState: false);
        private int _createCount;

        public BlockingFirstDbContextFactory(DbContextOptions<TransfersDbContext> options)
        {
            _options = options;
        }

        public TaskCompletionSource FirstCreateStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CreateCount => Volatile.Read(ref _createCount);

        public TransfersDbContext CreateDbContext()
        {
            var count = Interlocked.Increment(ref _createCount);
            if (count == 1)
            {
                FirstCreateStarted.TrySetResult();
                _releaseFirstCreate.Wait(TimeSpan.FromSeconds(5));
            }

            return new TransfersDbContext(_options);
        }

        public void ReleaseFirstCreate()
        {
            _releaseFirstCreate.Set();
        }
    }
}
