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
        public static readonly string ExecutablePath = TryGetExecutablePath();

        /// <remarks>
        ///     Inaccurate when running locally.
        /// </remarks>
        private static readonly Version AssemblyVersion = (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0)).Equals(new Version(1, 0, 0, 0))
            ? new Version(0, 0, 0, 0)
            : (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0));

        /// <remarks>
        ///     Inaccurate when running locally.
        /// </remarks>
        private static readonly string InformationalVersion = (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0") == "1.0.0"
            ? "0.0.0"
            : (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0");

        /// <summary>
        ///     Occurs when a new log event is emitted.
        /// </summary>
        public static event EventHandler<LogRecord> LogEmitted = (_, _) => { };

        /// <summary>
        ///     Gets the semantic application version.
        /// </summary>
        public static string SemanticVersion { get; } = InformationalVersion.Split('+').First();

        /// <summary>
        ///     Gets the full application version, including both assembly and informational versions.
        /// </summary>
        public static string FullVersion { get; } = $"{SemanticVersion} ({InformationalVersion})";

        /// <summary>
        ///     Gets a value indicating whether the current version is a Canary build.
        /// </summary>
        public static bool IsCanary { get; } = AssemblyVersion.Revision == 65534;

        /// <summary>
        ///     Gets a value indicating whether the current version is a Development build.
        /// </summary>
        public static bool IsDevelopment { get; } = new Version(0, 0, 0, 0) == AssemblyVersion;

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

        private static string TryGetExecutablePath()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

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
            // Use app directory in mutex name if set, otherwise use default
            var dir = AppDirectory ?? DefaultAppDirectory;
            return $"{AppName}_{Compute.Sha256Hash(dir)}";
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
            var aggregate = exception as AggregateException;
            var exceptions = aggregate != null
                ? aggregate.Flatten().InnerExceptions.ToArray()
                : new[] { exception };

            return exceptions.Length > 0 && exceptions.All(IsBenignUnobservedTaskInnerException);
        }

        private static bool IsBenignUnobservedTaskInnerException(Exception exception)
        {
            return false;
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

            // if a user has used one of the arguments above, perform the requested task, then quit
            if (ShowVersion)
            {
                Log.Information(FullVersion);
                return;
            }

            if (ShowHelp || ShowEnvironmentVariables)
            {
                if (!NoLogo)
                {
                    PrintLogo(FullVersion);
                }

                if (ShowHelp)
                {
                    PrintCommandLineArguments(typeof(Options));
                }

                if (ShowEnvironmentVariables)
                {
                    PrintEnvironmentVariables(typeof(Options), EnvironmentVariablePrefix);
                }

                return;
            }

            if (GenerateCertificate)
            {
                var (filename, password) = GenerateX509Certificate(password: Cryptography.Random.GetBytes(16).ToBase62(), filename: $"{AppName}.pfx");

                Log.Information($"Certificate exported to {filename}");
                Console.WriteLine($"Password: {password}");
                return;
            }

            if (GenerateSecret > 0)
            {
                if (GenerateSecret < 16 || GenerateSecret > 255)
                {
                    Log.Error("Invalid command line input: secret length must be between 16 and 255, inclusive");
                    return;
                }

                Log.Information(Cryptography.Random.GetBytes(GenerateSecret).ToBase62());
                return;
            }

            // derive the application directory value and defaults that are dependent upon it
            if (string.IsNullOrWhiteSpace(AppDirectory))
            {
                AppDirectory = DefaultAppDirectory;
            }

            // the application isn't being run in command mode. check the mutex to ensure
            // only one long-running instance per app directory.
            // Create mutex with name that includes app directory to allow multiple test instances
            Mutex = new Mutex(initiallyOwned: true, GetMutexName());
            if (!Mutex.WaitOne(millisecondsTimeout: 0, exitContext: false))
            {
                Log.Fatal($"An instance of {AppName} is already running in app directory: {AppDirectory}");
                return;
            }

            DataDirectory = Path.Combine(AppDirectory, "data");
            DataBackupDirectory = Path.Combine(DataDirectory, "backups");
            LogDirectory = Path.Combine(AppDirectory, "logs");
            ScriptDirectory = Path.Combine(AppDirectory, "scripts");

            DefaultConfigurationFile = Path.Combine(AppDirectory, $"{AppName}.yml");
            DefaultDownloadsDirectory = Path.Combine(AppDirectory, "downloads");
            DefaultIncompleteDirectory = Path.Combine(AppDirectory, "incomplete");

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
                VerifyDirectory(AppDirectory, createIfMissing: true, verifyWriteable: true);
                VerifyDirectory(DataDirectory, createIfMissing: true, verifyWriteable: true);
                VerifyDirectory(DataBackupDirectory, createIfMissing: true, verifyWriteable: true);
                VerifyDirectory(ScriptDirectory, createIfMissing: true, verifyWriteable: false);
                VerifyDirectory(DefaultDownloadsDirectory, createIfMissing: true, verifyWriteable: true);
                VerifyDirectory(DefaultIncompleteDirectory, createIfMissing: true, verifyWriteable: true);
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
            // initialize
            // avoids: System.Exception: You need to call SQLitePCL.raw.SetProvider().  If you are using a bundle package, this is done by calling SQLitePCL.Batteries.Init().
            SQLitePCL.Batteries.Init();

            // check the threading mode set at compile time. if it is 0 it is unsafe to use in a multithreaded application, which slskd is.
            // https://www.sqlite.org/compile.html#threadsafe
            var threadSafe = SQLitePCL.raw.sqlite3_threadsafe();

            if (threadSafe == 0)
            {
                throw new InvalidOperationException($"SQLite binary was not compiled with THREADSAFE={threadSafe}, which is not compatible with this application. Please create a GitHub issue to report this and include details about your environment.");
            }

            Log.Debug("SQLite was compiled with THREADSAFE={Mode}", threadSafe);

            if (SQLitePCL.raw.sqlite3_config(SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED) != SQLitePCL.raw.SQLITE_OK)
            {
                throw new InvalidOperationException($"SQLite threading mode could not be set to SERIALIZED ({SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED}). Please create a GitHub issue to report this and include details about your environment.");
            }

            Log.Debug("SQLite threading mode set to {Mode} ({Number})", "SERIALIZED", SQLitePCL.raw.SQLITE_CONFIG_SERIALIZED);
        }

        private static void ConfigureGlobalLogger()
        {
            Serilog.Log.Logger = (OptionsAtStartup.Debug ? new LoggerConfiguration().MinimumLevel.Debug() : new LoggerConfiguration().MinimumLevel.Information())
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Override("System.Net.Http.HttpClient", OptionsAtStartup.Debug ? LogEventLevel.Warning : LogEventLevel.Fatal)
                .MinimumLevel.Override("slskd.Authentication.PassthroughAuthenticationHandler", LogEventLevel.Warning)
                .MinimumLevel.Override("slskd.Authentication.ApiKeyAuthenticationHandler", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning) // bump this down to Information to show SQL
                .Enrich.WithProperty("InstanceName", OptionsAtStartup.InstanceName)
                .Enrich.WithProperty("InvocationId", InvocationId)
                .Enrich.WithProperty("ProcessId", ProcessId)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    theme: (OptionsAtStartup.Logger.NoColor || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) ? ConsoleTheme.None : SystemConsoleTheme.Literate,
                    outputTemplate: (OptionsAtStartup.Debug ? "[{SourceContext}] " : string.Empty) + "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Async(config =>
                    config.Conditional(
                        e => OptionsAtStartup.Logger.Disk,
                        config => config.File(
                            Path.Combine(LogDirectory, $"{AppName}-.log"),
                            outputTemplate: (OptionsAtStartup.Debug ? "[{SourceContext}] " : string.Empty) + "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                            rollingInterval: RollingInterval.Day,
                            retainedFileTimeLimit: TimeSpan.FromDays(OptionsAtStartup.Retention.Logs))))
                .WriteTo.Conditional(
                    e => !string.IsNullOrEmpty(OptionsAtStartup.Logger.Loki),
                    config => config.GrafanaLoki(
                        OptionsAtStartup.Logger.Loki ?? string.Empty,
                        textFormatter: new MessageTemplateTextFormatter("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", null)))
                .WriteTo.Sink(new DelegatingSink(logEvent =>
                {
                    string message = string.Empty;

                    try
                    {
                        message = logEvent.RenderMessage();

                        if (logEvent.Exception != null)
                        {
                            message = $"{message}: {logEvent.Exception}";
                        }

                        var record = new LogRecord()
                        {
                            Timestamp = logEvent.Timestamp.LocalDateTime,
                            Context = logEvent.Properties["SourceContext"].ToString().TrimStart('"').TrimEnd('"'),
                            SubContext = logEvent.Properties.ContainsKey("SubContext") ? logEvent.Properties["SubContext"].ToString().TrimStart('"').TrimEnd('"') : string.Empty,
                            Level = logEvent.Level.ToString(),
                            Message = message.TrimStart('"').TrimEnd('"'),
                        };

                        LogBuffer.Enqueue(record);
                        RaiseLogEmitted(record);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Misconfigured delegating logger: {Exception}.  Message: {Message}", ex.Message, message);
                    }
                }))
                .CreateLogger();

            if (OptionsAtStartup.Flags.LogUnobservedExceptions)
            {
                // log Exceptions raised on fired-and-forgotten tasks, which adds very little value but might help debug someday
                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    Serilog.Log.Logger.Error(e.Exception, "Unobserved exception: {Message}", e.Exception.Message);
                    e.SetObserved();
                };
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;

                if (e.IsTerminating)
                {
                    Serilog.Log.Logger.Fatal(exception, "Unhandled fatal exception: {Message}", e.IsTerminating);
                }
                else
                {
                    Serilog.Log.Logger.Error(exception, "Unhandled exception: {Message}", exception?.Message ?? "Unknown exception");
                }
            };
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
            => new(root, exclusionFilters);

        private static void PrintCommandLineArguments(Type targetType)
        {
            static string GetLongName(string longName, Type type)
                => type == typeof(bool) ? longName : $"{longName} <{type.ToColloquialString().ToLowerInvariant()}>";

            var lines = new List<(string Item, string Description)>();

            void Map(Type type)
            {
                try
                {
                    var defaults = Activator.CreateInstance(type);
                    var props = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    foreach (PropertyInfo property in props)
                    {
                        var attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ArgumentAttribute));
                        var descriptionAttribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DescriptionAttribute));
                        var isRequired = property.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));

                        if (attribute != default)
                        {
                            var shortName = attribute.ConstructorArguments[0].Value is char shortNameValue ? shortNameValue : default;
                            var longName = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                            var description = descriptionAttribute?.ConstructorArguments[0].Value;

                            var suffix = isRequired ? " (required)" : $" (default: {property.GetValue(defaults) ?? "<null>"})";
                            var item = $"{(shortName == default ? "  " : $"{shortName}|")}--{GetLongName(longName, property.PropertyType)}";
                            var desc = $"{description}{(property.PropertyType == typeof(bool) ? string.Empty : suffix)}";
                            lines.Add(new(item, desc));
                        }
                        else
                        {
                            Map(property.PropertyType);
                        }
                    }
                }
                catch
                {
                    return;
                }
            }

            Map(targetType);

            var longestItem = lines.Max(l => l.Item.Length);

            Log.Information("\nusage: slskd [arguments]\n");
            Log.Information("arguments:\n");

            foreach (var line in lines)
            {
                Log.Information($"  {line.Item.PadRight(longestItem)}   {line.Description}");
            }
        }

        private static void PrintEnvironmentVariables(Type targetType, string prefix)
        {
            static string GetName(string name, Type type) => $"{name} <{type.ToColloquialString().ToLowerInvariant()}>";

            var lines = new List<(string Item, string Description)>();

            void Map(Type type)
            {
                try
                {
                    var defaults = Activator.CreateInstance(type);
                    var props = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    foreach (PropertyInfo property in props)
                    {
                        var attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(EnvironmentVariableAttribute));
                        var descriptionAttribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(DescriptionAttribute));
                        var isRequired = property.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute));

                        if (attribute != default)
                        {
                            var name = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                            var description = descriptionAttribute?.ConstructorArguments[0].Value;

                            var suffix = isRequired ? " (required)" : $" (default: {property.GetValue(defaults) ?? "<null>"})";
                            var item = $"{prefix}{GetName(name, property.PropertyType)}";
                            var desc = $"{description}{(type == typeof(bool) ? string.Empty : suffix)}";
                            lines.Add(new(item, desc));
                        }
                        else
                        {
                            Map(property.PropertyType);
                        }
                    }
                }
                catch
                {
                    return;
                }
            }

            Map(targetType);

            var longestItem = lines.Max(l => l.Item.Length);

            Log.Information("\nenvironment variables (arguments and config file have precedence):\n");

            foreach (var line in lines)
            {
                Log.Information($"  {line.Item.PadRight(longestItem)}   {line.Description}");
            }
        }

        private static void PrintLogo(string version)
        {
            try
            {
                var padding = 56 - version.Length;
                var paddingLeft = padding / 2;
                var paddingRight = paddingLeft + (padding % 2);

                var centeredVersion = new string(' ', paddingLeft) + version + new string(' ', paddingRight);

                var logos = new[]
                {
                    $@"
                   ▄▄▄▄         ▄▄▄▄       ▄▄▄▄
           ▄▄▄▄▄▄▄ █  █ ▄▄▄▄▄▄▄ █  █▄▄▄ ▄▄▄█  █
           █__ --█ █  █ █__ --█ █    ◄█ █  -  █
           █▄▄▄▄▄█ █▄▄█ █▄▄▄▄▄█ █▄▄█▄▄█ █▄▄▄▄▄█",
                    @$"
                    ▄▄▄▄     ▄▄▄▄     ▄▄▄▄
              ▄▄▄▄▄▄█  █▄▄▄▄▄█  █▄▄▄▄▄█  █
              █__ --█  █__ --█    ◄█  -  █
              █▄▄▄▄▄█▄▄█▄▄▄▄▄█▄▄█▄▄█▄▄▄▄▄█",
                };

                var logo = logos[new System.Random().Next(0, logos.Length)];

                var banner = @$"
{logo}
╒════════════════════════════════════════════════════════╕
│           GNU AFFERO GENERAL PUBLIC LICENSE            │
│                   https://slskd.org                    │
│                                                        │
│{centeredVersion}│";

                if (IsDevelopment)
                {
                    banner += "\n│■■■■■■■■■■■■■■■■■■■■► DEVELOPMENT ◄■■■■■■■■■■■■■■■■■■■■■│";
                }

                if (IsCanary)
                {
                    banner += "\n│■■■■■■■■■■■■■■■■■■■■■■■► CANARY ◄■■■■■■■■■■■■■■■■■■■■■■■│";
                }

                banner += "\n└────────────────────────────────────────────────────────┘";

                Console.WriteLine(banner);
            }
            catch
            {
                // noop. console may not be available in all cases.
            }
        }

        private static void VerifyDirectory(string directory, bool createIfMissing = true, bool verifyWriteable = true)
        {
            StartupFileSystem.VerifyDirectory(directory, createIfMissing, verifyWriteable);
        }

        private static void InstallShutdownTelemetry()
        {
            // Install hard telemetry to catch silent exits and unhandled exceptions
            // This ensures we always know WHY the process terminated
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                var expectedShutdown = Application.IsShuttingDown;
                var msg = expectedShutdown
                    ? "ProcessExit event fired during expected shutdown"
                    : "[FATAL] ProcessExit event fired - process terminating";
                if (!expectedShutdown)
                {
                    Console.Error.WriteLine(msg);
                }

                try
                {
                    if (expectedShutdown)
                    {
                        Log?.Information(msg);
                    }
                    else
                    {
                        Log?.Fatal(msg);
                    }
                }
                catch
                {
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                var msg = $"[FATAL] Unhandled exception: {ex?.Message ?? e.ExceptionObject?.ToString() ?? "unknown"}";
                Console.Error.WriteLine(msg);
                Console.Error.WriteLine(ex?.StackTrace ?? "no stack trace");
                try
                {
                    Log?.Fatal(ex, msg);
                }
                catch
                {
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                if (IsBenignUnobservedTaskException(e.Exception))
                {
                    var msg = $"[WARN] Ignoring benign unobserved task exception: {e.Exception.Message}";
                    Console.Error.WriteLine(msg);
                    try
                    {
                        Log?.Warning(e.Exception, msg);
                    }
                    catch
                    {
                    }

                    e.SetObserved();
                    return;
                }

                var baseException = e.Exception.GetBaseException();

                if (IsExpectedSoulseekNetworkException(e.Exception))
                {
                    var warningMessage = $"Ignoring expected Soulseek peer/distributed network exception: {baseException.Message}";
                    try
                    {
                        Log?.Debug(baseException, warningMessage);
                    }
                    catch
                    {
                    }

                    e.SetObserved();
                    return;
                }

                var fatalMessage = $"[FATAL] Unobserved task exception: {e.Exception.Message}";
                Console.Error.WriteLine(fatalMessage);
                Console.Error.WriteLine(e.Exception.StackTrace);
                try
                {
                    Log?.Fatal(e.Exception, fatalMessage);
                }
                catch
                {
                }

                e.SetObserved(); // Prevent process termination
            };
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
