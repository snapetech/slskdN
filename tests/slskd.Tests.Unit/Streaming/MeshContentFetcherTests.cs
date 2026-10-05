// <copyright file="MeshContentFetcherTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh.ServiceFabric;
using slskd.Streaming;
using Xunit;

public class MeshContentFetcherTests
{
    [Theory]
    [InlineData(1000, null, true)]
    [InlineData(0, null, true)]
    [InlineData(0, 2048L, false)]
    [InlineData(1000, 2048L, false)]
    [InlineData(2049, null, false)]
    public async Task FetchAsync_RangeResponse_EnforcesKnownSizeAndRangeLimit(int size, long? expectedSize, bool valid)
    {
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply
            {
                StatusCode = ServiceStatusCodes.OK,
                Payload = new byte[size],
            });
        var fetcher = new MeshContentFetcher(meshClient.Object, Mock.Of<ILogger<MeshContentFetcher>>());

        var result = await fetcher.FetchAsync("peer-1", "content:audio:track:test", expectedSize: expectedSize, offset: 2048, length: 2048);

        using (result.Data)
        {
            Assert.Equal(valid, result.SizeValid);
            if (valid)
            {
                Assert.Null(result.Error);
                Assert.Equal(size, result.Size);
                Assert.NotNull(result.Data);
            }
            else if (size == 0 || size > 2048)
            {
                Assert.NotNull(result.Error);
                Assert.Null(result.Data);
            }
        }
    }

    [Fact]
    public async Task FetchAsync_WhenMeshServiceReplyFails_ReturnsSanitizedError()
    {
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply
            {
                StatusCode = ServiceStatusCodes.UnknownError,
                ErrorMessage = "sensitive remote detail",
                Payload = Array.Empty<byte>()
            });

        var fetcher = new MeshContentFetcher(
            meshClient.Object,
            Mock.Of<ILogger<MeshContentFetcher>>());

        var result = await fetcher.FetchAsync("peer-1", "content:mb:recording:test", cancellationToken: CancellationToken.None);

        Assert.Equal("Mesh content fetch failed", result.Error);
        Assert.DoesNotContain("sensitive", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.SizeValid);
        Assert.False(result.HashValid);
    }

    [Fact]
    public async Task FetchAsync_WhenMeshClientThrows_ReturnsSanitizedError()
    {
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive fetch detail"));

        var fetcher = new MeshContentFetcher(
            meshClient.Object,
            Mock.Of<ILogger<MeshContentFetcher>>());

        var result = await fetcher.FetchAsync("peer-1", "content:mb:recording:test", cancellationToken: CancellationToken.None);

        Assert.Equal("Mesh content fetch failed", result.Error);
        Assert.DoesNotContain("sensitive", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.SizeValid);
        Assert.False(result.HashValid);
    }

    [Fact]
    public async Task FetchAsync_EscapesRemoteIdentifiersAndExceptionTextInDiagnostics()
    {
        var peerId = "peer\r\nforged";
        var contentId = "content:mb:recording:test\r\nforged";
        var logger = new Mock<ILogger<MeshContentFetcher>>();
        logger.Setup(entry => entry.IsEnabled(LogLevel.Debug)).Returns(true);
        logger.Setup(entry => entry.IsEnabled(LogLevel.Error)).Returns(true);
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync(peerId, It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("remote\r\nfailure"));
        var fetcher = new MeshContentFetcher(meshClient.Object, logger.Object);

        var result = await fetcher.FetchAsync(peerId, contentId, cancellationToken: CancellationToken.None);

        Assert.Equal("Mesh content fetch failed", result.Error);
        meshClient.Verify(client => client.CallAsync(peerId, It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()), Times.Once);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("peer\\r\\nforged") &&
                    state.ToString()!.Contains("test\\r\\nforged") &&
                    state.ToString()!.Contains("remote\\r\\nfailure") &&
                    !state.ToString()!.Contains(peerId) &&
                    !state.ToString()!.Contains("remote\r\nfailure")),
                It.IsAny<System.Exception?>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }
}
