// <copyright file="ApplicationRunExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using slskd.Common.CodeQuality;

public static class ApplicationRunExtensions
{
    public static void RunSlskdApplication(this WebApplication app, OptionsAtStartup optionsAtStartup)
    {
        Log.Information("Configuration complete.  Starting application...");

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = app.Urls;
            Log.Information("✓ Host started and bound to: {Addresses}", string.Join(", ", addresses));
            WriteServerProbe(app);
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            Log.Information("Application is stopping...");
        });

        WriteHostedServiceProbe(app);

        Log.Debug("[Program] About to call app.Run()...");
        Log.Debug("[Program] app.Run() will start the web server and all hosted services...");

        var hostLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        hostLifetime.ApplicationStarted.Register(() =>
        {
            Log.Debug("[Program] ApplicationStarted event fired - all hosted services have completed StartAsync");
            StartLanDiscoveryIfEnabled(app, optionsAtStartup);
        });

        hostLifetime.ApplicationStopping.Register(() =>
        {
            StopLanDiscovery(app);
        });

        Log.Debug("[Program] Calling app.Run() - this will block until shutdown...");
        Log.Debug("[Program] If you see this but not 'Host started and bound', the web server is hanging");

        if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
        {
            Console.Error.WriteLine($"[KestrelProbe] URLs={string.Join(";", app.Urls)}");
        }

        app.Run();
        Log.Debug("[Program] app.Run() returned after host shutdown");
    }

    private static void WriteServerProbe(WebApplication app)
    {
        if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") != "1")
        {
            return;
        }

        try
        {
            var server = app.Services.GetService<IServer>();
            Console.Error.WriteLine($"[ServerProbe] IServer={server?.GetType().FullName ?? "<null>"}");

            var serverFeatures = server?.Features.Get<IServerAddressesFeature>();
            if (serverFeatures == null)
            {
                Console.Error.WriteLine("[ServerProbe] IServerAddressesFeature=<null>");
            }
            else
            {
                Console.Error.WriteLine($"[ServerProbe] PreferHostingUrls={serverFeatures.PreferHostingUrls}");
                Console.Error.WriteLine($"[ServerProbe] Addresses={string.Join(" | ", serverFeatures.Addresses)}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ServerProbe] EX={ex}");
        }
    }

    private static void WriteHostedServiceProbe(WebApplication app)
    {
        if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") != "1")
        {
            return;
        }

        var hostedServices = app.Services.GetServices<IHostedService>()
            .Select(s => s.GetType().FullName)
            .OrderBy(s => s)
            .ToArray();
        Console.Error.WriteLine($"[HostedList] count={hostedServices.Length}");
        foreach (var hosted in hostedServices)
        {
            Console.Error.WriteLine($"[HostedList] {hosted}");
        }
    }

    private static void StartLanDiscoveryIfEnabled(WebApplication app, OptionsAtStartup optionsAtStartup)
    {
        if (!optionsAtStartup.Feature.IdentityFriends)
        {
            return;
        }

        try
        {
            var discovery = app.Services.GetService<Identity.ILanDiscoveryService>();
            if (discovery != null)
            {
                _ = TaskObservation.Observe(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await discovery.StartAdvertisingAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "[Program] Failed to start LAN discovery advertising");
                        }
                    },
                    CancellationToken.None),
                    ex => Log.Warning(ex, "[Program] Unobserved LAN discovery startup failure"));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Program] Failed to initialize LAN discovery");
        }
    }

    private static void StopLanDiscovery(WebApplication app)
    {
        try
        {
            var discovery = app.Services.GetService<Identity.ILanDiscoveryService>();
            if (discovery is not null)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                discovery.StopAdvertisingAsync().WaitAsync(timeout.Token).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Program] Failed to stop LAN discovery advertising on host stopping");
        }
    }
}
