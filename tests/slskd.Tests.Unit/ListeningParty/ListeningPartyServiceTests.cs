// <copyright file="ListeningPartyServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.ListeningParty;

using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.ListeningParty;
using slskd.Mesh.Dht;
using slskd.NowPlaying;
using slskd.PodCore;
using slskd.Streaming;

public sealed class ListeningPartyServiceTests
{
    private const string DirectoryIndexKey = "slskdn:listening-party:index:v1";
    private const string PartyKey = "slskdn:listening-party:party:party-a";

    [Fact]
    public async Task RefreshDirectory_FailedForcedRequestCanRetryWithinTheCooldown()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(DateTimeOffset.UtcNow);
        var dht = new Mock<IMeshDhtClient>();
        dht.SetupSequence(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .ReturnsAsync(Serialize(new ListeningPartyIndex()))
            .ThrowsAsync(new InvalidOperationException("Directory unavailable"))
            .ReturnsAsync(Serialize(new ListeningPartyIndex()));
        var service = CreateService(dht.Object, clock.Object);
        await service.ListDirectoryAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshDirectoryAsync());
        Assert.Empty(await service.RefreshDirectoryAsync());
        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Exactly(3));
    }

    [Fact]
    public async Task RefreshDirectory_BypassesCurrentCacheAndCoalescesBoundedManualRequests()
    {
        var now = DateTimeOffset.UtcNow;
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(() => now);
        var completion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dht = new Mock<IMeshDhtClient>();
        dht.SetupSequence(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .ReturnsAsync(Serialize(new ListeningPartyIndex()))
            .Returns(completion.Task)
            .ReturnsAsync(Serialize(new ListeningPartyIndex()));
        var service = CreateService(dht.Object, clock.Object);
        await service.ListDirectoryAsync();
        var first = service.RefreshDirectoryAsync();
        var second = service.RefreshDirectoryAsync();
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);
        completion.SetResult(Serialize(new ListeningPartyIndex()));
        await Task.WhenAll(first, second);
        await service.RefreshDirectoryAsync();
        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Exactly(2));
        now = now.AddSeconds(3);
        await service.RefreshDirectoryAsync();
        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Exactly(3));
    }

    [Fact]
    public async Task RefreshDirectory_RemovesRemoteEntriesWithdrawnFromAValidIndex()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.SetupSequence(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .ReturnsAsync(Serialize(new ListeningPartyIndex { PartyIds = ["party-a"] }))
            .ReturnsAsync(Serialize(new ListeningPartyIndex()));
        dht.Setup(instance => instance.GetRawAsync(PartyKey, CancellationToken.None)).ReturnsAsync(Serialize(CreateAnnouncement()));
        var service = CreateService(dht.Object);
        Assert.Single(await service.ListDirectoryAsync());
        Assert.Empty(await service.RefreshDirectoryAsync());
    }

    [Fact]
    public async Task ListDirectoryAsync_ConcurrentAndRepeatedCallersShareOneDhtRefresh()
    {
        var indexCompletion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .Returns(indexCompletion.Task);
        dht.Setup(instance => instance.GetRawAsync(PartyKey, CancellationToken.None))
            .ReturnsAsync(Serialize(CreateAnnouncement()));
        var service = CreateService(dht.Object);

        var first = service.ListDirectoryAsync();
        var second = service.ListDirectoryAsync();

        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Once);
        indexCompletion.SetResult(Serialize(new ListeningPartyIndex { PartyIds = ["party-a"] }));

        var results = await Task.WhenAll(first, second);
        var repeated = await service.ListDirectoryAsync();

        Assert.All(results, result => Assert.Single(result));
        Assert.Single(repeated);
        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Once);
        dht.Verify(instance => instance.GetRawAsync(PartyKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ListDirectoryAsync_CancelledWaiterDoesNotCancelSharedRefresh()
    {
        var indexCompletion = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .Returns(indexCompletion.Task);
        var service = CreateService(dht.Object);

        var sharedRefresh = service.ListDirectoryAsync();
        using var cancellation = new CancellationTokenSource();
        var cancelledWaiter = service.ListDirectoryAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);
        indexCompletion.SetResult(Serialize(new ListeningPartyIndex()));
        await sharedRefresh;

        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ListDirectoryAsync_FailedRefreshIsRetried()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.SetupSequence(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("DHT unavailable"))
            .ReturnsAsync(Serialize(new ListeningPartyIndex()));
        var service = CreateService(dht.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListDirectoryAsync());
        await service.ListDirectoryAsync();

        dht.Verify(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task Publish_ListedSnapshot_SeparatesWebAccountAndOverlayIdentity()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(service => service.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        byte[]? published = null;
        dht.Setup(service => service.PutAsync("slskdn:listening-party:party:party-a", It.IsAny<object>(), 900, It.IsAny<CancellationToken>()))
            .Callback((string key, object value, int ttl, CancellationToken token) => published = (byte[])value);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(service => service.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var services = new ServiceCollection().AddSingleton(storage.Object).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(service => service.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var hub = new Mock<IHubContext<ListeningPartyHub>>();
        var clients = new Mock<IHubClients>();
        clients.Setup(service => service.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());
        hub.SetupGet(service => service.Clients).Returns(clients.Object);
        var tickets = new StreamTicketService();
        var service = new ListeningPartyService(hub.Object, dht.Object, router.Object, services.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), tickets, Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options { Soulseek = new Options.SoulseekOptions { Username = "overlay-host" } }));

        await service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-a",
            PodId = "pod-a",
            ChannelId = "channel-a",
            HostPeerId = "web-account",
            ContentId = "track",
            Action = "play",
            Listed = true,
            AllowMeshStreaming = true,
        });

        Assert.NotNull(published);
        var announcement = JsonSerializer.Deserialize<ListeningPartyAnnouncement>(published, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(announcement);
        Assert.Equal("web-account", announcement.HostPeerId);
        Assert.Equal("overlay-host", announcement.TransportUsername);
        Assert.Equal("listening-party:party-a", tickets.Validate(announcement.StreamTicket, "track")?.OwnerKey);
        dht.Setup(instance => instance.GetRawAsync(DirectoryIndexKey, CancellationToken.None)).ReturnsAsync(Serialize(new ListeningPartyIndex()));
        Assert.Equal("party-a", Assert.Single(await service.RefreshDirectoryAsync()).PartyId);
    }

    private static ListeningPartyService CreateService(IMeshDhtClient dht, TimeProvider? clock = null)
    {
        return new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(),
            dht,
            Mock.Of<IPodMessageRouter>(),
            Mock.Of<IServiceScopeFactory>(),
            new NowPlayingService(),
            Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()), clock);
    }

    private static ListeningPartyAnnouncement CreateAnnouncement()
    {
        return new ListeningPartyAnnouncement
        {
            PartyId = "party-a",
            PodId = "pod-a",
            ChannelId = "channel-a",
            ExpiresAtUnixMs = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds(),
            LastSeenUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }

    private static byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
