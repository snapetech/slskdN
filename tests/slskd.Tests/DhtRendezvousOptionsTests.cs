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
}
