// <copyright file="PeerMetricsService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource.Metrics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using slskd.HashDb;
    using slskd.Telemetry;
    using static slskd.Telemetry.PeerMetrics;

    /// <summary>
    ///     Service for tracking per-peer performance metrics with exponential moving averages.
    /// </summary>
    public class PeerMetricsService : IPeerMetricsService
    {
        private readonly ConcurrentDictionary<string, PeerPerformanceMetrics> metricsCache = new();
        private readonly IHashDbService hashDb;
        private readonly ILogger<PeerMetricsService> log;

        // Configuration
        private const int MaxRecentSamples = 30;  // Sliding window size
        private const double EmaAlpha = 0.3;  // Exponential moving average weight (0-1, higher = more weight to recent samples)
        private const double ReputationHalfLifeHours = 24.0; // decay toward neutral (0.5) every 24h
        private const double ReputationWeightSuccess = 0.05;
        private const double ReputationWeightFailed = 0.15;
        private const double ReputationWeightTimedOut = 0.10;
        private const double ReputationWeightCorrupted = 0.20;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerMetricsService"/> class.
        /// </summary>
        public PeerMetricsService(
            IHashDbService hashDb,
            ILogger<PeerMetricsService> log)
        {
            this.hashDb = hashDb;
            this.log = log;
        }

        /// <inheritdoc/>
        public async Task<PeerPerformanceMetrics> GetMetricsAsync(string peerId, PeerSource source, CancellationToken cancellationToken = default)
        {
            return await GetOrCreateMetricsAsync(peerId, source, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<string, PeerPerformanceMetrics?>> GetMetricsAsync(
            IEnumerable<(string PeerId, PeerSource Source)> peers,
            CancellationToken cancellationToken = default)
        {
            var requests = peers
                .GroupBy(peer => peer.PeerId, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            var result = new Dictionary<string, PeerPerformanceMetrics?>(StringComparer.Ordinal);
            var missing = new List<(string PeerId, PeerSource Source)>();
            foreach (var request in requests)
            {
                if (metricsCache.TryGetValue(request.PeerId, out var cached))
                {
                    result[request.PeerId] = cached;
                }
                else
                {
                    missing.Add(request);
                }
            }

            if (missing.Count == 0)
            {
                return result;
            }

            var persisted = await hashDb
                .GetPeerMetricsAsync(missing.Select(request => request.PeerId), cancellationToken)
                .ConfigureAwait(false);
            var persistedByPeerId = persisted.ToDictionary(metrics => metrics.PeerId, StringComparer.Ordinal);
            var now = DateTimeOffset.UtcNow;
            foreach (var request in missing)
            {
                var lookupPeerId = request.PeerId.Trim();
                var metrics = persistedByPeerId.GetValueOrDefault(lookupPeerId) ?? new PeerPerformanceMetrics
                {
                    PeerId = request.PeerId,
                    Source = request.Source,
                    FirstSeen = now,
                    LastUpdated = now,
                    ReputationScore = 0.5,
                    ReputationUpdatedAt = now,
                };
                metricsCache[request.PeerId] = metrics;
                result[request.PeerId] = metrics;
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task RecordRttSampleAsync(string peerId, double rttMs, CancellationToken cancellationToken = default)
        {
            var metrics = await GetOrCreateMetricsAsync(peerId, PeerSource.Soulseek, cancellationToken).ConfigureAwait(false);
            var sourceLabel = metrics.Source.ToString().ToLowerInvariant();
            PeerRttMilliseconds.WithLabels(sourceLabel).Observe(rttMs);

            lock (metrics)
            {
                // Add to recent samples
                metrics.RecentRttSamples.Enqueue(new RttSample
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    RttMs = rttMs,
                });

                // Trim sliding window
                while (metrics.RecentRttSamples.Count > MaxRecentSamples)
                {
                    metrics.RecentRttSamples.Dequeue();
                }

                // Update exponential moving average
                if (metrics.SampleCount == 0)
                {
                    metrics.RttAvgMs = rttMs;
                }
                else
                {
                    metrics.RttAvgMs = (EmaAlpha * rttMs) + ((1 - EmaAlpha) * metrics.RttAvgMs);
                }

                // Compute standard deviation from recent samples
                metrics.RttStdDevMs = ComputeStdDev(metrics.RecentRttSamples, static sample => sample.RttMs);

                metrics.LastRttSample = DateTimeOffset.UtcNow;
                metrics.SampleCount++;
                metrics.LastUpdated = DateTimeOffset.UtcNow;
            }

            await PersistMetricsAsync(metrics, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RecordThroughputSampleAsync(string peerId, long bytesTransferred, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            if (duration.TotalSeconds <= 0)
            {
                return; // Invalid duration
            }

            var metrics = await GetOrCreateMetricsAsync(peerId, PeerSource.Soulseek, cancellationToken).ConfigureAwait(false);
            var sourceLabel = metrics.Source.ToString().ToLowerInvariant();

            double bytesPerSec = bytesTransferred / duration.TotalSeconds;

            // Update Prometheus metrics
            PeerThroughputBytesPerSecond.WithLabels(sourceLabel).Observe(bytesPerSec);
            PeerBytesTransferredTotal.WithLabels(sourceLabel).Inc(bytesTransferred);

            lock (metrics)
            {
                // Add to recent samples
                metrics.RecentThroughputSamples.Enqueue(new ThroughputSample
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    BytesPerSec = bytesPerSec,
                    BytesTransferred = bytesTransferred,
                    Duration = duration,
                });

                // Trim sliding window
                while (metrics.RecentThroughputSamples.Count > MaxRecentSamples)
                {
                    metrics.RecentThroughputSamples.Dequeue();
                }

                // Update EMA
                if (metrics.TotalBytesTransferred == 0)
                {
                    metrics.ThroughputAvgBytesPerSec = bytesPerSec;
                }
                else
                {
                    metrics.ThroughputAvgBytesPerSec = (EmaAlpha * bytesPerSec) + ((1 - EmaAlpha) * metrics.ThroughputAvgBytesPerSec);
                }

                // Compute standard deviation
                metrics.ThroughputStdDevBytesPerSec = ComputeStdDev(
                    metrics.RecentThroughputSamples,
                    static sample => sample.BytesPerSec);

                metrics.TotalBytesTransferred += bytesTransferred;
                metrics.LastThroughputSample = DateTimeOffset.UtcNow;
                metrics.LastUpdated = DateTimeOffset.UtcNow;
            }

            await PersistMetricsAsync(metrics, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RecordChunkCompletionAsync(string peerId, ChunkCompletionResult result, CancellationToken cancellationToken = default)
        {
            var metrics = await GetOrCreateMetricsAsync(peerId, PeerSource.Soulseek, cancellationToken).ConfigureAwait(false);
            var sourceLabel = metrics.Source.ToString().ToLowerInvariant();

            // Update Prometheus metrics
            PeerChunksRequestedTotal.WithLabels(sourceLabel).Inc();

            string statusLabel = result switch
            {
                ChunkCompletionResult.Success => "success",
                ChunkCompletionResult.Failed => "failed",
                ChunkCompletionResult.TimedOut => "timeout",
                ChunkCompletionResult.Corrupted => "corrupted",
                _ => "unknown"
            };
            PeerChunksCompletedTotal.WithLabels(sourceLabel, statusLabel).Inc();

            lock (metrics)
            {
                metrics.ChunksRequested++;

                switch (result)
                {
                    case ChunkCompletionResult.Success:
                        metrics.ChunksCompleted++;
                        UpdateReputation(metrics, result);
                        break;
                    case ChunkCompletionResult.Failed:
                        metrics.ChunksFailed++;
                        UpdateReputation(metrics, result);
                        break;
                    case ChunkCompletionResult.TimedOut:
                        metrics.ChunksTimedOut++;
                        UpdateReputation(metrics, result);
                        break;
                    case ChunkCompletionResult.Corrupted:
                        metrics.ChunksCorrupted++;
                        UpdateReputation(metrics, result);
                        break;
                }

                metrics.LastUpdated = DateTimeOffset.UtcNow;
            }

            await PersistMetricsAsync(metrics, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<PeerPerformanceMetrics>> GetRankedPeersAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return new List<PeerPerformanceMetrics>();
            }

            var topMetrics = await hashDb.GetTopPeerMetricsAsync(limit, cancellationToken).ConfigureAwait(false);

            // Keep the C# cost function authoritative for the returned order. The database query
            // applies the same default formula to avoid hydrating every persisted peer.
            var costFunction = new PeerCostFunction();
            var rankedPeers = costFunction.RankPeers(topMetrics);

            return rankedPeers
                .Select(rp => rp.Metrics)
                .ToList();
        }

        private static double ComputeStdDev<T>(Queue<T> values, Func<T, double> valueSelector)
        {
            var count = 0;
            var mean = 0.0;
            var sumSquaredDifferences = 0.0;
            foreach (var value in values)
            {
                count++;
                var sample = valueSelector(value);
                var difference = sample - mean;
                mean += difference / count;
                sumSquaredDifferences += difference * (sample - mean);
            }

            return count < 2
                ? 0.0
                : Math.Sqrt(sumSquaredDifferences / count);
        }

        private async Task<PeerPerformanceMetrics> GetOrCreateMetricsAsync(string peerId, PeerSource source, CancellationToken ct)
        {
            if (metricsCache.TryGetValue(peerId, out var cached))
            {
                return cached;
            }

            // Load from database or create new
            var metrics = await hashDb.GetPeerMetricsAsync(peerId, ct).ConfigureAwait(false) ?? new PeerPerformanceMetrics
            {
                PeerId = peerId,
                Source = source,
                FirstSeen = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                ReputationScore = 0.5,
                ReputationUpdatedAt = DateTimeOffset.UtcNow,
            };

            metricsCache[peerId] = metrics;
            return metrics;
        }

        private async Task PersistMetricsAsync(PeerPerformanceMetrics metrics, CancellationToken ct)
        {
            try
            {
                await hashDb.UpsertPeerMetricsAsync(metrics, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[PeerMetrics] Failed to persist metrics for peer {PeerId}", metrics.PeerId);
            }
        }

        private void UpdateReputation(PeerPerformanceMetrics metrics, ChunkCompletionResult result)
        {
            var now = DateTimeOffset.UtcNow;

            // Decay toward neutral (0.5) over time
            var last = metrics.ReputationUpdatedAt ?? now;
            var hours = Math.Max(0, (now - last).TotalHours);
            if (hours > 0)
            {
                var decay = Math.Exp(-hours / ReputationHalfLifeHours);
                metrics.ReputationScore = 0.5 + (metrics.ReputationScore - 0.5) * decay;
            }

            double score = metrics.ReputationScore;
            switch (result)
            {
                case ChunkCompletionResult.Success:
                    score += (1 - score) * ReputationWeightSuccess;
                    break;
                case ChunkCompletionResult.Failed:
                    score -= score * ReputationWeightFailed;
                    break;
                case ChunkCompletionResult.TimedOut:
                    score -= score * ReputationWeightTimedOut;
                    break;
                case ChunkCompletionResult.Corrupted:
                    score -= score * ReputationWeightCorrupted;
                    break;
            }

            // Clamp
            score = Math.Max(0.0, Math.Min(1.0, score));

            metrics.ReputationScore = score;
            metrics.ReputationUpdatedAt = now;
        }
    }
}
