// <copyright file="SearchServiceLifecycleTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using slskd.Common.Security;
using slskd.Search;
using slskd.Search.API;
using slskd.Search.Providers;
using slskd.VirtualSoulfind.Capture;
using Serilog;
using Soulseek;
using Xunit;

public class SearchServiceLifecycleTests
{
    [Fact]
    public void TryCancel_RemovesAndDisposesTrackedCancellationToken()
    {
        using var service = CreateService();
        var searchId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        GetCancellationTokens(service)[searchId] = cts;

        Assert.True(service.TryCancel(searchId));
        Assert.False(GetCancellationTokens(service).ContainsKey(searchId));
        Assert.True(cts.IsCancellationRequested);
        Assert.Throws<ObjectDisposedException>(() => _ = cts.Token.WaitHandle);
    }

    [Fact]
    public void Dispose_CancelsAndDisposesAllTrackedCancellationTokens()
    {
        var service = CreateService();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        using var firstCts = new CancellationTokenSource();
        using var secondCts = new CancellationTokenSource();
        var tracked = GetCancellationTokens(service);
        tracked[firstId] = firstCts;
        tracked[secondId] = secondCts;

        service.Dispose();

        Assert.Empty(tracked);
        Assert.True(firstCts.IsCancellationRequested);
        Assert.True(secondCts.IsCancellationRequested);
        Assert.Throws<ObjectDisposedException>(() => _ = firstCts.Token.WaitHandle);
        Assert.Throws<ObjectDisposedException>(() => _ = secondCts.Token.WaitHandle);
    }

    [Fact]
    public void IsExpectedSearchCancellation_WhenSearchTokenCancelled_ReturnsTrue()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = SearchService.IsExpectedSearchCancellation(
            new OperationCanceledException(),
            cts.Token,
            applicationIsShuttingDown: false);

        Assert.True(result);
    }

    [Fact]
    public void IsExpectedSearchCancellation_WhenApplicationShuttingDown_ReturnsTrue()
    {
        using var cts = new CancellationTokenSource();

        var result = SearchService.IsExpectedSearchCancellation(
            new OperationCanceledException(),
            cts.Token,
            applicationIsShuttingDown: true);

        Assert.True(result);
    }

    [Fact]
    public void IsExpectedSearchCancellation_WhenNotCancelled_ReturnsFalse()
    {
        using var cts = new CancellationTokenSource();

        var result = SearchService.IsExpectedSearchCancellation(
            new OperationCanceledException(),
            cts.Token,
            applicationIsShuttingDown: false);

        Assert.False(result);
    }

    [Fact]
    public void IsSearchUnavailableDuringLogin_WhenSoulseekStillLoggingIn_ReturnsTrue()
    {
        var result = SearchService.IsSearchUnavailableDuringLogin(
            new InvalidOperationException("The server connection must be connected and logged in to perform a search (currently: Connected, LoggingIn)"));

        Assert.True(result);
    }

    [Fact]
    public void IsSearchUnavailableDuringLogin_WhenDifferentInvalidOperation_ReturnsFalse()
    {
        var result = SearchService.IsSearchUnavailableDuringLogin(
            new InvalidOperationException("Search rate limit exceeded. See Soulseek safety configuration."));

        Assert.False(result);
    }

    [Fact]
    public void IsExpectedSearchFinalizationFailure_WhenObjectDisposedDuringShutdown_ReturnsTrue()
    {
        var result = SearchService.IsExpectedSearchFinalizationFailure(
            new ObjectDisposedException("SearchDbContext"),
            applicationIsShuttingDown: true);

        Assert.True(result);
    }

    [Fact]
    public void IsExpectedSearchFinalizationFailure_WhenObjectDisposedDuringRuntime_ReturnsFalse()
    {
        var result = SearchService.IsExpectedSearchFinalizationFailure(
            new ObjectDisposedException("SearchDbContext"),
            applicationIsShuttingDown: false);

        Assert.False(result);
    }

    [Fact]
    public void IsExpectedSearchRuntimeShutdownFailure_WhenLockDisposingDuringShutdown_ReturnsTrue()
    {
        var result = SearchService.IsExpectedSearchRuntimeShutdownFailure(
            new InvalidOperationException("The lock is being disposed while still being used. It either is being held by a thread and/or has active waiters waiting to acquire the lock."),
            applicationIsShuttingDown: true);

        Assert.True(result);
    }

    [Fact]
    public void IsExpectedSearchRuntimeShutdownFailure_WhenLockDisposingDuringRuntime_ReturnsFalse()
    {
        var result = SearchService.IsExpectedSearchRuntimeShutdownFailure(
            new InvalidOperationException("The lock is being disposed while still being used."),
            applicationIsShuttingDown: false);

        Assert.False(result);
    }

    [Fact]
    public void IsExpectedSearchRuntimeShutdownFailure_WhenDifferentExceptionDuringShutdown_ReturnsFalse()
    {
        var result = SearchService.IsExpectedSearchRuntimeShutdownFailure(
            new InvalidOperationException("Search backend failed."),
            applicationIsShuttingDown: true);

        Assert.False(result);
    }

    [Fact]
    public void ApplyResponseSummary_IncludesEarlyMeshResponses()
    {
        var search = new slskd.Search.Search();
        var responses = new List<Response>
        {
            new()
            {
                Username = "mesh-peer",
                FileCount = 1,
                LockedFileCount = 0,
                Files = new List<slskd.Search.File>
                {
                    new()
                    {
                        Filename = "song.flac",
                        Size = 1234,
                    },
                },
                LockedFiles = new List<slskd.Search.File>(),
            },
        };

        SearchService.ApplyResponseSummary(search, responses);

        Assert.Equal(1, search.ResponseCount);
        Assert.Equal(1, search.FileCount);
        Assert.Equal(0, search.LockedFileCount);
    }

    [Fact]
    public void WithSoulseekSearch_PreservesPersistedProvenanceAndResponseAvailability()
    {
        var wishlistItemId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddSeconds(-1);
        var original = new slskd.Search.Search
        {
            Id = Guid.NewGuid(),
            ResponsesAvailable = true,
            SearchText = "artist title",
            Source = "wishlist",
            StartedAt = startedAt,
            Token = 42,
            WishlistItemId = wishlistItemId,
        };
        var progress = new Soulseek.Search(
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            42,
            SearchStates.InProgress,
            responseCount: 3,
            fileCount: 12,
            lockedFileCount: 2);

        var updated = original.WithSoulseekSearch(progress);

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("artist title", updated.SearchText);
        Assert.Equal("wishlist", updated.Source);
        Assert.Equal(startedAt, updated.StartedAt);
        Assert.Equal(42, updated.Token);
        Assert.Equal(wishlistItemId, updated.WishlistItemId);
        Assert.True(updated.ResponsesAvailable);
        Assert.Equal(3, updated.ResponseCount);
        Assert.Equal(12, updated.FileCount);
        Assert.Equal(2, updated.LockedFileCount);
        Assert.Equal(SearchStates.InProgress, updated.State);
    }

    [Fact]
    public async Task ListAsync_ResponseLessProjectionPreservesProvenance()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlite(connection)
            .Options;
        var searchId = Guid.NewGuid();
        var wishlistItemId = Guid.NewGuid();
        await using (var context = new SearchDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Searches.Add(new slskd.Search.Search
            {
                Id = searchId,
                Responses =
                [
                    new Response
                    {
                        FileCount = 1,
                        Files =
                        [
                            new slskd.Search.File
                            {
                                Filename = "artist\\album\\track.flac",
                                Size = 1_024,
                            },
                        ],
                        Username = "peer",
                    },
                ],
                SearchText = "artist title",
                Source = "wishlist",
                State = SearchStates.InProgress,
                Token = 42,
                WishlistItemId = wishlistItemId,
            });
            await context.SaveChangesAsync();
        }

        using var service = new SearchService(
            CreateSearchHub().Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<ISoulseekClient>(),
            new SearchDbContextFactory(options),
            Mock.Of<ISoulseekSafetyLimiter>());

        var listed = Assert.Single(await service.ListAsync());
        Assert.Equal(searchId, listed.Id);
        Assert.Equal("artist title", listed.SearchText);
        Assert.Equal("wishlist", listed.Source);
        Assert.Equal(wishlistItemId, listed.WishlistItemId);
        Assert.True(listed.ResponsesAvailable);
        Assert.Empty(listed.Responses);
    }

    [Fact]
    public async Task NotifyTrafficObserverAsync_WhenOneResponseFails_Continues()
    {
        var observer = new Mock<ITrafficObserver>();
        var responses = new[]
        {
            new SearchResponse("first", 1, true, 0, 0, Array.Empty<Soulseek.File>(), Array.Empty<Soulseek.File>()),
            new SearchResponse("second", 1, true, 0, 0, Array.Empty<Soulseek.File>(), Array.Empty<Soulseek.File>()),
        };

        observer
            .Setup(m => m.OnSearchResultsAsync("query", responses[0], It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new InvalidOperationException("observer failed")));
        observer
            .Setup(m => m.OnSearchResultsAsync("query", responses[1], It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await SearchService.NotifyTrafficObserverAsync(
            observer.Object,
            "query",
            responses,
            new LoggerConfiguration().CreateLogger(),
            CancellationToken.None);

        observer.Verify(
            m => m.OnSearchResultsAsync("query", It.IsAny<SearchResponse>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_WhenBridgedSearch_PreservesSourceAndWishlistItemId()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (var context = new SearchDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var hub = CreateSearchHub();
        var provider = new Mock<ISearchProvider>();
        provider.Setup(p => p.Name).Returns("pod");
        provider.Setup(p => p.StartSearchAsync(
                It.IsAny<slskd.Search.Providers.SearchRequest>(),
                It.IsAny<ISearchResultSink>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var wishlistItemId = Guid.NewGuid();
        using var service = new SearchService(
            hub.Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions
                {
                    ScenePodBridge = true,
                },
            }),
            Mock.Of<ISoulseekClient>(),
            new SearchDbContextFactory(options),
            Mock.Of<ISoulseekSafetyLimiter>(),
            searchProviders: [provider.Object]);

        var search = await service.StartAsync(
            Guid.NewGuid(),
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            new SearchOptions(),
            requestedProviders: null,
            safetySource: "wishlist",
            wishlistItemId: wishlistItemId);

        Assert.Equal("wishlist", search.Source);
        Assert.Equal(wishlistItemId, search.WishlistItemId);

        await using var verifyContext = new SearchDbContext(options);
        var persisted = await verifyContext.Searches.AsNoTracking().SingleAsync();
        Assert.Equal("wishlist", persisted.Source);
        Assert.Equal(wishlistItemId, persisted.WishlistItemId);
    }

    [Fact]
    public async Task StartAsync_WhenClientLaunchFails_ReleasesCancellationToken()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (var context = new SearchDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var client = new Mock<ISoulseekClient>();
        client.Setup(candidate => candidate.GetNextToken()).Returns(42);
        client
            .Setup(candidate => candidate.SearchAsync(
                It.IsAny<SearchQuery>(),
                It.IsAny<Action<SearchResponse>>(),
                It.IsAny<SearchScope>(),
                It.IsAny<int?>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken?>()))
            .Throws(new InvalidOperationException("search launch failed"));
        var safetyLimiter = new Mock<ISoulseekSafetyLimiter>();
        safetyLimiter.Setup(limiter => limiter.TryConsumeSearch("wishlist")).Returns(true);
        using var service = new SearchService(
            CreateSearchHub().Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            client.Object,
            new SearchDbContextFactory(options),
            safetyLimiter.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(
            Guid.NewGuid(),
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            new SearchOptions(),
            requestedProviders: null,
            safetySource: "wishlist"));

        Assert.Empty(GetCancellationTokens(service));
        await using var verifyContext = new SearchDbContext(options);
        var persisted = await verifyContext.Searches.AsNoTracking().SingleAsync();
        Assert.True(persisted.State.HasFlag(SearchStates.Errored));
    }

    [Fact]
    public async Task StartAsync_AfterLaunch_ContinuesPublishingBoundedProgress()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (var context = new SearchDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var updates = new ConcurrentQueue<slskd.Search.Search>();
        var completion = new TaskCompletionSource<Soulseek.Search>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<SearchResponse>? responseHandler = null;
        SearchOptions? capturedOptions = null;
        var client = new Mock<ISoulseekClient>();
        client.Setup(candidate => candidate.GetNextToken()).Returns(42);
        client
            .Setup(candidate => candidate.SearchAsync(
                It.IsAny<SearchQuery>(),
                It.IsAny<Action<SearchResponse>>(),
                It.IsAny<SearchScope>(),
                It.IsAny<int?>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<CancellationToken?>()))
            .Callback<SearchQuery, Action<SearchResponse>, SearchScope, int?, SearchOptions, CancellationToken?>(
                (_, handler, _, _, searchOptions, _) =>
                {
                    responseHandler = handler;
                    capturedOptions = searchOptions;
                })
            .Returns(completion.Task);
        var safetyLimiter = new Mock<ISoulseekSafetyLimiter>();
        safetyLimiter.Setup(limiter => limiter.TryConsumeSearch("wishlist")).Returns(true);
        var wishlistItemId = Guid.NewGuid();
        using var service = new SearchService(
            CreateSearchHub(updates).Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            client.Object,
            new SearchDbContextFactory(options),
            safetyLimiter.Object);

        await service.StartAsync(
            Guid.NewGuid(),
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            new SearchOptions(),
            requestedProviders: null,
            safetySource: "wishlist",
            wishlistItemId: wishlistItemId);

        var firstResponse = new SearchResponse(
            "peer-one",
            42,
            hasFreeUploadSlot: true,
            uploadSpeed: 1,
            queueLength: 0,
            [new Soulseek.File(1, "artist\\album\\one.flac", 1_024, "flac")]);
        var secondResponse = new SearchResponse(
            "peer-two",
            42,
            hasFreeUploadSlot: true,
            uploadSpeed: 1,
            queueLength: 0,
            [new Soulseek.File(1, "artist\\album\\two.flac", 2_048, "flac")]);
        var inProgress = new Soulseek.Search(
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            42,
            SearchStates.InProgress,
            responseCount: 2,
            fileCount: 2,
            lockedFileCount: 0);

        Assert.NotNull(responseHandler);
        Assert.NotNull(capturedOptions);
        responseHandler(firstResponse);
        capturedOptions.ResponseReceived((inProgress, firstResponse));
        Assert.Single(updates.Where(update => update.ResponseCount == 1));

        responseHandler(secondResponse);
        capturedOptions.ResponseReceived((inProgress, secondResponse));
        Assert.DoesNotContain(updates, update => update.ResponseCount == 2);

        Assert.True(await WaitUntilAsync(
            () => updates.Any(update => update.ResponseCount == 2),
            attempts: 120,
            delayMs: 25));

        await using (var progressContext = new SearchDbContext(options))
        {
            var progressRow = await progressContext.Searches.AsNoTracking().SingleAsync();
            Assert.Equal(0, progressRow.ResponseCount);
            Assert.Empty(progressRow.Responses);
        }

        completion.SetResult(new Soulseek.Search(
            SearchQuery.FromText("artist title"),
            SearchScope.Network,
            42,
            SearchStates.Completed,
            responseCount: 2,
            fileCount: 2,
            lockedFileCount: 0));
        Assert.True(await WaitUntilAsync(
            () => GetCancellationTokens(service).IsEmpty,
            attempts: 120,
            delayMs: 25));

        await using var verifyContext = new SearchDbContext(options);
        var persisted = await verifyContext.Searches.AsNoTracking().SingleAsync();
        Assert.Equal("wishlist", persisted.Source);
        Assert.Equal(wishlistItemId, persisted.WishlistItemId);
        Assert.Equal(2, persisted.ResponseCount);
        Assert.Equal(2, persisted.Responses.Count());
    }

    private static SearchService CreateService()
    {
        return new SearchService(
            Mock.Of<IHubContext<SearchHub>>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IDbContextFactory<SearchDbContext>>(),
            Mock.Of<ISoulseekSafetyLimiter>());
    }

    private static Mock<IHubContext<SearchHub>> CreateSearchHub(
        ConcurrentQueue<slskd.Search.Search>? updates = null)
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(client => client.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, arguments, _) =>
            {
                if (
                    updates != null
                    && method == SearchHubMethods.Update
                    && arguments.FirstOrDefault() is slskd.Search.Search search
                )
                {
                    updates.Enqueue(search);
                }
            })
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(clientProxy.Object);

        var hub = new Mock<IHubContext<SearchHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        return hub;
    }

    private static ConcurrentDictionary<Guid, CancellationTokenSource> GetCancellationTokens(SearchService service)
    {
        var property = typeof(SearchService).GetProperty(
            "CancellationTokens",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return Assert.IsType<ConcurrentDictionary<Guid, CancellationTokenSource>>(property?.GetValue(service));
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, int attempts, int delayMs)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(delayMs);
        }

        return condition();
    }

    private sealed class SearchDbContextFactory : IDbContextFactory<SearchDbContext>
    {
        private readonly DbContextOptions<SearchDbContext> _options;

        public SearchDbContextFactory(DbContextOptions<SearchDbContext> options)
        {
            _options = options;
        }

        public SearchDbContext CreateDbContext() => new(_options);

        public ValueTask<SearchDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }
}
