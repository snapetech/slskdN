// <copyright file="ConversationsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Messaging;

using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Messaging;
using slskd.Messaging.API;
using Xunit;

public class ConversationsControllerTests
{
    [Fact]
    public async Task GetByUsername_With_Since_Returns_Only_Newer_Messages()
    {
        var cutoff = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var messages = new[]
        {
            new PrivateMessage { Id = 1, Username = "user-1", Timestamp = cutoff.AddSeconds(-1) },
            new PrivateMessage { Id = 2, Username = "user-1", Timestamp = cutoff.AddSeconds(1) },
        };
        var conversations = new Mock<IConversationService>();
        conversations
            .Setup(x => x.FindAsync("user-1", true, false))
            .ReturnsAsync(new Conversation { Username = "user-1" });
        conversations
            .Setup(x => x.ListMessagesAsync(It.IsAny<Expression<Func<PrivateMessage, bool>>>()))
            .Returns((Expression<Func<PrivateMessage, bool>> expression) =>
                Task.FromResult(messages.Where(expression.Compile())));
        var controller = CreateController(conversations.Object);

        var result = await controller.GetByUsername(
            " user-1 ",
            includeMessages: true,
            since: new DateTimeOffset(cutoff).ToUnixTimeMilliseconds());

        var ok = Assert.IsType<OkObjectResult>(result);
        var conversation = Assert.IsType<Conversation>(ok.Value);
        var message = Assert.Single(conversation.Messages);
        Assert.Equal(2, message.Id);
        conversations.Verify(x => x.FindAsync("user-1", true, false), Times.Once);
    }

    [Fact]
    public async Task GetByUsername_With_Negative_Since_Returns_BadRequest()
    {
        var conversations = new Mock<IConversationService>();
        var controller = CreateController(conversations.Object);

        var result = await controller.GetByUsername("user-1", since: -1);

        Assert.IsType<BadRequestObjectResult>(result);
        conversations.Verify(
            x => x.FindAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

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
