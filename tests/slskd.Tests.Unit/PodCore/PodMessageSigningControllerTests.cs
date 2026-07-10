// <copyright file="PodMessageSigningControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.PodCore;
using slskd.PodCore.API.Controllers;
using Xunit;

public class PodMessageSigningControllerTests
{
    [Fact]
    public async Task SignMessage_WhenSenderDoesNotMatchAuthenticatedMember_ReturnsForbiddenWithoutSigning()
    {
        var signer = new Mock<IMessageSigner>();
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "alice", Role = "member" } });
        var controller = new PodMessageSigningController(
            NullLogger<PodMessageSigningController>.Instance,
            signer.Object,
            podService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Name, "alice") },
                        "test")),
                },
            },
        };

        var result = await controller.SignMessage(
            new MessageSigningRequest(
                new PodMessage { MessageId = "message-1", PodId = "pod-1", SenderPeerId = "mallory" },
                "private-key"),
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        signer.Verify(service => service.SignMessageAsync(
            It.IsAny<PodMessage>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignMessage_TrimsPrivateKeyAndMessageFieldsBeforeDispatch()
    {
        var signer = new Mock<IMessageSigner>();
        signer
            .Setup(service => service.SignMessageAsync(It.IsAny<PodMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PodMessage { MessageId = "msg-1" });

        var controller = PodControllerTestContext.AsAdministrator(new PodMessageSigningController(
            NullLogger<PodMessageSigningController>.Instance,
            signer.Object,
            Mock.Of<IPodService>()), "peer-1");

        var result = await controller.SignMessage(
            new MessageSigningRequest(
                new PodMessage
                {
                    MessageId = " msg-1 ",
                    PodId = " pod-1 ",
                    ChannelId = " general ",
                    SenderPeerId = " peer-1 ",
                    Body = " hello ",
                    Signature = " sig ",
                },
                " secret "),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        signer.Verify(
            service => service.SignMessageAsync(
                It.Is<PodMessage>(message =>
                    message.MessageId == "msg-1" &&
                    message.PodId == "pod-1" &&
                    message.ChannelId == "general" &&
                    message.SenderPeerId == "peer-1" &&
                    message.Body == "hello" &&
                    message.Signature == "sig"),
                "secret",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyMessage_WithWhitespaceOnlyMessageId_ReturnsBadRequest()
    {
        var signer = new Mock<IMessageSigner>();
        var controller = PodControllerTestContext.AsAdministrator(new PodMessageSigningController(
            NullLogger<PodMessageSigningController>.Instance,
            signer.Object,
            Mock.Of<IPodService>()));

        var result = await controller.VerifyMessage(
            new PodMessage { MessageId = "   " },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        signer.Verify(service => service.VerifyMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyMessage_ReturnsSanitizedSuccessPayload()
    {
        var signer = new Mock<IMessageSigner>();
        signer
            .Setup(service => service.VerifyMessageAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = PodControllerTestContext.AsAdministrator(new PodMessageSigningController(
            NullLogger<PodMessageSigningController>.Instance,
            signer.Object,
            Mock.Of<IPodService>()));

        var result = await controller.VerifyMessage(
            new PodMessage { MessageId = "msg-1", PodId = "pod-1" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("isValid", ok.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("msg-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
