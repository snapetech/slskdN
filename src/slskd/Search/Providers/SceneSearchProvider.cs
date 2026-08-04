// <copyright file="SceneSearchProvider.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search.Providers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Common.Security;
using slskd.Search;
using Soulseek;
using SearchOptions = Soulseek.SearchOptions;
using SearchQuery = Soulseek.SearchQuery;
using SearchScope = Soulseek.SearchScope;

/// <summary>
///     Search provider for Soulseek Scene (wraps existing Soulseek search logic).
/// </summary>
public class SceneSearchProvider : ISearchProvider
{
    private readonly ISoulseekClient _soulseekClient;
    private readonly ISoulseekSafetyLimiter _safetyLimiter;
    private readonly ILogger<SceneSearchProvider> _logger;

    public SceneSearchProvider(
        ISoulseekClient soulseekClient,
        ISoulseekSafetyLimiter safetyLimiter,
        ILogger<SceneSearchProvider> logger)
    {
        _soulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
        _safetyLimiter = safetyLimiter ?? throw new ArgumentNullException(nameof(safetyLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "scene";

    public async Task StartSearchAsync(SearchRequest request, ISearchResultSink sink, CancellationToken ct)
    {
        // H-08: Check Soulseek safety caps before initiating search
        if (!_safetyLimiter.TryConsumeSearch("scene-provider"))
        {
            _logger.LogWarning("[SceneProvider] Search rejected for query='{Query}': rate limit exceeded", request.SearchText);
            return; // Silently fail - don't block pod provider
        }

        try
        {
            var scope = SearchScope.Network;
            var responses = new List<Soulseek.SearchResponse>();
            var timeoutMilliseconds = Math.Max(1, request.TimeoutSeconds ?? 15) * 1000;
            var responseLimit = request.ResponseLimit ?? 100;
            var fileLimit = request.FileLimit ?? 10000;
            var queryTexts = request.AllowSmartSoulseekFallback
                ? new[] { request.SearchText }.Concat(SmartSearchFallback.CreateQueries(request.SearchText))
                : new[] { request.SearchText };

            foreach (var (queryText, queryIndex) in queryTexts.Select((text, index) => (text, index)))
            {
                if (queryIndex > 0 && !_safetyLimiter.TryConsumeSearch("scene-provider"))
                {
                    _logger.LogDebug(
                        "[SceneProvider] Smart Wishlist fallback stopped by the Soulseek safety limiter for '{Query}'",
                        request.SearchText);
                    break;
                }

                var query = SearchQuery.FromText(queryText);
                var queryResponses = new List<Soulseek.SearchResponse>();
                var queryTimeout = queryIndex == 0
                    ? timeoutMilliseconds
                    : Math.Min(timeoutMilliseconds, SmartSearchFallback.FallbackTimeoutMilliseconds);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(queryTimeout);

                try
                {
                    await _soulseekClient.SearchAsync(
                        query,
                        responseHandler: response =>
                        {
                            queryResponses.Add(response);
                            responses.Add(response);
                        },
                        scope,
                        token: _soulseekClient.GetNextToken(),
                        options: new SearchOptions(
                            searchTimeout: queryTimeout,
                            responseLimit: responseLimit,
                            fileLimit: fileLimit,
                            filterResponses: true,
                            minimumResponseFileCount: 1),
                        cancellationToken: timeoutCts.Token);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("[SceneProvider] Search timed out for '{Query}'", queryText);
                }

                if (!SmartSearchFallback.NeedsFallback(
                        queryResponses.Count,
                        queryResponses.Sum(response => response.FileCount + response.LockedFileCount),
                        responseLimit,
                        fileLimit))
                {
                    break;
                }
            }

            // Convert Soulseek responses to SearchResult with provenance
            // Group files by response to create one SearchResult per response
            foreach (var response in responses)
            {
                var firstFile = response.Files.FirstOrDefault();
                if (firstFile == null)
                {
                    continue;
                }

                var responseObj = Response.FromSoulseekSearchResponse(response);

                // Attach provenance to Response
                responseObj.SourceProviders = new List<string> { "scene" };
                responseObj.PrimarySource = "scene";
                responseObj.SceneContentRef = new SceneContentRef
                {
                    Username = response.Username,
                    Filename = firstFile.Filename,
                    Size = firstFile.Size
                };

                var searchResult = new SearchResult
                {
                    Provider = "scene",
                    SourceProviders = new List<string> { "scene" },
                    PrimarySource = "scene",
                    Response = responseObj,
                    SceneContentRef = responseObj.SceneContentRef,
                    SceneUserHint = response.Username
                };

                sink.AddResult(searchResult);
            }

            _logger.LogDebug("[SceneProvider] Search completed for '{Query}': {Count} responses", request.SearchText, responses.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("[SceneProvider] Search cancelled for '{Query}'", request.SearchText);
            throw;
        }
        catch (Exception ex)
        {
            // Don't block pod provider - log and continue
            _logger.LogDebug(ex, "[SceneProvider] Search failed for '{Query}': {Message}", request.SearchText, ex.Message);
        }
    }
}
