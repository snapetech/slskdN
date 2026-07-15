// <copyright file="ShareGrantAnnouncementServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Sharing;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Sharing;
using Soulseek;
using Xunit;

public class ShareGrantAnnouncementServiceTests
{
    [Fact]
    public async Task IngestAsync_WithSenderOwnerMismatch_DoesNotPersistGrant()
    {
        using var fixture = new CollectionsDbFixture();
        var service = CreateService(fixture.Factory);
        var announcement = CreateAnnouncement(ownerUserId: "alice");

        await service.IngestAsync(announcement, senderUsername: "mallory", default);

        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.False(await db.Collections.AnyAsync());
        Assert.False(await db.ShareGrants.AnyAsync());
    }

    [Fact]
    public async Task IngestAsync_WithSenderOwnerMatch_PersistsGrant()
    {
        using var fixture = new CollectionsDbFixture();
        var service = CreateService(fixture.Factory);
        var announcement = CreateAnnouncement(ownerUserId: "alice");

        await service.IngestAsync(announcement, senderUsername: "alice", default);

        await using var db = await fixture.Factory.CreateDbContextAsync();
        var collection = Assert.Single(await db.Collections.ToListAsync());
        var grant = Assert.Single(await db.ShareGrants.ToListAsync());

        Assert.Equal("alice", collection.OwnerUserId);
        Assert.Equal("network:recipient", grant.AudienceId);
        Assert.Equal("recipient", grant.AudiencePeerId);
        Assert.Equal("https://owner.example.test", grant.OwnerEndpoint);
        Assert.Equal("share-token", grant.ShareToken);
    }

    [Fact]
    public async Task IngestForWebAccountAsync_BindsExplicitWebAudience()
    {
        using var fixture = new CollectionsDbFixture();
        var service = CreateService(fixture.Factory);
        var announcement = CreateAnnouncement(ownerUserId: "alice");

        await service.IngestForWebAccountAsync(announcement, "web-recipient", default);

        await using var db = await fixture.Factory.CreateDbContextAsync();
        var grant = Assert.Single(await db.ShareGrants.ToListAsync());
        Assert.Equal("web-recipient", grant.AudienceId);
        Assert.Equal("recipient", grant.AudiencePeerId);
    }

    [Fact]
    public void Dispose_UnsubscribesSoulseekEvent()
    {
        var soulseekClient = new Mock<ISoulseekClient>();
        var service = new ShareGrantAnnouncementService(
            Mock.Of<Microsoft.EntityFrameworkCore.IDbContextFactory<CollectionsDbContext>>(),
            NullLogger<ShareGrantAnnouncementService>.Instance,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            soulseekClient.Object);

        service.Dispose();

        soulseekClient.VerifyRemove(x => x.PrivateMessageReceived -= It.IsAny<EventHandler<PrivateMessageReceivedEventArgs>>(), Times.Once);
    }

    private static ShareGrantAnnouncementService CreateService(Microsoft.EntityFrameworkCore.IDbContextFactory<CollectionsDbContext> factory)
    {
        return new ShareGrantAnnouncementService(
            factory,
            NullLogger<ShareGrantAnnouncementService>.Instance,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options
            {
                Soulseek = new slskd.Options.SoulseekOptions
                {
                    Username = "recipient",
                },
            }));
    }

    private static ShareGrantAnnouncement CreateAnnouncement(string ownerUserId)
    {
        return new ShareGrantAnnouncement
        {
            ShareGrantId = Guid.NewGuid(),
            CollectionId = Guid.NewGuid(),
            CollectionTitle = "Shared",
            OwnerUserId = ownerUserId,
            OwnerEndpoint = "https://owner.example.test",
            Token = "share-token",
            RecipientUserId = "recipient",
            Items =
            [
                new ShareGrantAnnouncementItem
                {
                    ContentId = "track:1",
                    MediaKind = "audio",
                },
            ],
        };
    }

    private sealed class CollectionsDbFixture : IDisposable
    {
        private readonly string _dbPath;

        public CollectionsDbFixture()
        {
            _dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"share_grant_{Guid.NewGuid()}.db");
            var options = new DbContextOptionsBuilder<CollectionsDbContext>()
                .UseSqlite($"Data Source={_dbPath}")
                .Options;

            Factory = new TestDbContextFactory(options);

            using var db = new CollectionsDbContext(options);
            db.Database.EnsureCreated();
        }

        public Microsoft.EntityFrameworkCore.IDbContextFactory<CollectionsDbContext> Factory { get; }

        public void Dispose()
        {
            if (System.IO.File.Exists(_dbPath))
            {
                System.IO.File.Delete(_dbPath);
            }
        }
    }

    private sealed class TestDbContextFactory : Microsoft.EntityFrameworkCore.IDbContextFactory<CollectionsDbContext>
    {
        private readonly DbContextOptions<CollectionsDbContext> _options;

        public TestDbContextFactory(DbContextOptions<CollectionsDbContext> options)
        {
            _options = options;
        }

        public CollectionsDbContext CreateDbContext() => new(_options);
    }
}
