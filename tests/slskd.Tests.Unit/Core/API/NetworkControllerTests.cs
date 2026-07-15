// <copyright file="NetworkControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Core.API;

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.Backfill;
using slskd.Capabilities;
using slskd.Core.API;
using slskd.DhtRendezvous;
using slskd.HashDb;
using slskd.Mesh;
using slskd.Transfers.MultiSource;
using Xunit;

public class NetworkControllerTests
{
    [Fact]
    public async Task GetStats_Returns_One_Combined_Snapshot_With_Peers()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.GetStats(includePeers: true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NetworkStatsResponse>(ok.Value);
        Assert.Equal("slskdn/1.0+mesh", response.CapabilitiesVersion);
        Assert.Equal(42, response.HashDb.TotalHashEntries);
        Assert.Equal(3, response.Mesh.KnownMeshPeers);
        Assert.Single(response.MeshPeers);
        Assert.Single(response.DiscoveredPeers);
        var swarmJob = Assert.Single(response.SwarmJobs);
        Assert.Equal(2, swarmJob.ActiveSources);
        Assert.Equal(512, swarmJob.DownloadedBytes);
        Assert.Equal("album/track.flac", swarmJob.Filename);
        Assert.Equal(50, swarmJob.ProgressPercent);
        Assert.Equal(1_024, swarmJob.TotalBytes);
        Assert.Equal(7, response.Transport.ActiveDhtSessions);
        Assert.DoesNotContain("outputPath", JsonSerializer.Serialize(response), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStats_Without_Peers_Skips_Peer_List_Enumeration()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.GetStats(includePeers: false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<NetworkStatsResponse>(ok.Value);
        Assert.Empty(response.MeshPeers);
        Assert.Empty(response.DiscoveredPeers);
        fixture.MeshSync.Verify(service => service.GetMeshPeers(), Times.Never);
        fixture.Capabilities.Verify(service => service.GetAllSlskdnPeers(), Times.Never);
    }

    private static Fixture CreateFixture()
    {
        var capabilities = new Mock<ICapabilityService>();
        capabilities.SetupGet(service => service.VersionString).Returns("slskdn/1.0+mesh");
        capabilities
            .Setup(service => service.GetCapabilityFileContent())
            .Returns("{\"version\":\"1.0\",\"features\":[\"mesh_sync\"]}");
        capabilities
            .Setup(service => service.GetAllSlskdnPeers())
            .Returns([
                new PeerCapabilities
                {
                    ClientVersion = "slskdn/1.0",
                    LastSeen = DateTime.UtcNow,
                    Username = "discovered-peer",
                },
            ]);

        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetStats()).Returns(new HashDbStats
        {
            CurrentSeqId = 9,
            TotalHashEntries = 42,
        });

        var meshSync = new Mock<IMeshSyncService>();
        meshSync.SetupGet(service => service.Stats).Returns(new MeshSyncStats
        {
            CurrentSeqId = 9,
            KnownMeshPeers = 3,
        });
        meshSync.Setup(service => service.GetMeshPeers()).Returns([
            new slskd.Mesh.MeshPeerInfo
            {
                LatestSeqId = 8,
                Username = "mesh-peer",
            },
        ]);

        var backfill = new Mock<IBackfillSchedulerService>();
        backfill.SetupGet(service => service.Stats).Returns(new BackfillStats
        {
            Active = 1,
        });

        var activeDownloads = new ConcurrentDictionary<Guid, MultiSourceDownloadStatus>();
        activeDownloads[Guid.NewGuid()] = new MultiSourceDownloadStatus
        {
            ActiveWorkers = 2,
            BytesDownloaded = 512,
            CompletedChunks = 2,
            FileSize = 1_024,
            Filename = "album/track.flac",
            TotalChunks = 4,
        };
        var multiSource = new Mock<IMultiSourceDownloadService>();
        multiSource.SetupGet(service => service.ActiveDownloads).Returns(activeDownloads);

        var dht = new Mock<IDhtRendezvousService>();
        dht.Setup(service => service.GetStats()).Returns(new DhtRendezvousStats
        {
            DhtNodeCount = 7,
            IsDhtRunning = true,
        });

        var meshStats = new Mock<IMeshStatsCollector>();
        meshStats
            .Setup(service => service.GetStatsAsync())
            .ReturnsAsync(new MeshTransportStats(7, 2, 0, slskd.Mesh.NatType.Direct));

        return new Fixture(
            new NetworkController(
                capabilities.Object,
                hashDb.Object,
                meshSync.Object,
                backfill.Object,
                multiSource.Object,
                dht.Object,
                meshStats.Object),
            capabilities,
            meshSync);
    }

    private sealed record Fixture(
        NetworkController Controller,
        Mock<ICapabilityService> Capabilities,
        Mock<IMeshSyncService> MeshSync);
}
