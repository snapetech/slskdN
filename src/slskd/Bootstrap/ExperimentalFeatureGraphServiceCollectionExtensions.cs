// <copyright file="ExperimentalFeatureGraphServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using slskd.DhtRendezvous;
using slskd.DhtRendezvous.Security;

public static class ExperimentalFeatureGraphServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdExperimentalFeatureGraph(
        this IServiceCollection services,
        IConfiguration configuration,
        slskd.Options optionsAtStartup)
    {
        services.AddSlskdMultiSourceFeatureServices(optionsAtStartup);
        services.AddSlskdVirtualSoulfindServices();

        services.AddSlskdMediaCorePodServices();

        services.AddSlskdExperimentalMeshServices(configuration, optionsAtStartup);

        // MediaCore publisher
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Constructing ContentPublisherService hosted service...");
            var service = ActivatorUtilities.CreateInstance<MediaCore.ContentPublisherService>(p);
            Log.Debug("[DI] ContentPublisherService constructed");
            return service;
        });

        // Capabilities - tracks available features per peer
        services.AddSingleton<Capabilities.ICapabilityService, Capabilities.CapabilityService>();
        services.AddHostedService<Capabilities.SoulseekCapabilityBridgeService>();

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
        services.AddHostedService<MeshNeighborPeerSyncService>();

        services.AddSingleton<IDhtRendezvousService, DhtRendezvousService>();
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Resolving DhtRendezvousService hosted service...");
            var service = (DhtRendezvousService)p.GetRequiredService<IDhtRendezvousService>();
            Log.Debug("[DI] DhtRendezvousService hosted service resolved");
            return service;
        });

        services.AddSlskdTransferDiscoveryServices();

        services.AddSlskdIntegrationAndMediaServices();

        return services;
    }
}
