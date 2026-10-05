// <copyright file="MeshMessageSignerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using System;
using Moq;
using slskd.Mesh;
using slskd.Mesh.Messages;
using slskd.Mesh.Overlay;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class MeshMessageSignerTests
{
    [Fact]
    public void VerifyMessage_WhenPeerPublicKeyIsMalformed_EscapesFailureWithoutAttachingException()
    {
        var logger = new CapturingLogger<MeshMessageSigner>();
        var signer = new MeshMessageSigner(Mock.Of<IKeyStore>(), logger);
        var message = new MeshHelloMessage
        {
            PublicKey = "invalid\r\nkey",
            Signature = "signature",
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        Assert.False(signer.VerifyMessage(message));

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("System.FormatException", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public void SignMessage_WhenKeyStoreThrows_EscapesFailureAndRethrows()
    {
        var keyStore = new Mock<IKeyStore>();
        keyStore.SetupGet(store => store.Current).Throws(new InvalidOperationException("key store failure\r\nforged"));
        var logger = new CapturingLogger<MeshMessageSigner>();
        var signer = new MeshMessageSigner(keyStore.Object, logger);

        var exception = Assert.Throws<InvalidOperationException>(() => signer.SignMessage(new MeshHelloMessage()));

        Assert.Contains("key store failure\r\nforged", exception.Message);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains("key store failure\\r\\nforged", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
    }
}
