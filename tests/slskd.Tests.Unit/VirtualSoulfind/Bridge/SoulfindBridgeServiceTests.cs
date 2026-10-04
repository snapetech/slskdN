// <copyright file="SoulfindBridgeServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Bridge;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Core;
using slskd.VirtualSoulfind.Bridge;
using Xunit;

public sealed class SoulfindBridgeServiceTests
{
    [Fact]
    public async Task StartAsync_DrainsBothProcessStreamsAndStopKillsTheChildTree()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), $"slskdn-soulfind-bridge-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var scriptPath = Path.Combine(directory, "soulfind.sh");
        var markerPath = Path.Combine(directory, "streams-drained");
        File.WriteAllText(
            scriptPath,
            "#!/bin/sh\ni=0\nwhile [ \"$i\" -lt 4096 ]; do printf 'stdout line %s\n' \"$i\"; printf 'stderr line %s\n' \"$i\" >&2; i=$((i + 1)); done\nprintf ready > '" + markerPath + "'\nsleep 30\n");
        File.SetUnixFileMode(
            scriptPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var options = new slskd.Options
        {
            VirtualSoulfind = new VirtualSoulfindOptions
            {
                Bridge = new BridgeOptions
                {
                    Enabled = true,
                    SoulfindPath = scriptPath,
                },
            },
        };
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.SetupGet(monitor => monitor.CurrentValue).Returns(options);
        var service = new SoulfindBridgeService(
            NullLogger<SoulfindBridgeService>.Instance,
            optionsMonitor.Object);

        try
        {
            await service.StartAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(8));

            Assert.True(File.Exists(markerPath));
            Assert.True(service.IsRunning);

            await service.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(6));

            Assert.False(service.IsRunning);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            Directory.Delete(directory, recursive: true);
        }
    }
}
