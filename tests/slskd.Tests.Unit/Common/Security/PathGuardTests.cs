// <copyright file="PathGuardTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.IO;
using slskd.Common.Security;
using Xunit;

public class PathGuardTests
{
    [Theory]
    [InlineData("Artist/Album/01 - Track.flac", true)]
    [InlineData("Artist\\Album\\01 - Track.flac", true)]
    [InlineData("../etc/passwd", false)]
    [InlineData("..\\Windows\\System32", false)]
    [InlineData("folder/../../../etc/passwd", false)]
    [InlineData("%2e%2e/etc/passwd", false)]
    [InlineData("%252e%252e/etc/passwd", false)]
    [InlineData("/absolute/path.flac", false)]
    [InlineData("C:\\absolute\\path.flac", false)]
    [InlineData("file\0.txt", false)]
    public void NormalizeAndValidate_RejectsTraversalAndUnsafePaths(string peerPath, bool shouldPass)
    {
        var root = Path.Combine(Path.GetTempPath(), "slskdn-pathguard-tests");

        var result = PathGuard.NormalizeAndValidate(peerPath, root);

        Assert.Equal(shouldPass, result is not null);
        if (shouldPass)
        {
            Assert.True(PathGuard.IsContainedIn(result!, root));
        }
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\Windows\\System32")]
    [InlineData("folder/../../../etc/passwd")]
    [InlineData("%2e%2e/etc/passwd")]
    [InlineData("%252e%252e/etc/passwd")]
    public void ContainsTraversal_DetectsPlainAndEncodedTraversal(string path)
    {
        Assert.True(PathGuard.ContainsTraversal(path));
    }

    [Theory]
    [InlineData("x..xyeppersesx..x")]
    [InlineData("folder/.../track.flac")]
    [InlineData("/api/v0/transfers/downloads/x..xyeppersesx..x")]
    public void ContainsTraversal_AllowsAdjacentDotsInsidePathComponents(string path)
    {
        Assert.False(PathGuard.ContainsTraversal(path));
    }

    [Theory]
    [InlineData("song?.flac", "song_.flac")]
    [InlineData("folder/name.mp3", "folder_name.mp3")]
    [InlineData("folder\\name.mp3", "folder_name.mp3")]
    [InlineData("   ...   ", "unnamed")]
    [InlineData(null, "unnamed")]
    public void SanitizeFilename_RemovesUnsafeFilesystemCharacters(string? filename, string expected)
    {
        var actual = PathGuard.SanitizeFilename(filename);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("track.flac", false, true)]
    [InlineData("track.mp3", false, true)]
    [InlineData("payload.exe", true, false)]
    [InlineData("script.sh", true, false)]
    [InlineData("archive.zip", false, false)]
    public void ExtensionClassifiers_SeparateDangerousAndSafeAudioExtensions(string filename, bool dangerous, bool safeAudio)
    {
        Assert.Equal(dangerous, PathGuard.HasDangerousExtension(filename));
        Assert.Equal(safeAudio, PathGuard.HasSafeAudioExtension(filename));
    }

    [Fact]
    public void Validate_ReturnsViolationTypeForTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "slskdn-pathguard-tests");

        var result = PathGuard.Validate("../etc/passwd", root);

        Assert.False(result.IsValid);
        Assert.Equal(PathViolationType.DirectoryTraversal, result.ViolationType);
    }
}
