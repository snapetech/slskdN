// <copyright file="StartupDiagnostics.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using Serilog;
using slskd.Configuration;

public sealed record StartupDiagnosticsContext(
    string FullVersion,
    bool IsDevelopment,
    bool IsCanary,
    string IssuesUrl,
    int ProcessId,
    string ExecutablePath,
    string BaseDirectory,
    Guid InvocationId,
    string AppDirectory,
    string ConfigurationFile,
    string DataDirectory,
    string LogDirectory);

public static class StartupDiagnostics
{
    public static void LogStartupIdentity(
        OptionsAtStartup optionsAtStartup,
        StartupDiagnosticsContext context,
        ILogger log)
    {
        if (!optionsAtStartup.Flags.NoLogo)
        {
            StartupConsoleOutput.PrintLogo(context.FullVersion, context.IsDevelopment, context.IsCanary);
        }

        log.Information("Version: {Version}", context.FullVersion);

        if (context.IsDevelopment)
        {
            log.Warning("This is a Development build; YMMV");
        }

        if (context.IsCanary)
        {
            log.Warning("This is a canary build");
            log.Warning("Canary builds are considered UNSTABLE and may be completely BROKEN");
            log.Warning("Please report any issues here: {IssuesUrl}", context.IssuesUrl);
        }

        log.Information("System: .NET {DotNet}, {OS}, {BitNess} bit, {ProcessorCount} processors", Environment.Version, Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.ProcessorCount);
        log.Information("Process ID: {ProcessId} ({BitNess} bit)", context.ProcessId, Environment.Is64BitProcess ? 64 : 32);
        log.Information("Executable path: {ExecutablePath}", context.ExecutablePath);
        log.Information("Base directory: {BaseDirectory}", context.BaseDirectory);

        log.Information("Invocation ID: {InvocationId}", context.InvocationId);
        log.Information("Instance Name: {InstanceName}", optionsAtStartup.InstanceName);

        log.Information("Configuring application...");
    }

    public static void LogConfigurationUsage(
        OptionsAtStartup optionsAtStartup,
        StartupDiagnosticsContext context,
        ILogger log,
        Action<string> recreateConfigurationFileIfMissing)
    {
        log.Information("Using application directory {AppDirectory}", context.AppDirectory);
        log.Information("Using configuration file {ConfigurationFile}", context.ConfigurationFile);

        foreach (var warning in ConfigurationCompatibilityWarnings.GetWarnings(context.ConfigurationFile, optionsAtStartup))
        {
            log.Warning("{Warning}", warning);
        }

        if (optionsAtStartup.Flags.NoConfigWatch)
        {
            log.Warning("Configuration watch DISABLED; all configuration changes will require a restart to take effect");
        }

        log.Information("Storing application data in {DataDirectory}", context.DataDirectory);

        if (optionsAtStartup.Logger.Disk)
        {
            log.Information("Saving application logs to {LogDirectory}", context.LogDirectory);
        }

        recreateConfigurationFileIfMissing(context.ConfigurationFile);

        if (!string.IsNullOrEmpty(optionsAtStartup.Logger.Loki))
        {
            log.Information("Forwarding logs to Grafana Loki instance at {LoggerLokiUrl}", optionsAtStartup.Logger.Loki);
        }
    }
}
