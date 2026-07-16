// <copyright file="ConnectionFingerprintServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous.Security;

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.DhtRendezvous.Security;
using Xunit;

public class ConnectionFingerprintServiceTests
{
    [Fact]
    public void GetStats_AggregatesConnectionsAndSecurityEvents()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var first = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 1, "alice", null, null, null);
        var second = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 2, "alice", null, null, null);
        service.RecordConnection(IPAddress.Parse("192.0.2.2"), 3, "Alice", null, null, null);
        service.RecordDisconnection(first.Id, "done");
        service.RecordSecurityEvent(second.Id, "test", "detail");

        var stats = service.GetStats();

        Assert.Equal(3, stats.TotalFingerprints);
        Assert.Equal(2, stats.ActiveConnections);
        Assert.Equal(3, stats.ConnectionsLastHour);
        Assert.Equal(2, stats.UniqueIps);
        Assert.Equal(2, stats.UniqueUsernames);
        Assert.Equal(1, stats.TotalSecurityEvents);
        Assert.Equal(5, stats.EventLogSize);
    }

    [Fact]
    public void GetStats_ThousandFingerprintsUsesBoundedWorkingMemory()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        for (var index = 0; index < ConnectionFingerprintService.MaxFingerprints; index++)
        {
            service.RecordConnection(IPAddress.Loopback, index, "same-user", null, null, null);
        }

        _ = service.GetStats();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stats = service.GetStats();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(ConnectionFingerprintService.MaxFingerprints, stats.TotalFingerprints);
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected single-pass stats allocation below 8 KiB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void RecordConnection_AtCapacityEvictsOldestWithoutFullSortAllocation()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var oldest = service.RecordConnection(IPAddress.Loopback, 0, "same-user", null, null, null);
        Assert.True(SpinWait.SpinUntil(() => DateTimeOffset.UtcNow > oldest.Timestamp, TimeSpan.FromSeconds(1)));

        for (var index = 1; index < ConnectionFingerprintService.MaxFingerprints; index++)
        {
            service.RecordConnection(IPAddress.Loopback, index, "same-user", null, null, null);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var replacement = service.RecordConnection(IPAddress.Loopback, 10_000, "same-user", null, null, null);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Null(service.GetFingerprint(oldest.Id));
        Assert.Same(replacement, service.GetFingerprint(replacement.Id));
        Assert.Equal(ConnectionFingerprintService.MaxFingerprints, service.GetStats().TotalFingerprints);
        Assert.True(
            allocatedBytes < 32 * 1024,
            $"Expected single-pass eviction allocation below 32 KiB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void GetRecentEvents_ReturnsRequestedNewestEventsFirst()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var fingerprint = service.RecordConnection(IPAddress.Loopback, 1, "user", null, null, null);
        service.RecordSecurityEvent(fingerprint.Id, "first", "one");
        service.RecordSecurityEvent(fingerprint.Id, "second", "two");

        var events = service.GetRecentEvents(2);

        Assert.Equal(2, events.Count);
        Assert.Equal("second: two", events[0].Details);
        Assert.Equal("first: one", events[1].Details);
        Assert.Empty(service.GetRecentEvents(0));
        Assert.Empty(service.GetRecentEvents(-1));
    }

    [Fact]
    public void GetRecentEvents_FullLogUsesRequestedSizeWorkingMemory()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var fingerprint = service.RecordConnection(IPAddress.Loopback, 1, "user", null, null, null);
        for (var index = 0; index < ConnectionFingerprintService.MaxEventLogSize; index++)
        {
            service.RecordDisconnection(fingerprint.Id, string.Empty);
        }

        _ = service.GetRecentEvents();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var events = service.GetRecentEvents();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(100, events.Count);
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected requested-size tail allocation below 8 KiB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task RecordEvent_ConcurrentProducersRetainExactCapAndSize()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var fingerprint = service.RecordConnection(IPAddress.Loopback, 1, "user", null, null, null);
        const int ProducerCount = 4;
        const int EventsPerProducer = 3_000;

        var producers = Enumerable.Range(0, ProducerCount)
            .Select(producer => Task.Run(() =>
            {
                for (var index = 0; index < EventsPerProducer; index++)
                {
                    service.RecordDisconnection(fingerprint.Id, $"{producer}-{index}");
                }
            }));

        await Task.WhenAll(producers);

        Assert.Equal(ConnectionFingerprintService.MaxEventLogSize, service.GetStats().EventLogSize);
        Assert.Equal(
            ConnectionFingerprintService.MaxEventLogSize,
            service.GetRecentEvents(ConnectionFingerprintService.MaxEventLogSize + 1).Count);
    }

    [Fact]
    public void FindFingerprints_PreservesFiltersAndDescendingTimestampOrder()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        var older = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 1, "Alice", "cert-1", null, null);
        Assert.True(SpinWait.SpinUntil(() => DateTimeOffset.UtcNow > older.Timestamp, TimeSpan.FromSeconds(1)));
        var newer = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 2, "alice", "cert-1", null, null);
        service.RecordConnection(IPAddress.Parse("192.0.2.2"), 3, "bob", "cert-2", null, null);

        var results = service.FindFingerprints(
            ipHash: older.IpHash,
            username: "ALICE",
            certThumbprint: "cert-1",
            since: older.Timestamp);

        Assert.Equal(new[] { newer.Id, older.Id }, results.Select(result => result.Id));
    }

    [Fact]
    public void FindFingerprints_ThousandResultsAvoidsConcurrentValuesSnapshot()
    {
        var service = new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
        for (var index = 0; index < ConnectionFingerprintService.MaxFingerprints; index++)
        {
            service.RecordConnection(IPAddress.Loopback, index, "same-user", null, null, null);
        }

        _ = service.FindFingerprints();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var results = service.FindFingerprints();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(ConnectionFingerprintService.MaxFingerprints, results.Count);
        Assert.True(
            allocatedBytes < 48 * 1024,
            $"Expected direct dictionary query allocation below 48 KiB, got {allocatedBytes:N0} bytes.");
    }
}
