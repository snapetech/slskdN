// <copyright file="SharedMeshTcpListenerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.DhtRendezvous;
using slskd.SoulseekRuntime;
using Xunit;

public class SharedMeshTcpListenerTests
{
    [Fact]
    public void ShouldRun_IsUnconditionalWheneverDhtRendezvousIsEnabled()
    {
        // No separate opt-in: sharing runs whenever DHT rendezvous does, for every installation.
        Assert.True(SharedMeshTcpListener.ShouldRun(new DhtRendezvousOptions { Enabled = true }));
        Assert.False(SharedMeshTcpListener.ShouldRun(new DhtRendezvousOptions { Enabled = false }));
    }

    [Fact]
    public void ShouldRun_Options_AlsoRequiresTheDhtFeatureFlag()
    {
        var defaultOptions = new OptionsAtStartup();
        Assert.True(defaultOptions.Feature.Dht, "Test assumes Feature.Dht defaults to true.");
        Assert.True(defaultOptions.DhtRendezvous.Enabled, "Test assumes DhtRendezvous.Enabled defaults to true.");
        Assert.True(SharedMeshTcpListener.ShouldRun(defaultOptions));

        var withFeatureOff = new OptionsAtStartup { Feature = new Options.FeatureOptions { Dht = false } };
        Assert.False(SharedMeshTcpListener.ShouldRun(withFeatureOff));
    }

    [Theory]
    [InlineData(new byte[] { 0x16, 0x03, 0x01, 0x02, 0x00 }, nameof(SharedMeshTcpListener.ConnectionKind.MeshOverlay))]
    [InlineData(new byte[] { 0x16, 0x01 }, nameof(SharedMeshTcpListener.ConnectionKind.Soulseek))] // 0x16 but wrong "TLS major version" byte
    [InlineData(new byte[] { 0x0b, 0x00, 0x00, 0x00 }, nameof(SharedMeshTcpListener.ConnectionKind.Soulseek))] // plain Soulseek-shaped small LE length prefix
    [InlineData(new byte[] { 0xa4, 0x37, 0x9c, 0x02 }, nameof(SharedMeshTcpListener.ConnectionKind.Soulseek))] // obfuscated-shaped pseudo-random bytes
    public async Task ClassifyConnectionAsync_ClassifiesByFirstTwoBytesWithoutConsumingThem(byte[] sentBytes, string expectedKindName)
    {
        var expected = Enum.Parse<SharedMeshTcpListener.ConnectionKind>(expectedKindName);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var acceptTask = listener.AcceptTcpClientAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        await client.GetStream().WriteAsync(sentBytes);

        using var serverClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(2));

        var kind = await SharedMeshTcpListener.ClassifyConnectionAsync(serverClient.Client, CancellationToken.None);

        Assert.Equal(expected, kind);

        // Peek must not have consumed anything: the same bytes are still readable from the start.
        var readBuffer = new byte[sentBytes.Length];
        var totalRead = 0;
        while (totalRead < readBuffer.Length)
        {
            var read = await serverClient.GetStream().ReadAsync(readBuffer.AsMemory(totalRead));
            totalRead += read;
        }

        Assert.Equal(sentBytes, readBuffer);
    }

    [Fact]
    public async Task ClassifyConnectionAsync_ReturnsUnknown_WhenRemoteClosesWithoutSendingData()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var acceptTask = listener.AcceptTcpClientAsync();

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        }

        using var serverClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(2));

        var kind = await SharedMeshTcpListener.ClassifyConnectionAsync(serverClient.Client, CancellationToken.None);

        Assert.Equal(SharedMeshTcpListener.ConnectionKind.Unknown, kind);
    }

    [Fact]
    public async Task ExecuteAsync_RoutesMeshOverlayAndSoulseekConnectionsToTheirDestinations()
    {
        var dhtOptions = new DhtRendezvousOptions { Enabled = true };
        var optionsAtStartup = new OptionsAtStartup
        {
            Soulseek = new Options.SoulseekOptions
            {
                ListenIpAddress = "127.0.0.1",
                ListenPort = 0, // OS-assigned ephemeral port
            },
        };

        var fedTcpListener = new FedTcpListener();
        var fakeMeshOverlay = new FakeMeshOverlayServer();

        var sut = new SharedMeshTcpListener(
            NullLogger<SharedMeshTcpListener>.Instance,
            optionsAtStartup,
            dhtOptions,
            fedTcpListener,
            fakeMeshOverlay);

        await sut.StartAsync(CancellationToken.None);
        try
        {
            var boundEndPoint = await WaitForBoundEndPointAsync(sut);

            using var meshClient = new TcpClient();
            await meshClient.ConnectAsync(boundEndPoint.Address, boundEndPoint.Port);
            await meshClient.GetStream().WriteAsync(new byte[] { 0x16, 0x03, 0x03, 0x00, 0x10 });

            var handled = await fakeMeshOverlay.Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(handled);

            using var soulseekClient = new TcpClient();
            await soulseekClient.ConnectAsync(boundEndPoint.Address, boundEndPoint.Port);
            var peerInitLength = new byte[] { 0x03, 0x00, 0x00, 0x00 }; // small plausible plain init length
            await soulseekClient.GetStream().WriteAsync(peerInitLength);

            var fed = await fedTcpListener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(fed);
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<IPEndPoint> WaitForBoundEndPointAsync(SharedMeshTcpListener listener)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while (listener.LocalEndPoint is null)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("Timed out waiting for SharedMeshTcpListener to bind.");
            }

            await Task.Delay(10);
        }

        return listener.LocalEndPoint;
    }

    private sealed class FakeMeshOverlayServer : IMeshOverlayServer
    {
        public TaskCompletionSource<TcpClient> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsListening => true;

        public int ActiveConnections => 0;

        public long TotalConnectionsAccepted => 0;

        public long TotalConnectionsRejected => 0;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;

        public MeshOverlayServerStats GetStats() => new();

        public Task HandleExternallyAcceptedConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken = default)
        {
            Received.TrySetResult(tcpClient);
            return Task.CompletedTask;
        }
    }
}
