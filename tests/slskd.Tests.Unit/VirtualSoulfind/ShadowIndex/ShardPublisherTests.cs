// <copyright file="ShardPublisherTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.VirtualSoulfind.ShadowIndex;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Core;
using slskd.HashDb;
using slskd.Mesh.Dht;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

public sealed class ShardPublisherTests
{
    [Fact]
    public async Task GetNextRecordingIdsAsync_AdvancesAndWrapsBoundedPages()
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(null, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a", "b"]);
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync("b", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["c"]);
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a"]);
        var publisher = CreatePublisher(hashDb.Object);

        var first = await publisher.GetNextRecordingIdsAsync(2, CancellationToken.None);
        var second = await publisher.GetNextRecordingIdsAsync(2, CancellationToken.None);

        Assert.Equal(new[] { "a", "b" }, first);
        Assert.Equal(new[] { "c", "a" }, second);
        hashDb.Verify(
            service => service.GetRecordingIdsWithVariantsPageAsync("b", 2, It.IsAny<CancellationToken>()),
            Times.Once);
        hashDb.Verify(
            service => service.GetRecordingIdsWithVariantsPageAsync(null, 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishShardsAsync_UsesBoundedPageInsteadOfFullRecordingSet()
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(null, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a", "b"]);
        var builder = new Mock<IShadowIndexBuilder>();
        builder.Setup(service => service.BuildShardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShadowIndexShard
            {
                Timestamp = DateTimeOffset.UtcNow,
                TTLSeconds = 3600,
            });
        var dht = new Mock<IMeshDhtClient>();
        var publisher = CreatePublisher(hashDb.Object, builder.Object, dht.Object);

        await publisher.PublishShardsAsync(CancellationToken.None);

        hashDb.Verify(
            service => service.GetRecordingIdsWithVariantsPageAsync(null, 2, It.IsAny<CancellationToken>()),
            Times.Once);
        hashDb.Verify(
            service => service.GetRecordingIdsWithVariantsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        builder.Verify(
            service => service.BuildShardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        dht.Verify(
            client => client.PutAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), 3600, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PublishShardsAsync_DoesNotSelectMoreThanImmediateDhtBudget()
    {
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(null, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a", "b", "c"]);
        var builder = new Mock<IShadowIndexBuilder>();
        builder.Setup(service => service.BuildShardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShadowIndexShard { TTLSeconds = 3600 });
        var dht = new Mock<IMeshDhtClient>();
        var rateLimiter = new Mock<IDhtRateLimiter>();
        rateLimiter.Setup(limiter => limiter.TryAcquireAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var publisher = CreatePublisher(
            hashDb.Object,
            builder.Object,
            dht.Object,
            rateLimiter.Object,
            maxShardsPerPublish: 100,
            maxDhtOperationsPerMinute: 3);

        await publisher.PublishShardsAsync(CancellationToken.None);

        hashDb.Verify(
            service => service.GetRecordingIdsWithVariantsPageAsync(null, 3, It.IsAny<CancellationToken>()),
            Times.Once);
        builder.Verify(
            service => service.BuildShardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        dht.Verify(
            client => client.PutAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), 3600, It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task PublishShardsAsync_PropagatesHostCancellationFromCandidatePage()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetRecordingIdsWithVariantsPageAsync(null, 2, cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var publisher = CreatePublisher(hashDb.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => publisher.PublishShardsAsync(cancellation.Token));
    }

    private static ShardPublisher CreatePublisher(
        IHashDbService hashDb,
        IShadowIndexBuilder? builder = null,
        IMeshDhtClient? dht = null,
        IDhtRateLimiter? rateLimiter = null,
        int maxShardsPerPublish = 2,
        int maxDhtOperationsPerMinute = 60)
    {
        var options = new slskd.Options
        {
            VirtualSoulfind = new VirtualSoulfindOptions
            {
                ShadowIndex = new ShadowIndexOptions
                {
                    Enabled = true,
                    MaxShardsPerPublish = maxShardsPerPublish,
                    MaxDhtOperationsPerMinute = maxDhtOperationsPerMinute,
                    ShardTTLHours = 1,
                },
            },
        };

        return new ShardPublisher(
            NullLogger<ShardPublisher>.Instance,
            builder ?? Mock.Of<IShadowIndexBuilder>(),
            dht ?? Mock.Of<IMeshDhtClient>(),
            new TestOptionsMonitor<slskd.Options>(options),
            hashDb,
            rateLimiter);
    }
}
