// <copyright file="ConnectionFingerprintServicePerformanceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

public class ConnectionFingerprintServicePerformanceTests
{
    [Fact]
    public void GetStatsAndClear_PreserveAllCountersAndResetState()
    {
        var service = CreateService();
        var first = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 1, "alice", null, null, null);
        var second = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 2, null, null, null, null);
        service.SetUsername(second.Id, "Alice");
        service.RecordDisconnection(first.Id, "done");
        service.RecordSecurityEvent(second.Id, "test", "detail");

        var stats = service.GetStats();

        Assert.Equal(2, stats.TotalFingerprints);
        Assert.Equal(1, stats.ActiveConnections);
        Assert.Equal(2, stats.ConnectionsLastHour);
        Assert.Equal(1, stats.UniqueIpHashes);
        Assert.Equal(2, stats.UniqueUsernames);
        Assert.Equal(1, stats.TotalSecurityEvents);
        Assert.Equal(5, stats.EventLogSize);
        Assert.Equal(3, service.GetEventsForFingerprint(second.Id).Count);

        service.Clear();

        Assert.Equal(0, service.GetStats().EventLogSize);
        Assert.Empty(service.GetRecentEvents());
        Assert.Null(service.GetFingerprint(first.Id));
    }

    [Fact]
    public async Task ConcurrentEventsRetainExactCapAndBoundRecentAllocation()
    {
        var service = CreateService();
        var fingerprint = service.RecordConnection(IPAddress.Loopback, 1, "user", null, null, null);
        var producers = Enumerable.Range(0, 4)
            .Select(producer => Task.Run(() =>
            {
                for (var index = 0; index < 3_000; index++)
                {
                    service.RecordDisconnection(fingerprint.Id, $"{producer}-{index}");
                }
            }));

        await Task.WhenAll(producers);
        _ = service.GetRecentEvents();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var recent = service.GetRecentEvents();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(ConnectionFingerprintService.MaxEventLogSize, service.GetStats().EventLogSize);
        Assert.Equal(100, recent.Count);
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected requested-size recent-event allocation below 8 KiB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void AtCapacityEvictsOldestWithBoundedAllocation()
    {
        var service = CreateService();
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
    public void FindFingerprints_PreservesCombinedFiltersAndBoundedAllocation()
    {
        var service = CreateService();
        var first = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 1, "Alice", "cert", null, null);
        Assert.True(SpinWait.SpinUntil(() => DateTimeOffset.UtcNow > first.Timestamp, TimeSpan.FromSeconds(1)));
        var second = service.RecordConnection(IPAddress.Parse("192.0.2.1"), 2, "alice", "cert", null, null);

        var filtered = service.FindFingerprints(first.IpHash, "ALICE", "cert", first.Timestamp);

        Assert.Equal(new[] { second.Id, first.Id }, filtered.Select(result => result.Id));

        for (var index = 2; index < ConnectionFingerprintService.MaxFingerprints; index++)
        {
            service.RecordConnection(IPAddress.Loopback, index, "same-user", null, null, null);
        }

        _ = service.FindFingerprints();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var all = service.FindFingerprints();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(ConnectionFingerprintService.MaxFingerprints, all.Count);
        Assert.True(
            allocatedBytes < 48 * 1024,
            $"Expected direct dictionary query allocation below 48 KiB, got {allocatedBytes:N0} bytes.");
    }

    private static ConnectionFingerprintService CreateService()
    {
        return new ConnectionFingerprintService(NullLogger<ConnectionFingerprintService>.Instance);
    }
}
