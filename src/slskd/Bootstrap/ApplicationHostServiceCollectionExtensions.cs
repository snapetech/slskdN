// <copyright file="ApplicationHostServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using slskd.Authentication;
using slskd.Core.Features;
using slskd.Player;
using slskd.Core.API;
using slskd.Events;
using slskd.Files;
using slskd.Messaging;
using slskd.Relay;
using slskd.Search;
using slskd.Shares;
using slskd.Signals;
using slskd.Transfers;
using slskd.Users;
using slskd.Validation;
using slskd.SoulseekRuntime;
using Soulseek;

public static class ApplicationHostServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdApplicationHost(
        this IServiceCollection services,
        IConfiguration configuration,
        string appName,
        OptionsAtStartup optionsAtStartup,
        int soulseekMinorVersion)
    {
        // add the instance of optionsAtStartup to DI as they were at startup. use when Options might change, but
        // the values at startup are to be used (generally anything marked RequiresRestart).
        services.AddSingleton(optionsAtStartup);

        // add IOptionsMonitor and IOptionsSnapshot to DI.
        // use when the current Options are to be used (generally anything not marked RequiresRestart)
        // the monitor should be used for services with Singleton lifetime, snapshots for everything else
        services.AddOptions<slskd.Options>()
            .Bind(configuration.GetSection(appName), o => { o.BindNonPublicProperties = true; })
            .Validate(options =>
            {
                if (!options.TryValidate(out var result))
                {
                    Log.Warning("Options (re)configuration rejected.");
                    Log.Warning(result.GetResultView());
                    return false;
                }

                return true;
            });

        services.AddSingleton<IFeatureGate, FeatureGate>();

        // add IManagedState, IStateMutator, IStateMonitor, and IStateSnapshot state to DI.
        // the mutator should be used any time application state needs to be mutated (as the name implies)
        // as with options, the monitor should be used for services with Singleton lifetime, snapshots for everything else
        // IManagedState should be used where state is being mutated and accessed in the same context
        services.AddManagedState<State>();

        // add configured-only external player integrations.
        services.AddSingleton<IExternalProcessStarter, ExternalProcessStarter>();
        services.AddSingleton<IExternalVisualizerLauncher, ExternalVisualizerLauncher>();

        // add IHttpClientFactory
        // use through 'using var http = HttpClientFactory.CreateClient()' wherever HTTP calls will be made
        // this is important to prevent memory leaks
        services.AddHttpClient();
        services.AddHttpClient(Common.Security.OutboundUriGuard.NoRedirectHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(Common.Security.OutboundUriGuard.CreateNoRedirectHandler);
        services.AddHttpClient(Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(Common.Security.OutboundUriGuard.CreateNoRedirectOnlyHandler);
        services.AddHttpClient("ExternalModeration")
            .ConfigurePrimaryHttpMessageHandler(Common.Security.OutboundUriGuard.CreateNoRedirectHandler);

        // PR-14: SSRF-safe key fetcher for ActivityPub HTTP Signature (timeout 3s, no redirects to prevent SSRF)
        services.AddHttpClient<SocialFederation.IHttpSignatureKeyFetcher, SocialFederation.HttpSignatureKeyFetcher>(c => c.Timeout = TimeSpan.FromSeconds(3))
            .ConfigurePrimaryHttpMessageHandler(Common.Security.OutboundUriGuard.CreateNoRedirectHandler);

        // add a partially configured instance of SoulseekClient. the Application instance will
        // complete configuration at startup.
        services.AddSingleton<ISoulseekClient, SoulseekClient>(_ =>
            new SoulseekClient(soulseekMinorVersion, options: SoulseekClientOptionsFactory.CreateInitial(optionsAtStartup)));

        // add the core application service to DI as well as a hosted service so that other services can
        // access instance methods
        services.AddSingleton<IApplication>(sp =>
        {
            Log.Debug("[DI] Factory function called to construct Application singleton...");
            Log.Debug("[DI] Resolving OptionsAtStartup...");
            var optionsAtStartup = sp.GetRequiredService<OptionsAtStartup>();
            Log.Debug("[DI] Resolving IOptionsMonitor<slskd.Options>...");
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<slskd.Options>>();
            Log.Debug("[DI] Resolving IManagedState<State>...");
            var state = sp.GetRequiredService<IManagedState<State>>();
            Log.Debug("[DI] Resolving ISoulseekClient...");
            var soulseekClient = sp.GetRequiredService<ISoulseekClient>();
            Log.Debug("[DI] Resolving FileService...");
            var fileService = sp.GetRequiredService<FileService>();
            Log.Debug("[DI] Resolving ConnectionWatchdog...");
            var connectionWatchdog = sp.GetRequiredService<ConnectionWatchdog>();
            Log.Debug("[DI] Resolving ITransferService...");
            var transferService = sp.GetRequiredService<ITransferService>();
            Log.Debug("[DI] Resolving IBrowseTracker...");
            var browseTracker = sp.GetRequiredService<IBrowseTracker>();
            Log.Debug("[DI] Resolving IRoomService...");
            var roomService = sp.GetRequiredService<IRoomService>();
            Log.Debug("[DI] Resolving IUserService...");
            var userService = sp.GetRequiredService<IUserService>();
            Log.Debug("[DI] Resolving IMessagingService...");
            var messagingService = sp.GetRequiredService<IMessagingService>();
            Log.Debug("[DI] Resolving IShareService...");
            var shareService = sp.GetRequiredService<IShareService>();
            Log.Debug("[DI] Resolving ISearchService...");
            var searchService = sp.GetRequiredService<ISearchService>();
            Log.Debug("[DI] Resolving INotificationService...");
            var notificationService = sp.GetRequiredService<Integrations.Notifications.INotificationService>();
            Log.Debug("[DI] Resolving IRelayService...");
            var relayService = sp.GetRequiredService<IRelayService>();
            Log.Debug("[DI] Resolving IHubContext<ApplicationHub>...");
            var applicationHub = sp.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<ApplicationHub>>();
            Log.Debug("[DI] Resolving IHubContext<LogsHub>...");
            var logHub = sp.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<LogsHub>>();
            Log.Debug("[DI] Resolving IHubContext<TransfersHub>...");
            var transfersHub = sp.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<Transfers.API.TransfersHub>>();
            Log.Debug("[DI] Resolving EventBus...");
            var eventBus = sp.GetRequiredService<Events.EventBus>();
            var eventService = sp.GetRequiredService<Events.EventService>();
            Log.Debug("[DI] Resolving ShareGrantAnnouncementService (best-effort)...");
            _ = sp.GetService<Sharing.ShareGrantAnnouncementService>();
            Log.Debug("[DI] All dependencies resolved, constructing Application...");
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var nowPlayingService = sp.GetRequiredService<NowPlaying.NowPlayingService>();
            var app = new Application(
                optionsAtStartup, optionsMonitor, state, soulseekClient, fileService,
                connectionWatchdog, transferService, browseTracker, roomService,
                userService, messagingService, shareService, searchService,
                notificationService, relayService, applicationHub, logHub, transfersHub,
                eventBus, eventService, sp, scopeFactory, nowPlayingService);
            Log.Debug("[DI] Application singleton constructed successfully");
            return app;
        });

        // Use a wrapper to avoid factory function blocking
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SLSKDN_E2E_SKIP_APP_HOSTED")))
        {
            services.AddHostedService(p =>
            {
                Log.Debug("[DI] Constructing ApplicationHostedServiceWrapper hosted service...");
                Log.Debug("[DI] About to resolve IApplication from DI...");
                var app = p.GetRequiredService<IApplication>();
                Log.Debug("[DI] IApplication resolved successfully");
                Log.Debug("[DI] About to create ApplicationHostedServiceWrapper instance...");
                var service = new ApplicationHostedServiceWrapper(app, p.GetService<Microsoft.Extensions.Logging.ILogger<ApplicationHostedServiceWrapper>>());
                Log.Debug("[DI] ApplicationHostedServiceWrapper constructed");
                return service;
            });
        }
        else
        {
            Log.Debug("[DI] SLSKDN_E2E_SKIP_APP_HOSTED=1; skipping ApplicationHostedServiceWrapper registration");
        }

        return services;
    }
}
