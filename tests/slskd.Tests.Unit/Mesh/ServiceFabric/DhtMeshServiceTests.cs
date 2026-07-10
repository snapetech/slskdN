// <copyright file="DhtMeshServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.ServiceFabric;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using slskd.Mesh.Dht;
using slskd.Mesh.Messages;
using slskd.Mesh.Overlay;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.Mesh.Transport;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;

public class DhtMeshServiceTests
{
    [Fact]
    public async Task HandleCallAsync_Store_EnforcesAuthenticatedPublisherAndNamespaceQuota()
    {
        var keyPair = Ed25519KeyPair.Generate();
        var keyStore = new Mock<IKeyStore>();
        keyStore.Setup(store => store.Current).Returns(keyPair);
        var signer = new MeshMessageSigner(keyStore.Object, NullLogger<MeshMessageSigner>.Instance);
        var requesterId = SHA256.HashData(keyPair.PublicKey).AsSpan(0, 20).ToArray();
        var peerId = Ed25519Signer.DerivePeerId(keyPair.PublicKey);
        var dhtClient = new Mock<IDhtClient>();
        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            new KademliaRoutingTable(CreateNodeId(0x01)),
            dhtClient.Object,
            signer);

        var forged = await StoreAsync(service, signer, requesterId, 0, remotePeerId: "forged-peer");
        Assert.Equal(ServiceStatusCodes.Unauthorized, forged.StatusCode);

        for (var index = 0; index < 64; index++)
        {
            var accepted = await StoreAsync(service, signer, requesterId, index, peerId);
            Assert.Equal(ServiceStatusCodes.OK, accepted.StatusCode);
        }

        var limited = await StoreAsync(service, signer, requesterId, 64, peerId);
        Assert.Equal(ServiceStatusCodes.RateLimited, limited.StatusCode);
        dhtClient.Verify(client => client.PutAsync(
            It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(64));
    }

    [Fact]
    public async Task HandleCallAsync_UnknownMethod_ReturnsSanitizedMethodNotFound()
    {
        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            new KademliaRoutingTable(CreateNodeId(0x01)),
            Mock.Of<IDhtClient>(),
            Mock.Of<IMeshMessageSigner>());

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "dht",
                Method = "SensitiveDhtMethod",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = Array.Empty<byte>(),
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.MethodNotFound, reply.StatusCode);
        Assert.Equal("Unknown method", reply.ErrorMessage);
        Assert.DoesNotContain("SensitiveDhtMethod", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_Ping_WithPreCancelledToken_StillTouchesRoutingTable()
    {
        var routingTable = new KademliaRoutingTable(CreateNodeId(0x01));
        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            routingTable,
            Mock.Of<IDhtClient>(),
            Mock.Of<IMeshMessageSigner>());

        var requesterId = CreateNodeId(0x02);
        var call = new ServiceCall
        {
            ServiceName = "dht",
            Method = "Ping",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new PingRequest
            {
                RequesterId = requesterId,
            }),
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var reply = await service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-1" },
            cts.Token).ConfigureAwait(false);

        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && routingTable.Count == 0)
        {
            await Task.Delay(20).ConfigureAwait(false);
        }

        Assert.Single(routingTable.GetAllNodes());
        Assert.Equal("peer-1", routingTable.GetAllNodes()[0].Address);
    }

    [Fact]
    public async Task HandleCallAsync_FindValue_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var dhtClient = new Mock<IDhtClient>();
        dhtClient
            .Setup(client => client.GetAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            new KademliaRoutingTable(CreateNodeId(0x01)),
            dhtClient.Object,
            Mock.Of<IMeshMessageSigner>());

        var call = new ServiceCall
        {
            ServiceName = "dht",
            Method = "FindValue",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new FindValueRequest
            {
                Key = CreateNodeId(0x03),
                RequesterId = CreateNodeId(0x02)
            }),
        };

        var reply = await service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.UnknownError, reply.StatusCode);
        Assert.Equal("FindValue failed", reply.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_FindNode_WithInvalidPayload_ReturnsSanitizedError()
    {
        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            new KademliaRoutingTable(CreateNodeId(0x01)),
            Mock.Of<IDhtClient>(),
            Mock.Of<IMeshMessageSigner>());

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "dht",
                Method = "FindNode",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    TargetId = new byte[] { 0x01, 0x02 },
                    RequesterId = CreateNodeId(0x02)
                }),
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.InvalidPayload, reply.StatusCode);
        Assert.Equal("Invalid request payload", reply.ErrorMessage);
        Assert.DoesNotContain("20 bytes", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_Ping_WithInvalidRequesterId_ReturnsInvalidPayload()
    {
        var routingTable = new KademliaRoutingTable(CreateNodeId(0x01));
        var service = new DhtMeshService(
            Mock.Of<ILogger<DhtMeshService>>(),
            routingTable,
            Mock.Of<IDhtClient>(),
            Mock.Of<IMeshMessageSigner>());

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "dht",
                Method = "Ping",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new PingRequest
                {
                    RequesterId = new byte[] { 0x01, 0x02 }
                }),
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.InvalidPayload, reply.StatusCode);
        Assert.Equal("Invalid request payload", reply.ErrorMessage);
        Assert.Empty(routingTable.GetAllNodes());
    }

    private static byte[] CreateNodeId(byte value)
    {
        var nodeId = new byte[20];
        Array.Fill(nodeId, value);
        return nodeId;
    }

    private static async Task<ServiceReply> StoreAsync(
        DhtMeshService service,
        IMeshMessageSigner signer,
        byte[] requesterId,
        int keySuffix,
        string remotePeerId)
    {
        var key = new byte[20];
        key[1] = (byte)keySuffix;
        var message = DhtStoreMessage.CreateSigned(key, new byte[] { 1 }, requesterId, 60, signer);
        var call = new ServiceCall
        {
            ServiceName = "dht",
            Method = "Store",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new StoreRequest
            {
                Key = message.Key,
                Value = message.Value,
                RequesterId = message.RequesterId,
                TtlSeconds = message.TtlSeconds,
                PublicKeyBase64 = message.PublicKeyBase64!,
                SignatureBase64 = message.SignatureBase64!,
                TimestampUnixMs = message.TimestampUnixMs,
            }),
        };

        return await service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = remotePeerId },
            CancellationToken.None);
    }
}
