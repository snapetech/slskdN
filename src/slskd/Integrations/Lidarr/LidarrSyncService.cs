// <copyright file="LidarrSyncService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.Lidarr;

using System;
using System.Collections.Generic;
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
        var trackedAlbums = existing
            .Where(item => item.LidarrAlbumId.HasValue && !item.LidarrTrackId.HasValue)
            .GroupBy(item => item.LidarrAlbumId!.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var trackedTracks = existing
            .Where(item => item.LidarrAlbumId.HasValue && item.LidarrTrackId.HasValue)
            .GroupBy(item => item.LidarrTrackId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var result = new LidarrSyncResult { Enabled = true };
        var qualityProfiles = new Dictionary<int, LidarrQualityProfile>();
        var qualityProfilesLoaded = false;
        var seenTrackIds = new HashSet<int>();

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

            var pendingItems = new List<WishlistItem>();
            foreach (var album in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var searchText = album.SearchText.Trim();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    result.SkippedCount++;
                    continue;
                }

                var qualityProfileId = album.ProfileId > 0
                    ? album.ProfileId
                    : album.Artist?.QualityProfileId ?? 0;
                if (qualityProfileId > 0 && !qualityProfilesLoaded)
                {
                    qualityProfiles = (await LidarrClient.GetQualityProfilesAsync(cancellationToken).ConfigureAwait(false))
                        .Where(profile => profile.Id > 0)
                        .GroupBy(profile => profile.Id)
                        .ToDictionary(group => group.Key, group => group.First());
                    qualityProfilesLoaded = true;
                }

                var filter = options.WishlistFilter;
                if (qualityProfiles.TryGetValue(qualityProfileId, out var qualityProfile))
                {
                    var qualityFilter = BuildQualityFilter(qualityProfile);
                    if (!string.IsNullOrWhiteSpace(qualityFilter))
                    {
                        filter = qualityFilter;
                    }
                }

                if (album.Id > 0 && !trackedAlbums.ContainsKey(album.Id))
                {
                    var legacyAlbum = FindLegacyAlbumItem(existing, searchText, options.WishlistFilter);
                    if (legacyAlbum != null)
                    {
                        trackedAlbums[album.Id] = legacyAlbum;
                    }
                }

                var targets = new List<(string SearchText, int? TrackId)>();
                var expandedPartialAlbum = IsPartiallyMissing(album) && album.Id > 0;
                if (expandedPartialAlbum)
                {
                    var tracks = await LidarrClient.GetAlbumTracksAsync(album.Id, cancellationToken).ConfigureAwait(false);
                    var missingTracks = tracks
                        .Where(IsMissingTrack)
                        .ToList();

                    if (missingTracks.Count > 0)
                    {
                        if (trackedAlbums.TryGetValue(album.Id, out var albumItem) && albumItem.Enabled)
                        {
                            await UpdateTrackedItemAsync(
                                albumItem,
                                searchText,
                                filter,
                                options,
                                album.Id,
                                trackId: null,
                                isTrack: false,
                                enabled: false).ConfigureAwait(false);
                        }

                        var missingTrackIds = missingTracks
                            .Where(track => track.Id > 0)
                            .Select(track => track.Id)
                            .ToHashSet();
                        foreach (var trackedItem in trackedTracks.Values
                            .Where(item => item.LidarrAlbumId == album.Id && item.LidarrTrackId.HasValue && !missingTrackIds.Contains(item.LidarrTrackId.Value))
                            .ToList())
                        {
                            if (trackedItem.Enabled)
                            {
                                trackedItem.Enabled = false;
                                await WishlistService.UpdateAsync(trackedItem).ConfigureAwait(false);
                            }
                        }

                        foreach (var track in missingTracks)
                        {
                            if (track.Id <= 0 || string.IsNullOrWhiteSpace(track.Title) || !seenTrackIds.Add(track.Id))
                            {
                                continue;
                            }

                            var trackSearchText = BuildTrackSearchText(album, track);
                            if (!string.IsNullOrWhiteSpace(trackSearchText))
                            {
                                targets.Add((trackSearchText, track.Id));
                            }
                        }
                    }
                    else if (tracks.Count > 0)
                    {
                        if (trackedAlbums.TryGetValue(album.Id, out var albumItem) && albumItem.Enabled)
                        {
                            await UpdateTrackedItemAsync(
                                albumItem,
                                searchText,
                                filter,
                                options,
                                album.Id,
                                trackId: null,
                                isTrack: false,
                                enabled: false).ConfigureAwait(false);
                        }

                        foreach (var trackedItem in trackedTracks.Values
                            .Where(item => item.LidarrAlbumId == album.Id && item.Enabled)
                            .ToList())
                        {
                            trackedItem.Enabled = false;
                            await WishlistService.UpdateAsync(trackedItem).ConfigureAwait(false);
                        }
                    }
                }

                if (!expandedPartialAlbum)
                {
                    if (album.Id > 0 && album.Statistics?.TrackFileCount == 0 && GetTotalTrackCount(album) > 0)
                    {
                        foreach (var trackedItem in trackedTracks.Values
                            .Where(item => item.LidarrAlbumId == album.Id && item.Enabled)
                            .ToList())
                        {
                            trackedItem.Enabled = false;
                            await WishlistService.UpdateAsync(trackedItem).ConfigureAwait(false);
                        }
                    }

                    targets.Add((searchText, null));
                }
                else if (targets.Count == 0)
                {
                    result.SkippedCount++;
                }

                foreach (var target in targets)
                {
                    if (result.CreatedCount + pendingItems.Count >= options.MaxItemsPerSync)
                    {
                        Log.Information(
                            "Lidarr sync reached per-cycle cap of {Cap} new items; will continue from page {Page} next cycle",
                            options.MaxItemsPerSync,
                            page);
                        reachedCap = true;
                        break;
                    }

                    if (target.TrackId.HasValue && trackedTracks.TryGetValue(target.TrackId.Value, out var trackedTrack))
                    {
                        await UpdateTrackedItemAsync(
                            trackedTrack,
                            target.SearchText,
                            filter,
                            options,
                            album.Id,
                            target.TrackId,
                            isTrack: true).ConfigureAwait(false);
                        continue;
                    }

                    if (!target.TrackId.HasValue && album.Id > 0 && trackedAlbums.TryGetValue(album.Id, out var trackedAlbum))
                    {
                        await UpdateTrackedItemAsync(
                            trackedAlbum,
                            target.SearchText,
                            filter,
                            options,
                            album.Id,
                            trackId: null,
                            isTrack: false).ConfigureAwait(false);
                        continue;
                    }

                    var wishlistKey = BuildWishlistKey(target.SearchText, filter);
                    if (existingSearches.Contains(wishlistKey))
                    {
                        result.DuplicateCount++;
                        continue;
                    }

                    pendingItems.Add(new WishlistItem
                    {
                        SearchText = target.SearchText,
                        Filter = filter,
                        Enabled = true,
                        AutoDownload = options.AutoDownload,
                        MaxResults = options.WishlistMaxResults,
                        MaxDownloads = target.TrackId.HasValue ? 1 : null,
                        LidarrAlbumId = album.Id > 0 ? album.Id : null,
                        LidarrTrackId = target.TrackId,
                    });

                    existingSearches.Add(wishlistKey);
                }

                if (reachedCap)
                {
                    break;
                }
            }

            if (pendingItems.Count > 0)
            {
                await WishlistService.CreateManyAsync(pendingItems, cancellationToken).ConfigureAwait(false);
                result.CreatedCount += pendingItems.Count;
            }

            if (page * lidarrPageSize >= result.WantedCount)
            {
                break;
            }

            page++;
        }

        Log.Information(
            "Lidarr wanted sync complete: {Created} created, {Duplicates} duplicates, {Skipped} skipped from {Wanted} wanted records",
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

    private static WishlistItem? FindLegacyAlbumItem(
        IEnumerable<WishlistItem> existing,
        string searchText,
        string fallbackFilter)
        => existing.FirstOrDefault(item =>
            !item.LidarrAlbumId.HasValue &&
            !item.LidarrTrackId.HasValue &&
            string.Equals(item.SearchText.Trim(), searchText, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Filter.Trim(), fallbackFilter.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsPartiallyMissing(LidarrWantedAlbum album)
    {
        var statistics = album.Statistics;
        return statistics != null && statistics.TrackFileCount > 0 && GetTotalTrackCount(album) > statistics.TrackFileCount;
    }

    private static int GetTotalTrackCount(LidarrWantedAlbum album)
        => Math.Max(album.Statistics?.TrackCount ?? 0, album.Statistics?.TotalTrackCount ?? 0);

    private static bool IsMissingTrack(LidarrTrackResource track)
        => !track.HasFile && track.TrackFileId <= 0;

    private static string BuildTrackSearchText(LidarrWantedAlbum album, LidarrTrackResource track)
        => string.Join(
            " ",
            new[] { album.Artist?.ArtistName, album.Title, track.TrackNumber, track.Title }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

    private async Task UpdateTrackedItemAsync(
        WishlistItem item,
        string searchText,
        string filter,
        global::slskd.Options.IntegrationOptions.LidarrOptions options,
        int albumId,
        int? trackId,
        bool isTrack,
        bool? enabled = null)
    {
        int? maxDownloads = isTrack ? 1 : null;
        var desiredEnabled = enabled ?? item.Enabled;
        if (item.SearchText == searchText &&
            item.Filter == filter &&
            item.AutoDownload == options.AutoDownload &&
            item.MaxResults == options.WishlistMaxResults &&
            item.MaxDownloads == maxDownloads &&
            item.Enabled == desiredEnabled &&
            item.LidarrAlbumId == albumId &&
            item.LidarrTrackId == trackId)
        {
            return;
        }

        item.SearchText = searchText;
        item.Filter = filter;
        item.AutoDownload = options.AutoDownload;
        item.MaxResults = options.WishlistMaxResults;
        item.MaxDownloads = maxDownloads;
        item.Enabled = desiredEnabled;
        item.LidarrAlbumId = albumId;
        item.LidarrTrackId = trackId;
        await WishlistService.UpdateAsync(item).ConfigureAwait(false);
    }

    internal static string BuildQualityFilter(LidarrQualityProfile profile)
    {
        var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (profile.Items == null)
        {
            return string.Empty;
        }

        foreach (var item in profile.Items)
        {
            AddQualityFormats(item, formats);
        }

        return string.Join(" OR ", formats.OrderBy(format => format, StringComparer.OrdinalIgnoreCase));
    }

    private static void AddQualityFormats(LidarrQualityProfileItem item, ISet<string> formats)
    {
        if (item.Allowed)
        {
            var qualityName = item.Quality?.Name ?? item.Name;
            foreach (var format in GetFileFormats(qualityName))
            {
                formats.Add(format);
            }
        }

        if (item.Items == null)
        {
            return;
        }

        foreach (var child in item.Items)
        {
            AddQualityFormats(child, formats);
        }
    }

    private static IReadOnlyList<string> GetFileFormats(string qualityName)
    {
        if (qualityName.Contains("mp3", StringComparison.OrdinalIgnoreCase))
        {
            return ["mp3"];
        }

        if (qualityName.Contains("flac", StringComparison.OrdinalIgnoreCase))
        {
            return ["flac"];
        }

        if (qualityName.Contains("alac", StringComparison.OrdinalIgnoreCase))
        {
            return ["alac", "m4a"];
        }

        if (qualityName.Contains("aac", StringComparison.OrdinalIgnoreCase))
        {
            return ["aac", "m4a"];
        }

        if (qualityName.Contains("vorbis", StringComparison.OrdinalIgnoreCase) ||
            qualityName.Contains("ogg", StringComparison.OrdinalIgnoreCase))
        {
            return ["ogg"];
        }

        if (qualityName.Contains("opus", StringComparison.OrdinalIgnoreCase))
        {
            return ["opus"];
        }

        if (qualityName.Contains("ape", StringComparison.OrdinalIgnoreCase))
        {
            return ["ape"];
        }

        if (qualityName.Contains("wav", StringComparison.OrdinalIgnoreCase))
        {
            return ["wav"];
        }

        if (qualityName.Contains("aiff", StringComparison.OrdinalIgnoreCase) ||
            qualityName.Contains("aif", StringComparison.OrdinalIgnoreCase))
        {
            return ["aiff", "aif"];
        }

        return [];
    }

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
            catch (OperationCanceledException ex) when (IsHttpClientTimeout(ex))
            {
                Log.Information("Lidarr wanted sync unavailable: {Message}", ex.Message);
                SyncState.LastError = ex.Message;
            }
            catch (Exception ex) when (IsExpectedExternalHttpFailure(ex))
            {
                Log.Information("Lidarr wanted sync unavailable: {Message}", ex.Message);
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

    internal static bool IsHttpClientTimeout(OperationCanceledException exception)
        => exception.Message.Contains("HttpClient.Timeout", StringComparison.Ordinal);
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
