// <copyright file="MeshDhtClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Mesh.Dht;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class MeshDhtClientTests
{
    [Fact]
    public void DeriveKey_UsesFrozenSha1NamespaceContract()
    {
        var key = MeshDhtClient.DeriveKey("mesh:content-peers:recording-1");

        Assert.Equal(20, key.Length);
        Assert.Equal("636693889e36652eac8f48fa6c4189eae0a3be7d", Convert.ToHexString(key).ToLowerInvariant());
    }

    [Fact]
    public async Task PutAsync_WithoutDistributedService_UsesTwentyByteDerivedKey()
    {
        var inner = new Mock<IDhtClient>();
        var client = new MeshDhtClient(
            NullLogger<MeshDhtClient>.Instance,
            inner.Object);

        await client.PutAsync("mesh:content-peers:recording-1", new byte[] { 1, 2, 3 }, 1800);

        inner.Verify(dht => dht.PutAsync(
            It.Is<byte[]>(key => key.Length == 20 && Convert.ToHexString(key) == "636693889E36652EAC8F48FA6C4189EAE0A3BE7D"),
            It.Is<byte[]>(value => value.SequenceEqual(new byte[] { 1, 2, 3 })),
            1800,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
