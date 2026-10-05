// <copyright file="PodMessageRoutingControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.PodCore;
using slskd.PodCore.API.Controllers;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class PodMessageRoutingControllerTests
{
    [Fact]
    public async Task RouteMessageToPeers_WithOnlyWhitespacePeerIds_ReturnsBadRequest()
    {
        var router = new Mock<IPodMessageRouter>();
        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = await controller.RouteMessageToPeers(
            new PodMessagePeerRoutingRequest(
                new PodMessage { MessageId = "msg-1", ChannelId = "channel-1" },
                new[] { "   ", "\t" }),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        router.Verify(
            service => service.RouteMessageToPeersAsync(
                It.IsAny<PodMessage>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RouteMessageToPeers_TrimsMessageAndPeerIdsBeforeRouting()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageToPeersAsync(
                It.IsAny<PodMessage>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(
                true,
                "msg-1",
                "pod-1",
                1,
                1,
                0,
                TimeSpan.Zero));

        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = await controller.RouteMessageToPeers(
            new PodMessagePeerRoutingRequest(
                new PodMessage
                {
                    MessageId = " msg-1 ",
                    ChannelId = " channel-1 ",
                    PodId = " pod-1 ",
                    SenderPeerId = " sender-1 "
                },
                new[] { " peer-1 " }),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        router.Verify(
            service => service.RouteMessageToPeersAsync(
                It.Is<PodMessage>(message =>
                    message.MessageId == "msg-1" &&
                    message.ChannelId == "channel-1" &&
                    message.PodId == "pod-1" &&
                    message.SenderPeerId == "sender-1"),
                It.Is<IEnumerable<string>>(peerIds => peerIds.SequenceEqual(new[] { "peer-1" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteMessageToPeers_FiltersDuplicatePeerIdsBeforeRouting()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageToPeersAsync(
                It.IsAny<PodMessage>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(
                true,
                "msg-1",
                "pod-1",
                2,
                2,
                0,
                TimeSpan.Zero));

        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = await controller.RouteMessageToPeers(
            new PodMessagePeerRoutingRequest(
                new PodMessage { MessageId = "msg-1", ChannelId = "channel-1" },
                new[] { "peer-1", " peer-1 ", "peer-2" }),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        router.Verify(
            service => service.RouteMessageToPeersAsync(
                It.IsAny<PodMessage>(),
                It.Is<IEnumerable<string>>(peerIds => peerIds.SequenceEqual(new[] { "peer-1", "peer-2" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteMessage_WhenServiceReturnsFailure_DoesNotLeakErrorMessage()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessageRoutingResult(false, "msg-1", "pod-1", 0, 0, 0, TimeSpan.Zero, "sensitive detail"));

        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = await controller.RouteMessage(
            new PodMessage { MessageId = "msg-1", ChannelId = "channel-1", PodId = "pod-1" },
            CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
        Assert.Contains("Failed to route message", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task RouteMessage_WhenRouterReturnsExternalIdentifiers_EscapesOnlyLogValues()
    {
        var routed = new PodMessageRoutingResult(
            true,
            "message\r\nforged",
            "pod\r\nforged",
            1,
            1,
            0,
            TimeSpan.Zero);
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routed);
        var logger = new CapturingLogger<PodMessageRoutingController>();
        var controller = new PodMessageRoutingController(logger, router.Object);

        var result = await controller.RouteMessage(
            new PodMessage { MessageId = "message-1", ChannelId = "channel-1" },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Same(routed, response.Value);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains("message\\r\\nforged", entry.Message);
        Assert.Contains("pod\\r\\nforged", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task RouteMessage_WhenRouterThrows_EscapesExceptionWithoutAttachingIt()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("router failure\r\nforged"));
        var logger = new CapturingLogger<PodMessageRoutingController>();
        var controller = new PodMessageRoutingController(logger, router.Object);

        var result = await controller.RouteMessage(
            new PodMessage { MessageId = "message-1", ChannelId = "channel-1" },
            CancellationToken.None);

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains("router failure\\r\\nforged", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
    }

    [Theory]
    [InlineData("route")]
    [InlineData("route-to-peers")]
    [InlineData("stats")]
    [InlineData("cleanup")]
    public async Task AsyncAction_WhenCallerCancels_PropagatesCancellation(string actionName)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RouteMessageAsync(It.IsAny<PodMessage>(), cancellation.Token))
            .Returns(Task.FromCanceled<PodMessageRoutingResult>(cancellation.Token));
        router
            .Setup(service => service.RouteMessageToPeersAsync(
                It.IsAny<PodMessage>(), It.IsAny<IEnumerable<string>>(), cancellation.Token))
            .Returns(Task.FromCanceled<PodMessageRoutingResult>(cancellation.Token));
        router
            .Setup(service => service.GetRoutingStatsAsync(cancellation.Token))
            .Returns(Task.FromCanceled<PodMessageRoutingStats>(cancellation.Token));
        router
            .Setup(service => service.CleanupSeenMessagesAsync(cancellation.Token))
            .Returns(Task.FromCanceled<PodMessageCleanupResult>(cancellation.Token));
        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        Task<IActionResult> action = actionName switch
        {
            "route" => controller.RouteMessage(
                new PodMessage { MessageId = "message-1", ChannelId = "channel-1" },
                cancellation.Token),
            "route-to-peers" => controller.RouteMessageToPeers(
                new PodMessagePeerRoutingRequest(
                    new PodMessage { MessageId = "message-1", ChannelId = "channel-1" },
                    new[] { "peer-1" }),
                cancellation.Token),
            "stats" => controller.GetRoutingStats(cancellation.Token),
            "cleanup" => controller.CleanupSeenMessages(cancellation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(actionName)),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => action);
    }

    [Fact]
    public void IsMessageSeen_ReturnsSanitizedSuccessPayload()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.IsMessageSeen("msg-1", "pod-1"))
            .Returns(true);

        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = controller.IsMessageSeen("msg-1", "pod-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("isSeen", ok.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("msg-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pod-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterMessageSeen_ReturnsSanitizedSuccessPayload()
    {
        var router = new Mock<IPodMessageRouter>();
        router
            .Setup(service => service.RegisterMessageSeen("msg-1", "pod-1"))
            .Returns(true);

        var controller = new PodMessageRoutingController(
            NullLogger<PodMessageRoutingController>.Instance,
            router.Object);

        var result = controller.RegisterMessageSeen("msg-1", "pod-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("wasNewlyRegistered", ok.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("msg-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pod-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
