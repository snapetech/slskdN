// <copyright file="KademliaRpcClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Mesh;
using slskd.Mesh.Dht;
using slskd.Mesh.Messages;
using slskd.Mesh.Overlay;
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
        var keyStore = new Mock<IKeyStore>();
        keyStore.Setup(k => k.Current).Returns(Ed25519KeyPair.Generate());
        var signer = new MeshMessageSigner(keyStore.Object, NullLogger<MeshMessageSigner>.Instance);

        var message = DhtStoreMessage.CreateSigned(
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 },
            new byte[] { 7, 8, 9 },
            30,
            signer);

        Assert.True(message.VerifySignature());
    }
}
