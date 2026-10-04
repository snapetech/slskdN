// <copyright file="IntegrationsServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using slskd.Bootstrap;
using slskd.Integrations.Lidarr;
using slskd.Integrations.Scripts;
using slskd.Integrations.VPN;
using slskd.Integrations.Webhooks;
using slskd.ListeningParty;
using Xunit;

public class IntegrationsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlskdIntegrations_RegistersIntegrationServicesAndHostedWorkersOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdIntegrations();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(VPNService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ILidarrClient));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(LidarrSyncService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ILidarrSyncService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(LidarrImportService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ILidarrImportService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ScriptService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(WebhookService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(global::slskd.NowPlaying.NowPlayingService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IListeningPartyService));
        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService)));
    }
}
