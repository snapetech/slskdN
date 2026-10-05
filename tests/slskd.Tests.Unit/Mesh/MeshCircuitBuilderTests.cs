// <copyright file="MeshCircuitBuilderTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Common.Security;
using slskd.Mesh;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class MeshCircuitBuilderTests : IDisposable
{
    private readonly Mock<ILogger<MeshCircuitBuilder>> _loggerMock;
    private readonly Mock<IMeshPeerManager> _peerManagerMock;
    private readonly Mock<IAnonymityTransportSelector> _transportSelectorMock;
    private readonly MeshOptions _defaultOptions;

    public MeshCircuitBuilderTests()
    {
        _loggerMock = new Mock<ILogger<MeshCircuitBuilder>>();
        _peerManagerMock = new Mock<IMeshPeerManager>();
        _transportSelectorMock = new Mock<IAnonymityTransportSelector>();

        _defaultOptions = new MeshOptions
        {
            SelfPeerId = "self-peer"
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert - Should not throw
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);
        Assert.NotNull(builder);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MeshCircuitBuilder(null!, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MeshCircuitBuilder(_defaultOptions, null!, _peerManagerMock.Object, _transportSelectorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullPeerManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, null!, _transportSelectorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullTransportSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, null!));
    }

    [Fact]
    public async Task BuildCircuitAsync_WithNullTargetPeerId_ThrowsArgumentException()
    {
        // Arrange
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildCircuitAsync(null!));
        Assert.Contains("Target peer ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task BuildCircuitAsync_WithEmptyTargetPeerId_ThrowsArgumentException()
    {
        // Arrange
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildCircuitAsync(string.Empty));
        Assert.Contains("Target peer ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public async Task BuildCircuitAsync_WithInvalidCircuitLength_ThrowsArgumentException(int circuitLength)
    {
        // Arrange
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            builder.BuildCircuitAsync("target-peer", circuitLength));
        Assert.Contains("Circuit length must be between 2 and 6 hops", exception.Message);
    }

    [Fact]
    public async Task BuildCircuitAsync_WhenHopConnectionIsCanceled_PropagatesCancellationAndDisposesEstablishedHops()
    {
        using var cancellation = new CancellationTokenSource();
        var firstStream = new MemoryStream();
        var transport = Mock.Of<IAnonymityTransport>();
        var connectionAttempts = 0;
        _peerManagerMock
            .Setup(manager => manager.GetAvailablePeersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCircuitPeers());
        _transportSelectorMock
            .Setup(selector => selector.SelectAndConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, int _, string? _, CancellationToken token) =>
            {
                if (Interlocked.Increment(ref connectionAttempts) == 1)
                {
                    return Task.FromResult<(IAnonymityTransport Transport, Stream Stream)>((transport, firstStream));
                }

                cancellation.Cancel();
                return Task.FromCanceled<(IAnonymityTransport Transport, Stream Stream)>(token);
            });

        using var builder = new MeshCircuitBuilder(
            _defaultOptions,
            _loggerMock.Object,
            _peerManagerMock.Object,
            _transportSelectorMock.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            builder.BuildCircuitAsync("target-peer", circuitLength: 2, cancellation.Token));

        Assert.False(firstStream.CanRead);
    }

    [Fact]
    public async Task BuildCircuitAsync_WhenHopConnectionFails_DisposesEstablishedHops()
    {
        var firstStream = new MemoryStream();
        var transport = Mock.Of<IAnonymityTransport>();
        var connectionAttempts = 0;
        _peerManagerMock
            .Setup(manager => manager.GetAvailablePeersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCircuitPeers());
        _transportSelectorMock
            .Setup(selector => selector.SelectAndConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, int _, string? _, CancellationToken _) =>
            {
                if (Interlocked.Increment(ref connectionAttempts) == 1)
                {
                    return Task.FromResult<(IAnonymityTransport Transport, Stream Stream)>((transport, firstStream));
                }

                return Task.FromException<(IAnonymityTransport Transport, Stream Stream)>(new IOException("Peer unavailable"));
            });

        using var builder = new MeshCircuitBuilder(
            _defaultOptions,
            _loggerMock.Object,
            _peerManagerMock.Object,
            _transportSelectorMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildCircuitAsync("target-peer", circuitLength: 2));

        Assert.False(firstStream.CanRead);
    }

    [Fact]
    public void GetCircuit_WithNonExistentCircuitId_ReturnsNull()
    {
        // Arrange
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);

        // Act
        var circuit = builder.GetCircuit("non-existent-id");

        // Assert
        Assert.Null(circuit);
    }

    [Fact]
    public void GetStatistics_WithNoCircuits_ReturnsEmptyStatistics()
    {
        // Arrange
        using var builder = new MeshCircuitBuilder(_defaultOptions, _loggerMock.Object, _peerManagerMock.Object, _transportSelectorMock.Object);

        // Act
        var stats = builder.GetStatistics();

        // Assert
        Assert.Equal(0, stats.ActiveCircuits);
        Assert.Equal(0, stats.TotalCircuitsBuilt);
        Assert.Equal(0, stats.AverageCircuitLength);
        Assert.Empty(stats.CircuitLengths);
    }

    private static List<MeshPeer> CreateCircuitPeers()
    {
        return new List<MeshPeer>
        {
            new("relay-peer", new List<IPEndPoint> { new(IPAddress.Loopback, 4100) }),
            new("target-peer", new List<IPEndPoint> { new(IPAddress.Loopback, 4200) })
        };
    }
}
