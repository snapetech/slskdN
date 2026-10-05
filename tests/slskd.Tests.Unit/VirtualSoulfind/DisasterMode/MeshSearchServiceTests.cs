// <copyright file="MeshSearchServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.DisasterMode;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.VirtualSoulfind.DisasterMode;
using Xunit;

public class MeshSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_EscapesLogBreakingQueryAndRetainsOriginalQuery()
    {
        var query = "alpha\r\nforged";
        var logger = new Mock<ILogger<MeshSearchService>>();
        logger.Setup(entry => entry.IsEnabled(LogLevel.Information)).Returns(true);
        var service = new MeshSearchService(logger.Object);

        var result = await service.SearchAsync(query);

        Assert.Equal(query, result.Query);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("alpha\\r\\nforged") &&
                    !state.ToString()!.Contains(query)),
                It.IsAny<System.Exception?>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }
}
