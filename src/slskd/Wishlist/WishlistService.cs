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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
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
        ///     Gets the newest wishlist item with the exact search text, case-insensitively.
        /// </summary>
        Task<WishlistItem?> FindBySearchTextAsync(string searchText);

        /// <summary>
        ///     Creates a new wishlist item.
        /// </summary>
        Task<WishlistItem> CreateAsync(WishlistItem item);

        /// <summary>
        ///     Creates wishlist items in bounded database batches.
        /// </summary>
        Task<List<WishlistItem>> CreateManyAsync(
            IEnumerable<WishlistItem> items,
            CancellationToken cancellationToken = default);

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
        ///     Gets searches linked to a wishlist item.
        /// </summary>
        Task<List<SlskdSearch>> GetSearchesForItemAsync(Guid wishlistItemId, int limit = 50);

        /// <summary>
        ///     Marks a wishlist item as viewed, updating the last-viewed timestamp.
        /// </summary>
        Task MarkViewedAsync(Guid id);

        /// <summary>
        ///     Marks all wishlist items as viewed.
        /// </summary>
        Task MarkAllViewedAsync();

        Task<List<WishlistIgnoredResult>> ListIgnoredResultsAsync(Guid wishlistItemId);

        Task<WishlistIgnoredResult> IgnoreResultAsync(Guid wishlistItemId, string username, string directory);

        Task DeleteIgnoredResultAsync(Guid wishlistItemId, Guid ignoredResultId);

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
        private const int WishlistInsertBatchSize = 40;

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
        public async Task<WishlistItem?> FindBySearchTextAsync(string searchText)
        {
            searchText = searchText?.Trim() ?? string.Empty;
            if (searchText.Length == 0)
            {
                return null;
            }

            using var context = ContextFactory.CreateDbContext();
            return await context.WishlistItems
                .AsNoTracking()
                .Where(item => EF.Functions.Collate(item.SearchText, "NOCASE") == searchText)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync();
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
        public async Task<List<WishlistItem>> CreateManyAsync(
            IEnumerable<WishlistItem> items,
            CancellationToken cancellationToken = default)
        {
            var created = items.ToList();
            foreach (var item in created)
            {
                item.Id = Guid.NewGuid();
                item.CreatedAt = DateTime.UtcNow;
            }

            if (created.Count == 0)
            {
                return created;
            }

            using var context = ContextFactory.CreateDbContext();
            await InsertWishlistItemsAsync(context, created, cancellationToken).ConfigureAwait(false);
            Log.Information("Created {Count} wishlist items in a batch", created.Count);
            return created;
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
            existing.MaxDownloads = item.MaxDownloads;

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
        public async Task<List<SlskdSearch>> GetSearchesForItemAsync(Guid wishlistItemId, int limit = 50)
        {
            return await SearchService.GetByWishlistItemIdAsync(wishlistItemId, limit);
        }

        /// <inheritdoc/>
        public async Task MarkViewedAsync(Guid id)
        {
            using var context = ContextFactory.CreateDbContext();
            var item = await context.WishlistItems.FindAsync(id);
            if (item == null)
            {
                throw new NotFoundException($"Wishlist item {id} not found");
            }

            item.LastViewedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task MarkAllViewedAsync()
        {
            using var context = ContextFactory.CreateDbContext();
            var now = DateTime.UtcNow;
            await context.WishlistItems
                .Where(i => i.LastViewedAt == null || i.LastSearchedAt > i.LastViewedAt)
                .ExecuteUpdateAsync(updates => updates.SetProperty(item => item.LastViewedAt, now));
        }

        public async Task<List<WishlistIgnoredResult>> ListIgnoredResultsAsync(Guid wishlistItemId)
        {
            using var context = ContextFactory.CreateDbContext();
            if (!await context.WishlistItems.AnyAsync(item => item.Id == wishlistItemId))
            {
                throw new NotFoundException($"Wishlist item {wishlistItemId} not found");
            }

            return await context.WishlistIgnoredResults
                .Where(rule => rule.WishlistItemId == wishlistItemId)
                .OrderByDescending(rule => rule.CreatedAt)
                .ToListAsync();
        }

        public async Task<WishlistIgnoredResult> IgnoreResultAsync(Guid wishlistItemId, string username, string directory)
        {
            using var context = ContextFactory.CreateDbContext();
            if (!await context.WishlistItems.AnyAsync(item => item.Id == wishlistItemId))
            {
                throw new NotFoundException($"Wishlist item {wishlistItemId} not found");
            }

            var normalizedDirectory = NormalizeDirectory(directory);
            var existing = (await context.WishlistIgnoredResults
                    .Where(rule => rule.WishlistItemId == wishlistItemId)
                    .ToListAsync())
                .FirstOrDefault(rule =>
                    rule.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                    rule.Directory.Equals(normalizedDirectory, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return existing;
            }

            var rule = new WishlistIgnoredResult
            {
                WishlistItemId = wishlistItemId,
                Username = username,
                Directory = normalizedDirectory,
            };
            context.WishlistIgnoredResults.Add(rule);
            await context.SaveChangesAsync();
            return rule;
        }

        public async Task DeleteIgnoredResultAsync(Guid wishlistItemId, Guid ignoredResultId)
        {
            using var context = ContextFactory.CreateDbContext();
            var rule = await context.WishlistIgnoredResults.FirstOrDefaultAsync(candidate =>
                candidate.Id == ignoredResultId && candidate.WishlistItemId == wishlistItemId);
            if (rule == null)
            {
                throw new NotFoundException($"Ignored wishlist result {ignoredResultId} not found");
            }

            context.WishlistIgnoredResults.Remove(rule);
            await context.SaveChangesAsync();
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

                result.CreatedItems.Add(item);
                existingKeys.Add(key);
            }

            if (result.CreatedItems.Count > 0)
            {
                await InsertWishlistItemsAsync(context, result.CreatedItems, cancellationToken).ConfigureAwait(false);
            }

            result.CreatedCount = result.CreatedItems.Count;
            Log.Information(
                "Imported {CreatedCount} wishlist searches from CSV ({DuplicateCount} duplicates, {SkippedCount} skipped)",
                result.CreatedCount,
                result.DuplicateCount,
                result.SkippedCount);

            return result;
        }

        private static async Task InsertWishlistItemsAsync(
            WishlistDbContext context,
            IReadOnlyList<WishlistItem> items,
            CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var batch in items.Chunk(WishlistInsertBatchSize))
            {
                var values = batch.Select((_, index) =>
                    $"(@id{index}, @search_text{index}, @filter{index}, @enabled{index}, @auto_download{index}, @max_results{index}, @created_at{index}, @last_searched_at{index}, @last_match_count{index}, @last_visible_hit_count{index}, @last_hidden_locked_hit_count{index}, @last_filtered_out_hit_count{index}, @last_ignored_result_hit_count{index}, @last_response_count{index}, @total_search_count{index}, @total_download_count{index}, @max_downloads{index}, @last_search_id{index}, @last_viewed_at{index})");
                var commandText = $"""
                    INSERT INTO WishlistItems (
                        Id,
                        SearchText,
                        Filter,
                        Enabled,
                        AutoDownload,
                        MaxResults,
                        CreatedAt,
                        LastSearchedAt,
                        LastMatchCount,
                        LastVisibleHitCount,
                        LastHiddenLockedHitCount,
                        LastFilteredOutHitCount,
                        LastIgnoredResultHitCount,
                        LastResponseCount,
                        TotalSearchCount,
                        TotalDownloadCount,
                        MaxDownloads,
                        LastSearchId,
                        LastViewedAt)
                    VALUES {string.Join(", ", values)}
                    """;
                var parameters = new List<object>(batch.Length * 19);

                for (var index = 0; index < batch.Length; index++)
                {
                    var item = batch[index];
                    AddParameter(parameters, $"@id{index}", item.Id);
                    AddParameter(parameters, $"@search_text{index}", item.SearchText);
                    AddParameter(parameters, $"@filter{index}", item.Filter);
                    AddParameter(parameters, $"@enabled{index}", item.Enabled);
                    AddParameter(parameters, $"@auto_download{index}", item.AutoDownload);
                    AddParameter(parameters, $"@max_results{index}", item.MaxResults);
                    AddParameter(parameters, $"@created_at{index}", item.CreatedAt);
                    AddParameter(parameters, $"@last_searched_at{index}", item.LastSearchedAt);
                    AddParameter(parameters, $"@last_match_count{index}", item.LastMatchCount);
                    AddParameter(parameters, $"@last_visible_hit_count{index}", item.LastVisibleHitCount);
                    AddParameter(parameters, $"@last_hidden_locked_hit_count{index}", item.LastHiddenLockedHitCount);
                    AddParameter(parameters, $"@last_filtered_out_hit_count{index}", item.LastFilteredOutHitCount);
                    AddParameter(parameters, $"@last_ignored_result_hit_count{index}", item.LastIgnoredResultHitCount);
                    AddParameter(parameters, $"@last_response_count{index}", item.LastResponseCount);
                    AddParameter(parameters, $"@total_search_count{index}", item.TotalSearchCount);
                    AddParameter(parameters, $"@total_download_count{index}", item.TotalDownloadCount);
                    AddParameter(parameters, $"@max_downloads{index}", item.MaxDownloads);
                    AddParameter(parameters, $"@last_search_id{index}", item.LastSearchId);
                    AddParameter(parameters, $"@last_viewed_at{index}", item.LastViewedAt);
                }

                await context.Database
                    .ExecuteSqlRawAsync(commandText, parameters, cancellationToken)
                    .ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        private static void AddParameter(List<object> parameters, string name, object? value)
        {
            parameters.Add(new SqliteParameter(name, value ?? DBNull.Value));
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
                    await context.Entry(item).ReloadAsync(cancellationToken);
                    if (context.Entry(item).State == EntityState.Detached || !item.Enabled)
                    {
                        continue;
                    }

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

            // Apply the wishlist filter string as a file filter during search collection.
            // Without this, filterResponses: true only enables default filtering (e.g., locked files)
            // but ignores the user's filter expression like "flac OR mp3".
            if (!string.IsNullOrEmpty(item.Filter))
            {
                var fileFilter = CreateFileFilter(item.Filter);
                searchOptions = searchOptions.WithFilters(fileFilter: fileFilter);
            }

            var search = await SearchService.StartAsync(searchId, query, scope, searchOptions, requestedProviders: null, safetySource: "wishlist", wishlistItemId: item.Id);

            // Poll for search completion (up to 20 seconds)
            var maxWait = TimeSpan.FromSeconds(20);
            var pollInterval = TimeSpan.FromSeconds(1);
            var waited = TimeSpan.Zero;
            slskd.Search.Search? searchState = null;

            while (waited < maxWait)
            {
                await Task.Delay(pollInterval, cancellationToken);
                waited += pollInterval;

                searchState = await SearchService.FindAsync(s => s.Id == searchId, includeResponses: false);

                if (searchState?.State.HasFlag(Soulseek.SearchStates.Completed) == true)
                {
                    break;
                }
            }

            // Hydrate the response payload once after polling only the lightweight state projection.
            var searchWithResponses = await SearchService.FindAsync(s => s.Id == searchId, includeResponses: true);

            var ignoredResults = await context.WishlistIgnoredResults
                .Where(rule => rule.WishlistItemId == item.Id)
                .ToListAsync(cancellationToken);

            var hitStats = CountWishlistHits(searchWithResponses, item.Filter, ignoredResults);

            // Update wishlist item stats
            item.LastSearchedAt = DateTime.UtcNow;
            item.LastSearchId = searchId;
            item.TotalSearchCount++;
            item.LastResponseCount = searchWithResponses?.ResponseCount ?? 0;
            item.LastVisibleHitCount = hitStats.Visible;
            item.LastHiddenLockedHitCount = hitStats.HiddenLocked;
            item.LastFilteredOutHitCount = hitStats.FilteredOut;
            item.LastIgnoredResultHitCount = hitStats.Ignored;
            item.LastMatchCount = hitStats.Visible;

            await context.SaveChangesAsync(cancellationToken);

            Log.Information(
                "Wishlist search {Id} completed with {Visible} visible hits ({Responses} responses, {HiddenLocked} locked hidden, {FilteredOut} filtered out)",
                searchId,
                item.LastVisibleHitCount,
                item.LastResponseCount,
                item.LastHiddenLockedHitCount,
                item.LastFilteredOutHitCount);

            // If auto-download is enabled and we have results, download the best ones
            if (item.AutoDownload && searchWithResponses?.Responses?.Any() == true)
            {
                var downloadResult = await AutoDownloadBestResultsAsync(searchWithResponses, item.Filter, ignoredResults, cancellationToken);
                if (downloadResult.EnqueuedCount > 0)
                {
                    item.TotalDownloadCount += downloadResult.EnqueuedCount;

                    // Auto-disable when MaxDownloads limit is reached.
                    // When MaxDownloads is null, disable after the first auto-download (legacy behavior).
                    var shouldDisable = item.MaxDownloads == null
                        ? true
                        : item.TotalDownloadCount >= item.MaxDownloads.Value;

                    if (shouldDisable)
                    {
                        item.Enabled = false;
                        Log.Information(
                            "Wishlist item {Id} disabled after reaching {Count} download(s) (limit: {Limit})",
                            item.Id,
                            item.TotalDownloadCount,
                            item.MaxDownloads.HasValue ? item.MaxDownloads.Value.ToString() : "1 (one-shot)");
                    }
                    else
                    {
                        Log.Information(
                            "Wishlist item {Id} downloaded {Count}/{Limit} file(s); keeping enabled",
                            item.Id,
                            item.TotalDownloadCount,
                            item.MaxDownloads);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    Log.Information(
                        "Wishlist item {Id} enqueued {Count} download(s)",
                        item.Id,
                        downloadResult.EnqueuedCount);
                }
            }

            return searchWithResponses ?? search;
        }

        private async Task<WishlistDownloadResult> AutoDownloadBestResultsAsync(
            SlskdSearch search,
            string filter,
            IReadOnlyCollection<WishlistIgnoredResult> ignoredResults,
            CancellationToken cancellationToken)
        {
            try
            {
                var fileFilter = CreateSearchFileFilter(filter);

                var candidates = new List<SourceCandidate>();
                foreach (var response in search.Responses)
                {
                    foreach (var file in response.Files)
                    {
                        if (!fileFilter(file.Filename) || IsIgnored(ignoredResults, response.Username, file.Filename))
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
                            BitRate = file.BitRate,
                            SampleRate = file.SampleRate,
                            BitDepth = file.BitDepth,
                            Length = file.Length,
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
                    .Select(c => new slskd.Transfers.Downloads.DownloadEnqueueRequest
                    {
                        Filename = c.Filename,
                        Size = c.Size,
                        BitRate = c.BitRate,
                        SampleRate = c.SampleRate,
                        BitDepth = c.BitDepth,
                        Length = c.Length,
                    })
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

        private static WishlistHitStats CountWishlistHits(
            SlskdSearch? search,
            string filter,
            IReadOnlyCollection<WishlistIgnoredResult> ignoredResults)
        {
            if (search?.Responses == null)
            {
                return new WishlistHitStats(0, 0, 0, 0);
            }

            var fileFilter = CreateSearchFileFilter(filter);
            var visible = 0;
            var hiddenLocked = 0;
            var filteredOut = 0;
            var ignored = 0;

            foreach (var response in search.Responses)
            {
                foreach (var file in response.Files)
                {
                    if (!fileFilter(file.Filename))
                    {
                        filteredOut++;
                    }
                    else if (IsIgnored(ignoredResults, response.Username, file.Filename))
                    {
                        ignored++;
                    }
                    else
                    {
                        visible++;
                    }
                }

                foreach (var file in response.LockedFiles)
                {
                    if (!fileFilter(file.Filename))
                    {
                        filteredOut++;
                    }
                    else if (IsIgnored(ignoredResults, response.Username, file.Filename))
                    {
                        ignored++;
                    }
                    else
                    {
                        hiddenLocked++;
                    }
                }
            }

            return new WishlistHitStats(visible, hiddenLocked, filteredOut, ignored);
        }

        internal static bool IsIgnored(
            IReadOnlyCollection<WishlistIgnoredResult> ignoredResults,
            string username,
            string filename)
        {
            var directory = GetParentDirectory(filename);
            return ignoredResults.Any(rule =>
                rule.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                rule.Directory.Equals(directory, StringComparison.OrdinalIgnoreCase));
        }

        internal static string NormalizeDirectory(string directory) =>
            directory.Replace('\\', '/').Trim().TrimEnd('/');

        /// <summary>
        ///     Creates a file filter function from a wishlist filter string. Positive terms match either a file extension
        ///     or any filename/path substring; terms prefixed with '-' exclude filename/path substrings.
        /// </summary>
        private static Func<Soulseek.File, bool> CreateFileFilter(string filter)
        {
            var filenameFilter = CreateSearchFileFilter(filter);
            return file => filenameFilter(file.Filename);
        }

        internal static Func<string, bool> CreateSearchFileFilter(string filter)
        {
            var terms = ParseFilterTerms(filter);
            if (terms.Include.Count == 0 && terms.Exclude.Count == 0)
            {
                return _ => true;
            }

            return filename =>
            {
                var normalizedFilename = filename.Replace('\\', '/').ToLowerInvariant();
                var extension = Path.GetExtension(normalizedFilename).TrimStart('.');

                if (terms.Exclude.Any(term => normalizedFilename.Contains(term, StringComparison.Ordinal)))
                {
                    return false;
                }

                return terms.Include.Count == 0
                    || terms.Include.Any(term =>
                        extension.Equals(term.TrimStart('.'), StringComparison.Ordinal)
                        || normalizedFilename.Contains(term, StringComparison.Ordinal));
            };
        }

        private static WishlistFilterTerms ParseFilterTerms(string filter)
        {
            var include = new HashSet<string>(StringComparer.Ordinal);
            var exclude = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match match in Regex.Matches(filter, "-?\"[^\"]+\"|\\S+"))
            {
                var rawTerm = match.Value;
                if (rawTerm.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isExclude = rawTerm.StartsWith("-", StringComparison.Ordinal);
                var term = (isExclude ? rawTerm[1..] : rawTerm)
                    .Trim()
                    .Trim('"')
                    .ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(term))
                {
                    continue;
                }

                (isExclude ? exclude : include).Add(term.TrimStart('.'));
            }

            return new WishlistFilterTerms(include, exclude);
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

        private readonly record struct WishlistHitStats(int Visible, int HiddenLocked, int FilteredOut, int Ignored);

        private readonly record struct WishlistFilterTerms(HashSet<string> Include, HashSet<string> Exclude);
    }
}
