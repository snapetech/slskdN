// <copyright file="DownloadServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.Downloads;

using System;
using System.Collections.Generic;
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
using slskd.HashDb;
using slskd.HashDb.Models;
using slskd.Integrations.FTP;
using slskd.Relay;
using slskd.Tests.Unit;
using slskd.Transfers;
using slskd.Transfers.AutoReplace;
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
    public async Task EnqueueAsync_CompletedSoulseekClientTransfer_DoesNotBlockRetry()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var staleClientTransfer = new Soulseek.Transfer(
            TransferDirection.Download,
            "alice",
            @"Music\track.flac",
            token: 1,
            state: TransferStates.Completed | TransferStates.TimedOut,
            size: 1234,
            startOffset: 0,
            bytesTransferred: 0);
        var downloadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .SetupGet(client => client.Downloads)
            .Returns(new[] { staleClientTransfer });
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

        var service = CreateDownloadService(options, soulseekClient);

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\track.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            await downloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            soulseekClient.Verify(client => client.DownloadAsync(
                "alice",
                @"Music\track.flac",
                It.IsAny<Func<Task<Stream>>>(),
                1234,
                0,
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()), Times.Once);
            Assert.True(service.TryCancel(enqueued.Single().Id));
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
    [InlineData("connection-reset")]
    [InlineData("remote-closed")]
    [InlineData("message-connection")]
    [InlineData("transfer-connection")]
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
    public async Task EnqueueAsync_BackgroundExpectedRemoteFailure_DoesNotLeakCleanupAggregateTaskFault()
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
            .ThrowsAsync(new AggregateException(CreateExpectedRemoteFailure("rejected")));

        var service = CreateDownloadService(options, soulseekClient);
        var unobservedExceptions = new List<Exception>();

        void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            unobservedExceptions.Add(args.Exception);
            args.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            var (enqueued, failed) = await service.EnqueueAsync(
                "alice",
                new[] { (Filename: @"Music\remote-failed.flac", Size: 1234L) },
                CancellationToken.None);

            Assert.Single(enqueued);
            Assert.Empty(failed);

            _ = await WaitForTransferAsync(
                () => service.Find(t => t.Id == enqueued.Single().Id && t.State.HasFlag(TransferStates.Completed)),
                TimeSpan.FromSeconds(5));

            service.Dispose();
            service = null!;

            await ForceTaskFinalizersAsync();

            Assert.DoesNotContain(
                unobservedExceptions,
                exception => exception.ToString().Contains("File not shared", StringComparison.Ordinal));
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            service?.Dispose();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public void CreateRetryPlan_RespectsGlobalPerPeerAndCooldownBudgets()
    {
        var now = DateTime.UtcNow;
        var opts = new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions();
        var transfers = new[]
        {
            CreateFailedDownload("alice", "a-1.flac", now.AddMinutes(-40)),
            CreateFailedDownload("alice", "a-2.flac", now.AddMinutes(-39)),
            CreateFailedDownload("bob", "b-1.flac", now.AddMinutes(-38)),
            CreateFailedDownload("carol", "c-1.flac", now.AddMinutes(-37)),
            CreateFailedDownload("dave", "d-1.flac", now.AddMinutes(-36)),
            CreateFailedDownload("erin", "e-1.flac", now.AddMinutes(-35)),
            CreateFailedDownload("frank", "f-1.flac", now.AddMinutes(-34)),
            CreateFailedDownload("grace", "g-1.flac", now.AddMinutes(-33)),
            CreateFailedDownload("heidi", "h-1.flac", now.AddMinutes(-32)),
            CreateFailedDownload("ivan", "i-1.flac", now.AddMinutes(-31)),
            CreateFailedDownload("judy", "j-1.flac", now.AddMinutes(-30)),
            CreateFailedDownload("mallory", "m-1.flac", now.AddMinutes(-29)),
        };

        var plan = DownloadAutoRetryService.CreateRetryPlan(
            transfers,
            new HashSet<Guid>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, int>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(
                new[] { new KeyValuePair<string, DateTime>("carol", now.AddMinutes(5)) },
                StringComparer.OrdinalIgnoreCase),
            opts,
            now);

        Assert.Equal(10, plan.Count);
        Assert.DoesNotContain(plan, t => t.Username == "carol");
        Assert.Single(plan.Where(t => t.Username == "alice"));
        Assert.All(
            plan.GroupBy(t => t.Username, StringComparer.OrdinalIgnoreCase),
            group => Assert.True(group.Count() <= opts.MaxFilesPerPeerPerCycle));
    }

    [Fact]
    public void CreateRetryPlan_SkipsAlreadyRetriedAndFiniteMaxAttemptFiles()
    {
        var now = DateTime.UtcNow;
        var alreadyRetried = CreateFailedDownload("alice", "old.flac", now.AddMinutes(-40));
        var maxed = CreateFailedDownload("bob", "maxed.flac", now.AddMinutes(-39));
        var eligible = CreateFailedDownload("carol", "ok.flac", now.AddMinutes(-38));
        var retryCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        var opts = new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions { MaxAttempts = 5 };
        retryCounts[$"{maxed.Username}:{maxed.Filename}"] = opts.MaxAttempts;

        var plan = DownloadAutoRetryService.CreateRetryPlan(
            new[] { alreadyRetried, maxed, eligible },
            new HashSet<Guid> { alreadyRetried.Id },
            retryCounts,
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(),
            opts,
            now);

        Assert.Equal(new[] { eligible.Id }, plan.Select(t => t.Id));
    }

    [Fact]
    public void CreateRetryPlan_DefaultMaxAttemptsStopsAfterBoundedRetries()
    {
        var now = DateTime.UtcNow;
        var retriedManyTimes = CreateFailedDownload("alice", "forever.flac", now.AddMinutes(-40));
        var retryCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        retryCounts[$"{retriedManyTimes.Username}:{retriedManyTimes.Filename}"] = 500;

        var plan = DownloadAutoRetryService.CreateRetryPlan(
            new[] { retriedManyTimes },
            new HashSet<Guid>(),
            retryCounts,
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(),
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now);

        Assert.Empty(plan);
    }

    [Fact]
    public void CreateRetryPlan_SkipsNonAudioSidecars()
    {
        var now = DateTime.UtcNow;
        var cover = CreateFailedDownload("alice", @"Album\cover.jpg", now.AddMinutes(-40), size: 1234);
        var log = CreateFailedDownload("bob", @"Album\album.log", now.AddMinutes(-39), size: 2345);
        var track = CreateFailedDownload("carol", @"Album\01 Track.flac", now.AddMinutes(-38), size: 3456);

        var plan = DownloadAutoRetryService.CreateRetryPlan(
            new[] { cover, log, track },
            new HashSet<Guid>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, int>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(),
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now);

        Assert.Equal(new[] { track.Id }, plan.Select(t => t.Id));
    }

    [Fact]
    public async Task CreateRetryPlanAsync_StopsAfterDefaultPlanIsFinal()
    {
        var now = DateTime.UtcNow;
        var enumerated = 0;
        var transfers = Enumerable.Range(0, 50)
            .Select(index => CreateFailedDownload($"peer-{index}", $"track-{index}.flac", now.AddMinutes(index - 100)))
            .ToList();

        var plan = await DownloadAutoRetryService.CreateRetryPlanAsync(
            ToAsyncSequence(transfers, () => enumerated++),
            new HashSet<Guid>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, int>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(),
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            CancellationToken.None);

        Assert.Equal(10, plan.Count);
        Assert.Equal(10, enumerated);
        Assert.Equal(transfers.Take(10).Select(transfer => transfer.Id), plan.Select(transfer => transfer.Id));
    }

    [Fact]
    public async Task CreateRetryPlanAsync_WaitsForEarlierUnderfilledPeerGroups()
    {
        var now = DateTime.UtcNow;
        var aliceFirst = CreateFailedDownload("alice", "alice-1.flac", now.AddMinutes(-50));
        var bob = CreateFailedDownload("bob", "bob.flac", now.AddMinutes(-49));
        var carol = CreateFailedDownload("carol", "carol.flac", now.AddMinutes(-48));
        var aliceSecond = CreateFailedDownload("alice", "alice-2.flac", now.AddMinutes(-47));
        var dave = CreateFailedDownload("dave", "dave.flac", now.AddMinutes(-46));
        var ordered = new[] { aliceFirst, bob, carol, aliceSecond, dave };
        var enumerated = 0;
        var opts = new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions
        {
            MaxFilesPerCycle = 3,
            MaxFilesPerPeerPerCycle = 2,
        };

        var plan = await DownloadAutoRetryService.CreateRetryPlanAsync(
            ToAsyncSequence(ordered, () => enumerated++),
            new HashSet<Guid>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, int>(),
            new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>(),
            opts,
            now,
            CancellationToken.None);

        Assert.Equal(new[] { aliceFirst.Id, aliceSecond.Id, bob.Id }, plan.Select(transfer => transfer.Id));
        Assert.Equal(4, enumerated);
    }

    [Fact]
    public async Task StreamAutoRetryCandidatesAsync_FiltersAndOrdersInDatabase()
    {
        var databasePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var now = DateTime.UtcNow;
        var oldest = CreateFailedDownload("alice", "oldest.flac", now.AddHours(-3));
        var newer = CreateFailedDownload("bob", "newer.flac", now.AddHours(-2));
        var removed = CreateFailedDownload("carol", "removed.flac", now.AddHours(-4));
        removed.Removed = true;
        var succeeded = CreateFailedDownload("dave", "succeeded.flac", now.AddHours(-4));
        succeeded.State = TransferStates.Completed | TransferStates.Succeeded;
        var cancelled = CreateFailedDownload("erin", "cancelled.flac", now.AddHours(-4));
        cancelled.State = TransferStates.Completed | TransferStates.Cancelled;
        var rejected = CreateFailedDownload("frank", "rejected.flac", now.AddHours(-4));
        rejected.State = TransferStates.Completed | TransferStates.Rejected;
        var tooRecent = CreateFailedDownload("grace", "recent.flac", now.AddMinutes(-5));
        var upload = CreateFailedDownload("heidi", "upload.flac", now.AddHours(-4));
        upload = new slskd.Transfers.Transfer
        {
            Id = upload.Id,
            Username = upload.Username,
            Direction = TransferDirection.Upload,
            Filename = upload.Filename,
            Size = upload.Size,
            RequestedAt = upload.RequestedAt,
            EndedAt = upload.EndedAt,
            State = upload.State,
        };

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.AddRange(oldest, newer, removed, succeeded, cancelled, rejected, tooRecent, upload);
            await context.SaveChangesAsync();
        }

        try
        {
            using var service = CreateDownloadService(options, new Mock<ISoulseekClient>());
            var candidates = new List<slskd.Transfers.Transfer>();
            await foreach (var candidate in service.StreamAutoRetryCandidatesAsync(now.AddHours(-1)))
            {
                candidates.Add(candidate);
            }

            Assert.Equal(new[] { oldest.Id, newer.Id }, candidates.Select(candidate => candidate.Id));
            Assert.All(candidates, candidate => Assert.Null(candidate.RequestId));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task ResolveRetryTargetAsync_PrefersCooledDownHashDbAlternate()
    {
        var now = DateTime.UtcNow;
        var failed = CreateFailedDownload("alice", @"Album\track.flac", now.AddMinutes(-40), size: 1234);
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(x => x.GetFlacEntriesBySizeAsync(1234, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new FlacInventoryEntry { PeerId = "alice", Path = @"Album\track.flac", Size = 1234 },
                new FlacInventoryEntry { PeerId = "mallory", Path = @"Other\wrong-track.flac", Size = 1234 },
                new FlacInventoryEntry { PeerId = "bob", Path = @"Other\track.flac", Size = 1234 },
            });

        var service = CreateAutoRetryService(hashDb: hashDb.Object);

        var target = await service.ResolveRetryTargetAsync(
            failed,
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            allowNetworkSearch: true,
            CancellationToken.None);

        Assert.Equal("bob", target.Username);
        Assert.Equal(@"Other\track.flac", target.Filename);
        Assert.Equal("hashdb", target.SourceKind);
        Assert.False(target.UsedNetworkSearch);
    }

    [Fact]
    public async Task ResolveRetryTargetAsync_IgnoresHashDbAlternatesWithDifferentFilename()
    {
        var now = DateTime.UtcNow;
        var failed = CreateFailedDownload("alice", @"Album\track.flac", now.AddMinutes(-40), size: 1234);
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(x => x.GetFlacEntriesBySizeAsync(1234, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new FlacInventoryEntry { PeerId = "mallory", Path = @"Other\wrong-track.flac", Size = 1234 },
            });

        var service = CreateAutoRetryService(hashDb: hashDb.Object);

        var target = await service.ResolveRetryTargetAsync(
            failed,
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            allowNetworkSearch: false,
            CancellationToken.None);

        Assert.Equal("alice", target.Username);
        Assert.Equal(@"Album\track.flac", target.Filename);
        Assert.Equal("original", target.SourceKind);
    }

    [Fact]
    public async Task ResolveRetryTargetAsync_UsesBoundedSearchWhenLocalCandidateUnavailable()
    {
        var now = DateTime.UtcNow;
        var failed = CreateFailedDownload("alice", @"Album\track.flac", now.AddMinutes(-40), size: 1234);
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(x => x.GetFlacEntriesBySizeAsync(1234, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FlacInventoryEntry>());

        var autoReplace = new Mock<IAutoReplaceService>();
        autoReplace.Setup(x => x.FindAlternativesAsync(
                It.Is<FindAlternativeRequest>(r => r.Username == "alice" && r.Filename == failed.Filename && r.Size == failed.Size),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlternativeCandidate>
            {
                new() { Username = "carol", Filename = @"Other\track.flac", Size = 1234 },
            });

        var service = CreateAutoRetryService(hashDb: hashDb.Object, autoReplace: autoReplace.Object);

        var target = await service.ResolveRetryTargetAsync(
            failed,
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            allowNetworkSearch: true,
            CancellationToken.None);

        Assert.Equal("carol", target.Username);
        Assert.Equal("search", target.SourceKind);
        Assert.True(target.UsedNetworkSearch);
    }

    [Fact]
    public async Task ResolveRetryTargetAsync_FallsBackToOriginalWhenNetworkSearchBudgetUnavailable()
    {
        var now = DateTime.UtcNow;
        var failed = CreateFailedDownload("alice", @"Album\track.flac", now.AddMinutes(-40), size: 1234);
        var autoReplace = new Mock<IAutoReplaceService>(MockBehavior.Strict);
        var service = CreateAutoRetryService(autoReplace: autoReplace.Object);

        var target = await service.ResolveRetryTargetAsync(
            failed,
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            allowNetworkSearch: false,
            CancellationToken.None);

        Assert.Equal("alice", target.Username);
        Assert.Equal(failed.Filename, target.Filename);
        Assert.Equal("original", target.SourceKind);
        Assert.False(target.UsedNetworkSearch);
    }

    [Fact]
    public async Task ResolveRetryTargetAsync_ConsumesNetworkSearchBudgetWhenNoAlternativeFound()
    {
        var now = DateTime.UtcNow;
        var failed = CreateFailedDownload("alice", @"Album\track.flac", now.AddMinutes(-40), size: 1234);
        var autoReplace = new Mock<IAutoReplaceService>();
        autoReplace.Setup(x => x.FindAlternativesAsync(It.IsAny<FindAlternativeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlternativeCandidate>());

        var service = CreateAutoRetryService(autoReplace: autoReplace.Object);

        var target = await service.ResolveRetryTargetAsync(
            failed,
            new slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions(),
            now,
            allowNetworkSearch: true,
            CancellationToken.None);

        Assert.Equal("alice", target.Username);
        Assert.Equal("original", target.SourceKind);
        Assert.True(target.UsedNetworkSearch);
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

    [Fact]
    public void ResolveCompletedDestinationDirectory_Default_UsesRemoteFolder()
    {
        var options = CreateDownloadLayoutOptions();
        var transfer = new slskd.Transfers.Transfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Direction = TransferDirection.Download,
            Filename = @"Root\Artist - Album\01 Song.flac",
            BatchId = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow,
        };

        using var service = CreateDownloadServiceForLayoutTest(options);
        var destination = ResolveCompletedDestinationDirectory(service, transfer);

        Assert.Equal(
            System.IO.Path.Combine(options.Directories.Downloads, "Artist - Album"),
            destination);
    }

    [Fact]
    public void ResolveCompletedDestinationDirectory_BatchId_UsesBatchIdWhenExplicitlyConfigured()
    {
        var options = CreateDownloadLayoutOptions("batch_id");
        var batchId = Guid.NewGuid();
        var transfer = new slskd.Transfers.Transfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Direction = TransferDirection.Download,
            Filename = @"Root\Artist - Album\01 Song.flac",
            BatchId = batchId,
            RequestedAt = DateTime.UtcNow,
        };

        using var service = CreateDownloadServiceForLayoutTest(options);
        var destination = ResolveCompletedDestinationDirectory(service, transfer);

        Assert.Equal(
            System.IO.Path.Combine(options.Directories.Downloads, batchId.ToString()),
            destination);
    }

    [Fact]
    public void ResolveCompletedDestinationDirectory_UploaderFolder_UsesUploaderAndRemoteParentFolder()
    {
        var options = CreateDownloadLayoutOptions("uploader_folder");
        var transfer = new slskd.Transfers.Transfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Direction = TransferDirection.Download,
            Filename = @"Root\Artist - Album\01 Song.flac",
            RequestedAt = DateTime.UtcNow,
        };

        using var service = CreateDownloadServiceForLayoutTest(options);
        var destination = ResolveCompletedDestinationDirectory(service, transfer);

        Assert.Equal(
            System.IO.Path.Combine(options.Directories.Downloads, "alice", "Artist - Album"),
            destination);
    }

    private static slskd.Options CreateDownloadLayoutOptions(string? completedLayout = null)
    {
        return new slskd.Options
        {
            Directories = new slskd.Options.DirectoriesOptions
            {
                Downloads = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"slskdn-download-layout-{Guid.NewGuid():N}"),
            },
            Global = new slskd.Options.GlobalOptions
            {
                Download = completedLayout is null
                    ? new slskd.Options.GlobalOptions.GlobalDownloadOptions()
                    : new slskd.Options.GlobalOptions.GlobalDownloadOptions
                    {
                        CompletedLayout = completedLayout,
                    },
            },
        };
    }

    private static DownloadService CreateDownloadServiceForLayoutTest(slskd.Options options)
    {
        var optionsMonitor = new TestOptionsMonitor<slskd.Options>(options);
        return new DownloadService(
            optionsMonitor,
            Mock.Of<ISoulseekClient>(),
            Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<TransfersDbContext>>(),
            new FileService(optionsMonitor),
            Mock.Of<IRelayService>(),
            Mock.Of<IFTPService>(),
            new EventBus(new EventService(Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<EventsDbContext>>())));
    }

    private static string ResolveCompletedDestinationDirectory(DownloadService service, slskd.Transfers.Transfer transfer)
    {
        var method = typeof(DownloadService).GetMethod("ResolveCompletedDestinationDirectory", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveCompletedDestinationDirectory was not found.");

        return Assert.IsType<string>(method.Invoke(service, [transfer, null]));
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

    private static async Task ForceTaskFinalizersAsync()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(50);
        }
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

    private static DownloadAutoRetryService CreateAutoRetryService(
        IHashDbService? hashDb = null,
        IAutoReplaceService? autoReplace = null)
    {
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient.SetupGet(client => client.State).Returns(SoulseekClientStates.Connected);

        return new DownloadAutoRetryService(
            Mock.Of<IDownloadService>(),
            soulseekClient.Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            hashDb,
            autoReplace);
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
            "connection-reset" => new SoulseekClientException(
                "Failed to download file Music\\remote-failed.flac from user alice: Transfer failed: Read error: Unable to read data from the transport connection: Connection reset by peer.",
                new ConnectionException("Transfer failed: Read error: Unable to read data from the transport connection: Connection reset by peer.")),
            "remote-closed" => new SoulseekClientException(
                "Failed to download file Music\\remote-failed.flac from user alice: Transfer failed: Read error: Remote connection closed",
                new ConnectionException("Transfer failed: Read error: Remote connection closed")),
            "message-connection" => new SoulseekClientException(
                "Failed to download file Music\\remote-failed.flac from user alice: Failed to establish a direct or indirect message connection to alice (203.0.113.10:50300)",
                new ConnectionException("Failed to establish a direct or indirect message connection to alice (203.0.113.10:50300)")),
            "transfer-connection" => new SoulseekClientException(
                "Failed to download file Music\\remote-failed.flac from user alice: Failed to establish a direct or indirect transfer connection to alice (203.0.113.10:50300)",
                new ConnectionException("Failed to establish a direct or indirect transfer connection to alice (203.0.113.10:50300)")),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind)),
        };

    private static slskd.Transfers.Transfer CreateFailedDownload(string username, string filename, DateTime endedAt, long size = 1234)
        => new()
        {
            Id = Guid.NewGuid(),
            Username = username,
            Direction = TransferDirection.Download,
            Filename = filename,
            Size = size,
            RequestedAt = endedAt.AddMinutes(-5),
            EndedAt = endedAt,
            State = TransferStates.Completed | TransferStates.Errored,
        };

    private static async IAsyncEnumerable<slskd.Transfers.Transfer> ToAsyncSequence(
        IEnumerable<slskd.Transfers.Transfer> transfers,
        Action onEnumerated,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var transfer in transfers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onEnumerated();
            yield return transfer;
            await Task.Yield();
        }
    }

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
