// <copyright file="WishlistControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Wishlist;

using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using slskd.Search;
using slskd.Tests.Unit;
using slskd.Transfers.Downloads;
using slskd.Transfers.Ranking;
using slskd.Wishlist;
using slskd.Wishlist.API;
using Soulseek;
using SlskdSearch = slskd.Search.Search;
using Xunit;

public class WishlistControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WishlistDbContextFactory _contextFactory;

    public WishlistControllerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _contextFactory = new WishlistDbContextFactory(_connection);

        using var context = _contextFactory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Create_TrimsSearchTextAndFilterBeforePersisting()
    {
        var service = new Mock<IWishlistService>();
        service
            .Setup(x => x.CreateAsync(It.IsAny<WishlistItem>()))
            .ReturnsAsync((WishlistItem item) =>
            {
                item.Id = Guid.NewGuid();
                return item;
            });

        var controller = new WishlistController(service.Object);

        var result = await controller.Create(new CreateWishlistRequest
        {
            SearchText = " artist - title ",
            Filter = " flac ",
            Enabled = true,
            AutoDownload = false,
            MaxResults = 25,
        });

        Assert.IsType<CreatedAtActionResult>(result);
        service.Verify(
            x => x.CreateAsync(It.Is<WishlistItem>(item =>
                item.SearchText == "artist - title" &&
                item.Filter == "flac" &&
                item.MaxResults == 25)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WithBlankSearchTextAfterTrim_ReturnsBadRequest()
    {
        var controller = new WishlistController(Mock.Of<IWishlistService>());

        var result = await controller.Update(Guid.NewGuid(), new UpdateWishlistRequest
        {
            SearchText = "   ",
            Filter = " flac ",
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("SearchText is required", bad.Value);
    }

    [Fact]
    public async Task Create_WithNonPositiveMaxResults_ReturnsBadRequest()
    {
        var controller = new WishlistController(Mock.Of<IWishlistService>());

        var result = await controller.Create(new CreateWishlistRequest
        {
            SearchText = "artist - title",
            MaxResults = 0,
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("MaxResults must be greater than 0", bad.Value);
    }

    [Fact]
    public async Task Update_WithNonPositiveMaxResults_ReturnsBadRequest()
    {
        var controller = new WishlistController(Mock.Of<IWishlistService>());

        var result = await controller.Update(Guid.NewGuid(), new UpdateWishlistRequest
        {
            SearchText = "artist - title",
            MaxResults = -1,
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("MaxResults must be greater than 0", bad.Value);
    }

    [Fact]
    public async Task ImportCsv_TrimsFilterAndPassesOptions()
    {
        var service = new Mock<IWishlistService>();
        service
            .Setup(x => x.ImportCsvAsync(
                It.IsAny<string>(),
                It.IsAny<WishlistCsvImportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistCsvImportResult
            {
                TotalRows = 1,
                CreatedCount = 1,
            });
        var controller = new WishlistController(service.Object);

        var result = await controller.ImportCsv(new ImportWishlistCsvRequest
        {
            CsvText = "Track name,Artist name\nSong,Artist",
            Filter = " flac ",
            Enabled = false,
            AutoDownload = true,
            IncludeAlbum = true,
            MaxResults = 25,
        });

        Assert.IsType<OkObjectResult>(result);
        service.Verify(
            x => x.ImportCsvAsync(
                "Track name,Artist name\nSong,Artist",
                It.Is<WishlistCsvImportOptions>(options =>
                    options.Filter == "flac" &&
                    options.Enabled == false &&
                    options.AutoDownload &&
                    options.IncludeAlbum &&
                    options.MaxResults == 25),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportCsv_WithBlankCsv_ReturnsBadRequest()
    {
        var controller = new WishlistController(Mock.Of<IWishlistService>());

        var result = await controller.ImportCsv(new ImportWishlistCsvRequest
        {
            CsvText = "   ",
        });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("CsvText is required", bad.Value);
    }

    [Fact]
    public void ParseCsvTracks_HandlesTuneMyMusicHeadersAndEscapedFields()
    {
        const string csv = "Track name,Artist name,Album name\n\"Song, Part 1\",\"Artist \"\"Name\"\"\",Album";

        var tracks = WishlistService.ParseCsvTracks(csv, includeAlbum: true);

        var track = Assert.Single(tracks);
        Assert.Equal("Artist \"Name\" Song, Part 1 Album", track.SearchText);
        Assert.Equal(2, track.RowNumber);
    }

    [Fact]
    public void ParseCsvTracks_SkipsHeaderlessRowsWithoutArtistAndTitle()
    {
        const string csv = "Song Only\nTitle,Artist";

        var tracks = WishlistService.ParseCsvTracks(csv, includeAlbum: false);

        Assert.Equal(2, tracks.Count);
        Assert.Equal(string.Empty, tracks[0].SearchText);
        Assert.Equal("Artist Title", tracks[1].SearchText);
    }

    [Fact]
    public async Task RunSearch_UsesNetworkScopeAndWishlistSafetySource()
    {
        var itemId = Guid.NewGuid();
        await using (var context = _contextFactory.CreateDbContext())
        {
            context.WishlistItems.Add(new WishlistItem
            {
                Id = itemId,
                SearchText = "artist title",
                MaxResults = 25,
            });
            await context.SaveChangesAsync();
        }

        SearchScope? capturedScope = null;
        string? capturedSafetySource = null;
        SearchOptions? capturedOptions = null;
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<string>()))
            .Callback<Guid, SearchQuery, SearchScope, SearchOptions?, List<string>?, string>((id, query, scope, options, providers, safetySource) =>
            {
                capturedScope = scope;
                capturedSafetySource = safetySource;
                capturedOptions = options;
            })
            .ReturnsAsync((Guid id, SearchQuery query, SearchScope scope, SearchOptions? options, List<string>? providers, string safetySource) =>
                new SlskdSearch
                {
                    Id = id,
                    SearchText = query.SearchText,
                    State = SearchStates.Requested,
                });
        searchService
            .Setup(service => service.FindAsync(It.IsAny<Expression<Func<SlskdSearch, bool>>>(), true))
            .ReturnsAsync((Expression<Func<SlskdSearch, bool>> expression, bool includeResponses) =>
                new SlskdSearch
                {
                    Id = itemId,
                    SearchText = "artist title",
                    State = SearchStates.Completed,
                    ResponseCount = 3,
                });

        var service = new WishlistService(
            _contextFactory,
            searchService.Object,
            Mock.Of<ISoulseekClient>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<ISourceRankingService>(),
            Mock.Of<IDownloadService>());

        var result = await service.RunSearchAsync(itemId);

        Assert.Equal(SearchScopeType.Network, capturedScope?.Type);
        Assert.Equal("wishlist", capturedSafetySource);
        Assert.Equal(25, capturedOptions?.ResponseLimit);
        Assert.Equal(3, result.ResponseCount);
    }

    [Fact]
    public async Task RunSearch_WhenAutoDownloadEnqueueFails_DoesNotDisableWishlistItem()
    {
        var itemId = Guid.NewGuid();
        await using (var context = _contextFactory.CreateDbContext())
        {
            context.WishlistItems.Add(new WishlistItem
            {
                Id = itemId,
                SearchText = "artist title",
                Filter = "flac",
                AutoDownload = true,
                Enabled = true,
                MaxResults = 25,
            });
            await context.SaveChangesAsync();
        }

        var searchService = CreateCompletedSearchService(itemId);
        var rankingService = new Mock<ISourceRankingService>();
        rankingService
            .Setup(service => service.RankSourcesAsync(It.IsAny<IEnumerable<SourceCandidate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SourceCandidate> candidates, CancellationToken cancellationToken) =>
                candidates.Select(candidate => new RankedSource
                {
                    Username = candidate.Username,
                    Filename = candidate.Filename,
                    Size = candidate.Size,
                    SmartScore = 10,
                }));
        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<(string Filename, long Size)>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<slskd.Transfers.Transfer>(), new List<string> { "failed.flac" }));

        var service = new WishlistService(
            _contextFactory,
            searchService.Object,
            Mock.Of<ISoulseekClient>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            rankingService.Object,
            downloadService.Object);

        await service.RunSearchAsync(itemId);

        await using var verifyContext = _contextFactory.CreateDbContext();
        var item = await verifyContext.WishlistItems.FindAsync(itemId);
        Assert.NotNull(item);
        Assert.True(item.Enabled);
        Assert.Equal(0, item.TotalDownloadCount);
    }

    [Fact]
    public async Task RunSearch_WhenAutoDownloadEnqueuesFiles_DisablesWishlistItemAndCountsEnqueued()
    {
        var itemId = Guid.NewGuid();
        await using (var context = _contextFactory.CreateDbContext())
        {
            context.WishlistItems.Add(new WishlistItem
            {
                Id = itemId,
                SearchText = "artist title",
                Filter = "flac",
                AutoDownload = true,
                Enabled = true,
                MaxResults = 25,
            });
            await context.SaveChangesAsync();
        }

        var searchService = CreateCompletedSearchService(itemId);
        var rankingService = new Mock<ISourceRankingService>();
        rankingService
            .Setup(service => service.RankSourcesAsync(It.IsAny<IEnumerable<SourceCandidate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SourceCandidate> candidates, CancellationToken cancellationToken) =>
                candidates.Select(candidate => new RankedSource
                {
                    Username = candidate.Username,
                    Filename = candidate.Filename,
                    Size = candidate.Size,
                    SmartScore = 10,
                }));
        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<(string Filename, long Size)>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<slskd.Transfers.Transfer>
                {
                    new() { Id = Guid.NewGuid(), Username = "alice", Filename = @"Music\Album\01 Song.flac" },
                    new() { Id = Guid.NewGuid(), Username = "alice", Filename = @"Music\Album\02 Song.flac" },
                },
                new List<string>()));

        var service = new WishlistService(
            _contextFactory,
            searchService.Object,
            Mock.Of<ISoulseekClient>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            rankingService.Object,
            downloadService.Object);

        await service.RunSearchAsync(itemId);

        await using var verifyContext = _contextFactory.CreateDbContext();
        var item = await verifyContext.WishlistItems.FindAsync(itemId);
        Assert.NotNull(item);
        Assert.False(item.Enabled);
        Assert.Equal(2, item.TotalDownloadCount);
    }

    private static Mock<ISearchService> CreateCompletedSearchService(Guid searchId)
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<string>()))
            .ReturnsAsync((Guid id, SearchQuery query, SearchScope scope, SearchOptions? options, List<string>? providers, string safetySource) =>
                new SlskdSearch
                {
                    Id = id,
                    SearchText = query.SearchText,
                    State = SearchStates.Requested,
                });
        searchService
            .Setup(service => service.FindAsync(It.IsAny<Expression<Func<SlskdSearch, bool>>>(), true))
            .ReturnsAsync(new SlskdSearch
            {
                Id = searchId,
                SearchText = "artist title",
                State = SearchStates.Completed,
                ResponseCount = 1,
                Responses =
                [
                    new Response
                    {
                        Username = "alice",
                        HasFreeUploadSlot = true,
                        QueueLength = 0,
                        UploadSpeed = 1000,
                        Files =
                        [
                            new slskd.Search.File { Filename = @"Music\Album\01 Song.flac", Size = 100 },
                            new slskd.Search.File { Filename = @"Music\Album\02 Song.flac", Size = 200 },
                        ],
                    },
                ],
            });

        return searchService;
    }

    private sealed class WishlistDbContextFactory : IDbContextFactory<WishlistDbContext>
    {
        private readonly DbContextOptions<WishlistDbContext> _options;

        public WishlistDbContextFactory(SqliteConnection connection)
        {
            _options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        public WishlistDbContext CreateDbContext() => new(_options);

        public ValueTask<WishlistDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }
}
