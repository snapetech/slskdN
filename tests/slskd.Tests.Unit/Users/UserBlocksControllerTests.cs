// <copyright file="UserBlocksControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Users;

using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.Users.Notes;
using slskd.Users.Notes.API;
using Xunit;

public sealed class UserBlocksControllerTests
{
    [Fact]
    public async Task GetAllReturnsDurableBlocks()
    {
        var service = new Mock<IUserBlockService>();
        var blocks = new[] { new UserBlock { Username = "peer" } };
        service
            .Setup(blockService => blockService.GetAllBlocksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocks);
        var controller = new UserBlocksController(service.Object);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(blocks, ok.Value);
    }

    [Fact]
    public async Task BlockTrimsUsernameBeforeDispatch()
    {
        var service = new Mock<IUserBlockService>();
        service
            .Setup(blockService => blockService.BlockAsync("peer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserBlock { Username = "peer" });
        var controller = new UserBlocksController(service.Object);

        await controller.Block(" peer ", CancellationToken.None);

        service.Verify(
            blockService => blockService.BlockAsync("peer", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnblockWithBlankUsernameReturnsBadRequest()
    {
        var controller = new UserBlocksController(Mock.Of<IUserBlockService>());

        var result = await controller.Unblock("   ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
