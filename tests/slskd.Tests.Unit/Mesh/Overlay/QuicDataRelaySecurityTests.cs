// <copyright file="QuicDataRelaySecurityTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.Overlay;

using System.Net;
using System.Text;
using slskd.Mesh.Overlay;
using Xunit;

public class QuicDataRelaySecurityTests
{
    [Fact]
    public void RelayAuthentication_RequiresConfiguredMatchingToken()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("correct secret"));

        Assert.True(QuicDataServer.IsRelayAuthenticated($"AUTH {encoded}", "correct secret"));
        Assert.False(QuicDataServer.IsRelayAuthenticated($"AUTH {encoded}", "wrong secret"));
        Assert.False(QuicDataServer.IsRelayAuthenticated($"AUTH {encoded}", string.Empty));
        Assert.False(QuicDataServer.IsRelayAuthenticated("RELAY_TCP example.com 443", "correct secret"));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.1.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    public void PublicRelayAddress_RejectsInternalTargets(string address)
    {
        Assert.False(QuicDataServer.IsPublicRelayAddress(IPAddress.Parse(address)));
    }

    [Fact]
    public async Task RelayDestination_RequiresExactAllowlistEntryAndPublicAddress()
    {
        var allowed = new[] { "8.8.8.8:443", "127.0.0.1:443" };

        var approved = await QuicDataServer.ResolveAllowedRelayDestinationAsync(
            "8.8.8.8", 443, allowed, CancellationToken.None);
        var wrongPort = await QuicDataServer.ResolveAllowedRelayDestinationAsync(
            "8.8.8.8", 80, allowed, CancellationToken.None);
        var privateTarget = await QuicDataServer.ResolveAllowedRelayDestinationAsync(
            "127.0.0.1", 443, allowed, CancellationToken.None);

        Assert.Equal(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 443), approved);
        Assert.Null(wrongPort);
        Assert.Null(privateTarget);
    }
}
