// <copyright file="ExperimentalFeatureGraphServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using slskd.Audio;
using slskd.AudioCore;
using slskd.Configuration;
using slskd.VirtualSoulfind.Core;
using slskd.Telemetry;
using slskd.Streaming;
using slskd.Messaging;
using slskd.Common.Security;
using slskd.DhtRendezvous;
using slskd.DhtRendezvous.Security;
using slskd.Integrations.AcoustId;
using slskd.Integrations.AutoTagging;
using slskd.Integrations.Chromaprint;
using slskd.Integrations.FTP;
using slskd.Integrations.MetadataFacade;
using slskd.Integrations.MusicBrainz;
using slskd.Integrations.Pushbullet;
using slskd.LibraryHealth;
using slskd.Mesh;
using slskd.Mesh.Gossip;
using slskd.Mesh.Governance;
using slskd.Mesh.Realm;
using slskd.Mesh.Realm.Bridge;
using slskd.Relay;
using slskd.Signals;
using slskd.SocialFederation;
using slskd.SongID;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using slskd.Transfers.MultiSource;
using slskd.Transfers.MultiSource.Discovery;
using slskd.Transfers.Rescue;
using Soulseek;

public static class ExperimentalFeatureGraphServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdExperimentalFeatureGraph(
        this IServiceCollection services,
        IConfiguration configuration,
        slskd.Options optionsAtStartup)
    {
        services.AddSlskdMultiSourceFeatureServices(optionsAtStartup);

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

        // Note: IDhtClient is registered later in MeshCore section (line ~1456) as InMemoryDhtClient
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IDhtRateLimiter>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            var maxOpsPerMin = options.CurrentValue.VirtualSoulfind?.ShadowIndex?.MaxDhtOperationsPerMinute ?? 60;
            return new VirtualSoulfind.ShadowIndex.DhtRateLimiter(
                sp.GetRequiredService<ILogger<VirtualSoulfind.ShadowIndex.DhtRateLimiter>>(),
                maxOpsPerMin);
        });
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardPublisher, VirtualSoulfind.ShadowIndex.ShardPublisher>();
        services.AddHostedService(sp => (VirtualSoulfind.ShadowIndex.ShardPublisher)sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IShardPublisher>());
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShadowIndexQuery, VirtualSoulfind.ShadowIndex.ShadowIndexQuery>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardMerger, VirtualSoulfind.ShadowIndex.ShardMerger>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IShardCache, VirtualSoulfind.ShadowIndex.ShardCache>();
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IDhtRateLimiter, VirtualSoulfind.ShadowIndex.DhtRateLimiter>();
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
        services.AddHostedService(sp => (VirtualSoulfind.DisasterMode.SoulseekHealthMonitor)sp.GetRequiredService<VirtualSoulfind.DisasterMode.ISoulseekHealthMonitor>());
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

        // Register ITransferProgressProxy BEFORE BridgeApi (BridgeApi depends on it)
        services.AddSingleton<VirtualSoulfind.Bridge.IPeerIdAnonymizer, VirtualSoulfind.Bridge.PeerIdAnonymizer>();
        services.AddSingleton<VirtualSoulfind.Bridge.IFilenameGenerator, VirtualSoulfind.Bridge.FilenameGenerator>();
        services.AddSingleton<VirtualSoulfind.Bridge.IRoomSceneMapper, VirtualSoulfind.Bridge.RoomSceneMapper>();
        services.AddSingleton<VirtualSoulfind.Bridge.ITransferProgressProxy, VirtualSoulfind.Bridge.TransferProgressProxy>();
        services.AddSingleton<VirtualSoulfind.Bridge.IBridgeApi, VirtualSoulfind.Bridge.BridgeApi>();
        services.AddSingleton<VirtualSoulfind.Bridge.Protocol.SoulseekProtocolParser>();

        // BridgeProxyServer is opt-in because construction has blocked startup in local-dev runs.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SLSKDN_E2E_SKIP_BRIDGE_PROXY")) &&
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
        services.AddHostedService(sp => sp.GetRequiredService<VirtualSoulfind.v2.Processing.IntentQueueProcessorBackgroundService>());

        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.LocalLibraryBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.NativeMeshBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.MeshDhtBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.HttpBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.WebDavBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.S3Backend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.TorrentBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.LanBackend>();
        services.AddSingleton<VirtualSoulfind.v2.Backends.IContentBackend, VirtualSoulfind.v2.Backends.SoulseekBackend>();

        // Content Domain Provider Registry (P3: Custom Domain Matching Logic)
        services.AddContentDomainProviders();

        // Peer Reputation System (T-MCP04)
        services.AddSingleton<Common.Moderation.IPeerReputationStore>(sp =>
        {
            var dataProtection = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var protector = dataProtection.CreateProtector("PeerReputation");
            var storagePath = Path.Combine(Program.AppDirectory, "reputation", "peers.db");
            return new Common.Moderation.PeerReputationStore(
                sp.GetRequiredService<ILogger<Common.Moderation.PeerReputationStore>>(),
                protector,
                storagePath);
        });
        services.AddSingleton<Common.Moderation.PeerReputationService>();

        // MediaCore (Phase 9)
        services.AddOptions<MediaCore.MediaCoreOptions>();
        services.AddSingleton<MediaCore.IDescriptorValidator, MediaCore.DescriptorValidator>();
        services.AddSingleton<MediaCore.IDescriptorPublisher>(sp =>
            new MediaCore.DescriptorPublisher(
                sp.GetRequiredService<ILogger<MediaCore.DescriptorPublisher>>(),
                sp.GetRequiredService<MediaCore.IDescriptorValidator>(),
                sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>(),
                sp.GetRequiredService<IOptions<MediaCore.MediaCoreOptions>>()));
        services.AddSingleton<MediaCore.IContentIdRegistry, MediaCore.ContentIdRegistry>();
        services.AddSingleton<MediaCore.IIpldMapper, MediaCore.IpldMapper>();
        services.AddSingleton<MediaCore.IPerceptualHasher, MediaCore.PerceptualHasher>();
        services.AddSingleton<MediaCore.IMetadataPortability, MediaCore.MetadataPortability>();
        services.AddSingleton<MediaCore.IContentDescriptorPublisher, MediaCore.ContentDescriptorPublisher>();
        services.AddSingleton<MediaCore.IDescriptorRetriever, MediaCore.DescriptorRetriever>();
        services.AddSingleton<MediaCore.IFuzzyMatcher, MediaCore.FuzzyMatcher>();
        services.AddSingleton<MediaCore.IMediaCoreStatsService, MediaCore.MediaCoreStatsService>();

        // PodCore services
        services.AddSingleton<PodCore.IPodDhtPublisher, PodCore.PodDhtPublisher>();
        services.AddSingleton<PodCore.IPodMembershipService, PodCore.PodMembershipService>();
        services.AddSingleton<PodCore.IPodMembershipVerifier, PodCore.PodMembershipVerifier>();
        services.AddSingleton<PodCore.IPodDiscoveryService, PodCore.PodDiscoveryService>();
        services.AddSingleton<PodCore.IPodJoinLeaveService, PodCore.PodJoinLeaveService>();
        services.AddSingleton<PodCore.IPodMessageRouter>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PodCore.PodMessageRouter>>();
            var podService = sp.GetRequiredService<PodCore.IPodService>();
            var overlayClient = sp.GetRequiredService<Mesh.Overlay.IOverlayClient>();
            var controlSigner = sp.GetRequiredService<Mesh.Overlay.IControlSigner>();
            var peerResolution = sp.GetRequiredService<PodCore.IPeerResolutionService>();
            var privacyLayer = sp.GetService<Mesh.Privacy.IPrivacyLayer>();
            return new PodCore.PodMessageRouter(logger, podService, overlayClient, controlSigner, peerResolution, privacyLayer);
        });
        services.AddSingleton<PodCore.IMessageSigner, PodCore.MessageSigner>();

        // MultiSource MediaCore integration
        services.AddSingleton<IMediaCoreSwarmIntelligence, MediaCoreSwarmIntelligence>();
        services.AddSingleton<IMediaCoreSwarmService, MediaCoreSwarmService>();
        services.AddSingleton<slskd.Transfers.MultiSource.Scheduling.IChunkScheduler, slskd.Transfers.MultiSource.Scheduling.MediaCoreChunkScheduler>();
        services.AddSingleton<MediaCore.IIpldMapper, MediaCore.IpldMapper>();
        services.AddSingleton<MediaCore.IFuzzyMatcher, MediaCore.FuzzyMatcher>();
        services.AddSingleton<MediaCore.IContentDescriptorSource, MediaCore.ShadowIndexDescriptorSource>();

        // PodCore (Phase 10 - SQLite persistence)
        var podDbPath = Path.Combine(Program.AppDirectory, "pods.db");
        services.AddDbContextFactory<PodCore.PodDbContext>(options =>
        {
            options.UseSqlite($"Data Source={podDbPath}");
        });

        // Ensure pod database is created with secure permissions and migrations
        using (var podContext = new PodCore.PodDbContext(
            new DbContextOptionsBuilder<PodCore.PodDbContext>()
                .UseSqlite($"Data Source={podDbPath}")
                .Options))
        {
            podContext.Database.EnsureCreated();

            // Apply schema migrations for existing databases (synchronous since we're in ConfigureServices)
            try
            {
                var connection = podContext.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }

                // Check if AllowGuests column exists, if not add it
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "PRAGMA table_info(Pods)";
                using var reader = checkCmd.ExecuteReader();
                var hasAllowGuests = false;
                while (reader.Read())
                {
                    if (reader.GetString(1) == "AllowGuests")
                    {
                        hasAllowGuests = true;
                        break;
                    }
                }

                reader.Close();

                if (!hasAllowGuests)
                {
                    Log.Information("[PodDb] Adding missing AllowGuests column to Pods table");
                    using var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE Pods ADD COLUMN AllowGuests INTEGER NOT NULL DEFAULT 0";
                    alterCmd.ExecuteNonQuery();
                    Log.Information("[PodDb] AllowGuests column added successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PodDb] Could not apply schema migration (database may be new or already up to date)");
            }

            // SECURITY: Set restrictive file permissions on the database (Unix/Linux only)
            if (System.IO.File.Exists(podDbPath))
            {
                try
                {
                    // Unix chmod 600 (owner read/write only) - requires Mono.Posix.NETStandard package
                    // For now, just log warning if on Windows (file permissions are more complex there)
                    if (!OperatingSystem.IsWindows())
                    {
                        Log.Information("Pod database created at {Path} - ensure file permissions are secure (chmod 600)", podDbPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not verify secure file permissions on pods.db");
                }
            }
        }

        // Pod membership signer
        services.AddSingleton<PodCore.IPodMembershipSigner, PodCore.PodMembershipSigner>();

        // Pod DHT publishing + discovery
        services.AddSingleton<PodCore.IPodPublisher>(sp =>
        {
            Log.Debug("[DI] Constructing PodPublisher...");
            Log.Debug("[DI] Resolving IMeshDhtClient for PodPublisher...");
            var dht = sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>();
            Log.Debug("[DI] Resolving IServiceScopeFactory for PodPublisher...");
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            Log.Debug("[DI] Resolving ILogger<PodPublisher> for PodPublisher...");
            var logger = sp.GetRequiredService<ILogger<PodCore.PodPublisher>>();
            Log.Debug("[DI] All PodPublisher dependencies resolved, creating instance...");
            var service = new PodCore.PodPublisher(dht, scopeFactory, logger);
            Log.Debug("[DI] PodPublisher constructed");
            return service;
        });
        services.AddSingleton<PodCore.IPodDiscovery, PodCore.PodDiscovery>();

        // Peer resolution service (for PeerReputation lookup)
        services.AddSingleton<PodCore.IPeerResolutionService, PodCore.PeerResolutionService>();

        // Soulseek chat bridge
        services.AddSingleton<PodCore.ISoulseekChatBridge>(sp =>
        {
            Log.Debug("[DI] Constructing SoulseekChatBridge...");
            Log.Debug("[DI] Resolving IServiceScopeFactory for SoulseekChatBridge...");
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            Log.Debug("[DI] Resolving IRoomService for SoulseekChatBridge...");
            var roomService = sp.GetRequiredService<IRoomService>();
            Log.Debug("[DI] Resolving ISoulseekClient for SoulseekChatBridge...");
            var soulseekClient = sp.GetRequiredService<ISoulseekClient>();
            Log.Debug("[DI] Resolving ILogger<SoulseekChatBridge> for SoulseekChatBridge...");
            var logger = sp.GetRequiredService<ILogger<PodCore.SoulseekChatBridge>>();
            Log.Debug("[DI] All SoulseekChatBridge dependencies resolved, creating instance...");
            var service = new PodCore.SoulseekChatBridge(scopeFactory, roomService, soulseekClient, logger);
            Log.Debug("[DI] SoulseekChatBridge constructed");
            return service;
        });

        // Main pod service (SQLite-backed with persistence)
        services.AddSingleton<PodCore.IPodService>(sp =>
        {
            Log.Debug("[DI] Constructing SqlitePodService...");
            Log.Debug("[DI] Resolving IDbContextFactory<PodDbContext> for SqlitePodService...");
            var factory = sp.GetRequiredService<IDbContextFactory<PodCore.PodDbContext>>();
            Log.Debug("[DI] Resolving IPodPublisher for SqlitePodService (optional)...");
            var podPublisher = sp.GetRequiredService<PodCore.IPodPublisher>();
            Log.Debug("[DI] Resolving IPodMembershipSigner for SqlitePodService (optional)...");
            var membershipSigner = sp.GetRequiredService<PodCore.IPodMembershipSigner>();
            Log.Debug("[DI] Resolving ILogger<SqlitePodService> for SqlitePodService...");
            var logger = sp.GetRequiredService<ILogger<PodCore.SqlitePodService>>();
            Log.Debug("[DI] Resolving IServiceScopeFactory for SqlitePodService (for lazy IContentLinkService resolution)...");
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            Log.Debug("[DI] All SqlitePodService dependencies resolved, creating instance (IContentLinkService will be resolved lazily via scope)...");
            var service = new PodCore.SqlitePodService(factory, podPublisher, membershipSigner, logger, scopeFactory);
            Log.Debug("[DI] SqlitePodService constructed");
            return service;
        });

        services.AddSingleton<PodCore.GoldStarClubService>();
        services.AddSingleton<PodCore.IGoldStarClubService>(sp => sp.GetRequiredService<PodCore.GoldStarClubService>());
        services.AddHostedService(sp => sp.GetRequiredService<PodCore.GoldStarClubService>());

        // Pod messaging service (SQLite-backed)
        services.AddScoped<PodCore.IPodMessaging>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<PodCore.PodDbContext>>();
            var dbContext = factory.CreateDbContext();
            return new PodCore.SqlitePodMessaging(
                dbContext,
                sp.GetRequiredService<ILogger<PodCore.SqlitePodMessaging>>(),
                sp.GetRequiredService<PodCore.IPodMessageRouter>());
        });

        // Pod message storage service with full-text search and retention policies
        services.AddScoped<PodCore.IPodMessageStorage>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<PodCore.PodDbContext>>();
            var dbContext = factory.CreateDbContext();
            return new PodCore.SqlitePodMessageStorage(
                dbContext,
                sp.GetRequiredService<ILogger<PodCore.SqlitePodMessageStorage>>());
        });

        // Content link service for pod content validation
        services.AddScoped<PodCore.IContentLinkService>(sp =>
        {
            var musicBrainzClient = sp.GetRequiredService<Integrations.MusicBrainz.IMusicBrainzClient>();
            return new PodCore.ContentLinkService(
                musicBrainzClient,
                sp.GetRequiredService<ILogger<PodCore.ContentLinkService>>());
        });

        // Pod message backfill service for synchronizing missed messages
        services.AddScoped<PodCore.IPodMessageBackfill>(sp =>
        {
            var messageStorage = sp.GetRequiredService<PodCore.IPodMessageStorage>();
            var messageRouter = sp.GetRequiredService<PodCore.IPodMessageRouter>();
            var overlayClient = sp.GetRequiredService<Mesh.Overlay.IOverlayClient>();
            var podService = sp.GetRequiredService<PodCore.IPodService>();
            var profileService = sp.GetRequiredService<Identity.IProfileService>();
            return new PodCore.PodMessageBackfill(
                messageStorage,
                messageRouter,
                overlayClient,
                podService,
                profileService,
                sp.GetRequiredService<ILogger<PodCore.PodMessageBackfill>>());
        });

        // Pod opinion service for managing content variant opinions
        services.AddScoped<PodCore.IPodOpinionService>(sp =>
        {
            var podService = sp.GetRequiredService<PodCore.IPodService>();
            var dhtClient = sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>();
            return new PodCore.PodOpinionService(
                podService,
                dhtClient,
                sp.GetRequiredService<Mesh.Transport.Ed25519Signer>(),
                sp.GetRequiredService<ILogger<PodCore.PodOpinionService>>());
        });

        // Pod opinion aggregator for weighted opinion analysis and consensus
        services.AddScoped<PodCore.IPodOpinionAggregator>(sp =>
        {
            var podService = sp.GetRequiredService<PodCore.IPodService>();
            var opinionService = sp.GetRequiredService<PodCore.IPodOpinionService>();
            var messageStorage = sp.GetRequiredService<PodCore.IPodMessageStorage>();
            return new PodCore.PodOpinionAggregator(
                podService,
                opinionService,
                messageStorage,
                sp.GetRequiredService<ILogger<PodCore.PodOpinionAggregator>>());
        });

        // Background service for periodic pod metadata refresh
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Constructing PodPublisherBackgroundService hosted service...");
            var service = ActivatorUtilities.CreateInstance<PodCore.PodPublisherBackgroundService>(p);
            Log.Debug("[DI] PodPublisherBackgroundService constructed");
            return service;
        });

        // Typed options (Phase 11) - bind under slskd: namespace to match YAML provider
        var slskdSection = configuration.GetSection(Program.AppName);
        services.AddOptions<Core.SwarmOptions>().Bind(slskdSection.GetSection("Swarm"));
        services.AddOptions<Core.SecurityOptions>().Bind(slskdSection.GetSection("Security"));
        services.AddOptions<Common.Security.AdversarialOptions>().Bind(slskdSection.GetSection("Security:Adversarial"));
        services.AddOptions<PodCore.PodMessageSignerOptions>().Bind(slskdSection.GetSection("PodCore:Security"));
        services.AddOptions<PodCore.PodJoinOptions>().Bind(slskdSection.GetSection("PodCore:Join"));

        // Transport policy manager for per-peer/per-pod transport policies
        services.AddSingleton<Mesh.Transport.TransportPolicyManager>();

        // Anonymity transport selector with policy-aware selection
        services.AddSingleton<Common.Security.IAnonymityTransportSelector>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Common.Security.AdversarialOptions>>();
            var policyManager = sp.GetRequiredService<Mesh.Transport.TransportPolicyManager>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Common.Security.AnonymityTransportSelector>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var overlayDataPlane = sp.GetService<Mesh.Overlay.IOverlayDataPlane>();
            return new Common.Security.AnonymityTransportSelector(options.Value, policyManager, logger, loggerFactory, overlayDataPlane);
        });

        // Privacy layer for traffic analysis protection
        services.AddSingleton<Mesh.Privacy.IPrivacyLayer>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Common.Security.AdversarialOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Privacy.PrivacyLayer>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new Mesh.Privacy.PrivacyLayer(logger, loggerFactory, options.Value.Privacy);
        });

        services.AddOptions<Core.BrainzOptions>().Bind(configuration.GetSection($"{Program.AppName}:Brainz"));
        services.AddOptions<Mesh.MeshOptions>().Bind(configuration.GetSection($"{Program.AppName}:Mesh")); // transport prefs
        services.AddOptions<Mesh.MeshSyncSecurityOptions>().Bind(configuration.GetSection($"{Program.AppName}:Mesh:SyncSecurity"));
        services.AddOptions<Mesh.MeshTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Mesh:Transport"));
        services.AddOptions<Mesh.TorTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Mesh:Transport:Tor"));
        services.AddOptions<Mesh.I2PTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Mesh:Transport:I2P"));
        services.AddOptions<Common.Security.WebSocketTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Security:Adversarial:Transport:WebSocket"));
        services.AddOptions<Common.Security.HttpTunnelTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Security:Adversarial:Transport:HttpTunnel"));
        services.AddOptions<Common.Security.Obfs4TransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Security:Adversarial:Transport:Obfs4"));
        services.AddOptions<Common.Security.MeekTransportOptions>().Bind(configuration.GetSection($"{Program.AppName}:Security:Adversarial:Transport:Meek"));

        // Register options as singletons for direct injection (temporary workaround)
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Mesh.TorTransportOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Mesh.I2PTransportOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Common.Security.WebSocketTransportOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Common.Security.HttpTunnelTransportOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Common.Security.Obfs4TransportOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<Common.Security.MeekTransportOptions>>().Value);
        services.AddOptions<MediaCore.MediaCoreOptions>().Bind(configuration.GetSection($"{Program.AppName}:MediaCore"));
        services.AddOptions<Mesh.Overlay.OverlayOptions>().Bind(configuration.GetSection($"{Program.AppName}:Overlay"));
        services.AddOptions<Mesh.Overlay.DataOverlayOptions>().Bind(configuration.GetSection($"{Program.AppName}:OverlayData"));
        services.PostConfigure<Mesh.MeshOptions>(options =>
        {
            options.DataDirectory = Program.ResolveAppRelativePath(options.DataDirectory, "data");
        });
        services.PostConfigure<Mesh.Overlay.OverlayOptions>(options =>
        {
            options.KeyPath = Program.ResolveAppRelativePath(options.KeyPath, "mesh-overlay.key");
        });
        services.AddOptions<Mesh.ServiceFabric.MeshGatewayOptions>()
            .Bind(configuration.GetSection($"{Program.AppName}:MeshGateway"))
            .Validate(options => options.Validate().IsValid, "MeshGateway configuration is invalid.")
            .ValidateOnStart();

        // Realm services (T-REALM-01, T-REALM-02, T-REALM-04)
        Log.Debug("[DI] Configuring Realm services...");
        services.Configure<Mesh.Realm.RealmConfig>(configuration.GetSection($"{Program.AppName}:Realm"));
        services.Configure<Mesh.Realm.MultiRealmConfig>(configuration.GetSection($"{Program.AppName}:MultiRealm"));
        services.AddRealmServices();

        // Social federation services (required by bridges)
        Log.Debug("[DI] Configuring Social Federation services...");
        services.AddSocialFederation();
        services.AddBridgeServices();

        // Governance and Gossip services (T-REALM-03)
        Log.Debug("[DI] Configuring Governance and Gossip services...");
        services.AddGovernanceServices();
        services.AddGossipServices();

        // MeshCore (Phase 8 implementation)
        Log.Debug("[DI] Configuring MeshCore services...");
        services.Configure<Mesh.MeshOptions>(configuration.GetSection($"{Program.AppName}:Mesh"));
        services.AddSingleton<Mesh.INatDetector, Mesh.StunNatDetector>();
        services.AddSingleton<Mesh.Nat.IUdpHolePuncher, Mesh.Nat.UdpHolePuncher>();
        services.AddSingleton<Mesh.Nat.IRelayClient, Mesh.Nat.RelayClient>();
        services.AddSingleton<Mesh.Nat.INatTraversalService, Mesh.Nat.NatTraversalService>();

        // DHT: use in-memory Kademlia-style implementation for now
        services.AddSingleton<VirtualSoulfind.ShadowIndex.IDhtClient>(sp =>
        {
            Log.Debug("[DI] Constructing InMemoryDhtClient...");
            Log.Debug("[DI] Resolving ILogger<InMemoryDhtClient>...");
            var logger = sp.GetRequiredService<ILogger<Mesh.Dht.InMemoryDhtClient>>();
            Log.Debug("[DI] Resolving IOptions<MeshOptions> for InMemoryDhtClient...");
            var options = sp.GetRequiredService<IOptions<Mesh.MeshOptions>>();
            Log.Debug("[DI] Resolving MeshStatsCollector for InMemoryDhtClient (optional)...");
            var statsCollector = sp.GetRequiredService<Mesh.MeshStatsCollector>();
            Log.Debug("[DI] All InMemoryDhtClient dependencies resolved, creating instance...");
            var service = new Mesh.Dht.InMemoryDhtClient(logger, options, statsCollector);
            Log.Debug("[DI] InMemoryDhtClient constructed");
            return service;
        });
        services.AddSingleton<Mesh.Dht.IMeshDhtClient>(sp =>
        {
            Log.Debug("[DI] Constructing MeshDhtClient...");
            Log.Debug("[DI] Resolving ILogger<MeshDhtClient>...");
            var logger = sp.GetRequiredService<ILogger<Mesh.Dht.MeshDhtClient>>();
            Log.Debug("[DI] Resolving IDhtClient for MeshDhtClient...");
            var dhtClient = sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtClient>();
            Log.Debug("[DI] All MeshDhtClient dependencies resolved, creating instance (DhtService will be resolved lazily to break circular dependency)...");
            var service = new Mesh.Dht.MeshDhtClient(logger, dhtClient, sp, sp.GetService<IOptions<Mesh.MeshOptions>>());
            Log.Debug("[DI] MeshDhtClient constructed");
            return service;
        });
        services.AddSingleton<Mesh.Dht.IPeerDescriptorPublisher>(sp =>
        {
            Log.Debug("[DI] Constructing PeerDescriptorPublisher...");
            var service = new Mesh.Dht.PeerDescriptorPublisher(
                sp.GetRequiredService<ILogger<Mesh.Dht.PeerDescriptorPublisher>>(),
                sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>(),
                sp.GetRequiredService<IOptions<Mesh.MeshOptions>>(),
                sp.GetRequiredService<Mesh.INatDetector>(),
                sp.GetRequiredService<IOptions<Mesh.MeshTransportOptions>>(),
                sp.GetRequiredService<IOptions<Mesh.Overlay.OverlayOptions>>(),
                sp.GetRequiredService<Mesh.Transport.DescriptorSigningService>(),
                sp.GetService<Mesh.Overlay.IKeyStore>());
            Log.Debug("[DI] PeerDescriptorPublisher constructed");
            return service;
        });
        services.AddSingleton<Mesh.IMeshDirectory, Mesh.Dht.ContentDirectory>();
        services.AddSingleton<Mesh.IMeshAdvanced>(sp => new Mesh.MeshAdvanced(
            sp.GetRequiredService<ILogger<Mesh.MeshAdvanced>>(),
            sp.GetRequiredService<Mesh.IMeshDirectory>(),
            sp.GetRequiredService<Mesh.MeshStatsCollector>(),
            sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>(),
            sp.GetRequiredService<Mesh.Nat.INatTraversalService>()));
        services.AddSingleton<Mesh.MeshStatsCollector>(sp =>
        {
            Log.Debug("[DI] Constructing MeshStatsCollector...");
            var service = new Mesh.MeshStatsCollector(
                sp.GetRequiredService<ILogger<Mesh.MeshStatsCollector>>(),
                sp);
            Log.Debug("[DI] MeshStatsCollector constructed");
            return service;
        });
        services.AddSingleton<Mesh.IMeshStatsCollector>(sp => sp.GetRequiredService<Mesh.MeshStatsCollector>());
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Resolving MeshBootstrapService hosted service...");
            var service = ActivatorUtilities.CreateInstance<Mesh.Bootstrap.MeshBootstrapService>(p);
            Log.Debug("[DI] MeshBootstrapService hosted service resolved");
            return service;
        });
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Resolving PeerDescriptorRefreshService hosted service...");
            var service = ActivatorUtilities.CreateInstance<Mesh.Dht.PeerDescriptorRefreshService>(p);
            Log.Debug("[DI] PeerDescriptorRefreshService hosted service resolved");
            return service;
        });
        services.AddSingleton<Mesh.Dht.IContentPeerPublisher>(sp =>
        {
            Log.Debug("[DI] Constructing ContentPeerPublisher...");
            Log.Debug("[DI] Resolving ILogger<ContentPeerPublisher>...");
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Dht.ContentPeerPublisher>>();
            Log.Debug("[DI] Resolving IMeshDhtClient for ContentPeerPublisher...");
            var dht = sp.GetRequiredService<Mesh.Dht.IMeshDhtClient>();
            Log.Debug("[DI] Resolving IOptions<MeshOptions> for ContentPeerPublisher...");
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.MeshOptions>>();
            Log.Debug("[DI] All ContentPeerPublisher dependencies resolved, creating instance...");
            var service = new Mesh.Dht.ContentPeerPublisher(logger, dht, options);
            Log.Debug("[DI] ContentPeerPublisher constructed");
            return service;
        });
        services.AddSingleton<Mesh.Dht.IContentPeerHintService>(sp =>
        {
            Log.Debug("[DI] Constructing ContentPeerHintService...");
            var service = new Mesh.Dht.ContentPeerHintService(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Dht.ContentPeerHintService>>(),
                sp.GetRequiredService<Mesh.Dht.IContentPeerPublisher>());
            Log.Debug("[DI] ContentPeerHintService constructed");
            return service;
        });
        services.AddHostedService(sp => (Mesh.Dht.ContentPeerHintService)sp.GetRequiredService<Mesh.Dht.IContentPeerHintService>());
        services.AddSingleton<Mesh.Health.IMeshHealthService, Mesh.Health.MeshHealthService>();

        // Service Fabric (client + directory + validation)
        services.AddSingleton<Mesh.ServiceFabric.IMeshServiceDescriptorValidator, Mesh.ServiceFabric.MeshServiceDescriptorValidator>();
        services.AddSingleton<Mesh.ServiceFabric.IMeshServiceDirectory, Mesh.ServiceFabric.DhtMeshServiceDirectory>();
        services.AddSingleton<Mesh.ServiceFabric.IMeshServiceClient, Mesh.ServiceFabric.MeshServiceClient>();
        services.AddOptions<Mesh.ServiceFabric.MeshServiceFabricOptions>().Bind(configuration.GetSection($"{Program.AppName}:MeshServiceFabric"));
        services.AddSingleton<Mesh.ServiceFabric.MeshServiceRouter>();

        // MeshContentFetcher requires IMeshServiceClient, so register after it
        services.AddSingleton<IMeshContentFetcher, MeshContentFetcher>();

        // Kademlia routing table using overlay key material for node ID
        services.AddSingleton<Mesh.Dht.KademliaRoutingTable>(sp =>
        {
            var keyStore = sp.GetRequiredService<Mesh.Overlay.IKeyStore>();
            var pubKey = keyStore.Current.PublicKey;

            // KademliaRoutingTable expects 160-bit IDs (20 bytes). SHA1 gives exactly 20 bytes.
            var selfId = System.Security.Cryptography.SHA1.HashData(pubKey);

            return new Mesh.Dht.KademliaRoutingTable(selfId);
        });

        // DHT services for Kademlia operations
        services.AddSingleton<Mesh.Dht.KademliaRpcClient>(sp =>
        {
            Log.Debug("[DI] Constructing KademliaRpcClient...");
            Log.Debug("[DI] Resolving ILogger<KademliaRpcClient>...");
            var logger = sp.GetRequiredService<ILogger<Mesh.Dht.KademliaRpcClient>>();
            Log.Debug("[DI] Resolving IMeshServiceClient for KademliaRpcClient...");
            var meshClient = sp.GetRequiredService<Mesh.ServiceFabric.IMeshServiceClient>();
            Log.Debug("[DI] Resolving KademliaRoutingTable for KademliaRpcClient...");
            var routingTable = sp.GetRequiredService<Mesh.Dht.KademliaRoutingTable>();
            Log.Debug("[DI] Resolving IDhtClient for KademliaRpcClient...");
            var dhtClient = sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtClient>();
            Log.Debug("[DI] All KademliaRpcClient dependencies resolved, creating instance...");
            var service = new Mesh.Dht.KademliaRpcClient(logger, meshClient, routingTable, dhtClient);
            Log.Debug("[DI] KademliaRpcClient constructed");
            return service;
        });
        services.AddSingleton<Mesh.ServiceFabric.Services.DhtMeshService>();
        services.AddSingleton<Mesh.Dht.DhtService>(sp =>
        {
            Log.Debug("[DI] Constructing DhtService...");
            Log.Debug("[DI] Resolving ILogger<DhtService>...");
            var logger = sp.GetRequiredService<ILogger<Mesh.Dht.DhtService>>();
            Log.Debug("[DI] Resolving KademliaRoutingTable for DhtService...");
            var routingTable = sp.GetRequiredService<Mesh.Dht.KademliaRoutingTable>();
            Log.Debug("[DI] Resolving IDhtClient for DhtService...");
            var dhtClient = sp.GetRequiredService<VirtualSoulfind.ShadowIndex.IDhtClient>();
            Log.Debug("[DI] Resolving KademliaRpcClient for DhtService...");
            var rpcClient = sp.GetRequiredService<Mesh.Dht.KademliaRpcClient>();
            Log.Debug("[DI] Resolving IMeshMessageSigner for DhtService...");
            var messageSigner = sp.GetRequiredService<Mesh.IMeshMessageSigner>();
            Log.Debug("[DI] All DhtService dependencies resolved, creating instance...");
            var service = new Mesh.Dht.DhtService(logger, routingTable, dhtClient, rpcClient, messageSigner);
            Log.Debug("[DI] DhtService constructed");
            return service;
        });

        // Hole punching services for NAT traversal
        services.AddSingleton<Mesh.ServiceFabric.Services.HolePunchMeshService>();
        services.AddSingleton<Mesh.ServiceFabric.Services.MeshContentMeshService>();
        services.AddSingleton<Mesh.Nat.IHolePunchCoordinator, Mesh.Nat.HolePunchCoordinator>();
        services.AddSingleton<Mesh.Nat.INatTraversalService, Mesh.Nat.NatTraversalService>();

        // Private gateway service for VPN functionality (Phase 14)
        services.AddSingleton<DnsSecurityService>();
        services.AddSingleton<LocalPortForwarder>();
        services.AddSingleton<Mesh.ServiceFabric.Services.PrivateGatewayMeshService>();

        // Onion routing services (Phase 12)
        services.AddSingleton<Mesh.IMeshPeerManager, Mesh.MeshPeerManager>();
        services.AddSingleton<Mesh.IMeshTransportService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.MeshTransportService>>();
            var meshOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.MeshOptions>>();
            var anonymitySelector = sp.GetService<Common.Security.IAnonymityTransportSelector>();
            var adversarialOptions = sp.GetService<Microsoft.Extensions.Options.IOptions<Common.Security.AdversarialOptions>>();
            return new Mesh.MeshTransportService(logger, meshOptions, anonymitySelector, adversarialOptions);
        });

        services.AddSingleton<Mesh.MeshCircuitBuilder>(sp =>
        {
            var meshOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.MeshOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.MeshCircuitBuilder>>();
            var peerManager = sp.GetRequiredService<Mesh.IMeshPeerManager>();
            var transportSelector = sp.GetRequiredService<Common.Security.IAnonymityTransportSelector>();
            return new Mesh.MeshCircuitBuilder(meshOptions.Value, logger, peerManager, transportSelector);
        });
        services.AddSingleton<Mesh.IMeshCircuitBuilder>(sp => sp.GetRequiredService<Mesh.MeshCircuitBuilder>());
        services.AddHostedService(p =>
        {
            Log.Debug("[DI] Constructing CircuitMaintenanceService hosted service...");
            var service = ActivatorUtilities.CreateInstance<Mesh.CircuitMaintenanceService>(p);
            Log.Debug("[DI] CircuitMaintenanceService constructed");
            return service;
        });

        // Transport dialers (Tor/I2P integration Phase 2)
        var meshTransportoptionsAtStartup =
            configuration.GetSection($"{Program.AppName}:Mesh:Transport").Get<Mesh.MeshTransportOptions>() ??
            new Mesh.MeshTransportOptions();

        if (Mesh.QuicRuntime.IsAvailable())
        {
            services.AddSingleton<Mesh.Transport.ITransportDialer, Mesh.Transport.DirectQuicDialer>();
        }
        else if (meshTransportoptionsAtStartup.EnableDirect)
        {
            Log.Warning("[DI] Direct mesh transport is enabled but QUIC runtime support is unavailable; direct clearnet mesh circuits will be disabled until QUIC support is installed or a non-QUIC direct transport is configured");
        }

        services.AddSingleton<Mesh.Transport.ITransportDialer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Mesh.TorTransportOptions>>();
            var logger = sp.GetRequiredService<ILogger<Mesh.Transport.TorSocksDialer>>();
            return new Mesh.Transport.TorSocksDialer(options.Value, logger);
        });
        services.AddSingleton<Mesh.Transport.ITransportDialer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Mesh.I2PTransportOptions>>();
            var logger = sp.GetRequiredService<ILogger<Mesh.Transport.I2pSocksDialer>>();
            return new Mesh.Transport.I2pSocksDialer(options.Value, logger);
        });

        // Transport policy manager for per-peer/per-pod policies
        services.AddSingleton<Mesh.Transport.TransportPolicyManager>();

        // Transport downgrade protection
        services.AddSingleton<Mesh.Transport.TransportDowngradeProtector>();

        // Certificate pin management for peer identity verification
        services.AddSingleton<Mesh.Transport.CertificatePinManager>();

        // Rate limiting for DoS protection
        services.AddSingleton<Mesh.Transport.RateLimiter>();
        services.AddSingleton<Mesh.Transport.ConnectionThrottler>();
        services.AddSingleton<Mesh.Dht.DhtRateLimiter>();

        // DNS leak prevention verification
        services.AddSingleton<Mesh.Transport.DnsLeakPreventionVerifier>();

        // Transport selector for endpoint negotiation
        services.AddSingleton<Mesh.Transport.TransportSelector>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Mesh.MeshTransportOptions>>();
            var dialers = sp.GetServices<Mesh.Transport.ITransportDialer>();
            var policyManager = sp.GetRequiredService<Mesh.Transport.TransportPolicyManager>();
            var downgradeProtector = sp.GetRequiredService<Mesh.Transport.TransportDowngradeProtector>();
            var connectionThrottler = sp.GetRequiredService<Mesh.Transport.ConnectionThrottler>();
            var logger = sp.GetRequiredService<ILogger<Mesh.Transport.TransportSelector>>();
            return new Mesh.Transport.TransportSelector(
                options.Value,
                dialers,
                policyManager,
                downgradeProtector,
                connectionThrottler,
                logger);
        });

        // Descriptor signing service for cryptographic integrity
        services.AddSingleton<Mesh.Transport.DescriptorSigningService>();

        // Ed25519 signing implementation
        services.AddSingleton<Mesh.Transport.Ed25519Signer>();

        // Control envelope validator for replay protection and peer-bound verification
        services.AddSingleton<Mesh.Overlay.ControlEnvelopeValidator>();

        // KeyStore for Ed25519 signing (used by ControlSigner and MeshMessageSigner)
        services.AddSingleton<Mesh.Overlay.IKeyStore, Mesh.Overlay.FileKeyStore>();
        services.AddSingleton<Mesh.Overlay.IControlSigner, Mesh.Overlay.ControlSigner>();
        services.AddSingleton<Mesh.Overlay.IControlDispatcher>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Overlay.ControlDispatcher>>();
            var validator = sp.GetRequiredService<Mesh.Overlay.ControlEnvelopeValidator>();
            var privacyLayer = sp.GetService<Mesh.Privacy.IPrivacyLayer>();
            return new Mesh.Overlay.ControlDispatcher(logger, validator, privacyLayer);
        });

        // Mesh message signing for mesh sync security
        services.AddSingleton<Mesh.IMeshMessageSigner, Mesh.MeshMessageSigner>();
        services.AddSingleton(sp =>
        {
            var keyStore = sp.GetRequiredService<Mesh.Overlay.IKeyStore>();
            return keyStore.Current;
        });
        var overlayoptionsAtStartup = configuration.GetSection($"{Program.AppName}:Overlay").Get<Mesh.Overlay.OverlayOptions>() ?? new Mesh.Overlay.OverlayOptions();
        var dataOverlayoptionsAtStartup = configuration.GetSection($"{Program.AppName}:OverlayData").Get<Mesh.Overlay.DataOverlayOptions>() ?? new Mesh.Overlay.DataOverlayOptions();
        var dhtoptionsAtStartup = optionsAtStartup.DhtRendezvous;
        var quicPlatformSupported = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();
        var quicRuntimeAvailable = quicPlatformSupported && Mesh.QuicRuntime.IsAvailable();
        var quicOverlayRequested = overlayoptionsAtStartup.Enable && overlayoptionsAtStartup.EnableQuic;
        var quicDataRequested = dataOverlayoptionsAtStartup.Enable;
        var sharedMeshUdpRequested = DhtRendezvousService.ShouldUseSharedMeshUdpListener(dhtoptionsAtStartup, overlayoptionsAtStartup);

        if (Program.ShouldRunStandaloneUdpOverlayServer(overlayoptionsAtStartup.Enable, sharedMeshUdpRequested))
        {
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Constructing UdpOverlayServer hosted service...");
                var service = ActivatorUtilities.CreateInstance<Mesh.Overlay.UdpOverlayServer>(p);
                Log.Debug("[DI] UdpOverlayServer constructed");
                return service;
            });
        }
        else
        {
            Log.Debug("[DI] Standalone UDP overlay server skipped because the shared mesh UDP listener owns the configured overlay port");
        }

        if (quicOverlayRequested && quicRuntimeAvailable)
        {
#pragma warning disable CA1416 // Runtime platform guards apply in this branch
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Constructing QuicOverlayServer hosted service...");
                var service = Program.CreateQuicOverlayServer(p);
                Log.Debug("[DI] QuicOverlayServer constructed");
                return service;
            });
#pragma warning restore CA1416
        }
        else if (quicOverlayRequested)
        {
            Log.Warning("[DI] QUIC overlay requested but runtime/platform support is unavailable; skipping QuicOverlayServer hosted service");
        }
        else
        {
            Log.Debug("[DI] QUIC overlay disabled by configuration; skipping QuicOverlayServer hosted service");
        }

        if (quicOverlayRequested && quicRuntimeAvailable)
        {
#pragma warning disable CA1416 // Runtime platform guards apply in this branch.
            services.AddSingleton<Mesh.Overlay.IOverlayClient>(sp =>
            {
                return Program.CreateQuicOverlayClient(sp);
            });
#pragma warning restore CA1416
        }
        else
        {
            services.AddSingleton<Mesh.Overlay.IOverlayClient>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Overlay.UdpOverlayClient>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.Overlay.OverlayOptions>>();
                var privacyLayer = sp.GetService<Mesh.Privacy.IPrivacyLayer>();
                return new Mesh.Overlay.UdpOverlayClient(logger, options, privacyLayer);
            });
        }

        if (quicDataRequested && quicRuntimeAvailable)
        {
#pragma warning disable CA1416 // Runtime platform guards apply in this branch.
            services.AddSingleton<Mesh.Overlay.IOverlayDataPlane>(sp => Program.CreateQuicDataClient(sp));
#pragma warning restore CA1416
        }

        if (quicDataRequested && quicRuntimeAvailable)
        {
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Constructing QuicDataServer hosted service...");
                var service = ActivatorUtilities.CreateInstance<Mesh.Overlay.QuicDataServer>(p);
                Log.Debug("[DI] QuicDataServer constructed");
                return service;
            });
        }
        else if (quicDataRequested)
        {
            Log.Warning("[DI] QUIC data overlay requested but runtime/platform support is unavailable; skipping QuicDataServer hosted service");
        }
        else
        {
            Log.Debug("[DI] QUIC data overlay disabled by configuration; skipping QuicDataServer hosted service");
        }

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
        services.AddHostedService<UnderperformanceDetectorHostedService>();
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

        services.AddSlskdIntegrationAndMediaServices();

        return services;
    }
}
