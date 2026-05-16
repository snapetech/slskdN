// <copyright file="MediaCorePodServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using slskd.Messaging;
using slskd.Transfers.MultiSource;
using slskd.VirtualSoulfind.Core;
using Soulseek;

public static class MediaCorePodServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdMediaCorePodServices(this IServiceCollection services)
    {
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
                    if (!OperatingSystem.IsWindows())
                    {
                        System.IO.File.SetUnixFileMode(
                            podDbPath,
                            UnixFileMode.UserRead | UnixFileMode.UserWrite);
                        Log.Information("Secured pod database permissions at {Path} (0600)", podDbPath);
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

        return services;
    }
}
