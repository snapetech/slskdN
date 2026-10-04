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
        using var services = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(service => service.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task Publish_ReservesPartyIdAcrossRoomsBeforeStorage()
    {
        var storeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowStore = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Returns((string pod, string channel, PodMessage message, CancellationToken token) =>
            {
                storeEntered.TrySetResult();
                return allowStore.Task;
            });
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        using var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));

        var first = service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-shared",
            PodId = "pod-a",
            ChannelId = "music-a",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        });
        await storeEntered.Task;
        var conflict = await Record.ExceptionAsync(() => service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-shared",
            PodId = "pod-a",
            ChannelId = "music-b",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        }));
        allowStore.SetResult(true);
        var accepted = await first;

        Assert.IsType<ListeningPartyIdConflictException>(conflict);
        Assert.Equal("music-a", accepted.ChannelId);
        Assert.Equal(accepted, await service.GetStateByPartyIdAsync("party-shared"));
        Assert.Null(await service.GetStateAsync("pod-a", "music-b"));
        storage.Verify(instance => instance.StoreMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_RejectsPartyIdOwnedByAnotherDhtRoomBeforeStorage()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync("slskdn:listening-party:party:party-shared", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(new ListeningPartyAnnouncement
            {
                PartyId = "party-shared",
                PodId = "pod-other",
                ChannelId = "music",
                ExpiresAtUnixMs = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds(),
            }));
        var storage = new Mock<IPodMessageStorage>();
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object,
            Mock.Of<IPodMessageRouter>(), provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));

        await Assert.ThrowsAsync<ListeningPartyIdConflictException>(() => service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-shared",
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        }));

        Assert.Null(await service.GetStateAsync("pod-a", "music"));
        storage.Verify(instance => instance.StoreMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        dht.Verify(instance => instance.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_DirectoryPreflightFailureDoesNotStoreRoomMessage()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync("slskdn:listening-party:party:party-new", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DHT unavailable"));
        var storage = new Mock<IPodMessageStorage>();
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object,
            Mock.Of<IPodMessageRouter>(), provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));

        await Assert.ThrowsAsync<ListeningPartyDirectoryUnavailableException>(() => service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-new",
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        }));

        Assert.Null(await service.GetStateAsync("pod-a", "music"));
        storage.Verify(instance => instance.StoreMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Publish_NotifiesLocalAndRoutesRoomStateBeforeDirectoryFailure()
    {
        var order = new List<string>();
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        dht.Setup(instance => instance.PutAsync(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string key, object? value, int ttl, CancellationToken token) => order.Add("directory"))
            .ThrowsAsync(new InvalidOperationException("Directory unavailable"));
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Callback((PodMessage message, CancellationToken token) => order.Add("mesh"))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var target = new Mock<IClientProxy>();
        target.Setup(instance => instance.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback((string method, object?[] arguments, CancellationToken token) => order.Add("local"))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(instance => instance.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(target.Object);
        var hub = new Mock<IHubContext<ListeningPartyHub>>();
        hub.SetupGet(instance => instance.Clients).Returns(clients.Object);
        using var service = new ListeningPartyService(
            hub.Object, dht.Object, router.Object, provider.GetRequiredService<IServiceScopeFactory>(),
            new NowPlayingService(), Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()));
        var administrator = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator") }, "test"));
        Assert.True(service.Subscribe("local-listener", "pod-a", "channel-a", administrator));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-a",
            PodId = "pod-a",
            ChannelId = "channel-a",
            HostPeerId = "host",
            ContentId = "track",
            Action = "play",
            Listed = true,
        }));

        Assert.Equal(new[] { "local", "mesh", "directory" }, order);
        Assert.Equal("track", (await service.GetStateAsync("pod-a", "channel-a"))?.ContentId);
    }

    [Fact]
    public async Task HostSessions_RenewExpiredAnnouncementsAndFenceReplacedTabs()
    {
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(() => now);
        var timers = new List<(TimerCallback Callback, object? State, Mock<ITimer> Timer)>();
        clock.Setup(provider => provider.CreateTimer(
                It.IsAny<TimerCallback>(), It.IsAny<object?>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
            .Returns((TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            {
                var timer = new Mock<ITimer>();
                timer.Setup(instance => instance.Change(It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>())).Returns(true);
                timers.Add((callback, state, timer));
                return timer.Object;
            });
        var values = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
        var writes = new List<(string Key, int Ttl)>();
        var failingAnnouncementTitle = string.Empty;
        var expiringPartyId = string.Empty;
        var failNextExpiryStopStorage = false;
        var expiryStopRouted = new TaskCompletionSource<ListeningPartyEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expiryIndexWithdrawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expiryStopStorageFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) => values.TryGetValue(key, out var value) ? value : null);
        dht.Setup(instance => instance.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string key, object value, int ttl, CancellationToken token) =>
            {
                if (key.StartsWith("slskdn:listening-party:party:", StringComparison.Ordinal) &&
                    JsonSerializer.Deserialize<ListeningPartyAnnouncement>((byte[])value, new JsonSerializerOptions(JsonSerializerDefaults.Web))?.Title == failingAnnouncementTitle)
                {
                    throw new InvalidOperationException("Announcement storage unavailable");
                }

                values[key] = (byte[])value;
                writes.Add((key, ttl));
                if (key == DirectoryIndexKey && !string.IsNullOrWhiteSpace(expiringPartyId))
                {
                    var index = JsonSerializer.Deserialize<ListeningPartyIndex>((byte[])value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    if (index?.PartyIds.Contains(expiringPartyId, StringComparer.Ordinal) == false)
                    {
                        expiryIndexWithdrawn.TrySetResult();
                    }
                }
            })
            .Returns(Task.CompletedTask);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string podId, string channelId, PodMessage message, CancellationToken token) =>
            {
                var state = JsonSerializer.Deserialize<ListeningPartyEvent>(message.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (failNextExpiryStopStorage && state?.Action == "stop" && state.PartyId == expiringPartyId)
                {
                    failNextExpiryStopStorage = false;
                    expiryStopStorageFailed.TrySetResult();
                    return false;
                }

                return true;
            });
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PodMessage message, CancellationToken token) =>
            {
                var state = JsonSerializer.Deserialize<ListeningPartyEvent>(message.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (state?.Action == "stop" && state.PartyId == expiringPartyId)
                {
                    expiryStopRouted.TrySetResult(state);
                }

                return new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero);
            });
        var tickets = new Mock<IStreamTicketService>();
        var ticketNumber = 0;
        tickets.Setup(instance => instance.Create("track", It.IsAny<string>(), TimeSpan.FromSeconds(900)))
            .Returns(() => $"ticket-{++ticketNumber}");
        using var service = new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), tickets.Object,
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()), clock.Object);
        var firstSession = Guid.NewGuid().ToString("N");
        var replacementSession = Guid.NewGuid().ToString("N");
        var restartedSession = Guid.NewGuid().ToString("N");
        ListeningPartyEvent HostEvent(string action, string title, string partyId = "") => new()
        {
            PartyId = partyId,
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            Title = title,
            HostPeerId = "host",
            Action = action,
            Listed = true,
            AllowMeshStreaming = true,
        };

        var first = await service.PublishHostEventAsync(HostEvent("play", "First"), firstSession, startHostSession: true);
        var firstAnnouncementKey = $"slskdn:listening-party:party:{first.PartyId}";
        var initialAnnouncement = JsonSerializer.Deserialize<ListeningPartyAnnouncement>(values[firstAnnouncementKey], new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(initialAnnouncement);
        Assert.Equal(now.AddSeconds(900).ToUnixTimeMilliseconds(), initialAnnouncement.ExpiresAtUnixMs);

        now = now.AddSeconds(901);
        await service.RenewHostSessionAsync("pod-a", "music", first.PartyId, firstSession);
        var renewedAnnouncement = JsonSerializer.Deserialize<ListeningPartyAnnouncement>(values[firstAnnouncementKey], new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(renewedAnnouncement);
        Assert.NotEqual(initialAnnouncement.StreamTicket, renewedAnnouncement.StreamTicket);
        Assert.Equal(now.AddSeconds(900).ToUnixTimeMilliseconds(), renewedAnnouncement.ExpiresAtUnixMs);
        Assert.Equal(2, writes.Count(write => write.Key == firstAnnouncementKey && write.Ttl == 900));
        Assert.Equal(2, writes.Count(write => write.Key == DirectoryIndexKey && write.Ttl == 900));
        tickets.Verify(instance => instance.Create("track", $"listening-party:{first.PartyId}", TimeSpan.FromSeconds(900)), Times.Exactly(2));

        failingAnnouncementTitle = "Failed replacement";
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PublishHostEventAsync(HostEvent("play", failingAnnouncementTitle), replacementSession, startHostSession: true));
        var fencedRenewal = await Assert.ThrowsAsync<ListeningPartyHostSessionConflictException>(
            () => service.RenewHostSessionAsync("pod-a", "music", first.PartyId, firstSession));
        Assert.Equal("host_session_replaced", fencedRenewal.Code);
        failingAnnouncementTitle = string.Empty;

        var replacement = await service.PublishHostEventAsync(HostEvent("play", "Replacement"), replacementSession, startHostSession: true);
        expiringPartyId = replacement.PartyId;
        Assert.NotEqual(first.PartyId, replacement.PartyId);
        Assert.Equal("Replacement", (await service.GetStateAsync("pod-a", "music"))?.Title);

        var staleRenewal = await Assert.ThrowsAsync<ListeningPartyHostSessionConflictException>(
            () => service.RenewHostSessionAsync("pod-a", "music", first.PartyId, firstSession));
        Assert.Equal("host_session_replaced", staleRenewal.Code);
        var stalePause = await Assert.ThrowsAsync<ListeningPartyHostSessionConflictException>(() =>
            service.PublishHostEventAsync(HostEvent("pause", "Stale", first.PartyId), firstSession, startHostSession: false));
        Assert.Equal("host_session_replaced", stalePause.Code);
        var staleStop = await Assert.ThrowsAsync<ListeningPartyHostSessionConflictException>(() =>
            service.PublishHostEventAsync(HostEvent("stop", "Stale", first.PartyId), firstSession, startHostSession: false));
        Assert.Equal("host_session_replaced", staleStop.Code);
        Assert.Equal("Replacement", (await service.GetStateAsync("pod-a", "music"))?.Title);

        now = now.AddMinutes(31);
        var expired = await Assert.ThrowsAsync<ListeningPartyHostSessionConflictException>(
            () => service.RenewHostSessionAsync("pod-a", "music", replacement.PartyId, replacementSession));
        Assert.Equal("host_session_expired", expired.Code);
        var replacementTimer = timers[^1];
        failNextExpiryStopStorage = true;
        replacementTimer.Callback(replacementTimer.State);
        await expiryStopStorageFailed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Replacement", (await service.GetStateAsync("pod-a", "music"))?.Title);
        now = now.AddSeconds(30);
        replacementTimer.Callback(replacementTimer.State);
        var expiryStop = await expiryStopRouted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await expiryIndexWithdrawn.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(replacement.PartyId, expiryStop.PartyId);
        Assert.Null(await service.GetStateAsync("pod-a", "music"));
        var restarted = await service.PublishHostEventAsync(HostEvent("play", "Restarted"), restartedSession, startHostSession: true);
        Assert.NotEqual(replacement.PartyId, restarted.PartyId);
        Assert.Equal("Restarted", (await service.GetStateAsync("pod-a", "music"))?.Title);
    }

    [Theory]
    [InlineData("play", 500, 20.5)]
    [InlineData("seek", 25000, 30)]
    [InlineData("pause", 500, 20)]
    [InlineData("seek", -500, 20)]
    public async Task Publish_AdjustsActivePositionByBoundedClientObservationAge(
        string action,
        long observedAgeMilliseconds,
        double expectedPositionSeconds)
    {
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);
        var clock = new Mock<TimeProvider>();
        clock.Setup(provider => provider.GetUtcNow()).Returns(() => now);
        string? storedBody = null;
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Callback((string podId, string channelId, PodMessage message, CancellationToken token) => storedBody = message.Body)
            .ReturnsAsync(true);
        var dht = new Mock<IMeshDhtClient>();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        using var service = new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()), clock.Object);

        var published = await service.PublishAsync(new ListeningPartyEvent
        {
            PodId = "pod-a",
            ChannelId = "music",
            HostPeerId = "host",
            ContentId = "track",
            Action = action,
            PositionSeconds = 20,
            ClientPositionObservedAtUnixMs = now.ToUnixTimeMilliseconds() - observedAgeMilliseconds,
        });

        Assert.Equal(expectedPositionSeconds, published.PositionSeconds);
        Assert.Equal(now.ToUnixTimeMilliseconds(), published.ServerTimeUnixMs);
        Assert.Equal(0, published.ClientPositionObservedAtUnixMs);
        Assert.DoesNotContain("clientPositionObservedAtUnixMs", storedBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Publish_RechecksMembershipAndRequiresRejoinAfterRevocation(bool banned)
    {
        var members = new[] { new PodMember { PeerId = "member" } };
        var pods = new Mock<IPodService>();
        pods.Setup(instance => instance.GetChannelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string pod, string channel, CancellationToken token) => new PodChannel { ChannelId = channel });
        pods.Setup(instance => instance.GetMembersAsync("pod-a", It.IsAny<CancellationToken>())).ReturnsAsync(() => members);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(pods.Object).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
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
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Returns((PodMessage message, CancellationToken token) => ++routes == 1 ? firstRouting.Task : Task.FromResult(result));
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
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
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .Returns((PodMessage message, CancellationToken token) => completion.Task.WaitAsync(token));
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        using var cancellation = new CancellationTokenSource();
        var play = new ListeningPartyEvent { PodId = "pod-a", ChannelId = "room", ContentId = "track", HostPeerId = "host", Action = "play" };
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Publish_RejectsMissingRoomOrStorageBeforeExposingSnapshot(bool missingRoom)
    {
        var pods = new Mock<IPodService>();
        pods.Setup(instance => instance.GetChannelAsync("pod-a", "music", It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingRoom ? null : new PodChannel { ChannelId = "music" });
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingRoom);
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(pods.Object).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var rejected = await Record.ExceptionAsync(() => service.PublishAsync(new ListeningPartyEvent
        {
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        }));
        if (missingRoom) Assert.IsType<ListeningPartyRoomNotFoundException>(rejected);
        else Assert.IsType<ListeningPartyStorageException>(rejected);
        Assert.Null(await service.GetStateAsync("pod-a", "music"));
        Assert.Empty(await service.ListDirectoryAsync());
        router.Verify(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        storage.Verify(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), missingRoom ? Times.Never() : Times.Once());
        dht.Verify(instance => instance.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("play")]
    [InlineData("stop")]
    public async Task Publish_RejectedWriteRetainsExistingSnapshotAndListing(string action)
    {
        var accepted = true;
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => accepted);
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var initial = await service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party",
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        });
        accepted = false;
        await Assert.ThrowsAsync<ListeningPartyStorageException>(() => service.PublishAsync(initial with { Action = action, ContentId = "replacement" }));
        Assert.Equal(initial, await service.GetStateAsync("pod-a", "music"));
        Assert.Equal("track", Assert.Single(await service.ListDirectoryAsync()).ContentId);
        router.Verify(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        accepted = true;
        await service.PublishAsync(initial with { Action = "stop" });
        Assert.Null(await service.GetStateAsync("pod-a", "music"));
    }

    [Theory]
    [InlineData("unlist", false, false)]
    [InlineData("replace", false, false)]
    [InlineData("stop", false, false)]
    [InlineData("unlist", true, false)]
    [InlineData("replace", true, false)]
    [InlineData("stop", true, false)]
    [InlineData("unlist", false, true)]
    [InlineData("replace", false, true)]
    [InlineData("stop", false, true)]
    [InlineData("renew", false, false)]
    [InlineData("renew", true, false)]
    public async Task Publish_WithdrawsPreviousRoomListing(string change, bool staleRefresh, bool failedIndexWrite)
    {
        var values = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) => values.TryGetValue(key, out var value) ? value : null);
        dht.Setup(instance => instance.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string key, object value, int ttl, CancellationToken token) => values[key] = (byte[])value);
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var initial = await service.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-a",
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        });
        Assert.Equal("party-a", Assert.Single(await CreateService(dht.Object).ListDirectoryAsync()).PartyId);
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (staleRefresh)
        {
            dht.Setup(instance => instance.GetRawAsync(PartyKey, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    var snapshot = values[PartyKey];
                    readStarted.TrySetResult();
                    await readRelease.Task;
                    return snapshot;
                });
        }

        var refresh = staleRefresh ? service.RefreshDirectoryAsync() : null;
        if (refresh != null)
        {
            await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var update = initial with
        {
            PartyId = change is "unlist" or "renew" ? "party-a" : "party-b",
            Title = "Updated title",
            Action = change == "stop" ? "stop" : "play",
            Listed = change != "unlist",
        };
        if (failedIndexWrite)
        {
            dht.Setup(instance => instance.PutAsync(DirectoryIndexKey, It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Index unavailable"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(update));
            Assert.DoesNotContain(await service.RefreshDirectoryAsync(), item => item.PartyId == "party-a");
            dht.Setup(instance => instance.PutAsync(DirectoryIndexKey, It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback((string key, object value, int ttl, CancellationToken token) => values[key] = (byte[])value)
                .Returns(Task.CompletedTask);
        }

        await service.PublishAsync(update);
        readRelease.TrySetResult();
        if (refresh != null)
        {
            await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var expected = change == "replace" ? new[] { "party-b" } : change == "renew" ? new[] { "party-a" } : Array.Empty<string>();
        Assert.Equal(expected, (await service.RefreshDirectoryAsync()).Select(item => item.PartyId));
        Assert.Equal(expected, (await CreateService(dht.Object).ListDirectoryAsync()).Select(item => item.PartyId));
        if (change == "renew")
        {
            Assert.Equal("Updated title", Assert.Single(await service.ListDirectoryAsync()).Title);
        }

        if (change == "unlist")
        {
            var reads = dht.Invocations.Count(call => call.Method.Name == nameof(IMeshDhtClient.GetRawAsync) && Equals(call.Arguments[0], DirectoryIndexKey));
            await service.PublishAsync(update with { Action = "pause" });
            Assert.Equal(reads, dht.Invocations.Count(call => call.Method.Name == nameof(IMeshDhtClient.GetRawAsync) && Equals(call.Arguments[0], DirectoryIndexKey)));
            await service.PublishAsync(initial with { PartyId = "private-room", ChannelId = "private", Listed = false });
            Assert.Equal(reads, dht.Invocations.Count(call => call.Method.Name == nameof(IMeshDhtClient.GetRawAsync) && Equals(call.Arguments[0], DirectoryIndexKey)));
            await service.PublishAsync(initial);
            Assert.Equal("party-a", Assert.Single(await service.RefreshDirectoryAsync()).PartyId);
            Assert.Equal("party-a", Assert.Single(await CreateService(dht.Object).ListDirectoryAsync()).PartyId);
        }
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("unlist")]
    public async Task Publish_ConcurrentRoomsPreserveBothDirectoryEntries(string change)
    {
        var values = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
        var firstIndexWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAnnouncement = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var indexReads = 0;
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(instance => instance.GetRawAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) =>
            {
                if (key == DirectoryIndexKey)
                {
                    Interlocked.Increment(ref indexReads);
                }

                return values.TryGetValue(key, out var value) ? value : null;
            });
        dht.Setup(instance => instance.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async (string key, object value, int ttl, CancellationToken token) =>
            {
                if (key == DirectoryIndexKey && !firstIndexWrite.Task.IsCompleted)
                {
                    firstIndexWrite.TrySetResult();
                    await releaseFirstWrite.Task.WaitAsync(token);
                }

                values[key] = (byte[])value;
                if (key == "slskdn:listening-party:party:party-b")
                {
                    secondAnnouncement.TrySetResult();
                }
            });
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var provider = new ServiceCollection().AddSingleton(storage.Object).AddSingleton(AvailableRooms()).BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        router.Setup(instance => instance.RouteListenAlongMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(true, "message", "pod-a", 0, 0, 0, TimeSpan.Zero));
        var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), dht.Object, router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(), Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));
        var initial = new ListeningPartyEvent
        {
            PartyId = "party-a",
            PodId = "pod-a",
            ChannelId = "music",
            ContentId = "track",
            HostPeerId = "host",
            Action = "play",
            Listed = true,
        };
        var first = service.PublishAsync(initial);
        await firstIndexWrite.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.PublishAsync(initial with { PartyId = "party-b", ChannelId = "other" });
        await secondAnnouncement.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, Volatile.Read(ref indexReads));
        releaseFirstWrite.TrySetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "party-a", "party-b" }, (await CreateService(dht.Object).ListDirectoryAsync()).Select(item => item.PartyId).Order());
        await service.PublishAsync(initial with { PartyId = "party-b", Action = change == "stop" ? "stop" : "play", Listed = false });
        Assert.Equal("party-b", Assert.Single(await CreateService(dht.Object).ListDirectoryAsync()).PartyId);
        await service.PublishAsync(initial with { PartyId = "party-b", Action = "stop", Listed = false });
        Assert.Equal("party-b", Assert.Single(await CreateService(dht.Object).ListDirectoryAsync()).PartyId);
    }

    [Fact]
    public async Task ApplyRemoteMessageAsync_StoresAndOrdersStateWithoutRepublishing()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var pods = new Mock<IPodService>();
        pods.Setup(service => service.GetChannelAsync("pod:00000000000000000000000000000001", "music", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "music" });
        pods.Setup(service => service.GetMembersAsync("pod:00000000000000000000000000000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "peer-host" } });
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var services = new ServiceCollection();
        services.AddScoped(_ => pods.Object);
        services.AddScoped(_ => storage.Object);
        using var provider = services.BuildServiceProvider();
        var router = new Mock<IPodMessageRouter>();
        using var serviceUnderTest = new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(), Mock.Of<IMeshDhtClient>(), router.Object,
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()));

        var play = RemoteMessage("listen-remote-1", "play", sequence: 1, now);
        var stale = RemoteMessage("listen-remote-stale", "pause", sequence: 1, now + 1);
        var pause = RemoteMessage("listen-remote-2", "pause", sequence: 2, now + 2);
        var stop = RemoteMessage("listen-remote-3", "stop", sequence: 3, now + 3);

        Assert.Equal(ListeningPartyRemoteApplyResult.Applied,
            await serviceUnderTest.ApplyRemoteMessageAsync(play, "PEER-HOST"));
        Assert.Equal("Remote track", (await serviceUnderTest.GetStateAsync(play.PodId, play.ChannelId))?.Title);
        Assert.Equal(ListeningPartyRemoteApplyResult.Ignored,
            await serviceUnderTest.ApplyRemoteMessageAsync(stale, "PEER-HOST"));
        Assert.Equal(ListeningPartyRemoteApplyResult.Applied,
            await serviceUnderTest.ApplyRemoteMessageAsync(pause, "PEER-HOST"));
        Assert.Equal("pause", (await serviceUnderTest.GetStateAsync(play.PodId, play.ChannelId))?.Action);
        Assert.Equal(ListeningPartyRemoteApplyResult.Applied,
            await serviceUnderTest.ApplyRemoteMessageAsync(stop, "PEER-HOST"));
        Assert.Null(await serviceUnderTest.GetStateAsync(play.PodId, play.ChannelId));
        storage.Verify(instance => instance.StoreMessageAsync(
            play.PodId, play.ChannelId, It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        router.Verify(instance => instance.RouteListenAlongMessageAsync(
            It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyRemoteMessageAsync_RequiresCurrentMembershipAndMatchingTransportIdentity()
    {
        var message = RemoteMessage("listen-remote-denied", "play", sequence: 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var pods = new Mock<IPodService>();
        pods.Setup(service => service.GetChannelAsync(message.PodId, message.ChannelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = message.ChannelId });
        pods.Setup(service => service.GetMembersAsync(message.PodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "peer-host", IsBanned = true } });
        var storage = new Mock<IPodMessageStorage>();
        var services = new ServiceCollection();
        services.AddScoped(_ => pods.Object);
        services.AddScoped(_ => storage.Object);
        using var provider = services.BuildServiceProvider();
        using var serviceUnderTest = new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(), Mock.Of<IMeshDhtClient>(), Mock.Of<IPodMessageRouter>(),
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()));

        Assert.Equal(ListeningPartyRemoteApplyResult.Forbidden,
            await serviceUnderTest.ApplyRemoteMessageAsync(message, "peer-attacker"));
        Assert.Equal(ListeningPartyRemoteApplyResult.Forbidden,
            await serviceUnderTest.ApplyRemoteMessageAsync(message, "peer-host"));
        storage.Verify(instance => instance.StoreMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyRemoteMessageAsync_IgnoresPartyIdAlreadyActiveInAnotherRoom()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var first = RemoteMessage("listen-remote-owner", "play", sequence: 1, now, listed: true);
        var duplicate = RemoteMessage("listen-remote-duplicate", "play", sequence: 2, now + 1, channelId: "room-b", listed: true);
        var pods = new Mock<IPodService>();
        pods.Setup(instance => instance.GetChannelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string pod, string channel, CancellationToken token) => new PodChannel { ChannelId = channel });
        pods.Setup(instance => instance.GetMembersAsync(first.PodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = first.SenderPeerId } });
        var storage = new Mock<IPodMessageStorage>();
        storage.Setup(instance => instance.StoreMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        using var provider = new ServiceCollection().AddScoped(_ => storage.Object).AddScoped(_ => pods.Object).BuildServiceProvider();
        using var service = new ListeningPartyService(Mock.Of<IHubContext<ListeningPartyHub>>(), Mock.Of<IMeshDhtClient>(),
            Mock.Of<IPodMessageRouter>(), provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(), new TestOptionsMonitor<Options>(new Options()));

        Assert.Equal(ListeningPartyRemoteApplyResult.Applied, await service.ApplyRemoteMessageAsync(first, first.SenderPeerId));
        Assert.Equal(ListeningPartyRemoteApplyResult.Ignored, await service.ApplyRemoteMessageAsync(duplicate, duplicate.SenderPeerId));
        Assert.NotNull(await service.GetStateAsync(first.PodId, "music"));
        Assert.Null(await service.GetStateAsync(first.PodId, "room-b"));
        storage.Verify(instance => instance.StoreMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailedLocalStore_PreservesThePreviouslyAppliedRemoteSnapshot()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var remote = RemoteMessage("listen-remote-before-local", "play", sequence: 1, now);
        var pods = new Mock<IPodService>();
        pods.Setup(service => service.GetChannelAsync(remote.PodId, remote.ChannelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = remote.ChannelId });
        pods.Setup(service => service.GetMembersAsync(remote.PodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = remote.SenderPeerId } });
        var storage = new Mock<IPodMessageStorage>();
        storage.SetupSequence(instance => instance.StoreMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var services = new ServiceCollection();
        services.AddScoped(_ => pods.Object);
        services.AddScoped(_ => storage.Object);
        using var provider = services.BuildServiceProvider();
        using var serviceUnderTest = new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(), Mock.Of<IMeshDhtClient>(), Mock.Of<IPodMessageRouter>(),
            provider.GetRequiredService<IServiceScopeFactory>(), new NowPlayingService(),
            Mock.Of<IStreamTicketService>(), Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()));

        Assert.Equal(ListeningPartyRemoteApplyResult.Applied,
            await serviceUnderTest.ApplyRemoteMessageAsync(remote, remote.SenderPeerId));
        await Assert.ThrowsAsync<ListeningPartyStorageException>(() => serviceUnderTest.PublishAsync(new ListeningPartyEvent
        {
            PartyId = "party-local",
            PodId = remote.PodId,
            ChannelId = remote.ChannelId,
            HostPeerId = "peer-local",
            Action = "play",
            ContentId = "sha256:local-track",
            Title = "Local replacement",
            PositionSeconds = 0,
        }));

        Assert.Equal("Remote track", (await serviceUnderTest.GetStateAsync(remote.PodId, remote.ChannelId))?.Title);
    }

    private static PodMessage RemoteMessage(
        string messageId,
        string action,
        long sequence,
        long timestamp,
        string channelId = "music",
        string partyId = "party:remote",
        bool listed = false)
    {
        var state = new ListeningPartyEvent
        {
            PartyId = partyId,
            PodId = "pod:00000000000000000000000000000001",
            ChannelId = channelId,
            HostPeerId = "peer-host",
            Action = action,
            Listed = listed,
            ContentId = action == "stop" ? string.Empty : "sha256:track",
            Title = "Remote track",
            PositionSeconds = 10,
            ServerTimeUnixMs = timestamp,
            Sequence = sequence,
        };
        return new PodMessage
        {
            MessageId = messageId,
            PodId = state.PodId,
            ChannelId = state.ChannelId,
            SenderPeerId = state.HostPeerId,
            Body = JsonSerializer.Serialize(state, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            TimestampUnixMs = timestamp,
        };
    }

    private static IPodService AvailableRooms()
    {
        var pods = new Mock<IPodService>();
        pods.Setup(instance => instance.GetChannelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string pod, string channel, CancellationToken token) => new PodChannel { ChannelId = channel });
        return pods.Object;
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
