// <copyright file="CapabilityServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Capabilities;

using slskd.Capabilities;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class CapabilityServiceTests
{
    [Fact]
    public void SetPeerCapabilities_EscapesRemoteUsernameOnlyInLog()
    {
        const string username = "alice\r\nforged peer";
        var logger = new CapturingLogger<CapabilityService>();
        var service = new CapabilityService(logger);
        var capabilities = new PeerCapabilities
        {
            Flags = PeerCapabilityFlags.SupportsMeshSync,
        };

        service.SetPeerCapabilities(username, capabilities);

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("alice\\r\\nforged peer", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Equal(username, capabilities.Username);
        Assert.Equal(username, service.GetPeerCapabilities(username)!.Username);
    }
}
