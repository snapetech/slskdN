// <copyright file="WishlistServicePersistenceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Wishlist;

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Search;
using slskd.Transfers.Downloads;
using slskd.Transfers.Ranking;
using slskd.Wishlist;
using Soulseek;
using Xunit;

public sealed class WishlistServicePersistenceTests
{
    [Fact]
    public async Task CreateManyAsync_WithOneHundredItems_UsesThreeMultiRowCommands()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.WishlistItems.AddRange(Enumerable.Range(1, 100).Select(index => new WishlistItem
            {
                SearchText = $"baseline-{index}",
            }));
            await context.SaveChangesAsync();
            Assert.Equal(100, commandCapture.Commands.Count);
            await context.WishlistItems.ExecuteDeleteAsync();
        }
        commandCapture.Commands.Clear();
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.SetupGet(monitor => monitor.CurrentValue).Returns(new slskd.Options());
        using var service = new WishlistService(
            contextFactory,
            Mock.Of<ISearchService>(),
            Mock.Of<ISoulseekClient>(),
            optionsMonitor.Object,
            Mock.Of<ISourceRankingService>(),
            Mock.Of<IDownloadService>());
        var originalId = Guid.NewGuid();
        var items = Enumerable.Range(1, 100)
            .Select(index => new WishlistItem
            {
                Id = originalId,
                SearchText = $"search-{index}",
                Filter = "flac",
            })
            .ToList();
        var lastSearchId = Guid.NewGuid();
        var lastActivityAt = DateTime.UtcNow.AddMinutes(-5);
        items[0].Enabled = false;
        items[0].AutoDownload = true;
        items[0].MaxResults = 25;
        items[0].LastSearchedAt = lastActivityAt;
        items[0].LastMatchCount = 8;
        items[0].LastVisibleHitCount = 7;
        items[0].LastHiddenLockedHitCount = 6;
        items[0].LastFilteredOutHitCount = 5;
        items[0].LastIgnoredResultHitCount = 4;
        items[0].LastResponseCount = 3;
        items[0].TotalSearchCount = 2;
        items[0].TotalDownloadCount = 1;
        items[0].MaxDownloads = 9;
        items[0].LastSearchId = lastSearchId;
        items[0].LastViewedAt = lastActivityAt;

        var created = await service.CreateManyAsync(items);

        Assert.Equal(100, created.Count);
        Assert.Equal(100, created.Select(item => item.Id).Distinct().Count());
        Assert.DoesNotContain(created, item => item.Id == originalId);
        Assert.Equal(3, commandCapture.Commands.Count);
        Assert.All(commandCapture.Commands, command =>
            Assert.StartsWith("INSERT INTO WishlistItems", command.TrimStart(), StringComparison.OrdinalIgnoreCase));
        await using var verificationContext = await contextFactory.CreateDbContextAsync();
        Assert.Equal(100, await verificationContext.WishlistItems.CountAsync());
        var stored = await verificationContext.WishlistItems.SingleAsync(item => item.SearchText == "search-1");
        Assert.False(stored.Enabled);
        Assert.True(stored.AutoDownload);
        Assert.Equal(25, stored.MaxResults);
        Assert.Equal(lastActivityAt, stored.LastSearchedAt);
        Assert.Equal(8, stored.LastMatchCount);
        Assert.Equal(7, stored.LastVisibleHitCount);
        Assert.Equal(6, stored.LastHiddenLockedHitCount);
        Assert.Equal(5, stored.LastFilteredOutHitCount);
        Assert.Equal(4, stored.LastIgnoredResultHitCount);
        Assert.Equal(3, stored.LastResponseCount);
        Assert.Equal(2, stored.TotalSearchCount);
        Assert.Equal(1, stored.TotalDownloadCount);
        Assert.Equal(9, stored.MaxDownloads);
        Assert.Equal(lastSearchId, stored.LastSearchId);
        Assert.Equal(lastActivityAt, stored.LastViewedAt);
    }

    [Fact]
    public async Task ImportCsvAsync_WithOneHundredTracks_UsesThreeMultiRowCommands()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
        }
        commandCapture.Commands.Clear();
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.SetupGet(monitor => monitor.CurrentValue).Returns(new slskd.Options());
        using var service = new WishlistService(
            contextFactory,
            Mock.Of<ISearchService>(),
            Mock.Of<ISoulseekClient>(),
            optionsMonitor.Object,
            Mock.Of<ISourceRankingService>(),
            Mock.Of<IDownloadService>());
        var csv = "Track,Artist\n" + string.Join(
            '\n',
            Enumerable.Range(1, 100).Select(index => $"Track {index},Artist"));

        var result = await service.ImportCsvAsync(csv, new WishlistCsvImportOptions { Filter = "flac" });

        Assert.Equal(100, result.CreatedCount);
        Assert.Equal(3, commandCapture.Commands.Count);
        Assert.All(commandCapture.Commands, command =>
            Assert.StartsWith("INSERT INTO WishlistItems", command.TrimStart(), StringComparison.OrdinalIgnoreCase));
        await using var verificationContext = await contextFactory.CreateDbContextAsync();
        Assert.Equal(100, await verificationContext.WishlistItems.CountAsync());
    }

    [Fact]
    public async Task MarkAllViewedAsync_UsesOneSetBasedUpdate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var commandCapture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(commandCapture)
            .Options;
        var contextFactory = new TestDbContextFactory(options);
        var alreadyViewedAt = DateTime.UtcNow;
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            await context.Database.EnsureCreatedAsync();
            context.WishlistItems.AddRange(
                Enumerable.Range(0, 501).Select(index => new WishlistItem
                {
                    SearchText = $"search-{index}",
                    LastSearchedAt = alreadyViewedAt,
                }));
            context.WishlistItems.Add(new WishlistItem
            {
                SearchText = "already-viewed",
                LastSearchedAt = alreadyViewedAt.AddMinutes(-1),
                LastViewedAt = alreadyViewedAt,
            });
            await context.SaveChangesAsync();
        }
        commandCapture.Commands.Clear();
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.SetupGet(monitor => monitor.CurrentValue).Returns(new slskd.Options());
        using var service = new WishlistService(
            contextFactory,
            Mock.Of<ISearchService>(),
            Mock.Of<ISoulseekClient>(),
            optionsMonitor.Object,
            Mock.Of<ISourceRankingService>(),
            Mock.Of<IDownloadService>());

        await service.MarkAllViewedAsync();

        var command = Assert.Single(commandCapture.Commands);
        Assert.StartsWith("UPDATE", command.TrimStart(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", command, StringComparison.OrdinalIgnoreCase);
        await using var verificationContext = await contextFactory.CreateDbContextAsync();
        Assert.Equal(501, await verificationContext.WishlistItems.CountAsync(item => item.LastViewedAt > alreadyViewedAt));
        var alreadyViewed = await verificationContext.WishlistItems.SingleAsync(item => item.SearchText == "already-viewed");
        Assert.Equal(alreadyViewedAt, alreadyViewed.LastViewedAt);
    }

    private sealed class TestDbContextFactory(DbContextOptions<WishlistDbContext> options)
        : IDbContextFactory<WishlistDbContext>
    {
        public WishlistDbContext CreateDbContext() => new(options);

        public ValueTask<WishlistDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureWrite(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CaptureWrite(command);
            return ValueTask.FromResult(result);
        }

        private void CaptureWrite(DbCommand command)
        {
            var text = command.CommandText.TrimStart();
            if (text.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                Commands.Add(command.CommandText);
            }
        }
    }
}
