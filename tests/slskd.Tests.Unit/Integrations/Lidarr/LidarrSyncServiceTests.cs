// <copyright file="LidarrSyncServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.Lidarr;

using System.Net.Http;
using slskd.Integrations.Lidarr;
using slskd.Wishlist;
using Xunit;

public class LidarrSyncServiceTests
{
    [Fact]
    public async Task SyncWantedToWishlist_DedupesBySearchTextAndFilter()
    {
        var lidarr = new FakeLidarrClient
        {
            Wanted =
            [
                new LidarrWantedAlbum
                {
                    Title = "Album",
                    Artist = new LidarrArtistResource { ArtistName = "Artist" },
                },
            ],
        };
        var wishlist = new FakeWishlistService
        {
            Items =
            [
                new WishlistItem { SearchText = "Artist Album", Filter = "mp3" },
            ],
        };
        var service = CreateService(lidarr, wishlist, wishlistFilter: "flac");

        var result = await service.SyncWantedToWishlistAsync();

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Equal("flac", Assert.Single(wishlist.Created).Filter);
    }

    [Fact]
    public async Task SyncWantedToWishlist_SkipsDuplicateWhenFilterMatches()
    {
        var lidarr = new FakeLidarrClient
        {
            Wanted =
            [
                new LidarrWantedAlbum
                {
                    Title = "Album",
                    Artist = new LidarrArtistResource { ArtistName = "Artist" },
                },
            ],
        };
        var wishlist = new FakeWishlistService
        {
            Items =
            [
                new WishlistItem { SearchText = "Artist Album", Filter = "flac" },
            ],
        };
        var service = CreateService(lidarr, wishlist, wishlistFilter: "flac");

        var result = await service.SyncWantedToWishlistAsync();

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Empty(wishlist.Created);
    }

    [Fact]
    public async Task SyncWantedToWishlist_DeduplicatesWithinPendingBatch()
    {
        var lidarr = new FakeLidarrClient
        {
            Wanted =
            [
                new LidarrWantedAlbum
                {
                    Title = "Album",
                    Artist = new LidarrArtistResource { ArtistName = "Artist" },
                },
                new LidarrWantedAlbum
                {
                    Title = "Album",
                    Artist = new LidarrArtistResource { ArtistName = "Artist" },
                },
            ],
        };
        var wishlist = new FakeWishlistService();
        var service = CreateService(lidarr, wishlist, wishlistFilter: "flac");

        var result = await service.SyncWantedToWishlistAsync();

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(1, Assert.Single(wishlist.CreateBatches));
    }

    [Fact]
    public async Task SyncWantedToWishlist_WithLargePage_CreatesOneBoundedBatchAtCap()
    {
        var lidarr = new FakeLidarrClient
        {
            Wanted = Enumerable.Range(1, 250)
                .Select(index => new LidarrWantedAlbum
                {
                    Title = $"Album {index}",
                    Artist = new LidarrArtistResource { ArtistName = "Artist" },
                })
                .ToList(),
        };
        var wishlist = new FakeWishlistService();
        var service = CreateService(lidarr, wishlist, wishlistFilter: "flac", maxItemsPerSync: 100);

        var result = await service.SyncWantedToWishlistAsync();

        Assert.Equal(100, result.CreatedCount);
        Assert.Equal(100, wishlist.Created.Count);
        Assert.Equal(100, Assert.Single(wishlist.CreateBatches));
        Assert.Equal(1, lidarr.PageCalls);
    }

    [Fact]
    public void IsExpectedExternalHttpFailure_ReturnsTrue_ForHttpRequestException()
    {
        var ex = new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error).");

        Assert.True(LidarrSyncService.IsExpectedExternalHttpFailure(ex));
    }

    [Fact]
    public void IsHttpClientTimeout_ReturnsTrue_ForHttpClientTimeout()
    {
        var ex = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 60 seconds elapsing.");

        Assert.True(LidarrSyncService.IsHttpClientTimeout(ex));
    }

    [Fact]
    public void IsExpectedExternalHttpFailure_ReturnsFalse_ForUnexpectedException()
    {
        var ex = new InvalidOperationException("local bug");

        Assert.False(LidarrSyncService.IsExpectedExternalHttpFailure(ex));
    }

    private static LidarrSyncService CreateService(
        FakeLidarrClient lidarr,
        FakeWishlistService wishlist,
        string wishlistFilter,
        int maxItemsPerSync = 100)
        => new(
            lidarr,
            wishlist,
            new TestOptionsMonitor<Options>(new Options
            {
                Integration = new Options.IntegrationOptions
                {
                    Lidarr = new Options.IntegrationOptions.LidarrOptions
                    {
                        Enabled = true,
                        Url = "http://lidarr.test",
                        ApiKey = "key",
                        SyncWantedToWishlist = true,
                        WishlistFilter = wishlistFilter,
                        MaxItemsPerSync = maxItemsPerSync,
                    },
                },
            }));

    private sealed class FakeWishlistService : IWishlistService
    {
        public List<WishlistItem> Items { get; init; } = [];

        public List<WishlistItem> Created { get; } = [];

        public List<int> CreateBatches { get; } = [];

        public List<WishlistIgnoredResult> IgnoredResults { get; } = [];

        public Task<List<WishlistItem>> ListAsync() => Task.FromResult(Items);

        public Task<WishlistItem?> GetAsync(Guid id) => Task.FromResult<WishlistItem?>(null);

        public Task<WishlistItem?> FindBySearchTextAsync(string searchText) =>
            Task.FromResult(Items
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault(item => string.Equals(item.SearchText, searchText, StringComparison.OrdinalIgnoreCase)));

        public Task<WishlistItem> CreateAsync(WishlistItem item)
        {
            Created.Add(item);
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<List<WishlistItem>> CreateManyAsync(
            IEnumerable<WishlistItem> items,
            CancellationToken cancellationToken = default)
        {
            var batch = items.ToList();
            CreateBatches.Add(batch.Count);
            Created.AddRange(batch);
            Items.AddRange(batch);
            return Task.FromResult(batch);
        }

        public Task<WishlistItem> UpdateAsync(WishlistItem item) => Task.FromResult(item);

        public Task DeleteAsync(Guid id) => Task.CompletedTask;

        public Task<slskd.Search.Search> RunSearchAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<slskd.Search.Search>> GetSearchesForItemAsync(Guid wishlistItemId, int limit = 50) =>
            Task.FromResult(new List<slskd.Search.Search>());

        public Task MarkViewedAsync(Guid id) => Task.CompletedTask;

        public Task MarkAllViewedAsync() => Task.CompletedTask;

        public Task<List<WishlistIgnoredResult>> ListIgnoredResultsAsync(Guid wishlistItemId) =>
            Task.FromResult(IgnoredResults.Where(result => result.WishlistItemId == wishlistItemId).ToList());

        public Task<WishlistIgnoredResult> IgnoreResultAsync(Guid wishlistItemId, string username, string directory)
        {
            var ignoredResult = new WishlistIgnoredResult
            {
                WishlistItemId = wishlistItemId,
                Username = username,
                Directory = directory,
            };
            IgnoredResults.Add(ignoredResult);
            return Task.FromResult(ignoredResult);
        }

        public Task DeleteIgnoredResultAsync(Guid wishlistItemId, Guid ignoredResultId)
        {
            IgnoredResults.RemoveAll(result => result.WishlistItemId == wishlistItemId && result.Id == ignoredResultId);
            return Task.CompletedTask;
        }

        public Task<WishlistCsvImportResult> ImportCsvAsync(
            string csvText,
            WishlistCsvImportOptions options,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeLidarrClient : ILidarrClient
    {
        public IReadOnlyList<LidarrWantedAlbum> Wanted { get; init; } = [];

        public int PageCalls { get; private set; }

        public Task<LidarrSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrSystemStatus());

        public Task<IReadOnlyList<LidarrWantedAlbum>> GetWantedMissingAsync(int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(Wanted);

        public Task<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)> GetWantedMissingPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            PageCalls++;
            return Task.FromResult((page == 1 ? Wanted : [], Wanted.Count));
        }

        public Task<IReadOnlyList<LidarrManualImportResource>> GetManualImportCandidatesAsync(
            string folder,
            bool filterExistingFiles,
            bool replaceExistingFiles,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LidarrManualImportResource>>([]);

        public Task<LidarrCommandResponse> StartManualImportAsync(
            IReadOnlyList<LidarrManualImportResource> files,
            string importMode,
            bool replaceExistingFiles,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrCommandResponse());

        public Task<LidarrCommandResponse> StartCommandAsync(string name, object payload, CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrCommandResponse());
    }
}
