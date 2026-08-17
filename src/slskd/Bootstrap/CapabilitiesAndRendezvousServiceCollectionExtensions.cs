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
        var shareOverlayTcpPort = SharedMeshTcpListener.ShouldRun(optionsAtStartup);
        if (shareOverlayTcpPort)
        {
            // The mesh TCP overlay handshake will arrive on the Soulseek listen port instead of
            // its own OverlayPort; mutate the shared options instance once, at startup, so every
            // existing consumer (UPnP mapping, VPN port-forward sync, peer advertisement) reads
            // the correct port without individual changes.
            optionsAtStartup.DhtRendezvous.OverlayPort = optionsAtStartup.Soulseek.ListenPort;
        }

        services.AddSingleton(optionsAtStartup.DhtRendezvous);

        if (shareOverlayTcpPort)
        {
            services.AddSingleton<slskd.SoulseekRuntime.FedTcpListener>();
            services.AddHostedService<SharedMeshTcpListener>();
        }

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
