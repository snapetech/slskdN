// <copyright file="BridgeProxySecurityTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Bridge;

using System.Net;
using slskd.VirtualSoulfind.Bridge;
using slskd.VirtualSoulfind.Bridge.Proxy;
using Xunit;

public sealed class BridgeProxySecurityTests
{
    [Fact]
    public void BridgeOptions_DefaultToLoopbackAndAuthentication()
    {
        var options = new BridgeOptions();

        Assert.Equal("127.0.0.1", options.BindAddress);
        Assert.True(options.RequireAuth);
        Assert.Equal(60, options.MaxRequestsPerMinute);
        Assert.Equal(10, options.MaxTransfersPerSession);
    }

    [Fact]
    public void ValidateSecurityConfiguration_NonLoopbackWithoutAuthentication_Throws()
    {
        var options = new BridgeOptions
        {
            BindAddress = "0.0.0.0",
            RequireAuth = false,
        };

        Assert.Throws<InvalidOperationException>(() =>
            BridgeProxyServer.ValidateSecurityConfiguration(options, IPAddress.Any));
    }

    [Fact]
    public void ValidateSecurityConfiguration_NonLoopbackWithPassword_Allows()
    {
        var options = new BridgeOptions
        {
            BindAddress = "0.0.0.0",
            RequireAuth = true,
            Password = "EXAMPLE_BRIDGE_PASSWORD",
        };

        BridgeProxyServer.ValidateSecurityConfiguration(options, IPAddress.Any);
    }

    [Fact]
    public void ResolveListenerAddress_InvalidAddress_Throws()
    {
        var options = new BridgeOptions { BindAddress = "not-an-ip-address" };

        Assert.Throws<InvalidOperationException>(() => BridgeProxyServer.ResolveListenerAddress(options));
    }

    [Fact]
    public void TryConsumeRequestQuota_DeniesRequestsPastLimit()
    {
        var options = new BridgeOptions { MaxRequestsPerMinute = 2 };
        var session = new BridgeProxyServer.ClientSession();

        Assert.True(BridgeProxyServer.TryConsumeRequestQuota(session, options));
        Assert.True(BridgeProxyServer.TryConsumeRequestQuota(session, options));
        Assert.False(BridgeProxyServer.TryConsumeRequestQuota(session, options));
    }
}
