// <copyright file="MeshOverlaySearchServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.DhtRendezvous;
using slskd.DhtRendezvous.Search;
using Xunit;

public class MeshOverlaySearchServiceTests
{
    [Fact]
    public async Task SearchAsync_EscapesLogBreakingQueryWhenNoPeersAreAvailable()
    {
        var query = "alpha\r\nforged";
        var logger = new Mock<ILogger<MeshOverlaySearchService>>();
        logger.Setup(entry => entry.IsEnabled(LogLevel.Debug)).Returns(true);
        await using var registry = new MeshNeighborRegistry(NullLogger<MeshNeighborRegistry>.Instance);
        var service = new MeshOverlaySearchService(registry, new MeshOverlayRequestRouter(), logger.Object);

        var results = await service.SearchAsync(query);

        Assert.Empty(results);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("alpha\\r\\nforged") &&
                    !state.ToString()!.Contains(query)),
                It.IsAny<System.Exception?>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }
}
