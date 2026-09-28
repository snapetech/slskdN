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
    }

    private static ListeningPartyService CreateService(IMeshDhtClient dht)
    {
        return new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(),
            dht,
            Mock.Of<IPodMessageRouter>(),
            Mock.Of<IServiceScopeFactory>(),
            new NowPlayingService(),
            Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>(),
            new TestOptionsMonitor<Options>(new Options()));
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
