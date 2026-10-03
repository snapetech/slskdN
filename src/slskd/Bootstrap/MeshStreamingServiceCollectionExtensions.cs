// <copyright file="MeshStreamingServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using slskd.Mesh;
using slskd.Streaming;
using slskd.Transfers;

public static class MeshStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdMeshStreamingServices(this IServiceCollection services)
    {
        services.AddSingleton<IContentLocator, ContentLocator>();
        services.AddSingleton<IStreamSessionLimiter, StreamSessionLimiter>();
        services.AddSingleton<IStreamTicketService, StreamTicketService>();
        services.AddSingleton<IPeerStreamTicketService, PeerStreamTicketService>();
        services.AddSingleton<IPeerStreamService, PeerStreamService>();
        services.AddSingleton<IMeshStreamTicketService, MeshStreamTicketService>();
        services.AddSingleton<IMeshStreamService>(sp => new MeshStreamService(
            sp.GetRequiredService<IMeshStreamTicketService>(),
            sp.GetRequiredService<IStreamSessionLimiter>(),
            sp.GetRequiredService<IMeshDirectory>(),
            sp.GetRequiredService<IMeshContentFetcher>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MeshStreamService>>(),
            sp.GetService<Transfers.MultiSource.Metrics.IFairnessGuard>(),
            sp.GetService<Transfers.MultiSource.Metrics.ITrafficAccountingService>()));

        return services;
    }
}
