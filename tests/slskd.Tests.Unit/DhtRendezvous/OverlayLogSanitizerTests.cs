// <copyright file="OverlayLogSanitizerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System.Net;
using slskd.DhtRendezvous;
using Xunit;

public class OverlayLogSanitizerTests
{
    [Fact]
    public void Username_PreservesRawSoulseekName()
    {
        var result = OverlayLogSanitizer.Username("keef_shape");

        Assert.Equal("keef_shape", result);
    }

    [Fact]
    public void PeerId_PreservesUsernameBackedPeerId()
    {
        var result = OverlayLogSanitizer.PeerId("spynn56");

        Assert.Equal("spynn56", result);
    }

    [Fact]
    public void Endpoint_PreservesPublicIpAndPortForTriage()
    {
        var endpoint = new IPEndPoint(IPAddress.Parse("24.109.206.134"), 34160);

        var result = OverlayLogSanitizer.Endpoint(endpoint);

        Assert.Equal("24.109.206.134:34160", result);
    }
}
