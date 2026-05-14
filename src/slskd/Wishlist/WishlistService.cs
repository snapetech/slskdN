// <copyright file="WishlistService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Wishlist
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using slskd.Search;
    using slskd.Transfers.Downloads;
    using slskd.Transfers.Ranking;
    using Soulseek;
    using SlskdSearch = slskd.Search.Search;

    /// <summary>
    ///     Wishlist service interface.
    /// </summary>
    public interface IWishlistService
    {
        /// <summary>
        ///     Gets all wishlist items.
        /// </summary>
        Task<List<WishlistItem>> ListAsync();

        /// <summary>
        ///     Gets a wishlist item by ID.
        /// </summary>
        Task<WishlistItem?> GetAsync(Guid id);

        /// <summary>
        ///     Creates a new wishlist item.
        /// </summary>
        Task<WishlistItem> CreateAsync(WishlistItem item);

        /// <summary>
        ///     Updates an existing wishlist item.
        /// </summary>
        Task<WishlistItem> UpdateAsync(WishlistItem item);

        /// <summary>
        ///     Deletes a wishlist item.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        ///     Manually triggers a search for a wishlist item.
        /// </summary>
        Task<SlskdSearch> RunSearchAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Imports wishlist searches from a CSV playlist export.
        /// </summary>
        Task<WishlistCsvImportResult> ImportCsvAsync(
            string csvText,
            WishlistCsvImportOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Handles wishlist management and background searches.
    /// </summary>
    public class WishlistService : BackgroundService, IWishlistService
    {
        public WishlistService(
            IDbContextFactory<WishlistDbContext> contextFactory,
            ISearchService searchService,
            ISoulseekClient soulseekClient,
            IOptionsMonitor<slskd.Options> optionsMonitor,
            ISourceRankingService rankingService,
            IDownloadService downloadService)
        {
            ContextFactory = contextFactory;
            SearchService = searchService;
            Client = soulseekClient;
            OptionsMonitor = optionsMonitor;
            RankingService = rankingService;
            DownloadService = downloadService;
        }

        private IDbContextFactory<WishlistDbContext> ContextFactory { get; }
        private ISearchService SearchService { get; }
        private ISoulseekClient Client { get; }
        private IOptionsMonitor<slskd.Options> OptionsMonitor { get; }
        private ISourceRankingService RankingService { get; }
        private IDownloadService DownloadService { get; }
        private ILogger Log { get; } = Serilog.Log.ForContext<WishlistService>();

        /// <inheritdoc/>
        public async Task<List<WishlistItem>> ListAsync()
        {
            using var context = ContextFactory.CreateDbContext();
            return await context.WishlistItems
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<WishlistItem?> GetAsync(Guid id)
        {
            using var context = ContextFactory.CreateDbContext();
            return await context.WishlistItems.FindAsync(id);
        }

        /// <inheritdoc/>
        public async Task<WishlistItem> CreateAsync(WishlistItem item)
        {
            using var context = ContextFactory.CreateDbContext();

            item.Id = Guid.NewGuid();
            item.CreatedAt = DateTime.UtcNow;

            context.WishlistItems.Add(item);
            await context.SaveChangesAsync();

            Log.Information("Created wishlist item {Id} for search: {SearchText}", item.Id, item.SearchText);
            return item;
        }

        /// <inheritdoc/>
        public async Task<WishlistItem> UpdateAsync(WishlistItem item)
        {
            using var context = ContextFactory.CreateDbContext();

            var existing = await context.WishlistItems.FindAsync(item.Id);
            if (existing == null)
            {
                throw new NotFoundException($"Wishlist item {item.Id} not found");
            }

            existing.SearchText = item.SearchText;
            existing.Filter = item.Filter;
            existing.Enabled = item.Enabled;
            existing.AutoDownload = item.AutoDownload;
            existing.MaxResults = item.MaxResults;

            await context.SaveChangesAsync();

            Log.Information("Updated wishlist item {Id}", item.Id);
            return existing;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid id)
        {
            using var context = ContextFactory.CreateDbContext();

            var item = await context.WishlistItems.FindAsync(id);
            if (item != null)
            {
                context.WishlistItems.Remove(item);
                await context.SaveChangesAsync();
                Log.Information("Deleted wishlist item {Id}", id);
            }
        }

        /// <inheritdoc/>
        public async Task<SlskdSearch> RunSearchAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var context = ContextFactory.CreateDbContext();

            var item = await context.WishlistItems.FindAsync([id], cancellationToken);
            if (item == null)
            {
                throw new NotFoundException($"Wishlist item {id} not found");
            }

            return await ExecuteWishlistSearchAsync(item, context, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<WishlistCsvImportResult> ImportCsvAsync(
            string csvText,
            WishlistCsvImportOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = new WishlistCsvImportResult();
            var parsed = ParseCsvTracks(csvText, options.IncludeAlbum);

            using var context = ContextFactory.CreateDbContext();
            var existingKeys = (await context.WishlistItems
                    .Select(item => item.SearchText + "\u001f" + item.Filter)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var importKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var track in parsed)
            {
                result.TotalRows++;

                if (string.IsNullOrWhiteSpace(track.SearchText))
                {
                    result.SkippedCount++;
                    result.SkippedRows.Add(new WishlistCsvImportSkippedRow
                    {
                        RowNumber = track.RowNumber,
                        Reason = "Missing artist or track title",
                        RawText = track.RawText,
                    });
                    continue;
                }

                var key = track.SearchText + "\u001f" + options.Filter;
                if (existingKeys.Contains(key) || !importKeys.Add(key))
                {
                    result.DuplicateCount++;
                    continue;
                }

                var item = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    SearchText = track.SearchText,
                    Filter = options.Filter,
                    Enabled = options.Enabled,
                    AutoDownload = options.AutoDownload,
                    MaxResults = options.MaxResults,
                    CreatedAt = DateTime.UtcNow,
                };

                context.WishlistItems.Add(item);
                result.CreatedItems.Add(item);
                existingKeys.Add(key);
            }

            if (result.CreatedItems.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            result.CreatedCount = result.CreatedItems.Count;
            Log.Information(
                "Imported {CreatedCount} wishlist searches from CSV ({DuplicateCount} duplicates, {SkippedCount} skipped)",
                result.CreatedCount,
                result.DuplicateCount,
                result.SkippedCount);

            return result;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
            await Task.Yield();

            Log.Information("Wishlist background service started");

            // Wait for Soulseek to connect before the first run so we don't immediately
            // miss the first interval on a fresh start when the network is still coming up.
            var warmupDeadline = DateTime.UtcNow.AddSeconds(60);
            while (!stoppingToken.IsCancellationRequested
                   && !IsClientSearchReady()
                   && DateTime.UtcNow < warmupDeadline)
            {
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var options = OptionsMonitor.CurrentValue;
                var intervalSeconds = Client.ServerInfo.WishlistInterval ??
                    options.Wishlist?.IntervalSeconds ??
                    3600;

                try
                {
                    if (options.Wishlist?.Enabled == true && IsClientSearchReady())
                    {
                        await ProcessWishlistItemsAsync(stoppingToken);
                    }
                }
                catch (InvalidOperationException ex) when (IsExpectedSearchDeferral(ex))
                {
                    Log.Warning("Deferred wishlist cycle because search is temporarily unavailable: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing wishlist items: {Message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }

            Log.Information("Wishlist background service stopped");
        }

        internal static IReadOnlyList<WishlistCsvTrack> ParseCsvTracks(string csvText, bool includeAlbum)
        {
            var rows = ParseCsvRows(csvText);
            if (rows.Count == 0)
            {
                return [];
            }

            var firstRow = rows[0];
            var hasHeader = LooksLikeHeader(firstRow);
            var header = hasHeader ? firstRow : [];
            var titleIndex = hasHeader ? FindColumn(header, "trackname", "track", "title", "songname", "song", "name") : 0;
            var artistIndex = hasHeader ? FindColumn(header, "artistname", "artistnames", "artists", "artist") : 1;
            var albumIndex = hasHeader ? FindColumn(header, "albumname", "album", "release") : 2;
            var startIndex = hasHeader ? 1 : 0;
            var tracks = new List<WishlistCsvTrack>();

            for (var index = startIndex; index < rows.Count; index++)
            {
                var row = rows[index];
                var title = GetCell(row, titleIndex);
                var artist = GetCell(row, artistIndex);
                var album = GetCell(row, albumIndex);
                var searchText = BuildSearchText(title, artist, includeAlbum ? album : string.Empty);

                tracks.Add(new WishlistCsvTrack
                {
                    RowNumber = index + 1,
                    SearchText = searchText,
                    RawText = string.Join(",", row),
                });
            }

            return tracks;
        }

        private static string BuildSearchText(string title, string artist, string album)
        {
            var parts = new[] { artist, title, album }
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            return parts.Length >= 2 ? string.Join(" ", parts) : string.Empty;
        }

        private static int FindColumn(IReadOnlyList<string> header, params string[] names)
        {
            for (var index = 0; index < header.Count; index++)
            {
                var normalized = NormalizeHeader(header[index]);
                if (names.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string GetCell(IReadOnlyList<string> row, int index)
        {
            return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        }

        private static bool LooksLikeHeader(IReadOnlyList<string> row)
        {
            return row
                .Select(NormalizeHeader)
                .Any(value => value is "trackname" or "track" or "title" or "songname" or "song" or "artistname" or "artist" or "artists" or "albumname" or "album");
        }

        private static string NormalizeHeader(string value)
        {
            var builder = new StringBuilder();
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(char.ToLowerInvariant(ch));
                }
            }

            return builder.ToString();
        }

        private static List<List<string>> ParseCsvRows(string csvText)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < csvText.Length; index++)
            {
                var ch = csvText[index];
                if (ch == '"')
                {
                    if (inQuotes && index + 1 < csvText.Length && csvText[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if ((ch == '\n' || ch == '\r') && !inQuotes)
                {
                    if (ch == '\r' && index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Clear();
                    AddCsvRow(rows, row);
                    row = [];
                }
                else
                {
                    field.Append(ch);
                }
            }

            row.Add(field.ToString());
            AddCsvRow(rows, row);
            return rows;
        }

        private static void AddCsvRow(List<List<string>> rows, List<string> row)
        {
            if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                rows.Add(row);
            }
        }

        private async Task ProcessWishlistItemsAsync(CancellationToken cancellationToken)
        {
            using var context = ContextFactory.CreateDbContext();

            var enabledItems = await context.WishlistItems
                .Where(w => w.Enabled)
                .OrderBy(w => w.LastSearchedAt ?? DateTime.MinValue)
                .ThenBy(w => w.CreatedAt)
                .ToListAsync(cancellationToken);

            Log.Information("Processing {Count} enabled wishlist items", enabledItems.Count);

            foreach (var item in enabledItems)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await ExecuteWishlistSearchAsync(item, context, cancellationToken);

                    // Small delay between searches to avoid hammering the network
                    await Task.Delay(5000, cancellationToken);
                }
                catch (InvalidOperationException ex) when (IsSearchRateLimitExceeded(ex))
                {
                    Log.Warning("Stopping wishlist cycle early because the Soulseek search safety budget is exhausted");
                    break;
                }
                catch (InvalidOperationException ex) when (IsSearchUnavailableDuringLogin(ex))
                {
                    Log.Warning("Stopping wishlist cycle early because Soulseek is still logging in: {Message}", ex.Message);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug("Wishlist cycle cancelled during shutdown");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error executing wishlist search for {Id}: {Message}", item.Id, ex.Message);
                }
            }
        }

        private async Task<SlskdSearch> ExecuteWishlistSearchAsync(
            WishlistItem item,
            WishlistDbContext context,
            CancellationToken cancellationToken)
        {
            Log.Information("Executing wishlist search: {SearchText}", item.SearchText);

            var searchId = Guid.NewGuid();
            var query = new SearchQuery(item.SearchText);
            var scope = SearchScope.Network;

            var searchOptions = new SearchOptions(
                searchTimeout: 15000,
                responseLimit: item.MaxResults,
                filterResponses: !string.IsNullOrEmpty(item.Filter));

            var search = await SearchService.StartAsync(searchId, query, scope, searchOptions, requestedProviders: null, safetySource: "wishlist");

            // Poll for search completion (up to 20 seconds)
            var maxWait = TimeSpan.FromSeconds(20);
            var pollInterval = TimeSpan.FromMilliseconds(500);
            var waited = TimeSpan.Zero;
            slskd.Search.Search? searchWithResponses = null;

            while (waited < maxWait)
            {
                await Task.Delay(pollInterval, cancellationToken);
                waited += pollInterval;

                searchWithResponses = await SearchService.FindAsync(s => s.Id == searchId, includeResponses: true);

                if (searchWithResponses?.State.HasFlag(Soulseek.SearchStates.Completed) == true)
                {
                    break;
                }
            }

            // If we timed out, get the final state anyway
            searchWithResponses ??= await SearchService.FindAsync(s => s.Id == searchId, includeResponses: true);

            // Update wishlist item stats
            item.LastSearchedAt = DateTime.UtcNow;
            item.LastSearchId = searchId;
            item.TotalSearchCount++;
            item.LastMatchCount = searchWithResponses?.ResponseCount ?? 0;

            context.WishlistItems.Update(item);
            await context.SaveChangesAsync(cancellationToken);

            Log.Information("Wishlist search {Id} completed with {Count} responses", searchId, item.LastMatchCount);

            // If auto-download is enabled and we have results, download the best ones
            if (item.AutoDownload && searchWithResponses?.Responses?.Any() == true)
            {
                var downloadResult = await AutoDownloadBestResultsAsync(searchWithResponses, item.Filter, cancellationToken);
                if (downloadResult.EnqueuedCount > 0)
                {
                    item.TotalDownloadCount += downloadResult.EnqueuedCount;
                    item.Enabled = false;
                    context.WishlistItems.Update(item);
                    await context.SaveChangesAsync(cancellationToken);
                    Log.Information(
                        "Wishlist item {Id} disabled after enqueuing {Count} download(s)",
                        item.Id,
                        downloadResult.EnqueuedCount);
                }
            }

            return searchWithResponses ?? search;
        }

        private async Task<WishlistDownloadResult> AutoDownloadBestResultsAsync(SlskdSearch search, string filter, CancellationToken cancellationToken)
        {
            try
            {
                var wantedExt = string.IsNullOrWhiteSpace(filter)
                    ? null
                    : "." + filter.TrimStart('.').ToLowerInvariant();

                var candidates = new List<SourceCandidate>();
                foreach (var response in search.Responses)
                {
                    foreach (var file in response.Files)
                    {
                        if (wantedExt != null &&
                            !Path.GetExtension(file.Filename).Equals(wantedExt, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        candidates.Add(new SourceCandidate
                        {
                            Username = response.Username,
                            Filename = file.Filename,
                            Size = file.Size,
                            HasFreeUploadSlot = response.HasFreeUploadSlot,
                            QueueLength = (int)response.QueueLength,
                            UploadSpeed = response.UploadSpeed,
                        });
                    }
                }

                if (candidates.Count == 0)
                {
                    return WishlistDownloadResult.Empty;
                }

                // Group by (user, directory) so we can download a complete album at once.
                // Rank one representative file per group (peer-level stats are the same for all).
                var groups = candidates
                    .GroupBy(c => (c.Username, Dir: GetParentDirectory(c.Filename)))
                    .ToList();

                var representatives = groups.Select(g => g.First()).ToList();
                var ranked = await RankingService.RankSourcesAsync(representatives, cancellationToken);

                var bestRep = ranked.FirstOrDefault();
                if (bestRep == null)
                {
                    return WishlistDownloadResult.Empty;
                }

                var bestDir = GetParentDirectory(bestRep.Filename);
                var filesToDownload = groups
                    .First(g => g.Key.Username == bestRep.Username && g.Key.Dir == bestDir)
                    .Select(c => (c.Filename, c.Size))
                    .ToList();

                Log.Information(
                    "Auto-downloading {Count} file(s) from {Username} in {Directory} (score: {Score:F1})",
                    filesToDownload.Count,
                    bestRep.Username,
                    bestDir,
                    bestRep.SmartScore);

                var (enqueued, failed) = await DownloadService.EnqueueAsync(bestRep.Username, filesToDownload, cancellationToken);
                if (failed.Count > 0)
                {
                    Log.Warning(
                        "Wishlist auto-download could not enqueue {FailedCount}/{RequestedCount} file(s) from {Username}",
                        failed.Count,
                        filesToDownload.Count,
                        bestRep.Username);
                }

                return new WishlistDownloadResult(enqueued.Count, failed.Count);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error auto-downloading wishlist results: {Message}", ex.Message);
                return WishlistDownloadResult.Empty;
            }
        }

        private static string GetParentDirectory(string filename)
        {
            var normalized = filename.Replace('\\', '/');
            var lastSlash = normalized.LastIndexOf('/');
            return lastSlash < 0 ? string.Empty : normalized[..lastSlash];
        }

        private bool IsClientSearchReady()
        {
            return Client.State.HasFlag(SoulseekClientStates.Connected)
                && Client.State.HasFlag(SoulseekClientStates.LoggedIn);
        }

        private static bool IsExpectedSearchDeferral(Exception exception)
        {
            return IsSearchRateLimitExceeded(exception) || IsSearchUnavailableDuringLogin(exception);
        }

        private static bool IsSearchRateLimitExceeded(Exception exception)
        {
            return exception.Message.Contains("Search rate limit exceeded", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSearchUnavailableDuringLogin(Exception exception)
        {
            return exception.Message.Contains("must be connected and logged in", StringComparison.OrdinalIgnoreCase)
                && exception.Message.Contains("LoggingIn", StringComparison.OrdinalIgnoreCase);
        }

        private readonly record struct WishlistDownloadResult(int EnqueuedCount, int FailedCount)
        {
            public static WishlistDownloadResult Empty { get; } = new(0, 0);
        }
    }
}
