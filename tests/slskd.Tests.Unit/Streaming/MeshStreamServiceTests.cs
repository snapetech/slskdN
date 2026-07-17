// <copyright file="MeshStreamServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using slskd.Streaming;
using slskd.Transfers.MultiSource.Metrics;
using Xunit;

public class MeshStreamServiceTests
{
    [Fact]
    public async Task OpenAsync_ValidTicket_StreamsMeshChunksAccountsTrafficAndReleasesLimiter()
    {
        var payload = Encoding.UTF8.GetBytes("mesh-preview-bytes");
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var tickets = new Mock<IMeshStreamTicketService>();
        var limiter = new Mock<IStreamSessionLimiter>();
        var directory = new Mock<IMeshDirectory>();
        var fetcher = new Mock<IMeshContentFetcher>();
        var fairness = new Mock<IFairnessGuard>();
        var traffic = new Mock<ITrafficAccountingService>();

        tickets.Setup(x => x.Validate("ticket-1"))
            .Returns(new MeshStreamTicket("ticket-1", "content-1", "track.flac", null, payload.Length, hash, "user:alice", DateTimeOffset.UtcNow.AddMinutes(1), "audio/flac"));
        limiter.Setup(x => x.TryAcquire("user:alice", 1)).Returns(true);
        fairness.Setup(x => x.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FairnessDecision { ThrottleOverlayDownloads = false, Reason = "within fairness constraints" });
        directory.Setup(x => x.FindPeersByContentAsync("content-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MeshPeerDescriptor("peer-1") });
        fetcher.Setup(x => x.FetchAsync(
                "peer-1",
                "content-1",
                payload.Length,
                null,
                0,
                payload.Length,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeshContentFetchResult
            {
                Data = new MemoryStream(payload),
                Size = payload.Length,
                SizeValid = true,
            });

        var service = new MeshStreamService(
            tickets.Object,
            limiter.Object,
            directory.Object,
            fetcher.Object,
            Mock.Of<ILogger<MeshStreamService>>(),
            fairness.Object,
            traffic.Object);

        var lease = await service.OpenAsync("ticket-1", CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal("audio/flac", lease.ContentType);
        await using (var stream = lease.Stream)
        {
            var actual = await ReadAllAsync(stream);
            Assert.Equal(payload, actual);
        }

        traffic.Verify(x => x.AddOverlayDownloadAsync(payload.Length, It.IsAny<CancellationToken>()), Times.Once);
        limiter.Verify(x => x.Release("user:alice"), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_FairnessRejects_DoesNotAcquireLimiterOrFetch()
    {
        var tickets = new Mock<IMeshStreamTicketService>();
        var limiter = new Mock<IStreamSessionLimiter>();
        var fetcher = new Mock<IMeshContentFetcher>();
        var fairness = new Mock<IFairnessGuard>();

        tickets.Setup(x => x.Validate("ticket-1"))
            .Returns(new MeshStreamTicket("ticket-1", "content-1", "track.mp3", "peer-1", 10, null, "user:alice", DateTimeOffset.UtcNow.AddMinutes(1), "audio/mpeg"));
        fairness.Setup(x => x.EvaluateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FairnessDecision { ThrottleOverlayDownloads = true, Reason = "ratio too low" });

        var service = new MeshStreamService(
            tickets.Object,
            limiter.Object,
            Mock.Of<IMeshDirectory>(),
            fetcher.Object,
            Mock.Of<ILogger<MeshStreamService>>(),
            fairness.Object);

        await Assert.ThrowsAsync<MeshStreamLimitException>(() => service.OpenAsync("ticket-1", CancellationToken.None));
        limiter.Verify(x => x.TryAcquire(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        fetcher.Verify(x => x.FetchAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<string?>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenAsync_HashMismatch_EmitsNoBytes()
    {
        var payload = Encoding.UTF8.GetBytes("tampered-bytes");
        var tickets = new Mock<IMeshStreamTicketService>();
        var limiter = new Mock<IStreamSessionLimiter>();
        var fetcher = new Mock<IMeshContentFetcher>();

        tickets.Setup(x => x.Validate("ticket-1"))
            .Returns(new MeshStreamTicket("ticket-1", "content-1", "track.flac", "peer-1", payload.Length, "0000", "user:alice", DateTimeOffset.UtcNow.AddMinutes(1), "audio/flac"));
        limiter.Setup(x => x.TryAcquire("user:alice", 1)).Returns(true);
        fetcher.Setup(x => x.FetchAsync(
                "peer-1",
                "content-1",
                payload.Length,
                null,
                0,
                payload.Length,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeshContentFetchResult
            {
                Data = new MemoryStream(payload),
                Size = payload.Length,
                SizeValid = true,
            });

        var service = new MeshStreamService(
            tickets.Object,
            limiter.Object,
            Mock.Of<IMeshDirectory>(),
            fetcher.Object,
            Mock.Of<ILogger<MeshStreamService>>());

        var lease = await service.OpenAsync("ticket-1", CancellationToken.None);

        Assert.NotNull(lease);
        await using (var stream = lease.Stream)
        {
            var actual = await ReadAllAsync(stream);
            Assert.Empty(actual);
        }

        limiter.Verify(x => x.Release("user:alice"), Times.Once);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var output = new MemoryStream();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await stream.CopyToAsync(output, timeout.Token);
        return output.ToArray();
    }
}
