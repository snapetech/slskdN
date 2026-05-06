// <copyright file="TimedBatcherTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Moq;
using slskd.Common.Security;
using System.Reflection;
using System.Threading;
using Xunit;

namespace slskd.Tests.Unit.Common.Security;

public class TimedBatcherTests
{
    [Fact]
    public async Task GetNextBatchAsync_WhenBatchWindowExpires_ReturnsQueuedMessages()
    {
        var logger = Mock.Of<ILogger<TimedBatcher>>();
        var batcher = new TimedBatcher(
            new MessageBatchingOptions
            {
                BatchWindowMs = 100,
                MaxBatchSize = 10,
            },
            logger);

        await batcher.AddMessageAsync(new byte[] { 1, 2, 3 });

        var batch = await batcher.GetNextBatchAsync();

        var message = Assert.Single(batch);
        Assert.Equal(new byte[] { 1, 2, 3 }, message.Data);
    }

    [Fact]
    public async Task AddMessageAsync_WhenBatchReachesMaxSize_DisposesExistingBatchTimer()
    {
        var logger = Mock.Of<ILogger<TimedBatcher>>();
        var batcher = new TimedBatcher(
            new MessageBatchingOptions
            {
                BatchWindowMs = 1000,
                MaxBatchSize = 2,
            },
            logger);

        await batcher.AddMessageAsync(new byte[] { 1 });
        var timer = GetCurrentBatchTimer(batcher);
        Assert.NotNull(timer);

        await batcher.AddMessageAsync(new byte[] { 2 });

        Assert.Null(GetCurrentBatchTimer(batcher));
        Assert.Throws<ObjectDisposedException>(() => _ = timer!.Token);
    }

    [Fact]
    public async Task FlushAsync_DisposesExistingBatchTimer()
    {
        var logger = Mock.Of<ILogger<TimedBatcher>>();
        var batcher = new TimedBatcher(
            new MessageBatchingOptions
            {
                BatchWindowMs = 1000,
                MaxBatchSize = 10,
            },
            logger);

        await batcher.AddMessageAsync(new byte[] { 1 });
        var timer = GetCurrentBatchTimer(batcher);
        Assert.NotNull(timer);

        await batcher.FlushAsync();

        Assert.Null(GetCurrentBatchTimer(batcher));
        Assert.Throws<ObjectDisposedException>(() => _ = timer!.Token);
    }

    [Fact]
    public void BatchedMessage_CopiesMessageData()
    {
        var messageBytes = new byte[] { 1, 2, 3, 4 };
        var message = new BatchedMessage(messageBytes, null, DateTimeOffset.UtcNow);

        messageBytes[0] = 9;

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, message.Data);
    }

    [Fact]
    public void BatchedMessage_DeepCopiesNestedMetadata()
    {
        var nestedBytes = new byte[] { 5, 6, 7 };
        var nestedMap = new Dictionary<string, object>
        {
            ["innerBytes"] = nestedBytes,
            ["label"] = "payload",
        };

        var metadataList = new List<object>
        {
            "first",
            new byte[] { 8, 9 },
            nestedMap
        };

        var metadata = new Dictionary<string, object>
        {
            ["rootBytes"] = new byte[] { 1, 2, 3 },
            ["nested"] = nestedMap,
            ["metadataList"] = metadataList,
            ["name"] = "outer"
        };

        var message = new BatchedMessage(Array.Empty<byte>(), metadata, DateTimeOffset.UtcNow);

        var messageMetadata = message.Metadata;
        var sourceRootBytes = (byte[])metadata["rootBytes"];
        var messageRootBytes = Assert.IsType<byte[]>(messageMetadata["rootBytes"]);
        var messageNested = Assert.IsType<Dictionary<string, object>>(messageMetadata["nested"]);
        var messageNestedBytes = Assert.IsType<byte[]>(messageNested["innerBytes"]);
        var messageList = Assert.IsType<List<object>>(messageMetadata["metadataList"]);
        var messageListBytes = Assert.IsType<byte[]>(messageList[1]);
        var messageListNested = Assert.IsType<Dictionary<string, object>>(messageList[2]);

        nestedBytes[0] = 9;
        metadata["name"] = "mutated";
        metadataList.Add("second");
        nestedMap["innerBytes"] = new byte[] { 11, 12, 13 };
        ((byte[])metadata["rootBytes"])[0] = 9;

        Assert.NotSame(sourceRootBytes, messageRootBytes);
        Assert.Equal(new byte[] { 1, 2, 3 }, messageRootBytes);
        Assert.NotSame(nestedBytes, messageNestedBytes);
        Assert.Equal(new byte[] { 5, 6, 7 }, messageNestedBytes);
        Assert.NotSame(messageListBytes, nestedBytes);
        Assert.Equal(new byte[] { 8, 9 }, messageListBytes);
        Assert.Equal("payload", messageNested["label"]);
        Assert.Equal(3, messageList.Count);
        Assert.NotSame(metadataList, messageList);
        Assert.NotSame(messageNested, messageListNested);
        Assert.Equal("outer", messageMetadata["name"]);
    }

    private static CancellationTokenSource? GetCurrentBatchTimer(TimedBatcher batcher)
    {
        var field = typeof(TimedBatcher).GetField("_currentBatchTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (CancellationTokenSource?)field!.GetValue(batcher);
    }
}
