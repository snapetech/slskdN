// <copyright file="PodPublisher.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.PodCore;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using slskd.Mesh.Dht;

/// <summary>
/// Publishes pod metadata to DHT for discovery.
/// </summary>
public interface IPodPublisher
{
    /// <summary>
    /// Publishes a pod's metadata to the DHT.
    /// </summary>
    Task PublishPodAsync(Pod pod, CancellationToken ct = default);

    /// <summary>
    /// Publishes a pod's metadata to the DHT (alias for PublishPodAsync).
    /// </summary>
    Task PublishAsync(Pod pod, CancellationToken ct = default) => PublishPodAsync(pod, ct);

    /// <summary>
    /// Updates a pod's metadata in the DHT (alias for PublishPodAsync).
    /// </summary>
    Task UpdatePodAsync(Pod pod, CancellationToken ct = default) => PublishPodAsync(pod, ct);

    /// <summary>
    /// Removes a pod's metadata from the DHT (unpublish).
    /// </summary>
    Task UnpublishPodAsync(string podId, CancellationToken ct = default);

    /// <summary>
    /// Refreshes pod metadata in DHT (updates TTL).
    /// </summary>
    Task RefreshPodAsync(string podId, CancellationToken ct = default);

    /// <summary>
    /// Refreshes a complete local snapshot of listed pods and their shared index.
    /// </summary>
    Task RefreshListedPodsAsync(IReadOnlyList<Pod> pods, CancellationToken ct = default);
}

/// <summary>
/// Implements pod metadata publishing to DHT.
/// </summary>
public class PodPublisher : IPodPublisher
{
    private readonly IMeshDhtClient dht;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PodPublisher> logger;
    private const int DefaultTTLSeconds = 3600; // 1 hour
    private const string PodKeyPrefix = "pod:metadata:";

    public PodPublisher(
        IMeshDhtClient dht,
        IServiceScopeFactory scopeFactory,
        ILogger<PodPublisher> logger)
    {
        this.dht = dht;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task PublishPodAsync(Pod pod, CancellationToken ct = default)
    {
        if (!CanPublish(pod))
        {
            return;
        }

        if (await TryPublishMetadataAsync(pod, ct))
        {
            await UpdatePodIndexAsync([pod.PodId], add: true, refreshUnchanged: false, ct);
        }
    }

    public async Task RefreshListedPodsAsync(IReadOnlyList<Pod> pods, CancellationToken ct = default)
    {
        var publishedPodIds = new List<string>(pods.Count);

        foreach (var pod in pods)
        {
            ct.ThrowIfCancellationRequested();
            if (CanPublish(pod) && await TryPublishMetadataAsync(pod, ct))
            {
                publishedPodIds.Add(pod.PodId);
            }
        }

        if (publishedPodIds.Count > 0)
        {
            await UpdatePodIndexAsync(publishedPodIds, add: true, refreshUnchanged: true, ct);
        }
    }

    private bool CanPublish(Pod? pod)
    {
        if (pod == null || string.IsNullOrWhiteSpace(pod.PodId))
        {
            logger.LogWarning("[PodPublisher] Cannot publish pod - invalid pod data");
            return false;
        }

        if (pod.Visibility != PodVisibility.Listed)
        {
            logger.LogDebug("[PodPublisher] Skipping publish for unlisted pod {PodId}", pod.PodId);
            return false;
        }

        // HARDENING: Never publish DM pods, even if marked as listed
        if (pod.Tags?.Contains("dm") == true)
        {
            logger.LogWarning("[PodPublisher] Blocking publish attempt for DM pod {PodId}", pod.PodId);
            return false;
        }

        return true;
    }

    private async Task<bool> TryPublishMetadataAsync(Pod pod, CancellationToken ct)
    {
        try
        {
            var dhtKey = DeriveDhtKey(pod.PodId);

            // Create pod metadata for DHT (exclude sensitive data)
            var metadata = new PodMetadata
            {
                PodId = pod.PodId,
                Name = pod.Name,
                Visibility = pod.Visibility,
                FocusContentId = pod.FocusContentId,
                Tags = pod.Tags ?? new List<string>(),
                ChannelCount = pod.Channels?.Count ?? 0,
                PublishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            // Publish to DHT with TTL
            await dht.PutAsync(dhtKey, Serialize(metadata), DefaultTTLSeconds, ct);

            logger.LogInformation("[PodPublisher] Published pod {PodId} ({Name}) to DHT with TTL {TTL}s",
                pod.PodId, pod.Name, DefaultTTLSeconds);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PodPublisher] Failed to publish pod {PodId} to DHT", pod.PodId);
            return false;
        }
    }

    public async Task UnpublishPodAsync(string podId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(podId))
        {
            return;
        }

        try
        {
            logger.LogInformation("[PodPublisher] Unpublishing pod {PodId} from DHT", podId);

            // Remove from index
            await UpdatePodIndexAsync([podId], add: false, refreshUnchanged: false, ct);

            // Note: DHT doesn't support deletion, metadata entry will expire naturally
            // We could publish a tombstone with short TTL if needed
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PodPublisher] Failed to unpublish pod {PodId} from DHT", podId);
        }
    }

    public async Task RefreshPodAsync(string podId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(podId))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var podService = scope.ServiceProvider.GetRequiredService<IPodService>();

            var pod = await podService.GetPodAsync(podId, ct);
            if (pod != null)
            {
                await PublishPodAsync(pod, ct);
            }
            else
            {
                logger.LogWarning("[PodPublisher] Cannot refresh pod {PodId} - pod not found", podId);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PodPublisher] Failed to refresh pod {PodId} in DHT", podId);
        }
    }

    private async Task UpdatePodIndexAsync(
        IReadOnlyCollection<string> podIds,
        bool add,
        bool refreshUnchanged,
        CancellationToken ct)
    {
        const string indexKey = "pod:index:listed";
        try
        {
            // Get current index
            var index = await GetPodIndexAsync(indexKey, ct);
            var changed = false;

            if (add)
            {
                foreach (var podId in podIds)
                {
                    if (!index.PodIds.Contains(podId))
                    {
                        index.PodIds.Add(podId);
                        changed = true;
                        logger.LogDebug("[PodPublisher] Added pod {PodId} to index", podId);
                    }
                }
            }
            else
            {
                foreach (var podId in podIds)
                {
                    if (index.PodIds.Remove(podId))
                    {
                        changed = true;
                        logger.LogDebug("[PodPublisher] Removed pod {PodId} from index", podId);
                    }
                }
            }

            if (changed || refreshUnchanged)
            {
                index.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await dht.PutAsync(indexKey, Serialize(index), DefaultTTLSeconds, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[PodPublisher] Failed to update pod index");
        }
    }

    private static string DeriveDhtKey(string podId)
    {
        return $"{PodKeyPrefix}{podId}";
    }

    private async Task<PodIndex> GetPodIndexAsync(string indexKey, CancellationToken ct)
    {
        var raw = await dht.GetRawAsync(indexKey, ct).ConfigureAwait(false);
        return raw == null
            ? new PodIndex { PodIds = new List<string>() }
            : JsonSerializer.Deserialize<PodIndex>(raw) ?? new PodIndex { PodIds = new List<string>() };
    }

    private static byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value);
    }
}

/// <summary>
/// Pod index stored in DHT (list of all listed pod IDs).
/// </summary>
public class PodIndex
{
    public List<string> PodIds { get; set; } = new();
    public long UpdatedAt { get; set; } // Unix timestamp in milliseconds
}

/// <summary>
/// Background service that periodically refreshes pod metadata in DHT.
/// </summary>
public class PodPublisherBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IPodPublisher podPublisher;
    private readonly ILogger<PodPublisherBackgroundService> logger;
    private const int RefreshIntervalMinutes = 30; // Refresh every 30 minutes

    public PodPublisherBackgroundService(
        IServiceScopeFactory scopeFactory,
        IPodPublisher podPublisher,
        ILogger<PodPublisherBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.podPublisher = podPublisher;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        await Task.Yield();

        logger.LogInformation("[PodPublisher] Starting background refresh service (interval: {Interval} minutes)", RefreshIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(RefreshIntervalMinutes), stoppingToken);
                await RefreshOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[PodPublisher] Error in background refresh cycle");

                // Continue running despite errors
            }
        }

        logger.LogInformation("[PodPublisher] Background refresh service stopped");
    }

    internal async Task RefreshOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var podService = scope.ServiceProvider.GetRequiredService<IPodService>();
        var listedPods = await podService.ListListedAsync(ct);

        logger.LogDebug("[PodPublisher] Refreshing {Count} listed pods in DHT", listedPods.Count);
        await podPublisher.RefreshListedPodsAsync(listedPods, ct);
        logger.LogInformation("[PodPublisher] Refreshed {Count} pods in DHT", listedPods.Count);
    }
}

/// <summary>
/// Pod metadata published to DHT (public information only).
/// </summary>
public class PodMetadata
{
    public string PodId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PodVisibility Visibility { get; set; }
    public string? FocusContentId { get; set; }
    public List<string> Tags { get; set; } = new();
    public int ChannelCount { get; set; }
    public long PublishedAt { get; set; } // Unix timestamp in milliseconds
}
