// <copyright file="ConversationsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Messaging;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Messaging;
using slskd.Messaging.API;
using Xunit;

public class ConversationsControllerTests
{
    [Fact]
    public async Task GetUnAcknowledgedActivity_Returns_Service_Value()
    {
        var conversations = new Mock<IConversationService>();
        conversations.Setup(x => x.HasUnAcknowledgedMessagesAsync()).ReturnsAsync(true);
        var controller = CreateController(conversations.Object);

        var result = await controller.GetUnAcknowledgedActivity();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(Assert.IsType<bool>(ok.Value));
        conversations.Verify(x => x.HasUnAcknowledgedMessagesAsync(), Times.Once);
    }

    [Fact]
    public async Task Send_Trims_Username_And_Message_Before_Dispatch()
    {
        var conversations = new Mock<IConversationService>();
        var controller = CreateController(conversations.Object);

        var result = await controller.Send(" user-1 ", " hello ");

        Assert.IsType<StatusCodeResult>(result);
        conversations.Verify(x => x.SendMessageAsync("user-1", "hello"), Times.Once);
    }

    [Fact]
    public async Task Send_With_Blank_Message_Returns_BadRequest()
    {
        var controller = CreateController();

        var result = await controller.Send("user-1", "   ");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Acknowledge_With_NonPositive_Id_Returns_BadRequest()
    {
        var controller = CreateController();

        var result = await controller.Acknowledge("user-1", 0);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Acknowledge_When_Soulseek_Is_Disconnected_Returns_ServiceUnavailable()
    {
        var conversations = new Mock<IConversationService>();
        var controller = CreateController(conversations.Object);

        var result = await controller.Acknowledge("user-1", 1);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
        conversations.Verify(x => x.AcknowledgeMessageAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AcknowledgeAll_When_Soulseek_Is_Disconnected_Returns_ServiceUnavailable()
    {
        var conversations = new Mock<IConversationService>();
        var controller = CreateController(conversations.Object);

        var result = await controller.AcknowledgeAll("user-1");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
        conversations.Verify(x => x.AcknowledgeAsync(It.IsAny<string>()), Times.Never);
    }

    private static ConversationsController CreateController(IConversationService? conversations = null)
    {
        var stateMonitor = new Mock<IStateMonitor<State>>();
        stateMonitor.Setup(x => x.CurrentValue).Returns(new State());

        var messagingService = new Mock<IMessagingService>();
        messagingService.SetupGet(x => x.Conversations).Returns(conversations ?? Mock.Of<IConversationService>());

        var optionsSnapshot = new Mock<IOptionsSnapshot<slskd.Options>>();
        optionsSnapshot.Setup(x => x.Value).Returns(new slskd.Options());

        return new ConversationsController(
            stateMonitor.Object,
            messagingService.Object,
            optionsSnapshot.Object);
    }
}
