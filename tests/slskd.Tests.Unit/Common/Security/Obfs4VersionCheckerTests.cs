// <copyright file="Obfs4VersionCheckerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using slskd.Common.Security;
using Xunit;

public sealed class Obfs4VersionCheckerTests
{
    [Fact]
    public async Task RunVersionCheckAsync_DrainsLargeStandardOutputAndError()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), $"slskdn-obfs4-version-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var scriptPath = Path.Combine(directory, "obfs4proxy.sh");
        File.WriteAllText(
            scriptPath,
            "#!/bin/sh\ni=0\nwhile [ \"$i\" -lt 4096 ]; do printf 'stdout line %s\n' \"$i\"; printf 'stderr line %s\n' \"$i\" >&2; i=$((i + 1)); done\n");
        File.SetUnixFileMode(
            scriptPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        try
        {
            var exitCode = await new Obfs4VersionChecker()
                .RunVersionCheckAsync(scriptPath)
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(0, exitCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunVersionCheckAsync_CancellationStopsTheChildProcess()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), $"slskdn-obfs4-cancel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var scriptPath = Path.Combine(directory, "obfs4proxy.sh");
        File.WriteAllText(scriptPath, "#!/bin/sh\nsleep 30\n");
        File.SetUnixFileMode(
            scriptPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => new Obfs4VersionChecker().RunVersionCheckAsync(scriptPath, cancellation.Token));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
