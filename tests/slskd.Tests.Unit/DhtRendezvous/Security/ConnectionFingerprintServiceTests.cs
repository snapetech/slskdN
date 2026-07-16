// <copyright file="ConnectionFingerprintServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous.Security;

using System;
using System.Net;
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
}
