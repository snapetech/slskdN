// <copyright file="SearchActionsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search.API;

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Mesh;
using slskd.Search;
using slskd.Search.Providers;
using slskd.Streaming;
using slskd.Transfers.Downloads;
using slskd.Search.API;
using Xunit;

public class SearchActionsControllerTests
{
    [Fact]
    public async Task DownloadItem_WhenSearchMissing_ReturnsSanitizedNotFound()
    {
        var searchId = Guid.NewGuid();
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<slskd.Search.Search, bool>>>(), true))
            .ReturnsAsync((slskd.Search.Search?)null);

        var controller = CreateController(searchService: searchService);

        var result = await controller.DownloadItem(searchId, "0", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Search not found", details.Detail);
        Assert.DoesNotContain(searchId.ToString(), details.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadItem_WhenResponseMissing_ReturnsSanitizedNotFound()
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<slskd.Search.Search, bool>>>(), true))
            .ReturnsAsync(new slskd.Search.Search
            {
                Id = Guid.NewGuid(),
                Responses = Array.Empty<slskd.Search.Response>()
            });

        var controller = CreateController(searchService: searchService);

        var result = await controller.DownloadItem(Guid.NewGuid(), "3", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Search result item not found", details.Detail);
        Assert.DoesNotContain("3", details.Detail ?? string.Empty);
    }

    [Fact]
    public async Task DownloadItem_WhenFileMissing_ReturnsSanitizedNotFound()
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<slskd.Search.Search, bool>>>(), true))
            .ReturnsAsync(new slskd.Search.Search
            {
                Id = Guid.NewGuid(),
                Responses = new[]
                {
                    new slskd.Search.Response
                    {
                        Username = "alice",
                        Files = new[] { new slskd.Search.File { Filename = "song.flac", Size = 123 } },
                        PrimarySource = "scene",
                        SceneContentRef = new SceneContentRef { Username = "alice", Filename = "song.flac", Size = 123 }
                    }
                }
            });

        var controller = CreateController(searchService: searchService);

        var result = await controller.DownloadItem(Guid.NewGuid(), "0:9", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Search result file not found", details.Detail);
        Assert.DoesNotContain("9", details.Detail ?? string.Empty);
    }

    [Fact]
    public async Task DownloadItem_WhenSourceCannotBeDetermined_ReturnsSanitizedBadRequest()
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<slskd.Search.Search, bool>>>(), true))
            .ReturnsAsync(new slskd.Search.Search
            {
                Id = Guid.NewGuid(),
                Responses = new[]
                {
                    new slskd.Search.Response
                    {
                        Username = "alice",
                        Files = new[] { new slskd.Search.File { Filename = "song.flac", Size = 123 } },
                        PrimarySource = string.Empty
                    }
                }
            });

        var controller = CreateController(searchService: searchService);

        var result = await controller.DownloadItem(Guid.NewGuid(), "0", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Cannot determine download source", details.Detail);
        Assert.DoesNotContain("0", details.Detail ?? string.Empty);
    }

    [Fact]
    public async Task DownloadItem_WhenDestinationIsNotConfigured_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.DownloadItem(
            Guid.NewGuid(),
            "0",
            CancellationToken.None,
            "/not-configured");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("invalid_destination", details.Type);
    }

    [Fact]
    public async Task HandlePodDownloadAsync_WhenNoFallbackPeerExists_ReturnsSanitizedNotFound()
    {
        var meshDirectory = new Mock<IMeshDirectory>();
        meshDirectory
            .Setup(directory => directory.FindPeersByContentAsync("sha256:test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MeshPeerDescriptor>());

        var controller = CreateController(meshDirectory: meshDirectory);
        var method = typeof(SearchActionsController).GetMethod("HandlePodDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                "sha256:test",
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                string.Empty,
                null!,
                CancellationToken.None
            })!;

        var result = await task;
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("No pod peers found hosting content", details.Detail);
        Assert.DoesNotContain("sha256:test", details.Detail ?? string.Empty);
    }

    [Theory]
    [InlineData("0", true, 0, 0)]
    [InlineData("2:3", true, 2, 3)]
    [InlineData(" 2 : 3 ", true, 2, 3)]
    [InlineData("0:-1", false, 0, 0)]
    [InlineData("-1", false, 0, 0)]
    [InlineData("-1:0", false, 0, 0)]
    [InlineData("abc", false, 0, 0)]
    [InlineData("1:two", false, 0, 0)]
    public void TryParseItemId_ValidatesResponseAndNonNegativeFileIndex(string itemId, bool expectedResult, int expectedResponseIndex, int expectedFileIndex)
    {
        var method = typeof(SearchActionsController).GetMethod("TryParseItemId", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var args = new object[] { itemId, 0, 0 };
        var result = (bool)method!.Invoke(null, args)!;

        Assert.Equal(expectedResult, result);
        if (expectedResult)
        {
            Assert.Equal(expectedResponseIndex, (int)args[1]);
            Assert.Equal(expectedFileIndex, (int)args[2]);
        }
    }

    [Fact]
    public async Task HandlePodDownloadAsync_WhenFetcherThrows_DoesNotLeakExceptionMessage()
    {
        var meshFetcher = new Mock<IMeshContentFetcher>();
        meshFetcher
            .Setup(fetcher => fetcher.FetchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<string?>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = CreateController(meshFetcher: meshFetcher);
        var method = typeof(SearchActionsController).GetMethod("HandlePodDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                "sha256:test",
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                "peer-1",
                null!,
                CancellationToken.None
            })!;

        var result = await task;
        var error = Assert.IsType<ObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(error.Value);
        Assert.DoesNotContain("sensitive detail", details.Detail ?? string.Empty);
        Assert.Equal("Pod download failed", details.Detail);
    }

    [Fact]
    public async Task HandlePodDownloadAsync_WhenFetcherReturnsError_DoesNotLeakErrorMessage()
    {
        var meshFetcher = new Mock<IMeshContentFetcher>();
        meshFetcher
            .Setup(fetcher => fetcher.FetchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<string?>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeshContentFetchResult
            {
                Error = "sensitive detail",
                Data = null
            });

        var controller = CreateController(meshFetcher: meshFetcher);
        var method = typeof(SearchActionsController).GetMethod("HandlePodDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                "sha256:test",
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                "peer-1",
                null!,
                CancellationToken.None
            })!;

        var result = await task;
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, error.StatusCode);
        var details = Assert.IsType<ProblemDetails>(error.Value);
        Assert.DoesNotContain("sensitive detail", details.Detail ?? string.Empty);
        Assert.Equal("Failed to fetch content from pod peer", details.Detail);
    }

    [Fact]
    public async Task HandlePodDownloadAsync_FetchesPodContentInBoundedChunks()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "slskdn-search-actions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var calls = new List<(long Offset, int Length)>();
        var meshFetcher = new Mock<IMeshContentFetcher>();
        meshFetcher
            .Setup(fetcher => fetcher.FetchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long?>(),
                It.IsAny<string?>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string peerId, string contentId, long? expectedSize, string? expectedHash, long offset, int length, CancellationToken ct) =>
            {
                calls.Add((offset, length));
                return new MeshContentFetchResult
                {
                    Data = new MemoryStream(new byte[length]),
                    Size = length,
                    SizeValid = true,
                    HashValid = true,
                };
            });

        try
        {
            var controller = CreateController(meshFetcher: meshFetcher, incompleteDir: tempDir);
            var method = typeof(SearchActionsController).GetMethod("HandlePodDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task<IActionResult>)method!.Invoke(
                controller,
                new object[]
                {
                    "sha256:test",
                    new slskd.Search.File { Filename = "song.flac", Size = 4097 },
                    "peer-1",
                    null!,
                    CancellationToken.None
                })!;

            var result = await task;

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(new[] { 0L, 2048L, 4096L }, calls.Select(call => call.Offset).ToArray());
            Assert.Equal(new[] { 2048, 2048, 1 }, calls.Select(call => call.Length).ToArray());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HandleSceneDownloadAsync_WhenEnqueueThrows_DoesNotLeakExceptionMessage()
    {
        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = CreateController(downloadService: downloadService);
        var method = typeof(SearchActionsController).GetMethod("HandleSceneDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                new SceneContentRef { Username = "alice", Filename = "Music/song.flac", Size = 1234 },
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                null!,
                CancellationToken.None
            })!;

        var result = await task;
        var error = Assert.IsType<ObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(error.Value);
        Assert.DoesNotContain("sensitive detail", details.Detail ?? string.Empty);
        Assert.Equal("Scene download failed", details.Detail);
    }

    [Fact]
    public async Task HandleSceneDownloadAsync_WhenEnqueueReturnsFailedReasons_DoesNotLeakFailureDetails()
    {
        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<slskd.Transfers.Transfer>(), new List<string> { "alice: sensitive detail" }));

        var controller = CreateController(downloadService: downloadService);
        var method = typeof(SearchActionsController).GetMethod("HandleSceneDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                new SceneContentRef { Username = "alice", Filename = "Music/song.flac", Size = 1234 },
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                null!,
                CancellationToken.None
            })!;

        var result = await task;
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Failed to enqueue scene download", details.Detail);
        Assert.DoesNotContain("sensitive detail", details.Detail ?? string.Empty);
        Assert.DoesNotContain("alice", details.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleSceneDownloadAsync_PreservesExplicitDestination()
    {
        var captured = Array.Empty<DownloadEnqueueRequest>();
        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(service => service.EnqueueAsync(
                "alice",
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .Callback((string _, IEnumerable<DownloadEnqueueRequest> requests, CancellationToken _) =>
                captured = requests.ToArray())
            .ReturnsAsync((
                new List<slskd.Transfers.Transfer>
                {
                    new slskd.Transfers.Transfer { Id = Guid.NewGuid() },
                },
                new List<string>()));

        var controller = CreateController(downloadService: downloadService);
        var method = typeof(SearchActionsController).GetMethod("HandleSceneDownloadAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = (Task<IActionResult>)method!.Invoke(
            controller,
            new object[]
            {
                new SceneContentRef { Username = "alice", Filename = "Music/song.flac", Size = 1234 },
                new slskd.Search.File { Filename = "song.flac", Size = 1234 },
                "/tmp/music",
                CancellationToken.None
            })!;

        Assert.IsType<OkObjectResult>(await task);
        Assert.Single(captured);
        Assert.Equal("/tmp/music", captured[0].DestinationDirectory);
    }

    private static SearchActionsController CreateController(
        Mock<ISearchService>? searchService = null,
        Mock<IMeshContentFetcher>? meshFetcher = null,
        Mock<IDownloadService>? downloadService = null,
        Mock<IMeshDirectory>? meshDirectory = null,
        string? incompleteDir = null)
    {
        var options = new Mock<IOptionsMonitor<slskd.Options>>();
        options.SetupGet(x => x.CurrentValue).Returns(new slskd.Options
        {
            Directories = new slskd.Options.DirectoriesOptions
            {
                Downloads = incompleteDir ?? "/tmp",
                Incomplete = incompleteDir ?? "/tmp",
            }
        });

        return new SearchActionsController(
            (searchService ?? new Mock<ISearchService>()).Object,
            (downloadService ?? new Mock<IDownloadService>()).Object,
            Mock.Of<IContentLocator>(),
            (meshFetcher ?? new Mock<IMeshContentFetcher>()).Object,
            Mock.Of<IMeshStreamTicketService>(),
            (meshDirectory ?? new Mock<IMeshDirectory>()).Object,
            options.Object,
            NullLogger<SearchActionsController>.Instance);
    }
}
