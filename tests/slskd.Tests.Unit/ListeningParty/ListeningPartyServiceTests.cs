// <copyright file="ListeningPartyServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.ListeningParty;

using System.Text.Json;
using System.Security.Claims;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Publish_RechecksMembershipAndRequiresRejoinAfterRevocation(bool banned)
    {
        var members = new[] { new PodMember { PeerId = "member" } };
        var pods = new Mock<IPodService>();
        pods.Setup(instance => instance.GetMembersAsync("pod-a", It.IsAny<CancellationToken>())).ReturnsAsync(() => members);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(pods.Object).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var deliveries = new List<(string[] Recipients, string Method)>();
        var clients = new Mock<IHubClients>();
        clients.Setup(instance => instance.Clients(It.IsAny<IReadOnlyList<string>>())).Returns((IReadOnlyList<string> recipients) =>
        {
            var target = new Mock<IClientProxy>();
            target.Setup(instance => instance.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Callback((string method, object?[] arguments, CancellationToken token) => deliveries.Add((recipients.ToArray(), method)))
                .Returns(Task.CompletedTask);
            return target.Object;
        });
        var hub = new Mock<IHubContext<ListeningPartyHub>>();
        hub.SetupGet(instance => instance.Clients).Returns(clients.Object);
        var service = new ListeningPartyService(hub.Object, Mock.Of<IMeshDhtClient>(), router.Object, provider.GetRequiredService<IServiceScopeFactory>(),
            new NowPlayingService(), Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "member") }, "test"));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator") }, "test"));
        Assert.True(service.Subscribe("member-connection", "pod-a", "channel-a", user));
        Assert.True(service.Subscribe("admin-connection", "pod-a", "channel-a", admin));
        Assert.True(service.Subscribe("other-room", "pod-a", "other-channel", admin));
        var state = new ListeningPartyEvent { PodId = "pod-a", ChannelId = "channel-a", ContentId = "track", HostPeerId = "admin", Action = "play" };
        await service.PublishAsync(state);
        Assert.Equal(new[] { "member-connection", "admin-connection" }, Assert.Single(deliveries).Recipients);
        Assert.Equal("partyState", deliveries[0].Method);
        deliveries.Clear();
        members = banned ? [new PodMember { PeerId = "member", IsBanned = true }] : [];
        await service.PublishAsync(state);
        Assert.Contains(deliveries, delivery => delivery.Method == "partyAccessRevoked" && delivery.Recipients.SequenceEqual(new[] { "member-connection" }));
        Assert.Contains(deliveries, delivery => delivery.Method == "partyState" && delivery.Recipients.SequenceEqual(new[] { "admin-connection" }));
        deliveries.Clear();
        members = [new PodMember { PeerId = "member" }];
        await service.PublishAsync(state);
        Assert.Equal(new[] { "admin-connection" }, Assert.Single(deliveries).Recipients);
        pods.Verify(instance => instance.GetMembersAsync("pod-a", It.IsAny<CancellationToken>()), Times.Exactly(2));
        deliveries.Clear();
        Assert.True(service.Subscribe("member-connection", "pod-a", "channel-a", user));
        await service.PublishAsync(state);
        Assert.Contains(deliveries, delivery => delivery.Recipients.Contains("member-connection"));
        deliveries.Clear();
        var membershipRead = new TaskCompletionSource<IReadOnlyList<PodMember>>(TaskCreationOptions.RunContinuationsAsynchronously);
        pods.Setup(instance => instance.GetMembersAsync("pod-a", It.IsAny<CancellationToken>())).Returns(membershipRead.Task);
        var pendingPublication = service.PublishAsync(state);
        Assert.False(pendingPublication.IsCompleted);
        service.Unsubscribe("member-connection", "pod-a", "channel-a");
        service.Disconnect("admin-connection");
        membershipRead.SetResult(members);
        await pendingPublication;
        Assert.Empty(deliveries);
    }

    [Fact]
    public void Subscriptions_BoundCapacityAndReleaseDisconnectedRooms()
    {
        var service = CreateService(Mock.Of<IMeshDhtClient>());
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "member") }, "test"));
        Assert.False(service.Subscribe("anonymous", "pod-a", "room", new ClaimsPrincipal()));
        for (var index = 0; index < 16; index++) Assert.True(service.Subscribe("connection", "pod-a", index.ToString(), user));
        Assert.True(service.Subscribe("connection", "pod-a", "0", user));
        Assert.False(service.Subscribe("connection", "pod-a", "overflow", user));
        service.Unsubscribe("connection", "pod-a", "0");
        Assert.True(service.Subscribe("connection", "pod-a", "overflow", user));
        service.Disconnect("connection");
        for (var index = 0; index < 4096; index++) Assert.True(service.Subscribe(index.ToString(), "pod-a", "room", user));
        Assert.False(service.Subscribe("global-overflow", "pod-a", "room", user));
        service.Disconnect("0");
        Assert.True(service.Subscribe("global-overflow", "pod-a", "room", user));
    }

    [Fact]
    public async Task Publish_OrdersStopAfterPendingPlayWithoutStaleFanout()
    {
        var firstRouting = new TaskCompletionSource<PodMessageRoutingResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero);
        var routes = 0;
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Returns((PodMessage message, CancellationToken token) => ++routes == 1 ? firstRouting.Task : Task.FromResult(result));
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).BuildServiceProvider();
        var actions = new List<string>();
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(instance => instance.SendCoreAsync("partyState", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback((string method, object?[] arguments, CancellationToken token) => actions.Add(((ListeningPartyEvent)arguments[0]!).Action))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(instance => instance.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);
        var hub = new Mock<IHubContext<ListeningPartyHub>>();
        hub.SetupGet(instance => instance.Clients).Returns(clients.Object);
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var service = new ListeningPartyService(hub.Object, dht.Object, router.Object, provider.GetRequiredService<IServiceScopeFactory>(),
            new NowPlayingService(), Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator") }, "test"));
        Assert.True(service.Subscribe("connection", "pod-a", "channel-a", admin));
        var play = new ListeningPartyEvent { PodId = "pod-a", ChannelId = "channel-a", ContentId = "track", Action = "play", HostPeerId = "admin", PartyId = "party-a" };
        var first = service.PublishAsync(play);
        var stop = service.PublishAsync(play with { Action = "stop" });
        try
        {
            Assert.False(stop.IsCompleted);
            var unrelated = await service.PublishAsync(play with { ChannelId = "other-room" });
            Assert.Equal("other-room", unrelated.ChannelId);
        }
        finally
        {
            firstRouting.TrySetResult(result);
            await Task.WhenAll(first, stop);
        }

        Assert.Equal(new[] { "play", "stop" }, actions);
        Assert.Null(await service.GetStateAsync("pod-a", "channel-a"));
    }

    [Theory]
    [InlineData(false, 16)]
    [InlineData(true, 256)]
    public async Task Publish_BoundsPendingWorkAndReleasesCancelledReservations(bool distinctRooms, int capacity)
    {
        var completion = new TaskCompletionSource<PodMessageRoutingResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Returns((PodMessage message, CancellationToken token) => completion.Task.WaitAsync(token));
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        using var provider = new ServiceCollection().AddSingleton(Mock.Of<IPodMessageStorage>()).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        using var cancellation = new CancellationTokenSource();
        var play = new ListeningPartyEvent { PodId = "pod-a", ChannelId = "room", ContentId = "track", Action = "play" };
        var pending = Enumerable.Range(0, capacity).Select(index => service.PublishAsync(
            play with { ChannelId = distinctRooms ? index.ToString() : "room" }, cancellation.Token)).ToArray();
        try
        {
            await Assert.ThrowsAsync<ListeningPartyCapacityException>(() => service.PublishAsync(
                play with { ChannelId = distinctRooms ? "overflow" : "room" }));
        }
        finally
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(pending));
        }

        completion.SetResult(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        Assert.Equal("play", (await service.PublishAsync(play)).Action);
        await Assert.ThrowsAsync<ArgumentException>(() => service.PublishAsync(play with { Action = "invalid" }));
        Assert.Equal("play", (await service.PublishAsync(play)).Action);
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
