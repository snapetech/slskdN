// <copyright file="SmallWorldNeighborServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class SmallWorldNeighborServiceTests
{
    [Fact]
    public void NeighborLogs_EscapeUsernameWithoutChangingStoredIdentity()
    {
        const string username = "peer-1\r\nforged warning";
        var logger = new CapturingLogger<SmallWorldNeighborService>();
        var service = new SmallWorldNeighborService(logger, Mock.Of<IMeshSyncService>());

        service.AddNeighbor(username);
        service.RecordInteraction(username, default);

        Assert.Equal(username, Assert.Single(service.GetNeighbors()));
        Assert.Equal(2, logger.Entries.Count);
        foreach (var entry in logger.Entries)
        {
            Assert.Contains("peer-1\\r\\nforged warning", entry.Message);
            Assert.DoesNotContain('\r', entry.Message);
            Assert.DoesNotContain('\n', entry.Message);
        }
    }
}
