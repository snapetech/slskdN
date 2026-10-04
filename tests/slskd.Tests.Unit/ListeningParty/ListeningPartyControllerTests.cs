// <copyright file="ListeningPartyControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.ListeningParty;

using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.ListeningParty;
using slskd.ListeningParty.API;
using slskd.Streaming;
using slskd.PodCore;
using slskd.Tests.Unit.PodCore;

public sealed class ListeningPartyControllerTests
{
    [Theory]
    [InlineData(true, 404)]
    [InlineData(false, 503)]
    public async Task Publish_WhenRoomOrStorageIsUnavailable_ReturnsStableFailure(bool missingRoom, int status)
    {
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(missingRoom ? new ListeningPartyRoomNotFoundException() : new ListeningPartyStorageException());
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));
        var result = Assert.IsAssignableFrom<ObjectResult>(await controller.Publish("pod-a", "music",
            new ListeningPartyEvent { Action = "play", ContentId = "track" }, CancellationToken.None));
        Assert.Equal(status, result.StatusCode);
        if (!missingRoom) Assert.Contains("room_storage_unavailable", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task Publish_WhenRoomIsAtCapacity_ReturnsRetryableLimit()
    {
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ListeningPartyCapacityException());
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));
        var result = Assert.IsType<ObjectResult>(await controller.Publish("pod-a", "room",
            new ListeningPartyEvent { Action = "play", ContentId = "track" }, CancellationToken.None));
        Assert.Equal(429, result.StatusCode);
        Assert.Equal("Too many room updates are pending. Retry later.", result.Value);
    }

    [Fact]
    public async Task Publish_WhenServiceThrowsArgumentException_ReturnsStableError()
    {
        var service = new Mock<IListeningPartyService>();
        service
            .Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("ContentId /private/file.flac is invalid"));

        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(),
            service.Object,
            Mock.Of<IStreamSessionLimiter>(),
            Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<IPodService>()));

        var result = await controller.Publish(
            "pod-a",
            "channel-a",
            new ListeningPartyEvent { Action = "play", ContentId = "content-a" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Listen-along event is invalid.", badRequest.Value);
    }

    [Fact]
    public async Task Publish_ForwardsHostSessionFenceAndStartIntent()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), sessionId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListeningPartyEvent { PartyId = "party", PodId = "pod-a", ChannelId = "music", Action = "play" });
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));
        controller.Request.Headers["X-Listen-Along-Host-Session"] = sessionId;
        controller.Request.Headers["X-Listen-Along-Start-Host-Session"] = "true";

        var result = await controller.Publish("pod-a", "music", new ListeningPartyEvent { Action = "play", ContentId = "track" }, CancellationToken.None,
            sessionId, true);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(instance => instance.PublishHostEventAsync(
            It.Is<ListeningPartyEvent>(party => party.PodId == "pod-a" && party.ChannelId == "music"),
            sessionId, true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RenewHostSession_RequiresRoomAccessAndForwardsOwnership()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var service = new Mock<IListeningPartyService>();
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));
        controller.Request.Headers["X-Listen-Along-Host-Session"] = sessionId;

        var result = await controller.RenewHostSession("pod-a", "music", new ListeningPartyHostSessionRenewal("party"), CancellationToken.None, sessionId);

        Assert.IsType<NoContentResult>(result);
        service.Verify(instance => instance.RenewHostSessionAsync("pod-a", "music", "party", sessionId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Publish_ReportsSupersededHostSessionAsConflict()
    {
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ListeningPartyHostSessionConflictException("host_session_replaced"));
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));

        var result = await controller.Publish("pod-a", "music", new ListeningPartyEvent { Action = "pause", ContentId = "track" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("host_session_replaced", System.Text.Json.JsonSerializer.Serialize(conflict.Value));
    }

    [Fact]
    public async Task Publish_ReportsPartyIdOwnershipConflict()
    {
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ListeningPartyIdConflictException());
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));

        var result = await controller.Publish("pod-a", "music", new ListeningPartyEvent { Action = "play", ContentId = "track" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("party_id_in_use", System.Text.Json.JsonSerializer.Serialize(conflict.Value));
    }

    [Fact]
    public async Task Publish_ReportsDirectoryPreflightFailureAsRetryableUnavailable()
    {
        var service = new Mock<IListeningPartyService>();
        service.Setup(instance => instance.PublishHostEventAsync(It.IsAny<ListeningPartyEvent>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ListeningPartyDirectoryUnavailableException(new InvalidOperationException()));
        var controller = PodControllerTestContext.AsAdministrator(new ListeningPartyController(
            Mock.Of<IContentLocator>(), service.Object, Mock.Of<IStreamSessionLimiter>(), Mock.Of<IStreamTicketService>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()), Mock.Of<IPodService>()));

        var result = Assert.IsType<ObjectResult>(await controller.Publish("pod-a", "music",
            new ListeningPartyEvent { Action = "play", ContentId = "track" }, CancellationToken.None));

        Assert.Equal(503, result.StatusCode);
        Assert.Contains("party_directory_unavailable", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }
}
