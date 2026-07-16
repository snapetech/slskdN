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
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
