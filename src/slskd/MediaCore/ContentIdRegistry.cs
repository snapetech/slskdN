// <copyright file="ContentIdRegistry.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace slskd.MediaCore;

/// <summary>
/// In-memory ContentID registry implementation.
/// Thread-safe for concurrent access.
/// </summary>
public class ContentIdRegistry : IContentIdRegistry
{
    private readonly object _mutationLock = new();

    // externalId -> contentId mapping
    private readonly ConcurrentDictionary<string, string> _externalToContent = new();

    // contentId -> set of externalId (values ignored) for reverse lookup; supports remove on overwrite
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _contentToExternal = new();

    private readonly Dictionary<string, int> _mappingCountsByDomain = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _contentIdsByDomain = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _contentIdsByDomainAndType = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task RegisterAsync(string externalId, string contentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID cannot be empty", nameof(externalId));

        if (string.IsNullOrWhiteSpace(contentId))
            throw new ArgumentException("Content ID cannot be empty", nameof(contentId));

        externalId = externalId.Trim();
        contentId = contentId.Trim();

        lock (_mutationLock)
        {
            if (_externalToContent.TryGetValue(externalId, out var oldContentId))
            {
                if (oldContentId == contentId)
                {
                    return Task.CompletedTask;
                }

                var oldSet = _contentToExternal[oldContentId];
                oldSet.TryRemove(externalId, out _);
                var oldContentStillMapped = oldSet.Count > 0;
                if (!oldContentStillMapped)
                {
                    _contentToExternal.TryRemove(oldContentId, out _);
                }

                RemoveFromIndexes(oldContentId, oldContentStillMapped);
            }

            _externalToContent[externalId] = contentId;

            var externalIds = _contentToExternal.GetOrAdd(contentId, _ => new ConcurrentDictionary<string, byte>());
            externalIds.TryAdd(externalId, 0);
            AddToIndexes(contentId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> ResolveAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return Task.FromResult<string?>(null);

        externalId = externalId.Trim();
        _externalToContent.TryGetValue(externalId, out var contentId);
        return Task.FromResult(contentId);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetExternalIdsAsync(string contentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        contentId = contentId.Trim();
        if (_contentToExternal.TryGetValue(contentId, out var externalIds))
        {
            return Task.FromResult<IReadOnlyList<string>>(externalIds.Keys.ToArray());
        }

        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    /// <inheritdoc/>
    public Task<bool> IsRegisteredAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return Task.FromResult(false);

        externalId = externalId.Trim();
        return Task.FromResult(_externalToContent.ContainsKey(externalId));
    }

    /// <inheritdoc/>
    public Task<bool> IsContentIdRegisteredAsync(string contentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            return Task.FromResult(false);

        contentId = contentId.Trim();
        return Task.FromResult(_contentToExternal.ContainsKey(contentId));
    }

    /// <inheritdoc/>
    public Task<ContentIdRegistryStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (_mutationLock)
        {
            var mappingsByDomain = new Dictionary<string, int>(_mappingCountsByDomain, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(new ContentIdRegistryStats(
                TotalMappings: _externalToContent.Count,
                TotalDomains: mappingsByDomain.Count,
                MappingsByDomain: mappingsByDomain));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> FindByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var normalizedDomain = ContentIdParser.NormalizeDomain(domain.Trim(), string.Empty);
        lock (_mutationLock)
        {
            var results = _contentIdsByDomain.TryGetValue(normalizedDomain, out var contentIds)
                ? contentIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<string>();
            return Task.FromResult<IReadOnlyList<string>>(results);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> FindByDomainAndTypeAsync(string domain, string type, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(type))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var normalizedDomain = ContentIdParser.NormalizeDomain(domain.Trim(), type.Trim());
        var normalizedType = ContentIdParser.NormalizeType(domain.Trim(), type.Trim());
        lock (_mutationLock)
        {
            var results = _contentIdsByDomainAndType.TryGetValue(normalizedDomain, out var contentIdsByType)
                && contentIdsByType.TryGetValue(normalizedType, out var contentIds)
                    ? contentIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : Array.Empty<string>();
            return Task.FromResult<IReadOnlyList<string>>(results);
        }
    }

    /// <summary>
    /// Clear all registry data (for testing).
    /// </summary>
    public void Clear()
    {
        lock (_mutationLock)
        {
            _externalToContent.Clear();
            _contentToExternal.Clear();
            _mappingCountsByDomain.Clear();
            _contentIdsByDomain.Clear();
            _contentIdsByDomainAndType.Clear();
        }
    }

    private void AddToIndexes(string contentId)
    {
        var parsed = ContentIdParser.Parse(contentId);
        var domain = parsed == null
            ? "unknown"
            : ContentIdParser.NormalizeDomain(parsed.Domain, parsed.Type);
        _mappingCountsByDomain.TryGetValue(domain, out var count);
        _mappingCountsByDomain[domain] = count + 1;

        if (parsed == null)
        {
            return;
        }

        var type = ContentIdParser.NormalizeType(parsed.Domain, parsed.Type);
        var domainContentIds = GetOrAdd(_contentIdsByDomain, domain);
        domainContentIds.Add(contentId);

        if (!_contentIdsByDomainAndType.TryGetValue(domain, out var contentIdsByType))
        {
            contentIdsByType = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _contentIdsByDomainAndType[domain] = contentIdsByType;
        }

        GetOrAdd(contentIdsByType, type).Add(contentId);
    }

    private void RemoveFromIndexes(string contentId, bool contentIdStillMapped)
    {
        var parsed = ContentIdParser.Parse(contentId);
        var domain = parsed == null
            ? "unknown"
            : ContentIdParser.NormalizeDomain(parsed.Domain, parsed.Type);
        var remainingMappings = _mappingCountsByDomain[domain] - 1;
        if (remainingMappings == 0)
        {
            _mappingCountsByDomain.Remove(domain);
        }
        else
        {
            _mappingCountsByDomain[domain] = remainingMappings;
        }

        if (parsed == null || contentIdStillMapped)
        {
            return;
        }

        if (_contentIdsByDomain.TryGetValue(domain, out var domainContentIds))
        {
            domainContentIds.Remove(contentId);
            if (domainContentIds.Count == 0)
            {
                _contentIdsByDomain.Remove(domain);
            }
        }

        var type = ContentIdParser.NormalizeType(parsed.Domain, parsed.Type);
        if (_contentIdsByDomainAndType.TryGetValue(domain, out var contentIdsByType)
            && contentIdsByType.TryGetValue(type, out var contentIds))
        {
            contentIds.Remove(contentId);
            if (contentIds.Count == 0)
            {
                contentIdsByType.Remove(type);
            }

            if (contentIdsByType.Count == 0)
            {
                _contentIdsByDomainAndType.Remove(domain);
            }
        }
    }

    private static HashSet<string> GetOrAdd(
        Dictionary<string, HashSet<string>> index,
        string key)
    {
        if (!index.TryGetValue(key, out var contentIds))
        {
            contentIds = new HashSet<string>(StringComparer.Ordinal);
            index[key] = contentIds;
        }

        return contentIds;
    }
}
