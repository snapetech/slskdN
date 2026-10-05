// <copyright file="MeshSearchRpcHandlerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous.Search;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.DhtRendezvous.Messages;
using slskd.DhtRendezvous.Search;
using slskd.Shares;
using slskd.Tests.Unit.TestHelpers;
using Soulseek;
using Xunit;
using NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<slskd.DhtRendezvous.Search.MeshSearchRpcHandler>;

public class MeshSearchRpcHandlerTests
{
    private readonly Mock<IShareService> _shareServiceMock = new();
    private readonly ILogger<MeshSearchRpcHandler> _logger = NullLogger.Instance;

    private MeshSearchRpcHandler CreateHandler()
    {
        return new MeshSearchRpcHandler(_shareServiceMock.Object, _logger);
    }

    [Fact]
    public async Task HandleAsync_QueryTooLong_ReturnsError()
    {
        var handler = CreateHandler();
        var request = new MeshSearchRequestMessage
        {
            RequestId = "req1",
            SearchText = new string('a', 300), // Exceeds 256 char limit
            MaxResults = 10
        };

        var response = await handler.HandleAsync(request, CancellationToken.None);

        Assert.NotNull(response.Error);
        Assert.Contains("Query too long", response.Error);
        Assert.Empty(response.Files);
    }

    [Fact]
    public async Task HandleAsync_QueryWithinLimit_Success()
    {
        var handler = CreateHandler();
        var request = new MeshSearchRequestMessage
        {
            RequestId = "req1",
            SearchText = "test query",
            MaxResults = 10
        };
        var files = new List<Soulseek.File>
        {
            new Soulseek.File(1, "test.mp3", 1000, ".mp3", null)
        };
        _shareServiceMock.Setup(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .ReturnsAsync(files);

        var response = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Null(response.Error);
        Assert.Single(response.Files);
        Assert.Equal("test.mp3", response.Files[0].Filename);
    }

    [Theory]
    [InlineData("Linkin Park Meteora")]
    [InlineData("Metallica")]
    public async Task HandleAsync_ForwardsTermsToLocalMeshIndexWithoutSoulseekFiltering(string searchText)
    {
        SearchQuery? capturedQuery = null;
        var request = new MeshSearchRequestMessage
        {
            RequestId = "req-term-filter",
            SearchText = searchText,
            MaxResults = 10,
        };
        var file = new Soulseek.File(1, $"{searchText}\\track.flac", 1_000, ".flac", null);

        _shareServiceMock
            .Setup(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .Callback<SearchQuery>(query => capturedQuery = query)
            .ReturnsAsync(new[] { file });

        var response = await CreateHandler().HandleAsync(request, CancellationToken.None);

        Assert.Null(response.Error);
        Assert.Single(response.Files);
        Assert.Equal(request.SearchText, capturedQuery?.SearchText);
        Assert.Equal(file.Filename, response.Files[0].Filename);
    }

    [Fact]
    public async Task HandleAsync_IncludesMediaKinds()
    {
        var handler = CreateHandler();
        var request = new MeshSearchRequestMessage
        {
            RequestId = "req1",
            SearchText = "test",
            MaxResults = 10
        };
        var files = new List<Soulseek.File>
        {
            new Soulseek.File(1, "song.mp3", 1000, ".mp3", null),
            new Soulseek.File(1, "video.mp4", 2000, ".mp4", null),
            new Soulseek.File(1, "image.jpg", 500, ".jpg", null)
        };
        _shareServiceMock.Setup(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .ReturnsAsync(files);

        var response = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(3, response.Files.Count);
        var mp3 = response.Files.First(f => f.Filename == "song.mp3");
        Assert.NotNull(mp3.MediaKinds);
        Assert.Contains("Music", mp3.MediaKinds);

        var mp4 = response.Files.First(f => f.Filename == "video.mp4");
        Assert.NotNull(mp4.MediaKinds);
        Assert.Contains("Video", mp4.MediaKinds);

        var jpg = response.Files.First(f => f.Filename == "image.jpg");
        Assert.NotNull(jpg.MediaKinds);
        Assert.Contains("Image", jpg.MediaKinds);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellationBeforeSearch_Propagates()
    {
        var handler = CreateHandler();
        var request = new MeshSearchRequestMessage
        {
            RequestId = "req1",
            SearchText = "test",
            MaxResults = 10
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(request, cts.Token));

        _shareServiceMock.Verify(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CallerCancellationDuringSearch_StopsWaitingAndPropagates()
    {
        var searchTask = new TaskCompletionSource<IEnumerable<Soulseek.File>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _shareServiceMock
            .Setup(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .Returns(searchTask.Task);
        using var cts = new CancellationTokenSource();
        var handlerTask = CreateHandler().HandleAsync(new MeshSearchRequestMessage
        {
            RequestId = "req-cancel",
            SearchText = "test",
            MaxResults = 10,
        }, cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handlerTask);
        _shareServiceMock.Verify(x => x.GetLocalRepository(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SearchFailureEscapesRequestIdAndExceptionInLogs()
    {
        var logger = new CapturingLogger<MeshSearchRpcHandler>();
        var requestId = $"\r\n{Guid.NewGuid():D}";
        var request = new MeshSearchRequestMessage
        {
            RequestId = requestId,
            SearchText = "test",
            MaxResults = 10,
        };
        Assert.True(Guid.TryParse(requestId, out _));
        _shareServiceMock
            .Setup(x => x.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .Returns(Task.FromException<IEnumerable<Soulseek.File>>(new InvalidOperationException("remote\r\nforged")));

        var response = await new MeshSearchRpcHandler(_shareServiceMock.Object, logger).HandleAsync(request);

        Assert.Equal(requestId, response.RequestId);
        Assert.Equal("Search failed", response.Error);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Contains("\\r\\n" + requestId[2..], entry.Message, StringComparison.Ordinal);
        Assert.Contains("remote\\r\\nforged", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ContentLookupFailureEscapesFilenameAndExceptionInLogs()
    {
        var logger = new CapturingLogger<MeshSearchRpcHandler>();
        var fileName = "folder\r\nforged.flac";
        var repository = new Mock<IShareRepository>();
        repository
            .Setup(r => r.ListContentItemsForFile(fileName))
            .Throws(new InvalidOperationException("database\r\nforged"));
        _shareServiceMock
            .Setup(s => s.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new[] { new Soulseek.File(1, fileName, 1_000, ".flac", null) });
        _shareServiceMock.Setup(s => s.GetLocalRepository()).Returns(repository.Object);

        var response = await new MeshSearchRpcHandler(_shareServiceMock.Object, logger).HandleAsync(new MeshSearchRequestMessage
        {
            RequestId = "request-content-lookup",
            SearchText = "track",
            MaxResults = 10,
        });

        Assert.Equal(fileName, Assert.Single(response.Files).Filename);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Contains("folder\\r\\nforged.flac", entry.Message, StringComparison.Ordinal);
        Assert.Contains("database\\r\\nforged", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_StopsContentMappingEnumerationAtFirstAdvertisableItem()
    {
        var repository = new Mock<IShareRepository>();
        var enumerated = 0;

        IEnumerable<(string ContentId, string Domain, string WorkId, bool IsAdvertisable, string ModerationReason)> ContentItems()
        {
            for (var index = 0; index < 1_000; index++)
            {
                enumerated++;
                yield return ($"content-{index}", "audio", string.Empty, index == 0, string.Empty);
            }
        }

        repository
            .Setup(r => r.ListContentItemsForFile("song.flac"))
            .Returns(ContentItems());
        _shareServiceMock
            .Setup(s => s.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new[] { new Soulseek.File(1, "song.flac", 1_000, ".flac", null) });
        _shareServiceMock
            .Setup(s => s.GetLocalRepository())
            .Returns(repository.Object);

        var response = await CreateHandler().HandleAsync(new MeshSearchRequestMessage
        {
            RequestId = "request-1",
            SearchText = "song",
            MaxResults = 10,
        });

        Assert.Equal("content-0", Assert.Single(response.Files).ContentId);
        Assert.Equal(1, enumerated);
    }

    [Fact]
    public async Task HandleAsync_NoAdvertisableMappingRetainsFirstFallbackWithoutBuffering()
    {
        var repository = new Mock<IShareRepository>();
        var contentItems = new[]
        {
            (ContentId: "first", Domain: "audio", WorkId: string.Empty, IsAdvertisable: false, ModerationReason: string.Empty),
            (ContentId: "second", Domain: "audio", WorkId: string.Empty, IsAdvertisable: false, ModerationReason: string.Empty),
        };

        repository
            .Setup(r => r.ListContentItemsForFile("song.flac"))
            .Returns(contentItems);
        _shareServiceMock
            .Setup(s => s.SearchLocalAsync(It.IsAny<SearchQuery>()))
            .ReturnsAsync(new[] { new Soulseek.File(1, "song.flac", 1_000, ".flac", null) });
        _shareServiceMock
            .Setup(s => s.GetLocalRepository())
            .Returns(repository.Object);

        var response = await CreateHandler().HandleAsync(new MeshSearchRequestMessage
        {
            RequestId = "request-1",
            SearchText = "song",
            MaxResults = 10,
        });

        Assert.Equal("first", Assert.Single(response.Files).ContentId);
    }
}
