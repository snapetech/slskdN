// <copyright file="UserBlockServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Users;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Users.Notes;
using Xunit;

public sealed class UserBlockServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly UserNotesDbContext context;
    private readonly UserBlockService service;

    public UserBlockServiceTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<UserNotesDbContext>()
            .UseSqlite(connection)
            .Options;
        context = new UserNotesDbContext(options);
        context.Database.EnsureCreated();
        service = new UserBlockService(new TestDbContextFactory(options), NullLogger<UserBlockService>.Instance);
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task BlockIsCaseInsensitiveAndIdempotent()
    {
        var first = await service.BlockAsync(" Peer ");
        var second = await service.BlockAsync("peer");

        Assert.Equal("Peer", first.Username);
        Assert.Equal(first.Username, second.Username);
        Assert.Single(await service.GetAllBlocksAsync());
        Assert.Contains("PEER", await service.GetBlockedUsernamesAsync());
    }

    [Fact]
    public async Task UnblockIsIdempotent()
    {
        await service.BlockAsync("peer");

        await service.UnblockAsync(" PEER ");
        await service.UnblockAsync("peer");

        Assert.Empty(await service.GetAllBlocksAsync());
    }

    private sealed class TestDbContextFactory(DbContextOptions<UserNotesDbContext> options)
        : IDbContextFactory<UserNotesDbContext>
    {
        public UserNotesDbContext CreateDbContext() => new(options);

        public ValueTask<UserNotesDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new UserNotesDbContext(options));
    }
}
