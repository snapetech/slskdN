// <copyright file="MeshStreamingServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using slskd.Bootstrap;
using slskd.Streaming;
using Xunit;

public class MeshStreamingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlskdMeshStreamingServices_RegistersStreamingServicesOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdMeshStreamingServices();

        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IContentLocator)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IStreamSessionLimiter)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IStreamTicketService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IPeerStreamTicketService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IPeerStreamService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IMeshStreamTicketService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IMeshStreamService)));
    }
}
