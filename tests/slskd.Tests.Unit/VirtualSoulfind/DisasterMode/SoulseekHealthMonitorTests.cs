// <copyright file="SoulseekHealthMonitorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.DisasterMode;

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SoulseekClient = Soulseek.ISoulseekClient;
using SoulseekStates = Soulseek.SoulseekClientStates;
using slskd.VirtualSoulfind.DisasterMode;
using Xunit;

public sealed class SoulseekHealthMonitorTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenReconnectDelayIsCanceled_PropagatesHostCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new Mock<SoulseekClient>();
        client.SetupGet(soulseek => soulseek.State).Returns(SoulseekStates.Disconnected);
        var monitor = new SoulseekHealthMonitor(
            NullLogger<SoulseekHealthMonitor>.Instance,
            client.Object,
            Mock.Of<IOptionsMonitor<slskd.Options>>());
        var checkHealth = typeof(SoulseekHealthMonitor).GetMethod(
            "CheckHealthAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = Assert.IsAssignableFrom<Task<SoulseekHealth>>(
            checkHealth.Invoke(monitor, new object[] { cancellation.Token }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
