// <copyright file="ContentPeerHintServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Mesh;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Mesh.Dht;
using Xunit;

public sealed class ContentPeerHintServiceTests
{
    [Fact]
    public async Task PendingDuplicatesArePublishedOnceInOneBatch()
    {
        var published = new TaskCompletionSource<IReadOnlyList<string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var publisher = new Mock<IContentPeerPublisher>();
        publisher.Setup(service => service.PublishBatchAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<string>, TimeSpan, CancellationToken>((contentIds, _, _) =>
                published.TrySetResult(contentIds))
            .Returns(Task.CompletedTask);
        using var service = new ContentPeerHintService(
            NullLogger<ContentPeerHintService>.Instance,
            publisher.Object,
            TimeSpan.Zero,
            batchSize: 32);

        Assert.True(service.Enqueue("content:one"));
        Assert.True(service.Enqueue("content:one"));
        Assert.True(service.Enqueue("content:two"));
        await service.StartAsync(CancellationToken.None);

        var contentIds = await published.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(new[] { "content:one", "content:two" }, contentIds);
        publisher.Verify(candidate => candidate.PublishBatchAsync(
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
