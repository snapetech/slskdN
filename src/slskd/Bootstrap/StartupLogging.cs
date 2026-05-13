// <copyright file="StartupLogging.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.SystemConsole.Themes;

public static class StartupLogging
{
    public static ILogger Configure(
        OptionsAtStartup optionsAtStartup,
        string appName,
        string logDirectory,
        Guid invocationId,
        int processId,
        Action<LogRecord> emitLogRecord)
    {
        Log.Logger = (optionsAtStartup.Debug ? new LoggerConfiguration().MinimumLevel.Debug() : new LoggerConfiguration().MinimumLevel.Information())
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", optionsAtStartup.Debug ? LogEventLevel.Warning : LogEventLevel.Fatal)
            .MinimumLevel.Override("slskd.Authentication.PassthroughAuthenticationHandler", LogEventLevel.Warning)
            .MinimumLevel.Override("slskd.Authentication.ApiKeyAuthenticationHandler", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning) // bump this down to Information to show SQL
            .Enrich.WithProperty("InstanceName", optionsAtStartup.InstanceName)
            .Enrich.WithProperty("InvocationId", invocationId)
            .Enrich.WithProperty("ProcessId", processId)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                theme: (optionsAtStartup.Logger.NoColor || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"))) ? ConsoleTheme.None : SystemConsoleTheme.Literate,
                outputTemplate: (optionsAtStartup.Debug ? "[{SourceContext}] " : string.Empty) + "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Async(config =>
                config.Conditional(
                    e => optionsAtStartup.Logger.Disk,
                    config => config.File(
                        Path.Combine(logDirectory, $"{appName}-.log"),
                        outputTemplate: (optionsAtStartup.Debug ? "[{SourceContext}] " : string.Empty) + "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        retainedFileTimeLimit: TimeSpan.FromDays(optionsAtStartup.Retention.Logs))))
            .WriteTo.Conditional(
                e => !string.IsNullOrEmpty(optionsAtStartup.Logger.Loki),
                config => config.GrafanaLoki(
                    optionsAtStartup.Logger.Loki ?? string.Empty,
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

                    emitLogRecord(record);
                }
                catch (Exception ex)
                {
                    Log.Error("Misconfigured delegating logger: {Exception}.  Message: {Message}", ex.Message, message);
                }
            }))
            .CreateLogger();

        if (optionsAtStartup.Flags.LogUnobservedExceptions)
        {
            // log Exceptions raised on fired-and-forgotten tasks, which adds very little value but might help debug someday
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Logger.Error(e.Exception, "Unobserved exception: {Message}", e.Exception.Message);
                e.SetObserved();
            };
        }

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;

            if (e.IsTerminating)
            {
                Log.Logger.Fatal(exception, "Unhandled fatal exception: {Message}", e.IsTerminating);
            }
            else
            {
                Log.Logger.Error(exception, "Unhandled exception: {Message}", exception?.Message ?? "Unknown exception");
            }
        };

        return Log.ForContext(typeof(Program));
    }
}
