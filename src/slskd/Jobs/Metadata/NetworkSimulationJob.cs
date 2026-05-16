// <copyright file="NetworkSimulationJob.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Threading;
using System.Threading.Tasks;

namespace slskd.Jobs.Metadata;

/// <summary>
/// Mesh network simulation / stress test job shell.
/// </summary>
public class NetworkSimulationJob : IMetadataJob
{
    public string JobId { get; } = Ulid.NewUlid().ToString();
    public string Kind => "network-simulation";

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        // The job is registered as an explicit no-op until simulation work is enabled.
        return Task.CompletedTask;
    }
}
