// <copyright file="LidarrSyncServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.Lidarr;

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

    private static LidarrSyncService CreateService(FakeLidarrClient lidarr, FakeWishlistService wishlist, string wishlistFilter)
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
                        MaxItemsPerSync = 100,
                    },
                },
            }));

    private sealed class FakeWishlistService : IWishlistService
    {
        public List<WishlistItem> Items { get; init; } = [];

        public List<WishlistItem> Created { get; } = [];

        public Task<List<WishlistItem>> ListAsync() => Task.FromResult(Items);

        public Task<WishlistItem?> GetAsync(Guid id) => Task.FromResult<WishlistItem?>(null);

        public Task<WishlistItem> CreateAsync(WishlistItem item)
        {
            Created.Add(item);
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<WishlistItem> UpdateAsync(WishlistItem item) => Task.FromResult(item);

        public Task DeleteAsync(Guid id) => Task.CompletedTask;

        public Task<slskd.Search.Search> RunSearchAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WishlistCsvImportResult> ImportCsvAsync(
            string csvText,
            WishlistCsvImportOptions options,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeLidarrClient : ILidarrClient
    {
        public IReadOnlyList<LidarrWantedAlbum> Wanted { get; init; } = [];

        public Task<LidarrSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrSystemStatus());

        public Task<IReadOnlyList<LidarrWantedAlbum>> GetWantedMissingAsync(int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(Wanted);

        public Task<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)> GetWantedMissingPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult((page == 1 ? Wanted : [], Wanted.Count));

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
