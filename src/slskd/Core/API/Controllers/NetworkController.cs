// <copyright file="NetworkController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using slskd.Backfill;
using slskd.Capabilities;
using slskd.Core.Security;
using slskd.DhtRendezvous;
using slskd.HashDb;
using slskd.Mesh;
using slskd.Transfers.MultiSource;

/// <summary>
///     Provides bounded network dashboard snapshots.
/// </summary>
[Route("api/v{version:apiVersion}/network")]
[ApiVersion("0")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
[ValidateCsrfForCookiesOnly]
public class NetworkController : ControllerBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NetworkController"/> class.
    /// </summary>
    public NetworkController(
        ICapabilityService capabilities,
        IHashDbService hashDb,
        IMeshSyncService meshSync,
        IBackfillSchedulerService backfill,
        IMultiSourceDownloadService multiSource,
        IDhtRendezvousService dht,
        IMeshStatsCollector meshStats)
    {
        Capabilities = capabilities;
        HashDb = hashDb;
        MeshSync = meshSync;
        Backfill = backfill;
        MultiSource = multiSource;
        Dht = dht;
        MeshStats = meshStats;
    }

    private IBackfillSchedulerService Backfill { get; }
    private ICapabilityService Capabilities { get; }
    private IDhtRendezvousService Dht { get; }
    private IHashDbService HashDb { get; }
    private IMeshStatsCollector MeshStats { get; }
    private IMeshSyncService MeshSync { get; }
    private IMultiSourceDownloadService MultiSource { get; }

    /// <summary>
    ///     Gets one snapshot containing the status used by shared network dashboards.
    /// </summary>
    /// <param name="includePeers">Whether to include peer lists used by the detailed Network pane.</param>
    /// <returns>The current network dashboard snapshot.</returns>
    [HttpGet("stats")]
    [Authorize(Policy = AuthPolicy.Any)]
    [ProducesResponseType(typeof(NetworkStatsResponse), 200)]
    public async Task<ActionResult<NetworkStatsResponse>> GetStats([FromQuery] bool includePeers = false)
    {
        var meshPeers = includePeers
            ? MeshSync.GetMeshPeers().ToArray()
            : Array.Empty<slskd.Mesh.MeshPeerInfo>();
        var discoveredPeers = includePeers
            ? Capabilities.GetAllSlskdnPeers().ToArray()
            : Array.Empty<PeerCapabilities>();

        return Ok(new NetworkStatsResponse
        {
            Backfill = Backfill.Stats,
            CapabilitiesJson = Capabilities.GetCapabilityFileContent(),
            CapabilitiesVersion = Capabilities.VersionString,
            Dht = Dht.GetStats(),
            DiscoveredPeers = discoveredPeers,
            HashDb = HashDb.GetStats(),
            Mesh = MeshSync.Stats,
            MeshPeers = meshPeers,
            SwarmJobs = MultiSource.ActiveDownloads.Select(download => new NetworkSwarmJobResponse
            {
                ActiveSources = download.Value.ActiveWorkers,
                DownloadedBytes = download.Value.BytesDownloaded,
                Filename = download.Value.Filename,
                JobId = download.Key,
                ProgressPercent = download.Value.TotalChunks > 0
                    ? download.Value.CompletedChunks * 100.0 / download.Value.TotalChunks
                    : 0,
                TotalBytes = download.Value.FileSize,
            }).ToArray(),
            Transport = await MeshStats.GetStatsAsync(),
        });
    }
}

/// <summary>
///     Network dashboard data returned from one bounded request.
/// </summary>
public sealed class NetworkStatsResponse
{
    public required BackfillStats Backfill { get; init; }
    public required string CapabilitiesJson { get; init; }
    public required string CapabilitiesVersion { get; init; }
    public required DhtRendezvousStats Dht { get; init; }
    public required IReadOnlyList<PeerCapabilities> DiscoveredPeers { get; init; }
    public required HashDbStats HashDb { get; init; }
    public required MeshSyncStats Mesh { get; init; }
    public required IReadOnlyList<slskd.Mesh.MeshPeerInfo> MeshPeers { get; init; }
    public required IReadOnlyList<NetworkSwarmJobResponse> SwarmJobs { get; init; }
    public required MeshTransportStats Transport { get; init; }
}

/// <summary>
///     Minimal active swarm status used by shared dashboards.
/// </summary>
public sealed class NetworkSwarmJobResponse
{
    public int ActiveSources { get; init; }
    public long DownloadedBytes { get; init; }
    public required string Filename { get; init; }
    public Guid JobId { get; init; }
    public double ProgressPercent { get; init; }
    public long TotalBytes { get; init; }
}
