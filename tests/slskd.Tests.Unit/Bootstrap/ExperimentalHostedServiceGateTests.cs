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
        var options = new slskd.Options();

        services.AddSlskdExperimentalMeshServices(configuration, options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void CapabilitiesAndRendezvous_AllNetworkFlagsDisabled_RegisterNoHostedServices()
    {
        var services = new ServiceCollection();
        var options = new slskd.Options();

        services.AddSlskdCapabilitiesAndRendezvousServices(options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void VirtualSoulfindDisabled_RegistersNoHostedServices()
    {
        var services = new ServiceCollection();
        var options = new slskd.Options();

        services.AddSlskdVirtualSoulfindServices(options);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }
}
