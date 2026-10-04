// <copyright file="SignalServiceExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Signals;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Security;
using slskd.Signals;
using slskd.Signals.Swarm;
using Xunit;

public sealed class SignalServiceExtensionsTests
{
    [Fact]
    public async Task InitializeSignalSystemAsync_CreatesPeerIdDependentSwarmHandler()
    {
        var optionsMonitor = new Mock<IOptionsMonitor<SignalSystemOptions>>();
        optionsMonitor.Setup(monitor => monitor.CurrentValue).Returns(new SignalSystemOptions { Enabled = true });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalSystem();
        services.AddSingleton(optionsMonitor.Object);
        services.AddSingleton<ISwarmJobStore, InMemorySwarmJobStore>();
        services.AddSingleton<slskd.Security.ISecurityPolicyEngine>(Mock.Of<slskd.Security.ISecurityPolicyEngine>());
        services.AddSingleton<IBitTorrentBackend, StubBitTorrentBackend>();
        await using var provider = services.BuildServiceProvider();

        await provider.InitializeSignalSystemAsync("local-peer");

        Assert.NotNull(provider.GetRequiredService<ISignalBus>());
    }
}
