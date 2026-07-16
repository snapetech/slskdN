// <copyright file="MeshBootstrapService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Mesh.Dht;

namespace slskd.Mesh.Bootstrap;

/// <summary>
/// Hosted service to publish the initial self descriptor during bootstrap.
/// </summary>
public class MeshBootstrapService : BackgroundService
{
    private readonly ILogger<MeshBootstrapService> logger;
    private readonly IPeerDescriptorPublisher publisher;
    private readonly MeshOptions options;

    public MeshBootstrapService(
        ILogger<MeshBootstrapService> logger,
        IPeerDescriptorPublisher publisher,
        IOptions<MeshOptions> options)
    {
        logger.LogDebug("[MeshBootstrapService] Constructor called");
        this.logger = logger;
        this.publisher = publisher;
        this.options = options.Value;
        logger.LogDebug("[MeshBootstrapService] Constructor completed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        await Task.Yield();

        if (!options.EnableDht)
        {
            logger.LogInformation("[MeshBootstrap] DHT disabled; skipping bootstrap publish loop");
            return;
        }

        logger.LogDebug("[MeshBootstrapService] ExecuteAsync called");
        logger.LogInformation("[MeshBootstrap] Publishing initial self descriptor (bootstrap nodes: {Count})", options.BootstrapNodes.Count);

        await PublishOnce(stoppingToken);
        logger.LogInformation("[MeshBootstrap] Initial self descriptor publication completed");
    }

    private async Task PublishOnce(CancellationToken ct)
    {
        // Called both before and inside the ExecuteAsync loop, whose only catch handles
        // OperationCanceledException. A non-cancellation publish failure escaping here would
        // reach ExecuteAsync and, under the default StopHost behavior, take down the host.
        try
        {
            logger.LogInformation("[MeshBootstrap] Publishing self descriptor to DHT");
            await publisher.PublishSelfAsync(ct);
            logger.LogDebug("[MeshBootstrap] Self descriptor published; bootstrap nodes={Count}", options.BootstrapNodes.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "[MeshBootstrap] Failed to publish self descriptor to DHT");
        }
    }
}
