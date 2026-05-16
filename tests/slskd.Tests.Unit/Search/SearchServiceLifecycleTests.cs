// <copyright file="SearchServiceLifecycleTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using slskd.Common.Security;
using slskd.Search;
using slskd.Search.API;
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

    private static SearchService CreateService()
    {
        return new SearchService(
            Mock.Of<IHubContext<SearchHub>>(),
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IDbContextFactory<SearchDbContext>>(),
            Mock.Of<ISoulseekSafetyLimiter>());
    }

    private static ConcurrentDictionary<Guid, CancellationTokenSource> GetCancellationTokens(SearchService service)
    {
        var property = typeof(SearchService).GetProperty(
            "CancellationTokens",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return Assert.IsType<ConcurrentDictionary<Guid, CancellationTokenSource>>(property?.GetValue(service));
    }
}
