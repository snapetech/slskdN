// <copyright file="ListedRadioTransportTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.ListeningParty;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.DhtRendezvous.Messages;
using slskd.DhtRendezvous.Security;
using slskd.ListeningParty;
using slskd.ListeningParty.API;
using slskd.Mesh;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.Streaming;
using slskd.Transfers.MultiSource.Metrics;

public sealed class ListedRadioTransportTests
{
    [Fact]
    public async Task RadioRange_InitialHostFailureReleasesBothReservations()
    {
        var tickets = new MeshStreamTicketService();
        var ticket = tickets.Create(new MeshStreamTicketRequest("track", "tone.wav", "host", 4, null)
        {
            Radio = new MeshRadioScope("party", "capability"),
        }, "listener", TimeSpan.FromMinutes(2));
        var limiter = new StreamSessionLimiter();
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(service => service.FetchRadioAsync("host", "track", It.IsAny<MeshRadioScope>(), 0, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeshContentFetchResult { Error = "Radio permission was revoked." });
        var streams = new MeshStreamService(tickets, limiter, Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());
        await Assert.ThrowsAsync<MeshStreamException>(() => streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None));
        Assert.True(limiter.TryAcquire("listener", 1));
        limiter.Release("listener");
        Assert.True(limiter.TryAcquire("mesh-radio-host:host", 1));
        limiter.Release("mesh-radio-host:host");
    }

    [Fact]
    public async Task ExpiredRadioTicket_DoesNotStartAnyPeerRead()
    {
        var tickets = new MeshStreamTicketService();
        var ticket = tickets.Create(new MeshStreamTicketRequest("track", "tone.wav", "host", 4, null)
        {
            Radio = new MeshRadioScope("party", "capability"),
        }, "listener", TimeSpan.FromSeconds(-1));
        var fetcher = new Mock<IMeshContentFetcher>(MockBehavior.Strict);
        var streams = new MeshStreamService(tickets, new StreamSessionLimiter(), Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());
        Assert.Null(await streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None));
        fetcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RadioRange_ReusesAdmittedTicketButChecksNewTicketFairness()
    {
        var tickets = new MeshStreamTicketService();
        var request = new MeshStreamTicketRequest("track", "tone.wav", "host", 4, null)
        {
            Radio = new MeshRadioScope("party", "capability"),
        };
        var ticket = tickets.Create(request, "listener", TimeSpan.FromMinutes(2));
        var guard = new Mock<IFairnessGuard>();
        var deny = false;
        guard.Setup(service => service.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new FairnessDecision { ThrottleOverlayDownloads = deny });
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(service => service.FetchRadioAsync("host", "track", It.IsAny<MeshRadioScope>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MeshContentFetchResult { Data = new MemoryStream(new byte[4]), Size = 4, SizeValid = true });
        var streams = new MeshStreamService(tickets, new StreamSessionLimiter(), Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>(), guard.Object);
        var initial = await streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None);
        Assert.NotNull(initial);
        await initial.Stream.DisposeAsync();
        deny = true;

        var seek = await streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None);
        Assert.NotNull(seek);
        await seek.Stream.DisposeAsync();
        guard.Verify(service => service.EvaluateAsync(It.IsAny<CancellationToken>()), Times.Once);
        var fresh = tickets.Create(request, "listener", TimeSpan.FromMinutes(2));
        await Assert.ThrowsAsync<MeshStreamLimitException>(() => streams.OpenRangeAsync(fresh.Ticket, 0, 4, CancellationToken.None));
        guard.Verify(service => service.EvaluateAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RadioRange_SignalsOnlyTheSameTicketResponseAndReleasesReservations()
    {
        var tickets = new MeshStreamTicketService();
        var request = new MeshStreamTicketRequest("track", "tone.wav", "host", 4, null)
        {
            Radio = new MeshRadioScope("party", "capability"),
        };
        var ticket = tickets.Create(request, "listener", TimeSpan.FromMinutes(2));
        var other = tickets.Create(request, "other", TimeSpan.FromMinutes(2));
        var limiter = new StreamSessionLimiter();
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(service => service.FetchRadioAsync("host", "track", It.IsAny<MeshRadioScope>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MeshContentFetchResult { Data = new MemoryStream(new byte[4]), Size = 4, SizeValid = true });
        var streams = new MeshStreamService(tickets, limiter, Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());
        var previous = await streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None);
        Assert.NotNull(previous);
        using var cancellation = new CancellationTokenSource();
        var conflict = streams.OpenRangeAsync(other.Ticket, 0, 4, cancellation.Token);
        Assert.False(previous.Superseded.IsCancellationRequested);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conflict);
        using var responseAbort = previous.Superseded.Register(previous.Stream.Dispose);

        var replacement = await streams.OpenRangeAsync(ticket.Ticket, 0, 4, CancellationToken.None);

        Assert.True(previous.Superseded.IsCancellationRequested);
        Assert.NotNull(replacement);
        Assert.False(replacement.Superseded.IsCancellationRequested);
        await previous.Stream.DisposeAsync();
        Assert.False(limiter.TryAcquire("listener", 1));
        await replacement.Stream.DisposeAsync();
        Assert.True(limiter.TryAcquire("listener", 1));
        limiter.Release("listener");
        Assert.True(limiter.TryAcquire("mesh-radio-host:host", 1));
        limiter.Release("mesh-radio-host:host");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RadioRange_WaitsForPreviousReservationOrCancelsWithoutLeaking(bool cancel)
    {
        var tickets = new MeshStreamTicketService();
        var ticket = tickets.Create(new MeshStreamTicketRequest("track", "tone.wav", "host", 4, null)
        {
            Radio = new MeshRadioScope("party", "capability"),
        }, "listener", TimeSpan.FromMinutes(2));
        var limiter = new StreamSessionLimiter();
        Assert.True(limiter.TryAcquire("mesh-radio-host:host", 1));
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(service => service.FetchRadioAsync("host", "track", It.IsAny<MeshRadioScope>(), 0, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MeshContentFetchResult { Data = new MemoryStream(new byte[4]), Size = 4, SizeValid = true });
        var streams = new MeshStreamService(tickets, limiter, Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());
        using var cancellation = new CancellationTokenSource();
        var pending = streams.OpenRangeAsync(ticket.Ticket, 0, 4, cancellation.Token);
        Assert.False(pending.IsCompleted);
        Assert.True(limiter.TryAcquire("listener", 1));
        limiter.Release("listener");

        if (cancel)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            limiter.Release("mesh-radio-host:host");
        }
        else
        {
            limiter.Release("mesh-radio-host:host");
            var lease = await pending;
            Assert.NotNull(lease);
            await lease.Stream.DisposeAsync();
        }

        Assert.True(limiter.TryAcquire("listener", 1));
        limiter.Release("listener");
        Assert.True(limiter.TryAcquire("mesh-radio-host:host", 1));
        limiter.Release("mesh-radio-host:host");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Host_Read_RechecksPermissionAndRejectsAnotherContentOrParty(bool revoke)
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4 });
            var state = new ListeningPartyEvent { PartyId = "party", ContentId = "track", Listed = true, AllowMeshStreaming = true };
            var parties = new Mock<IListeningPartyService>();
            parties.Setup(service => service.GetStateByPartyIdAsync("party", It.IsAny<CancellationToken>())).ReturnsAsync(() => state);
            var tickets = new StreamTicketService();
            var capability = tickets.Create("track", "listening-party:party", TimeSpan.FromMinutes(2));
            var locator = new Mock<IContentLocator>();
            locator.Setup(service => service.Resolve("track", It.IsAny<CancellationToken>())).Returns(new ResolvedContent(path, 4, "audio/wav"));
            var host = new ListedRadioMeshService(parties.Object, tickets, locator.Object, EnabledOptions(), Mock.Of<ILogger<ListedRadioMeshService>>());

            var first = await host.HandleCallAsync(Call("Read", new ListedRadioRequest("party", "track", capability, 1, 2)), Context());
            Assert.Equal(ServiceStatusCodes.OK, first.StatusCode);
            Assert.Equal(new byte[] { 2, 3 }, first.Payload);
            state = revoke ? state with { AllowMeshStreaming = false } : state with { ContentId = "new-track" };
            var denied = await host.HandleCallAsync(Call("Read", new ListedRadioRequest("party", "track", capability, 1, 2)), Context());
            Assert.Equal(404, denied.StatusCode);
            Assert.Empty(denied.Payload);
            locator.Verify(service => service.Resolve("track", It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("other-party", "track", 0, 1)]
    [InlineData("party", "other-track", 0, 1)]
    [InlineData("party", "track", -1, 1)]
    [InlineData("party", "track", 0, 0)]
    [InlineData("party", "track", 0, 45057)]
    public async Task Host_InvalidScopeOrRange_DoesNotResolveContent(string party, string content, long offset, int length)
    {
        var state = new ListeningPartyEvent { PartyId = "party", ContentId = "track", Listed = true, AllowMeshStreaming = true };
        var parties = new Mock<IListeningPartyService>();
        parties.Setup(service => service.GetStateByPartyIdAsync("party", It.IsAny<CancellationToken>())).ReturnsAsync(state);
        var tickets = new StreamTicketService();
        var capability = tickets.Create("track", "listening-party:other-party", TimeSpan.FromMinutes(2));
        var locator = new Mock<IContentLocator>();
        var host = new ListedRadioMeshService(parties.Object, tickets, locator.Object, EnabledOptions(), Mock.Of<ILogger<ListedRadioMeshService>>());

        var reply = await host.HandleCallAsync(Call("Read", new ListedRadioRequest(party, content, capability, offset, length)), Context());

        Assert.NotEqual(ServiceStatusCodes.OK, reply.StatusCode);
        Assert.Empty(reply.Payload);
        locator.Verify(service => service.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Listener_HostMetadata_CreatesPinnedScopedTicketAndStreamsRange()
    {
        var path = Path.Combine(Path.GetTempPath(), $"radio-{Guid.NewGuid():N}.wav");
        try
        {
            var bytes = Enumerable.Range(0, 50000).Select(value => (byte)(value % 251)).ToArray();
            await File.WriteAllBytesAsync(path, bytes);
            var hostParties = new Mock<IListeningPartyService>();
            hostParties.Setup(service => service.GetStateByPartyIdAsync("party", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListeningPartyEvent { PartyId = "party", ContentId = "track", Listed = true, AllowMeshStreaming = true });
            var hostTickets = new StreamTicketService();
            var capability = hostTickets.Create("track", "listening-party:party", TimeSpan.FromMinutes(2));
            var locator = new Mock<IContentLocator>();
            locator.Setup(service => service.Resolve("track", It.IsAny<CancellationToken>())).Returns(new ResolvedContent(path, bytes.Length, "audio/wav"));
            var host = new ListedRadioMeshService(hostParties.Object, hostTickets, locator.Object, EnabledOptions(), Mock.Of<ILogger<ListedRadioMeshService>>());
            var client = new Mock<IMeshServiceClient>();
            client.Setup(service => service.CallAsync("host-overlay", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
                .Returns((string peer, ServiceCall call, CancellationToken token) => host.HandleCallAsync(call, Context(), token));
            var listenerParties = new Mock<IListeningPartyService>();
            listenerParties.Setup(service => service.ListDirectoryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new ListeningPartyAnnouncement
                    {
                        PartyId = "party",
                        ContentId = "track",
                        HostPeerId = "web-account",
                        TransportUsername = "host-overlay",
                        StreamTicket = capability,
                        AllowMeshStreaming = true,
                        ExpiresAtUnixMs = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeMilliseconds(),
                    },
                });
            var localTickets = new MeshStreamTicketService();
            var controller = new ListedRadioController(listenerParties.Object, client.Object, localTickets, EnabledOptions(), new StreamTicketService())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            };
            controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "listener") }, "test"));
            var response = Assert.IsType<OkObjectResult>(await controller.CreateTicket("party", new ListedRadioSelection("track"), CancellationToken.None));
            var url = Assert.IsType<string>(response.Value!.GetType().GetProperty("streamUrl")!.GetValue(response.Value));
            var ticket = url.Split('/').Last();
            var claims = Assert.IsType<MeshStreamTicket>(localTickets.Validate(ticket));
            Assert.Equal("host-overlay", claims.PeerId);
            Assert.Equal("user:listener", claims.OwnerKey);
            Assert.Equal(new MeshRadioScope("party", capability), claims.Radio);
            var limiter = new StreamSessionLimiter();
            var directory = new Mock<IMeshDirectory>(MockBehavior.Strict);
            var streams = new MeshStreamService(localTickets, limiter, directory.Object, new MeshContentFetcher(client.Object, Mock.Of<ILogger<MeshContentFetcher>>()), Mock.Of<ILogger<MeshStreamService>>());

            var lease = await streams.OpenRangeAsync(ticket, 123, 49999, CancellationToken.None);
            Assert.NotNull(lease);
            var competing = localTickets.Create(new MeshStreamTicketRequest(claims.ContentId, claims.Filename, claims.PeerId, claims.ExpectedSize, null)
            {
                Radio = claims.Radio,
            }, "user:second", TimeSpan.FromMinutes(2));
            await Assert.ThrowsAsync<MeshStreamLimitException>(() => streams.OpenAsync(competing.Ticket, CancellationToken.None));
            Assert.True(limiter.TryAcquire("user:second", 1));
            limiter.Release("user:second");
            await using (lease.Stream)
            {
                using var result = new MemoryStream();
                await lease.Stream.CopyToAsync(result);
                Assert.Equal(bytes[123..49999], result.ToArray());
            }

            Assert.True(limiter.TryAcquire("user:listener", 1));
            limiter.Release("user:listener");
            Assert.True(limiter.TryAcquire("mesh-radio-host:host-overlay", 1));
            limiter.Release("mesh-radio-host:host-overlay");
            client.Verify(service => service.CallAsync("host-overlay", It.Is<ServiceCall>(call => call.ServiceName == "ListedRadio" && call.Method == "Read"), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MaximumRadioReply_FitsOverlayFrame()
    {
        using var memory = new MemoryStream();
        using var framer = new SecureMessageFramer(memory);
        await framer.WriteMessageAsync(new MeshServiceReplyMessage
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            StatusCode = ServiceStatusCodes.OK,
            Payload = new byte[ListedRadioMeshService.MaxChunkBytes],
        });
        Assert.True(memory.Length < OverlayProtocol.MaxMessageSize);
    }

    [Fact]
    public async Task Router_RadioBudget_PreservesTheGlobalPeerLimit()
    {
        var host = new Mock<IMeshService>();
        host.SetupGet(service => service.ServiceName).Returns("ListedRadio");
        host.Setup(service => service.HandleCallAsync(It.IsAny<ServiceCall>(), It.IsAny<MeshServiceContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply { StatusCode = ServiceStatusCodes.OK });
        var router = new MeshServiceRouter(Mock.Of<ILogger<MeshServiceRouter>>(), Microsoft.Extensions.Options.Options.Create(new MeshServiceFabricOptions()));
        router.RegisterService(host.Object);
        for (var index = 0; index < 500; index++)
        {
            var reply = await router.RouteAsync(Call("Metadata", new ListedRadioRequest("party", "track", "capability")), "listener", "certificate");
            Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        }

        var limited = await router.RouteAsync(Call("Metadata", new ListedRadioRequest("party", "track", "capability")), "listener", "certificate");
        Assert.Equal(ServiceStatusCodes.RateLimited, limited.StatusCode);
    }

    private static ServiceCall Call(string method, ListedRadioRequest request) => new()
    {
        ServiceName = "ListedRadio",
        Method = method,
        CorrelationId = Guid.NewGuid().ToString("N"),
        Payload = JsonSerializer.SerializeToUtf8Bytes(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
    };

    private static MeshServiceContext Context() => new() { RemotePeerId = "listener-overlay" };

    private static TestOptionsMonitor<Options> EnabledOptions() => new(new Options
    {
        Feature = new Options.FeatureOptions { Mesh = true, Streaming = true },
        Soulseek = new Options.SoulseekOptions { Username = "listener-overlay" },
    });
}
