// <copyright file="DownloadRequestsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.Downloads;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using slskd.Transfers.Downloads.API;
using Soulseek;
using Xunit;
using SlskdTransfer = slskd.Transfers.Transfer;

public class DownloadRequestsControllerTests
{
    [Fact]
    public async Task List_AggregatesAttemptHistoryAndHydratesOnlyCurrentAttempts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var materialization = new TransferMaterializationInterceptor();
        var options = new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(materialization)
            .Options;
        var now = DateTime.UtcNow;
        var activeWithHistory = new DownloadRequest
        {
            Id = Guid.NewGuid(),
            Name = "active-history",
            OriginalFilename = "Music/history.flac",
            State = DownloadRequestState.Active,
            CreatedAt = now.AddMinutes(-2),
        };
        var activeWithoutAttempts = new DownloadRequest
        {
            Id = Guid.NewGuid(),
            Name = "active-empty",
            OriginalFilename = "Music/empty.flac",
            State = DownloadRequestState.Active,
            CreatedAt = now.AddMinutes(-1),
        };
        var activeWithOnlyRemovedAttempts = new DownloadRequest
        {
            Id = Guid.NewGuid(),
            Name = "active-removed",
            OriginalFilename = "Music/removed.flac",
            State = DownloadRequestState.Active,
            CreatedAt = now.AddMinutes(-1.5),
        };
        var completed = new DownloadRequest
        {
            Id = Guid.NewGuid(),
            Name = "completed",
            OriginalFilename = "Music/completed.flac",
            State = DownloadRequestState.Completed,
            CreatedAt = now,
        };
        var expectedCurrentId = Guid.NewGuid();
        var expectedRemovedCurrentId = Guid.NewGuid();

        await using (var context = new TransfersDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.DownloadRequests.AddRange(
                activeWithHistory,
                activeWithoutAttempts,
                activeWithOnlyRemovedAttempts,
                completed);
            context.Transfers.AddRange(Enumerable.Range(0, 50).Select(index => new SlskdTransfer
            {
                Id = index == 1 ? expectedCurrentId : Guid.NewGuid(),
                RequestId = activeWithHistory.Id,
                Username = $"active-peer-{index:D2}",
                Direction = TransferDirection.Download,
                Filename = $"Music/active-{index:D2}.flac",
                RequestedAt = now.AddMinutes(index),
                Removed = index != 0 && index != 1,
            }));
            context.Transfers.AddRange(Enumerable.Range(0, 50).Select(index => new SlskdTransfer
            {
                Id = Guid.NewGuid(),
                RequestId = completed.Id,
                Username = $"completed-peer-{index:D2}",
                Direction = TransferDirection.Download,
                Filename = $"Music/completed-{index:D2}.flac",
                RequestedAt = now.AddMinutes(index),
                Removed = false,
            }));
            context.Transfers.AddRange(Enumerable.Range(0, 3).Select(index => new SlskdTransfer
            {
                Id = index == 2 ? expectedRemovedCurrentId : Guid.NewGuid(),
                RequestId = activeWithOnlyRemovedAttempts.Id,
                Username = $"removed-peer-{index:D2}",
                Direction = TransferDirection.Download,
                Filename = $"Music/removed-{index:D2}.flac",
                RequestedAt = now.AddMinutes(index),
                Removed = true,
            }));
            await context.SaveChangesAsync();
        }

        materialization.TransferCount = 0;
        var controller = new DownloadRequestsController(
            new TestDbContextFactory(options),
            Mock.Of<IDownloadService>());

        var result = await controller.List("Active");

        var response = Assert.IsType<OkObjectResult>(result);
        var summaries = Assert.IsType<List<DownloadRequestSummary>>(response.Value);
        Assert.Equal(
            new[] { activeWithoutAttempts.Id, activeWithOnlyRemovedAttempts.Id, activeWithHistory.Id },
            summaries.Select(summary => summary.Request.Id));
        Assert.Equal(0, summaries[0].AttemptCount);
        Assert.Null(summaries[0].Current);
        Assert.Equal(3, summaries[1].AttemptCount);
        Assert.Equal(expectedRemovedCurrentId, summaries[1].Current!.Id);
        Assert.Equal(50, summaries[2].AttemptCount);
        Assert.Equal(expectedCurrentId, summaries[2].Current!.Id);
        Assert.Equal(2, materialization.TransferCount);
    }

    private sealed class TestDbContextFactory(DbContextOptions<TransfersDbContext> options)
        : IDbContextFactory<TransfersDbContext>
    {
        public TransfersDbContext CreateDbContext() => new(options);
    }

    private sealed class TransferMaterializationInterceptor : IMaterializationInterceptor
    {
        public int TransferCount { get; set; }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            if (entity is SlskdTransfer)
            {
                TransferCount++;
            }

            return entity;
        }
    }
}
