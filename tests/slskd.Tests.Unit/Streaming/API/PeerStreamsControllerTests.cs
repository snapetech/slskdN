// <copyright file="PeerStreamsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming.API;

using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd;
using slskd.Streaming;
using Xunit;
using TestOptionsMonitor = slskd.Tests.Unit.TestOptionsMonitor<slskd.Options>;

public class PeerStreamsControllerTests
{
    private readonly Mock<IPeerStreamTicketService> _tickets = new();
    private readonly Mock<IPeerStreamService> _streams = new();
    private IOptionsMonitor<slskd.Options> _options = new TestOptionsMonitor(new slskd.Options
    {
        Feature = new slskd.Options.FeatureOptions { Streaming = true },
        Soulseek = new slskd.Options.SoulseekOptions { Username = "alice" },
    });

    private PeerStreamsController CreateController()
    {
        var controller = new PeerStreamsController(_tickets.Object, _streams.Object, _options);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "alice"),
        }, "Test"));
        return controller;
    }

    [Fact]
    public void CreateTicket_FeatureDisabled_ReturnsNotFound()
    {
        _options = new TestOptionsMonitor(new slskd.Options
        {
            Feature = new slskd.Options.FeatureOptions { Streaming = false },
        });
        var controller = CreateController();

        var result = controller.CreateTicket(new PeerStreamTicketRequest("peer", "track.mp3", 10));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void CreateTicket_ValidRequest_ReturnsStreamUrl()
    {
        var controller = CreateController();
        _tickets.Setup(x => x.Create(
                It.Is<PeerStreamTicketRequest>(r => r.Username == "peer" && r.Filename == "track.mp3"),
                "user:alice",
                TimeSpan.FromMinutes(2)))
            .Returns(new PeerStreamTicket("ticket-1", "peer", "track.mp3", 10, "user:alice", DateTimeOffset.UtcNow.AddMinutes(2), "audio/mpeg"));

        var result = controller.CreateTicket(new PeerStreamTicketRequest("peer", "track.mp3", 10));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ticket-1", ok.Value?.GetType().GetProperty("ticket")?.GetValue(ok.Value));
        Assert.Equal("/api/v0/peer-streams/ticket-1", ok.Value?.GetType().GetProperty("streamUrl")?.GetValue(ok.Value));
        Assert.Equal("audio/mpeg", ok.Value?.GetType().GetProperty("contentType")?.GetValue(ok.Value));
    }

    [Fact]
    public void CreateTicket_InvalidRequest_ReturnsBadRequest()
    {
        var controller = CreateController();
        _tickets.Setup(x => x.Create(It.IsAny<PeerStreamTicketRequest>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Throws(new ArgumentException("Only audio files can be preview streamed from peers."));

        var result = controller.CreateTicket(new PeerStreamTicketRequest("peer", "archive.zip", 10));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only audio files can be preview streamed from peers.", badRequest.Value);
    }

    [Fact]
    public async Task Get_MissingTicket_ReturnsNotFound()
    {
        var controller = CreateController();
        _streams.Setup(x => x.OpenAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PeerStreamLease?)null);

        var result = await controller.Get("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ValidTicket_ReturnsNonRangeFileStream()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var controller = CreateController();
        _streams.Setup(x => x.OpenAsync("ticket-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeerStreamLease(stream, "audio/mpeg", "user:alice"));

        var result = await controller.Get("ticket-1", CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("audio/mpeg", file.ContentType);
        Assert.False(file.EnableRangeProcessing);
    }

    [Fact]
    public async Task Get_LimiterRejects_Returns429()
    {
        var controller = CreateController();
        _streams.Setup(x => x.OpenAsync("ticket-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PeerStreamLimitException("Too many concurrent peer preview streams."));

        var result = await controller.Get("ticket-1", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, objectResult.StatusCode);
    }
}
