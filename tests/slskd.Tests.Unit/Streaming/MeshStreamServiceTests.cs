// <copyright file="MeshStreamServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.Shares;
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
            await Assert.ThrowsAsync<MeshStreamException>(() => ReadAllAsync(stream));
        }

        limiter.Verify(x => x.Release("user:alice"), Times.Once);
    }

    [Theory]
    [InlineData(3000)]
    [InlineData(4096)]
    public async Task OpenAsync_UnknownLength_PreservesTailAndAcceptsEndOfFile(int size)
    {
        var payload = Enumerable.Range(0, size).Select(index => (byte)(index % 251)).ToArray();
        var tickets = new Mock<IMeshStreamTicketService>();
        tickets.Setup(instance => instance.Validate("tail-ticket"))
            .Returns(new MeshStreamTicket("tail-ticket", "tail-content", "track.wav", "host-peer", null, null, "user:test", DateTimeOffset.UtcNow.AddMinutes(1), "audio/wav"));
        var limiter = new Mock<IStreamSessionLimiter>();
        limiter.Setup(instance => instance.TryAcquire("user:test", 1)).Returns(true);
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(instance => instance.FetchAsync("host-peer", "tail-content", It.IsAny<long?>(), null, It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string peer, string content, long? expected, string? hash, long offset, int length, CancellationToken token) =>
            {
                var bytes = payload.Skip((int)offset).Take(length).ToArray();
                return Task.FromResult(new MeshContentFetchResult
                {
                    Data = new MemoryStream(bytes),
                    Size = bytes.Length,
                    SizeValid = !expected.HasValue || bytes.Length == expected.Value,
                });
            });
        var service = new MeshStreamService(tickets.Object, limiter.Object, Mock.Of<IMeshDirectory>(), fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());
        var lease = await service.OpenAsync("tail-ticket", CancellationToken.None);
        Assert.NotNull(lease);
        await using (var stream = lease.Stream)
        {
            Assert.Equal(payload, await ReadAllAsync(stream));
        }
        limiter.Verify(instance => instance.Release("user:test"), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenAsync_ProducerFailure_ThrowsToReaderAndReleasesLimiter(bool peerAvailable)
    {
        var tickets = new Mock<IMeshStreamTicketService>();
        tickets.Setup(instance => instance.Validate("failed-ticket"))
            .Returns(new MeshStreamTicket("failed-ticket", "failed-content", "track.wav", null, null, null, "user:test", DateTimeOffset.UtcNow.AddMinutes(1), "audio/wav"));
        var limiter = new Mock<IStreamSessionLimiter>();
        limiter.Setup(instance => instance.TryAcquire("user:test", 1)).Returns(true);
        var directory = new Mock<IMeshDirectory>();
        directory.Setup(instance => instance.FindPeersByContentAsync("failed-content", It.IsAny<CancellationToken>()))
            .ReturnsAsync(peerAvailable ? new[] { new MeshPeerDescriptor("host-peer") } : Array.Empty<MeshPeerDescriptor>());
        var fetcher = new Mock<IMeshContentFetcher>();
        fetcher.Setup(instance => instance.FetchAsync("host-peer", "failed-content", It.IsAny<long?>(), null, It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Peer disconnected"));
        var service = new MeshStreamService(tickets.Object, limiter.Object, directory.Object, fetcher.Object, Mock.Of<ILogger<MeshStreamService>>());

        var lease = await service.OpenAsync("failed-ticket", CancellationToken.None);
        Assert.NotNull(lease);
        await using (var stream = lease.Stream)
        {
            var error = await Record.ExceptionAsync(() => ReadAllAsync(stream));
            if (peerAvailable)
            {
                Assert.IsType<IOException>(error);
            }
            else
            {
                Assert.IsType<MeshStreamException>(error);
            }
        }

        limiter.Verify(instance => instance.Release("user:test"), Times.Once);
    }

    [Theory]
    [InlineData(3000)]
    [InlineData(4096)]
    public async Task OpenAsync_RealHostAndFetcher_PreserveUnknownLengthBytes(int size)
    {
        var payload = Enumerable.Range(0, size).Select(index => (byte)(index % 251)).ToArray();
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, payload);
            var repository = new Mock<IShareRepository>();
            repository.Setup(instance => instance.FindContentItem("content:audio:track:test"))
                .Returns((Domain: "audio", WorkId: "work-1", MaskedFilename: "track.wav", IsAdvertisable: true, ModerationReason: string.Empty, CheckedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            repository.Setup(instance => instance.FindFileInfo("track.wav"))
                .Returns((Filename: tempFile, Size: (long)size));
            var shares = new Mock<IShareService>();
            shares.Setup(instance => instance.GetLocalRepository()).Returns(repository.Object);
            shares.Setup(instance => instance.ResolveFileAsync("track.wav"))
                .ReturnsAsync((Program.LocalHostName, tempFile, (long)size));
            var host = new MeshContentMeshService(Mock.Of<ILogger<MeshContentMeshService>>(), shares.Object);
            var client = new Mock<IMeshServiceClient>();
            client.Setup(instance => instance.CallAsync("host-peer", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
                .Returns((string peer, ServiceCall call, CancellationToken token) =>
                    host.HandleCallAsync(call, new MeshServiceContext { RemotePeerId = "listener-peer" }, token));
            var tickets = new Mock<IMeshStreamTicketService>();
            tickets.Setup(instance => instance.Validate("host-ticket"))
                .Returns(new MeshStreamTicket("host-ticket", "content:audio:track:test", "track.wav", "host-peer", null, null, "user:test", DateTimeOffset.UtcNow.AddMinutes(1), "audio/wav"));
            var limiter = new Mock<IStreamSessionLimiter>();
            limiter.Setup(instance => instance.TryAcquire("user:test", 1)).Returns(true);
            var service = new MeshStreamService(
                tickets.Object,
                limiter.Object,
                Mock.Of<IMeshDirectory>(),
                new MeshContentFetcher(client.Object, Mock.Of<ILogger<MeshContentFetcher>>()),
                Mock.Of<ILogger<MeshStreamService>>());

            var lease = await service.OpenAsync("host-ticket", CancellationToken.None);
            Assert.NotNull(lease);
            await using (var stream = lease.Stream)
            {
                Assert.Equal(payload, await ReadAllAsync(stream));
            }

            client.Verify(instance => instance.CallAsync("host-peer", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()), Times.Exactly(size / 2048 + 1));
            limiter.Verify(instance => instance.Release("user:test"), Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var output = new MemoryStream();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await stream.CopyToAsync(output, timeout.Token);
        return output.ToArray();
    }
}
