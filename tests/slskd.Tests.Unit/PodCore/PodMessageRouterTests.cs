// <copyright file="PodMessageRouterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh.Overlay;
using slskd.Mesh.Privacy;
using slskd.Mesh.ServiceFabric;
using slskd.PodCore;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

namespace slskd.Tests.Unit.PodCore;

/// <summary>
/// Unit tests for PodMessageRouter. PR-13: envelope signing, PeerResolution, no hardcoded loopback.
/// </summary>
public class PodMessageRouterTests
{
    [Fact]
    public async Task RouteMessageAsync_EscapesRemoteFieldsAndExceptionWithoutChangingInputs()
    {
        const string podId = "pod-1\r\nforged pod";
        const string channelId = "general\r\nforged channel";
        const string messageId = "message-1\r\nforged id";
        const string body = "private message body";
        var logger = new CapturingLogger<PodMessageRouter>();
        var podService = new Mock<IPodService>();
        podService
            .Setup(service => service.GetChannelAsync(podId, channelId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"failure for {body}\r\nforged exception line and {channelId}"));
        var router = new PodMessageRouter(
            logger,
            podService.Object,
            Mock.Of<IOverlayClient>(),
            Mock.Of<IControlSigner>(),
            Mock.Of<IPeerResolutionService>());

        var message = new PodMessage
        {
            MessageId = messageId,
            PodId = podId,
            ChannelId = channelId,
            SenderPeerId = "peer-sender",
            Body = body,
            TimestampUnixMs = 1,
        };
        var result = await router.RouteMessageAsync(message);

        Assert.False(result.Success);
        Assert.Equal(podId, result.PodId);
        Assert.Equal(messageId, result.MessageId);
        podService.Verify(service => service.GetChannelAsync(podId, channelId, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(2, logger.Entries.Count);
        var messages = string.Join(" | ", logger.Entries.Select(entry => entry.Message));
        Assert.Contains("message-1\\r\\nforged id", messages);
        Assert.Contains("pod-1\\r\\nforged pod", messages);
        Assert.Contains("general\\r\\nforged channel", messages);
        Assert.Contains("forged exception line", messages);
        Assert.DoesNotContain(body, messages);
        Assert.All(logger.Entries, entry =>
        {
            Assert.DoesNotContain("\r", entry.Message);
            Assert.DoesNotContain("\n", entry.Message);
            Assert.Null(entry.Exception);
        });
    }

    [Fact]
    public async Task RouteListenAlongMessageAsync_UsesAuthenticatedPodsServiceAndSkipsSenderAndBannedMembers()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();
        var meshClient = new Mock<IMeshServiceClient>();
        ServiceCall? routedCall = null;
        podService.Setup(service => service.GetChannelAsync("pod1", "music", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "music" });
        podService.Setup(service => service.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PodMember>
            {
                new() { PeerId = "PEER-HOST" },
                new() { PeerId = "peer-listener" },
                new() { PeerId = "peer-banned", IsBanned = true },
            });
        meshClient.Setup(client => client.CallAsync("peer-listener", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Callback<string, ServiceCall, CancellationToken>((_, call, _) => routedCall = call)
            .ReturnsAsync(new ServiceReply { StatusCode = ServiceStatusCodes.OK });

        var router = new PodMessageRouter(
            logger.Object, podService.Object, overlayClient.Object, controlSigner.Object,
            peerResolution.Object, meshServiceClient: meshClient.Object);
        var message = new PodMessage
        {
            MessageId = "listen-1",
            PodId = "pod1",
            ChannelId = "music",
            SenderPeerId = "peer-host",
            Body = "{}",
            TimestampUnixMs = 1,
        };

        var result = await router.RouteListenAlongMessageAsync(message);

        Assert.True(result.Success);
        Assert.Equal(1, result.TargetPeerCount);
        Assert.Equal(1, result.SuccessfullyRoutedCount);
        Assert.Equal("{}", ServicePayloadParser.TryParseJson<PodMessage>(routedCall!).Value?.Body);
        meshClient.Verify(client => client.CallAsync("peer-listener", It.Is<ServiceCall>(call =>
            call.ServiceName == "pods" && call.Method == "ApplyListenAlong"), It.IsAny<CancellationToken>()), Times.Once);
        meshClient.Verify(client => client.CallAsync("peer-host", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()), Times.Never);
        meshClient.Verify(client => client.CallAsync("peer-banned", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()), Times.Never);
        overlayClient.Verify(client => client.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteListenAlongMessageAsync_ReportsUnavailableAuthenticatedPeer()
    {
        var podService = new Mock<IPodService>();
        var meshClient = new Mock<IMeshServiceClient>();
        podService.Setup(service => service.GetChannelAsync("pod1", "music", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "music" });
        podService.Setup(service => service.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PodMember> { new() { PeerId = "peer-listener" } });
        meshClient.Setup(client => client.CallAsync("peer-listener", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply { StatusCode = ServiceStatusCodes.ServiceUnavailable });
        var router = new PodMessageRouter(
            Mock.Of<ILogger<PodMessageRouter>>(), podService.Object, Mock.Of<IOverlayClient>(),
            Mock.Of<IControlSigner>(), Mock.Of<IPeerResolutionService>(), meshServiceClient: meshClient.Object);

        var result = await router.RouteListenAlongMessageAsync(new PodMessage
        {
            MessageId = "listen-2",
            PodId = "pod1",
            ChannelId = "music",
            SenderPeerId = "peer-host",
            Body = "{}",
            TimestampUnixMs = 1,
        });

        Assert.False(result.Success);
        Assert.Equal(1, result.FailedRoutingCount);
        Assert.Equal(new[] { "peer-listener" }, result.FailedPeerIds);
    }

    [Fact]
    public async Task RouteListenAlongMessageAsync_StopsFanOutAtTheOverallBudget()
    {
        var podService = new Mock<IPodService>();
        var meshClient = new Mock<IMeshServiceClient>();
        podService.Setup(service => service.GetChannelAsync("pod1", "music", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "music" });
        podService.Setup(service => service.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 40).Select(index => new PodMember { PeerId = $"peer-{index}" }).ToList());
        meshClient.Setup(client => client.CallAsync(It.IsAny<string>(), It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall _, CancellationToken token) => WaitForCancellationAsync(token));
        var router = new PodMessageRouter(
            Mock.Of<ILogger<PodMessageRouter>>(), podService.Object, Mock.Of<IOverlayClient>(),
            Mock.Of<IControlSigner>(), Mock.Of<IPeerResolutionService>(), meshServiceClient: meshClient.Object);

        var result = await router.RouteListenAlongMessageAsync(new PodMessage
        {
            MessageId = "listen-budget",
            PodId = "pod1",
            ChannelId = "music",
            SenderPeerId = "peer-host",
            Body = "{}",
            TimestampUnixMs = 1,
        });

        Assert.False(result.Success);
        Assert.Equal(40, result.TargetPeerCount);
        Assert.Equal(40, result.FailedRoutingCount);
        Assert.Contains("2-second time budget", result.ErrorMessage);
        Assert.True(result.RoutingDuration < TimeSpan.FromSeconds(4), $"Routing took {result.RoutingDuration}");
    }

    private static async Task<ServiceReply> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return new ServiceReply { StatusCode = ServiceStatusCodes.OK };
    }

    [Fact]
    public async Task RouteMessageToPeersAsync_when_peer_resolution_returns_null_fails_for_that_peer()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();

        controlSigner.Setup(c => c.Sign(It.IsAny<ControlEnvelope>())).Returns<ControlEnvelope>(e => e);
        peerResolution.Setup(r => r.ResolvePeerIdToEndpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPEndPoint?)null);

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer: null);

        var message = new PodMessage
        {
            MessageId = "msg-1",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        podService.Setup(s => s.GetChannelAsync("pod1", "general", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "general", Name = "General" });
        podService.Setup(s => s.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PodMember> { new PodMember { PeerId = "peer-sender", Role = "member" }, new PodMember { PeerId = "peer-other", Role = "member" } });

        var result = await router.RouteMessageToPeersAsync(message, new[] { "peer-other" });

        Assert.Equal(1, result.FailedRoutingCount);
        Assert.Equal(0, result.SuccessfullyRoutedCount);
        overlayClient.Verify(o => o.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteMessageToPeersAsync_when_peer_resolution_returns_endpoint_calls_SendAsync_with_it()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();

        var resolvedEp = new IPEndPoint(IPAddress.Loopback, 9000);
        controlSigner.Setup(c => c.Sign(It.IsAny<ControlEnvelope>())).Returns<ControlEnvelope>(e => e);
        peerResolution.Setup(r => r.ResolvePeerIdToEndpointAsync("peer-other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedEp);
        overlayClient.Setup(o => o.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer: null);

        var message = new PodMessage
        {
            MessageId = "msg-2",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        var result = await router.RouteMessageToPeersAsync(message, new[] { "peer-other" });

        Assert.Equal(1, result.SuccessfullyRoutedCount);
        Assert.Equal(0, result.FailedRoutingCount);
        overlayClient.Verify(o => o.SendAsync(It.IsAny<ControlEnvelope>(), resolvedEp, It.IsAny<CancellationToken>()), Times.Once);
        controlSigner.Verify(c => c.Sign(It.Is<ControlEnvelope>(e => e.Type == "pod_message")), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();

        podService
            .Setup(s => s.GetChannelAsync("pod1", "general", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer: null);

        var message = new PodMessage
        {
            MessageId = "msg-3",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        var result = await router.RouteMessageAsync(message);

        Assert.False(result.Success);
        Assert.Equal("Failed to route message", result.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenCancelledDuringFanOut_PropagatesAndAllowsRetry()
    {
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var peerResolution = new Mock<IPeerResolutionService>();
        var resolvedEndpoint = new IPEndPoint(IPAddress.Loopback, 9001);
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        podService.Setup(service => service.GetChannelAsync("pod1", "general", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "general" });
        podService.Setup(service => service.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PodMember> { new() { PeerId = "peer-recipient" } });
        peerResolution.Setup(service => service.ResolvePeerIdToEndpointAsync("peer-recipient", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedEndpoint);
        overlayClient.Setup(client => client.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
            .Callback<ControlEnvelope, IPEndPoint, CancellationToken>((_, _, _) => sendStarted.TrySetResult(true))
            .Returns((ControlEnvelope _, IPEndPoint _, CancellationToken token) => WaitForOverlayCancellationAsync(token));
        var router = new PodMessageRouter(
            Mock.Of<ILogger<PodMessageRouter>>(), podService.Object, overlayClient.Object,
            Mock.Of<IControlSigner>(), peerResolution.Object);
        var message = new PodMessage
        {
            MessageId = "msg-cancelled-route",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1,
        };

        using var cancellation = new CancellationTokenSource();
        var route = router.RouteMessageAsync(message, cancellation.Token);
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => route);
        Assert.False(router.IsMessageSeen(message.MessageId, message.PodId));

        overlayClient.Setup(client => client.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var retry = await router.RouteMessageAsync(message);

        Assert.True(retry.Success);
        Assert.True(router.IsMessageSeen(message.MessageId, message.PodId));
    }

    private static async Task<bool> WaitForOverlayCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return true;
    }

    [Fact]
    public async Task RouteMessageToPeersAsync_TrimsAndDeduplicatesTargetPeers()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();

        var resolvedEp = new IPEndPoint(IPAddress.Loopback, 9001);
        controlSigner.Setup(c => c.Sign(It.IsAny<ControlEnvelope>())).Returns<ControlEnvelope>(e => e);
        peerResolution.Setup(r => r.ResolvePeerIdToEndpointAsync("peer-other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedEp);
        overlayClient.Setup(o => o.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer: null);

        var message = new PodMessage
        {
            MessageId = "msg-4",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        var result = await router.RouteMessageToPeersAsync(message, new[] { " peer-other ", "peer-other", "PEER-OTHER", " " });

        Assert.Equal(1, result.TargetPeerCount);
        Assert.Equal(1, result.SuccessfullyRoutedCount);
        overlayClient.Verify(o => o.SendAsync(It.IsAny<ControlEnvelope>(), resolvedEp, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageToPeersAsync_WhenPrivacyQueuesPayload_DoesNotReportSuccess()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();
        var privacyLayer = new Mock<IPrivacyLayer>();

        privacyLayer.SetupGet(p => p.IsEnabled).Returns(true);
        privacyLayer
            .Setup(p => p.ProcessOutboundMessageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer.Object);

        var message = new PodMessage
        {
            MessageId = "msg-5",
            PodId = "pod1",
            ChannelId = "general",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        var result = await router.RouteMessageToPeersAsync(message, new[] { "peer-other" });

        Assert.False(result.Success);
        Assert.Equal(0, result.SuccessfullyRoutedCount);
        Assert.Equal(1, result.FailedRoutingCount);
        overlayClient.Verify(o => o.SendAsync(It.IsAny<ControlEnvelope>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteMessageAsync_UsesSeparatePodIdAndChannelIdFields()
    {
        var logger = new Mock<ILogger<PodMessageRouter>>();
        var podService = new Mock<IPodService>();
        var overlayClient = new Mock<IOverlayClient>();
        var controlSigner = new Mock<IControlSigner>();
        var peerResolution = new Mock<IPeerResolutionService>();

        var resolvedEp = new IPEndPoint(IPAddress.Loopback, 9002);
        controlSigner.Setup(c => c.Sign(It.IsAny<ControlEnvelope>())).Returns<ControlEnvelope>(e => e);
        podService.Setup(s => s.GetChannelAsync("pod1", "general", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodChannel { ChannelId = "general", Name = "General" });
        podService.Setup(s => s.GetMembersAsync("pod1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PodMember>
            {
                new() { PeerId = "peer-sender", Role = "member" },
                new() { PeerId = "peer-other", Role = "member" }
            });
        peerResolution.Setup(r => r.ResolvePeerIdToEndpointAsync("peer-other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedEp);
        overlayClient.Setup(o => o.SendAsync(It.IsAny<ControlEnvelope>(), resolvedEp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var router = new PodMessageRouter(
            logger.Object,
            podService.Object,
            overlayClient.Object,
            controlSigner.Object,
            peerResolution.Object,
            privacyLayer: null);

        var message = new PodMessage
        {
            MessageId = "msg-6",
            PodId = " pod1 ",
            ChannelId = " general ",
            SenderPeerId = "peer-sender",
            Body = "hi",
            TimestampUnixMs = 1
        };

        var result = await router.RouteMessageAsync(message);

        Assert.True(result.Success);
        Assert.Equal("pod1", result.PodId);
        Assert.Equal(1, result.SuccessfullyRoutedCount);
        podService.Verify(s => s.GetChannelAsync("pod1", "general", It.IsAny<CancellationToken>()), Times.Once);
        overlayClient.Verify(o => o.SendAsync(It.IsAny<ControlEnvelope>(), resolvedEp, It.IsAny<CancellationToken>()), Times.Once);
    }
}
