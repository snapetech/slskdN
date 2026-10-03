// <copyright file="IntegrationsServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using slskd.Integrations.Lidarr;
using slskd.Integrations.Scripts;
using slskd.Integrations.VPN;
using slskd.Integrations.Webhooks;
using slskd.ListeningParty;

public static class IntegrationsServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<VPNService>();
        services.AddSingleton<ILidarrClient, LidarrClient>();
        services.AddSingleton<LidarrSyncService>();
        services.AddSingleton<ILidarrSyncService>(sp => sp.GetRequiredService<LidarrSyncService>());
        services.AddHostedService(sp => sp.GetRequiredService<LidarrSyncService>());
        services.AddSingleton<LidarrImportService>();
        services.AddSingleton<ILidarrImportService>(sp => sp.GetRequiredService<LidarrImportService>());
        services.AddHostedService(sp => sp.GetRequiredService<LidarrImportService>());
        services.AddSingleton<ScriptService>();
        services.AddSingleton<WebhookService>();
        services.AddSingleton<NowPlaying.NowPlayingService>();
        services.AddSingleton<IListeningPartyService, ListeningPartyService>();

        return services;
    }
}
