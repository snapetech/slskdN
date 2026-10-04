// <copyright file="VirtualSoulfindServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using slskd.Bootstrap;
using slskd.Core;
using slskd.Tests.Unit;
using slskd.VirtualSoulfind.v2.Backends;
using slskd.VirtualSoulfind.v2.Configuration;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;
using VirtualSoulfindV2Options = slskd.VirtualSoulfind.v2.Configuration.VirtualSoulfindOptions;

public sealed class VirtualSoulfindServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddSlskdVirtualSoulfindServices_RegistersOneConfiguredDhtRateLimiter()
    {
        var options = new slskd.Options
        {
            VirtualSoulfind = new slskd.Core.VirtualSoulfindOptions
            {
                ShadowIndex = new ShadowIndexOptions
                {
                    MaxDhtOperationsPerMinute = 1,
                },
            },
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptionsMonitor<slskd.Options>>(new TestOptionsMonitor<slskd.Options>(options));
        services.AddSlskdVirtualSoulfindServices(options);
        await using var provider = services.BuildServiceProvider();

        var registrations = provider.GetServices<IDhtRateLimiter>().ToList();
        var limiter = Assert.Single(registrations);

        Assert.True(await limiter.TryAcquireAsync(CancellationToken.None));
        Assert.False(await limiter.TryAcquireAsync(CancellationToken.None));
    }

    [Fact]
    public void BuildTorrentBackendOptions_MapsDisabledByDefaultAndPrivatePeerPolicy()
    {
        var disabled = VirtualSoulfindServiceCollectionExtensions.BuildTorrentBackendOptions(new VirtualSoulfindV2Options());
        Assert.False(disabled.Enabled);

        var configured = VirtualSoulfindServiceCollectionExtensions.BuildTorrentBackendOptions(new VirtualSoulfindV2Options
        {
            Enabled = true,
            Backends = new BackendLimits
            {
                Torrent = new TorrentBackendLimits
                {
                    Enabled = true,
                    MinSeeders = 5,
                    PrivateOnly = false,
                    DisableDht = true,
                    DisablePex = false,
                    AllowedPeerSources = PrivatePeerSource.InviteList,
                    InviteList = new[] { "peer.example:6881" },
                },
            },
        });

        Assert.True(configured.Enabled);
        Assert.Equal(5, configured.MinimumSeeders);
        Assert.NotNull(configured.PrivateMode);
        Assert.False(configured.PrivateMode.PrivateOnly);
        Assert.True(configured.PrivateMode.DisableDht);
        Assert.False(configured.PrivateMode.DisablePex);
        Assert.Equal(PrivatePeerSource.InviteList, configured.PrivateMode.AllowedPeerSources);
        Assert.Equal(new[] { "peer.example:6881" }, configured.PrivateMode.InviteList);
    }

    [Fact]
    public async Task AddSlskdVirtualSoulfindServices_ExposesTorrentSettingsFromApplicationOptions()
    {
        var options = new slskd.Options
        {
            VirtualSoulfindV2 = new VirtualSoulfindV2Options
            {
                Backends = new BackendLimits
                {
                    Torrent = new TorrentBackendLimits
                    {
                        Enabled = true,
                        InviteList = new[] { "peer.example:6881" },
                    },
                },
            },
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptionsMonitor<slskd.Options>>(new TestOptionsMonitor<slskd.Options>(options));
        services.AddSlskdVirtualSoulfindServices(options);
        await using var provider = services.BuildServiceProvider();

        var configured = provider.GetRequiredService<IOptionsMonitor<TorrentBackendOptions>>().CurrentValue;

        Assert.True(configured.Enabled);
        Assert.True(configured.PrivateMode.PrivateOnly);
        Assert.Equal(new[] { "peer.example:6881" }, configured.PrivateMode.InviteList);
    }
}
