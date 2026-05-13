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
    public void Defaults_KeepExperimentalFeatureGatesDisabled()
    {
        var options = new slskd.Options();

        Assert.False(options.Feature.SongId);
        Assert.False(options.Feature.Mesh);
        Assert.False(options.Feature.Dht);
        Assert.False(options.Feature.Pods);
        Assert.False(options.Feature.SocialFederation);
        Assert.False(options.Feature.VirtualSoulfind);
        Assert.False(options.Feature.MultiSourceDownloads);
    }
}
