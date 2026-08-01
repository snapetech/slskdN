// <copyright file="TransferDiscoveryServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using slskd.Audio;
using slskd.Integrations.AcoustId;
using slskd.Integrations.Chromaprint;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using slskd.Transfers.MultiSource.Discovery;
using slskd.Transfers.Rescue;
using Soulseek;

public static class TransferDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdTransferDiscoveryServices(
        this IServiceCollection services,
        slskd.Options optionsAtStartup)
    {
        // Backfill services (Long-tail content discovery)
        services.AddSingleton<Backfill.IBackfillSchedulerService, Backfill.BackfillSchedulerService>();
        services.AddHostedService(p => (Backfill.BackfillSchedulerService)p.GetRequiredService<Backfill.IBackfillSchedulerService>());

        // Mesh services (Hash database synchronization)
        services.AddSingleton<Mesh.IFlacKeyToPathResolver, Mesh.ShareBasedFlacKeyToPathResolver>();
        services.AddSingleton<Mesh.IProofOfPossessionService, Mesh.ProofOfPossessionService>();
        services.AddSingleton<Mesh.IMeshSyncService, Mesh.MeshSyncService>();

        // Multi-source download services (Swarm)
        services.AddSingleton<ISourceDiscoveryService>(sp => new SourceDiscoveryService(
            Program.AppDirectory,
            sp.GetRequiredService<ISoulseekClient>(),
            sp.GetRequiredService<Transfers.MultiSource.IContentVerificationService>(),
            sp.GetRequiredService<Common.Security.ISoulseekSafetyLimiter>()));
        services.AddSingleton<Transfers.MultiSource.IMultiSourceDownloadService, Transfers.MultiSource.MultiSourceDownloadService>();
        services.AddSingleton<Transfers.MultiSource.Analytics.ISwarmAnalyticsService, Transfers.MultiSource.Analytics.SwarmAnalyticsService>();
        services.AddSingleton<Transfers.MultiSource.Discovery.IAdvancedDiscoveryService, Transfers.MultiSource.Discovery.AdvancedDiscoveryService>();
        services.AddSingleton<IAcceleratedDownloadService, AcceleratedDownloadService>();
        services.AddSingleton<IRescueGuardrailService, RescueGuardrailService>();
        services.AddSingleton<IRescueService>(sp => new RescueService(
            sp.GetService<HashDb.IHashDbService>(),
            sp.GetService<IFingerprintExtractionService>(),
            sp.GetService<IAcoustIdClient>(),
            sp.GetService<Mesh.IMeshSyncService>(),
            sp.GetService<Mesh.IMeshDirectory>(),
            sp.GetService<Transfers.MultiSource.IMultiSourceDownloadService>(),
            sp.GetRequiredService<IDownloadService>(),
            sp.GetService<IRescueGuardrailService>()));
        if (optionsAtStartup.Feature.MultiSourceDownloads)
        {
            services.AddHostedService<UnderperformanceDetectorHostedService>();
        }

        services.AddSingleton<Transfers.MultiSource.IContentVerificationService, Transfers.MultiSource.ContentVerificationService>();
        services.AddSingleton<Transfers.MultiSource.Metrics.IPeerMetricsService, Transfers.MultiSource.Metrics.PeerMetricsService>();
        services.AddSingleton<Transfers.MultiSource.Scheduling.IChunkScheduler>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            bool enableCostBasedScheduling = options.CurrentValue.Global.Download.CostBasedScheduling;
            return new Transfers.MultiSource.Scheduling.ChunkScheduler(
                sp.GetRequiredService<Transfers.MultiSource.Metrics.IPeerMetricsService>(),
                enableCostBasedScheduling: enableCostBasedScheduling);
        });

        return services;
    }
}
