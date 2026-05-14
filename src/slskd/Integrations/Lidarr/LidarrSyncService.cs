// <copyright file="LidarrSyncService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.Lidarr;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using slskd.Wishlist;

public interface ILidarrSyncService
{
    LidarrSyncState SyncState { get; }

    Task<LidarrSyncResult> SyncWantedToWishlistAsync(CancellationToken cancellationToken = default);
}

public sealed class LidarrSyncService : BackgroundService, ILidarrSyncService
{
    public LidarrSyncService(
        ILidarrClient lidarrClient,
        IWishlistService wishlistService,
        IOptionsMonitor<global::slskd.Options> optionsMonitor)
    {
        LidarrClient = lidarrClient;
        WishlistService = wishlistService;
        OptionsMonitor = optionsMonitor;
    }

    private ILidarrClient LidarrClient { get; }

    private IWishlistService WishlistService { get; }

    private IOptionsMonitor<global::slskd.Options> OptionsMonitor { get; }

    private ILogger Log { get; } = Serilog.Log.ForContext<LidarrSyncService>();

    public LidarrSyncState SyncState { get; } = new LidarrSyncState();

    public async Task<LidarrSyncResult> SyncWantedToWishlistAsync(CancellationToken cancellationToken = default)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        if (!options.Enabled)
        {
            return new LidarrSyncResult { Enabled = false };
        }

        var existing = await WishlistService.ListAsync().ConfigureAwait(false);
        var existingSearches = existing
            .Select(item => BuildWishlistKey(item.SearchText, item.Filter))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new LidarrSyncResult { Enabled = true };

        const int lidarrPageSize = 250;
        var page = 1;
        var reachedCap = false;

        while (!cancellationToken.IsCancellationRequested && !reachedCap)
        {
            var (records, totalRecords) = await LidarrClient.GetWantedMissingPageAsync(page, lidarrPageSize, cancellationToken).ConfigureAwait(false);

            result.WantedCount = totalRecords;

            if (records.Count == 0)
            {
                break;
            }

            foreach (var album in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var searchText = album.SearchText.Trim();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    result.SkippedCount++;
                    continue;
                }

                var wishlistKey = BuildWishlistKey(searchText, options.WishlistFilter);
                if (existingSearches.Contains(wishlistKey))
                {
                    result.DuplicateCount++;
                    continue;
                }

                var item = new WishlistItem
                {
                    SearchText = searchText,
                    Filter = options.WishlistFilter,
                    Enabled = true,
                    AutoDownload = options.AutoDownload,
                    MaxResults = options.WishlistMaxResults,
                };

                await WishlistService.CreateAsync(item).ConfigureAwait(false);
                existingSearches.Add(wishlistKey);
                result.CreatedCount++;

                if (result.CreatedCount >= options.MaxItemsPerSync)
                {
                    Log.Information("Lidarr sync reached per-cycle cap of {Cap} new items; will continue from page {Page} next cycle", options.MaxItemsPerSync, page);
                    reachedCap = true;
                    break;
                }
            }

            if (page * lidarrPageSize >= result.WantedCount)
            {
                break;
            }

            page++;
        }

        Log.Information(
            "Lidarr wanted sync complete: {Created} created, {Duplicates} duplicates, {Skipped} skipped from {Wanted} wanted albums",
            result.CreatedCount,
            result.DuplicateCount,
            result.SkippedCount,
            result.WantedCount);

        SyncState.LastSyncAt = DateTime.UtcNow;
        SyncState.LastResult = result;
        SyncState.LastError = null;

        return result;
    }

    private static string BuildWishlistKey(string searchText, string filter)
        => searchText.Trim() + "\u001f" + filter.Trim();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
            var delay = TimeSpan.FromSeconds(Math.Max(300, options.SyncIntervalSeconds));

            try
            {
                if (options.Enabled && options.SyncWantedToWishlist)
                {
                    SyncState.IsSyncing = true;
                    await SyncWantedToWishlistAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (IsExpectedExternalHttpFailure(ex))
            {
                Log.Warning("Lidarr wanted sync failed: {Message}", ex.Message);
                SyncState.LastError = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Lidarr wanted sync failed: {Message}", ex.Message);
                SyncState.LastError = ex.Message;
            }
            finally
            {
                SyncState.IsSyncing = false;
            }

            SyncState.NextSyncAt = DateTime.UtcNow.Add(delay);
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }

    internal static bool IsExpectedExternalHttpFailure(Exception ex)
        => ex is HttpRequestException || ex.InnerException is HttpRequestException;
}

public sealed record LidarrSyncResult
{
    public bool Enabled { get; init; }

    public int WantedCount { get; set; }

    public int CreatedCount { get; set; }

    public int DuplicateCount { get; set; }

    public int SkippedCount { get; set; }
}

public sealed class LidarrSyncState
{
    public bool IsSyncing { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public DateTime? NextSyncAt { get; set; }

    public string? LastError { get; set; }

    public LidarrSyncResult? LastResult { get; set; }
}
