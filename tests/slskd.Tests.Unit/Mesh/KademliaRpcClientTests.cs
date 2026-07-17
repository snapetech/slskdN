// <copyright file="KademliaRpcClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Mesh;
using slskd.Mesh.Dht;
using slskd.Mesh.Messages;
using slskd.Mesh.Overlay;
using slskd.Mesh.Transport;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class KademliaRpcClientTests
{
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
