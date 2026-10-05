// <copyright file="DisasterModeLifecycleTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.DisasterMode;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using slskd.VirtualSoulfind.DisasterMode;
using Soulseek;
using Xunit;

public class DisasterModeLifecycleTests
{
    [Fact]
    public void DisasterModeCoordinator_Force_StartsInFullFallback()
    {
        var healthMonitor = new Mock<ISoulseekHealthMonitor>();
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new slskd.Options
        {
            VirtualSoulfind = new slskd.Core.VirtualSoulfindOptions
            {
                DisasterMode = new DisasterModeOptions { Force = true },
            },
        });
        using var coordinator = new DisasterModeCoordinator(
            NullLogger<DisasterModeCoordinator>.Instance,
            healthMonitor.Object,
            optionsMonitor.Object);

        Assert.Equal(DisasterModeLevel.FullFallback, coordinator.CurrentLevel);
        Assert.True(((IDisasterModeCoordinator)coordinator).IsDisasterModeActive);
    }

    [Fact]
    public void SoulseekClientWrapper_Dispose_UnsubscribesRoomMessageProxy()
    {
        var soulseekClient = new Mock<Soulseek.ISoulseekClient>();
        var wrapper = new SoulseekClientWrapper(soulseekClient.Object);

        wrapper.Dispose();

        soulseekClient.VerifyRemove(x => x.RoomMessageReceived -= It.IsAny<EventHandler<RoomMessageReceivedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void SoulseekClientWrapper_EscapesSubscriberExceptionBeforeLogging()
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
            var soulseekClient = new Mock<Soulseek.ISoulseekClient>();
            using var wrapper = new SoulseekClientWrapper(soulseekClient.Object);
            wrapper.RoomMessageReceived += (_, _) => throw new IOException("remote\r\nforged");

            soulseekClient.Raise(
                client => client.RoomMessageReceived += null,
                soulseekClient.Object,
                new RoomMessageReceivedEventArgs("room", "peer", "message"));

            var entry = Assert.Single(sink.Events, logEvent =>
                logEvent.RenderMessage().StartsWith("[VSF-DISCO] RoomMessageReceived subscriber failed", StringComparison.Ordinal));
            var rendered = entry.RenderMessage();

            Assert.Contains("IOException: remote\\r\\nforged", rendered);
            Assert.DoesNotContain("\r\n", rendered);
        }
        finally
        {
            Log.Logger = originalLogger;
        }
    }

    [Fact]
    public void DisasterModeCoordinator_Dispose_UnsubscribesHealthMonitor()
    {
        var healthMonitor = new Mock<ISoulseekHealthMonitor>();
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new slskd.Options());
        var coordinator = new DisasterModeCoordinator(
            NullLogger<DisasterModeCoordinator>.Instance,
            healthMonitor.Object,
            optionsMonitor.Object);

        coordinator.Dispose();

        healthMonitor.VerifyRemove(x => x.HealthChanged -= It.IsAny<EventHandler<SoulseekHealthChangedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void DisasterModeRecovery_Dispose_UnsubscribesHealthMonitor()
    {
        var healthMonitor = new Mock<ISoulseekHealthMonitor>();
        healthMonitor.SetupGet(x => x.CurrentHealth).Returns(SoulseekHealth.Healthy);
        var disasterMode = new Mock<IDisasterModeCoordinator>();
        disasterMode.SetupGet(x => x.CurrentLevel).Returns(DisasterModeLevel.SoulseekUnavailable);
        var optionsMonitor = new Mock<IOptionsMonitor<slskd.Options>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new slskd.Options());
        var recovery = new DisasterModeRecovery(
            NullLogger<DisasterModeRecovery>.Instance,
            healthMonitor.Object,
            disasterMode.Object,
            optionsMonitor.Object);

        recovery.Dispose();

        healthMonitor.VerifyRemove(x => x.HealthChanged -= It.IsAny<EventHandler<SoulseekHealthChangedEventArgs>>(), Times.Once);
    }

    private sealed class CapturingLogSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
