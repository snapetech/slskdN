// <copyright file="MeshPeerPublicKeyResolverTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.ServiceFabric;

using Moq;
using slskd.Mesh.Dht;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.Transport;
using Xunit;

public class MeshPeerPublicKeyResolverTests
{
    [Fact]
    public async Task ResolveTrustedKeysAsync_WithSelfCertifyingSignedDescriptor_ReturnsKey()
    {
        var (descriptor, publicKey) = CreateSignedDescriptor();
        var dhtClient = new Mock<IMeshDhtClient>();
        dhtClient.Setup(client => client.GetAsync<MeshPeerDescriptor>(
                $"mesh:peer:{descriptor.PeerId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(descriptor);
        var resolver = new MeshPeerPublicKeyResolver(dhtClient.Object);

        var keys = await resolver.ResolveTrustedKeysAsync(descriptor.PeerId);

        Assert.Single(keys);
        Assert.Equal(publicKey, keys[0]);
    }

    [Fact]
    public async Task ResolveTrustedKeysAsync_WithForgedPeerDescriptor_ReturnsNoKeys()
    {
        var (descriptor, _) = CreateSignedDescriptor();
        descriptor.Signature = Convert.ToBase64String(new byte[64]);
        var dhtClient = new Mock<IMeshDhtClient>();
        dhtClient.Setup(client => client.GetAsync<MeshPeerDescriptor>(
                $"mesh:peer:{descriptor.PeerId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(descriptor);
        var resolver = new MeshPeerPublicKeyResolver(dhtClient.Object);

        var keys = await resolver.ResolveTrustedKeysAsync(descriptor.PeerId);

        Assert.Empty(keys);
    }

    private static (MeshPeerDescriptor Descriptor, byte[] PublicKey) CreateSignedDescriptor()
    {
        using var signer = new Ed25519Signer();
        var (privateKey, publicKey) = signer.GenerateKeyPair();
        var descriptor = new MeshPeerDescriptor
        {
            PeerId = Ed25519Signer.DerivePeerId(publicKey),
            ExpiresAtUnixMs = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
            ControlSigningKeys = new List<string> { Convert.ToBase64String(publicKey) },
        };
        descriptor.Signature = Convert.ToBase64String(signer.Sign(descriptor.GetSignableData(), privateKey));
        return (descriptor, publicKey);
    }
}
