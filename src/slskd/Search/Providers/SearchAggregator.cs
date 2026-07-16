// <copyright file="SearchAggregator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search.Providers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using slskd.Search;
using ILogger = Serilog.ILogger;

/// <summary>
///     Aggregates search results from multiple providers (Pod and Scene) with deduplication.
/// </summary>
public class SearchAggregator
{
    private const int MaxInitialResultCapacity = 4096;

    private readonly ILogger _logger;
    private readonly string _preferredPrimarySource; // "pod" or "scene", default "pod"

    public SearchAggregator(ILogger logger, string preferredPrimarySource = "pod")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _preferredPrimarySource = preferredPrimarySource;
    }

    /// <summary>
    ///     Merges search results from multiple providers with deduplication.
    /// </summary>
    /// <param name="results">All search results from all providers.</param>
    /// <returns>Merged and deduplicated results.</returns>
    public List<SearchResult> MergeResults(IEnumerable<SearchResult> results)
    {
        var hasInputCount = results.TryGetNonEnumeratedCount(out var inputCountHint);
        if (hasInputCount && inputCountHint == 0)
        {
            return new List<SearchResult>();
        }

        var initialCapacity = inputCountHint <= MaxInitialResultCapacity
            ? inputCountHint
            : 0;

        var seenByAsciiFilename = new Dictionary<(string Filename, long Size), SearchResult>(
            initialCapacity,
            AsciiFilenameKeyComparer.Instance);
        Dictionary<(string Filename, long Size), SearchResult>? seenByUnicodeFilename = null;
        var merged = new List<SearchResult>(initialCapacity);
        var inputCount = 0;

        // Deduplicate by response (username + first file) rather than individual files
        // This matches the existing SearchResponseMerger behavior
        foreach (var result in results)
        {
            inputCount++;

            if (result.Response?.Files == null || !result.Response.Files.Any())
            {
                continue;
            }

            // Use first file for deduplication key
            // For cross-provider deduplication, we ignore username (pod and scene may have different usernames)
            // and match on normalized filename + size only
            var firstFile = result.Response.Files.First();
            var (filename, size, isAscii) = CreateFilenameKey(firstFile.Filename, firstFile.Size);
            var seenByFilename = isAscii
                ? seenByAsciiFilename
                : seenByUnicodeFilename ??= new Dictionary<(string Filename, long Size), SearchResult>();
            var key = (filename, size);

            if (seenByFilename.TryGetValue(key, out var existingResult))
            {
                // Merge: add provider to SourceProviders, update PrimarySource if preferred
                if (!existingResult.SourceProviders.Contains(result.Provider))
                {
                    existingResult.SourceProviders.Add(result.Provider);
                }

                // Prefer pod as primary source if available
                if (result.Provider == _preferredPrimarySource && existingResult.PrimarySource != _preferredPrimarySource)
                {
                    existingResult.PrimarySource = _preferredPrimarySource;
                }

                // Merge ContentRefs (keep both if different)
                if (result.PodContentRef != null && existingResult.PodContentRef == null)
                {
                    existingResult.PodContentRef = result.PodContentRef;
                    existingResult.PeerHint = result.PeerHint;
                }

                if (result.SceneContentRef != null && existingResult.SceneContentRef == null)
                {
                    existingResult.SceneContentRef = result.SceneContentRef;
                    existingResult.SceneUserHint = result.SceneUserHint;
                }
            }
            else
            {
                // New result - add to merged list
                seenByFilename[key] = result;
                merged.Add(result);
            }
        }

        // Update PrimarySource for merged results
        foreach (var result in merged)
        {
            if (result.SourceProviders.Count > 1)
            {
                // Multiple sources - prefer configured primary source if available
                if (result.SourceProviders.Contains(_preferredPrimarySource))
                {
                    result.PrimarySource = _preferredPrimarySource;
                }
                else
                {
                    result.PrimarySource = result.SourceProviders.First();
                }
            }
            else if (result.SourceProviders.Count == 1)
            {
                // Single source - use that as primary
                result.PrimarySource = result.SourceProviders.First();
            }
        }

        _logger.Debug("[SearchAggregator] Merged {InputCount} results into {OutputCount} unique results", inputCount, merged.Count);

        return merged;
    }

    private static (string Filename, long Size, bool IsAscii) CreateFilenameKey(string? filename, long size)
    {
        var normalized = (filename ?? string.Empty).Replace('\\', '/').Trim();
        var isAscii = IsAscii(normalized);
        if (!isAscii)
        {
            normalized = normalized.ToLowerInvariant();
            isAscii = IsAscii(normalized);
        }

        return (normalized, size, isAscii);
    }

    private static bool IsAscii(string value)
    {
        foreach (var character in value)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class AsciiFilenameKeyComparer : IEqualityComparer<(string Filename, long Size)>
    {
        public static AsciiFilenameKeyComparer Instance { get; } = new();

        public bool Equals((string Filename, long Size) x, (string Filename, long Size) y)
        {
            return x.Size == y.Size && string.Equals(x.Filename, y.Filename, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Filename, long Size) obj)
        {
            var filenameHash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Filename);
            return HashCode.Combine(obj.Size, filenameHash);
        }
    }

    /// <summary>
    ///     Starts searches from multiple providers in parallel and aggregates results.
    /// </summary>
    /// <param name="providers">The search providers to use.</param>
    /// <param name="request">The search request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated search results.</returns>
    public async Task<List<SearchResult>> AggregateAsync(
        IEnumerable<ISearchProvider> providers,
        SearchRequest request,
        CancellationToken ct)
    {
        var providersList = providers.ToList();
        if (providersList.Count == 0)
        {
            return new List<SearchResult>();
        }

        var sink = new CollectingSearchResultSink();
        var tasks = providersList.Select(provider => RunProviderSearchAsync(provider, request, sink, ct));

        await Task.WhenAll(tasks);

        return MergeResults(sink.Results);
    }

    private async Task RunProviderSearchAsync(
        ISearchProvider provider,
        SearchRequest request,
        CollectingSearchResultSink sink,
        CancellationToken ct)
    {
        try
        {
            await provider.StartSearchAsync(request, sink, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "[SearchAggregator] Provider {Provider} failed: {Message}", provider.Name, ex.Message);
        }
    }

    /// <summary>
    ///     Simple collecting sink that stores all results.
    /// </summary>
    private class CollectingSearchResultSink : ISearchResultSink
    {
        private readonly List<SearchResult> _results = new();

        public void AddResult(SearchResult result)
        {
            // Multiple providers run concurrently via Task.WhenAll; lock to prevent
            // concurrent List<T> mutations from corrupting the results list.
            lock (_results)
            {
                _results.Add(result);
            }
        }

        public List<SearchResult> Results => _results;
    }
}
