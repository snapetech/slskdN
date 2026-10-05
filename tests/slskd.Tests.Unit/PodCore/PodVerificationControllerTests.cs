// <copyright file="PodVerificationControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.PodCore;
using slskd.PodCore.API.Controllers;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class PodVerificationControllerTests
{
    [Fact]
    public async Task VerifyMembership_RequestCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var verifier = new Mock<IPodMembershipVerifier>();
        verifier
            .Setup(service => service.VerifyMembershipAsync("pod-1", "peer-1", cancellation.Token))
            .Returns(Task.FromCanceled<MembershipVerificationResult>(cancellation.Token));
        var controller = new PodVerificationController(NullLogger<PodVerificationController>.Instance, verifier.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.VerifyMembership("pod-1", "peer-1", cancellation.Token));
    }

    [Fact]
    public async Task VerifyMessage_RequestCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var verifier = new Mock<IPodMembershipVerifier>();
        verifier
            .Setup(service => service.VerifyMessageAsync(It.IsAny<PodMessage>(), cancellation.Token))
            .Returns(Task.FromCanceled<MessageVerificationResult>(cancellation.Token));
        var controller = new PodVerificationController(NullLogger<PodVerificationController>.Instance, verifier.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.VerifyMessage(
                new PodMessage
                {
                    MessageId = "message-1",
                    PodId = "pod-1",
                    ChannelId = "pod-1:general",
                    SenderPeerId = "peer-1",
                    Signature = "signature",
                },
                cancellation.Token));
    }

    [Fact]
    public async Task CheckRole_RequestCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var verifier = new Mock<IPodMembershipVerifier>();
        verifier
            .Setup(service => service.HasRoleAsync("pod-1", "peer-1", "moderator", cancellation.Token))
            .Returns(Task.FromCanceled<bool>(cancellation.Token));
        var controller = new PodVerificationController(NullLogger<PodVerificationController>.Instance, verifier.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.CheckRole("pod-1", "peer-1", "moderator", cancellation.Token));
    }

    [Fact]
    public async Task CheckRole_TrimsRouteArgumentsBeforeDispatch()
    {
        var verifier = new Mock<IPodMembershipVerifier>();
        verifier
            .Setup(service => service.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new PodVerificationController(
            NullLogger<PodVerificationController>.Instance,
            verifier.Object);

        var result = await controller.CheckRole(" pod-1 ", " peer-1 ", " moderator ", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        verifier.Verify(
            service => service.HasRoleAsync("pod-1", "peer-1", "moderator", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyMessage_WithBlankMessageIdOrPodId_ReturnsBadRequest()
    {
        var verifier = new Mock<IPodMembershipVerifier>();
        var controller = new PodVerificationController(
            NullLogger<PodVerificationController>.Instance,
            verifier.Object);

        var result = await controller.VerifyMessage(
            new PodMessage { MessageId = "   ", PodId = " pod-1 " },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        verifier.Verify(
            service => service.VerifyMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyMessage_EscapesRequestIdAndExceptionInLogs()
    {
        var verifier = new Mock<IPodMembershipVerifier>();
        verifier
            .Setup(service => service.VerifyMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("verification failed\r\n[forged]"));
        var logger = new CapturingLogger<PodVerificationController>();
        var controller = new PodVerificationController(logger, verifier.Object);

        var result = await controller.VerifyMessage(
            new PodMessage
            {
                MessageId = "message\r\n[forged]",
                PodId = "pod-1",
                ChannelId = "channel-1",
                SenderPeerId = "peer-1",
                Signature = "signature",
            },
            CancellationToken.None);

        Assert.Equal(500, Assert.IsType<ObjectResult>(result).StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
        Assert.Contains("message\\r\\n[forged]", entry.Message);
        Assert.Contains("verification failed\\r\\n[forged]", entry.Message);
        Assert.Null(entry.Exception);
    }
}
