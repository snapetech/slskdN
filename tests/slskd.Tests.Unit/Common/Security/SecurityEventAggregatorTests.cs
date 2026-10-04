// <copyright file="SecurityEventAggregatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    [Fact]
    public void Report_EscapesUntrustedLogFieldsAndRetainsOriginalEvent()
    {
        var logger = new Mock<ILogger<SecurityEventAggregator>>();
        var message = "Path traversal attempt: /etc/passwd\r\nforged";
        var ipAddress = "192.0.2.1\r\nforged";
        var username = "Alice\r\nforged";
        var securityEvent = new SecurityEvent
        {
            Type = SecurityEventType.PathTraversal,
            Severity = SecuritySeverity.High,
            Message = message,
            IpAddress = ipAddress,
            Username = username,
        };
        using var aggregator = new SecurityEventAggregator(logger.Object);

        aggregator.Report(securityEvent);

        logger.Verify(
            entry => entry.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Path traversal attempt: /etc/passwd\\r\\nforged", StringComparison.Ordinal) &&
                    state.ToString()!.Contains("192.0.2.1\\r\\nforged", StringComparison.Ordinal) &&
                    state.ToString()!.Contains("Alice\\r\\nforged", StringComparison.Ordinal) &&
                    !state.ToString()!.Contains(message, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        var retainedEvent = Assert.Single(aggregator.GetRecentEvents());
        Assert.Same(securityEvent, retainedEvent);
        Assert.Equal(message, retainedEvent.Message);
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
