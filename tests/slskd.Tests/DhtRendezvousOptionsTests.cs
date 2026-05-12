// <copyright file="DhtRendezvousOptionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests;

using slskd.DhtRendezvous;
using Xunit;

public class DhtRendezvousOptionsTests
{
    [Fact]
    public void EffectiveOverlayPort_defaults_to_local_overlay_port()
    {
        var options = new DhtRendezvousOptions
        {
            OverlayPort = 50305,
        };

        Assert.Equal(50305, options.EffectiveOverlayPort);
    }

    [Fact]
    public void EffectiveOverlayPort_uses_public_advertised_overlay_port_when_configured()
    {
        var options = new DhtRendezvousOptions
        {
            OverlayPort = 50305,
            AdvertisedOverlayPort = 38851,
        };

        Assert.Equal(38851, options.EffectiveOverlayPort);
    }

    [Theory]
    [InlineData("disabled", VpnOverlayPortSyncMode.Disabled)]
    [InlineData("primary", VpnOverlayPortSyncMode.Primary)]
    [InlineData("target_port", VpnOverlayPortSyncMode.TargetPort)]
    [InlineData("target-port", VpnOverlayPortSyncMode.TargetPort)]
    [InlineData("TargetPort", VpnOverlayPortSyncMode.TargetPort)]
    public void VpnPortSyncMode_accepts_documented_config_spellings(string value, VpnOverlayPortSyncMode expected)
    {
        var options = new DhtRendezvousOptions
        {
            VpnPortSync = value,
        };

        Assert.Equal(expected, options.VpnPortSyncMode);
    }
}
