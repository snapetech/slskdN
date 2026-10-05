// <copyright file="PeerVerificationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous.Security;

using Microsoft.Extensions.Logging;
using Moq;
using slskd.DhtRendezvous.Security;
using slskd.Tests.Unit.TestHelpers;
using Soulseek;
using Xunit;

public class PeerVerificationServiceTests
{
    [Fact]
    public async Task VerifyPeerAsync_PropagatesCallerCancellation()
    {
        var client = new Mock<ISoulseekClient>();
        client
            .Setup(c => c.GetUserInfoAsync("alice", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = new PeerVerificationService(
            Mock.Of<ILogger<PeerVerificationService>>(),
            client.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.VerifyPeerAsync("alice", "challenge", cancellation.Token));
    }

    [Fact]
    public async Task VerifyPeerAsync_WhenSoulseekThrows_ReturnsSanitizedFailure()
    {
        var client = new Mock<ISoulseekClient>();
        client
            .Setup(c => c.GetUserInfoAsync("alice", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("remote detail\r\nforged log"));
        var logger = new CapturingLogger<PeerVerificationService>();

        var service = new PeerVerificationService(
            logger,
            client.Object);

        var result = await service.VerifyPeerAsync("alice", "challenge", CancellationToken.None);

        Assert.False(result.IsVerified);
        Assert.False(result.IsPartial);
        Assert.Equal("Verification failed", result.FailureReason);
        Assert.DoesNotContain("remote detail", result.FailureReason);

        var entry = Assert.Single(logger.Entries);
        Assert.Null(entry.Exception);
        Assert.Contains("remote detail\\r\\nforged log", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
    }
}
