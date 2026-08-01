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
    public void Defaults_KeepNetworkedExperimentalFeatureGatesEnabled()
    {
        var options = new slskd.Options();

        Assert.True(options.Feature.SongId);
        Assert.True(options.Feature.Mesh);
        Assert.True(options.Feature.Dht);
        Assert.True(options.Feature.Pods);
        Assert.True(options.Feature.SocialFederation);
        Assert.True(options.Feature.VirtualSoulfind);
        Assert.True(options.Feature.MultiSourceDownloads);
        Assert.True(options.Feature.MeshParallelSearch);
        Assert.True(options.Feature.MeshPublishAvailability);
        Assert.True(options.Feature.IdentityFriends);
        Assert.NotEmpty(options.Soulseek.Description);
    }

    [Fact]
    public void Defaults_KeepIndependentNetworkTransportsEnabled()
    {
        var options = new slskd.Options();
        var mesh = new slskd.Mesh.MeshOptions();
        var overlay = new slskd.Mesh.Overlay.OverlayOptions();

        Assert.True(options.DhtRendezvous.Enabled);
        Assert.False(options.DhtRendezvous.LanOnly);
        Assert.True(options.DhtRendezvous.EnableStun);
        Assert.True(mesh.EnableDht);
        Assert.True(mesh.EnableOverlay);
        Assert.True(mesh.EnableStun);
        Assert.True(mesh.EnableSoulseekCapabilityHandshake);
        Assert.True(mesh.ProbeSoulseekRendezvousCapabilities);
        Assert.True(overlay.Enable);
        Assert.True(options.VirtualSoulfindV2.Enabled);
        Assert.True(options.SignalSystem.Enabled);
        Assert.True(options.SignalSystem.MeshChannel.Enabled);
        Assert.True(options.SignalSystem.BtExtensionChannel.Enabled);
    }
}
