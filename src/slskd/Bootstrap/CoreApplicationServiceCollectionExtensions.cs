// <copyright file="CoreApplicationServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using slskd.Configuration;
using slskd.Events;
using slskd.Files;
using slskd.Integrations.Lidarr;
using slskd.Integrations.FTP;
using slskd.Integrations.Scripts;
using slskd.Integrations.VPN;
using slskd.Integrations.Webhooks;
using slskd.ListeningParty;
using slskd.Mesh;
using slskd.Messaging;
using slskd.Relay;
using slskd.Search;
using slskd.Search.API;
using slskd.Shares;
using slskd.Sharing;
using slskd.SoulseekDiscovery;
using slskd.Streaming;
using slskd.Telemetry;
using slskd.Transfers;
using slskd.Transfers.Downloads;
using slskd.Transfers.Uploads;
using slskd.Users;
using Soulseek;

public static class CoreApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdCoreApplicationServices(
        this IServiceCollection services,
        slskd.Options optionsAtStartup,
        string dataDirectory)
    {
        services.AddSingleton<IWaiter, Waiter>();
        services.AddSingleton<ConnectionWatchdog, ConnectionWatchdog>();

        // wire up all of the connection strings we'll use. this is somewhat annoying but necessary because of the
        // intersection of run-time options (volatile, non-volatile) and ORM/mappers in use (EF, Dapper)
        var connectionStringDictionary = new ConnectionStringDictionary(Database.List
            .Select(database =>
            {
                var pooling = optionsAtStartup.Flags.NoSqlitePooling ? "False" : "True"; // don't invert and ToString this it is confusing

                var connStr = optionsAtStartup.Flags.Volatile
                    ? $"Data Source=file:{database}?mode=memory;Pooling={pooling};"
                    : $"Data Source={Path.Combine(dataDirectory, $"{database}.db")};Pooling={pooling}";

                return new KeyValuePair<Database, ConnectionString>(database, connStr);
            })
            .ToDictionary(x => x.Key, x => x.Value));

        services.AddDbContext<SearchDbContext>(connectionStringDictionary[Database.Search], optionsAtStartup);
        services.AddDbContext<TransfersDbContext>(connectionStringDictionary[Database.Transfers], optionsAtStartup);
        services.AddDbContext<MessagingDbContext>(connectionStringDictionary[Database.Messaging], optionsAtStartup);
        services.AddDbContext<EventsDbContext>(connectionStringDictionary[Database.Events], optionsAtStartup);

        services.AddSingleton<ConnectionStringDictionary>(connectionStringDictionary);

        if (!optionsAtStartup.Flags.Volatile)
        {
            // we're working with non-volatile database files, so register a Migrator to be used later in the
            // bootup process. the presence of a Migrator instance in DI determines whether a migration is needed.
            // it's important that we keep this list of databases in sync with those used by the application; anything
            // not in this list will not be able to be migrated.
            services.AddSingleton<Migrator>(_ => new Migrator(databases: connectionStringDictionary));
        }

        services.AddSingleton<EventService>();
        services.AddSingleton<EventBus>();

        services.AddSingleton<PrometheusService>();
        services.AddSingleton<ReportsService>();
        services.AddSingleton<TelemetryService>();

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

        services.AddSingleton<IBrowseTracker, BrowseTracker>();
        services.AddSingleton<IRoomTracker, RoomTracker>(_ => new RoomTracker(messageLimit: 250));

        services.AddSingleton<IMessagingService, MessagingService>();
        services.AddSingleton<Opinions.IOpinionService>(sp => new Opinions.OpinionService(
            sp.GetRequiredService<ILogger<Opinions.OpinionService>>(),
            Path.Combine(Program.AppDirectory, "opinions.json")));
        services.AddSingleton<ISoulseekDiscoveryService, SoulseekDiscoveryService>();
        services.AddSingleton<IConversationService>(sp =>
        {
            Log.Debug("[DI] Constructing ConversationService...");
            Log.Debug("[DI] Resolving ISoulseekClient for ConversationService...");
            var soulseekClient = sp.GetRequiredService<ISoulseekClient>();
            Log.Debug("[DI] Resolving EventBus for ConversationService...");
            var eventBus = sp.GetRequiredService<Events.EventBus>();
            Log.Debug("[DI] Resolving IDbContextFactory<MessagingDbContext> for ConversationService...");
            var contextFactory = sp.GetRequiredService<IDbContextFactory<Messaging.MessagingDbContext>>();
            Log.Debug("[DI] Resolving IPodService for ConversationService...");
            var podService = sp.GetRequiredService<PodCore.IPodService>();
            Log.Debug("[DI] All ConversationService dependencies resolved, creating instance...");
            var service = new Messaging.ConversationService(soulseekClient, eventBus, contextFactory, podService);
            Log.Debug("[DI] ConversationService constructed");
            return service;
        });

        services.AddSingleton<IShareService>(sp =>
        {
            Log.Debug("[DI] Constructing ShareService...");
            Log.Debug("[DI] Resolving FileService for ShareService...");
            var fileService = sp.GetRequiredService<FileService>();
            Log.Debug("[DI] Resolving IShareRepositoryFactory for ShareService...");
            var shareRepositoryFactory = sp.GetRequiredService<IShareRepositoryFactory>();
            Log.Debug("[DI] Resolving IOptionsMonitor<slskd.Options> for ShareService...");
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            Log.Debug("[DI] Resolving IModerationProvider for ShareService...");
            var moderationProvider = sp.GetRequiredService<Common.Moderation.IModerationProvider>();
            Log.Debug("[DI] Resolving IShareScanner for ShareService (optional)...");
            var scanner = sp.GetService<IShareScanner>();
            Log.Debug("[DI] Resolving IContentPeerHintService for ShareService (optional)...");
            var contentPeerHintService = sp.GetService<Mesh.Dht.IContentPeerHintService>();
            Log.Debug("[DI] All ShareService dependencies resolved, creating instance...");
            var service = new ShareService(
                fileService, shareRepositoryFactory, optionsMonitor, moderationProvider, scanner, contentPeerHintService);
            Log.Debug("[DI] ShareService constructed");
            return service;
        });
        services.AddSingleton<IShareRepository>(sp =>
            sp.GetRequiredService<IShareService>().GetLocalRepository());
        services.AddTransient<IShareRepositoryFactory, SqliteShareRepositoryFactory>();

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
        services.AddSingleton<IShareTokenService, ShareTokenService>();

        // Register search providers for Scene ↔ Pod Bridging
        services.AddSingleton<slskd.Search.Providers.ISearchProvider>(sp =>
            new slskd.Search.Providers.SceneSearchProvider(
                sp.GetRequiredService<ISoulseekClient>(),
                sp.GetRequiredService<slskd.Common.Security.ISoulseekSafetyLimiter>(),
                sp.GetRequiredService<ILogger<slskd.Search.Providers.SceneSearchProvider>>()));
        services.AddSingleton<slskd.Search.Providers.ISearchProvider>(sp =>
            new slskd.Search.Providers.PodSearchProvider(
                sp.GetRequiredService<slskd.DhtRendezvous.Search.IMeshOverlaySearchService>(),
                sp.GetRequiredService<ILogger<slskd.Search.Providers.PodSearchProvider>>()));

        services.AddSingleton<ISearchService>(sp =>
        {
            var searchHub = sp.GetRequiredService<IHubContext<SearchHub>>();
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            var soulseekClient = sp.GetRequiredService<ISoulseekClient>();
            var contextFactory = sp.GetRequiredService<IDbContextFactory<SearchDbContext>>();
            var safetyLimiter = sp.GetRequiredService<slskd.Common.Security.ISoulseekSafetyLimiter>();
            var eventBus = sp.GetService<slskd.Events.EventBus>();
            var disasterModeCoordinator = sp.GetService<slskd.VirtualSoulfind.DisasterMode.IDisasterModeCoordinator>();
            var meshSearchService = sp.GetService<slskd.VirtualSoulfind.DisasterMode.IMeshSearchService>();
            var meshOverlaySearchService = sp.GetService<slskd.DhtRendezvous.Search.IMeshOverlaySearchService>();
            var trafficObserver = sp.GetService<slskd.VirtualSoulfind.Capture.ITrafficObserver>();
            var searchProviders = sp.GetServices<slskd.Search.Providers.ISearchProvider>();

            return new SearchService(
                searchHub,
                optionsMonitor,
                soulseekClient,
                contextFactory,
                safetyLimiter,
                eventBus,
                disasterModeCoordinator,
                meshSearchService,
                meshOverlaySearchService,
                trafficObserver,
                searchProviders);
        });

        services.AddSingleton<IUsernameMatcher, RegexUsernameMatcher>();
        services.AddSingleton<IUserService, UserService>();

        services.AddSingleton<IRoomService, RoomService>();

        services.AddSingleton<IScheduledRateLimitService, ScheduledRateLimitService>();
        services.AddSingleton<IDownloadService>(sp =>
        {
            Log.Debug("[DI] Constructing DownloadService...");
            var service = new DownloadService(
                sp.GetRequiredService<IOptionsMonitor<slskd.Options>>(),
                sp.GetRequiredService<ISoulseekClient>(),
                sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>(),
                sp.GetRequiredService<FileService>(),
                sp.GetRequiredService<IRelayService>(),
                sp.GetRequiredService<IFTPService>(),
                sp.GetRequiredService<EventBus>(),
                sp.GetService<Transfers.MultiSource.Metrics.IPeerMetricsService>());
            Log.Debug("[DI] DownloadService constructed");
            return service;
        });
        services.AddSingleton<IUploadService>(sp =>
        {
            Log.Debug("[DI] Constructing UploadService...");
            Log.Debug("[DI] Resolving FileService for UploadService...");
            var fileService = sp.GetRequiredService<FileService>();
            Log.Debug("[DI] Resolving IUserService for UploadService...");
            var userService = sp.GetRequiredService<IUserService>();
            Log.Debug("[DI] Resolving ISoulseekClient for UploadService...");
            var soulseekClient = sp.GetRequiredService<ISoulseekClient>();
            Log.Debug("[DI] Resolving IOptionsMonitor<slskd.Options> for UploadService...");
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            Log.Debug("[DI] Resolving IShareService for UploadService...");
            var shareService = sp.GetRequiredService<IShareService>();
            Log.Debug("[DI] Resolving IRelayService for UploadService...");
            var relayService = sp.GetRequiredService<IRelayService>();
            Log.Debug("[DI] Resolving IDbContextFactory<TransfersDbContext> for UploadService...");
            var contextFactory = sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>();
            Log.Debug("[DI] Resolving EventBus for UploadService...");
            var eventBus = sp.GetRequiredService<EventBus>();
            Log.Debug("[DI] Resolving IScheduledRateLimitService for UploadService (optional)...");
            var scheduledRateLimitService = sp.GetService<IScheduledRateLimitService>();
            Log.Debug("[DI] All UploadService dependencies resolved, creating instance...");
            var service = new UploadService(
                fileService, userService, soulseekClient, optionsMonitor,
                shareService, relayService, contextFactory, eventBus, scheduledRateLimitService);
            Log.Debug("[DI] UploadService constructed");
            return service;
        });
        services.AddSingleton<ITransferService>(sp =>
        {
            Log.Debug("[DI] Constructing TransferService...");
            var service = new TransferService(
                sp.GetRequiredService<IUploadService>(),
                sp.GetRequiredService<IDownloadService>(),
                sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>());
            Log.Debug("[DI] TransferService constructed");
            return service;
        });
        services.AddSingleton<FileService>();
        services.AddSingleton<Transfers.AutoReplace.IAutoReplaceService, Transfers.AutoReplace.AutoReplaceService>();

        // Source ranking services (smart scoring + download history)
        var rankingDbPath = Path.Combine(Program.AppDirectory, "ranking.db");
        services.AddDbContextFactory<Transfers.Ranking.SourceRankingDbContext>(options =>
        {
            options.UseSqlite($"Data Source={rankingDbPath}");
        });

        // Ensure ranking database is created
        using (var rankingContext = new Transfers.Ranking.SourceRankingDbContext(
            new DbContextOptionsBuilder<Transfers.Ranking.SourceRankingDbContext>()
                .UseSqlite($"Data Source={rankingDbPath}")
                .Options))
        {
            rankingContext.Database.EnsureCreated();
        }

        services.AddSingleton<Transfers.Ranking.ISourceRankingService, Transfers.Ranking.SourceRankingService>();

        return services;
    }

    private static IServiceCollection AddDbContext<T>(
        this IServiceCollection services,
        string connectionString,
        slskd.Options optionsAtStartup)
        where T : DbContext
    {
        Log.Debug("Initializing database context {Name}", typeof(T).Name);

        try
        {
            services.AddDbContextFactory<T>(options =>
            {
                options.UseSqlite(connectionString);
                options.AddInterceptors(new SqliteConnectionOpenedInterceptor());

                if (optionsAtStartup.Debug && optionsAtStartup.Flags.LogSQL)
                {
                    options.LogTo(Log.Debug, LogLevel.Information);
                }
            });

            using var ctx = services
                .BuildServiceProvider()
                .GetRequiredService<IDbContextFactory<T>>()
                .CreateDbContext();

            Log.Debug("Ensuring {Contex} is created", typeof(T).Name);
            ctx.Database.EnsureCreated();

            ctx.Database.OpenConnection();
            var conn = ctx.Database.GetDbConnection();

            Log.Debug("Setting PRAGMAs for {Contex}", typeof(T).Name);
            using var initCommand = conn.CreateCommand();
            initCommand.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=1; PRAGMA optimize;";
            initCommand.ExecuteNonQuery();

            using var journalCmd = conn.CreateCommand();
            journalCmd.CommandText = "PRAGMA journal_mode;";
            var journalMode = journalCmd.ExecuteScalar()?.ToString();

            if (!string.Equals(journalMode, "WAL", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Failed to set database {Type} journal_mode PRAGMA to WAL; performance may be reduced", typeof(T).Name);
            }

            using var syncCmd = conn.CreateCommand();
            syncCmd.CommandText = "PRAGMA synchronous;";
            var sync = syncCmd.ExecuteScalar()?.ToString();

            if (!string.Equals(sync, "1", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Failed to set database {Type} synchronous PRAGMA to 1; performance may be reduced", typeof(T).Name);
            }

            Log.Debug("PRAGMAs for {Context}: journal_mode={JournalMode}, synchronous={Synchronous}", typeof(T).Name, journalMode, sync);

            return services;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to initialize database context {typeof(T).Name}: ${ex.Message}");
            throw;
        }
    }
}
