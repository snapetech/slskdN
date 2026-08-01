// <copyright file="FeatureOptionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core;

using Xunit;

public class FeatureOptionsTests
{
    [Fact]
    public void Defaults_KeepScenePodBridgeOptIn()
    {
        var options = new slskd.Options();

        Assert.False(options.Feature.ScenePodBridge);
    }

    [Fact]
    public void Defaults_KeepNetworkedExperimentalFeatureGatesDisabled()
    {
        var options = new slskd.Options();

        Assert.True(options.Feature.SongId);
        Assert.False(options.Feature.Mesh);
        Assert.False(options.Feature.Dht);
        Assert.False(options.Feature.Pods);
        Assert.False(options.Feature.SocialFederation);
        Assert.False(options.Feature.VirtualSoulfind);
        Assert.False(options.Feature.MultiSourceDownloads);
        Assert.False(options.Feature.MeshParallelSearch);
        Assert.False(options.Feature.MeshPublishAvailability);
        Assert.False(options.Feature.IdentityFriends);
        Assert.Equal(string.Empty, options.Soulseek.Description);
    }

    [Fact]
    public void Defaults_KeepIndependentNetworkTransportsDormant()
    {
        var options = new slskd.Options();
        var mesh = new slskd.Mesh.MeshOptions();
        var overlay = new slskd.Mesh.Overlay.OverlayOptions();

        Assert.False(options.DhtRendezvous.Enabled);
        Assert.True(options.DhtRendezvous.LanOnly);
        Assert.False(options.DhtRendezvous.EnableStun);
        Assert.False(mesh.EnableDht);
        Assert.False(mesh.EnableOverlay);
        Assert.False(mesh.EnableStun);
        Assert.False(mesh.EnableSoulseekCapabilityHandshake);
        Assert.False(mesh.ProbeSoulseekRendezvousCapabilities);
        Assert.False(overlay.Enable);
        Assert.False(options.VirtualSoulfindV2.Enabled);
        Assert.False(options.SignalSystem.Enabled);
        Assert.False(options.SignalSystem.MeshChannel.Enabled);
        Assert.False(options.SignalSystem.BtExtensionChannel.Enabled);
    }
}
