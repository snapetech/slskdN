// <copyright file="ApplicationFilePruningTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core;

using System;
using System.IO;
using Moq;
using slskd.Files;
using Xunit;

public class ApplicationFilePruningTests
{
    [Fact]
    public void PruneDirectoryFiles_StreamsAndResolvesEachFileOnce()
    {
        var directory = Directory.CreateTempSubdirectory("slskdn-prune-");
        try
        {
            var nested = directory.CreateSubdirectory("nested");
            var oldFile = WriteFile(directory, "old.part");
            var nestedOldFile = WriteFile(nested, "nested-old.part");
            var recentFile = WriteFile(directory, "recent.part");
            var utcNow = DateTime.UtcNow;
            File.SetLastAccessTimeUtc(oldFile, utcNow.AddMinutes(-120));
            File.SetLastAccessTimeUtc(nestedOldFile, utcNow.AddMinutes(-90));
            File.SetLastAccessTimeUtc(recentFile, utcNow.AddMinutes(-5));
            var fileService = new Mock<FileService>(
                new TestOptionsMonitor<Options>(new Options()))
            {
                CallBase = true,
            };

            var result = Application.PruneDirectoryFiles(
                age: 60,
                directory.FullName,
                fileService.Object,
                utcNow);

            Assert.Equal((Found: 2, Deleted: 2, Errors: 0), result);
            Assert.False(File.Exists(oldFile));
            Assert.False(File.Exists(nestedOldFile));
            Assert.True(File.Exists(recentFile));
            fileService.Verify(service => service.ResolveFileInfo(It.IsAny<string>()), Times.Exactly(3));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string WriteFile(DirectoryInfo directory, string filename)
    {
        var path = Path.Combine(directory.FullName, filename);
        File.WriteAllText(path, "fixture");
        return path;
    }
}
