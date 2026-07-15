// <copyright file="TransferServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Transfers;

using System;
using System.Collections.Generic;
using System.Data.Common;
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
                    startedAt: DateTime.UtcNow.AddSeconds(-10)),
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

        var snapshot = service.GetSpeedSnapshot();

        Assert.InRange(snapshot.DownloadSpeed, 3_400, 3_600);
        Assert.Equal(3_000, snapshot.UploadSpeed);
        Assert.Equal(51_000, snapshot.DownloadedBytes);
        Assert.Equal(54_000, snapshot.UploadedBytes);
        Assert.Equal(2, commandCapture.Commands.Count);
        Assert.Contains(commandCapture.Commands, command =>
            command.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(commandCapture.Commands, command =>
            command.Contains("Filename", StringComparison.OrdinalIgnoreCase));
    }

    private static SlskdTransfer CreateTransfer(
        TransferDirection direction,
        TransferStates state,
        long bytesTransferred,
        double averageSpeed = 0,
        DateTime? startedAt = null,
        bool removed = false)
    {
        return new SlskdTransfer
        {
            AverageSpeed = averageSpeed,
            BytesTransferred = bytesTransferred,
            Direction = direction,
            Filename = $"Music/{Guid.NewGuid():N}.flac",
            Id = Guid.NewGuid(),
            Removed = removed,
            RequestedAt = DateTime.UtcNow,
            StartedAt = startedAt,
            State = state,
            Username = "listener",
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
    }
}
