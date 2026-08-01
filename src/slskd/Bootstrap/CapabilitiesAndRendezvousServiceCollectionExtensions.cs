// <copyright file="CapabilitiesAndRendezvousServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using slskd.DhtRendezvous;
using slskd.DhtRendezvous.Security;

public static class CapabilitiesAndRendezvousServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdCapabilitiesAndRendezvousServices(
        this IServiceCollection services,
        slskd.Options optionsAtStartup)
    {
        // MediaCore publisher
        if (optionsAtStartup.Feature.MeshPublishAvailability)
        {
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Constructing ContentPublisherService hosted service...");
                var service = ActivatorUtilities.CreateInstance<MediaCore.ContentPublisherService>(p);
                Log.Debug("[DI] ContentPublisherService constructed");
                return service;
            });
        }

        // Capabilities - tracks available features per peer
        services.AddSingleton<Capabilities.ICapabilityService, Capabilities.CapabilityService>();
        if (optionsAtStartup.Feature.Mesh)
        {
            services.AddHostedService<Capabilities.SoulseekCapabilityBridgeService>();
        }

        // DhtRendezvous services (BitTorrent DHT peer discovery)
        services.AddSingleton(optionsAtStartup.DhtRendezvous);
        services.AddSingleton<CertificateManager>(sp => new CertificateManager(sp.GetRequiredService<ILogger<CertificateManager>>(), Program.AppDirectory));
        services.AddSingleton<CertificatePinStore>(sp => new CertificatePinStore(sp.GetRequiredService<ILogger<CertificatePinStore>>(), Program.AppDirectory));
        services.AddSingleton<OverlayRateLimiter>();
        services.AddSingleton<OverlayBlocklist>();
        services.AddSingleton<MeshNeighborRegistry>();
        services.AddSingleton<MeshOverlayRequestRouter>();
        services.AddSingleton<DhtRendezvous.Search.IMeshSearchRpcHandler, DhtRendezvous.Search.MeshSearchRpcHandler>();
        services.AddSingleton<DhtRendezvous.Search.IMeshOverlaySearchService, DhtRendezvous.Search.MeshOverlaySearchService>();
        services.AddSingleton<IMeshOverlayServer, MeshOverlayServer>();
        services.AddSingleton<IMeshOverlayConnector, MeshOverlayConnector>();
        if (optionsAtStartup.Feature.Dht && optionsAtStartup.DhtRendezvous.Enabled)
        {
            services.AddHostedService<MeshNeighborPeerSyncService>();
        }

        services.AddSingleton<IDhtRendezvousService, DhtRendezvousService>();
        if (optionsAtStartup.Feature.Dht && optionsAtStartup.DhtRendezvous.Enabled)
        {
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Resolving DhtRendezvousService hosted service...");
                var service = (DhtRendezvousService)p.GetRequiredService<IDhtRendezvousService>();
                Log.Debug("[DI] DhtRendezvousService hosted service resolved");
                return service;
            });
        }

        return services;
    }
}
