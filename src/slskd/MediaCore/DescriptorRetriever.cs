// <copyright file="DescriptorRetriever.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Mesh.Dht;

namespace slskd.MediaCore;

/// <summary>
/// Content descriptor retrieval service implementation with caching and verification.
/// </summary>
public class DescriptorRetriever : IDescriptorRetriever
{
    private readonly ILogger<DescriptorRetriever> _logger;
    private readonly IMeshDhtClient _dht;
    private readonly IDescriptorValidator _validator;
    private readonly MediaCoreOptions _options;

    // Simple in-memory cache (in production, consider Redis or distributed cache)
    private readonly ConcurrentDictionary<string, CachedDescriptor> _cache = new();
    private readonly ConcurrentDictionary<string, int> _retrievalStats = new();

    private DateTimeOffset _lastCacheCleanup = DateTimeOffset.UtcNow;
    private int _totalRetrievals;
    private int _cacheHits;

    public DescriptorRetriever(
        ILogger<DescriptorRetriever> logger,
        IMeshDhtClient dht,
        IDescriptorValidator validator,
        IOptions<MediaCoreOptions> options)
    {
        _logger = logger;
        _dht = dht;
        _validator = validator;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<DescriptorRetrievalResult> RetrieveAsync(string contentId, bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("ContentId cannot be empty", nameof(contentId));

        contentId = contentId.Trim();
        var startTime = DateTimeOffset.UtcNow;
        Interlocked.Increment(ref _totalRetrievals);

        try
        {
            // Check cache first (unless bypassed)
            if (!bypassCache && _cache.TryGetValue(contentId, out var cached))
            {
                if (!IsExpired(cached))
                {
                    Interlocked.Increment(ref _cacheHits);
                    var cacheVerification = await VerifyAsync(cached.Descriptor, cached.RetrievedAt, cancellationToken);

                    return new DescriptorRetrievalResult(
                        Found: true,
                        Descriptor: cached.Descriptor,
                        RetrievedAt: cached.RetrievedAt,
                        RetrievalDuration: DateTimeOffset.UtcNow - startTime,
                        FromCache: true,
                        Verification: cacheVerification);
                }
                else
                {
                    // Remove expired entry
                    _cache.TryRemove(contentId, out _);
                }
            }

            // Retrieve from DHT
            var key = $"mesh:content:{contentId}";
            var retrievedAt = DateTimeOffset.UtcNow;

            ContentDescriptor? descriptor = null;
            string? errorMessage = null;

            try
            {
                descriptor = await _dht.GetAsync<ContentDescriptor>(key, cancellationToken);

                if (descriptor != null)
                {
                    // Cache the result
                    var cacheEntry = new CachedDescriptor(
                        Descriptor: descriptor,
                        RetrievedAt: retrievedAt,
                        ExpiresAt: retrievedAt + TimeSpan.FromMinutes(_options.MaxTtlMinutes));

                    _cache[contentId] = cacheEntry;

                    // Update domain stats
                    var parsed = ContentIdParser.Parse(contentId);
                    var domain = parsed == null
                        ? "unknown"
                        : ContentIdParser.NormalizeDomain(parsed.Domain, parsed.Type);
                    _retrievalStats.AddOrUpdate(domain, 1, (_, count) => count + 1);
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to retrieve descriptor from DHT";
                _logger.LogWarning(ex, "[DescriptorRetriever] Failed to retrieve {ContentId} from DHT", contentId);
            }

            var verification = descriptor != null
                ? await VerifyAsync(descriptor, retrievedAt, cancellationToken)
                : null;

            var result = new DescriptorRetrievalResult(
                Found: descriptor != null,
                Descriptor: descriptor,
                RetrievedAt: retrievedAt,
                RetrievalDuration: DateTimeOffset.UtcNow - startTime,
                FromCache: false,
                Verification: verification,
                ErrorMessage: errorMessage);

            if (descriptor != null)
            {
                _logger.LogInformation(
                    "[DescriptorRetriever] Retrieved {ContentId} in {Duration}ms (verified: {Verified})",
                    contentId, result.RetrievalDuration.TotalMilliseconds, verification?.IsValid ?? false);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DescriptorRetriever] Error retrieving {ContentId}", contentId);
            return new DescriptorRetrievalResult(
                Found: false,
                Descriptor: null,
                RetrievedAt: DateTimeOffset.UtcNow,
                RetrievalDuration: DateTimeOffset.UtcNow - startTime,
                FromCache: false,
                Verification: null,
                ErrorMessage: "Failed to retrieve descriptor");
        }
    }

    /// <inheritdoc/>
    public async Task<BatchRetrievalResult> RetrieveBatchAsync(IEnumerable<string> contentIds, CancellationToken cancellationToken = default)
    {
        if (contentIds == null)
            throw new ArgumentNullException(nameof(contentIds));

        var contentIdList = contentIds
            .Where(contentId => !string.IsNullOrWhiteSpace(contentId))
            .Select(contentId => contentId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var startTime = DateTimeOffset.UtcNow;
        var results = new List<DescriptorRetrievalResult>();
        var found = 0;
        var failed = 0;

        // Process in parallel with concurrency control
        using var semaphore = new SemaphoreSlim(10); // Allow more concurrent retrievals for batch

        try
        {
            var tasks = contentIdList.Select(async contentId =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await RetrieveAsync(contentId, bypassCache: false, cancellationToken);
                    lock (results)
                    {
                        results.Add(result);
                        if (result.Found) found++;
                        else if (result.ErrorMessage != null) failed++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DescriptorRetriever] Error in batch retrieval");
        }

        var duration = DateTimeOffset.UtcNow - startTime;

        _logger.LogInformation(
            "[DescriptorRetriever] Batch retrieval: {Requested} requested, {Found} found, {Failed} failed in {Duration}",
            contentIdList.Count, found, failed, duration);

        return new BatchRetrievalResult(
            Requested: contentIdList.Count,
            Found: found,
            Failed: failed,
            TotalDuration: duration,
            Results: results);
    }

    /// <inheritdoc/>
    public Task<DescriptorQueryResult> QueryByDomainAsync(string domain, string? type = null, int maxResults = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty", nameof(domain));

        var trimmedDomain = domain.Trim();
        var trimmedType = string.IsNullOrWhiteSpace(type) ? null : type.Trim();

        domain = ContentIdParser.NormalizeDomain(trimmedDomain, trimmedType ?? string.Empty);
        type = trimmedType == null ? null : ContentIdParser.NormalizeType(domain, trimmedType);
        maxResults = Math.Clamp(maxResults, 1, 500);

        var startTime = DateTimeOffset.UtcNow;
        var results = new List<ContentDescriptor>();
        var hasMore = false;

        try
        {
            var newestByContentId = new Dictionary<string, QueryCandidate>(StringComparer.OrdinalIgnoreCase);
            var now = DateTimeOffset.UtcNow;
            var sequence = 0;

            foreach (var entry in _cache)
            {
                var currentSequence = sequence++;
                if (now > entry.Value.ExpiresAt)
                {
                    _cache.TryRemove(entry.Key, out _);
                    continue;
                }

                if (!MatchesDomainAndType(entry.Key, domain, type))
                {
                    continue;
                }

                var candidate = new QueryCandidate(
                    entry.Value.Descriptor,
                    entry.Value.RetrievedAt,
                    currentSequence);
                if (!newestByContentId.TryGetValue(candidate.Descriptor.ContentId, out var existing) ||
                    candidate.RetrievedAt > existing.RetrievedAt)
                {
                    newestByContentId[candidate.Descriptor.ContentId] = candidate;
                }
            }

            var selectionLimit = Math.Min(maxResults + 1, newestByContentId.Count);
            var newest = new PriorityQueue<QueryCandidate, QueryCandidate>(QueryCandidateWorstFirstComparer.Instance);
            foreach (var candidate in newestByContentId.Values)
            {
                if (newest.Count < selectionLimit)
                {
                    newest.Enqueue(candidate, candidate);
                }
                else if (selectionLimit > 0 &&
                    QueryCandidateWorstFirstComparer.Instance.Compare(candidate, newest.Peek()) > 0)
                {
                    newest.Dequeue();
                    newest.Enqueue(candidate, candidate);
                }
            }

            var ordered = new QueryCandidate[newest.Count];
            for (var index = ordered.Length - 1; index >= 0; index--)
            {
                ordered[index] = newest.Dequeue();
            }

            hasMore = ordered.Length > maxResults;
            results = new List<ContentDescriptor>(Math.Min(maxResults, ordered.Length));
            for (var index = 0; index < Math.Min(maxResults, ordered.Length); index++)
            {
                results.Add(ordered[index].Descriptor);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DescriptorRetriever] Error querying domain {Domain}", domain);
        }

        var duration = DateTimeOffset.UtcNow - startTime;

        return Task.FromResult(new DescriptorQueryResult(
            Domain: domain,
            Type: type,
            TotalFound: results.Count,
            QueryDuration: duration,
            Descriptors: results,
            HasMoreResults: hasMore));
    }

    /// <inheritdoc/>
    public async Task<DescriptorVerificationResult> VerifyAsync(ContentDescriptor descriptor, DateTimeOffset retrievedAt, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        var warnings = new List<string>();
        var age = DateTimeOffset.UtcNow - retrievedAt;

        try
        {
            // Basic validation
            var basicValid = _validator.Validate(descriptor, out var reason);
            if (!basicValid)
            {
                return new DescriptorVerificationResult(
                    IsValid: false,
                    SignatureValid: false,
                    FreshnessValid: false,
                    Age: age,
                    ValidationError: reason);
            }

            // Signature verification (simplified)
            var signatureValid = await VerifySignatureAsync(descriptor, cancellationToken);
            if (!signatureValid)
            {
                warnings.Add("Invalid or missing signature");
            }

            // Freshness check (based on signature timestamp if available)
            var freshnessValid = true;
            if (descriptor.Signature?.TimestampUnixMs > 0)
            {
                var signatureTime = DateTimeOffset.FromUnixTimeMilliseconds(descriptor.Signature.TimestampUnixMs);
                var signatureAge = DateTimeOffset.UtcNow - signatureTime;

                // Consider stale if signature is older than max TTL
                if (signatureAge > TimeSpan.FromMinutes(_options.MaxTtlMinutes))
                {
                    freshnessValid = false;
                    warnings.Add($"Signature is stale (age: {signatureAge.TotalHours:F1} hours)");
                }
            }

            // Additional freshness check based on retrieval time
            if (age > TimeSpan.FromMinutes(_options.MaxTtlMinutes))
            {
                freshnessValid = false;
                warnings.Add($"Retrieved data is stale (age: {age.TotalMinutes:F1} minutes)");
            }

            var isValid = signatureValid && freshnessValid;

            return new DescriptorVerificationResult(
                IsValid: isValid,
                SignatureValid: signatureValid,
                FreshnessValid: freshnessValid,
                Age: age,
                Warnings: warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DescriptorRetriever] Error verifying descriptor {ContentId}", descriptor.ContentId);
            return new DescriptorVerificationResult(
                IsValid: false,
                SignatureValid: false,
                FreshnessValid: false,
                Age: age,
                ValidationError: "Descriptor verification failed");
        }
    }

    /// <inheritdoc/>
    public async Task<RetrievalStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        // Perform cache cleanup if needed
        await PerformCacheCleanupAsync(cancellationToken);

        var cacheMisses = _totalRetrievals - _cacheHits;
        var cacheHitRatio = _totalRetrievals > 0 ? (double)_cacheHits / _totalRetrievals : 0;

        var activeEntries = 0;
        long cacheSizeBytes = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in _cache)
        {
            if (now <= entry.Value.ExpiresAt)
            {
                activeEntries++;
                cacheSizeBytes += EstimateDescriptorSize(entry.Value.Descriptor);
            }
        }

        var avgRetrievalTime = TimeSpan.Zero; // Would need to track individual retrieval times

        return new RetrievalStats(
            TotalRetrievals: _totalRetrievals,
            CacheHits: _cacheHits,
            CacheMisses: cacheMisses,
            CacheHitRatio: cacheHitRatio,
            AverageRetrievalTime: avgRetrievalTime,
            ActiveCacheEntries: activeEntries,
            CacheSizeBytes: cacheSizeBytes,
            LastCacheCleanup: _lastCacheCleanup);
    }

    /// <inheritdoc/>
    public Task<CacheOperationResult> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        var entriesCleared = 0;
        long bytesFreed = 0;
        foreach (var entry in _cache)
        {
            entriesCleared++;
            bytesFreed += EstimateDescriptorSize(entry.Value.Descriptor);
        }

        _cache.Clear();
        _lastCacheCleanup = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "[DescriptorRetriever] Cleared cache: {Entries} entries, {Bytes} bytes freed",
            entriesCleared, bytesFreed);

        return Task.FromResult(new CacheOperationResult(
            Success: true,
            EntriesCleared: entriesCleared,
            BytesFreed: bytesFreed));
    }

    private Task<bool> VerifySignatureAsync(ContentDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (descriptor.Signature == null)
            return Task.FromResult(false);

        try
        {
            // In a real implementation, this would verify the cryptographic signature
            // For now, perform a basic sanity check
            if (string.IsNullOrWhiteSpace(descriptor.Signature.PublicKey) ||
                string.IsNullOrWhiteSpace(descriptor.Signature.Signature))
            {
                return Task.FromResult(false);
            }

            // Verify signature matches expected format (hex)
            if (!IsValidHex(descriptor.Signature.Signature))
            {
                return Task.FromResult(false);
            }

            // Verify timestamp is reasonable (not in future, not too old)
            var signatureTime = DateTimeOffset.FromUnixTimeMilliseconds(descriptor.Signature.TimestampUnixMs);
            var now = DateTimeOffset.UtcNow;
            var age = now - signatureTime;

            if (signatureTime > now.AddMinutes(5))
            {
                // Allow 5 minute clock skew; anything beyond that looks forged.
                return Task.FromResult(false);
            }

            if (age > TimeSpan.FromDays(365))
            {
                // Descriptors older than one year are considered stale.
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DescriptorRetriever] Signature verification error for {ContentId}", descriptor.ContentId);
            return Task.FromResult(false);
        }
    }

    private Task PerformCacheCleanupAsync(CancellationToken cancellationToken)
    {
        var expiredCount = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in _cache)
        {
            if (now > entry.Value.ExpiresAt)
            {
                expiredCount++;
                _cache.TryRemove(entry.Key, out _);
            }
        }

        if (expiredCount > 0)
        {
            _lastCacheCleanup = DateTimeOffset.UtcNow;
            _logger.LogInformation("[DescriptorRetriever] Cleaned {Count} expired cache entries", expiredCount);
        }

        return Task.CompletedTask;
    }

    private static bool IsExpired(CachedDescriptor cached)
    {
        return DateTimeOffset.UtcNow > cached.ExpiresAt;
    }

    private static bool IsValidHex(string hex)
    {
        return !string.IsNullOrWhiteSpace(hex) &&
               hex.Length % 2 == 0 &&
               hex.All(c => "0123456789abcdefABCDEF".Contains(c));
    }

    private static bool MatchesDomainAndType(string contentId, string domain, string? type)
    {
        var value = contentId.AsSpan().Trim();
        const string Prefix = "content:";
        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = value[Prefix.Length..];
        var domainSeparator = value.IndexOf(':');
        if (domainSeparator <= 0)
        {
            return false;
        }

        var parsedDomain = value[..domainSeparator];
        value = value[(domainSeparator + 1)..];
        var typeSeparator = value.IndexOf(':');
        if (typeSeparator <= 0 || typeSeparator == value.Length - 1)
        {
            return false;
        }

        var parsedType = value[..typeSeparator];
        var parsedId = value[(typeSeparator + 1)..];
        if (parsedDomain.IsWhiteSpace() || parsedType.IsWhiteSpace() || parsedId.IsWhiteSpace() || parsedId.Contains('\n'))
        {
            return false;
        }

        if (parsedDomain.Equals("mb", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedDomain = parsedType.Equals("recording", StringComparison.OrdinalIgnoreCase) ||
                parsedType.Equals("release", StringComparison.OrdinalIgnoreCase) ||
                parsedType.Equals("artist", StringComparison.OrdinalIgnoreCase)
                    ? ContentDomains.Audio
                    : "mb";
            if (!domain.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (type == null)
            {
                return true;
            }

            var normalizedType = parsedType.Equals("recording", StringComparison.OrdinalIgnoreCase)
                ? ContentDomains.AudioTrack
                : parsedType.Equals("release", StringComparison.OrdinalIgnoreCase)
                    ? ContentDomains.AudioAlbum
                    : parsedType.Equals("artist", StringComparison.OrdinalIgnoreCase)
                        ? ContentDomains.AudioArtist
                        : null;
            return normalizedType == null
                ? parsedType.Equals(type, StringComparison.OrdinalIgnoreCase)
                : type.Equals(normalizedType, StringComparison.OrdinalIgnoreCase);
        }

        return parsedDomain.Equals(domain, StringComparison.OrdinalIgnoreCase) &&
            (type == null || parsedType.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static long EstimateDescriptorSize(ContentDescriptor descriptor)
    {
        // Rough estimation of memory usage
        var size = descriptor.ContentId.Length * 2L; // UTF-16

        if (descriptor.Hashes != null)
        {
            foreach (var hash in descriptor.Hashes)
            {
                size += (hash.Algorithm.Length + hash.Hex.Length) * 2;
            }
        }

        if (descriptor.PerceptualHashes != null)
        {
            foreach (var perceptualHash in descriptor.PerceptualHashes)
            {
                size += (perceptualHash.Algorithm.Length + perceptualHash.Hex.Length) * 2;
            }
        }

        size += (descriptor.Codec?.Length ?? 0) * 2;
        size += 100; // Overhead for object structure

        return size;
    }

    private readonly record struct QueryCandidate(
        ContentDescriptor Descriptor,
        DateTimeOffset RetrievedAt,
        int Sequence);

    private sealed class QueryCandidateWorstFirstComparer : IComparer<QueryCandidate>
    {
        public static QueryCandidateWorstFirstComparer Instance { get; } = new();

        public int Compare(QueryCandidate left, QueryCandidate right)
        {
            var timestampComparison = left.RetrievedAt.CompareTo(right.RetrievedAt);
            return timestampComparison != 0
                ? timestampComparison
                : right.Sequence.CompareTo(left.Sequence);
        }
    }
}

/// <summary>
/// Cached descriptor with metadata.
/// </summary>
internal record CachedDescriptor(
    ContentDescriptor Descriptor,
    DateTimeOffset RetrievedAt,
    DateTimeOffset ExpiresAt);
