// <copyright file="DhtRateLimiterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using Microsoft.Extensions.Logging;
using slskd.Mesh.Dht;
using slskd.Mesh.Transport;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class DhtRateLimiterTests
{
    [Fact]
    public void ShouldAllowQuery_WhenLimitIsExceeded_EscapesRemoteFieldsOnlyInLogs()
    {
        const string queryType = "find-node\r\nforged type";
        const string requesterId = "peer-1\r\nforged peer";
        var rateLimiter = new RateLimiter(new CapturingLogger<RateLimiter>());
        var logger = new CapturingLogger<DhtRateLimiter>();
        var dhtRateLimiter = new DhtRateLimiter(rateLimiter, logger);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            Assert.True(dhtRateLimiter.ShouldAllowQuery(queryType, requesterId));
        }

        Assert.False(dhtRateLimiter.ShouldAllowQuery(queryType, requesterId));

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Null(warning.Exception);
        Assert.Contains("find-node\\r\\nforged type", warning.Message);
        Assert.Contains("peer-1\\r\\nforged peer", warning.Message);
        Assert.DoesNotContain('\r', warning.Message);
        Assert.DoesNotContain('\n', warning.Message);
    }

    [Fact]
    public void ReportFailedOperation_WhenLimitIsExceeded_EscapesOperationPeerAndReason()
    {
        const string operation = "read\r\nforged operation";
        const string peer = "peer-1\r\nforged peer";
        const string reason = "timeout\r\nforged reason";
        var rateLimiter = new RateLimiter(new CapturingLogger<RateLimiter>());
        var logger = new CapturingLogger<DhtRateLimiter>();
        var dhtRateLimiter = new DhtRateLimiter(rateLimiter, logger);

        for (var attempt = 0; attempt < 11; attempt++)
        {
            dhtRateLimiter.ReportFailedOperation(operation, peer, reason);
        }

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Null(warning.Exception);
        Assert.Contains("read\\r\\nforged operation", warning.Message);
        Assert.Contains("peer-1\\r\\nforged peer", warning.Message);
        Assert.Contains("timeout\\r\\nforged reason", warning.Message);
        Assert.DoesNotContain('\r', warning.Message);
        Assert.DoesNotContain('\n', warning.Message);
    }

    [Fact]
    public void ReportSuccessfulOperation_EscapesRemoteFieldsAtTheLogBoundary()
    {
        var logger = new CapturingLogger<DhtRateLimiter>();
        var dhtRateLimiter = new DhtRateLimiter(
            new RateLimiter(new CapturingLogger<RateLimiter>()),
            logger);

        dhtRateLimiter.ReportSuccessfulOperation("read\r\nforged operation", "peer-1\r\nforged peer");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("read\\r\\nforged operation", entry.Message);
        Assert.Contains("peer-1\\r\\nforged peer", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
    }
}
