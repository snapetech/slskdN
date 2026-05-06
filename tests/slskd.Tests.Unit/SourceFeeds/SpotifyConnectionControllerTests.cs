// <copyright file="SpotifyConnectionControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SourceFeeds;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.SourceFeeds;
using slskd.SourceFeeds.API;

public sealed class SpotifyConnectionControllerTests
{
    [Fact]
    public void Authorize_WhenServiceThrows_ReturnsStableError()
    {
        var service = new Mock<ISpotifyConnectionService>();
        service
            .Setup(instance => instance.BeginAuthorization(It.IsAny<string>()))
            .Throws(new InvalidOperationException("client secret missing"));

        var controller = CreateController(service.Object);

        var result = controller.Authorize();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Spotify authorization is not configured.", badRequest.Value);
    }

    [Fact]
    public async Task Callback_WhenProviderReturnsError_DoesNotReflectErrorQuery()
    {
        var service = new Mock<ISpotifyConnectionService>();
        var controller = CreateController(service.Object);

        var result = await controller.Callback("state", "code", "access_denied sensitive-detail", CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Contains("Spotify authorization failed.", content.Content);
        Assert.DoesNotContain("sensitive-detail", content.Content);
    }

    [Fact]
    public async Task Callback_WhenServiceThrows_ReturnsStableError()
    {
        var service = new Mock<ISpotifyConnectionService>();
        service
            .Setup(instance => instance.CompleteAuthorizationAsync("state", "code", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("authorization state contains sensitive detail"));

        var controller = CreateController(service.Object);

        var result = await controller.Callback("state", "code", string.Empty, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Spotify authorization could not be completed.", badRequest.Value);
    }

    private static SpotifyConnectionController CreateController(ISpotifyConnectionService service)
    {
        var controller = new SpotifyConnectionController(
            service,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("slskdn.test");
        return controller;
    }
}
