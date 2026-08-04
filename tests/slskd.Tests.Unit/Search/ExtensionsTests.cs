// <copyright file="ExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search;

using System;
using Soulseek;
using slskd.Search;
using Xunit;

public class ExtensionsTests
{
    [Fact]
    public void WithActions_StateChanged_InvokesBothAndAggregatesMultipleFailures()
    {
        var originalCalls = 0;
        var injectedCalls = 0;

        var options = new SearchOptions(
            stateChanged: args =>
            {
                originalCalls++;
                throw new InvalidOperationException("existing");
            });

        var bound = options.WithActions(stateChanged: args =>
        {
            injectedCalls++;
            throw new ArgumentException("new");
        });

        var exception = Assert.Throws<AggregateException>(() => bound.StateChanged!((SearchStates.None, default!)));

        Assert.Equal(1, originalCalls);
        Assert.Equal(1, injectedCalls);
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
        Assert.IsType<ArgumentException>(exception.InnerExceptions[1]);
    }

    [Fact]
    public void WithActions_ResponseReceived_SingleFailurePreserved()
    {
        var originalCalls = 0;
        var options = new SearchOptions(
            responseReceived: response =>
            {
                originalCalls++;
                throw new InvalidOperationException("boom");
            });

        var bound = options.WithActions();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            bound.ResponseReceived!((default!, default!)));

        Assert.Equal(1, originalCalls);
        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public void WithSearchTimeout_PreservesFiltersAndCallbacks()
    {
        var responseFilter = (SearchResponse _) => true;
        var fileFilter = (Soulseek.File _) => true;
        var stateChanged = (Action<(SearchStates PreviousState, Soulseek.Search Search)>)(_ => { });
        var responseReceived = (Action<(Soulseek.Search Search, SearchResponse Response)>)(_ => { });
        var options = new SearchOptions(
            searchTimeout: 15_000,
            responseLimit: 25,
            filterResponses: true,
            minimumResponseFileCount: 1,
            maximumPeerQueueLength: 4,
            minimumPeerUploadSpeed: 5,
            fileLimit: 250,
            removeSingleCharacterSearchTerms: false,
            responseFilter: responseFilter,
            fileFilter: fileFilter,
            stateChanged: stateChanged,
            responseReceived: responseReceived);

        var fallback = options.WithSearchTimeout(5_000);

        Assert.Equal(5_000, fallback.SearchTimeout);
        Assert.Equal(25, fallback.ResponseLimit);
        Assert.True(fallback.FilterResponses);
        Assert.Equal(1, fallback.MinimumResponseFileCount);
        Assert.Equal(4, fallback.MaximumPeerQueueLength);
        Assert.Equal(5, fallback.MinimumPeerUploadSpeed);
        Assert.Equal(250, fallback.FileLimit);
        Assert.False(fallback.RemoveSingleCharacterSearchTerms);
        Assert.Same(responseFilter, fallback.ResponseFilter);
        Assert.Same(fileFilter, fallback.FileFilter);
        Assert.Same(stateChanged, fallback.StateChanged);
        Assert.Same(responseReceived, fallback.ResponseReceived);
    }
}
