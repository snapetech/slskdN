// <copyright file="PeerStreamServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Soulseek;
using slskd.Streaming;
using Xunit;

public class PeerStreamServiceTests
{
    [Fact]
    public async Task OpenAsync_ValidTicket_StreamsSoulseekBytesAndReleasesLimiter()
    {
        var payload = Encoding.UTF8.GetBytes("peer-preview-bytes");
        var tickets = new Mock<IPeerStreamTicketService>();
        var limiter = new Mock<IStreamSessionLimiter>();
        var client = new Mock<ISoulseekClient>();

        tickets.Setup(x => x.Validate("ticket-1"))
            .Returns(new PeerStreamTicket("ticket-1", "remote-user", @"Music\track.flac", payload.Length, "user:alice", DateTimeOffset.UtcNow.AddMinutes(1), "audio/flac"));
        limiter.Setup(x => x.TryAcquire("user:alice", 1)).Returns(true);
        client.Setup(x => x.DownloadAsync(
                "remote-user",
                @"Music\track.flac",
                It.IsAny<Func<Task<Stream>>>(),
                payload.Length,
                0,
                null,
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string _,
                string _,
                Func<Task<Stream>> outputStreamFactory,
                long? _,
                long _,
                int? _,
                TransferOptions _,
                CancellationToken? cancellationToken) =>
            {
                var output = await outputStreamFactory();
                await output.WriteAsync(payload, cancellationToken ?? CancellationToken.None);
                await output.FlushAsync(cancellationToken ?? CancellationToken.None);
                return null!;
            });

        var service = new PeerStreamService(tickets.Object, limiter.Object, client.Object, Mock.Of<ILogger<PeerStreamService>>());

        var lease = await service.OpenAsync("ticket-1", CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal("audio/flac", lease.ContentType);
        await using (var stream = lease.Stream)
        {
            var actual = await ReadAllAsync(stream);
            Assert.Equal(payload, actual);
        }

        limiter.Verify(x => x.Release("user:alice"), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_LimiterRejects_DoesNotStartSoulseekDownload()
    {
        var tickets = new Mock<IPeerStreamTicketService>();
        var limiter = new Mock<IStreamSessionLimiter>();
        var client = new Mock<ISoulseekClient>();

        tickets.Setup(x => x.Validate("ticket-1"))
            .Returns(new PeerStreamTicket("ticket-1", "remote-user", "track.mp3", 10, "user:alice", DateTimeOffset.UtcNow.AddMinutes(1), "audio/mpeg"));
        limiter.Setup(x => x.TryAcquire("user:alice", 1)).Returns(false);

        var service = new PeerStreamService(tickets.Object, limiter.Object, client.Object, Mock.Of<ILogger<PeerStreamService>>());

        await Assert.ThrowsAsync<PeerStreamLimitException>(() => service.OpenAsync("ticket-1", CancellationToken.None));
        client.Verify(x => x.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Func<Task<Stream>>>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<int?>(),
            It.IsAny<TransferOptions>(),
            It.IsAny<CancellationToken?>()), Times.Never);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var output = new MemoryStream();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await stream.CopyToAsync(output, timeout.Token);
        return output.ToArray();
    }
}
