// <copyright file="ShadowIndexQueryImpl.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.ShadowIndex;

using slskd.VirtualSoulfind.Capture;
using slskd.Mesh.Dht;

/// <summary>
/// Interface for querying shadow index via DHT.
/// </summary>
public interface IShadowIndexQuery
{
    /// <summary>
    /// Resolve MBID to peer hints and canonical variants.
    /// </summary>
    Task<ShadowIndexQueryResult?> QueryAsync(string mbid, CancellationToken ct = default);

    /// <summary>
    /// Resolve multiple MBIDs at once.
    /// </summary>
    Task<Dictionary<string, ShadowIndexQueryResult>> QueryBatchAsync(
        List<string> mbids,
        CancellationToken ct = default);
}

/// <summary>
/// Query result from shadow index.
/// </summary>
public class ShadowIndexQueryResult
{
    public string MBID { get; set; } = string.Empty;
    public List<string> PeerIds { get; set; } = new();
    public List<VariantHint> CanonicalVariants { get; set; } = new();
    public int TotalPeerCount { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}

/// <summary>
/// Queries shadow index from DHT and merges multiple shards.
/// </summary>
public class ShadowIndexQuery : IShadowIndexQuery
{
    private readonly ILogger<ShadowIndexQuery> logger;
    private readonly IMeshDhtClient dht;
    private readonly IShardMerger merger;
    private readonly IShardCache cache;
    private readonly IUsernamePseudonymizer pseudonymizer;

    public ShadowIndexQuery(
        ILogger<ShadowIndexQuery> logger,
        IMeshDhtClient dht,
        IShardMerger merger,
        IShardCache cache,
        IUsernamePseudonymizer pseudonymizer)
    {
        this.logger = logger;
        this.dht = dht;
        this.merger = merger;
        this.cache = cache;
        this.pseudonymizer = pseudonymizer;
    }

    public async Task<ShadowIndexQueryResult?> QueryAsync(string mbid, CancellationToken ct)
    {
        logger.LogDebug("[VSF-QUERY] Querying shadow index for {MBID}", mbid);

        // Check cache first
        var cachedShard = await cache.GetAsync(mbid, ct);
        if (cachedShard != null)
        {
            return ConvertShardToResult(mbid, cachedShard);
        }

        var key = DhtKeyDerivation.DeriveRecordingKey(mbid);
        var shardDataList = await dht.GetMultipleAsync(key, ct);

        if (shardDataList.Count == 0)
        {
            logger.LogDebug("[VSF-QUERY] No shards found for {MBID}", mbid);
            return null;
        }

        // Deserialize shards
        var shards = shardDataList
            .Select(ShardSerializer.Deserialize)
            .Where(s => s != null)
            .ToList();

        if (shards.Count == 0)
        {
            logger.LogWarning("[VSF-QUERY] Failed to deserialize any shards for {MBID}", mbid);
            return null;
        }

        // Merge shards from multiple peers
        var mergedShard = await merger.MergeShardsAsync(shards!, ct);

        // Cache merged shard
        await cache.SetAsync(mbid, mergedShard, TimeSpan.FromMinutes(10), ct);

        logger.LogInformation("[VSF-QUERY] Resolved {MBID}: {PeerCount} peers, {VariantCount} variants",
            mbid, mergedShard.ApproximatePeerCount, mergedShard.CanonicalVariants.Count);

        return ConvertShardToResult(mbid, mergedShard);
    }

    public async Task<Dictionary<string, ShadowIndexQueryResult>> QueryBatchAsync(
        List<string> mbids,
        CancellationToken ct)
    {
        var results = new Dictionary<string, ShadowIndexQueryResult>();

        foreach (var mbid in mbids)
        {
            var result = await QueryAsync(mbid, ct);
            if (result != null)
            {
                results[mbid] = result;
            }
        }

        return results;
    }

    private ShadowIndexQueryResult ConvertShardToResult(string mbid, ShadowIndexShard shard)
    {
        return new ShadowIndexQueryResult
        {
            MBID = mbid,
            PeerIds = shard.PeerIdHints
                .Select(hint => DecodePeerIdHint(hint))
                .Where(id => id != null)
                .ToList()!,
            CanonicalVariants = shard.CanonicalVariants,
            TotalPeerCount = shard.ApproximatePeerCount,
            LastUpdated = shard.Timestamp
        };
    }

    private string? DecodePeerIdHint(byte[] hint)
    {
        // Legacy eight-byte pseudonyms are not routable. New publishers may place
        // a bounded UTF-8 peer identity in the same MessagePack binary slot.
        if (hint.Length <= 8 || hint.Length > 256)
            return null;

        try
        {
            var peerId = new System.Text.UTF8Encoding(false, true).GetString(hint);
            return string.IsNullOrWhiteSpace(peerId) || peerId.Any(char.IsControl)
                ? null
                : peerId;
        }
        catch (System.Text.DecoderFallbackException)
        {
            return null;
        }
    }
}
