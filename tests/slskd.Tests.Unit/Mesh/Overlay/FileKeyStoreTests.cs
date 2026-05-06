// <copyright file="FileKeyStoreTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Mesh.Overlay;
using Xunit;

namespace slskd.Tests.Unit.Mesh.Overlay;

public class FileKeyStoreTests : IDisposable
{
    private readonly string _tempDir;

    public FileKeyStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"slskdn-key-store-tests-{Guid.NewGuid():N}");
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
    public void Constructor_WritesKeyWithoutLeavingTempFileAndReloadsExistingKey()
    {
        var keyPath = Path.Combine(_tempDir, "mesh-overlay.key");
        var options = Microsoft.Extensions.Options.Options.Create(new OverlayOptions
        {
            KeyPath = keyPath,
            RotateDays = 0,
        });

        var created = new FileKeyStore(NullLogger<FileKeyStore>.Instance, options);
        var reloaded = new FileKeyStore(NullLogger<FileKeyStore>.Instance, options);

        Assert.True(File.Exists(keyPath));
        Assert.False(Directory.EnumerateFiles(_tempDir, "mesh-overlay.key.*.tmp").Any());
        Assert.Equal(created.Current.PublicKey, reloaded.Current.PublicKey);
    }
}
