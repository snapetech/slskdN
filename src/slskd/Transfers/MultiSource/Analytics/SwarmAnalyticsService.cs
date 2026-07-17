// <copyright file="SwarmAnalyticsService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Telemetry;
using slskd.Transfers.MultiSource.Metrics;
using static slskd.Telemetry.PeerMetrics;
using static slskd.Telemetry.SwarmMetrics;

/// <summary>
///     Service for swarm analytics and reporting.
/// </summary>
public class SwarmAnalyticsService : ISwarmAnalyticsService
{
    private readonly IPeerMetricsService _peerMetricsService;
    private readonly IMultiSourceDownloadService _downloadService;
    private readonly ILogger<SwarmAnalyticsService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SwarmAnalyticsService"/> class.
    /// </summary>
    public SwarmAnalyticsService(
        IPeerMetricsService peerMetricsService,
        IMultiSourceDownloadService downloadService,
        ILogger<SwarmAnalyticsService> logger)
    {
        _peerMetricsService = peerMetricsService;
        _downloadService = downloadService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SwarmAnalyticsDashboard> GetDashboardAsync(
        TimeSpan timeWindow,
        int rankingLimit = 20,
        CancellationToken cancellationToken = default)
    {
        var rankedPeers = await _peerMetricsService
            .GetRankedPeersAsync(100, cancellationToken)
            .ConfigureAwait(false);
        var performanceMetrics = CreatePerformanceMetrics(timeWindow);
        var efficiencyMetrics = CreateEfficiencyMetrics(rankedPeers);
        var peerRankings = CreatePeerRankings(rankedPeers, rankingLimit);
        var recommendationPeers = CreatePeerRankings(rankedPeers, 10);

        return new SwarmAnalyticsDashboard(
            performanceMetrics,
            peerRankings,
            efficiencyMetrics,
            CreateRecommendations(performanceMetrics, efficiencyMetrics, recommendationPeers));
    }

    /// <inheritdoc/>
    public Task<SwarmPerformanceMetrics> GetPerformanceMetricsAsync(TimeSpan? timeWindow = null, CancellationToken cancellationToken = default)
    {
        timeWindow ??= TimeSpan.FromHours(24);

        try
        {
            return Task.FromResult(CreatePerformanceMetrics(timeWindow.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return Task.FromResult(new SwarmPerformanceMetrics { TimeWindow = timeWindow.Value });
        }
    }

    /// <inheritdoc/>
    public async Task<List<PeerPerformanceRanking>> GetPeerRankingsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var rankedPeers = await _peerMetricsService.GetRankedPeersAsync(limit, cancellationToken).ConfigureAwait(false);

            return CreatePeerRankings(rankedPeers, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting peer rankings");
            return new List<PeerPerformanceRanking>();
        }
    }

    /// <inheritdoc/>
    public async Task<SwarmEfficiencyMetrics> GetEfficiencyMetricsAsync(TimeSpan? timeWindow = null, CancellationToken cancellationToken = default)
    {
        timeWindow ??= TimeSpan.FromHours(24);

        try
        {
            var rankedPeers = await _peerMetricsService.GetRankedPeersAsync(100, cancellationToken).ConfigureAwait(false);
            return CreateEfficiencyMetrics(rankedPeers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting efficiency metrics");
            return new SwarmEfficiencyMetrics();
        }
    }

    /// <inheritdoc/>
    public async Task<SwarmTrends> GetTrendsAsync(TimeSpan timeWindow, int dataPoints = 24, CancellationToken cancellationToken = default)
    {
        try
        {
            var trends = new SwarmTrends();
            _logger.LogDebug("Historical trend storage is unavailable; returning empty swarm trends");

            return await Task.FromResult(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends");
            return new SwarmTrends();
        }
    }

    /// <inheritdoc/>
    public async Task<List<SwarmRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var performanceMetrics = CreatePerformanceMetrics(TimeSpan.FromHours(24));
            var rankedPeers = await _peerMetricsService.GetRankedPeersAsync(100, cancellationToken).ConfigureAwait(false);
            return CreateRecommendations(
                performanceMetrics,
                CreateEfficiencyMetrics(rankedPeers),
                CreatePeerRankings(rankedPeers, 10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return new List<SwarmRecommendation>();
        }
    }

    private static SwarmPerformanceMetrics CreatePerformanceMetrics(TimeSpan timeWindow)
    {
        var started = SwarmDownloadsTotal.WithLabels("started").Value;
        var success = SwarmDownloadsTotal.WithLabels("success").Value;
        var failed = SwarmDownloadsTotal.WithLabels("failed").Value;
        var chunksSuccess = SwarmChunksCompletedTotal.WithLabels("success").Value;
        var chunksFailed = SwarmChunksCompletedTotal.WithLabels("failed").Value;
        var chunksTimeout = SwarmChunksCompletedTotal.WithLabels("timeout").Value;
        var chunksCorrupted = SwarmChunksCompletedTotal.WithLabels("corrupted").Value;
        var totalDownloads = (long)(started + success + failed);
        var totalChunks = (long)(chunksSuccess + chunksFailed + chunksTimeout + chunksCorrupted);

        return new SwarmPerformanceMetrics
        {
            TimeWindow = timeWindow,
            SuccessfulDownloads = (long)success,
            FailedDownloads = (long)failed,
            TotalDownloads = totalDownloads,
            SuccessRate = totalDownloads > 0 ? success / totalDownloads : 0,
            TotalBytesDownloaded = (long)SwarmBytesDownloadedTotal.Value,
            TotalChunksCompleted = totalChunks,
            ChunkSuccessRate = totalChunks > 0 ? chunksSuccess / totalChunks : 0,
        };
    }

    private static List<PeerPerformanceRanking> CreatePeerRankings(
        IReadOnlyList<PeerPerformanceMetrics> rankedPeers,
        int limit)
    {
        return rankedPeers
            .Take(limit)
            .Select((peer, index) =>
            {
                var totalChunks = peer.ChunksCompleted + peer.ChunksFailed + peer.ChunksTimedOut + peer.ChunksCorrupted;
                return new PeerPerformanceRanking
                {
                    PeerId = peer.PeerId,
                    Source = peer.Source.ToString().ToLowerInvariant(),
                    ReputationScore = peer.ReputationScore,
                    AverageRttMs = peer.RttAvgMs,
                    AverageThroughputBytesPerSecond = peer.ThroughputAvgBytesPerSec,
                    ChunksCompleted = peer.ChunksCompleted,
                    ChunksFailed = peer.ChunksFailed,
                    ChunkSuccessRate = totalChunks > 0 ? (double)peer.ChunksCompleted / totalChunks : 0,
                    TotalBytesTransferred = peer.TotalBytesTransferred,
                    Rank = index + 1,
                };
            })
            .ToList();
    }

    private SwarmEfficiencyMetrics CreateEfficiencyMetrics(IReadOnlyList<PeerPerformanceMetrics> rankedPeers)
    {
        var metrics = new SwarmEfficiencyMetrics();
        var activeDownloads = _downloadService.ActiveDownloads.Values;
        var totalDownloads = (long)SwarmDownloadsTotal.WithLabels("started").Value;
        if (totalDownloads > 0)
        {
            metrics.ChunkUtilization = Math.Min(1.0, (double)activeDownloads.Count / totalDownloads);
        }

        var downloadsWithChunks = 0;
        long activeWorkerTotal = 0;
        var reassignmentRateTotal = 0.0;
        foreach (var download in activeDownloads)
        {
            if (download.TotalChunks <= 0)
            {
                continue;
            }

            downloadsWithChunks++;
            activeWorkerTotal += download.ActiveWorkers > 0 ? download.ActiveWorkers : 1;
            reassignmentRateTotal += download.PeerTimeouts.Count / (double)download.TotalChunks;
        }

        if (downloadsWithChunks > 0)
        {
            metrics.RedundancyFactor = (double)activeWorkerTotal / downloadsWithChunks;
            metrics.AverageReassignmentRate = reassignmentRateTotal / downloadsWithChunks;
        }

        var activePeers = 0;
        var recentThroughputSamples = 0;
        var recentThroughputDurationMs = 0.0;
        foreach (var peer in rankedPeers)
        {
            if (peer.ChunksCompleted > 0 || peer.ChunksFailed > 0)
            {
                activePeers++;
            }

            foreach (var sample in peer.RecentThroughputSamples)
            {
                if (sample.Duration <= TimeSpan.Zero)
                {
                    continue;
                }

                recentThroughputSamples++;
                recentThroughputDurationMs += sample.Duration.TotalMilliseconds;
            }
        }

        if (rankedPeers.Count > 0)
        {
            metrics.PeerUtilization = (double)activePeers / rankedPeers.Count;
        }

        if (recentThroughputSamples > 0)
        {
            metrics.AverageTimeToFirstByteMs = recentThroughputDurationMs / recentThroughputSamples;
        }

        return metrics;
    }

    private static List<SwarmRecommendation> CreateRecommendations(
        SwarmPerformanceMetrics performanceMetrics,
        SwarmEfficiencyMetrics efficiencyMetrics,
        IReadOnlyList<PeerPerformanceRanking> peerRankings)
    {
        var recommendations = new List<SwarmRecommendation>();
        if (performanceMetrics.SuccessRate < 0.8)
        {
            recommendations.Add(new SwarmRecommendation
            {
                Type = RecommendationType.PeerSelection,
                Priority = RecommendationPriority.High,
                Title = "Low Success Rate",
                Description = $"Current success rate is {performanceMetrics.SuccessRate:P1}. Consider improving peer selection criteria.",
                Action = "Review peer reputation thresholds and increase minimum reputation score for peer selection.",
                EstimatedImpact = 0.3,
            });
        }

        if (performanceMetrics.ChunkSuccessRate < 0.9)
        {
            recommendations.Add(new SwarmRecommendation
            {
                Type = RecommendationType.ChunkSize,
                Priority = RecommendationPriority.Medium,
                Title = "High Chunk Failure Rate",
                Description = $"Chunk success rate is {performanceMetrics.ChunkSuccessRate:P1}. Consider adjusting chunk size.",
                Action = "Try reducing chunk size to improve reliability, or increase timeout values.",
                EstimatedImpact = 0.2,
            });
        }

        if (efficiencyMetrics.PeerUtilization < 0.5)
        {
            recommendations.Add(new SwarmRecommendation
            {
                Type = RecommendationType.SourceCount,
                Priority = RecommendationPriority.Low,
                Title = "Low Peer Utilization",
                Description = $"Only {efficiencyMetrics.PeerUtilization:P1} of available peers are being utilized.",
                Action = "Consider increasing the number of sources per download to improve redundancy.",
                EstimatedImpact = 0.15,
            });
        }

        var lowReputationPeers = peerRankings.Count(p => p.ReputationScore < 0.5);
        if (lowReputationPeers > 0)
        {
            recommendations.Add(new SwarmRecommendation
            {
                Type = RecommendationType.PeerSelection,
                Priority = RecommendationPriority.Medium,
                Title = "Low-Reputation Peers Detected",
                Description = $"{lowReputationPeers} peers have reputation scores below 0.5.",
                Action = "Consider blacklisting or deprioritizing low-reputation peers to improve overall performance.",
                EstimatedImpact = 0.25,
            });
        }

        var speedMbps = performanceMetrics.AverageSpeedBytesPerSecond / (1024.0 * 1024.0);
        if (speedMbps < 0.5)
        {
            recommendations.Add(new SwarmRecommendation
            {
                Type = RecommendationType.NetworkConfig,
                Priority = RecommendationPriority.High,
                Title = "Low Download Speed",
                Description = $"Average download speed is {speedMbps:F2} MB/s. This may indicate network or peer issues.",
                Action = "Check network connectivity, firewall settings, and consider using more sources per download.",
                EstimatedImpact = 0.4,
            });
        }

        return recommendations
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.EstimatedImpact)
            .ToList();
    }
}
