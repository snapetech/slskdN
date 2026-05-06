// <copyright file="ListeningPartyControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.ListeningParty;

using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.ListeningParty;
using slskd.ListeningParty.API;
using slskd.Streaming;

public sealed class ListeningPartyControllerTests
{
    [Fact]
    public async Task Publish_WhenServiceThrowsArgumentException_ReturnsStableError()
    {
        var service = new Mock<IListeningPartyService>();
        service
            .Setup(instance => instance.PublishAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("ContentId /private/file.flac is invalid"));

        var controller = new ListeningPartyController(
            Mock.Of<IContentLocator>(),
            service.Object,
            Mock.Of<IStreamSessionLimiter>(),
            Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()));

        var result = await controller.Publish(
            "pod-a",
            "channel-a",
            new ListeningPartyEvent { Action = "play", ContentId = "content-a" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Listen-along event is invalid.", badRequest.Value);
    }
}
