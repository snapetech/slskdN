// <copyright file="CanonicalStatsService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using slskd.HashDb;

    public class CanonicalStatsService : ICanonicalStatsService
    {
        private const int RecomputeRecordingPageSize = 500;

        private readonly IHashDbService hashDb;
        private readonly ILogger<CanonicalStatsService> log;

        public CanonicalStatsService(IHashDbService hashDb, ILogger<CanonicalStatsService> log)
        {
            this.hashDb = hashDb;
            this.log = log;
        }

        public async Task<CanonicalStats?> AggregateStatsAsync(string recordingId, string codecProfileKey, CancellationToken ct = default)
        {
            var variants = await hashDb.GetVariantsByRecordingAndProfileAsync(recordingId, codecProfileKey, ct).ConfigureAwait(false);
            if (variants == null || variants.Count == 0)
            {
                return null;
            }

            var stats = BuildCanonicalStats(recordingId, codecProfileKey, variants);
            await hashDb.UpsertCanonicalStatsAsync(stats, ct).ConfigureAwait(false);
            return stats;
        }

        public async Task<List<AudioVariant>> GetCanonicalVariantCandidatesAsync(string recordingId, CancellationToken ct = default)
        {
            var variants = await hashDb.GetVariantsByRecordingAsync(recordingId, ct).ConfigureAwait(false);
            if (variants == null || variants.Count == 0)
            {
                return new List<AudioVariant>();
            }

            // Deduplicate across codecs using stream hash or audio sketch + duration bucket
            var deduped = DeduplicateStreams(variants, crossCodec: true);

            var variantsByProfile = variants
                .GroupBy(variant => CodecProfile.FromVariant(variant).ToKey(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            var statsByProfile = (await hashDb
                    .GetCanonicalStatsForRecordingAsync(recordingId, ct)
                    .ConfigureAwait(false))
                .GroupBy(stats => stats.CodecProfileKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var missingStats = new List<CanonicalStats>();
            foreach (var profileKey in deduped
                .Select(variant => CodecProfile.FromVariant(variant).ToKey())
                .Distinct(StringComparer.Ordinal))
            {
                if (!statsByProfile.ContainsKey(profileKey))
                {
                    var computed = BuildCanonicalStats(recordingId, profileKey, variantsByProfile[profileKey]);
                    statsByProfile[profileKey] = computed;
                    missingStats.Add(computed);
                }
            }

            if (missingStats.Count > 0)
            {
                await hashDb.UpsertCanonicalStatsAsync(missingStats, ct).ConfigureAwait(false);
            }

            return deduped
                .OrderByDescending(v => IsLossless(v.Codec))
                .ThenByDescending(v =>
                {
                    var key = CodecProfile.FromVariant(v).ToKey();
                    return statsByProfile.GetValueOrDefault(key)?.CanonicalityScore ?? 0.0;
                })
                .ThenByDescending(v => v.QualityScore)
                .ThenByDescending(v => v.SeenCount)
                .ToList();
        }

        public async Task RecomputeAllStatsAsync(CancellationToken ct = default)
        {
            string? afterRecordingId = null;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var recordingIds = await hashDb
                    .GetRecordingIdsWithVariantsPageAsync(afterRecordingId, RecomputeRecordingPageSize, ct)
                    .ConfigureAwait(false);
                if (recordingIds.Count == 0)
                {
                    break;
                }

                var variants = await hashDb
                    .GetVariantsByRecordingsAsync(recordingIds, ct)
                    .ConfigureAwait(false);
                var variantsByRecording = variants.ToLookup(
                    variant => variant.MusicBrainzRecordingId,
                    StringComparer.Ordinal);
                var stats = recordingIds
                    .SelectMany(recordingId => variantsByRecording[recordingId]
                        .GroupBy(variant => CodecProfile.FromVariant(variant).ToKey(), StringComparer.Ordinal)
                        .Select(group => BuildCanonicalStats(recordingId, group.Key, group.ToList())))
                    .ToList();
                if (stats.Count > 0)
                {
                    await hashDb.UpsertCanonicalStatsAsync(stats, ct).ConfigureAwait(false);
                }

                afterRecordingId = recordingIds[^1];
            }
        }

        internal static CanonicalStats BuildCanonicalStats(
            string recordingId,
            string codecProfileKey,
            List<AudioVariant> variants)
        {
            // Deduplicate identical streams within the profile using codec-specific hashes.
            var distinctVariants = DeduplicateStreams(variants);
            var codecDistribution = new Dictionary<string, int>();
            var bitrateDistribution = new Dictionary<int, int>();
            var sampleRateDistribution = new Dictionary<int, int>();
            var totalSeenCount = 0;
            var totalQualityScore = 0.0;
            var transcodeSuspectCount = 0;
            AudioVariant? bestVariant = null;
            foreach (var variant in distinctVariants)
            {
                totalSeenCount = checked(totalSeenCount + (variant.SeenCount <= 0 ? 1 : variant.SeenCount));
                totalQualityScore += variant.QualityScore;
                if (variant.TranscodeSuspect)
                {
                    transcodeSuspectCount++;
                }

                IncrementDistribution(codecDistribution, variant.Codec ?? "unknown");
                IncrementDistribution(bitrateDistribution, RoundToNearestBitrate(variant.BitrateKbps));
                IncrementDistribution(sampleRateDistribution, variant.SampleRateHz);

                if (bestVariant == null)
                {
                    bestVariant = variant;
                    continue;
                }

                var qualityComparison = Comparer<double>.Default.Compare(variant.QualityScore, bestVariant.QualityScore);
                if (qualityComparison > 0 ||
                    (qualityComparison == 0 && variant.SeenCount > bestVariant.SeenCount))
                {
                    bestVariant = variant;
                }
            }

            var selectedBestVariant = bestVariant!;
            var stats = new CanonicalStats
            {
                Id = $"{recordingId}:{codecProfileKey}",
                MusicBrainzRecordingId = recordingId,
                CodecProfileKey = codecProfileKey,
                VariantCount = distinctVariants.Count,
                TotalSeenCount = totalSeenCount,
                AvgQualityScore = totalQualityScore / distinctVariants.Count,
                MaxQualityScore = selectedBestVariant.QualityScore,
                PercentTranscodeSuspect = (transcodeSuspectCount / (double)distinctVariants.Count) * 100.0,
                CodecDistribution = codecDistribution,
                BitrateDistribution = bitrateDistribution,
                SampleRateDistribution = sampleRateDistribution,
                LastUpdated = DateTimeOffset.UtcNow,
            };

            stats.BestVariantId = selectedBestVariant.VariantId ?? selectedBestVariant.FlacKey;
            stats.CanonicalityScore = ComputeCanonicalityScore(selectedBestVariant, stats);
            return stats;
        }

        private static void IncrementDistribution<TKey>(Dictionary<TKey, int> distribution, TKey key)
            where TKey : notnull
        {
            distribution.TryGetValue(key, out var count);
            distribution[key] = checked(count + 1);
        }

        private static int RoundToNearestBitrate(int bitrate)
        {
            if (bitrate <= 0) return 0;

            // round to nearest 32 kbps bucket
            return (int)(Math.Round(bitrate / 32.0) * 32);
        }

        internal static List<AudioVariant> DeduplicateStreams(List<AudioVariant> variants, bool crossCodec = false)
        {
            var result = new List<AudioVariant>();
            var resultIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var variant in variants)
            {
                var key = BuildDedupKey(variant, crossCodec);
                if (!resultIndexByKey.TryGetValue(key, out var resultIndex))
                {
                    resultIndexByKey.Add(key, result.Count);
                    result.Add(variant);
                    continue;
                }

                var current = result[resultIndex];
                var qualityComparison = Comparer<double>.Default.Compare(variant.QualityScore, current.QualityScore);
                if (qualityComparison > 0 ||
                    (qualityComparison == 0 && variant.SeenCount > current.SeenCount))
                {
                    result[resultIndex] = variant;
                }
            }

            return result;
        }

        private static string BuildDedupKey(AudioVariant v, bool crossCodec)
        {
            var streamHash = v.Codec switch
            {
                "FLAC" => FirstNonEmpty(v.FlacStreamInfoHash42, v.FlacPcmMd5, v.FileSha256),
                "MP3" => FirstNonEmpty(v.Mp3StreamHash, v.FileSha256),
                "Opus" => FirstNonEmpty(v.OpusStreamHash, v.FileSha256),
                "AAC" => FirstNonEmpty(v.AacStreamHash, v.FileSha256),
                _ => FirstNonEmpty(v.FileSha256),
            };

            var sketch = string.IsNullOrWhiteSpace(v.AudioSketchHash) ? "nosketch" : v.AudioSketchHash;
            var durationBucket = RoundDuration(v.DurationMs);
            var codecPart = crossCodec ? string.Empty : (v.Codec ?? "unknown");
            return $"{codecPart}:{streamHash}:{sketch}:{durationBucket}";
        }

        private static string FirstNonEmpty(string? first, string? second = null, string? third = null)
        {
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }

            if (!string.IsNullOrWhiteSpace(second))
            {
                return second;
            }

            return !string.IsNullOrWhiteSpace(third) ? third : string.Empty;
        }

        private static int RoundDuration(int durationMs)
        {
            if (durationMs <= 0)
            {
                return 0;
            }

            const int bucketMs = 1500;
            return (int)(Math.Round(durationMs / (double)bucketMs) * bucketMs);
        }

        private static bool IsLossless(string codec)
        {
            return codec switch
            {
                "FLAC" => true,
                "ALAC" => true,
                "WAV" => true,
                "AIFF" => true,
                _ => false,
            };
        }

        private static double ComputeCanonicalityScore(AudioVariant variant, CanonicalStats stats)
        {
            double score = 0.0;

            // Factor 1: Quality score (0.4 weight)
            score += 0.4 * variant.QualityScore;

            // Factor 2: Prevalence (0.3 weight)
            double prevalence = variant.SeenCount / (double)Math.Max(1, stats.TotalSeenCount);
            score += 0.3 * prevalence;

            // Factor 3: Not transcode suspect (0.2 weight)
            score += variant.TranscodeSuspect ? 0.0 : 0.2;

            // Factor 4: Consensus (0.1 weight) - fewer competing variants increases consensus
            int similarQualityCount = stats.VariantCount;
            double consensus = 1.0 / Math.Log(similarQualityCount + 1);
            score += 0.1 * consensus;

            return Math.Clamp(score, 0.0, 1.0);
        }
    }
}
