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
            return string.IsNullOrWhiteSpace(AppDirectory) ? DefaultAppDirectory : AppDirectory;
        }

        internal static string ResolveOptionalAppRelativePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(GetWriteBaseDirectory(), path);
        }

        internal static string ResolveAppRelativePath(string path, string fallbackRelativePath)
        {
            var candidate = string.IsNullOrWhiteSpace(path) ? fallbackRelativePath : path;
            return ResolveOptionalAppRelativePath(candidate);
        }

        internal static IReadOnlyList<(string Pattern, string Replacement)> CreateWebHtmlRewriteRules(string urlBase)
        {
            var normalizedUrlBase = string.IsNullOrWhiteSpace(urlBase) || urlBase == "/"
                ? string.Empty
                : (urlBase.StartsWith("/") ? urlBase : "/" + urlBase).TrimEnd('/');

            string Prefix(string path) => string.IsNullOrEmpty(normalizedUrlBase) ? path : $"{normalizedUrlBase}{path}";
            string BaseTag() => string.IsNullOrEmpty(normalizedUrlBase)
                ? "<head>"
                : $"<head><base href=\"{normalizedUrlBase}/\" />";

            return new List<(string Pattern, string Replacement)>
            {
                ("<head>", BaseTag()),
                ("((?:src|href)=\")/assets/", $"$1{Prefix("/assets/")}"),
                ("((?:src|href)=\")/manifest\\.json", $"$1{Prefix("/manifest.json")}"),
                ("((?:src|href)=\")/logo192\\.png", $"$1{Prefix("/logo192.png")}"),
                ("((?:src|href)=\")/logo512\\.png", $"$1{Prefix("/logo512.png")}"),
            };
        }

        internal static SoulseekClientOptions CreateInitialSoulseekClientOptions(OptionsAtStartup optionsAtStartup)
        {
            if (!IPAddress.TryParse(optionsAtStartup.Soulseek.ListenIpAddress, out var startupListenAddress))
            {
                startupListenAddress = IPAddress.Any;
            }

            return new SoulseekClientOptions(
                enableListener: true,
                listenIPAddress: startupListenAddress,
                listenPort: optionsAtStartup.Soulseek.ListenPort,
                enableDistributedNetwork: !optionsAtStartup.Soulseek.DistributedNetwork.Disabled,
                acceptDistributedChildren: !optionsAtStartup.Soulseek.DistributedNetwork.DisableChildren,
                distributedChildLimit: optionsAtStartup.Soulseek.DistributedNetwork.ChildLimit,
                maximumUploadSpeed: optionsAtStartup.Global.Upload.SpeedLimit,
                maximumConcurrentUploads: optionsAtStartup.Global.Upload.Slots,
                maximumDownloadSpeed: optionsAtStartup.Global.Download.SpeedLimit,
                maximumConcurrentDownloads: optionsAtStartup.Global.Download.Slots,
                minimumDiagnosticLevel: optionsAtStartup.Soulseek.DiagnosticLevel.ToEnum<Soulseek.Diagnostics.DiagnosticLevel>(),
                maximumConcurrentSearches: 2,
                peerObfuscationOptions: SoulseekObfuscationSupport.BuildRuntimeOptions(optionsAtStartup.Soulseek),
                raiseEventsAsynchronously: true);
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
                    .AddConfigurationProviders(EnvironmentVariablePrefix, ConfigurationFile, reloadOnChange: !OptionsAtStartup.Flags.NoConfigWatch)
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

            foreach (var warning in GetConfigurationCompatibilityWarnings(ConfigurationFile, OptionsAtStartup))
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
                    .AddConfigurationProviders(EnvironmentVariablePrefix, ConfigurationFile, reloadOnChange: !OptionsAtStartup.Flags.NoConfigWatch);

                // Deterministic port probe for E2E startup debugging.
                var portStr = builder.Configuration[$"{AppName}:Web:Port"] ?? "<null>";
                if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
                {
                    System.Console.Error.WriteLine($"[ConfigProbe] slskd:web:port={portStr}");
                }

                // Note: OptionsAtStartup was bound earlier from a different Configuration instance.
                // Since Options properties are init-only, we can't rebind them. Instead, we read
                // values directly from builder.Configuration when needed (e.g., in UseKestrel below).
                builder.Host
                    .UseSerilog();

                var webPortSection = builder.Configuration.GetSection($"{AppName}:Web:Port");
                var webPort = webPortSection.Exists() && int.TryParse(webPortSection.Value, out var port)
                    ? port
                    : OptionsAtStartup.Web.Port; // Fallback to OptionsAtStartup if not in config

                var webAddressSection = builder.Configuration.GetSection($"{AppName}:Web:Address");
                var webAddress = webAddressSection.Exists() && !string.IsNullOrEmpty(webAddressSection.Value)
                    ? webAddressSection.Value
                    : OptionsAtStartup.Web.Address; // Fallback to OptionsAtStartup if not in config

                var configuredAddress = webAddress == "*" ? IPAddress.Any.ToString() : webAddress;
                if (!IPAddress.TryParse(configuredAddress, out var listenAddress))
                {
                    Log.Warning("Invalid web bind address '{Address}', defaulting to 0.0.0.0", configuredAddress);
                    listenAddress = IPAddress.Any;
                }

                var listenAddressUrl = listenAddress.AddressFamily == AddressFamily.InterNetworkV6
                    ? $"[{listenAddress}]"
                    : listenAddress.ToString();

                builder.WebHost
                    .UseUrls($"http://{listenAddressUrl}:{webPort}")
                    .UseKestrel(options =>
                    {
                        // PR-09: Global body size cap; configurable via Web.MaxRequestBodySize (default 10 MB). MeshGateway and others may enforce lower per-route.
                        options.Limits.MaxRequestBodySize = OptionsAtStartup.Web.MaxRequestBodySize;

                        Log.Debug("[ConfigProbe] slskd:web:port={A} slskd:slskd:web:port={B} using={C}",
                            builder.Configuration.GetValue<string>($"{AppName}:Web:Port") ?? "null",
                            builder.Configuration.GetValue<string>($"{AppName}:{AppName}:Web:Port") ?? "null",
                            webPort);

                        Log.Information($"[Kestrel] Configuring HTTP listener at http://{listenAddressUrl}:{webPort}/ (from config: port={webPortSection.Exists()}, address={webAddressSection.Exists()})");
                        options.Listen(listenAddress, webPort);
                        Log.Debug($"[Kestrel] HTTP listener configured");

                        if (!string.IsNullOrWhiteSpace(OptionsAtStartup.Web.Socket))
                        {
                            Log.Information($"Configuring HTTP listener on unix domain socket (UDS) {OptionsAtStartup.Web.Socket}");
                            options.ListenUnixSocket(OptionsAtStartup.Web.Socket);
                        }

                        if (!OptionsAtStartup.Web.Https.Disabled)
                        {
                            Log.Information($"Configuring HTTPS listener at https://{IPAddress.Any}:{OptionsAtStartup.Web.Https.Port}/");
                            options.Listen(IPAddress.Any, OptionsAtStartup.Web.Https.Port, listenOptions =>
                            {
                                var cert = OptionsAtStartup.Web.Https.Certificate;

                                if (!string.IsNullOrEmpty(cert.Pfx))
                                {
                                    Log.Information($"Using certificate from {cert.Pfx}");
                                    listenOptions.UseHttps(cert.Pfx, cert.Password);
                                }
                                else
                                {
                                    Log.Information($"Using randomly generated self-signed certificate");
                                    listenOptions.UseHttps(X509.Generate(subject: AppName));
                                }
                            });
                        }
                    });

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

                Log.Information("Configuration complete.  Starting application...");

                // Add lifecycle hook to log when host actually starts listening
                var lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
                lifetime.ApplicationStarted.Register(() =>
                {
                    var addresses = app.Urls;
                    Log.Information("✓ Host started and bound to: {Addresses}", string.Join(", ", addresses));

                    if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
                    {
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
                });

                lifetime.ApplicationStopping.Register(() =>
                {
                    Log.Information("Application is stopping...");
                });

                if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
                {
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

                Log.Debug("[Program] About to call app.Run()...");
                Log.Debug("[Program] app.Run() will start the web server and all hosted services...");

                // Add lifecycle hooks to track startup progress
                var hostLifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();

                // Log when web server starts listening (happens before hosted services StartAsync)
                hostLifetime.ApplicationStarted.Register(() =>
                {
                    Log.Debug("[Program] ApplicationStarted event fired - all hosted services have completed StartAsync");

                    // Start LAN discovery advertising if enabled
                    if (OptionsAtStartup.Feature.IdentityFriends)
                    {
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
                });

                hostLifetime.ApplicationStopping.Register(() =>
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
                });

                // Try to detect if we're hanging during web server startup
                Log.Debug("[Program] Calling app.Run() - this will block until shutdown...");
                Log.Debug("[Program] If you see this but not 'Host started and bound', the web server is hanging");

                // Deterministic Kestrel binding probe for E2E startup debugging.
                if (Environment.GetEnvironmentVariable("SLSKDN_E2E_SERVER_PROBE") == "1")
                {
                    System.Console.Error.WriteLine($"[KestrelProbe] URLs={string.Join(";", app.Urls)}");
                }

                app.Run();
                Log.Debug("[Program] app.Run() returned after host shutdown");
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

        /// <summary>
        /// Gets a configuration section under the slskd: namespace.
        /// This ensures all options bind correctly to the YAML provider's namespace.
        /// </summary>
        private static IConfigurationSection GetSlskdSection(this IConfiguration configuration, string sectionName)
        {
            return configuration.GetSection($"{AppName}:{sectionName}");
        }

        private static IConfigurationBuilder AddConfigurationProviders(this IConfigurationBuilder builder, string environmentVariablePrefix, string configurationFile, bool reloadOnChange)
        {
            configurationFile = Path.GetFullPath(configurationFile);
            Log.Information("[Config] Loading configuration from {ConfigFile}", configurationFile);

            var multiValuedArguments = typeof(Options)
                .GetPropertiesRecursively()
                .Where(p => p.PropertyType.IsArray)
                .SelectMany(p =>
                    p.CustomAttributes
                        .Where(a => a.AttributeType == typeof(ArgumentAttribute))
                        .Select(a => new[] { a.ConstructorArguments[0].Value, a.ConstructorArguments[1].Value })
                        .SelectMany(v => v))
                .Select(v => v?.ToString())
                .Where(v => v != "\u0000")
                .OfType<string>()
                .ToArray();

            var configurationDirectory = Path.GetDirectoryName(configurationFile);
            if (string.IsNullOrWhiteSpace(configurationDirectory))
            {
                throw new InvalidOperationException($"Configuration file path '{configurationFile}' does not have a directory component.");
            }

            var result = builder
                .AddDefaultValues(
                    targetType: typeof(Options))
                .AddEnvironmentVariables(
                    targetType: typeof(Options),
                    prefix: environmentVariablePrefix)
#pragma warning disable CA2000 // Framework configuration infrastructure owns the file provider lifecycle.
                .AddYamlFile(
                    path: Path.GetFileName(configurationFile),
                    targetType: typeof(Options),
                    optional: true,
                    reloadOnChange: reloadOnChange,
                    provider: CreateOwnedPhysicalFileProvider(configurationDirectory, ExclusionFilters.None)) // required for locations outside of the app directory
#pragma warning restore CA2000
                .AddCommandLine(
                    targetType: typeof(Options),
                    multiValuedArguments,
                    commandLine: Environment.CommandLine)
                .Add(VolatileOverlayConfigurationSource); // this must come last in order to supersede all other sources

            Log.Information("[Config] Configuration providers added, YAML file: {ConfigFile}", configurationFile);
            return result;
        }

        internal static IReadOnlyList<string> GetConfigurationCompatibilityWarnings(string configurationFile, Options options)
        {
            if (!IOFile.Exists(configurationFile))
            {
                return Array.Empty<string>();
            }

            var warnings = new List<string>();
            var lines = IOFile.ReadAllLines(configurationFile);
            var hasCanonicalIntegrations = HasTopLevelKey(lines, "integrations");
            var hasCanonicalTransferGroups = HasDirectChildKey(lines, "transfers", "groups");
            var hasCanonicalUploadLimits = HasNestedChildKey(lines, new[] { "transfers", "upload" }, "limits");

            if (HasTopLevelKey(lines, "global"))
            {
                warnings.Add("Configuration key 'global' is deprecated; slskdN accepts it for now, but 'transfers' is the canonical transfer-rate and retry section.");
            }

            if (HasTopLevelKey(lines, "groups") && !hasCanonicalTransferGroups)
            {
                warnings.Add("Top-level configuration key 'groups' is accepted for compatibility; new configuration should place groups under 'transfers.groups'.");
            }

            if (HasDirectChildKey(lines, "transfers", "limits") && !hasCanonicalUploadLimits)
            {
                warnings.Add("Configuration key 'transfers.limits' is accepted for compatibility; new configuration should place global upload limits under 'transfers.upload.limits'.");
            }

            if (HasTopLevelKey(lines, "integration") && !hasCanonicalIntegrations)
            {
                warnings.Add("Configuration key 'integration' is deprecated; slskdN accepts it for now, but 'integrations' is the canonical external integration section.");
            }

            if (HasGroupLevelLimits(lines))
            {
                warnings.Add("Group-level 'limits' entries are accepted for compatibility; place them under each group's 'upload' section in new configuration files.");
            }

            if (options.Global.Download.Retry.MaxDelay < MinimumRetryMaxDelayMilliseconds)
            {
                warnings.Add($"Download retry max_delay is below {MinimumRetryMaxDelayMilliseconds}ms; slskdN will clamp retry scheduling to that floor.");
            }

            return warnings.AsReadOnly();
        }

        private const int MinimumRetryMaxDelayMilliseconds = 30_000;

        private static bool HasTopLevelKey(IEnumerable<string> lines, string key)
        {
            var prefix = $"{key}:";
            return lines
                .Select(StripYamlComment)
                .Any(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasDirectChildKey(IEnumerable<string> lines, string parentKey, string childKey)
            => HasNestedChildKey(lines, new[] { parentKey }, childKey);

        private static bool HasNestedChildKey(IEnumerable<string> lines, IReadOnlyList<string> parentPath, string childKey)
        {
            var matchedDepth = 0;
            var matchedIndents = new List<int>();
            var childIndent = -1;

            foreach (var rawLine in lines.Select(StripYamlComment))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
                var trimmed = rawLine.TrimStart();

                while (matchedDepth > 0 && indent <= matchedIndents[matchedDepth - 1])
                {
                    matchedDepth--;
                    matchedIndents.RemoveAt(matchedIndents.Count - 1);
                    childIndent = -1;
                }

                if (matchedDepth < parentPath.Count &&
                    trimmed.StartsWith($"{parentPath[matchedDepth]}:", StringComparison.OrdinalIgnoreCase))
                {
                    matchedDepth++;
                    matchedIndents.Add(indent);
                    childIndent = -1;
                    continue;
                }

                if (matchedDepth != parentPath.Count)
                {
                    continue;
                }

                if (childIndent < 0)
                {
                    childIndent = indent;
                }

                if (indent == childIndent && trimmed.StartsWith($"{childKey}:", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGroupLevelLimits(IEnumerable<string> lines)
        {
            var inGroups = false;
            var groupsIndent = 0;
            var groupIndent = 0;

            foreach (var rawLine in lines.Select(StripYamlComment))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                var indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
                var trimmed = rawLine.TrimStart();

                if (indent == 0)
                {
                    inGroups = trimmed.StartsWith("groups:", StringComparison.OrdinalIgnoreCase);
                    groupsIndent = 0;
                    groupIndent = 0;
                    continue;
                }

                if (!inGroups)
                {
                    continue;
                }

                if (indent <= groupsIndent)
                {
                    inGroups = false;
                    continue;
                }

                if (groupIndent == 0 && trimmed.EndsWith(":", StringComparison.Ordinal))
                {
                    groupIndent = indent;
                    continue;
                }

                if (groupIndent > 0 && indent == groupIndent + 2 && trimmed.StartsWith("limits:", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string StripYamlComment(string line)
        {
            var index = line.IndexOf('#');
            return index >= 0 ? line[..index].TrimEnd() : line.TrimEnd();
        }

        private static void RecreateConfigurationFileIfMissing(string configurationFile)
        {
            if (!IOFile.Exists(configurationFile))
            {
                try
                {
                    Log.Warning("Configuration file {ConfigurationFile} does not exist; creating from example", configurationFile);
                    var source = Path.Combine(AppContext.BaseDirectory, "config", $"{AppName}.example.yml");
                    var destination = configurationFile;
                    IOFile.Copy(source, destination);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to create configuration file {ConfigurationFile}: {Message}", configurationFile, ex.Message);
                }
            }
        }

        private static (string Filename, string Password) GenerateX509Certificate(string password, string filename)
        {
            filename = Path.Combine(AppContext.BaseDirectory, filename);

            using var cert = X509.Generate(subject: AppName, password, X509KeyStorageFlags.Exportable);
            IOFile.WriteAllBytes(filename, cert.Export(X509ContentType.Pkcs12, password));
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                try
                {
                    IOFile.SetUnixFileMode(filename, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not set restrictive permissions on generated certificate {Filename}", filename);
                }
            }

            return (filename, password);
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
            if (!System.IO.Directory.Exists(directory))
            {
                if (createIfMissing)
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        throw new IOException($"Directory {directory} does not exist, and could not be created: {ex.Message}", ex);
                    }
                }
                else
                {
                    throw new IOException($"Directory {directory} does not exist");
                }
            }

            if (verifyWriteable)
            {
                try
                {
                    var file = Guid.NewGuid().ToString();
                    var probe = Path.Combine(directory, file);
                    IOFile.WriteAllText(probe, string.Empty);
                    IOFile.Delete(probe);
                }
                catch (Exception ex)
                {
                    throw new IOException($"Directory {directory} is not writeable: {ex.Message}", ex);
                }
            }
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
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Overlay.QuicOverlayClient>>();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.Overlay.OverlayOptions>>();
            var signer = serviceProvider.GetRequiredService<Mesh.Overlay.IControlSigner>();
            var privacyLayer = serviceProvider.GetService<Mesh.Privacy.IPrivacyLayer>();
            return new Mesh.Overlay.QuicOverlayClient(logger, options, signer, privacyLayer);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        [System.Runtime.Versioning.SupportedOSPlatform("macos")]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        internal static Mesh.Overlay.QuicDataClient CreateQuicDataClient(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Mesh.Overlay.QuicDataClient>>();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Mesh.Overlay.DataOverlayOptions>>();
            return new Mesh.Overlay.QuicDataClient(logger, options);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("linux")]
        [System.Runtime.Versioning.SupportedOSPlatform("macos")]
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        internal static Mesh.Overlay.QuicOverlayServer CreateQuicOverlayServer(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<Mesh.Overlay.QuicOverlayServer>(serviceProvider);
        }

        internal static bool ShouldRunStandaloneUdpOverlayServer(bool overlayEnabled, bool sharedMeshUdpRequested)
        {
            return overlayEnabled && !sharedMeshUdpRequested;
        }

        internal static bool IsExpectedSoulseekNetworkException(Exception exception)
        {
            var flattened = FlattenExceptions(exception).ToList();

            return flattened.Count > 0 && flattened.All(IsExpectedSoulseekNetworkExceptionCore);
        }

        internal static Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet? TryGetAndStoreAntiforgeryTokens(
            HttpContext context,
            Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
        {
            try
            {
                return antiforgery.GetAndStoreTokens(context);
            }
            catch (Exception ex) when (IsStaleAntiforgeryTokenException(ex))
            {
                ClearKnownAntiforgeryCookies(context);
                Log.Warning("[CSRF Middleware] Cleared stale antiforgery cookies for {Path} after key-ring mismatch", context.Request.Path);
                return antiforgery.GetAndStoreTokens(context);
            }
        }

        internal static bool IsStaleAntiforgeryTokenException(Exception exception)
        {
            return FlattenExceptions(exception).Any(innerException =>
                innerException is CryptographicException ||
                innerException.Message.Contains("could not be decrypted", StringComparison.OrdinalIgnoreCase) ||
                innerException.Message.Contains("key ring", StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StripKnownAntiforgeryCookiesFromRequest(HttpContext context)
        {
            var filteredSegments = new List<string>();
            var removed = false;

            foreach (var headerValue in context.Request.Headers.Cookie)
            {
                if (headerValue is null)
                {
                    continue;
                }

                foreach (var segment in headerValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var separatorIndex = segment.IndexOf('=');
                    var cookieName = separatorIndex >= 0 ? segment[..separatorIndex].Trim() : segment.Trim();

                    if (string.Equals(cookieName, $"XSRF-COOKIE-{OptionsAtStartup.Web.Port}", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cookieName, $"XSRF-TOKEN-{OptionsAtStartup.Web.Port}", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cookieName, "XSRF-COOKIE", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cookieName, "XSRF-TOKEN", StringComparison.OrdinalIgnoreCase))
                    {
                        removed = true;
                        continue;
                    }

                    filteredSegments.Add(segment);
                }
            }

            if (!removed)
            {
                return false;
            }

            if (filteredSegments.Count == 0)
            {
                context.Request.Headers.Remove("Cookie");
            }
            else
            {
                context.Request.Headers.Cookie = string.Join("; ", filteredSegments);
            }

            context.Features.Set<Microsoft.AspNetCore.Http.Features.IRequestCookiesFeature>(
                new Microsoft.AspNetCore.Http.Features.RequestCookiesFeature(context.Features));

            return true;
        }

        internal static void ClearKnownAntiforgeryCookies(HttpContext context)
        {
            var options = new CookieOptions
            {
                Path = "/",
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
            };

            context.Response.Cookies.Delete($"XSRF-COOKIE-{OptionsAtStartup.Web.Port}", options);
            context.Response.Cookies.Delete($"XSRF-TOKEN-{OptionsAtStartup.Web.Port}", options);
            context.Response.Cookies.Delete("XSRF-COOKIE", options);
            context.Response.Cookies.Delete("XSRF-TOKEN", options);
        }

        private static IEnumerable<Exception> FlattenExceptions(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    foreach (var flattenedInnerException in FlattenExceptions(innerException))
                    {
                        yield return flattenedInnerException;
                    }
                }

                yield break;
            }

            yield return exception;

            if (exception.InnerException is not null)
            {
                foreach (var innerException in FlattenExceptions(exception.InnerException))
                {
                    yield return innerException;
                }
            }
        }

        private static bool IsExpectedSoulseekNetworkExceptionCore(Exception exception)
        {
            var typeName = exception.GetType().FullName ?? exception.GetType().Name;
            var details = exception.ToString();
            var isSoulseekMessageConnectionClosed =
                exception is InvalidOperationException &&
                details.Contains("The underlying Tcp connection is closed", StringComparison.Ordinal) &&
                details.Contains("Soulseek.Network.MessageConnection.ReadContinuouslyAsync", StringComparison.Ordinal);
            var isSoulseekTimerResetReadRace =
                exception is NullReferenceException &&
                details.Contains("Soulseek.Extensions.Reset(", StringComparison.Ordinal) &&
                details.Contains("Soulseek.Network.MessageConnection.ReadContinuouslyAsync", StringComparison.Ordinal);
            var isSoulseekTimerResetWriteRace =
                exception is NullReferenceException &&
                details.Contains("Soulseek.Extensions.Reset(", StringComparison.Ordinal) &&
                details.Contains("Soulseek.Network.Tcp.Connection.WriteInternalAsync", StringComparison.Ordinal);
            var isSoulseekTcpDoubleDisconnectRace =
                exception is InvalidOperationException &&
                details.Contains("An attempt was made to transition a task to a final state", StringComparison.Ordinal) &&
                details.Contains("Soulseek.Network.Tcp.Connection.Disconnect", StringComparison.Ordinal);
            var isSoulseekListenerSocketDisposed =
                exception is ObjectDisposedException listenerDisposedException &&
                string.Equals(listenerDisposedException.ObjectName, "System.Net.Sockets.Socket", StringComparison.Ordinal) &&
                details.Contains("Soulseek.Network.Tcp.Listener.ListenContinuouslyAsync", StringComparison.Ordinal);

            var isNetworkFailure =
                exception is TimeoutException ||
                exception is OperationCanceledException ||
                exception is IOException ||
                (exception is ObjectDisposedException objectDisposedException && string.Equals(objectDisposedException.ObjectName, "Connection", StringComparison.Ordinal)) ||
                exception is System.Net.Sockets.SocketException ||
                isSoulseekMessageConnectionClosed ||
                isSoulseekTimerResetReadRace ||
                isSoulseekTimerResetWriteRace ||
                isSoulseekTcpDoubleDisconnectRace ||
                isSoulseekListenerSocketDisposed ||
                typeName.Contains("Soulseek.ConnectionReadException", StringComparison.Ordinal) ||
                typeName.Contains("Soulseek.ConnectionException", StringComparison.Ordinal) ||
                typeName.Contains("Soulseek.TransferException", StringComparison.Ordinal) ||
                typeName.Contains("Soulseek.TransferRejectedException", StringComparison.Ordinal) ||
                typeName.Contains("Soulseek.TransferReportedFailedException", StringComparison.Ordinal);

            if (!isNetworkFailure)
            {
                return false;
            }

            return details.Contains("Soulseek.Network.PeerConnectionManager", StringComparison.Ordinal) ||
                details.Contains("Soulseek.Network.DistributedConnectionManager", StringComparison.Ordinal) ||
                details.Contains("Soulseek.Network.Tcp.Connection", StringComparison.Ordinal) ||
                details.Contains("Soulseek.Network.Tcp.Listener", StringComparison.Ordinal) ||
                details.Contains("Failed to connect", StringComparison.Ordinal) ||
                details.Contains("Connection refused", StringComparison.Ordinal) ||
                details.Contains("Connection reset by peer", StringComparison.Ordinal) ||
                details.Contains("Remote connection closed", StringComparison.Ordinal) ||
                details.Contains("The underlying Tcp connection is closed", StringComparison.Ordinal) ||
                details.Contains("Download reported as failed by remote client", StringComparison.Ordinal) ||
                details.Contains("Enqueue failed due to internal error", StringComparison.Ordinal) ||
                details.Contains("Too many megabytes", StringComparison.Ordinal) ||
                details.Contains("Too many files", StringComparison.Ordinal) ||
                details.Contains("Transfer failed: Transfer complete", StringComparison.Ordinal) ||
                details.Contains("No route to host", StringComparison.Ordinal) ||
                details.Contains("Operation timed out", StringComparison.Ordinal) ||
                details.Contains("Connection timed out", StringComparison.Ordinal) ||
                details.Contains("The wait timed out", StringComparison.Ordinal) ||
                details.Contains("Inactivity timeout", StringComparison.Ordinal) ||
                details.Contains("Failed to read", StringComparison.Ordinal) ||
                details.Contains("Unable to read data from the transport connection", StringComparison.Ordinal) ||
                details.Contains("Operation canceled", StringComparison.Ordinal) ||
                details.Contains("Operation cancelled", StringComparison.Ordinal) ||
                details.Contains("Unknown PierceFirewall attempt", StringComparison.Ordinal) ||
                details.Contains("Cannot access a disposed object.", StringComparison.Ordinal);
        }

    }
}
