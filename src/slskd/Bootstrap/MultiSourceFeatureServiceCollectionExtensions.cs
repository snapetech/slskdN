// <copyright file="MultiSourceFeatureServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using slskd.Integrations.MusicBrainz;
using slskd.LibraryHealth;
using slskd.Signals;
using slskd.Telemetry;

public static class MultiSourceFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdMultiSourceFeatureServices(
        this IServiceCollection services,
        slskd.Options optionsAtStartup)
    {
        // Multi-source feature services
        // (IHashDbService, IMediaVariantStore, ICanonicalStatsService, IDedupeService, IAnalyzerMigrationService in AddAudioCore)
        services.AddSingleton<IArtistReleaseGraphService, ReleaseGraphService>();
        services.AddSingleton<IDiscographyProfileService, DiscographyProfileService>();
        services.AddSingleton<IDiscographyCoverageService, DiscographyCoverageService>();
        services.AddSingleton<Integrations.MusicBrainz.Bloom.ILibraryBloomDiffService, Integrations.MusicBrainz.Bloom.LibraryBloomDiffService>();
        services.AddSingleton<Integrations.MusicBrainz.Radar.IArtistReleaseRadarService, Integrations.MusicBrainz.Radar.ArtistReleaseRadarService>();
        services.AddSingleton<Integrations.MusicBrainz.Overlay.IMusicBrainzOverlayService, Integrations.MusicBrainz.Overlay.MusicBrainzOverlayService>();
        services.AddSingleton<QuarantineJury.IQuarantineJuryService, QuarantineJury.QuarantineJuryService>();
        services.AddSingleton<Jobs.IDiscographyJobService, Jobs.DiscographyJobService>();
        services.AddSingleton<Jobs.ILabelCrateJobService, Jobs.LabelCrateJobService>();
        services.AddSingleton<slskd.API.Native.IJobServiceWithList, slskd.Jobs.HashDbJobServiceListAdapter>();
        services.AddSingleton<Signals.Swarm.ISwarmJobStore, Signals.Swarm.InMemorySwarmJobStore>();
        services.AddSingleton<Signals.Swarm.ISecurityPolicyEngine, Signals.Swarm.StubSecurityPolicyEngine>();
        services.AddSingleton<Signals.Swarm.IBitTorrentBackend, Signals.Swarm.MonoTorrentBitTorrentBackend>();
        services.AddSingleton<Transfers.MultiSource.Metrics.ITrafficAccountingService, Transfers.MultiSource.Metrics.TrafficAccountingService>();
        services.AddSingleton<Transfers.MultiSource.Metrics.IFairnessGuard>(sp =>
            new Transfers.MultiSource.Metrics.FairnessGuard(
                sp.GetRequiredService<Transfers.MultiSource.Metrics.ITrafficAccountingService>()));
        services.AddSingleton<Jobs.Manifests.IJobManifestValidator, Jobs.Manifests.JobManifestValidator>();
        services.AddSingleton<Jobs.Manifests.IJobManifestService, Jobs.Manifests.JobManifestService>();
        services.AddSingleton<Transfers.MultiSource.Tracing.ISwarmEventStore, Transfers.MultiSource.Tracing.SwarmEventStore>();
        services.AddSingleton<Transfers.MultiSource.Tracing.ISwarmTraceSummarizer, Transfers.MultiSource.Tracing.SwarmTraceSummarizer>();

        // OpenTelemetry distributed tracing
        services.AddOpenTelemetryTracing(optionsAtStartup);
        services.AddSingleton<Transfers.MultiSource.Caching.IWarmCachePopularityService, Transfers.MultiSource.Caching.WarmCachePopularityService>();
        services.AddSingleton<Transfers.MultiSource.Optimization.IChunkSizeOptimizer, Transfers.MultiSource.Optimization.ChunkSizeOptimizer>();
        services.AddSingleton<Transfers.MultiSource.Caching.IWarmCacheService, Transfers.MultiSource.Caching.WarmCacheService>();

        // Add signal system
        services.AddSignalSystem();
        services.AddSingleton<Transfers.MultiSource.Playback.IPlaybackPriorityService, Transfers.MultiSource.Playback.PlaybackPriorityService>();
        services.AddSingleton<Transfers.MultiSource.Playback.IPlaybackFeedbackService, Transfers.MultiSource.Playback.PlaybackFeedbackService>();

        // (ILibraryHealthService, ILibraryHealthRemediationService in AddAudioCore)
        return services;
    }
}
