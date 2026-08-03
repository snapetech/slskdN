// <copyright file="ShadowIndexDescriptorSource.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using slskd.VirtualSoulfind.ShadowIndex;

namespace slskd.MediaCore;

/// <summary>
/// Content descriptor source backed by shadow index queries (best-effort).
/// </summary>
public class ShadowIndexDescriptorSource : IContentDescriptorSource
{
    private readonly ILogger<ShadowIndexDescriptorSource> logger;
    private readonly IShadowIndexQuery shadowIndex;

    public ShadowIndexDescriptorSource(ILogger<ShadowIndexDescriptorSource> logger, IShadowIndexQuery shadowIndex)
    {
        this.logger = logger;
        this.shadowIndex = shadowIndex;
    }

    public async IAsyncEnumerable<ContentDescriptor> GetDescriptorsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // This source is best-effort: it does not enumerate all content IDs.
        // It provides descriptors only when explicitly asked via PublishForAsync.
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Build a descriptor for a specific contentId (MB recording) using shadow index hints.
    /// </summary>
    public async Task<ContentDescriptor?> BuildForAsync(string contentId, CancellationToken ct = default)
    {
        // Expect format: content:mb:recording:<mbid>
        if (!contentId.StartsWith("content:mb:recording:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var mbid = contentId.Substring("content:mb:recording:".Length);
        try
        {
            var result = await shadowIndex.QueryAsync(mbid, ct);
            if (result == null)
            {
                return null;
            }

            var descriptor = BuildDescriptor(contentId, result);
            if (descriptor == null)
            {
                logger.LogDebug("[MediaCore] ShadowIndex returned no usable variant hints for {ContentId}", contentId);
            }

            return descriptor;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[MediaCore] ShadowIndex lookup failed for {ContentId}", contentId);
            return null;
        }
    }

    private static ContentDescriptor? BuildDescriptor(string contentId, ShadowIndexQueryResult result)
    {
        VariantHint? bestVariant = null;
        var hashCandidates = new List<HashCandidate>();
        var candidateIndexByHash = new Dictionary<byte[], int>(ByteArrayEqualityComparer.Instance);

        for (var index = 0; index < result.CanonicalVariants.Count; index++)
        {
            var variant = result.CanonicalVariants[index];
            if (variant == null)
            {
                continue;
            }

            if (bestVariant == null || CompareRank(variant, bestVariant) > 0)
            {
                bestVariant = variant;
            }

            if (variant.HashPrefix == null || variant.HashPrefix.Length == 0)
            {
                continue;
            }

            var candidate = new HashCandidate(variant.HashPrefix, variant, index);
            if (candidateIndexByHash.TryGetValue(variant.HashPrefix, out var candidateIndex))
            {
                if (CompareRank(variant, hashCandidates[candidateIndex].Variant) > 0)
                {
                    hashCandidates[candidateIndex] = candidate;
                }
            }
            else
            {
                candidateIndexByHash.Add(variant.HashPrefix, hashCandidates.Count);
                hashCandidates.Add(candidate);
            }
        }

        if (bestVariant == null)
        {
            return null;
        }

        hashCandidates.Sort(static (left, right) =>
        {
            var rankComparison = CompareRank(right.Variant, left.Variant);
            return rankComparison != 0
                ? rankComparison
                : left.InputIndex.CompareTo(right.InputIndex);
        });
        var hashes = new List<ContentHash>(hashCandidates.Count);
        foreach (var candidate in hashCandidates)
        {
            hashes.Add(new ContentHash("sha256-prefix16", ToLowerHex(candidate.HashPrefix)));
        }

        var peerContribution = Math.Min(0.15, result.TotalPeerCount * 0.03);
        var qualityContribution = Math.Min(0.18, Math.Max(0.0, bestVariant.QualityScore) * 0.18);
        var confidenceBase = 0.55 + peerContribution + qualityContribution;
        var confidence = Math.Min(0.98, confidenceBase);

        return new ContentDescriptor
        {
            ContentId = contentId,
            Hashes = hashes,
            SizeBytes = bestVariant.SizeBytes > 0 ? bestVariant.SizeBytes : null,
            Codec = string.IsNullOrWhiteSpace(bestVariant.Codec) ? null : bestVariant.Codec,
            BitrateKbps = bestVariant.BitrateKbps > 0 ? bestVariant.BitrateKbps : null,
            Confidence = confidence,
            IsAdvertisable = true,
        };
    }

    private static int CompareRank(VariantHint left, VariantHint right)
    {
        var qualityComparison = Comparer<double>.Default.Compare(left.QualityScore, right.QualityScore);
        return qualityComparison != 0
            ? qualityComparison
            : left.SizeBytes.CompareTo(right.SizeBytes);
    }

    private static string ToLowerHex(byte[] value)
    {
        const string LowerHex = "0123456789abcdef";
        return string.Create(value.Length * 2, value, static (characters, bytes) =>
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                characters[index * 2] = LowerHex[bytes[index] >> 4];
                characters[(index * 2) + 1] = LowerHex[bytes[index] & 0x0F];
            }
        });
    }

    private readonly record struct HashCandidate(byte[] HashPrefix, VariantHint Variant, int InputIndex);

    private sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Instance = new();

        public bool Equals(byte[]? left, byte[]? right)
        {
            return ReferenceEquals(left, right) ||
                (left != null && right != null && left.AsSpan().SequenceEqual(right));
        }

        public int GetHashCode(byte[] value)
        {
            HashCode hashCode = default;
            foreach (var item in value)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }
    }
}
