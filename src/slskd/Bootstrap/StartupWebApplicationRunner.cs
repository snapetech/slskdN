// <copyright file="StartupWebApplicationRunner.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using slskd.Common.Security;
using slskd.Configuration;

public sealed record StartupWebApplicationContext(
    string[] Args,
    string EnvironmentVariablePrefix,
    string ConfigurationFile,
    VolatileOverlayConfigurationSource<OptionsOverlay> VolatileOverlayConfigurationSource,
    IConfigurationRoot Configuration,
    string AppName,
    string DataDirectory,
    string XmlDocumentationFile,
    int SoulseekMinorVersion);

public static class StartupWebApplicationRunner
{
    public static void Run(
        StartupWebApplicationContext context,
        OptionsAtStartup optionsAtStartup,
        Serilog.ILogger log,
        Action<int> exit)
    {
        try
        {
            var bindExposure = BindExposureAnalyzer.AnalyzeWebBinding(optionsAtStartup);
            var isBindingNonLoopback = BindExposureAnalyzer.IsRemoteReachable(bindExposure);
            HardeningValidator.Validate(
                optionsAtStartup,
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
                isBindingNonLoopback);

            var builder = WebApplication.CreateBuilder(context.Args);

            builder.Configuration
                .AddSlskdConfigurationProviders(
                    context.EnvironmentVariablePrefix,
                    context.ConfigurationFile,
                    reloadOnChange: !optionsAtStartup.Flags.NoConfigWatch,
                    context.VolatileOverlayConfigurationSource,
                    log);

            // Deterministic port probe for E2E startup debugging.
            var portStr = builder.Configuration[$"{context.AppName}:Web:Port"] ?? "<null>";
            if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
            {
                Console.Error.WriteLine($"[ConfigProbe] slskd:web:port={portStr}");
            }

            builder.Host
                .UseSerilog();

            builder.ConfigureSlskdWebHost(optionsAtStartup, context.AppName);

            log.Debug("[MAIN] About to configure ASP.NET services...");
            builder.Services
                .AddSlskdWebServices(context.Configuration, optionsAtStartup, context.AppName, context.DataDirectory, context.EnvironmentVariablePrefix, context.XmlDocumentationFile)
                .AddSlskdRuntimeServices(context.Configuration, optionsAtStartup, context.DataDirectory, context.SoulseekMinorVersion)
                .AddSlskdHostDiagnostics();

            // Enable detailed logging for host lifetime and Kestrel in test/dev environments.
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Debug);

            log.Debug("[MAIN] Services configured, building DI container...");
            WebApplication app;
            try
            {
                log.Debug("Building DI container...");
                log.Debug("[DI] About to call builder.Build() - this will construct all singleton services...");
                app = builder.Build();
                log.Debug("DI container built successfully!");
            }
            catch (Exception diEx)
            {
                log.Fatal(diEx, "FAILED to build DI container");
                throw;
            }

            app.RunSlskdStartupTasks(optionsAtStartup);

            log.Debug("[DI] About to configure ASP.NET pipeline...");
            try
            {
                app.UseSlskdWebPipeline(optionsAtStartup);
                log.Debug("[DI] ASP.NET pipeline configured");
            }
            catch (Exception pipelineEx)
            {
                log.Error(pipelineEx, "[DI] EXCEPTION configuring ASP.NET pipeline: {Message}", pipelineEx.Message);
                throw;
            }

            if (optionsAtStartup.Flags.NoStart)
            {
                log.Information("Quitting because 'no-start' option is enabled");
                return;
            }

            app.RunSlskdApplication(optionsAtStartup);
        }
        catch (HardeningValidationException hex)
        {
            Console.Error.WriteLine($"[HardeningValidation] {hex.RuleName}: {hex.Message}");
            log.Fatal(hex, "Hardening validation failed: {Message}", hex.Message);
            exit(1);
        }
        catch (Exception ex)
        {
            HandleUnexpectedTermination(ex, log, exit);
        }
        finally
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    internal static void HandleUnexpectedTermination(
        Exception exception,
        Serilog.ILogger log,
        Action<int> exit)
    {
        log.Fatal(exception, "Application terminated unexpectedly");
        exit(1);
    }
}
