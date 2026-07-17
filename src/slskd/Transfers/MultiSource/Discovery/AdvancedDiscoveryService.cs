// <copyright file="AdvancedDiscoveryService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.MultiSource.Discovery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Transfers.MultiSource.Metrics;
using slskd.Transfers.MultiSource;

/// <summary>
///     Advanced discovery service with enhanced algorithms and content-aware matching.
/// </summary>
public class AdvancedDiscoveryService : IAdvancedDiscoveryService
{
    private readonly IContentVerificationService _contentVerification;
    private readonly IPeerMetricsService _peerMetrics;
    private readonly ILogger<AdvancedDiscoveryService> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdvancedDiscoveryService"/> class.
    /// </summary>
    public AdvancedDiscoveryService(
        IContentVerificationService contentVerification,
        IPeerMetricsService peerMetrics,
        ILogger<AdvancedDiscoveryService> logger)
    {
        _contentVerification = contentVerification;
        _peerMetrics = peerMetrics;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<DiscoveredPeer>> DiscoverPeersForContentAsync(
        ContentDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[AdvancedDiscovery] Discovering peers for {Filename} ({Size} bytes, domain: {Domain})",
                request.Filename, request.FileSize, request.Domain);

            var discoveredPeers = new List<DiscoveredPeer>();

            // Use content verification service to discover sources
            var verificationResult = await _contentVerification.VerifySourcesAsync(
                new ContentVerificationRequest
                {
                    Filename = request.Filename,
                    FileSize = request.FileSize,
                    CandidateUsernames = new List<string>(), // Would be populated from search
                },
                cancellationToken).ConfigureAwait(false);

            var sources = verificationResult.BestSources ?? new List<VerifiedSource>();
            FilenameSimilarityQuery? filenameSimilarityQuery = null;

            foreach (var source in sources)
            {
                filenameSimilarityQuery ??= PrepareFilenameSimilarityQuery(request.Filename);
                var similarity = CalculateSimilarity(
                    request,
                    filenameSimilarityQuery,
                    source,
                    request.FileSize);
                if (similarity >= request.MinSimilarity)
                {
                    var matchType = DetermineMatchType(request, source, similarity, request.FileSize);
                    var metadataConfidence = CalculateMetadataConfidence(request, source);

                    discoveredPeers.Add(new DiscoveredPeer
                    {
                        PeerId = source.Username,
                        Source = "soulseek", // Would be determined from source
                        Filename = source.FullPath ?? request.Filename,
                        FileSize = request.FileSize, // Use request size since VerifiedSource doesn't have it
                        SimilarityScore = similarity,
                        MatchType = matchType,
                        RecordingId = source.MusicBrainzRecordingId ?? request.RecordingId,
                        MetadataConfidence = metadataConfidence,
                    });
                }
            }

            _logger.LogInformation(
                "[AdvancedDiscovery] Discovered {Count} peers for {Filename}",
                discoveredPeers.Count, request.Filename);

            return discoveredPeers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdvancedDiscovery] Error discovering peers for {Filename}", request.Filename);
            return new List<DiscoveredPeer>();
        }
    }

    /// <inheritdoc/>
    public async Task<List<RankedPeer>> RankPeersAsync(
        List<DiscoveredPeer> peers,
        ContentDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rankedPeers = new List<RankedPeer>();
            var metricsByPeerId = await _peerMetrics.GetMetricsAsync(
                peers.Select(peer => (
                    peer.PeerId,
                    peer.Source == "soulseek" ? PeerSource.Soulseek : PeerSource.Overlay)),
                cancellationToken).ConfigureAwait(false);

            foreach (var peer in peers)
            {
                metricsByPeerId.TryGetValue(peer.PeerId, out var metrics);

                // Calculate performance score
                var performanceScore = CalculatePerformanceScore(metrics);

                // Calculate availability score
                var availabilityScore = CalculateAvailabilityScore(metrics);

                // Calculate overall ranking score
                var rankingScore = CalculateRankingScore(
                    peer,
                    performanceScore,
                    availabilityScore,
                    request);

                rankedPeers.Add(new RankedPeer
                {
                    PeerId = peer.PeerId,
                    Source = peer.Source,
                    Filename = peer.Filename,
                    FileSize = peer.FileSize,
                    SimilarityScore = peer.SimilarityScore,
                    MatchType = peer.MatchType,
                    RecordingId = peer.RecordingId,
                    MetadataConfidence = peer.MetadataConfidence,
                    RankingScore = rankingScore,
                    PerformanceScore = performanceScore,
                    AvailabilityScore = availabilityScore,
                });
            }

            // Sort by ranking score (descending) and assign ranks
            rankedPeers = rankedPeers
                .OrderByDescending(p => p.RankingScore)
                .ToList();

            for (int i = 0; i < rankedPeers.Count; i++)
            {
                rankedPeers[i].Rank = i + 1;
            }

            return rankedPeers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdvancedDiscovery] Error ranking peers");
            return peers.Select(p => new RankedPeer
            {
                PeerId = p.PeerId,
                Source = p.Source,
                Filename = p.Filename,
                FileSize = p.FileSize,
                SimilarityScore = p.SimilarityScore,
                MatchType = p.MatchType,
                RecordingId = p.RecordingId,
                MetadataConfidence = p.MetadataConfidence,
                RankingScore = p.SimilarityScore,
                Rank = 1,
            }).ToList();
        }
    }

    /// <inheritdoc/>
    public Task<List<ContentVariant>> FindSimilarVariantsAsync(
        string filename,
        long fileSize,
        string? recordingId = null,
        CancellationToken cancellationToken = default)
    {
        return FindSimilarVariantsCoreAsync(filename, fileSize, recordingId, cancellationToken);
    }

    private async Task<List<ContentVariant>> FindSimilarVariantsCoreAsync(
        string filename,
        long fileSize,
        string? recordingId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "[AdvancedDiscovery] Finding variants for {Filename} (recording: {RecordingId})",
                filename, recordingId ?? "none");

            var verificationResult = await _contentVerification.VerifySourcesAsync(
                new ContentVerificationRequest
                {
                    Filename = filename,
                    FileSize = fileSize,
                },
                cancellationToken).ConfigureAwait(false);

            var sources = verificationResult.SourcesBySemanticKey.Count > 0
                ? verificationResult.SourcesBySemanticKey.Values.SelectMany(group => group)
                : verificationResult.SourcesByHash.Values.SelectMany(group => group);

            var groups = new List<VariantAggregation>();
            var groupIndexes = new Dictionary<VariantGroupKey, int>();
            foreach (var source in sources)
            {
                if (string.IsNullOrWhiteSpace(source.FullPath))
                {
                    continue;
                }

                var variantFilename = System.IO.Path.GetFileName(source.FullPath);
                var variantRecordingId = source.MusicBrainzRecordingId ?? recordingId;
                var key = new VariantGroupKey(variantFilename, variantRecordingId);
                if (!groupIndexes.TryGetValue(key, out var groupIndex))
                {
                    groupIndex = groups.Count;
                    groupIndexes[key] = groupIndex;
                    groups.Add(new VariantAggregation(
                        variantFilename,
                        variantRecordingId,
                        source.Username,
                        groupIndex));
                    continue;
                }

                var group = groups[groupIndex];
                group.AddPeer(source.Username);
                groups[groupIndex] = group;
            }

            var retainedCount = 0;
            FilenameSimilarityQuery? filenameSimilarityQuery = null;
            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                group.SimilarityScore = !string.IsNullOrWhiteSpace(recordingId) &&
                    string.Equals(group.RecordingId, recordingId, StringComparison.OrdinalIgnoreCase)
                    ? 1.0
                    : CalculateFilenameSimilarity(
                        filenameSimilarityQuery ??= PrepareFilenameSimilarityQuery(filename),
                        group.Filename);
                if (group.SimilarityScore > 0)
                {
                    groups[retainedCount++] = group;
                }
            }

            if (retainedCount < groups.Count)
            {
                groups.RemoveRange(retainedCount, groups.Count - retainedCount);
            }

            groups.Sort(static (left, right) =>
            {
                var similarityOrder = right.SimilarityScore.CompareTo(left.SimilarityScore);
                if (similarityOrder != 0)
                {
                    return similarityOrder;
                }

                var peerCountOrder = right.PeerCount.CompareTo(left.PeerCount);
                return peerCountOrder != 0
                    ? peerCountOrder
                    : left.Sequence.CompareTo(right.Sequence);
            });

            var variants = new List<ContentVariant>(groups.Count);
            foreach (var group in groups)
            {
                variants.Add(new ContentVariant
                {
                    Filename = group.Filename,
                    FileSize = fileSize,
                    RecordingId = group.RecordingId,
                    SimilarityScore = group.SimilarityScore,
                    PeerCount = group.PeerCount,
                });
            }

            return variants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AdvancedDiscovery] Error finding variants for {Filename}", filename);
            return new List<ContentVariant>();
        }
    }

    private double CalculateSimilarity(
        ContentDiscoveryRequest request,
        FilenameSimilarityQuery filenameSimilarityQuery,
        VerifiedSource source,
        long sourceSize)
    {
        var sourceFilename = source.FullPath ?? string.Empty;

        // Filename similarity (Levenshtein distance normalized)
        var filenameSimilarity = CalculateFilenameSimilarity(filenameSimilarityQuery, sourceFilename);

        // Size similarity (within tolerance)
        var sizeTolerance = 0.1; // 10% tolerance
        var sizeDiff = Math.Abs(request.FileSize - sourceSize);
        var sizeSimilarity = sizeDiff <= (request.FileSize * sizeTolerance) ? 1.0 : 0.0;

        // Combined similarity
        return (filenameSimilarity * 0.6) + (sizeSimilarity * 0.4);
    }

    private static FilenameSimilarityQuery PrepareFilenameSimilarityQuery(string filename)
    {
        var normalizedName = System.IO.Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
        var words = normalizedName.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return new FilenameSimilarityQuery(
            normalizedName,
            words.Distinct().ToArray(),
            words.Length);
    }

    private static double CalculateFilenameSimilarity(FilenameSimilarityQuery query, string filename2)
    {
        // Simple similarity based on common substrings
        // In production, could use Levenshtein distance or other algorithms
        var name2 = System.IO.Path.GetFileNameWithoutExtension(filename2).ToLowerInvariant();

        if (query.NormalizedName == name2)
        {
            return 1.0;
        }

        var commonWords = 0;
        foreach (var word in query.UniqueWords)
        {
            if (ContainsFilenameToken(name2, word))
            {
                commonWords++;
            }
        }

        var totalWords = Math.Max(
            query.WordCount,
            CountFilenameTokens(name2));

        return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
    }

    private static int CountFilenameTokens(string value)
    {
        var count = 0;
        var offset = 0;
        while (TryReadFilenameToken(value, ref offset, out _, out _))
        {
            count++;
        }

        return count;
    }

    private static bool ContainsFilenameToken(string value, string token)
    {
        var offset = 0;
        while (TryReadFilenameToken(value, ref offset, out var candidateStart, out var candidateLength))
        {
            if (candidateLength == token.Length &&
                value.AsSpan(candidateStart, candidateLength).SequenceEqual(token.AsSpan()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadFilenameToken(
        string value,
        ref int offset,
        out int tokenStart,
        out int tokenLength)
    {
        while (offset < value.Length && IsFilenameTokenSeparator(value[offset]))
        {
            offset++;
        }

        tokenStart = offset;
        while (offset < value.Length && !IsFilenameTokenSeparator(value[offset]))
        {
            offset++;
        }

        tokenLength = offset - tokenStart;
        return tokenLength > 0;
    }

    private static bool IsFilenameTokenSeparator(char value)
    {
        return value is ' ' or '-' or '_';
    }

    private MatchType DetermineMatchType(ContentDiscoveryRequest request, VerifiedSource source, double similarity, long sourceSize)
    {
        if (similarity >= 0.99 && request.FileSize == sourceSize)
        {
            return MatchType.Exact;
        }

        if (!string.IsNullOrEmpty(request.RecordingId) &&
            !string.IsNullOrEmpty(source.MusicBrainzRecordingId) &&
            source.MusicBrainzRecordingId == request.RecordingId)
        {
            return MatchType.Metadata;
        }

        if (similarity >= 0.8)
        {
            return MatchType.Variant;
        }

        return MatchType.Fuzzy;
    }

    private double CalculateMetadataConfidence(ContentDiscoveryRequest request, VerifiedSource source)
    {
        var confidence = 0.5; // Base confidence

        // Increase confidence if recording ID matches
        if (!string.IsNullOrEmpty(request.RecordingId) &&
            !string.IsNullOrEmpty(source.MusicBrainzRecordingId) &&
            source.MusicBrainzRecordingId == request.RecordingId)
        {
            confidence = 0.9;
        }

        // Increase confidence if fingerprint matches
        if (!string.IsNullOrEmpty(request.Fingerprint) &&
            !string.IsNullOrEmpty(source.AudioFingerprint) &&
            request.Fingerprint == source.AudioFingerprint)
        {
            confidence = Math.Max(confidence, 0.95);
        }

        return confidence;
    }

    private double CalculatePerformanceScore(PeerPerformanceMetrics? metrics)
    {
        if (metrics == null)
        {
            return 0.5; // Neutral score for unknown peers
        }

        // Combine reputation, throughput, and RTT
        var reputationWeight = 0.4;
        var throughputWeight = 0.3;
        var rttWeight = 0.3;

        var reputationScore = metrics.ReputationScore;

        // Normalize throughput (assume 1 MB/s is good)
        var throughputScore = Math.Min(1.0, metrics.ThroughputAvgBytesPerSec / (1024.0 * 1024.0));

        // Normalize RTT (assume 100ms is good, 1000ms is bad)
        var rttScore = Math.Max(0.0, 1.0 - (metrics.RttAvgMs / 1000.0));

        return (reputationScore * reputationWeight) +
               (throughputScore * throughputWeight) +
               (rttScore * rttWeight);
    }

    private double CalculateAvailabilityScore(PeerPerformanceMetrics? metrics)
    {
        if (metrics == null)
        {
            return 0.5; // Neutral score
        }

        // Based on recent activity and failure rate
        var recentActivity = (DateTimeOffset.UtcNow - metrics.LastUpdated).TotalHours < 24 ? 1.0 : 0.5;

        var totalChunks = metrics.ChunksCompleted + metrics.ChunksFailed + metrics.ChunksTimedOut + metrics.ChunksCorrupted;
        var failureRate = totalChunks > 0
            ? (double)(metrics.ChunksFailed + metrics.ChunksTimedOut + metrics.ChunksCorrupted) / totalChunks
            : 0.0;

        var availabilityScore = recentActivity * (1.0 - failureRate);
        return Math.Max(0.0, Math.Min(1.0, availabilityScore));
    }

    private double CalculateRankingScore(
        DiscoveredPeer peer,
        double performanceScore,
        double availabilityScore,
        ContentDiscoveryRequest request)
    {
        // Weighted combination of factors
        var similarityWeight = 0.4;
        var performanceWeight = 0.3;
        var availabilityWeight = 0.2;
        var metadataWeight = 0.1;

        var matchTypeBonus = peer.MatchType switch
        {
            MatchType.Exact => 0.1,
            MatchType.Metadata => 0.08,
            MatchType.Variant => 0.05,
            _ => 0.0
        };

        return (peer.SimilarityScore * similarityWeight) +
               (performanceScore * performanceWeight) +
               (availabilityScore * availabilityWeight) +
               (peer.MetadataConfidence * metadataWeight) +
               matchTypeBonus;
    }

    private readonly record struct VariantGroupKey(string Filename, string? RecordingId);

    private sealed record FilenameSimilarityQuery(
        string NormalizedName,
        string[] UniqueWords,
        int WordCount);

    private struct VariantAggregation
    {
        private readonly string _firstUsername;
        private HashSet<string>? _peerUsernames;

        public VariantAggregation(string filename, string? recordingId, string username, int sequence)
        {
            Filename = filename;
            RecordingId = recordingId;
            _firstUsername = username;
            Sequence = sequence;
            PeerCount = 1;
            SimilarityScore = 0;
        }

        public string Filename { get; }

        public string? RecordingId { get; }

        public int Sequence { get; }

        public int PeerCount { get; private set; }

        public double SimilarityScore { get; set; }

        public void AddPeer(string username)
        {
            if (_peerUsernames == null)
            {
                if (string.Equals(_firstUsername, username, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _peerUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    _firstUsername,
                    username,
                };
                PeerCount = 2;
                return;
            }

            if (_peerUsernames.Add(username))
            {
                PeerCount++;
            }
        }
    }
}
