// <copyright file="LibraryItemsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.API.Native;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.API.Native;
using slskd.HashDb;
using slskd.HashDb.Models;
using slskd.Shares;
using Soulseek;
using Xunit;

/// <summary>
/// Unit tests for LibraryItemsController (library search API for E2E and Collections UI).
/// </summary>
[Collection(AllocationTestCollection.Name)]
public class LibraryItemsControllerTests
{
    private readonly Mock<IShareService> shareServiceMock;
    private readonly Mock<IHashDbService> hashDbServiceMock;
    private readonly Mock<ILogger<LibraryItemsController>> loggerMock;
    private readonly Mock<IShareRepository> shareRepositoryMock;
    private readonly LibraryItemsController controller;

    public LibraryItemsControllerTests()
    {
        shareServiceMock = new Mock<IShareService>();
        hashDbServiceMock = new Mock<IHashDbService>();
        loggerMock = new Mock<ILogger<LibraryItemsController>>();
        shareRepositoryMock = new Mock<IShareRepository>();

        // GetLocalRepository() is called by BuildCodeToMaskedFilenameMap() on every action
        shareRepositoryMock
            .Setup(x => x.ListFiles(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Enumerable.Empty<Soulseek.File>());
        shareServiceMock
            .Setup(x => x.GetLocalRepository())
            .Returns(shareRepositoryMock.Object);
        hashDbServiceMock
            .Setup(x => x.LookupHashesByFlacKeysAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HashDbEntry>());

        controller = new LibraryItemsController(
            shareServiceMock.Object,
            hashDbServiceMock.Object,
            loggerMock.Object);

        // Set up controller context with authenticated user (required for [Authorize])
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "testuser")
                }, "test"))
            }
        };
    }

    [Fact]
    public async Task SearchItems_NoQuery_ReturnsAllFiles()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/song1.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Music/song2.flac", 2048, ".flac")
            }),
            new Soulseek.Directory("Movies", new List<Soulseek.File>
            {
                new Soulseek.File(3, "Movies/movie.mp4", 4096, ".mp4")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        Assert.NotNull(itemsProp);

        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task SearchItems_WithQuery_FiltersByFilename()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/sintel.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Music/other.mp3", 2048, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: "sintel", kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Single(items);

        var itemType = items[0].GetType();
        var fileNameProp = itemType.GetProperty("FileName");
        var fileName = fileNameProp?.GetValue(items[0]) as string;
        Assert.Contains("sintel", fileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchItems_WithWhitespacePaddedQuery_FiltersByTrimmedFilename()
    {
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/sintel.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Music/other.mp3", 2048, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        var result = await controller.SearchItems(query: "  sintel  ", kinds: null, limit: 100, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value!.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp!.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Single(items);
    }

    [Fact]
    public async Task SearchItems_WithKinds_FiltersByMediaKind()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Media", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Media/song.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Media/movie.mp4", 2048, ".mp4"),
                new Soulseek.File(3, "Media/book.txt", 512, ".txt")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: null, kinds: "Audio", limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Single(items);

        var itemType = items[0].GetType();
        var mediaKindProp = itemType.GetProperty("MediaKind");
        var mediaKind = mediaKindProp?.GetValue(items[0]) as string;
        Assert.Equal("Audio", mediaKind);
    }

    [Fact]
    public async Task SearchItems_WhenBrowseThrows_DoesNotLeakExceptionMessage()
    {
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetItem_WithWhitespaceContentId_ReturnsBadRequest()
    {
        var result = await controller.GetItem("   ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task SearchItems_WithMultipleKinds_ReturnsMatchingFiles()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Media", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Media/song.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Media/movie.mp4", 2048, ".mp4"),
                new Soulseek.File(3, "Media/book.txt", 512, ".txt")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: null, kinds: "Audio,Video", limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetItem_WhenBrowseThrows_DoesNotLeakExceptionMessage()
    {
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var result = await controller.GetItem("sha256:test", CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task SearchItems_WithLimit_RespectsLimit()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", Enumerable.Range(1, 10)
                .Select(i => new Soulseek.File(i, $"Music/song{i}.mp3", 1024, ".mp3"))
                .ToList())
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: null, kinds: null, limit: 5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Equal(5, items.Count);
    }

    [Fact]
    public async Task SearchItems_WithOneHundredFiles_UsesOneBatchHashLookup()
    {
        const int fileCount = 100;
        var directories = new List<Soulseek.Directory>
        {
            new("Music", Enumerable.Range(1, fileCount)
                .Select(index => new Soulseek.File(index, $"Music/song-{index}.mp3", index, ".mp3"))
                .ToList()),
        };
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);
        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", $"/missing/{filename}", long.Parse(Path.GetFileNameWithoutExtension(filename).Split('-')[1])));
        hashDbServiceMock
            .Setup(x => x.LookupHashesByFlacKeysAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) => keys
                .Select((key, index) => new HashDbEntry
                {
                    FlacKey = key,
                    FileSha256 = $"sha-{index}",
                })
                .ToList());

        var result = await controller.SearchItems(limit: fileCount, cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var itemsProperty = okResult.Value!.GetType().GetProperty("items");
        var items = (itemsProperty!.GetValue(okResult.Value) as System.Collections.IEnumerable)!.Cast<object>().ToList();
        Assert.Equal(fileCount, items.Count);
        Assert.All(items, item => Assert.StartsWith(
            "sha256:sha-",
            item.GetType().GetProperty("ContentId")!.GetValue(item) as string));
        hashDbServiceMock.Verify(service => service.LookupHashesByFlacKeysAsync(
            It.Is<IEnumerable<string>>(keys => keys.Count() == fileCount),
            It.IsAny<CancellationToken>()), Times.Once);
        hashDbServiceMock.Verify(service => service.LookupHashAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrowseItems_ListsChildFoldersAndPagedFilesForPath()
    {
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music"),
            new Soulseek.Directory("Music\\Artist", new List<Soulseek.File>
            {
                new Soulseek.File(1, "song1.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "song2.flac", 2048, ".flac")
            }),
            new Soulseek.Directory("Music\\Artist\\Live", new List<Soulseek.File>
            {
                new Soulseek.File(3, "song3.ogg", 1024, ".ogg")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        var result = await controller.BrowseItems(
            path: "Music/Artist",
            query: null,
            kinds: "Audio",
            limit: 1,
            offset: 0,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value!.GetType();
        var directoriesProp = responseType.GetProperty("directories");
        var filesProp = responseType.GetProperty("files");
        var hasMoreProp = responseType.GetProperty("hasMore");
        var totalFilesProp = responseType.GetProperty("totalFiles");

        var browserDirectories = (directoriesProp!.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        var files = (filesProp!.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();

        Assert.NotNull(browserDirectories);
        Assert.Single(browserDirectories);
        Assert.NotNull(files);
        Assert.Single(files);
        Assert.True((bool)hasMoreProp!.GetValue(okResult.Value)!);
        Assert.Equal(2, (int)totalFilesProp!.GetValue(okResult.Value)!);

        var fileType = files[0].GetType();
        Assert.Equal("Music\\Artist\\song1.mp3", fileType.GetProperty("Path")?.GetValue(files[0]));
        hashDbServiceMock.Verify(service => service.LookupHashesByFlacKeysAsync(
            It.Is<IEnumerable<string>>(keys => keys.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        hashDbServiceMock.Verify(service => service.LookupHashAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BrowseItems_WithQuery_CollapsesDuplicateSearchResults()
    {
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music\\One", new List<Soulseek.File>
            {
                new Soulseek.File(1, "same-song.mp3", 1024, ".mp3")
            }),
            new Soulseek.Directory("Music\\Two", new List<Soulseek.File>
            {
                new Soulseek.File(2, "same-song.mp3", 1024, ".mp3")
            }),
            new Soulseek.Directory("Music\\Three", new List<Soulseek.File>
            {
                new Soulseek.File(3, "same-song.mp3", 2048, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        var result = await controller.BrowseItems(
            path: null,
            query: "same-song",
            kinds: "Audio",
            limit: 100,
            offset: 0,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value!.GetType();
        var filesProp = responseType.GetProperty("files");
        var duplicatesRemovedProp = responseType.GetProperty("duplicatesRemoved");
        var files = (filesProp!.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();

        Assert.NotNull(files);
        Assert.Equal(2, files.Count);
        Assert.Equal(1, (int)duplicatesRemovedProp!.GetValue(okResult.Value)!);

        var duplicateFile = files.First(file =>
            (int)file.GetType().GetProperty("DuplicateCount")!.GetValue(file)! == 2);
        Assert.Equal(
            "same-song.mp3",
            duplicateFile.GetType().GetProperty("FileName")?.GetValue(duplicateFile));
    }

    [Fact]
    public async Task BrowseItems_WideSearchPageHasBoundedAllocation()
    {
        const int fileCount = 10_000;
        const int offset = 25;
        const int limit = 50;
        var directories = new List<Soulseek.Directory>
        {
            new("Music", Enumerable.Range(0, fileCount)
                .Select(index =>
                {
                    var fileIndex = fileCount - index - 1;
                    return new Soulseek.File(index + 1, $"track-{fileIndex:D5}.mp3", fileIndex + 1, ".mp3");
                })
                .ToList()),
        };
        shareServiceMock
            .Setup(service => service.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);
        shareServiceMock
            .Setup(service => service.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", $"/missing/{filename}", 1L));
        hashDbServiceMock
            .Setup(service => service.LookupHashesByFlacKeysAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) => keys
                .Select((key, index) => new HashDbEntry
                {
                    FlacKey = key,
                    FileSha256 = $"sha-{index}",
                })
                .ToList());
        _ = await controller.BrowseItems(query: "track", kinds: null, limit: limit, offset: offset);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await controller.BrowseItems(query: "track", kinds: null, limit: limit, offset: offset);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        var response = Assert.IsType<OkObjectResult>(result).Value!;
        var responseType = response.GetType();
        var files = ((System.Collections.IEnumerable)responseType.GetProperty("files")!.GetValue(response)!)
            .Cast<object>()
            .ToList();
        Assert.Equal(limit, files.Count);
        Assert.Equal("Music\\track-00025.mp3", files[0].GetType().GetProperty("Path")!.GetValue(files[0]));
        Assert.Equal("Music\\track-00074.mp3", files[^1].GetType().GetProperty("Path")!.GetValue(files[^1]));
        Assert.Equal(fileCount, responseType.GetProperty("totalFiles")!.GetValue(response));
        Assert.Equal(0, responseType.GetProperty("duplicatesRemoved")!.GetValue(response));
        Assert.True((bool)responseType.GetProperty("hasMore")!.GetValue(response)!);
        Assert.True(
            allocatedBytes < 4_750_000,
            $"Library browser page allocated {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void BuildDirectoryEntries_WithTenThousandRoots_EnumeratesInputOnce()
    {
        const int rootCount = 10_000;
        var directories = Enumerable.Range(0, rootCount)
            .Select(index => new Soulseek.Directory($"Root-{index:D5}", new List<Soulseek.File>
            {
                new(index, $"track-{index}.flac", index + 1, ".flac"),
            }))
            .Append(new Soulseek.Directory("Root-00000\\Child", new List<Soulseek.File>()))
            .Append(new Soulseek.Directory("Root-00000/Child", new List<Soulseek.File>()))
            .ToList();
        var counted = new CountingReadOnlyList<Soulseek.Directory>(directories);
        var method = typeof(LibraryItemsController).GetMethod(
            "BuildDirectoryEntries",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = Assert.IsAssignableFrom<System.Collections.IEnumerable>(method!.Invoke(null, new object[] { counted, string.Empty }));
        var entries = result.Cast<object>().ToList();

        Assert.Equal(rootCount, entries.Count);
        Assert.Equal(1, counted.EnumerationCount);
        Assert.Equal(rootCount + 2, counted.VisitedCount);
        var first = entries[0];
        Assert.Equal("Root-00000", first.GetType().GetProperty("Path")!.GetValue(first));
        Assert.Equal(1, first.GetType().GetProperty("FileCount")!.GetValue(first));
        Assert.Equal(2, first.GetType().GetProperty("ChildDirectoryCount")!.GetValue(first));
    }

    [Fact]
    public void NormalizeVirtualPath_ReusesCanonicalInputAndPreservesCleanup()
    {
        var method = typeof(LibraryItemsController).GetMethod(
            "NormalizeVirtualPath",
            BindingFlags.NonPublic | BindingFlags.Static);
        var canonical = new string("Music\\Artist\\Track.mp3".ToCharArray());

        var unchanged = method!.Invoke(null, new object?[] { canonical });

        Assert.Same(canonical, unchanged);
        Assert.Equal("Music\\Artist\\Track.mp3", method.Invoke(null, new object?[] { " / Music // Artist / Track.mp3 / " }));
        Assert.Equal("Music\\Artist", method.Invoke(null, new object?[] { "Music\\\\Artist" }));
        Assert.Equal("Music Folder\\Artist Name", method.Invoke(null, new object?[] { " Music Folder \\ Artist Name " }));
        Assert.Equal(string.Empty, method.Invoke(null, new object?[] { " \t " }));
    }

    [Theory]
    [InlineData("Music\\Björk\\SÓNG.FLAC", "sóng")]
    [InlineData("Music\\Straße\\Track.flac", "STRASSE")]
    [InlineData("Music\\ΟΣ\\Track.flac", "ος")]
    [InlineData("Music\\İstanbul\\Track.flac", "istanbul")]
    public void ContainsLowerInvariant_MatchesLegacyInvariantSearch(string value, string query)
    {
        var method = typeof(LibraryItemsController).GetMethod(
            "ContainsLowerInvariant",
            BindingFlags.NonPublic | BindingFlags.Static);
        var lowerQuery = query.ToLowerInvariant();

        var actual = method!.Invoke(null, new object[] { value, lowerQuery });

        Assert.Equal(value.ToLowerInvariant().Contains(lowerQuery), actual);
    }

    [Fact]
    public async Task SearchItems_EmptyShares_ReturnsEmptyList()
    {
        // Arrange
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(new List<Soulseek.Directory>());

        // Act
        var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task SearchItems_WithSha256FromHashDb_UsesSha256ContentId()
    {
        // Arrange
        var testSha256 = "abc123def456";
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/song.mp3", 1024, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        var testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.mp3");
        try
        {
            System.IO.File.WriteAllBytes(testFilePath, new byte[] { 1, 2, 3 });

            shareServiceMock
                .Setup(x => x.ResolveFileAsync("Music/song.mp3"))
                .ReturnsAsync(("local", testFilePath, 1024L));

            var flacKey = HashDbEntry.GenerateFlacKey(testFilePath, 1024);
            hashDbServiceMock
                .Setup(x => x.LookupHashesByFlacKeysAsync(
                    It.Is<IEnumerable<string>>(keys => keys.SequenceEqual(new[] { flacKey })),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HashDbEntry>
                {
                    new()
                    {
                        FlacKey = flacKey,
                        FileSha256 = testSha256,
                    },
                });

            // Act
            var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseType = okResult.Value.GetType();
            var itemsProp = responseType.GetProperty("items");
            var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
            Assert.NotNull(items);
            Assert.Single(items);

            var itemType = items[0].GetType();
            var contentIdProp = itemType.GetProperty("ContentId");
            var contentId = contentIdProp?.GetValue(items[0]) as string;
            Assert.Equal($"sha256:{testSha256}", contentId);

            var sha256Prop = itemType.GetProperty("Sha256");
            var sha256 = sha256Prop?.GetValue(items[0]) as string;
            Assert.Equal(testSha256, sha256);
        }
        finally
        {
            try { System.IO.File.Delete(testFilePath); } catch { }
        }
    }

    [Fact]
    public async Task SearchItems_OnError_Returns500()
    {
        // Arrange
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetItem_ValidContentId_ReturnsItem()
    {
        // Arrange
        var testSha256 = "abc123def456";
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/song.mp3", 1024, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        var testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.mp3");
        try
        {
            System.IO.File.WriteAllBytes(testFilePath, new byte[] { 1, 2, 3 });

            shareServiceMock
                .Setup(x => x.ResolveFileAsync("Music/song.mp3"))
                .ReturnsAsync(("local", testFilePath, 1024L));

            var flacKey = HashDbEntry.GenerateFlacKey(testFilePath, 1024);
            hashDbServiceMock
                .Setup(x => x.LookupHashAsync(flacKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashDbEntry
                {
                    FileSha256 = testSha256
                });

            var contentId = $"sha256:{testSha256}";

            // Act
            var result = await controller.GetItem(contentId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var itemType = okResult.Value.GetType();
            var contentIdProp = itemType.GetProperty("ContentId");
            var returnedContentId = contentIdProp?.GetValue(okResult.Value) as string;
            Assert.Equal(contentId, returnedContentId);
        }
        finally
        {
            try { System.IO.File.Delete(testFilePath); } catch { }
        }
    }

    [Fact]
    public async Task GetItem_InvalidContentId_ReturnsNotFound()
    {
        // Arrange
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(new List<Soulseek.Directory>());

        // Act
        var result = await controller.GetItem("sha256:nonexistent", CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var responseType = notFoundResult.Value.GetType();
        var errorProp = responseType.GetProperty("error");
        var error = errorProp?.GetValue(notFoundResult.Value) as string;
        Assert.Equal("Item not found", error);
    }

    [Fact]
    public async Task GetItem_OnError_Returns500()
    {
        // Arrange
        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await controller.GetItem("sha256:test", CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task SearchItems_QueryCaseInsensitive_MatchesCorrectly()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/SINTEL.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Music/other.mp3", 2048, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act - lowercase query should match uppercase filename
        var result = await controller.SearchItems(query: "sintel", kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Single(items);
    }

    [Fact]
    public async Task SearchItems_MediaKindMapping_CorrectlyIdentifiesKinds()
    {
        // Arrange
        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Media", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Media/song.mp3", 1024, ".mp3"),
                new Soulseek.File(2, "Media/song.flac", 2048, ".flac"),
                new Soulseek.File(3, "Media/song.ogg", 512, ".ogg"),
                new Soulseek.File(4, "Media/movie.mp4", 4096, ".mp4"),
                new Soulseek.File(5, "Media/movie.mkv", 8192, ".mkv"),
                new Soulseek.File(6, "Media/book.txt", 256, ".txt"),
                new Soulseek.File(7, "Media/book.pdf", 512, ".pdf"),
                new Soulseek.File(8, "Media/unknown.xyz", 128, ".xyz")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        shareServiceMock
            .Setup(x => x.ResolveFileAsync(It.IsAny<string>()))
            .ReturnsAsync((string filename) => ("local", filename, 1024L));

        // Act
        var result = await controller.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Equal(8, items.Count);

        var itemType = items[0].GetType();
        var mediaKindProp = itemType.GetProperty("MediaKind");

        // Check Audio files
        var audioFiles = items.Where(i =>
        {
            var fileName = (itemType.GetProperty("FileName")?.GetValue(i) as string) ?? string.Empty;
            return fileName.Contains("song");
        }).ToList();
        foreach (var item in audioFiles)
        {
            var mediaKind = mediaKindProp?.GetValue(item) as string;
            Assert.Equal("Audio", mediaKind);
        }

        // Check Video files
        var videoFiles = items.Where(i =>
        {
            var fileName = (itemType.GetProperty("FileName")?.GetValue(i) as string) ?? string.Empty;
            return fileName.Contains("movie");
        }).ToList();
        foreach (var item in videoFiles)
        {
            var mediaKind = mediaKindProp?.GetValue(item) as string;
            Assert.Equal("Video", mediaKind);
        }

        // Check Book files
        var bookFiles = items.Where(i =>
        {
            var fileName = (itemType.GetProperty("FileName")?.GetValue(i) as string) ?? string.Empty;
            return fileName.Contains("book");
        }).ToList();
        foreach (var item in bookFiles)
        {
            var mediaKind = mediaKindProp?.GetValue(item) as string;
            Assert.Equal("Book", mediaKind);
        }

        // Check unknown file
        var unknownFile = items.FirstOrDefault(i =>
        {
            var fileName = (itemType.GetProperty("FileName")?.GetValue(i) as string) ?? string.Empty;
            return fileName.Contains("unknown");
        });
        Assert.NotNull(unknownFile);
        var unknownMediaKind = mediaKindProp?.GetValue(unknownFile) as string;
        Assert.Equal("File", unknownMediaKind);
    }

    [Fact]
    public async Task SearchItems_WithoutHashDb_GeneratesPathBasedContentId()
    {
        // Arrange - controller without HashDb service
        var controllerWithoutHashDb = new LibraryItemsController(
            shareServiceMock.Object,
            hashDbService: null, // No HashDb
            loggerMock.Object);

        controllerWithoutHashDb.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "testuser")
                }, "test"))
            }
        };

        var directories = new List<Soulseek.Directory>
        {
            new Soulseek.Directory("Music", new List<Soulseek.File>
            {
                new Soulseek.File(1, "Music/song.mp3", 1024, ".mp3")
            })
        };

        shareServiceMock
            .Setup(x => x.BrowseAsync(It.IsAny<slskd.Shares.Share>()))
            .ReturnsAsync(directories);

        // Use a path that doesn't exist to test path-based fallback (when file doesn't exist, SHA256 can't be computed)
        var testFilePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.mp3");

        shareServiceMock
            .Setup(x => x.ResolveFileAsync("Music/song.mp3"))
            .ReturnsAsync(("local", testFilePath, 1024L));

        // Act
        var result = await controllerWithoutHashDb.SearchItems(query: null, kinds: null, limit: 100, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseType = okResult.Value.GetType();
        var itemsProp = responseType.GetProperty("items");
        var items = (itemsProp.GetValue(okResult.Value) as System.Collections.IEnumerable)?.Cast<object>().ToList();
        Assert.NotNull(items);
        Assert.Single(items);

        var itemType = items[0].GetType();
        var contentIdProp = itemType.GetProperty("ContentId");
        var contentId = contentIdProp?.GetValue(items[0]) as string;
        Assert.StartsWith("path:", contentId); // Should use path-based fallback when file doesn't exist
    }

    private sealed class CountingReadOnlyList<T>(IReadOnlyList<T> items) : IReadOnlyList<T>
    {
        public int Count => items.Count;

        public int EnumerationCount { get; private set; }

        public int VisitedCount { get; private set; }

        public T this[int index] => items[index];

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            foreach (var item in items)
            {
                VisitedCount++;
                yield return item;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
