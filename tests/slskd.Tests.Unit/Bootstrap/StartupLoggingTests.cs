// <copyright file="StartupLoggingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System;
using System.IO;
using Serilog;
using slskd.Bootstrap;
using slskd.Tests.Unit;
using Xunit;

[Collection(StaticEventCollection.Name)]
public sealed class StartupLoggingTests
{
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
}
