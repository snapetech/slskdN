// <copyright file="RecentSecurityEventQueryTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

public sealed class RecentSecurityEventQueryTests
{
    [Fact]
    public void ParanoidMode_EmptyAndPartialHistoryPreserveNewestFirstOrder()
    {
        var paranoidMode = new ParanoidMode(NullLogger<ParanoidMode>.Instance);

        Assert.Empty(paranoidMode.GetRecentAnomalies(10));

        paranoidMode.TrackDisconnect("anomaly-0");
        paranoidMode.TrackDisconnect("anomaly-1");
        paranoidMode.TrackDisconnect("anomaly-2");

        Assert.Equal(
            new[] { "anomaly-2", "anomaly-1", "anomaly-0" },
            paranoidMode.GetRecentAnomalies(10).Select(anomaly => anomaly.Details));
        Assert.Equal(
            new[] { "anomaly-2", "anomaly-1" },
            paranoidMode.GetRecentAnomalies(2).Select(anomaly => anomaly.Details));
    }

    [Fact]
    public void Honeypot_FullRetentionSmallPageBoundsAllocation()
    {
        using var honeypot = new Honeypot(NullLogger<Honeypot>.Instance);
        var ip = IPAddress.Parse("192.0.2.1");
        for (var index = 0; index < Honeypot.MaxEvents; index++)
        {
            honeypot.RecordInteraction(
                ip,
                "scanner",
                HoneypotAction.Browse,
                $"event-{index:D5}");
        }

        for (var iteration = 0; iteration < 8; iteration++)
        {
            _ = honeypot.GetRecentEvents(50);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var events = honeypot.GetRecentEvents(50);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(50, events.Count);
        Assert.Equal("event-09999", events[0].Interaction.Filename);
        Assert.Equal("event-09950", events[^1].Interaction.Filename);
        Assert.Empty(honeypot.GetRecentEvents(0));
        Assert.Empty(honeypot.GetRecentEvents(-1));
        Assert.True(allocatedBytes < 2_048, $"Allocated {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void FingerprintDetection_FullRetentionSmallPageBoundsAllocation()
    {
        using var detection = new FingerprintDetection(NullLogger<FingerprintDetection>.Instance);
        for (var index = 0; index < FingerprintDetection.MaxEvents + 3; index++)
        {
            detection.RecordConnection(IPAddress.Loopback, index + 1);
        }

        for (var iteration = 0; iteration < 8; iteration++)
        {
            _ = detection.GetRecentEvents(50);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var events = detection.GetRecentEvents(50);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(50, events.Count);
        Assert.Contains("5003", events[0].Indicators[0].Description, StringComparison.Ordinal);
        Assert.Contains("4954", events[^1].Indicators[0].Description, StringComparison.Ordinal);
        Assert.Empty(detection.GetRecentEvents(0));
        Assert.Empty(detection.GetRecentEvents(-1));
        Assert.True(allocatedBytes < 2_048, $"Allocated {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void ParanoidMode_FullRetentionSmallPageBoundsAllocation()
    {
        var paranoidMode = new ParanoidMode(NullLogger<ParanoidMode>.Instance);
        for (var index = 0; index < ParanoidMode.MaxAnomalies; index++)
        {
            paranoidMode.TrackDisconnect($"anomaly-{index:D4}");
        }

        for (var iteration = 0; iteration < 8; iteration++)
        {
            _ = paranoidMode.GetRecentAnomalies(50);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var anomalies = paranoidMode.GetRecentAnomalies(50);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(50, anomalies.Count);
        Assert.Equal("anomaly-0999", anomalies[0].Details);
        Assert.Equal("anomaly-0950", anomalies[^1].Details);
        Assert.Empty(paranoidMode.GetRecentAnomalies(0));
        Assert.Empty(paranoidMode.GetRecentAnomalies(-1));
        Assert.True(allocatedBytes < 2_048, $"Allocated {allocatedBytes:N0} bytes.");
    }
}
