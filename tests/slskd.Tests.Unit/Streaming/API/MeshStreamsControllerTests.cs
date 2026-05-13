// <copyright file="MeshStreamsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming.API;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Streaming;
using Xunit;
using TestOptionsMonitor = slskd.Tests.Unit.TestOptionsMonitor<slskd.Options>;

public class MeshStreamsControllerTests
{
    private readonly Mock<IMeshStreamTicketService> _tickets = new();
    private readonly Mock<IMeshStreamService> _streams = new();
    private IOptionsMonitor<slskd.Options> _options = new TestOptionsMonitor(new slskd.Options
    {
        Feature = new slskd.Options.FeatureOptions { Streaming = true },
        Soulseek = new slskd.Options.SoulseekOptions { Username = "alice" },
    });

    private MeshStreamsController CreateController()
    {
        var controller = new MeshStreamsController(_tickets.Object, _streams.Object, _options);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "alice"),
        }, "Test"));
        return controller;
    }

    [Fact]
    public void CreateTicket_ValidRequest_ReturnsMeshStreamUrl()
    {
        var controller = CreateController();
        _tickets.Setup(x => x.Create(
                It.Is<MeshStreamTicketRequest>(r => r.ContentId == "content-1" && r.Filename == "track.mp3"),
                "user:alice",
                TimeSpan.FromMinutes(2)))
            .Returns(new MeshStreamTicket("ticket-1", "content-1", "track.mp3", "peer-1", 10, null, "user:alice", DateTimeOffset.UtcNow.AddMinutes(2), "audio/mpeg"));

        var result = controller.CreateTicket(new MeshStreamTicketRequest("content-1", "track.mp3", "peer-1", 10, null));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ticket-1", ok.Value?.GetType().GetProperty("ticket")?.GetValue(ok.Value));
        Assert.Equal("/api/v0/mesh-streams/ticket-1", ok.Value?.GetType().GetProperty("streamUrl")?.GetValue(ok.Value));
        Assert.Equal("mesh", ok.Value?.GetType().GetProperty("source")?.GetValue(ok.Value));
    }

    [Fact]
    public async Task Get_ValidTicket_ReturnsNonRangeFileStream()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var controller = CreateController();
        _streams.Setup(x => x.OpenAsync("ticket-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeshStreamLease(stream, "audio/mpeg", "user:alice"));

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
            .ThrowsAsync(new MeshStreamLimitException("blocked"));

        var result = await controller.Get("ticket-1", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, objectResult.StatusCode);
    }
}
