// <copyright file="SceneChatServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Scenes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using slskd.VirtualSoulfind.Scenes;
using Xunit;

public class SceneChatServiceTests
{
    [Fact]
    public void Dispose_UnsubscribesPubSubMessages()
    {
        var pubsub = new Mock<IScenePubSubService>();
        var service = new SceneChatService(
            NullLogger<SceneChatService>.Instance,
            pubsub.Object,
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            Mock.Of<slskd.Identity.IProfileService>());

        service.Dispose();

        pubsub.VerifyRemove(x => x.MessageReceived -= It.IsAny<EventHandler<SceneMessageReceivedEventArgs>>(), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_EscapesSceneIdAndDoesNotLogMessageContent()
    {
        var logger = new CapturingLogger<SceneChatService>();
        var pubsub = new Mock<IScenePubSubService>();
        var options = new Mock<IOptionsMonitor<slskd.Options>>();
        options.SetupGet(monitor => monitor.CurrentValue).Returns(new slskd.Options
        {
            VirtualSoulfind = new slskd.Core.VirtualSoulfindOptions
            {
                Scenes = new slskd.Core.ScenesOptions
                {
                    EnableChat = true,
                },
            },
        });
        var profileService = new Mock<slskd.Identity.IProfileService>();
        profileService
            .Setup(service => service.GetMyProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new slskd.Identity.PeerProfile { PeerId = "local-peer" });

        var service = new SceneChatService(logger, pubsub.Object, options.Object, profileService.Object);
        const string messageContent = "private chat body";

        await service.SendMessageAsync("scene:\r\nforged", messageContent, CancellationToken.None);

        var formattedLogs = string.Join("\n", logger.Messages);
        Assert.Contains("scene:\\r\\nforged", formattedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("\r\n", formattedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(messageContent, formattedLogs, StringComparison.Ordinal);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
