// <copyright file="UsersCompatibilityControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.API.Compatibility;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.API.Compatibility;
using slskd.Common.Security;
using Soulseek;
using Xunit;

public class UsersCompatibilityControllerTests
{
    [Fact]
    public async Task BrowseUser_WhenBrowseThrows_DoesNotLeakExceptionMessage()
    {
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .Setup(client => client.BrowseAsync(It.IsAny<string>(), It.IsAny<BrowseOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = new UsersCompatibilityController(
            NullLogger<UsersCompatibilityController>.Instance,
            soulseekClient.Object,
            CreatePermissiveLimiter());

        var result = await controller.BrowseUser(" alice ", CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("alice", error.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Failed to browse user", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task BrowseUser_WhenSafetyLimiterRejects_DoesNotBrowseNetwork()
    {
        var soulseekClient = new Mock<ISoulseekClient>(MockBehavior.Strict);
        var safetyLimiter = new Mock<ISoulseekSafetyLimiter>();
        safetyLimiter
            .Setup(limiter => limiter.TryConsumeBrowse("compatibility"))
            .Returns(false);

        var controller = new UsersCompatibilityController(
            NullLogger<UsersCompatibilityController>.Instance,
            soulseekClient.Object,
            safetyLimiter.Object);

        var result = await controller.BrowseUser("alice", CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, error.StatusCode);
        soulseekClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BrowseUser_WhenRequestIsCanceled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var soulseekClient = new Mock<ISoulseekClient>(MockBehavior.Strict);
        soulseekClient
            .Setup(client => client.BrowseAsync("alice", It.IsAny<BrowseOptions>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var controller = new UsersCompatibilityController(
            NullLogger<UsersCompatibilityController>.Instance,
            soulseekClient.Object,
            CreatePermissiveLimiter());

        await Assert.ThrowsAsync<OperationCanceledException>(() => controller.BrowseUser("alice", cts.Token));
    }

    private static ISoulseekSafetyLimiter CreatePermissiveLimiter()
    {
        var safetyLimiter = new Mock<ISoulseekSafetyLimiter>();
        safetyLimiter
            .Setup(limiter => limiter.TryConsumeBrowse(It.IsAny<string>()))
            .Returns(true);

        return safetyLimiter.Object;
    }
}
