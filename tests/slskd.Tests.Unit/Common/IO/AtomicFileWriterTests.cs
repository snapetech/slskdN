// <copyright file="AtomicFileWriterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.IO;

using System.Text;
using slskd.Common.IO;
using Xunit;

public sealed class AtomicFileWriterTests : IDisposable
{
    private readonly string _tempDir;

    public AtomicFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"slskdn-atomic-file-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void WriteAllText_ReplacesExistingFileWithoutTempResidue()
    {
        var path = Path.Combine(_tempDir, "state.json");
        File.WriteAllText(path, "old", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "new");

        Assert.Equal("new", File.ReadAllText(path));
        Assert.Empty(Directory.EnumerateFiles(_tempDir, "state.json.*.tmp"));
    }

    [Fact]
    public async Task WriteAllBytesAsync_CreatesDirectoryAndReplacesExistingFileWithoutTempResidue()
    {
        var directory = Path.Combine(_tempDir, "nested");
        var path = Path.Combine(directory, "state.bin");

        await AtomicFileWriter.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 }, CancellationToken.None);
        await AtomicFileWriter.WriteAllBytesAsync(path, new byte[] { 4, 5 }, CancellationToken.None);

        Assert.Equal(new byte[] { 4, 5 }, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(directory, "state.bin.*.tmp"));
    }

    [Fact]
    public void WriteAllBytes_AppliesRestrictiveModeWithoutTempResidue()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(_tempDir, "secret.bin");
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        AtomicFileWriter.WriteAllBytes(path, new byte[] { 1, 2, 3 }, mode);

        Assert.Equal(mode, File.GetUnixFileMode(path));
        Assert.Empty(Directory.EnumerateFiles(_tempDir, "secret.bin.*.tmp"));
    }
}
