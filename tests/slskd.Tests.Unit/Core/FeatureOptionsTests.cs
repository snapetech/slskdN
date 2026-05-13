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
    public void Defaults_KeepExperimentalFeatureGatesEnabled()
    {
        var options = new slskd.Options();

        Assert.True(options.Feature.SongId);
        Assert.True(options.Feature.Mesh);
        Assert.True(options.Feature.Dht);
        Assert.True(options.Feature.Pods);
        Assert.True(options.Feature.SocialFederation);
        Assert.True(options.Feature.VirtualSoulfind);
        Assert.True(options.Feature.MultiSourceDownloads);
    }
}
