// <copyright file="KademliaRpcClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using Moq;
using slskd.Mesh;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.VirtualSoulfind.ShadowIndex;
using System.Text.Json;
using slskd.Mesh.Dht;
using slskd.Mesh.Messages;
using slskd.Mesh.Overlay;
using slskd.Mesh.Transport;
using slskd.Tests.Unit.TestHelpers;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class KademliaRpcClientTests
{
    [Fact]
    public async Task FindNode_WhenPeerCallIsCanceled_PropagatesCallerCancellation()
    {
        var self = Enumerable.Repeat((byte)1, 20).ToArray();
        var remote = Enumerable.Repeat((byte)2, 20).ToArray();
        using var cancellation = new CancellationTokenSource();
        var routing = new KademliaRoutingTable(self);
        await routing.TouchAsync(remote, "peer-1");
        var transport = new Mock<IMeshServiceClient>();
        transport
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<ServiceReply>(token);
            });
        using var client = new KademliaRpcClient(
            NullLogger<KademliaRpcClient>.Instance,
            transport.Object,
            routing,
            Mock.Of<IDhtClient>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.FindNodeAsync(self, cancellation.Token));
    }

    [Fact]
    public async Task FindValue_WhenPeerCallIsCanceled_PropagatesCallerCancellation()
    {
        var self = Enumerable.Repeat((byte)1, 20).ToArray();
        var remote = Enumerable.Repeat((byte)2, 20).ToArray();
        using var cancellation = new CancellationTokenSource();
        var routing = new KademliaRoutingTable(self);
        await routing.TouchAsync(remote, "peer-1");
        var dht = new Mock<IDhtClient>();
        dht.Setup(store => store.GetMultipleAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<byte[]>());
        var transport = new Mock<IMeshServiceClient>();
        transport
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<ServiceReply>(token);
            });
        using var client = new KademliaRpcClient(
            NullLogger<KademliaRpcClient>.Instance,
            transport.Object,
            routing,
            dht.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.FindValueAsync(self, cancellation.Token));
    }

    [Fact]
    public async Task Ping_WhenCallerCancelsMeshCall_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var transport = new Mock<IMeshServiceClient>();
        transport
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<ServiceReply>(token);
            });
        using var client = new KademliaRpcClient(
            NullLogger<KademliaRpcClient>.Instance,
            transport.Object,
            new KademliaRoutingTable(Enumerable.Repeat((byte)1, 20).ToArray()),
            Mock.Of<IDhtClient>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.PingAsync(new KNode(Enumerable.Repeat((byte)2, 20).ToArray(), "peer-1", DateTimeOffset.UtcNow), cancellation.Token));
    }

    [Fact]
    public async Task Store_WhenPeerWriteIsCanceled_PropagatesCallerCancellation()
    {
        var self = Enumerable.Repeat((byte)1, 20).ToArray();
        var remote = Enumerable.Repeat((byte)2, 20).ToArray();
        using var cancellation = new CancellationTokenSource();
        var routing = new KademliaRoutingTable(self);
        await routing.TouchAsync(remote, "peer-1");
        var transport = new Mock<IMeshServiceClient>();
        transport
            .Setup(client => client.CallAsync("peer-1", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall call, CancellationToken token) =>
            {
                if (call.Method == "FindNode")
                {
                    return Task.FromResult(new ServiceReply
                    {
                        StatusCode = ServiceStatusCodes.OK,
                        Payload = JsonSerializer.SerializeToUtf8Bytes(new FindNodeResponse
                        {
                            TargetId = self,
                            ResponderId = remote,
                            Nodes = Array.Empty<DhtNodeInfo>(),
                        }),
                    });
                }

                cancellation.Cancel();
                return Task.FromCanceled<ServiceReply>(token);
            });
        using var client = new KademliaRpcClient(
            NullLogger<KademliaRpcClient>.Instance,
            transport.Object,
            routing,
            Mock.Of<IDhtClient>());
        var message = new DhtStoreMessage
        {
            Key = self,
            Value = new byte[] { 1 },
            RequesterId = self,
            TtlSeconds = 60,
            PublicKeyBase64 = "public-key",
            SignatureBase64 = "signature",
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.StoreAsync(message, cancellation.Token));
    }

    [Fact]
    public async Task FindNode_RetainsKnownContactAndLearnsResponderIdentity()
    {
        var self = Enumerable.Repeat((byte)1, 20).ToArray();
        var remote = Enumerable.Repeat((byte)2, 20).ToArray();
        var routing = new KademliaRoutingTable(self);
        await routing.TouchAsync(remote, "radio-host");
        var transport = new Mock<IMeshServiceClient>();
        transport.Setup(client => client.CallAsync("radio-host", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply
            {
                CorrelationId = "lookup",
                StatusCode = ServiceStatusCodes.OK,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new FindNodeResponse
                {
                    TargetId = self,
                    ResponderId = remote,
                    Nodes = Array.Empty<DhtNodeInfo>(),
                }),
            });
        using var client = new KademliaRpcClient(NullLogger<KademliaRpcClient>.Instance, transport.Object, routing, Mock.Of<IDhtClient>());

        var closest = await client.FindNodeAsync(self);

        Assert.Equal("radio-host", Assert.Single(closest).Address);
        Assert.Equal(remote, Assert.Single(routing.GetAllNodes()).NodeId);
    }

    [Theory]
    [InlineData("FindNode", false)]
    [InlineData("FindValue", false)]
    [InlineData("Ping", false)]
    [InlineData("Store", false)]
    [InlineData("FindNode", true)]
    [InlineData("FindValue", true)]
    [InlineData("Ping", true)]
    [InlineData("Store", true)]
    public async Task PeerRpcFailure_EscapesAddressAndDiagnosticText(string operation, bool throws)
    {
        var self = Enumerable.Repeat((byte)1, 20).ToArray();
        var remote = Enumerable.Repeat((byte)2, 20).ToArray();
        const string address = "peer\r\nforged-address";
        var routing = new KademliaRoutingTable(self);
        await routing.TouchAsync(remote, address);
        var dht = new Mock<IDhtClient>();
        dht.Setup(store => store.GetMultipleAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<byte[]>());
        var transport = new Mock<IMeshServiceClient>();
        transport
            .Setup(client => client.CallAsync(It.IsAny<string>(), It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .Returns((string _, ServiceCall call, CancellationToken _) =>
            {
                if (operation == "Store" && call.Method == "FindNode")
                {
                    return Task.FromResult(new ServiceReply
                    {
                        StatusCode = ServiceStatusCodes.OK,
                        Payload = JsonSerializer.SerializeToUtf8Bytes(new FindNodeResponse
                        {
                            TargetId = self,
                            ResponderId = remote,
                            Nodes = Array.Empty<DhtNodeInfo>(),
                        }),
                    });
                }

                if (call.Method == operation)
                {
                    if (throws)
                    {
                        throw new InvalidOperationException("remote exception\r\nforged");
                    }

                    return Task.FromResult(new ServiceReply
                    {
                        StatusCode = ServiceStatusCodes.UnknownError,
                        ErrorMessage = "remote error\r\nforged",
                    });
                }

                throw new InvalidOperationException($"Unexpected RPC method {call.Method}");
            });
        var logger = new CapturingLogger<KademliaRpcClient>();
        using var client = new KademliaRpcClient(logger, transport.Object, routing, dht.Object);

        switch (operation)
        {
            case "FindNode":
                await client.FindNodeAsync(self);
                break;
            case "FindValue":
                await client.FindValueAsync(self);
                break;
            case "Ping":
                await client.PingAsync(new KNode(remote, address, DateTimeOffset.UtcNow));
                break;
            case "Store":
                await client.StoreAsync(new DhtStoreMessage
                {
                    Key = self,
                    Value = [1],
                    RequesterId = self,
                    TtlSeconds = 60,
                    PublicKeyBase64 = "key",
                    SignatureBase64 = "signature",
                    TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        var expectedOperation = operation switch
        {
            "FindNode" => "FIND_NODE query to",
            "FindValue" => "FIND_VALUE query to",
            "Ping" => "PING to",
            "Store" => "STORE to",
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        var entry = Assert.Single(logger.Entries, item => item.Message.Contains(expectedOperation, StringComparison.Ordinal));
        Assert.Contains("peer\\r\\nforged-address", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
        Assert.Contains(throws ? "remote exception\\r\\nforged" : "remote error\\r\\nforged", entry.Message);
    }

    [Fact]
    public void CreateSigned_CopiesMutableInputs()
    {
        var signer = new Mock<IMeshMessageSigner>();
        signer
            .Setup(s => s.SignMessage(It.IsAny<MeshMessage>()))
            .Returns<MeshMessage>(message =>
            {
                message.PublicKey = "test-key";
                message.Signature = "test-signature";
                return message;
            });

        var key = new byte[] { 1, 2, 3 };
        var value = new byte[] { 4, 5, 6 };
        var requesterId = new byte[] { 7, 8, 9 };

        var message = DhtStoreMessage.CreateSigned(key, value, requesterId, 30, signer.Object);

        key[0] = 9;
        value[1] = 8;
        requesterId[2] = 7;

        Assert.NotSame(key, message.Key);
        Assert.NotSame(value, message.Value);
        Assert.NotSame(requesterId, message.RequesterId);
        Assert.Equal(new byte[] { 1, 2, 3 }, message.Key);
        Assert.Equal(new byte[] { 4, 5, 6 }, message.Value);
        Assert.Equal(new byte[] { 7, 8, 9 }, message.RequesterId);
        Assert.Equal("test-key", message.PublicKeyBase64);
        Assert.Equal("test-signature", message.SignatureBase64);
        signer.Verify(s => s.SignMessage(It.IsAny<MeshMessage>()), Times.Once);
    }

    [Fact]
    public void CreateSigned_WithRealSigner_VerifiesSignature()
    {
        var keyPair = Ed25519KeyPair.Generate();
        var keyStore = new Mock<IKeyStore>();
        keyStore.Setup(k => k.Current).Returns(keyPair);
        var signer = new MeshMessageSigner(keyStore.Object, NullLogger<MeshMessageSigner>.Instance);

        var message = DhtStoreMessage.CreateSigned(
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 },
            SHA256.HashData(keyPair.PublicKey).AsSpan(0, 20).ToArray(),
            30,
            signer);

        Assert.True(message.VerifySignature());
    }

    [Fact]
    public void VerifySignature_WithExpectedPeer_BindsPublicKeyAndRequesterId()
    {
        var keyPair = Ed25519KeyPair.Generate();
        var keyStore = new Mock<IKeyStore>();
        keyStore.Setup(k => k.Current).Returns(keyPair);
        var signer = new MeshMessageSigner(keyStore.Object, NullLogger<MeshMessageSigner>.Instance);
        var requesterId = SHA256.HashData(keyPair.PublicKey).AsSpan(0, 20).ToArray();
        var message = DhtStoreMessage.CreateSigned(
            new byte[20],
            new byte[] { 1 },
            requesterId,
            60,
            signer);

        Assert.True(message.VerifySignature(Ed25519Signer.DerivePeerId(keyPair.PublicKey)));
        Assert.False(message.VerifySignature("forged-peer"));

        message.RequesterId[0] ^= 0xff;
        Assert.False(message.VerifySignature(Ed25519Signer.DerivePeerId(keyPair.PublicKey)));
    }

    [Fact]
    public void SignedStore_NodeIdMatchesSelfCertifyingPeerIdentityDigest()
    {
        var keyPair = Ed25519KeyPair.Generate();
        var nodeId = SHA256.HashData(keyPair.PublicKey).AsSpan(0, 20).ToArray();
        var keyStore = new Mock<IKeyStore>();
        keyStore.Setup(k => k.Current).Returns(keyPair);
        var signer = new MeshMessageSigner(keyStore.Object, NullLogger<MeshMessageSigner>.Instance);

        var message = DhtStoreMessage.CreateSigned(new byte[20], new byte[] { 1 }, nodeId, 60, signer);

        Assert.True(message.VerifySignature(Ed25519Signer.DerivePeerId(keyPair.PublicKey)));
    }

    [Fact]
    public void StoreSigningPayload_HasFrozenCrossRuntimeShape()
    {
        var message = new DhtStoreMessage
        {
            Key = Enumerable.Range(0, 20).Select(value => (byte)value).ToArray(),
            Value = new byte[] { 0xfb, 0x00, 0x2a },
            RequesterId = Enumerable.Range(20, 20).Select(value => (byte)value).ToArray(),
            TtlSeconds = 1800,
            PublicKeyBase64 = Convert.ToBase64String(new byte[32]),
            SignatureBase64 = Convert.ToBase64String(new byte[64]),
            TimestampUnixMs = 1_700_000_000_123,
        };

        Assert.Equal(
            "DhtStore|1700000000123|{\"type\":9,\"key\":\"AAECAwQFBgcICQoLDA0ODxAREhM=\",\"value\":\"\\u002BwAq\",\"requester_id\":\"FBUWFxgZGhscHR4fICEiIyQlJic=\",\"ttl_seconds\":1800,\"proto_version\":1,\"public_key\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\",\"timestamp_ms\":1700000000123}",
            message.GetSignablePayload());
    }

    [Fact]
    public void RustStoreVector_VerifiesAgainstDotNetContract()
    {
        var message = new DhtStoreMessage
        {
            Key = Convert.FromBase64String("Y2aTiJ42ZS6sj0j6bEGJ6uCjvn0="),
            Value = Convert.FromBase64String("+wAq"),
            RequesterId = Convert.FromBase64String("/oEsEvOrTOasXbaaw1L5BssbEe8="),
            TtlSeconds = 1800,
            PublicKeyBase64 = "6kpsY+KcUgq+9VB7Ey7F+ZVHdq6+vnuSQh7qaRRG0iw=",
            SignatureBase64 = "SdZK14zmKFaZk7tQ/oPWXkedEJxkQodrM6CINlBbuP6vlhYbZw0TwOwOa+mf1i5/rykdDe3UTx9zB08PHWcvCg==",
            TimestampUnixMs = 1_700_000_000_123,
        };
        var publicKeyBytes = Convert.FromBase64String(message.PublicKeyBase64);
        Assert.Equal(SHA256.HashData(publicKeyBytes).AsSpan(0, 20).ToArray(), message.RequesterId);
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519, publicKeyBytes, KeyBlobFormat.RawPublicKey);

        Assert.True(SignatureAlgorithm.Ed25519.Verify(
            publicKey,
            Encoding.UTF8.GetBytes(message.GetSignablePayload()),
            Convert.FromBase64String(message.SignatureBase64)));
    }
}
