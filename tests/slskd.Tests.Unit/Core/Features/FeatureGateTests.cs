// <copyright file="FeatureGateTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Core.Features;

using slskd.Core.Features;
using slskd.DhtRendezvous;
using slskd.Mesh;
using slskd.Tests.Unit;
using Xunit;

public class FeatureGateTests
{
    [Fact]
    public void Get_SongId_DefaultsDisabled()
    {
        var gate = CreateGate(new slskd.Options());

        var result = gate.Get(FeatureId.SongId);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureStatus.Disabled, result.Status);
    }

    [Fact]
    public void Get_SongId_WhenOptionEnabled_ReturnsExperimental()
    {
        var gate = CreateGate(new slskd.Options
        {
            Feature = new slskd.Options.FeatureOptions
            {
                SongId = true,
            },
        });

        var result = gate.Get(FeatureId.SongId);

        Assert.True(result.IsEnabled);
        Assert.Equal(FeatureStatus.Experimental, result.Status);
    }

    [Fact]
    public void Get_Mesh_RequiresFeatureAndOverlayEnabled()
    {
        var gate = CreateGate(
            new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { Mesh = true },
            },
            new MeshOptions { EnableOverlay = false });

        var result = gate.Get(FeatureId.Mesh);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureStatus.Disabled, result.Status);
    }

    [Fact]
    public void Get_Dht_RequiresFeatureAndDhtEnabled()
    {
        var gate = CreateGate(
            new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { Dht = true },
            },
            dhtOptions: new DhtRendezvousOptions { Enabled = false });

        var result = gate.Get(FeatureId.Dht);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureStatus.Disabled, result.Status);
    }

    private static FeatureGate CreateGate(
        slskd.Options options,
        MeshOptions meshOptions = null,
        DhtRendezvousOptions dhtOptions = null) => new(
            new TestOptionsMonitor<slskd.Options>(options),
            new TestOptionsMonitor<MeshOptions>(meshOptions ?? new MeshOptions()),
            new TestOptionsMonitor<DhtRendezvousOptions>(dhtOptions ?? new DhtRendezvousOptions()));
}
