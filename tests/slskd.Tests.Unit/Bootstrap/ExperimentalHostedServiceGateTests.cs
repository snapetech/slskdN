// <copyright file="ExperimentalHostedServiceGateTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using slskd.Bootstrap;
using Xunit;

public sealed class ExperimentalHostedServiceGateTests
{
    [Fact]
    public void ExperimentalMeshServices_AllNetworkFlagsDisabled_RegisterNoHostedServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var options = DisabledNetworkOptions();

        services.AddSlskdExperimentalMeshServices(configuration, options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void CapabilitiesAndRendezvous_AllNetworkFlagsDisabled_RegisterNoHostedServices()
    {
        var services = new ServiceCollection();
        var options = DisabledNetworkOptions();

        services.AddSlskdCapabilitiesAndRendezvousServices(options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void VirtualSoulfindDisabled_RegistersNoHostedServices()
    {
        var services = new ServiceCollection();
        var options = DisabledNetworkOptions();

        services.AddSlskdVirtualSoulfindServices(options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    private static slskd.Options DisabledNetworkOptions() => new()
    {
        Feature = new()
        {
            Mesh = false,
            Dht = false,
            Pods = false,
            VirtualSoulfind = false,
            MeshPublishAvailability = false,
            IdentityFriends = false,
            MultiSourceDownloads = false,
        },
        DhtRendezvous = new() { Enabled = false },
        VirtualSoulfindV2 = new() { Enabled = false },
    };
}
