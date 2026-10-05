// <copyright file="MeshPeerManagerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using System.Net;
using slskd.Mesh;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class MeshPeerManagerTests
{
    [Fact]
    public void PeerLifecycleLogs_EscapePeerIdsWithoutChangingLookupKeys()
    {
        const string peerId = "peer-1\r\nforged warning";
        var logger = new CapturingLogger<MeshPeerManager>();
        var manager = new MeshPeerManager(logger);
        var endpoint = new IPEndPoint(IPAddress.Loopback, 50305);

        manager.AddOrUpdatePeer(new MeshPeer(peerId, [endpoint]));
        Assert.NotNull(manager.GetPeer(peerId));

        manager.UpdatePeerInfo(peerId, [endpoint], "mesh-test");
        manager.RecordConnectionSuccess(peerId, 12);
        manager.RecordConnectionFailure(peerId);

        Assert.NotNull(manager.GetPeer(peerId));
        manager.RemovePeer(peerId);
        Assert.Null(manager.GetPeer(peerId));

        Assert.Equal(5, logger.Entries.Count);
        foreach (var entry in logger.Entries)
        {
            Assert.Contains("peer-1\\r\\nforged warning", entry.Message);
            Assert.DoesNotContain('\r', entry.Message);
            Assert.DoesNotContain('\n', entry.Message);
        }
    }

    [Fact]
    public async Task MutablePeerState_IsCopiedOnInsertionAndLookup()
    {
        const string peerId = "peer-1";
        var manager = new MeshPeerManager(new CapturingLogger<MeshPeerManager>());
        var submitted = new MeshPeer(peerId, [new IPEndPoint(IPAddress.Loopback, 50305)])
        {
            TrustScore = 0.8,
            SupportsOnionRouting = false,
            Version = "submitted"
        };

        manager.AddOrUpdatePeer(submitted);
        submitted.TrustScore = 1.0;
        submitted.SupportsOnionRouting = true;
        submitted.Version = "caller-mutated";

        var returned = manager.GetPeer(peerId)!;
        Assert.Equal(0.8, returned.TrustScore);
        Assert.False(returned.SupportsOnionRouting);
        Assert.Equal("submitted", returned.Version);

        returned.TrustScore = 1.0;
        returned.SupportsOnionRouting = true;
        returned.Version = "lookup-mutated";

        var available = Assert.Single(await manager.GetAvailablePeersAsync());
        Assert.Equal(0.8, available.TrustScore);
        Assert.False(available.SupportsOnionRouting);
        Assert.Equal("submitted", available.Version);

        available.SupportsOnionRouting = true;
        available.TrustScore = 1.0;

        Assert.Empty(await manager.GetCircuitPeersAsync());
        var stored = manager.GetPeer(peerId)!;
        Assert.Equal(0.8, stored.TrustScore);
        Assert.False(stored.SupportsOnionRouting);
        Assert.Equal("submitted", stored.Version);
    }
}
