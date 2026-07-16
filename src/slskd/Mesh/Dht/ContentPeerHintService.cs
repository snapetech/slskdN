// <copyright file="ContentPeerHintService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace slskd.Mesh.Dht;

/// <summary>
/// Enqueue content IDs to publish peer hints for, and publish in background.
/// </summary>
public interface IContentPeerHintService
{
    bool Enqueue(string contentId);
}

public class ContentPeerHintService : BackgroundService, IContentPeerHintService
{
    private const int DefaultBatchSize = 32;
    private readonly ILogger<ContentPeerHintService> logger;
    private readonly IContentPeerPublisher publisher;
    private readonly ConcurrentDictionary<string, byte> pendingContentIds = new(StringComparer.Ordinal);
    private readonly Channel<string> queue = Channel.CreateBounded<string>(new BoundedChannelOptions(1024)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false,
    });
    private readonly TimeSpan delayBetween;
    private readonly int batchSize;

    public ContentPeerHintService(ILogger<ContentPeerHintService> logger, IContentPeerPublisher publisher)
        : this(logger, publisher, TimeSpan.FromSeconds(1), DefaultBatchSize)
    {
    }

    internal ContentPeerHintService(
        ILogger<ContentPeerHintService> logger,
        IContentPeerPublisher publisher,
        TimeSpan delayBetween,
        int batchSize)
    {
        logger.LogDebug("[ContentPeerHintService] Constructor called");
        this.logger = logger;
        this.publisher = publisher;
        this.delayBetween = delayBetween;
        this.batchSize = batchSize;
        logger.LogDebug("[ContentPeerHintService] Constructor completed");
    }

    public bool Enqueue(string contentId)
    {
        if (!pendingContentIds.TryAdd(contentId, 0))
        {
            return true;
        }

        if (queue.Writer.TryWrite(contentId))
        {
            return true;
        }

        pendingContentIds.TryRemove(contentId, out _);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        await Task.Yield();

        logger.LogDebug("[ContentPeerHintService] ExecuteAsync called");
        while (await queue.Reader.WaitToReadAsync(stoppingToken))
        {
            var contentIds = new List<string>(batchSize);
            while (contentIds.Count < batchSize && queue.Reader.TryRead(out var contentId))
            {
                contentIds.Add(contentId);
            }

            try
            {
                await publisher.PublishBatchAsync(contentIds, delayBetween, stoppingToken);
                await Task.Delay(delayBetween, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[MeshContent] Failed to publish peer hint batch of {Count}: {Message}", contentIds.Count, ex.Message);
            }
            finally
            {
                foreach (var contentId in contentIds)
                {
                    pendingContentIds.TryRemove(contentId, out _);
                }
            }
        }
    }
}
