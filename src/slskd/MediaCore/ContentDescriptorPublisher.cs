// <copyright file="ContentDescriptorPublisher.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace slskd.MediaCore;

/// <summary>
/// Advanced content descriptor publishing service with versioning and batch operations.
/// </summary>
public class ContentDescriptorPublisher : IContentDescriptorPublisher
{
    private readonly ILogger<ContentDescriptorPublisher> _logger;
    private readonly IDescriptorPublisher _basePublisher;
    private readonly IContentIdRegistry _registry;
    private readonly MediaCoreOptions _options;

    // Track published descriptors for statistics and management
    private readonly ConcurrentDictionary<string, PublishedDescriptorInfo> _publishedDescriptors = new();

    public ContentDescriptorPublisher(
        ILogger<ContentDescriptorPublisher> logger,
        IDescriptorPublisher basePublisher,
        IContentIdRegistry registry,
        IOptions<MediaCoreOptions> options)
    {
        _logger = logger;
        _basePublisher = basePublisher;
        _registry = registry;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<DescriptorPublishResult> PublishAsync(ContentDescriptor descriptor, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        // H-MCP01: Check if content is advertisable before publishing to network
        if (descriptor.IsAdvertisable == false)
        {
            _logger.LogWarning(
                "[ContentDescriptorPublisher] Blocked publication of non-advertisable content {ContentId}",
                descriptor.ContentId);

            return new DescriptorPublishResult(
                Success: false,
                ContentId: descriptor.ContentId,
                Version: "0",
                PublishedAt: DateTimeOffset.UtcNow,
                Ttl: TimeSpan.Zero,
                ErrorMessage: "Content is not advertisable",
                WasUpdated: false,
                PreviousVersion: null);
        }

        var startTime = DateTimeOffset.UtcNow;
        var version = GenerateVersion(descriptor);
        var ttl = TimeSpan.FromMinutes(Math.Min(_options.MaxTtlMinutes, 60)); // Cap at 1 hour

        try
        {
            // Check if already published and handle versioning
            if (_publishedDescriptors.TryGetValue(descriptor.ContentId, out var existingInfo))
            {
                if (!forceUpdate && !IsNewerVersion(version, existingInfo.Version))
                {
                    return new DescriptorPublishResult(
                        Success: false,
                        ContentId: descriptor.ContentId,
                        Version: version,
                        PublishedAt: startTime,
                        Ttl: ttl,
                        ErrorMessage: $"Version {version} is not newer than existing {existingInfo.Version}",
                        WasUpdated: false,
                        PreviousVersion: existingInfo.Version);
                }
            }

            if (descriptor.Signature == null)
            {
                return new DescriptorPublishResult(
                    Success: false,
                    ContentId: descriptor.ContentId,
                    Version: version,
                    PublishedAt: startTime,
                    Ttl: ttl,
                    ErrorMessage: "Descriptor signature is required; provide a signed descriptor before publishing.",
                    WasUpdated: false,
                    PreviousVersion: existingInfo?.Version);
            }

            // Publish using base publisher
            var success = await _basePublisher.PublishAsync(descriptor, cancellationToken);

            if (success)
            {
                // Update tracking
                var info = new PublishedDescriptorInfo(
                    ContentId: descriptor.ContentId,
                    Version: version,
                    PublishedAt: startTime,
                    ExpiresAt: startTime + ttl,
                    SizeBytes: descriptor.SizeBytes ?? 0);

                _publishedDescriptors[descriptor.ContentId] = info;

                _logger.LogInformation(
                    "[ContentDescriptorPublisher] Published {ContentId} v{Version} (ttl={TtlMinutes}min)",
                    descriptor.ContentId, version, ttl.TotalMinutes);

                return new DescriptorPublishResult(
                    Success: true,
                    ContentId: descriptor.ContentId,
                    Version: version,
                    PublishedAt: startTime,
                    Ttl: ttl,
                    WasUpdated: existingInfo != null,
                    PreviousVersion: existingInfo?.Version);
            }
            else
            {
                return new DescriptorPublishResult(
                    Success: false,
                    ContentId: descriptor.ContentId,
                    Version: version,
                    PublishedAt: startTime,
                    Ttl: ttl,
                    ErrorMessage: "Base publisher failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentDescriptorPublisher] Failed to publish {ContentId}", descriptor.ContentId);
            return new DescriptorPublishResult(
                Success: false,
                ContentId: descriptor.ContentId,
                Version: version,
                PublishedAt: startTime,
                Ttl: ttl,
                ErrorMessage: "Failed to publish descriptor");
        }
    }

    /// <inheritdoc/>
    public async Task<BatchPublishResult> PublishBatchAsync(IEnumerable<ContentDescriptor> descriptors, CancellationToken cancellationToken = default)
    {
        if (descriptors == null)
            throw new ArgumentNullException(nameof(descriptors));

        var descriptorList = descriptors.ToList();
        var startTime = DateTimeOffset.UtcNow;
        var results = new List<DescriptorPublishResult>(descriptorList.Count);
        var successfullyPublished = 0;
        var failedToPublish = 0;
        var skipped = 0;
        var nextIndex = -1;

        const int MaxConcurrency = 5;
        var workers = new Task[Math.Min(MaxConcurrency, descriptorList.Count)];
        for (var workerIndex = 0; workerIndex < workers.Length; workerIndex++)
        {
            workers[workerIndex] = ProcessBatchAsync();
        }

        await Task.WhenAll(workers);

        async Task ProcessBatchAsync()
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var index = Interlocked.Increment(ref nextIndex);
                if (index >= descriptorList.Count)
                {
                    return;
                }

                var descriptor = descriptorList[index];
                var result = await PublishAsync(descriptor, forceUpdate: false, cancellationToken);
                lock (results)
                {
                    results.Add(result);
                    if (result.Success)
                    {
                        successfullyPublished++;
                    }
                    else if (result.ErrorMessage?.Contains("not newer") == true)
                    {
                        skipped++;
                    }
                    else
                    {
                        failedToPublish++;
                    }
                }
            }
        }

        var duration = DateTimeOffset.UtcNow - startTime;

        return new BatchPublishResult(
            TotalRequested: descriptorList.Count,
            SuccessfullyPublished: successfullyPublished,
            FailedToPublish: failedToPublish,
            Skipped: skipped,
            TotalDuration: duration,
            Results: results);
    }

    /// <inheritdoc/>
    public Task<DescriptorUpdateResult> UpdateAsync(string contentId, DescriptorUpdates updates, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("ContentId cannot be empty", nameof(contentId));

        if (updates == null)
            throw new ArgumentNullException(nameof(updates));

        try
        {
            var previousVersion = _publishedDescriptors.TryGetValue(contentId, out var info) ? info.Version : "unknown";
            _logger.LogWarning("[ContentDescriptorPublisher] Update requested for {ContentId}, but descriptor update/republish is unavailable through this publisher", contentId);
            return Task.FromResult(new DescriptorUpdateResult(
                Success: false,
                ContentId: contentId,
                NewVersion: previousVersion,
                PreviousVersion: previousVersion,
                AppliedUpdates: Array.Empty<string>(),
                ErrorMessage: "Descriptor update/republish is unavailable through this publisher."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentDescriptorPublisher] Failed to update {ContentId}", contentId);
            return Task.FromResult(new DescriptorUpdateResult(
                Success: false,
                ContentId: contentId,
                NewVersion: "error",
                PreviousVersion: "unknown",
                AppliedUpdates: Array.Empty<string>(),
                ErrorMessage: "Failed to update descriptor"));
        }
    }

    /// <inheritdoc/>
    public Task<RepublishResult> RepublishExpiringAsync(IEnumerable<string>? contentIds = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var checkedCount = 0;
        var republished = 0;
        var failed = 0;
        var stillValid = 0;

        var targetIds = contentIds?.ToList() ?? _publishedDescriptors.Keys.ToList();
        var expiringThreshold = DateTimeOffset.UtcNow.AddMinutes(30); // Republish if expires within 30 min

        foreach (var contentId in targetIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            checkedCount++;

            if (_publishedDescriptors.TryGetValue(contentId, out var info))
            {
                if (info.ExpiresAt <= expiringThreshold)
                {
                    _logger.LogWarning(
                        "[ContentDescriptorPublisher] Descriptor {ContentId} is expiring at {Expiry}, but republish is unavailable through this publisher",
                        contentId, info.ExpiresAt);
                    failed++;
                }
                else
                {
                    stillValid++;
                }
            }
        }

        var duration = DateTimeOffset.UtcNow - startTime;

        return Task.FromResult(new RepublishResult(
            TotalChecked: checkedCount,
            Republished: republished,
            Failed: failed,
            StillValid: stillValid,
            Duration: duration));
    }

    /// <inheritdoc/>
    public Task<UnpublishResult> UnpublishAsync(string contentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("ContentId cannot be empty", nameof(contentId));

        try
        {
            var wasPublished = _publishedDescriptors.TryRemove(contentId, out _);

            // In a real implementation, this would need to expire/remove from DHT
            // DHT entries typically expire naturally, so this mainly updates local tracking
            _logger.LogInformation(
                "[ContentDescriptorPublisher] Unpublished {ContentId} (was published: {WasPublished})",
                contentId, wasPublished);

            return Task.FromResult(new UnpublishResult(
                Success: true,
                ContentId: contentId,
                WasPublished: wasPublished));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentDescriptorPublisher] Failed to unpublish {ContentId}", contentId);
            return Task.FromResult(new UnpublishResult(
                Success: false,
                ContentId: contentId,
                WasPublished: false,
                ErrorMessage: "Failed to unpublish descriptor"));
        }
    }

    /// <inheritdoc/>
    public Task<PublishingStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var publicationsByDomain = new Dictionary<string, int>();
        var totalStorageBytes = 0L;
        var totalTtlHours = 0.0;
        var activeCount = 0;
        var expiringSoonCount = 0;
        var lastPublish = DateTimeOffset.MinValue;

        foreach (var (contentId, info) in _publishedDescriptors)
        {
            if (info.ExpiresAt > now)
            {
                activeCount++;

                // Expires within 1 hour.
                if (info.ExpiresAt <= now.AddMinutes(60))
                {
                    expiringSoonCount++;
                }

                // Parse domain from ContentID
                var domain = ContentIdParser.GetDomain(contentId) ?? "unknown";
                publicationsByDomain.TryGetValue(domain, out var count);
                publicationsByDomain[domain] = count + 1;

                totalStorageBytes += info.SizeBytes;
                totalTtlHours += (info.ExpiresAt - now).TotalHours;

                if (info.PublishedAt > lastPublish)
                {
                    lastPublish = info.PublishedAt;
                }
            }
        }

        var averageTtlHours = activeCount > 0 ? totalTtlHours / activeCount : 0;

        return Task.FromResult(new PublishingStats(
            TotalPublishedDescriptors: _publishedDescriptors.Count,
            ActivePublications: activeCount,
            ExpiringSoon: expiringSoonCount,
            LastPublishOperation: lastPublish,
            PublicationsByDomain: publicationsByDomain,
            TotalStorageBytes: totalStorageBytes,
            AverageTtlHours: averageTtlHours));
    }

    internal static string GenerateVersion(ContentDescriptor descriptor)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendUtf8(incrementalHash, descriptor.ContentId);
        incrementalHash.AppendData(":"u8);
        AppendUtf8(incrementalHash, descriptor.Codec ?? string.Empty);
        incrementalHash.AppendData(":"u8);
        if (descriptor.BitrateKbps.HasValue)
        {
            AppendUtf8(incrementalHash, descriptor.BitrateKbps.Value.ToString(CultureInfo.InvariantCulture));
            incrementalHash.AppendData(":"u8);
        }

        Span<char> sizeChars = stackalloc char[32];
        if (descriptor.SizeBytes.HasValue)
        {
            if (descriptor.SizeBytes.Value.TryFormat(
                sizeChars,
                out var sizeLength,
                provider: CultureInfo.CurrentCulture))
            {
                AppendUtf8(incrementalHash, sizeChars[..sizeLength]);
            }
            else
            {
                AppendUtf8(incrementalHash, descriptor.SizeBytes.Value.ToString(CultureInfo.CurrentCulture));
            }
        }

        Span<byte> hash = stackalloc byte[32];
        incrementalHash.TryGetHashAndReset(hash, out _);
        const string LowerHex = "0123456789abcdef";
        Span<char> versionHash = stackalloc char[8];
        for (var index = 0; index < versionHash.Length / 2; index++)
        {
            versionHash[index * 2] = LowerHex[hash[index] >> 4];
            versionHash[(index * 2) + 1] = LowerHex[hash[index] & 0x0F];
        }

        Span<char> version = stackalloc char[32];
        if (!timestamp.TryFormat(version, out var timestampLength, provider: CultureInfo.CurrentCulture))
        {
            return $"{timestamp.ToString(CultureInfo.CurrentCulture)}-{new string(versionHash)}";
        }

        version[timestampLength] = '-';
        versionHash.CopyTo(version[(timestampLength + 1)..]);

        return new string(version[..(timestampLength + 1 + versionHash.Length)]);
    }

    private static void AppendUtf8(IncrementalHash hash, ReadOnlySpan<char> value)
    {
        const int CharacterChunkSize = 1024;
        Span<byte> utf8 = stackalloc byte[CharacterChunkSize * 3];
        var offset = 0;

        while (offset < value.Length)
        {
            var characterCount = Math.Min(CharacterChunkSize, value.Length - offset);
            if (offset + characterCount < value.Length &&
                char.IsHighSurrogate(value[offset + characterCount - 1]) &&
                char.IsLowSurrogate(value[offset + characterCount]))
            {
                characterCount--;
            }

            var byteCount = Encoding.UTF8.GetBytes(value.Slice(offset, characterCount), utf8);
            hash.AppendData(utf8[..byteCount]);
            offset += characterCount;
        }
    }

    private static bool IsNewerVersion(string newVersion, string existingVersion)
    {
        // Simple version comparison - in practice, this might be more sophisticated
        return string.CompareOrdinal(newVersion, existingVersion) > 0;
    }
}

/// <summary>
/// Information about a published descriptor.
/// </summary>
internal record PublishedDescriptorInfo(
    string ContentId,
    string Version,
    DateTimeOffset PublishedAt,
    DateTimeOffset ExpiresAt,
    long SizeBytes);
