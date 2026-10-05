// <copyright file="RescueServiceLoggingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.Rescue;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using slskd.Mesh;
using slskd.Tests.Unit;
using slskd.Transfers.Rescue;
using Xunit;

[Collection(StaticEventCollection.Name)]
public class RescueServiceLoggingTests
{
    [Fact]
    public async Task ActivateRescueModeAsync_EscapesCallerAndPeerDiagnosticsInLogs()
    {
        var sink = new CapturingLogSink();
        var originalLogger = Log.Logger;
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();
        Log.Logger = logger;

        try
        {
            var meshDirectory = new Mock<IMeshDirectory>();
            meshDirectory
                .Setup(directory => directory.FindPeersByContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("peer failure\r\nforged"));
            var service = new RescueService(meshDirectory: meshDirectory.Object);

            var result = await service.ActivateRescueModeAsync(
                "transfer\r\nforged",
                "peer",
                "Music\\track\r\ninjected [mbid-550e8400-e29b-41d4-a716-446655440000].flac",
                1_000,
                500,
                UnderperformanceReason.ThroughputTooLow,
                CancellationToken.None);

            Assert.Equal(RescueActivationOutcome.NoOverlayPeers, result.Outcome);

            var activation = Assert.Single(sink.Events, logEvent => logEvent.RenderMessage().StartsWith("[RESCUE] Activating rescue mode", StringComparison.Ordinal));
            Assert.Contains("track\\r\\ninjected", activation.RenderMessage());
            Assert.DoesNotContain("\r\n", activation.RenderMessage());

            var guardrail = Assert.Single(sink.Events, logEvent => logEvent.RenderMessage().StartsWith("[GUARDRAIL] Rescue allowed", StringComparison.Ordinal));
            Assert.Contains("transfer\\r\\nforged", guardrail.RenderMessage());
            Assert.Contains("track\\r\\ninjected", guardrail.RenderMessage());
            Assert.DoesNotContain("\r\n", guardrail.RenderMessage());

            var meshFailure = Assert.Single(sink.Events, logEvent => logEvent.RenderMessage().StartsWith("[RESCUE] Mesh query failed", StringComparison.Ordinal));
            Assert.Contains("peer failure\\r\\nforged", meshFailure.RenderMessage());
            Assert.DoesNotContain("\r\n", meshFailure.RenderMessage());
            Assert.Null(meshFailure.Exception);
        }
        finally
        {
            Log.Logger = originalLogger;
        }
    }

    private sealed class CapturingLogSink : ILogEventSink
    {
        private readonly ConcurrentBag<LogEvent> events = [];

        public IReadOnlyCollection<LogEvent> Events => events.ToArray();

        public void Emit(LogEvent logEvent) => events.Add(logEvent);
    }
}
