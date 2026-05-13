// <copyright file="Program.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

// <copyright file="Program.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.AudioCore;
using slskd.Bootstrap;
using slskd.Mesh.Gossip;
using slskd.Mesh.Governance;
using slskd.Mesh.Realm;
using slskd.Mesh.Realm.Bridge;
using slskd.SocialFederation;
using slskd.VirtualSoulfind.Core;

namespace slskd
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.RateLimiting;
    using System.Threading.Tasks;
    using Asp.Versioning.ApiExplorer;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi;
    using OpenTelemetry.Trace;
    using Prometheus.DotNetRuntime;
    using Prometheus.SystemMetrics;
    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Display;
    using Serilog.Sinks.Grafana.Loki;
    using Serilog.Sinks.SystemConsole.Themes;
    using slskd.Authentication;
    using slskd.Common.Security;
    using slskd.Configuration;
    using slskd.Core.API;
    using slskd.Core.Features;
    using slskd.Cryptography;
    using slskd.DhtRendezvous;
    using slskd.DhtRendezvous.Security;
    using slskd.Events;
    using slskd.Files;
    using slskd.Identity;
    using slskd.Integrations.AcoustId;
    using slskd.Integrations.AutoTagging;
    using slskd.Integrations.Chromaprint;
    using slskd.Integrations.FTP;
    using slskd.Integrations.MetadataFacade;
    using slskd.Integrations.Lidarr;
    using slskd.Integrations.MusicBrainz;
    using slskd.Integrations.Pushbullet;
    using slskd.LibraryHealth;
    using slskd.ListeningParty;
    using slskd.Mesh;
    using slskd.Messaging;
    using slskd.Player;
    using slskd.Relay;
    using slskd.Search;
    using slskd.Search.API;
    using slskd.Shares;
    using slskd.Sharing;
    using slskd.Signals;
    using slskd.SongID;
    using slskd.SoulseekDiscovery;
    using slskd.Streaming;
    using slskd.Telemetry;
    using slskd.Transfers;
    using slskd.Transfers.Downloads;
    using slskd.Transfers.MultiSource;
    using slskd.Transfers.MultiSource.Discovery;
    using slskd.Transfers.Rescue;
    using slskd.Transfers.Uploads;
    using slskd.Users;
    using slskd.Validation;
    using Soulseek;
    using Utility.CommandLine;
    using Utility.EnvironmentVariables;
    using IOFile = System.IO.File;

    /// <summary>
    ///     Bootstraps configuration and handles primitive command-line instructions.
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     The name of the application.
        /// </summary>
        public static readonly string AppName = "slskd";

        /// <summary>
        ///     The DateTime of the 'genesis' of the application (the initial commit).
        /// </summary>
        public static readonly DateTime GenesisDateTime = new(2020, 12, 30, 6, 22, 0, DateTimeKind.Utc);

        /// <summary>
        ///     The name of the local share host.
        /// </summary>
        public static readonly string LocalHostName = "local";

        /// <summary>
        ///     The url to the issues/support site.
        /// </summary>
        public static readonly string IssuesUrl = "https://github.com/snapetech/slskdn/issues";

        /// <summary>
        ///     The global prefix for environment variables.
        /// </summary>
        public static readonly string EnvironmentVariablePrefix = $"{AppName.ToUpperInvariant()}_";

        /// <summary>
        ///     The default XML documentation filename.
        /// </summary>
        public static readonly string XmlDocumentationFile = Path.Combine(AppContext.BaseDirectory, "etc", $"{AppName}.xml");

        /// <summary>
        ///     Soulseek.NET requires a caller-owned minor-version slot.
        ///     slskdN reserved range: 7700000-7709999 (registry PR pending).
        ///     Reserved range 760-7699999 belongs to upstream slskd.
        /// </summary>
        public static readonly int SoulseekMinorVersion = 7700000;

        /// <summary>
        ///     The default application data directory.
        /// </summary>
        public static readonly string DefaultAppDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), AppName);

        /// <summary>
        ///     Gets the unique Id of this application invocation.
        /// </summary>
        public static readonly Guid InvocationId = Guid.NewGuid();

        /// <summary>
        ///     Gets the Id of the current application process.
        /// </summary>
        public static readonly int ProcessId = Environment.ProcessId;

        /// <summary>
        ///     Gets the application's base directory.
        /// </summary>
        public static readonly string BaseDirectory = AppContext.BaseDirectory;

        /// <summary>
        ///     Gets the current executable path when available.
        /// </summary>
        public static readonly string ExecutablePath = ApplicationRuntimeInfo.TryGetExecutablePath();

        /// <remarks>
        ///     Inaccurate when running locally.
        /// </remarks>
        private static readonly Version AssemblyVersion = ApplicationRuntimeInfo.AssemblyVersion;

        /// <remarks>
        ///     Inaccurate when running locally.
        /// </remarks>
        private static readonly string InformationalVersion = ApplicationRuntimeInfo.InformationalVersion;

        /// <summary>
        ///     Occurs when a new log event is emitted.
        /// </summary>
        public static event EventHandler<LogRecord> LogEmitted = (_, _) => { };

        /// <summary>
        ///     Gets the semantic application version.
        /// </summary>
        public static string SemanticVersion { get; } = ApplicationRuntimeInfo.SemanticVersion;

        /// <summary>
        ///     Gets the full application version, including both assembly and informational versions.
        /// </summary>
        public static string FullVersion { get; } = ApplicationRuntimeInfo.FullVersion;

        /// <summary>
        ///     Gets a value indicating whether the current version is a Canary build.
        /// </summary>
        public static bool IsCanary { get; } = ApplicationRuntimeInfo.IsCanary;

        /// <summary>
        ///     Gets a value indicating whether the current version is a Development build.
        /// </summary>
        public static bool IsDevelopment { get; } = ApplicationRuntimeInfo.IsDevelopment;

        private static void RaiseLogEmitted(LogRecord record)
        {
            foreach (EventHandler<LogRecord> handler in LogEmitted.GetInvocationList())
            {
                try
                {
                    handler.Invoke(null, record);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "LogEmitted subscriber failed");
                }
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the application is being run in Relay Agent mode.
        /// </summary>
        public static bool IsRelayAgent { get; private set; }

        /// <summary>
        ///     Gets the application flags.
        /// </summary>
        public static Options.FlagsOptions Flags { get; private set; } = new();

        /// <summary>
        ///     Gets the path where application data is saved.
        /// </summary>
        [Argument('a', "app-dir", "path where application data is saved")]
        [EnvironmentVariable("APP_DIR")]
        public static string AppDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the fully qualified path to the application configuration file.
        /// </summary>
        [Argument('c', "config", "path to configuration file")]
        [EnvironmentVariable("CONFIG")]
        public static string ConfigurationFile { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the current configuration overlay, if one has been applied.
        /// </summary>
        public static OptionsOverlay? ConfigurationOverlay => VolatileOverlayConfigurationSource?.CurrentValue;

        /// <summary>
        ///     Gets the path where persistent data is saved.
        /// </summary>
        public static string DataDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the path where backups of persistent data saved.
        /// </summary>
        public static string DataBackupDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the default fully qualified path to the configuration file.
        /// </summary>
        public static string DefaultConfigurationFile { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the default downloads directory.
        /// </summary>
        public static string DefaultDownloadsDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the default incomplete download directory.
        /// </summary>
        public static string DefaultIncompleteDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the path where application logs are saved.
        /// </summary>
        public static string LogDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets the path where user-defined scripts are stored.
        /// </summary>
        public static string ScriptDirectory { get; private set; } = string.Empty;

        /// <summary>
        ///     Gets a buffer containing the last few log events.
        /// </summary>
        public static ConcurrentFixedSizeQueue<LogRecord> LogBuffer { get; } = new ConcurrentFixedSizeQueue<LogRecord>(size: 100);

        /// <summary>
        ///     Gets the master cancellation token source for the program.
        /// </summary>
        /// <remarks>
        ///     The token from this source should be used (or linked) to any long-running asynchronous task, so that when the application
        ///     begins to shut down these tasks also shut down in a timely manner. Actions that control the lifecycle of the program
        ///     (POSIX signals, a restart from the API, etc) should cancel this source.
        /// </remarks>
        public static CancellationTokenSource MasterCancellationTokenSource { get; } = new CancellationTokenSource();

        private static IConfigurationRoot? Configuration { get; set; }
        private static OptionsAtStartup OptionsAtStartup { get; } = new OptionsAtStartup();

        // Explicit Serilog.ILogger type to avoid ambiguity with Microsoft.Extensions.Logging.ILogger
        private static Serilog.ILogger Log { get; set; } = new Serilog.LoggerConfiguration()
            .WriteTo.Sink(new ConsoleWriteLineLogger())
            .CreateLogger();

        // Mutex is created lazily after AppDirectory is set to allow multiple test instances with different app dirs
        private static Mutex? Mutex { get; set; }

        private static string GetMutexName()
        {
            return StartupSingleInstance.GetMutexName(AppName, AppDirectory, DefaultAppDirectory);
        }

        internal static string GetWriteBaseDirectory()
        {
            return AppPathResolver.GetWriteBaseDirectory(AppDirectory, DefaultAppDirectory);
        }

        internal static string ResolveOptionalAppRelativePath(string? path)
        {
            return AppPathResolver.ResolveOptionalAppRelativePath(path, AppDirectory, DefaultAppDirectory);
        }

        internal static string ResolveAppRelativePath(string path, string fallbackRelativePath)
        {
            return AppPathResolver.ResolveAppRelativePath(path, fallbackRelativePath, AppDirectory, DefaultAppDirectory);
        }

        internal static IReadOnlyList<(string Pattern, string Replacement)> CreateWebHtmlRewriteRules(string urlBase)
        {
            return WebHtmlRewriteRules.Create(urlBase);
        }

        internal static SoulseekClientOptions CreateInitialSoulseekClientOptions(OptionsAtStartup optionsAtStartup)
        {
            return SoulseekRuntime.SoulseekClientOptionsFactory.CreateInitial(optionsAtStartup);
        }

        internal static bool IsBenignUnobservedTaskException(Exception exception)
        {
            return StartupExceptionClassifier.IsBenignUnobservedTaskException(exception);
        }

        private static IDisposable? DotNetRuntimeStats { get; set; }
        private static VolatileOverlayConfigurationSource<OptionsOverlay> VolatileOverlayConfigurationSource { get; set; } = new VolatileOverlayConfigurationSource<OptionsOverlay>();

        [Argument('g', "generate-cert", "generate X509 certificate and password for HTTPs")]
        private static bool GenerateCertificate { get; set; }

        [Argument('k', "generate-secret", "generate random secret of the specified length")]
        private static int GenerateSecret { get; set; }

        [Argument('n', "no-logo", "suppress logo on startup")]
        private static bool NoLogo { get; set; }

        [Argument('e', "envars", "display environment variables")]
        private static bool ShowEnvironmentVariables { get; set; }

        [Argument('h', "help", "display command line usage")]
        private static bool ShowHelp { get; set; }

        [Argument('v', "version", "display version information")]
        private static bool ShowVersion { get; set; }

        /// <summary>
        ///     Panic.
        /// </summary>
        /// <param name="code">An optional exit code.</param>
        public static void Exit(int code = 1) => Environment.Exit(code);

        /// <summary>
        ///     Apply an instance of <see cref="OptionsOverlay"/> on top of the existing application configuration.
        /// </summary>
        /// <param name="overlay">The overlay containing the property values to be overlaid.</param>
        public static void ApplyConfigurationOverlay(OptionsOverlay overlay) => VolatileOverlayConfigurationSource.Apply(overlay);

        /// <summary>
        ///     Entrypoint.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            // populate the properties above so that we can override the default config file if needed, and to
            // check if the application is being run in command mode (run task and quit).
            EnvironmentVariables.Populate(prefix: EnvironmentVariablePrefix);

            try
            {
                Arguments.Populate(clearExistingValues: false);
            }
            catch (Exception ex)
            {
                // this is pretty hacky, but i don't have a good way of trapping errors that bubble up here.
                Log.Error($"Invalid command line input: {ex.Message.Replace(".  See inner exception for details.", string.Empty)}");
                return;
            }

            if (StartupCommandMode.TryRun(
                new StartupCommandModeOptions(ShowVersion, ShowHelp, ShowEnvironmentVariables, NoLogo, GenerateCertificate, GenerateSecret),
                FullVersion,
                EnvironmentVariablePrefix,
                AppName,
                typeof(Options),
                Log,
                PrintLogo,
                PrintCommandLineArguments,
                PrintEnvironmentVariables,
                GenerateX509Certificate))
            {
                return;
            }

            var directories = StartupApplicationDirectoryResolver.Resolve(AppDirectory, DefaultAppDirectory, AppName);
            AppDirectory = directories.AppDirectory;

            // the application isn't being run in command mode. check the mutex to ensure
            // only one long-running instance per app directory.
            // Create mutex with name that includes app directory to allow multiple test instances
            Mutex = new Mutex(initiallyOwned: true, GetMutexName());
            if (!Mutex.WaitOne(millisecondsTimeout: 0, exitContext: false))
            {
                Log.Fatal($"An instance of {AppName} is already running in app directory: {AppDirectory}");
                return;
            }

            DataDirectory = directories.DataDirectory;
            DataBackupDirectory = directories.DataBackupDirectory;
            LogDirectory = directories.LogDirectory;
            ScriptDirectory = directories.ScriptDirectory;
            DefaultConfigurationFile = directories.DefaultConfigurationFile;
            DefaultDownloadsDirectory = directories.DefaultDownloadsDirectory;
            DefaultIncompleteDirectory = directories.DefaultIncompleteDirectory;

            // the location of the configuration file might have been overridden by command line or envar.
            // if not, set it to the default.
            if (string.IsNullOrWhiteSpace(ConfigurationFile))
            {
                ConfigurationFile = DefaultConfigurationFile;
            }

            // verify(create if needed) default application directories. if the downloads or complete
            // directories are overridden in config, those will be validated after the config is loaded.
            try
            {
                StartupApplicationDirectoryResolver.VerifyDefaults(directories);
            }
            catch (Exception ex)
            {
                Log.Information($"Filesystem exception: {ex.Message}");
                Exit(1);
            }

            // load and validate the configuration
            try
            {
                Configuration = new ConfigurationBuilder()
                    .AddSlskdConfigurationProviders(EnvironmentVariablePrefix, ConfigurationFile, reloadOnChange: !OptionsAtStartup.Flags.NoConfigWatch, VolatileOverlayConfigurationSource, Log)
                    .Build();

                Configuration.GetSection(AppName)
                    .Bind(OptionsAtStartup, (o) => { o.BindNonPublicProperties = true; });

                Log.Debug("[Config] After binding OptionsAtStartup.Security.Enabled = {Enabled}, Profile = {Profile}",
                    OptionsAtStartup.Security?.Enabled ?? false,
                    OptionsAtStartup.Security?.Profile.ToString() ?? "null");

                var securitySection = Configuration.GetSection("security");
                var slskdSecuritySection = Configuration.GetSection("slskd:security");
                Log.Debug("[Config] Raw config sections - security.Exists={SecurityExists}, slskd:security.Exists={SlskdSecurityExists}",
                    securitySection.Exists(),
                    slskdSecuritySection.Exists());
                if (securitySection.Exists())
                {
                    Log.Debug("[Config] Raw security section enabled value: {Enabled}", securitySection["enabled"]);
                }

                if (slskdSecuritySection.Exists())
                {
                    Log.Debug("[Config] Raw slskd:security section enabled value: {Enabled}", slskdSecuritySection["enabled"]);
                }

                if (!OptionsAtStartup.TryValidate(out var result))
                {
                    Log.Information(result.GetResultView());
                    Exit(1);
                }
            }
            catch (Exception ex)
            {
                Log.Information($"Invalid configuration: {(!OptionsAtStartup.Debug ? ex : ex.Message)}");
                Exit(1);
            }

            IsRelayAgent = OptionsAtStartup.Relay.Enabled && OptionsAtStartup.Relay.Mode.ToEnum<RelayMode>() == RelayMode.Agent;
            Flags = OptionsAtStartup.Flags;

            ConfigureGlobalLogger();
            Log = Serilog.Log.ForContext(typeof(Program));

            // Install hard telemetry to catch silent exits
            InstallShutdownTelemetry();

            if (!OptionsAtStartup.Flags.NoLogo)
            {
                PrintLogo(FullVersion);
            }

            Log.Information("Version: {Version}", FullVersion);

            if (IsDevelopment)
            {
                Log.Warning("This is a Development build; YMMV");
            }

            if (IsCanary)
            {
                Log.Warning("This is a canary build");
                Log.Warning("Canary builds are considered UNSTABLE and may be completely BROKEN");
                Log.Warning($"Please report any issues here: {IssuesUrl}");
            }

            Log.Information("System: .NET {DotNet}, {OS}, {BitNess} bit, {ProcessorCount} processors", Environment.Version, Environment.OSVersion, Environment.Is64BitOperatingSystem ? 64 : 32, Environment.ProcessorCount);
            Log.Information("Process ID: {ProcessId} ({BitNess} bit)", ProcessId, Environment.Is64BitProcess ? 64 : 32);
            Log.Information("Executable path: {ExecutablePath}", ExecutablePath);
            Log.Information("Base directory: {BaseDirectory}", BaseDirectory);

            Log.Information("Invocation ID: {InvocationId}", InvocationId);
            Log.Information("Instance Name: {InstanceName}", OptionsAtStartup.InstanceName);

            Log.Information("Configuring application...");

            // SQLite must have specific capabilities to function properly. this shouldn't be a concern for shrinkwrapped
            // binaries or in Docker, but if someone builds from source weird things can happen.
            InitSQLiteOrFailFast();

            Log.Information("Using application directory {AppDirectory}", AppDirectory);
            Log.Information("Using configuration file {ConfigurationFile}", ConfigurationFile);

            foreach (var warning in ConfigurationCompatibilityWarnings.GetWarnings(ConfigurationFile, OptionsAtStartup))
            {
                Log.Warning("{Warning}", warning);
            }

            if (OptionsAtStartup.Flags.NoConfigWatch)
            {
                Log.Warning("Configuration watch DISABLED; all configuration changes will require a restart to take effect");
            }

            Log.Information("Storing application data in {DataDirectory}", DataDirectory);

            if (OptionsAtStartup.Logger.Disk)
            {
                Log.Information("Saving application logs to {LogDirectory}", LogDirectory);
            }

            RecreateConfigurationFileIfMissing(ConfigurationFile);

            if (!string.IsNullOrEmpty(OptionsAtStartup.Logger.Loki))
            {
                Log.Information("Forwarding logs to Grafana Loki instance at {LoggerLokiUrl}", OptionsAtStartup.Logger.Loki);
            }

            // bootstrap the ASP.NET application
            try
            {
                var bindExposure = BindExposureAnalyzer.AnalyzeWebBinding(OptionsAtStartup);
                var isBindingNonLoopback = BindExposureAnalyzer.IsRemoteReachable(bindExposure);
                Common.Security.HardeningValidator.Validate(
                    OptionsAtStartup,
                    System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
                    isBindingNonLoopback);

                var builder = WebApplication.CreateBuilder(args);

                builder.Configuration
                    .AddSlskdConfigurationProviders(EnvironmentVariablePrefix, ConfigurationFile, reloadOnChange: !OptionsAtStartup.Flags.NoConfigWatch, VolatileOverlayConfigurationSource, Log);

                // Deterministic port probe for E2E startup debugging.
                var portStr = builder.Configuration[$"{AppName}:Web:Port"] ?? "<null>";
                if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
                {
                    System.Console.Error.WriteLine($"[ConfigProbe] slskd:web:port={portStr}");
                }

                builder.Host
                    .UseSerilog();

                builder.ConfigureSlskdWebHost(OptionsAtStartup, AppName);

                Log.Debug("[MAIN] About to configure ASP.NET services...");
                builder.Services
                    .AddSlskdWebServices(Configuration!, OptionsAtStartup, AppName, DataDirectory, EnvironmentVariablePrefix, XmlDocumentationFile)
                    .AddSlskdRuntimeServices(Configuration!, OptionsAtStartup, DataDirectory, SoulseekMinorVersion)
                    .AddSlskdHostDiagnostics();

                // Enable detailed logging for host lifetime and Kestrel in test/dev environments
                builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", Microsoft.Extensions.Logging.LogLevel.Information);
                builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", Microsoft.Extensions.Logging.LogLevel.Debug);

                Log.Debug("[MAIN] Services configured, building DI container...");
                WebApplication app;
                try
                {
                    Log.Debug("Building DI container...");
                    Log.Debug("[DI] About to call builder.Build() - this will construct all singleton services...");
                    app = builder.Build();
                    Log.Debug("DI container built successfully!");
                }
                catch (Exception diEx)
                {
                    Log.Fatal(diEx, "FAILED to build DI container");
                    throw;
                }

                app.RunSlskdStartupTasks(OptionsAtStartup);

                Log.Debug("[DI] About to configure ASP.NET pipeline...");
                try
                {
                    app.UseSlskdWebPipeline(OptionsAtStartup);
                    Log.Debug("[DI] ASP.NET pipeline configured");
                }
                catch (Exception pipelineEx)
                {
                    Log.Error(pipelineEx, "[DI] EXCEPTION configuring ASP.NET pipeline: {Message}", pipelineEx.Message);
                    throw;
                }

                if (OptionsAtStartup.Flags.NoStart)
                {
                    Log.Information("Quitting because 'no-start' option is enabled");
                    return;
                }

                app.RunSlskdApplication(OptionsAtStartup);
            }
            catch (Common.Security.HardeningValidationException hex)
            {
                Console.Error.WriteLine($"[HardeningValidation] {hex.RuleName}: {hex.Message}");
                Log.Fatal(hex, "Hardening validation failed: {Message}", hex.Message);
                Exit(1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        private static void InitSQLiteOrFailFast()
        {
            StartupSqlite.InitOrFailFast(Log);
        }

        private static void ConfigureGlobalLogger()
        {
            Log = StartupLogging.Configure(
                OptionsAtStartup,
                AppName,
                LogDirectory,
                InvocationId,
                ProcessId,
                record =>
                {
                    LogBuffer.Enqueue(record);
                    RaiseLogEmitted(record);
                });
        }

        private static void RecreateConfigurationFileIfMissing(string configurationFile)
        {
            StartupFileSystem.RecreateConfigurationFileIfMissing(configurationFile, AppName, AppContext.BaseDirectory, Log);
        }

        private static (string Filename, string Password) GenerateX509Certificate(string password, string filename)
        {
            return StartupFileSystem.GenerateX509Certificate(AppName, AppContext.BaseDirectory, password, filename, Log);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The assigned framework options/configuration source owns the file provider lifecycle.")]
        internal static PhysicalFileProvider CreateOwnedPhysicalFileProvider(string root, ExclusionFilters exclusionFilters = ExclusionFilters.Sensitive)
            => StartupFileSystem.CreateOwnedPhysicalFileProvider(root, exclusionFilters);

        private static void PrintCommandLineArguments(Type targetType)
        {
            StartupConsoleOutput.PrintCommandLineArguments(targetType, Log);
        }

        private static void PrintEnvironmentVariables(Type targetType, string prefix)
        {
            StartupConsoleOutput.PrintEnvironmentVariables(targetType, prefix, Log);
        }

        private static void PrintLogo(string version)
        {
            StartupConsoleOutput.PrintLogo(version, IsDevelopment, IsCanary);
        }

        private static void VerifyDirectory(string directory, bool createIfMissing = true, bool verifyWriteable = true)
        {
            StartupFileSystem.VerifyDirectory(directory, createIfMissing, verifyWriteable);
        }

        private static void InstallShutdownTelemetry()
        {
            StartupShutdownTelemetry.Install(
                () => Application.IsShuttingDown,
                IsBenignUnobservedTaskException,
                IsExpectedSoulseekNetworkException,
                () => Log);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        [System.Runtime.Versioning.SupportedOSPlatform("macos")]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        internal static Mesh.Overlay.QuicOverlayClient CreateQuicOverlayClient(IServiceProvider serviceProvider)
        {
            return Mesh.Overlay.QuicOverlayFactory.CreateOverlayClient(serviceProvider);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        [System.Runtime.Versioning.SupportedOSPlatform("macos")]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        internal static Mesh.Overlay.QuicDataClient CreateQuicDataClient(IServiceProvider serviceProvider)
        {
            return Mesh.Overlay.QuicOverlayFactory.CreateDataClient(serviceProvider);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        [System.Runtime.Versioning.SupportedOSPlatform("macos")]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        internal static Mesh.Overlay.QuicOverlayServer CreateQuicOverlayServer(IServiceProvider serviceProvider)
        {
            return Mesh.Overlay.QuicOverlayFactory.CreateOverlayServer(serviceProvider);
        }

        internal static bool ShouldRunStandaloneUdpOverlayServer(bool overlayEnabled, bool sharedMeshUdpRequested)
        {
            return Mesh.Overlay.QuicOverlayFactory.ShouldRunStandaloneUdpOverlayServer(overlayEnabled, sharedMeshUdpRequested);
        }

        internal static bool IsExpectedSoulseekNetworkException(Exception exception)
        {
            return SoulseekExceptions.SoulseekNetworkExceptionClassifier.IsExpected(exception);
        }

        internal static Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet? TryGetAndStoreAntiforgeryTokens(
            HttpContext context,
            Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
        {
            return Core.Security.AntiforgeryCookieRecovery.TryGetAndStoreTokens(
                context,
                antiforgery,
                OptionsAtStartup.Web.Port,
                path => Log.Warning("[CSRF Middleware] Cleared stale antiforgery cookies for {Path} after key-ring mismatch", path));
        }

        internal static bool IsStaleAntiforgeryTokenException(Exception exception)
        {
            return Core.Security.AntiforgeryCookieRecovery.IsStaleTokenException(exception);
        }

        internal static bool StripKnownAntiforgeryCookiesFromRequest(HttpContext context)
        {
            return Core.Security.AntiforgeryCookieRecovery.StripKnownCookiesFromRequest(context, OptionsAtStartup.Web.Port);
        }

        internal static void ClearKnownAntiforgeryCookies(HttpContext context)
        {
            Core.Security.AntiforgeryCookieRecovery.ClearKnownCookies(context, OptionsAtStartup.Web.Port);
        }

    }
}
