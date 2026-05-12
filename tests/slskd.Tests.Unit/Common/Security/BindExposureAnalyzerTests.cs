// <copyright file="BindExposureAnalyzerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using slskd.Common.Security;
using Xunit;

public class BindExposureAnalyzerTests
{
    [Theory]
    [InlineData(null, 0, null, BindExposure.None)]
    [InlineData("127.0.0.1", 0, "/tmp/slskd.sock", BindExposure.UnixSocketOnly)]
    [InlineData("127.0.0.1", 5030, null, BindExposure.LoopbackOnly)]
    [InlineData("::1", 5030, null, BindExposure.LoopbackOnly)]
    [InlineData("localhost", 5030, null, BindExposure.LoopbackOnly)]
    [InlineData("*", 5030, null, BindExposure.AnyAddress)]
    [InlineData("0.0.0.0", 5030, null, BindExposure.AnyAddress)]
    [InlineData("::", 5030, null, BindExposure.AnyAddress)]
    [InlineData("192.168.1.10", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("10.0.0.5", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("172.16.0.5", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("169.254.1.5", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("fc00::1", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("fe80::1", 5030, null, BindExposure.NonLoopbackPrivate)]
    [InlineData("8.8.8.8", 5030, null, BindExposure.NonLoopbackPublic)]
    [InlineData("2001:4860:4860::8888", 5030, null, BindExposure.NonLoopbackPublic)]
    [InlineData("not-an-ip", 5030, null, BindExposure.Unknown)]
    public void Analyze_ClassifiesBindExposure(string? address, int port, string? unixSocket, BindExposure expected)
    {
        var actual = BindExposureAnalyzer.Analyze(address, port, unixSocket);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(BindExposure.None, false)]
    [InlineData(BindExposure.LoopbackOnly, false)]
    [InlineData(BindExposure.UnixSocketOnly, false)]
    [InlineData(BindExposure.NonLoopbackPrivate, true)]
    [InlineData(BindExposure.NonLoopbackPublic, true)]
    [InlineData(BindExposure.AnyAddress, true)]
    [InlineData(BindExposure.Unknown, true)]
    public void IsRemoteReachable_FailsClosedForUnknownAndNonLoopback(BindExposure exposure, bool expected)
    {
        var actual = BindExposureAnalyzer.IsRemoteReachable(exposure);

        Assert.Equal(expected, actual);
    }
}
