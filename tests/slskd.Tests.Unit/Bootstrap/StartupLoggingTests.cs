// <copyright file="StartupLoggingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using slskd.Bootstrap;
using slskd.Tests.Unit;
using Xunit;

[Collection(StaticEventCollection.Name)]
public sealed class StartupLoggingTests
{
    [Fact]
    public void LogStartupDiagnostics_EscapesConfiguredPathsAndRedactsLokiCredentials()
    {
        var root = Path.Combine(Path.GetTempPath(), $"slskdn-startup-diagnostics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var configurationFile = Path.Combine(root, "slskd.yml");
        File.WriteAllText(configurationFile, string.Empty);
        var sink = new CapturingLogSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            var lineBreak = "\r\nforged startup event";
            var options = new OptionsAtStartup
            {
                Flags = new Options.FlagsOptions { NoLogo = true },
                InstanceName = $"instance{lineBreak}",
                Logger = new Options.LoggerOptions
                {
                    Disk = true,
                    Loki = "https://operator:secret@logs.example.test/tenant/push?token=secret",
                },
            };
            var context = new StartupDiagnosticsContext(
                "test-version",
                IsDevelopment: false,
                IsCanary: false,
                IssuesUrl: string.Empty,
                ProcessId: 9001,
                ExecutablePath: $"/opt/slskd{lineBreak}",
                BaseDirectory: $"/base{lineBreak}",
                InvocationId: Guid.NewGuid(),
                AppDirectory: $"/app{lineBreak}",
                ConfigurationFile: configurationFile,
                DataDirectory: $"/data{lineBreak}",
                LogDirectory: $"/logs{lineBreak}");

            StartupDiagnostics.LogStartupIdentity(options, context, logger);
            StartupDiagnostics.LogConfigurationUsage(options, context, "slskd", logger);

            var rendered = string.Join("\n", sink.Events.Select(logEvent => logEvent.RenderMessage()));
            Assert.Contains("/opt/slskd\\r\\nforged startup event", rendered);
            Assert.Contains("/base\\r\\nforged startup event", rendered);
            Assert.Contains("/app\\r\\nforged startup event", rendered);
            Assert.Contains("/data\\r\\nforged startup event", rendered);
            Assert.Contains("/logs\\r\\nforged startup event", rendered);
            Assert.Contains("instance\\r\\nforged startup event", rendered);
            Assert.Contains("https://logs.example.test", rendered);
            Assert.DoesNotContain("operator", rendered);
            Assert.DoesNotContain("secret", rendered);
            Assert.DoesNotContain("/tenant/push", rendered);
            Assert.DoesNotContain("\r\n", rendered);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RecreateConfigurationFileIfMissing_EscapesPathAndExceptionInLogs()
    {
        var sink = new CapturingLogSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();
        var configurationFile = Path.Combine(Path.GetTempPath(), "config\r\nforged.yml");
        var missingBaseDirectory = Path.Combine(Path.GetTempPath(), "missing\r\nsource");

        StartupFileSystem.RecreateConfigurationFileIfMissing(
            configurationFile,
            "slskd",
            missingBaseDirectory,
            logger);

        var warning = Assert.Single(sink.Events, logEvent => logEvent.RenderMessage().StartsWith("Configuration file", StringComparison.Ordinal));
        Assert.Contains("config\\r\\nforged.yml", warning.RenderMessage());
        Assert.DoesNotContain("\r\n", warning.RenderMessage());

        var failure = Assert.Single(sink.Events, logEvent => logEvent.RenderMessage().StartsWith("Failed to create configuration file", StringComparison.Ordinal));
        Assert.Contains("missing\\r\\nsource", failure.RenderMessage());
        Assert.DoesNotContain("\r\n", failure.RenderMessage());
        Assert.Null(failure.Exception);
    }

    [Fact]
    public void Configure_WhenLogRecordCallbackThrows_EscapesFallbackText()
    {
        var originalLogger = Log.Logger;
        var originalStandardError = Console.Error;
        using var capturedStandardError = new StringWriter();
        Console.SetError(capturedStandardError);

        try
        {
            var options = new OptionsAtStartup
            {
                Logger = new Options.LoggerOptions
                {
                    Disk = false,
                    NoColor = true,
                },
            };

            StartupLogging.Configure(
                options,
                "slskdn-test",
                Path.GetTempPath(),
                Guid.NewGuid(),
                Environment.ProcessId,
                _ => throw new InvalidOperationException("sink failure\r\nforged"));

            Log.Information("Remote message: {Value}", "first line\r\nforged");

            var output = capturedStandardError.ToString();
            Assert.Contains("sink failure\\r\\nforged", output);
            Assert.Contains("first line\\r\\nforged", output);
            Assert.DoesNotContain("\r", output.TrimEnd('\r', '\n'));
            Assert.DoesNotContain("\n", output.TrimEnd('\r', '\n'));
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
            Console.SetError(originalStandardError);
        }
    }

    private sealed class CapturingLogSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
