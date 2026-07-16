// <copyright file="SharingServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Sharing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using slskd.Identity;
using slskd.Sharing;
using Xunit;

public class SharingServiceTests
{
    private readonly Mock<IShareGroupRepository> _groupsMock = new();
    private readonly Mock<ICollectionRepository> _collectionsMock = new();
    private readonly Mock<IShareGrantRepository> _grantsMock = new();
    private readonly Mock<IShareTokenService> _tokensMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    private SharingService CreateService()
    {
        return new SharingService(_groupsMock.Object, _collectionsMock.Object, _grantsMock.Object, _tokensMock.Object, _serviceProviderMock.Object);
    }

    [Fact]
    public async Task GetCollectionItemAsync_DelegatesScopedLookup()
    {
        var collectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item = new CollectionItem { Id = itemId, CollectionId = collectionId, ContentId = "content" };
        _collectionsMock
            .Setup(repository => repository.GetItemAsync(collectionId, itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await CreateService().GetCollectionItemAsync(collectionId, itemId, default);

        Assert.Same(item, result);
        _collectionsMock.Verify(
            repository => repository.GetItemAsync(collectionId, itemId, It.IsAny<CancellationToken>()),
            Times.Once);
        _collectionsMock.Verify(
            repository => repository.GetItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTokenAsync_GrantNotFound_Throws()
    {
        var svc = CreateService();
        _grantsMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ShareGrant?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTokenAsync(Guid.NewGuid(), TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task CreateTokenAsync_Success_ReturnsToken()
    {
        var svc = CreateService();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant
        {
            Id = grantId,
            CollectionId = collectionId,
            AudienceId = "user1",
            AllowStream = true,
            AllowDownload = true,
            MaxConcurrentStreams = 2
        };
        _grantsMock.Setup(x => x.GetByIdAsync(grantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _tokensMock.Setup(x => x.CreateAsync(
            grantId.ToString(),
            collectionId.ToString(),
            "user1",
            true,
            true,
            2,
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>())).ReturnsAsync("token123");

        var token = await svc.CreateTokenAsync(grantId, TimeSpan.FromHours(1));

        Assert.Equal("token123", token);
    }

    [Fact]
    public async Task GetManifestAsync_TokenAuth_Success()
    {
        var svc = CreateService();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, CollectionId = collectionId, AllowStream = true };
        var collection = new Collection { Id = collectionId, Title = "Test", Type = CollectionType.ShareList };
        var items = new List<CollectionItem>
        {
            new()
            {
                ContentId = "c1",
                MediaKind = "Music",
                FileName = "Artist/Album/01 Track.flac",
                Title = "Track",
                Artist = "Artist",
                Album = "Album",
            },
        };

        _grantsMock.Setup(x => x.GetByIdAsync(grantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _collectionsMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collectionsMock.Setup(x => x.GetItemsAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var m = await svc.GetManifestAsync(grantId, "token123", null, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Equal(collectionId.ToString(), m.CollectionId);
        Assert.Equal("Test", m.Title);
        Assert.Single(m.Items);
        Assert.Equal("c1", m.Items[0].ContentId);
        Assert.Equal("Artist/Album/01 Track.flac", m.Items[0].FileName);
        Assert.Equal("Track", m.Items[0].Title);
        Assert.Equal("Artist", m.Items[0].Artist);
        Assert.Equal("Album", m.Items[0].Album);
        Assert.NotNull(m.Items[0].StreamUrl);
        Assert.Contains("token=token123", m.Items[0].StreamUrl);
    }

    [Fact]
    public async Task GetManifestAsync_NormalAuth_Success()
    {
        var svc = CreateService();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, CollectionId = collectionId, AllowStream = true, AudienceId = "alice" };
        var collection = new Collection { Id = collectionId, Title = "Test", Type = CollectionType.Playlist };
        var items = new List<CollectionItem> { new() { ContentId = "c1" } };

        _grantsMock.Setup(x => x.GetAccessibleByIdAsync(grantId, "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);
        _collectionsMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collectionsMock.Setup(x => x.GetItemsAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var m = await svc.GetManifestAsync(grantId, null, "alice", CancellationToken.None);

        Assert.NotNull(m);
        Assert.Single(m.Items);
        Assert.NotNull(m.Items[0].StreamUrl);
        Assert.DoesNotContain("token=", m.Items[0].StreamUrl);
        _grantsMock.Verify(x => x.GetAccessibleByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetManifestAsync_AllowStreamFalse_OmitsStreamUrl()
    {
        var svc = CreateService();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, CollectionId = collectionId, AllowStream = false };
        var collection = new Collection { Id = collectionId, Title = "Test", Type = CollectionType.ShareList };
        var items = new List<CollectionItem> { new() { ContentId = "c1" } };

        _grantsMock.Setup(x => x.GetByIdAsync(grantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _collectionsMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collectionsMock.Setup(x => x.GetItemsAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var m = await svc.GetManifestAsync(grantId, "token", null, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Null(m.Items[0].StreamUrl);
    }

    [Fact]
    public async Task AddShareGroupMemberByPeerIdAsync_DelegatesToRepository()
    {
        var svc = CreateService();
        var groupId = Guid.NewGuid();
        var peerId = "peer123";
        _groupsMock.Setup(x => x.AddMemberByPeerIdAsync(groupId, peerId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await svc.AddShareGroupMemberByPeerIdAsync(groupId, peerId, CancellationToken.None);

        _groupsMock.Verify(x => x.AddMemberByPeerIdAsync(groupId, peerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveShareGroupMemberByPeerIdAsync_DelegatesToRepository()
    {
        var svc = CreateService();
        var groupId = Guid.NewGuid();
        var peerId = "peer123";
        _groupsMock.Setup(x => x.RemoveMemberByPeerIdAsync(groupId, peerId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await svc.RemoveShareGroupMemberByPeerIdAsync(groupId, peerId, CancellationToken.None);

        _groupsMock.Verify(x => x.RemoveMemberByPeerIdAsync(groupId, peerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetShareGroupMemberInfosAsync_WithContacts_ResolvesNicknames()
    {
        var svc = CreateService();
        var groupId = Guid.NewGuid();
        var members = new List<ShareGroupMember>
        {
            new() { UserId = "peer123", PeerId = "peer123" },
            new() { UserId = "bob", PeerId = null }
        };
        var contact = new Contact { PeerId = "peer123", Nickname = "Alice" };
        var contactServiceMock = new Mock<IContactService>();
        contactServiceMock.Setup(x => x.GetByPeerIdsAsync(
                It.Is<IEnumerable<string>>(peerIds => peerIds.SequenceEqual(new[] { "peer123" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { contact });

        _groupsMock.Setup(x => x.GetMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IContactService)))
            .Returns(contactServiceMock.Object);

        var result = await svc.GetShareGroupMemberInfosAsync(groupId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsContactBased);
        Assert.Equal("Alice", result[0].ContactNickname);
        Assert.False(result[1].IsContactBased);
        Assert.Null(result[1].ContactNickname);
        contactServiceMock.Verify(x => x.GetByPeerIdsAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        contactServiceMock.Verify(x => x.GetByPeerIdAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetShareGroupMemberInfosAsync_NoContactService_ReturnsWithoutNicknames()
    {
        var svc = CreateService();
        var groupId = Guid.NewGuid();
        var members = new List<ShareGroupMember>
        {
            new() { UserId = "peer123", PeerId = "peer123" }
        };

        _groupsMock.Setup(x => x.GetMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IContactService)))
            .Returns((IContactService?)null);

        var result = await svc.GetShareGroupMemberInfosAsync(groupId, CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[0].IsContactBased);
        Assert.Null(result[0].ContactNickname);
    }

    [Fact]
    public async Task GetManifestAsync_IncludesOwnerInfo()
    {
        var svc = CreateService();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, CollectionId = collectionId, AllowStream = true };
        var collection = new Collection { Id = collectionId, Title = "Test", Type = CollectionType.ShareList, OwnerUserId = "alice" };
        var items = new List<CollectionItem> { new() { ContentId = "c1" } };

        _grantsMock.Setup(x => x.GetByIdAsync(grantId, It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _collectionsMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(collection);
        _collectionsMock.Setup(x => x.GetItemsAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(items);
        var contactService = new Mock<IContactService>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(IContactService))).Returns(contactService.Object);

        var m = await svc.GetManifestAsync(grantId, "token", null, CancellationToken.None);

        Assert.NotNull(m);
        Assert.Equal("alice", m.OwnerUserId);
        Assert.Null(m.OwnerContactNickname);
        Assert.Null(m.OwnerPeerId);
        contactService.Verify(service => service.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
