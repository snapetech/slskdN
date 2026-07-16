// <copyright file="TransferServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Transfers;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using slskd.Transfers.Uploads;
using Soulseek;
using Xunit;
using SlskdTransfer = slskd.Transfers.Transfer;

public sealed class TransferServiceTests
{
    [Fact]
    public void GetSpeedSnapshot_ProjectsActiveSpeedsAndAggregatesRetainedBytes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        var fallbackStartedAt = DateTime.UtcNow.AddSeconds(-10);
        using (var context = contextFactory.CreateDbContext())
        {
            context.Database.EnsureCreated();
            context.Transfers.AddRange(
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.InProgress,
                    bytesTransferred: 1_000,
                    averageSpeed: 1_500),
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.InProgress,
                    bytesTransferred: 20_000,
                    startedAt: fallbackStartedAt),
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.Completed | TransferStates.Succeeded,
                    bytesTransferred: 30_000,
                    removed: true),
                CreateTransfer(
                    TransferDirection.Upload,
                    TransferStates.InProgress,
                    bytesTransferred: 4_000,
                    averageSpeed: 3_000),
                CreateTransfer(
                    TransferDirection.Upload,
                    TransferStates.Completed | TransferStates.Succeeded,
                    bytesTransferred: 50_000));
            context.SaveChanges();
        }
        commandCapture.Commands.Clear();

        var service = new TransferService(
            Mock.Of<IUploadService>(),
            Mock.Of<IDownloadService>(),
            contextFactory);

        var callStartedAt = DateTime.UtcNow;
        var snapshot = service.GetSpeedSnapshot();
        var callEndedAt = DateTime.UtcNow;
        var minimumDownloadSpeed = 1_500 + (20_000 / (callEndedAt - fallbackStartedAt).TotalSeconds);
        var maximumDownloadSpeed = 1_500 + (20_000 / (callStartedAt - fallbackStartedAt).TotalSeconds);

        Assert.InRange(snapshot.DownloadSpeed, minimumDownloadSpeed - 1, maximumDownloadSpeed + 1);
        Assert.Equal(3_000, snapshot.UploadSpeed);
        Assert.Equal(51_000, snapshot.DownloadedBytes);
        Assert.Equal(54_000, snapshot.UploadedBytes);
        Assert.Equal(2, commandCapture.Commands.Count);
        Assert.Contains(commandCapture.Commands, command =>
            command.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(commandCapture.Commands, command =>
            command.Contains("Filename", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetUserDownloadStatsAsync_AggregatesRetainedDownloadsInSql()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        var lastDownloadAt = DateTime.UtcNow.AddMinutes(-1);
        await using (var context = contextFactory.CreateDbContext())
        {
            await context.Database.EnsureCreatedAsync();
            context.Transfers.AddRange(
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.Completed | TransferStates.Succeeded,
                    bytesTransferred: 30_000,
                    username: "alice",
                    endedAt: lastDownloadAt),
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.Completed,
                    bytesTransferred: 10_000,
                    username: "alice",
                    endedAt: lastDownloadAt.AddMinutes(-1)),
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.InProgress,
                    bytesTransferred: 5_000,
                    username: "bob"),
                CreateTransfer(
                    TransferDirection.Download,
                    TransferStates.Completed | TransferStates.Succeeded,
                    bytesTransferred: 40_000,
                    username: "removed",
                    removed: true),
                CreateTransfer(
                    TransferDirection.Upload,
                    TransferStates.Completed | TransferStates.Succeeded,
                    bytesTransferred: 50_000,
                    username: "uploader"));
            await context.SaveChangesAsync();
        }
        commandCapture.Commands.Clear();

        var service = new TransferService(
            Mock.Of<IUploadService>(),
            Mock.Of<IDownloadService>(),
            contextFactory);

        var stats = await service.GetUserDownloadStatsAsync();

        Assert.Equal(2, stats.Count);
        Assert.Equal(2, stats["alice"].TotalDownloads);
        Assert.Equal(1, stats["alice"].SuccessfulDownloads);
        Assert.Equal(1, stats["alice"].FailedDownloads);
        Assert.Equal(30_000, stats["alice"].TotalBytes);
        Assert.Equal(lastDownloadAt, stats["alice"].LastDownloadAt);
        Assert.Equal(1, stats["bob"].TotalDownloads);
        Assert.Equal(0, stats["bob"].SuccessfulDownloads);
        Assert.Equal(0, stats["bob"].FailedDownloads);
        Assert.Single(commandCapture.Commands);
        Assert.Contains("GROUP BY", commandCapture.Commands[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Filename", commandCapture.Commands[0], StringComparison.OrdinalIgnoreCase);
    }

    private static SlskdTransfer CreateTransfer(
        TransferDirection direction,
        TransferStates state,
        long bytesTransferred,
        double averageSpeed = 0,
        DateTime? startedAt = null,
        bool removed = false,
        string username = "listener",
        DateTime? endedAt = null)
    {
        return new SlskdTransfer
        {
            AverageSpeed = averageSpeed,
            BytesTransferred = bytesTransferred,
            Direction = direction,
            EndedAt = endedAt,
            Filename = $"Music/{Guid.NewGuid():N}.flac",
            Id = Guid.NewGuid(),
            Removed = removed,
            RequestedAt = DateTime.UtcNow,
            StartedAt = startedAt,
            State = state,
            Username = username,
        };
    }

    private sealed class TestDbContextFactory : IDbContextFactory<TransfersDbContext>
    {
        public TestDbContextFactory(DbContextOptions<TransfersDbContext> options)
        {
            Options = options;
        }

        private DbContextOptions<TransfersDbContext> Options { get; }

        public TransfersDbContext CreateDbContext()
        {
            return new TransfersDbContext(Options);
        }
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

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
