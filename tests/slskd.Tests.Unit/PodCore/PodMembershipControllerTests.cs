// <copyright file="PodMembershipControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Core.Security;
using slskd.PodCore;
using slskd.PodCore.API.Controllers;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class PodMembershipControllerTests
{
    [Fact]
    public async Task PublishMembership_ForPrivatePodOutsider_ReturnsForbiddenWithoutPublishing()
    {
        var membershipService = new Mock<IPodMembershipService>();
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.GetPodAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pod { PodId = "pod-1", IsPublic = false });
        podService.Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PodMember>());
        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            podService.Object), "mallory");
        controller.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "mallory") },
                "test"));

        var result = await controller.PublishMembership(
            "pod-1",
            new PodMember { PeerId = "mallory", Role = "owner" },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        membershipService.Verify(service => service.PublishMembershipAsync(
            It.IsAny<string>(),
            It.IsAny<PodMember>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Controller_RequiresAuthenticatedAccess()
    {
        var authorize = typeof(PodMembershipController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AuthPolicy.Any, authorize.Policy);
    }

    [Fact]
    public async Task ChangeRole_TrimsPodPeerAndRoleBeforeDispatch()
    {
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .Setup(service => service.ChangeRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipPublishResult(true, "pod-1", "peer-1", "dht:key", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            CreatePodService()));

        var result = await controller.ChangeRole(" pod-1 ", " peer-1 ", new ChangeRoleRequest(" moderator "), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        membershipService.Verify(
            service => service.ChangeRoleAsync("pod-1", "peer-1", "moderator", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMembership_TrimsMemberPeerIdBeforeDispatch()
    {
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .Setup(service => service.PublishMembershipAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipPublishResult(true, "pod-1", "peer-1", "dht:key", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            CreatePodService()), "peer-1");

        var result = await controller.PublishMembership(
            " pod-1 ",
            new PodMember { PeerId = " peer-1 ", Role = " member " },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        membershipService.Verify(
            service => service.PublishMembershipAsync(
                "pod-1",
                It.Is<PodMember>(member => member.PeerId == "peer-1" && member.Role == "owner"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMembership_TrimsMemberFieldsBeforeDispatch()
    {
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .Setup(service => service.UpdateMembershipAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipPublishResult(true, "pod-1", "peer-1", "dht:key", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            CreatePodService()));

        var result = await controller.UpdateMembership(
            " pod-1 ",
            " peer-1 ",
            new PodMember { Role = " member ", PublicKey = " pub " },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        membershipService.Verify(
            service => service.UpdateMembershipAsync(
                "pod-1",
                It.Is<PodMember>(member =>
                    member.PeerId == "peer-1" &&
                    member.Role == "member" &&
                    member.PublicKey == "pub"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMembership_WhenServiceReturnsFailure_DoesNotLeakErrorMessage()
    {
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .Setup(service => service.PublishMembershipAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipPublishResult(false, "pod-1", "peer-1", string.Empty, DateTimeOffset.MinValue, DateTimeOffset.MinValue, "sensitive detail"));

        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            CreatePodService()), "peer-1");

        var result = await controller.PublishMembership(
            "pod-1",
            new PodMember { PeerId = "peer-1", Role = "member" },
            CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
        Assert.Contains("Failed to publish membership", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetMembership_WhenServiceReturnsNotFound_DoesNotLeakErrorMessage()
    {
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .Setup(service => service.GetMembershipAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipRetrievalResult(false, "pod-1", "peer-1", null, DateTimeOffset.MinValue, DateTimeOffset.MinValue, false, "sensitive detail"));

        var controller = PodControllerTestContext.AsAdministrator(new PodMembershipController(
            NullLogger<PodMembershipController>.Instance,
            membershipService.Object,
            CreatePodService()));

        var result = await controller.GetMembership("pod-1", "peer-1", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.DoesNotContain("sensitive detail", notFound.Value?.ToString() ?? string.Empty);
        Assert.Contains("Membership not found", notFound.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("pod-1", notFound.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("peer-1", notFound.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("update")]
    [InlineData("remove")]
    [InlineData("get")]
    [InlineData("verify")]
    [InlineData("ban")]
    [InlineData("unban")]
    [InlineData("change-role")]
    [InlineData("stats")]
    [InlineData("cleanup")]
    public async Task MembershipActions_WhenRequestIsCanceled_PropagateCancellation(string actionName)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var membershipService = CreateCanceledMembershipService(cancellation.Token);
        var controller = PodControllerTestContext.AsAdministrator(
            new PodMembershipController(
                NullLogger<PodMembershipController>.Instance,
                membershipService.Object,
                CreatePodService()),
            "peer-1");

        Func<Task<IActionResult>> action = actionName switch
        {
            "publish" => () => controller.PublishMembership(
                "pod-1", new PodMember { PeerId = "peer-1", Role = "member" }, cancellation.Token),
            "update" => () => controller.UpdateMembership(
                "pod-1", "peer-1", new PodMember { Role = "member" }, cancellation.Token),
            "remove" => () => controller.RemoveMembership("pod-1", "peer-1", cancellation.Token),
            "get" => () => controller.GetMembership("pod-1", "peer-1", cancellation.Token),
            "verify" => () => controller.VerifyMembership("pod-1", "peer-1", cancellation.Token),
            "ban" => () => controller.BanMember("pod-1", "peer-1", new BanRequest("test"), cancellation.Token),
            "unban" => () => controller.UnbanMember("pod-1", "peer-1", cancellation.Token),
            "change-role" => () => controller.ChangeRole("pod-1", "peer-1", new ChangeRoleRequest("moderator"), cancellation.Token),
            "stats" => () => controller.GetMembershipStats(cancellation.Token),
            "cleanup" => () => controller.CleanupExpiredMemberships(cancellation.Token),
            _ => throw new InvalidOperationException($"Unknown action {actionName}"),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
    }

    [Fact]
    public async Task MembershipDiagnostics_EscapeRemoteValuesAndExceptionText()
    {
        const string podId = "pod-1\r\nforged pod";
        const string peerId = "peer-1\r\nforged peer";
        var membershipService = new Mock<IPodMembershipService>();
        membershipService
            .SetupSequence(service => service.RemoveMembershipAsync(podId, peerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipPublishResult(
                false,
                "result-pod\r\nforged pod",
                "result-peer\r\nforged peer",
                string.Empty,
                DateTimeOffset.MinValue,
                DateTimeOffset.MinValue,
                "failure\r\nforged result"))
            .ThrowsAsync(new InvalidOperationException("exception\r\nforged detail"));
        var logger = new CapturingLogger<PodMembershipController>();
        var controller = PodControllerTestContext.AsAdministrator(
            new PodMembershipController(logger, membershipService.Object, CreatePodService()),
            "admin");

        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.RemoveMembership(podId, peerId)).StatusCode);
        Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.RemoveMembership(podId, peerId)).StatusCode);

        Assert.Equal(2, logger.Entries.Count);
        Assert.Contains("peer-1\\r\\nforged peer", logger.Entries[0].Message);
        Assert.Contains("pod-1\\r\\nforged pod", logger.Entries[0].Message);
        Assert.Contains("failure\\r\\nforged result", logger.Entries[0].Message);
        Assert.Contains("peer-1\\r\\nforged peer", logger.Entries[1].Message);
        Assert.Contains("pod-1\\r\\nforged pod", logger.Entries[1].Message);
        Assert.Contains("exception\\r\\nforged detail", logger.Entries[1].Message);
        foreach (var entry in logger.Entries)
        {
            Assert.DoesNotContain('\r', entry.Message);
            Assert.DoesNotContain('\n', entry.Message);
            Assert.Null(entry.Exception);
        }

        membershipService.Verify(service => service.RemoveMembershipAsync(podId, peerId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static Mock<IPodMembershipService> CreateCanceledMembershipService(CancellationToken cancellationToken)
    {
        var service = new Mock<IPodMembershipService>();
        service.Setup(instance => instance.PublishMembershipAsync(It.IsAny<string>(), It.IsAny<PodMember>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.UpdateMembershipAsync(It.IsAny<string>(), It.IsAny<PodMember>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.RemoveMembershipAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.GetMembershipAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipRetrievalResult>(cancellationToken));
        service.Setup(instance => instance.VerifyMembershipAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipVerificationResult>(cancellationToken));
        service.Setup(instance => instance.BanMemberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.UnbanMemberAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.ChangeRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
            .Returns(Task.FromCanceled<MembershipPublishResult>(cancellationToken));
        service.Setup(instance => instance.GetStatsAsync(cancellationToken))
            .Returns(Task.FromCanceled<MembershipStats>(cancellationToken));
        service.Setup(instance => instance.CleanupExpiredAsync(cancellationToken))
            .Returns(Task.FromCanceled<MembershipCleanupResult>(cancellationToken));
        return service;
    }

    private static IPodService CreatePodService()
    {
        var service = new Mock<IPodService>();
        service.Setup(instance => instance.GetPodAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pod { PodId = "pod-1", IsPublic = true });
        service.Setup(instance => instance.GetMembersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "peer-1", Role = "owner" } });
        return service.Object;
    }
}
