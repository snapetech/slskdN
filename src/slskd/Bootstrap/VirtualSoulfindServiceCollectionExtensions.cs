// <copyright file="VirtualSoulfindServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

public static class VirtualSoulfindServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdVirtualSoulfindServices(
        this IServiceCollection services,
        slskd.Options optionsAtStartup)
    {
        // Virtual Soulfind services
        services.AddSingleton<VirtualSoulfind.Capture.ITrafficObserver, VirtualSoulfind.Capture.TrafficObserver>();
        services.AddSingleton<VirtualSoulfind.Capture.INormalizationPipeline, VirtualSoulfind.Capture.NormalizationPipeline>();
        services.AddSingleton<VirtualSoulfind.Capture.IUsernamePseudonymizer, VirtualSoulfind.Capture.UsernamePseudonymizer>();
        services.AddSingleton<VirtualSoulfind.Capture.IObservationStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            if (options.CurrentValue.VirtualSoulfind?.Privacy?.PersistRawObservations == true)
            {
                return new VirtualSoulfind.Capture.SqliteObservationStore(
                    sp.GetRequiredService<ILogger<VirtualSoulfind.Capture.SqliteObservationStore>>(),
                    options);
            }

            return new VirtualSoulfind.Capture.InMemoryObservationStore(
                sp.GetRequiredService<ILogger<VirtualSoulfind.Capture.InMemoryObservationStore>>());
        });
        services.AddSingleton<VirtualSoulfind.Capture.TrafficObserverIntegrationService>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShadowIndexBuilder, VirtualSoulfind.ShadowIndex.ShadowIndexBuilder>();

        // Note: IDhtClient is registered later in MeshCore as InMemoryDhtClient.
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IDhtRateLimiter>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            var maxOpsPerMin = options.CurrentValue.VirtualSoulfind?.ShadowIndex?.MaxDhtOperationsPerMinute ?? 60;
            return new VirtualSoulfind.ShadowIndex.DhtRateLimiter(
                sp.GetRequiredService<ILogger<VirtualSoulfind.ShadowIndex.DhtRateLimiter>>(),
                maxOpsPerMin);
        });
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardPublisher, VirtualSoulfind.ShadowIndex.ShardPublisher>();
        if (optionsAtStartup.Feature.VirtualSoulfind)
        {
            services.AddHostedService(sp => (VirtualSoulfind.ShadowIndex.ShardPublisher)sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IShardPublisher>());
        }

        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShadowIndexQuery, VirtualSoulfind.ShadowIndex.ShadowIndexQuery>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardMerger, VirtualSoulfind.ShadowIndex.ShardMerger>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardCache, VirtualSoulfind.ShadowIndex.ShardCache>();
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneService, VirtualSoulfind.Scenes.SceneService>();
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneAnnouncementService>(sp =>
            new VirtualSoulfind.Scenes.SceneAnnouncementService(
                sp.GetRequiredService<ILogger<VirtualSoulfind.Scenes.SceneAnnouncementService>>(),
                sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtClient>(),
                sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtRateLimiter>(),
                sp.GetRequiredService<Identity.IProfileService>(),
                sp.GetService<VirtualSoulfind.Scenes.ISceneService>()));
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneMembershipTracker, VirtualSoulfind.Scenes.SceneMembershipTracker>();
        services.AddSingleton<VirtualSoulfind.Scenes.IScenePubSubService>(sp =>
            new VirtualSoulfind.Scenes.ScenePubSubService(
                sp.GetRequiredService<ILogger<VirtualSoulfind.Scenes.ScenePubSubService>>(),
                sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtClient>()));
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneJobService, VirtualSoulfind.Scenes.SceneJobService>();
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneChatService>(sp =>
            new VirtualSoulfind.Scenes.SceneChatService(
                sp.GetRequiredService<ILogger<VirtualSoulfind.Scenes.SceneChatService>>(),
                sp.GetRequiredService<VirtualSoulfind.Scenes.IScenePubSubService>(),
                sp.GetRequiredService<IOptionsMonitor<slskd.Options>>(),
                sp.GetRequiredService<Identity.IProfileService>()));
        services.AddSingleton<VirtualSoulfind.Scenes.ISceneModerationService, VirtualSoulfind.Scenes.SceneModerationService>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.ISoulseekClient>(sp =>
            new VirtualSoulfind.DisasterMode.SoulseekClientWrapper(sp.GetRequiredService<Soulseek.ISoulseekClient>()));
        services.AddSingleton<VirtualSoulfind.DisasterMode.ISoulseekHealthMonitor>(sp =>
            new VirtualSoulfind.DisasterMode.SoulseekHealthMonitor(
                sp.GetRequiredService<ILogger<VirtualSoulfind.DisasterMode.SoulseekHealthMonitor>>(),
                sp.GetRequiredService<Soulseek.ISoulseekClient>(),
                sp.GetRequiredService<IOptionsMonitor<slskd.Options>>()));
        if (optionsAtStartup.Feature.VirtualSoulfind)
        {
            services.AddHostedService(sp => (VirtualSoulfind.DisasterMode.SoulseekHealthMonitor)sp.GetRequiredService<VirtualSoulfind.DisasterMode.ISoulseekHealthMonitor>());
        }

        services.AddSingleton<VirtualSoulfind.DisasterMode.IDisasterModeCoordinator, VirtualSoulfind.DisasterMode.DisasterModeCoordinator>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IMeshSearchService, VirtualSoulfind.DisasterMode.MeshSearchService>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IMeshTransferService, VirtualSoulfind.DisasterMode.MeshTransferService>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IScenePeerDiscovery, VirtualSoulfind.DisasterMode.ScenePeerDiscovery>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IDisasterModeTelemetry, VirtualSoulfind.DisasterMode.DisasterModeTelemetryService>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IGracefulDegradationService, VirtualSoulfind.DisasterMode.GracefulDegradationService>();
        services.AddSingleton<VirtualSoulfind.DisasterMode.IDisasterModeRecovery, VirtualSoulfind.DisasterMode.DisasterModeRecovery>();
        services.AddSingleton<VirtualSoulfind.Integration.IShadowIndexJobIntegration, VirtualSoulfind.Integration.ShadowIndexJobIntegration>();
        services.AddSingleton<VirtualSoulfind.Integration.ISceneLabelCrateIntegration, VirtualSoulfind.Integration.SceneLabelCrateIntegration>();
        services.AddSingleton<VirtualSoulfind.Integration.IDisasterRescueIntegration, VirtualSoulfind.Integration.DisasterRescueIntegration>();
        services.AddSingleton<VirtualSoulfind.Integration.IPrivacyAudit, VirtualSoulfind.Integration.PrivacyAudit>();
        services.AddSingleton<VirtualSoulfind.Integration.IPerformanceOptimizer, VirtualSoulfind.Integration.PerformanceOptimizer>();
        services.AddSingleton<VirtualSoulfind.Integration.ITelemetryDashboard, VirtualSoulfind.Integration.TelemetryDashboardService>();
        services.AddSingleton<VirtualSoulfind.Bridge.ISoulfindBridgeService, VirtualSoulfind.Bridge.SoulfindBridgeService>();

        // Register ITransferProgressProxy BEFORE BridgeApi (BridgeApi depends on it).
        services.AddSingleton<VirtualSoulfind.Bridge.IPeerIdAnonymizer, VirtualSoulfind.Bridge.PeerIdAnonymizer>();
        services.AddSingleton<VirtualSoulfind.Bridge.IFilenameGenerator, VirtualSoulfind.Bridge.FilenameGenerator>();
        services.AddSingleton<VirtualSoulfind.Bridge.IRoomSceneMapper, VirtualSoulfind.Bridge.RoomSceneMapper>();
        services.AddSingleton<VirtualSoulfind.Bridge.ITransferProgressProxy, VirtualSoulfind.Bridge.TransferProgressProxy>();
        services.AddSingleton<VirtualSoulfind.Bridge.IBridgeApi, VirtualSoulfind.Bridge.BridgeApi>();
        services.AddSingleton<VirtualSoulfind.Bridge.Protocol.SoulseekProtocolParser>();

        // BridgeProxyServer is opt-in because construction has blocked startup in local-dev runs.
        if (optionsAtStartup.Feature.VirtualSoulfind &&
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SLSKDN_E2E_SKIP_BRIDGE_PROXY")) &&
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SLSKDN_ENABLE_BRIDGE_PROXY")))
        {
            services.AddHostedService<VirtualSoulfind.Bridge.Proxy.BridgeProxyServer>();
        }
        else
        {
            Log.Debug("[DI] BridgeProxyServer disabled (set SLSKDN_ENABLE_BRIDGE_PROXY=1 to enable)");
        }

        services.AddSingleton<VirtualSoulfind.Bridge.IBridgeDashboard, VirtualSoulfind.Bridge.BridgeDashboard>();

        // VirtualSoulfind v2 Domain Providers (T-VC02, T-VC03) (IMusicContentDomainProvider in AddAudioCore)
        services.AddSingleton<VirtualSoulfind.Core.GenericFile.IGenericFileContentDomainProvider, VirtualSoulfind.Core.GenericFile.GenericFileContentDomainProvider>();
        services.AddSingleton<VirtualSoulfind.Core.Movie.IMovieContentDomainProvider, VirtualSoulfind.Core.Movie.MovieContentDomainProvider>();
        services.AddSingleton<VirtualSoulfind.Core.Tv.ITvContentDomainProvider, VirtualSoulfind.Core.Tv.TvContentDomainProvider>();
        services.AddSingleton<VirtualSoulfind.Core.Book.IBookContentDomainProvider, VirtualSoulfind.Core.Book.BookContentDomainProvider>();

        // VirtualSoulfind v2 core graph
        services.AddOptions<VirtualSoulfind.v2.VirtualSoulfindV2Options>();
        services.AddOptions<VirtualSoulfind.v2.Resolution.ResolverOptions>();
        services.AddOptions<VirtualSoulfind.v2.Processing.IntentQueueProcessorOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.HttpBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.WebDavBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.S3BackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.TorrentBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.MeshDhtBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.LanBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.NativeMeshBackendOptions>();
        services.AddOptions<VirtualSoulfind.v2.Backends.SoulseekBackendOptions>();

        services.AddSingleton<IOptionsMonitor<VirtualSoulfind.v2.VirtualSoulfindV2Options>>(sp =>
        {
            var root = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>().CurrentValue.VirtualSoulfindV2;
            var wrapped = Microsoft.Extensions.Options.Options.Create(new VirtualSoulfind.v2.VirtualSoulfindV2Options
            {
                Enabled = root.Enabled,
                DefaultPlanningMode = root.DefaultMode.ToString(),
                MaxConcurrentPlans = Math.Max(1, root.MaxConcurrentExecutions),
                PlanTimeoutSeconds = new VirtualSoulfind.v2.VirtualSoulfindV2Options().PlanTimeoutSeconds,
            });
            return new Common.Moderation.WrappedOptionsMonitor<VirtualSoulfind.v2.VirtualSoulfindV2Options>(wrapped);
        });

        services.AddSingleton<IOptionsMonitor<VirtualSoulfind.v2.Resolution.ResolverOptions>>(sp =>
        {
            var root = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>().CurrentValue.VirtualSoulfindV2;
            var wrapped = Microsoft.Extensions.Options.Options.Create(new VirtualSoulfind.v2.Resolution.ResolverOptions
            {
                MaxConcurrentExecutions = Math.Max(1, root.MaxConcurrentExecutions),
                DefaultStepTimeoutSeconds = new VirtualSoulfind.v2.Resolution.ResolverOptions().DefaultStepTimeoutSeconds,
            });
            return new Common.Moderation.WrappedOptionsMonitor<VirtualSoulfind.v2.Resolution.ResolverOptions>(wrapped);
        });

        services.AddSingleton<IOptionsMonitor<VirtualSoulfind.v2.Processing.IntentQueueProcessorOptions>>(sp =>
        {
            var root = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>().CurrentValue.VirtualSoulfindV2;
            var wrapped = Microsoft.Extensions.Options.Options.Create(new VirtualSoulfind.v2.Processing.IntentQueueProcessorOptions
            {
                Enabled = root.Enabled,
                BatchSize = Math.Max(1, root.ProcessorBatchSize),
                ProcessingIntervalSeconds = Math.Max(1, root.ProcessorIntervalMs / 1000),
                StartupDelaySeconds = 10,
            });
            return new Common.Moderation.WrappedOptionsMonitor<VirtualSoulfind.v2.Processing.IntentQueueProcessorOptions>(wrapped);
        });

        services.AddSingleton<IOptionsMonitor<VirtualSoulfind.v2.Backends.SoulseekBackendOptions>>(sp =>
        {
            var root = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>().CurrentValue.VirtualSoulfindV2;
            var wrapped = Microsoft.Extensions.Options.Options.Create(new VirtualSoulfind.v2.Backends.SoulseekBackendOptions
            {
                Enabled = root.Enabled,
                SearchTimeoutSeconds = Math.Max(1, root.Backends.Soulseek.SearchTimeoutMs / 1000),
                MinimumUploadSpeed = Math.Max(0, root.Backends.Soulseek.MinUploadSpeedBytesPerSec),
            });
            return new Common.Moderation.WrappedOptionsMonitor<VirtualSoulfind.v2.Backends.SoulseekBackendOptions>(wrapped);
        });

        var virtualSoulfindV2CataloguePath = Path.Combine(Program.AppDirectory, "virtualsoulfind-v2-catalogue.db");
        var virtualSoulfindV2SourcesPath = Path.Combine(Program.AppDirectory, "virtualsoulfind-v2-sources.db");

        services.AddSingleton<VirtualSoulfind.v2.Catalogue.ICatalogueStore>(_ =>
            new VirtualSoulfind.v2.Catalogue.SqliteCatalogueStore(virtualSoulfindV2CataloguePath));
        services.AddSingleton<VirtualSoulfind.v2.Sources.ISourceRegistry>(_ =>
            new VirtualSoulfind.v2.Sources.SqliteSourceRegistry($"Data Source={virtualSoulfindV2SourcesPath};"));
        services.AddSingleton<VirtualSoulfind.v2.Intents.IIntentQueue, VirtualSoulfind.v2.Intents.InMemoryIntentQueue>();
        services.AddSingleton<VirtualSoulfind.v2.Matching.IMatchEngine, VirtualSoulfind.v2.Matching.SimpleMatchEngine>();
        services.AddSingleton<VirtualSoulfind.v2.Fingerprinting.IAudioFingerprintService, VirtualSoulfind.v2.Fingerprinting.NoopAudioFingerprintService>();
        services.AddSingleton<VirtualSoulfind.v2.Planning.IPlanner>(sp =>
        {
            var root = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>().CurrentValue.VirtualSoulfindV2;
            return new VirtualSoulfind.v2.Planning.MultiSourcePlanner(
                sp.GetRequiredService<VirtualSoulfind.v2.Catalogue.ICatalogueStore>(),
                sp.GetRequiredService<VirtualSoulfind.v2.Sources.ISourceRegistry>(),
                sp.GetRequiredService<IEnumerable<VirtualSoulfind.v2.Backends.IContentBackend>>(),
                sp.GetRequiredService<Common.Moderation.IModerationProvider>(),
                sp.GetRequiredService<Common.Moderation.PeerReputationService>(),
                root.DefaultMode);
        });
        services.AddSingleton<VirtualSoulfind.v2.Resolution.IResolver, VirtualSoulfind.v2.Resolution.SimpleResolver>();
        services.AddSingleton<VirtualSoulfind.v2.Processing.IIntentQueueProcessor, VirtualSoulfind.v2.Processing.IntentQueueProcessor>();
        services.AddSingleton<VirtualSoulfind.v2.Reconciliation.ILibraryReconciliationService, VirtualSoulfind.v2.Reconciliation.LibraryReconciliationService>();
        services.AddSingleton<VirtualSoulfind.v2.Processing.IntentQueueProcessorBackgroundService>();
        if (optionsAtStartup.Feature.VirtualSoulfind && optionsAtStartup.VirtualSoulfindV2.Enabled)
        {
            services.AddHostedService(sp => sp.GetRequiredService<VirtualSoulfind.v2.Processing.IntentQueueProcessorBackgroundService>());
        }

        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.LocalLibraryBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.NativeMeshBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.MeshDhtBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.HttpBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.WebDavBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.S3Backend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.TorrentBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.LanBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.SoulseekBackend>();

        return services;
    }
}
