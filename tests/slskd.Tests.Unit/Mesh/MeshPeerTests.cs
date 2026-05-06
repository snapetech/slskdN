// <copyright file="MeshPeerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Net;
using slskd.Mesh;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class MeshPeerTests
{
    [Fact]
    public void Constructor_CopiesMutableEndpoints()
    {
        var endpoint = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 50305);
        var peer = new MeshPeer("peer-a", new List<IPEndPoint> { endpoint });

        endpoint.Address = IPAddress.Parse("203.0.113.11");
        endpoint.Port = 50306;

        Assert.Equal("203.0.113.10:50305", peer.Addresses.Single().ToString());
        Assert.NotSame(endpoint, peer.Addresses.Single());
        Assert.Equal("203.0.113.10:50305", peer.GetBestAddress().ToString());
    }

    [Fact]
    public void UpdateInfo_CopiesMutableEndpoints()
    {
        var peer = new MeshPeer(
            "peer-a",
            new List<IPEndPoint> { new(IPAddress.Parse("203.0.113.10"), 50305) });
        var endpoint = new IPEndPoint(IPAddress.Parse("198.51.100.20"), 50400);

        peer.UpdateInfo(new List<IPEndPoint> { endpoint });

        endpoint.Address = IPAddress.Parse("198.51.100.21");
        endpoint.Port = 50401;

        Assert.Equal("198.51.100.20:50400", peer.Addresses.Single().ToString());
        Assert.NotSame(endpoint, peer.Addresses.Single());
        Assert.Equal("198.51.100.20:50400", peer.GetBestAddress().ToString());
    }

    [Fact]
    public void Addresses_AndBestAddress_ReturnCopies()
    {
        var peer = new MeshPeer(
            "peer-a",
            new List<IPEndPoint> { new(IPAddress.Parse("203.0.113.10"), 50305) });

        var address = peer.Addresses.Single();
        var bestAddress = peer.GetBestAddress();

        address.Port = 1;
        bestAddress.Port = 2;

        Assert.Equal("203.0.113.10:50305", peer.Addresses.Single().ToString());
        Assert.Equal("203.0.113.10:50305", peer.GetBestAddress().ToString());
    }
}
