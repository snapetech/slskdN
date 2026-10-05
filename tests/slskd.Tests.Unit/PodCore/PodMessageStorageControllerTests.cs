// <copyright file="PodMessageStorageControllerTests.cs" company="slskdN Team">
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
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class PodMessageStorageControllerTests
{
    [Fact]
    public async Task SearchMessages_EscapesSearchTextAndExceptionOnlyInLogs()
    {
        const string query = "artist\r\nforged query";
        string? capturedQuery = null;
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.SearchMessagesAsync(
                "pod-1",
                It.IsAny<string>(),
                "general",
                50,
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, int, CancellationToken>((_, value, _, _, _) => capturedQuery = value)
            .ThrowsAsync(new InvalidOperationException($"Search failed for '{query}'\r\nforged exception line"));
        var logger = new CapturingLogger<PodMessageStorageController>();
        var controller = PodControllerTestContext.AsAdministrator(new PodMessageStorageController(
            storage.Object,
            Mock.Of<IPodService>(),
            logger));

        var result = await controller.SearchMessages("pod-1", query, "general", cancellationToken: CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, response.StatusCode);
        Assert.Equal(query, capturedQuery);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains("\\r\\nforged exception line", entry.Message);
        Assert.DoesNotContain(query, entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task SearchMessages_WhenCallerIsNotMember_ReturnsForbiddenWithoutSearching()
    {
        var storage = new Mock<IPodMessageStorage>();
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "alice", Role = "member" } });
        var controller = new PodMessageStorageController(
            storage.Object,
            podService.Object,
            NullLogger<PodMessageStorageController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Name, "mallory") },
                        "test")),
                },
            },
        };

        var result = await controller.SearchMessages("pod-1", "private", cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        storage.Verify(service => service.SearchMessagesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchMessages_TrimsPodQueryAndChannelBeforeDispatch()
    {
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.SearchMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PodMessage>());

        var controller = PodControllerTestContext.AsAdministrator(new PodMessageStorageController(
            storage.Object,
            Mock.Of<IPodService>(),
            NullLogger<PodMessageStorageController>.Instance));

        var result = await controller.SearchMessages(
            " pod-1 ",
            "  hello world  ",
            " general ",
            25,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        storage.Verify(
            service => service.SearchMessagesAsync("pod-1", "hello world", "general", 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupChannelMessages_WithWhitespaceOnlyChannelId_ReturnsBadRequest()
    {
        var storage = new Mock<IPodMessageStorage>();
        var controller = PodControllerTestContext.AsAdministrator(new PodMessageStorageController(
            storage.Object,
            Mock.Of<IPodService>(),
            NullLogger<PodMessageStorageController>.Instance));

        var result = await controller.CleanupChannelMessages(" pod-1 ", "   ", 1000, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        storage.Verify(
            service => service.DeleteMessagesInChannelOlderThanAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchMessages_WithOutOfRangeLimit_ReturnsBadRequest()
    {
        var storage = new Mock<IPodMessageStorage>();
        var controller = PodControllerTestContext.AsAdministrator(new PodMessageStorageController(
            storage.Object,
            Mock.Of<IPodService>(),
            NullLogger<PodMessageStorageController>.Instance));

        var result = await controller.SearchMessages(
            "pod-1",
            "hello",
            null,
            501,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        storage.Verify(
            service => service.SearchMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
