// <copyright file="SharedMeshTcpListenerRealMeshOverlayTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using slskd.DhtRendezvous;
using slskd.DhtRendezvous.Messages;
using slskd.DhtRendezvous.Search;
using slskd.DhtRendezvous.Security;
using slskd.Mesh;
using slskd.Mesh.Messages;
using slskd.SoulseekRuntime;
using Xunit;
using Moq;
using System.Text.Json;
using slskd.ListeningParty;
using slskd.Streaming;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.Mesh.Overlay;

/// <summary>
/// Proves the mesh-overlay half of port sharing end to end using the REAL production classes on
/// both sides -- a real <see cref="MeshOverlayServer"/> (real self-signed certificates, real TLS,
/// real mesh_hello/mesh_hello_ack protocol handling) behind a real <see cref="SharedMeshTcpListener"/>,
/// connected to by the real client-side <see cref="MeshOverlayConnection.ConnectAsync"/> +
/// <see cref="MeshOverlayConnection.PerformClientHandshakeAsync"/> path that a genuine slskdN peer
/// uses. Nothing here is mocked except the few collaborators (outbound reciprocal connector, mesh
/// sync, mesh search RPC) that this specific flow never touches.
/// </summary>
public sealed class SharedMeshTcpListenerRealMeshOverlayTests : IDisposable
{
    private readonly string _serverAppDirectory = Directory.CreateTempSubdirectory("slskdn-mesh-server-").FullName;
    private readonly string _clientAppDirectory = Directory.CreateTempSubdirectory("slskdn-mesh-client-").FullName;

    [Fact]
    public async Task RealClientHandshake_ThroughSharedTcpListener_IsAcceptedByRealMeshOverlayServer()
    {
        var dhtOptions = new DhtRendezvousOptions { Enabled = true };
        var meshOverlayServer = new MeshOverlayServer(
            NullLogger<MeshOverlayServer>.Instance,
            new StaticOptionsMonitor(new slskd.Options { Soulseek = new slskd.Options.SoulseekOptions { Username = "server-peer" } }),
            new CertificateManager(NullLogger<CertificateManager>.Instance, _serverAppDirectory),
            new CertificatePinStore(NullLogger<CertificatePinStore>.Instance, _serverAppDirectory),
            new OverlayRateLimiter(),
            new OverlayBlocklist(NullLogger<OverlayBlocklist>.Instance),
            new MeshNeighborRegistry(NullLogger<MeshNeighborRegistry>.Instance),
            new NoOpMeshOverlayConnector(),
            new NoOpMeshSyncService(),
            new NoOpMeshSearchRpcHandler(),
            new MeshOverlayRequestRouter(),
            dhtOptions);

        var optionsAtStartup = new OptionsAtStartup
        {
            Soulseek = new slskd.Options.SoulseekOptions
            {
                ListenIpAddress = "127.0.0.1",
                ListenPort = 0, // OS-assigned ephemeral port
            },
        };

        var sharedListener = new SharedMeshTcpListener(
            NullLogger<SharedMeshTcpListener>.Instance,
            optionsAtStartup,
            dhtOptions,
            new FedTcpListener(), // unused by this test: only the mesh-overlay TLS path is exercised
            meshOverlayServer);

        await meshOverlayServer.StartAsync();
        await sharedListener.StartAsync(CancellationToken.None);

        try
        {
            var boundEndPoint = await WaitForBoundEndPointAsync(sharedListener);

            var clientCertificateManager = new CertificateManager(NullLogger<CertificateManager>.Instance, _clientAppDirectory);
            var clientCertificate = clientCertificateManager.GetOrCreateServerCertificate();

            await using var connection = await MeshOverlayConnection.ConnectAsync(boundEndPoint, clientCertificate);
            var ack = await connection.PerformClientHandshakeAsync("client-peer", overlayPort: 12345);

            Assert.NotNull(ack);
            Assert.True(connection.IsHandshakeComplete);

            // Real production accounting on the real server, proving the connection was actually
            // accepted and processed -- not just that the TCP connect succeeded.
            await WaitUntilAsync(() => meshOverlayServer.TotalConnectionsAccepted == 1, TimeSpan.FromSeconds(5));
            Assert.Equal(1, meshOverlayServer.TotalConnectionsAccepted);
            Assert.Equal(0, meshOverlayServer.TotalConnectionsRejected);
            Assert.Equal(1, meshOverlayServer.ActiveConnections);
        }
        finally
        {
            await sharedListener.StopAsync(CancellationToken.None);
            await meshOverlayServer.StopAsync();
        }
    }

    [Fact]
    public async Task ListedRadio_RealTlsHostAndClient_ReadBytesAndEnforceRevocation()
    {
        var file = Path.Combine(_serverAppDirectory, "radio.wav");
        var bytes = Enumerable.Range(0, ListedRadioMeshService.MaxChunkBytes + 15).Select(index => (byte)(index % 251)).ToArray();
        await File.WriteAllBytesAsync(file, bytes);
        var state = new ListeningPartyEvent { PartyId = "party", ContentId = "track", Listed = true, AllowMeshStreaming = true };
        var parties = new Mock<IListeningPartyService>();
        parties.Setup(service => service.GetStateByPartyIdAsync("party", It.IsAny<CancellationToken>())).ReturnsAsync(() => state);
        var tickets = new StreamTicketService();
        var capability = tickets.Create("track", "listening-party:party", TimeSpan.FromMinutes(2));
        var locator = new Mock<IContentLocator>();
        locator.Setup(service => service.Resolve("track", It.IsAny<CancellationToken>())).Returns(new ResolvedContent(file, bytes.Length, "audio/wav"));
        var hostOptions = new StaticOptionsMonitor(new slskd.Options
        {
            Feature = new slskd.Options.FeatureOptions { Mesh = true, Streaming = true },
            Soulseek = new slskd.Options.SoulseekOptions { Username = "radio-host" },
        });
        var serviceRouter = new MeshServiceRouter(NullLogger<MeshServiceRouter>.Instance, Microsoft.Extensions.Options.Options.Create(new MeshServiceFabricOptions()));
        serviceRouter.RegisterService(new ListedRadioMeshService(parties.Object, tickets, locator.Object, hostOptions, NullLogger<ListedRadioMeshService>.Instance));
        var dhtOptions = new DhtRendezvousOptions { Enabled = true };
        var hostAccounting = new Mock<slskd.Transfers.MultiSource.Metrics.ITrafficAccountingService>();
        var clientAccounting = new Mock<slskd.Transfers.MultiSource.Metrics.ITrafficAccountingService>();
        var serverRegistry = new MeshNeighborRegistry(NullLogger<MeshNeighborRegistry>.Instance);
        var serverRequests = new MeshOverlayRequestRouter();
        var server = new MeshOverlayServer(
            NullLogger<MeshOverlayServer>.Instance, hostOptions,
            new CertificateManager(NullLogger<CertificateManager>.Instance, _serverAppDirectory),
            new CertificatePinStore(NullLogger<CertificatePinStore>.Instance, _serverAppDirectory),
            new OverlayRateLimiter(), new OverlayBlocklist(NullLogger<OverlayBlocklist>.Instance),
            serverRegistry,
            new NoOpMeshOverlayConnector(), new NoOpMeshSyncService(), new NoOpMeshSearchRpcHandler(),
            serverRequests, dhtOptions, serviceRouter, hostAccounting.Object);
        var listener = new SharedMeshTcpListener(
            NullLogger<SharedMeshTcpListener>.Instance,
            new OptionsAtStartup { Soulseek = new slskd.Options.SoulseekOptions { ListenIpAddress = "127.0.0.1", ListenPort = 0 } },
            dhtOptions, new FedTcpListener(), server);
        var registry = new MeshNeighborRegistry(NullLogger<MeshNeighborRegistry>.Instance);
        var requests = new MeshOverlayRequestRouter();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await server.StartAsync(timeout.Token);
        await listener.StartAsync(timeout.Token);
        try
        {
            var connector = new MeshOverlayConnector(
                NullLogger<MeshOverlayConnector>.Instance,
                new StaticOptionsMonitor(new slskd.Options { Soulseek = new slskd.Options.SoulseekOptions { Username = "radio-listener" } }),
                new CertificateManager(NullLogger<CertificateManager>.Instance, _clientAppDirectory),
                new CertificatePinStore(NullLogger<CertificatePinStore>.Instance, _clientAppDirectory),
                new OverlayRateLimiter(), new OverlayBlocklist(NullLogger<OverlayBlocklist>.Instance),
                registry, new NoOpMeshSyncService(), new NoOpMeshSearchRpcHandler(), requests, serviceRouter, clientAccounting.Object);
            var connection = await connector.ConnectToEndpointAsync(await WaitForBoundEndPointAsync(listener), timeout.Token);
            Assert.NotNull(connection);
            var client = new MeshServiceClient(NullLogger<MeshServiceClient>.Instance, Mock.Of<IMeshServiceDirectory>(), Mock.Of<IControlSigner>(), registry, requests);
            ServiceCall Read(long offset, int length) => new()
            {
                ServiceName = "ListedRadio",
                Method = "Read",
                CorrelationId = Guid.NewGuid().ToString("N"),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListedRadioRequest("party", "track", capability, offset, length), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            };

            var first = await client.CallAsync("radio-host", Read(0, ListedRadioMeshService.MaxChunkBytes), timeout.Token);
            Assert.Equal(ServiceStatusCodes.OK, first.StatusCode);
            Assert.Equal(bytes[..ListedRadioMeshService.MaxChunkBytes], first.Payload);
            var tail = await client.CallAsync("radio-host", Read(ListedRadioMeshService.MaxChunkBytes, 15), timeout.Token);
            Assert.Equal(ServiceStatusCodes.OK, tail.StatusCode);
            Assert.Equal(bytes[ListedRadioMeshService.MaxChunkBytes..], tail.Payload);
            var metadata = await client.CallAsync("radio-host", Read(0, 15) with { Method = "Metadata" }, timeout.Token);
            Assert.Equal(ServiceStatusCodes.OK, metadata.StatusCode);
            var expiredCapability = tickets.Create("track", "listening-party:party", TimeSpan.FromSeconds(-1));
            var expired = await client.CallAsync("radio-host", Read(0, 15) with
            {
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListedRadioRequest("party", "track", expiredCapability, 0, 15), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            }, timeout.Token);
            Assert.Equal(403, expired.StatusCode);
            hostAccounting.Verify(service => service.AddOverlayUploadAsync(ListedRadioMeshService.MaxChunkBytes, It.IsAny<CancellationToken>()), Times.Once);
            hostAccounting.Verify(service => service.AddOverlayUploadAsync(15, It.IsAny<CancellationToken>()), Times.Once);
            hostAccounting.VerifyNoOtherCalls();

            // Exercise the connector's inbound RPC handler on the same real TLS link.
            var inbound = Assert.Single(serverRegistry.GetAllConnections());
            var reverse = Read(0, 15);
            var reverseReply = serverRequests.WaitForMeshServiceReplyAsync(inbound, reverse.CorrelationId, timeout.Token);
            await inbound.WriteMessageAsync(new slskd.DhtRendezvous.Messages.MeshServiceCallMessage
            {
                CorrelationId = reverse.CorrelationId,
                ServiceName = reverse.ServiceName,
                Method = reverse.Method,
                Payload = reverse.Payload,
            }, timeout.Token);
            Assert.Equal(bytes[..15], (await reverseReply).Payload);
            var barrier = Read(0, 15) with { Method = "Metadata" };
            var barrierReply = serverRequests.WaitForMeshServiceReplyAsync(inbound, barrier.CorrelationId, timeout.Token);
            await inbound.WriteMessageAsync(new slskd.DhtRendezvous.Messages.MeshServiceCallMessage
            {
                CorrelationId = barrier.CorrelationId,
                ServiceName = barrier.ServiceName,
                Method = barrier.Method,
                Payload = barrier.Payload,
            }, timeout.Token);
            Assert.Equal(ServiceStatusCodes.OK, (await barrierReply).StatusCode);
            clientAccounting.Verify(service => service.AddOverlayUploadAsync(15, It.IsAny<CancellationToken>()), Times.Once);
            clientAccounting.VerifyNoOtherCalls();

            state = state with { AllowMeshStreaming = false };
            var revoked = await client.CallAsync("radio-host", Read(0, 15), timeout.Token);
            Assert.Equal(404, revoked.StatusCode);
            Assert.Empty(revoked.Payload);
            hostAccounting.VerifyNoOtherCalls();
            await connection.DisconnectAsync("Test completed", timeout.Token);
        }
        finally
        {
            await listener.StopAsync(CancellationToken.None);
            await server.StopAsync();
        }
    }

    public void Dispose()
    {
        TryDeleteDirectory(_serverAppDirectory);
        TryDeleteDirectory(_clientAppDirectory);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
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

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                return;
            }

            await Task.Delay(10);
        }
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<slskd.Options>
    {
        public StaticOptionsMonitor(slskd.Options value)
        {
            CurrentValue = value;
        }

        public slskd.Options CurrentValue { get; }

        public slskd.Options Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<slskd.Options, string> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class NoOpMeshOverlayConnector : IMeshOverlayConnector
    {
        public int PendingConnections => 0;

        public long SuccessfulConnections => 0;

        public long FailedConnections => 0;

        public Task<int> ConnectToCandidatesAsync(System.Collections.Generic.IEnumerable<IPEndPoint> candidates, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<MeshOverlayConnection?> ConnectToEndpointAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default)
            => Task.FromResult<MeshOverlayConnection?>(null);

        public MeshOverlayConnectorStats GetStats() => new();
    }

    private sealed class NoOpMeshSearchRpcHandler : IMeshSearchRpcHandler
    {
        public Task<MeshSearchResponseMessage> HandleAsync(MeshSearchRequestMessage request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Not exercised by this test.");
    }

    private sealed class NoOpMeshSyncService : IMeshSyncService
    {
        public MeshSyncStats Stats { get; } = new();

        public Task<MeshSyncResult> TrySyncWithPeerAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(new MeshSyncResult { Success = false, PeerUsername = username });

        public Task<MeshMessage?> HandleMessageAsync(string fromUser, MeshMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult<MeshMessage?>(null);

        public Task<MeshHashEntry?> LookupHashAsync(string flacKey, CancellationToken cancellationToken = default)
            => Task.FromResult<MeshHashEntry?>(null);

        public Task PublishHashAsync(string flacKey, string byteHash, long size, int? metaFlags = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public System.Collections.Generic.IEnumerable<slskd.Mesh.MeshPeerInfo> GetMeshPeers() => System.Array.Empty<slskd.Mesh.MeshPeerInfo>();

        public slskd.Mesh.Messages.MeshHelloMessage GenerateHelloMessage() => new() { ClientId = "server-peer" };

        public Task<MeshPushDeltaMessage> GenerateDeltaResponseAsync(long sinceSeqId, int maxEntries, CancellationToken cancellationToken = default)
            => Task.FromResult(new MeshPushDeltaMessage());

        public Task<int> MergeEntriesAsync(string fromUser, System.Collections.Generic.IEnumerable<MeshHashEntry> entries, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public void Dispose()
        {
        }
    }
}
