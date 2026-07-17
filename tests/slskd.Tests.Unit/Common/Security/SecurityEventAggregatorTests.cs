// <copyright file="SecurityEventAggregatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

public sealed class SecurityEventAggregatorTests
{
    [Fact]
    public void QueriesReturnNewestMatchingEventsWithExistingFilters()
    {
        using var aggregator = new SecurityEventAggregator(NullLogger<SecurityEventAggregator>.Instance);
        aggregator.Report(CreateEvent("event-0", SecuritySeverity.High, "192.0.2.1", "Alice"));
        aggregator.Report(CreateEvent("event-1", SecuritySeverity.Low, "192.0.2.2", "Bob"));
        aggregator.Report(CreateEvent("event-2", SecuritySeverity.Critical, "192.0.2.1", "ALICE"));
        aggregator.Report(CreateEvent("event-3", SecuritySeverity.Medium, "192.0.2.1", "alice"));
        aggregator.Report(CreateEvent("event-4", SecuritySeverity.High, "192.0.2.2", "Alice"));

        Assert.Equal(
            new[] { "event-4", "event-3" },
            aggregator.GetRecentEvents(2, SecuritySeverity.Medium).Select(evt => evt.Id));
        Assert.Equal(
            new[] { "event-3", "event-2" },
            aggregator.GetEventsForIp(IPAddress.Parse("192.0.2.1"), 2).Select(evt => evt.Id));
        Assert.Equal(
            new[] { "event-4", "event-3", "event-2" },
            aggregator.GetEventsForUser("aLiCe", 3).Select(evt => evt.Id));
        Assert.Empty(aggregator.GetRecentEvents(0));
        Assert.Empty(aggregator.GetEventsForIp(IPAddress.Loopback, -1));
        Assert.Empty(aggregator.GetEventsForUser("Alice", 0));
    }

    [Fact]
    public void GetRecentEvents_FullRetentionSmallPageBoundsAllocation()
    {
        using var aggregator = new SecurityEventAggregator(NullLogger<SecurityEventAggregator>.Instance);
        for (var index = 0; index < SecurityEventAggregator.MaxEvents; index++)
        {
            aggregator.Report(CreateEvent(
                $"event-{index:D5}",
                SecuritySeverity.High,
                "192.0.2.1",
                "Alice"));
        }

        for (var iteration = 0; iteration < 8; iteration++)
        {
            _ = aggregator.GetRecentEvents(50, SecuritySeverity.High);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = aggregator.GetRecentEvents(50, SecuritySeverity.High);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(50, result.Count);
        Assert.Equal("event-09999", result[0].Id);
        Assert.Equal("event-09950", result[^1].Id);
        Assert.True(allocatedBytes < 2_048, $"Allocated {allocatedBytes:N0} bytes.");
    }

    private static SecurityEvent CreateEvent(
        string id,
        SecuritySeverity severity,
        string ipAddress,
        string username)
    {
        return new SecurityEvent
        {
            Id = id,
            Type = SecurityEventType.Connection,
            Severity = severity,
            Message = id,
            IpAddress = ipAddress,
            Username = username,
        };
    }
}
