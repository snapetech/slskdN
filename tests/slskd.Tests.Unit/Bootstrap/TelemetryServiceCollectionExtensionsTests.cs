// <copyright file="TelemetryServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using slskd.Bootstrap;
using slskd.Telemetry;
using Xunit;

public class TelemetryServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlskdTelemetry_RegistersTelemetryServicesOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdTelemetry();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(PrometheusService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ReportsService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TelemetryService));
    }
}
