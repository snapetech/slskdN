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

    private static ListeningPartyService CreateService(IMeshDhtClient dht)
    {
        return new ListeningPartyService(
            Mock.Of<IHubContext<ListeningPartyHub>>(),
            dht,
            Mock.Of<IPodMessageRouter>(),
            Mock.Of<IServiceScopeFactory>(),
            new NowPlayingService(),
            Mock.Of<IStreamTicketService>(),
            Mock.Of<ILogger<ListeningPartyService>>());
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
