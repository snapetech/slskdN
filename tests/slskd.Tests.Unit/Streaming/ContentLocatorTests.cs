// <copyright file="ContentLocatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Shares;
using slskd.Streaming;
using Xunit;

public class ContentLocatorTests
{
    private readonly Mock<IShareService> _shareServiceMock = new();
    private readonly Mock<IShareRepository> _repoMock = new();
    private readonly Mock<ILogger<ContentLocator>> _logMock = new();
    private readonly Mock<IOptionsMonitor<slskd.Options>> _optionsMock = new();

    private ContentLocator CreateLocator(slskd.Options? options = null)
    {
        _shareServiceMock.Setup(x => x.GetLocalRepository()).Returns(_repoMock.Object);
        if (options == null)
        {
            return new ContentLocator(_shareServiceMock.Object, _logMock.Object);
        }

        _optionsMock.Setup(x => x.CurrentValue).Returns(options);
        return new ContentLocator(_shareServiceMock.Object, _logMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void Resolve_EmptyContentId_ReturnsNull()
    {
        var locator = CreateLocator();
        Assert.Null(locator.Resolve(""));
        Assert.Null(locator.Resolve("   "));
        Assert.Null(locator.Resolve(null!));
    }

    [Fact]
    public void Resolve_ContentItemNotFound_ReturnsNull()
    {
        var locator = CreateLocator();
        _repoMock.Setup(x => x.FindContentItem("c1")).Returns((ValueTuple<string, string, string, bool, string, long>?)null);

        var r = locator.Resolve("c1");

        Assert.Null(r);
    }

    [Fact]
    public void Resolve_ContentItemNotAdvertisable_ReturnsNull()
    {
        var locator = CreateLocator();
        _repoMock.Setup(x => x.FindContentItem("c1")).Returns(("Music", "w1", "masked.flac", false, "blocked", 0L));

        var r = locator.Resolve("c1");

        Assert.Null(r);
    }

    [Fact]
    public void Resolve_ContentItemNotAdvertisable_DoesNotUseAllowedRootFallback()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N")[..8]);
        var path = Path.Combine(root, "blocked.flac");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
            var contentId = $"path:{slskd.Compute.Sha256Hash($"{path}|4")}";
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = root,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem(contentId))
                .Returns(("Music", "w1", path, false, "blocked", 0L));

            var r = locator.Resolve(contentId);

            Assert.Null(r);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Resolve_FileInfoNotFound_ReturnsNull()
    {
        var locator = CreateLocator();
        _repoMock.Setup(x => x.FindContentItem("c1")).Returns(("Music", "w1", "masked.flac", true, "", 0L));
        _repoMock.Setup(x => x.FindFileInfo("masked.flac")).Returns((Filename: "", Size: 0));

        var r = locator.Resolve("c1");

        Assert.Null(r);
    }

    [Fact]
    public void Resolve_FileNotOnDisk_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = root,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem("c1"))
                .Returns(("Music", "w1", "masked.flac", true, "", 0L));
            _repoMock.Setup(x => x.FindFileInfo("masked.flac"))
                .Returns((Filename: Path.Combine(root, "missing.flac"), Size: 1000));

            var r = locator.Resolve("c1");

            Assert.Null(r);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_RepositoryFileOutsideAllowedRoots_ReturnsNull()
    {
        var allowedRoot = Path.Combine(Path.GetTempPath(), "ContentLocAllowed_" + Guid.NewGuid().ToString("N"));
        var outsideRoot = Path.Combine(Path.GetTempPath(), "ContentLocOutside_" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(outsideRoot, "outside.flac");
        try
        {
            Directory.CreateDirectory(allowedRoot);
            Directory.CreateDirectory(outsideRoot);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = allowedRoot,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem("c1"))
                .Returns(("Music", "w1", "masked.flac", true, string.Empty, 0L));
            _repoMock.Setup(x => x.FindFileInfo("masked.flac"))
                .Returns((Filename: path, Size: 3));

            Assert.Null(locator.Resolve("c1"));
        }
        finally
        {
            try { Directory.Delete(allowedRoot, recursive: true); } catch { }
            try { Directory.Delete(outsideRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Resolve_Success_ReturnsResolvedContent()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "track.mp3");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = root,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem("c1")).Returns(("Music", "w1", "masked.flac", true, "", 0L));
            _repoMock.Setup(x => x.FindFileInfo("masked.flac")).Returns((Filename: path, Size: 3));

            var r = locator.Resolve("c1");

            Assert.NotNull(r);
            Assert.Equal(path, r.AbsolutePath);
            Assert.Equal(3, r.Length);
            Assert.Equal("audio/mpeg", r.ContentType);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Resolve_DetectsMimeType()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "track.flac");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(path, new byte[] { 1 });
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = root,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem("c1")).Returns(("Music", "w1", "masked.flac", true, "", 0L));
            _repoMock.Setup(x => x.FindFileInfo("masked.flac")).Returns((Filename: path, Size: 1));

            var r = locator.Resolve("c1");

            Assert.NotNull(r);
            Assert.Equal("audio/flac", r.ContentType);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Resolve_AllowedDownloadContentId_ReturnsResolvedContent()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N")[..8]);
        var path = Path.Combine(root, "downloaded.ogg");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
            var contentId = $"path:{slskd.Compute.Sha256Hash($"{path}|4")}";
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions
                {
                    Downloads = root,
                    Incomplete = Path.GetTempPath(),
                },
            });
            _repoMock.Setup(x => x.FindContentItem(contentId))
                .Returns((ValueTuple<string, string, string, bool, string, long>?)null);

            var r = locator.Resolve(contentId);

            Assert.NotNull(r);
            Assert.Equal(path, r.AbsolutePath);
            Assert.Equal(4, r.Length);
            Assert.Equal("audio/ogg", r.ContentType);
            Assert.Equal(r, locator.Resolve(contentId));
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            Assert.Null(locator.Resolve(contentId));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void RegisterLocalFile_AllowsImmediateDistinctDownloadsAndRechecksAccess()
    {
        var root = Path.Combine(Path.GetTempPath(), "ContentLoc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var first = Path.Combine(root, "first.wav");
            var second = Path.Combine(root, "second.wav");
            File.WriteAllBytes(first, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(second, new byte[] { 4, 5, 6 });
            var locator = CreateLocator(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions { Downloads = root },
            });
            var firstId = locator.RegisterLocalFile(first);
            var secondId = locator.RegisterLocalFile(second);
            Assert.NotNull(firstId);
            Assert.NotNull(secondId);
            Assert.Equal(first, locator.Resolve(firstId!)!.AbsolutePath);
            Assert.Equal(second, locator.Resolve(secondId!)!.AbsolutePath);
            Assert.Null(locator.RegisterLocalFile(Path.Combine(root, "missing.wav")));
            _repoMock.Setup(repository => repository.FindContentItem(firstId!))
                .Returns(("Audio", "", first, false, "blocked", 0L));
            Assert.Null(locator.RegisterLocalFile(first));
            Assert.Null(locator.Resolve(firstId!));
            _optionsMock.Setup(options => options.CurrentValue).Returns(new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions { Downloads = Path.Combine(root, "removed") },
            });
            Assert.Null(locator.Resolve(secondId!));
            Assert.Null(locator.RegisterLocalFile(second));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CreateFallbackEnumerationOptions_SkipsReparsePoints()
    {
        var options = ContentLocator.CreateFallbackEnumerationOptions();

        Assert.True(options.RecurseSubdirectories);
        Assert.True(options.AttributesToSkip.HasFlag(FileAttributes.ReparsePoint));
        Assert.True(options.AttributesToSkip.HasFlag(FileAttributes.System));
    }
}
