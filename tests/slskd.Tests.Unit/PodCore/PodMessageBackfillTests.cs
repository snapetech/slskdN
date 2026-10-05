// <copyright file="PodMessageBackfillTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Identity;
using slskd.Mesh.Overlay;
using slskd.PodCore;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class PodMessageBackfillTests
{
    private const string ValidPodId = "pod:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task ProcessBackfillResponse_WhenStorageThrows_EscapesPeerExceptionWithoutAttachingIt()
    {
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.StoreMessageAsync(
                ValidPodId,
                "general",
                It.IsAny<PodMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("peer storage failure\r\nforged"));
        var logger = new CapturingLogger<PodMessageBackfill>();
        var backfill = CreateBackfill(storage.Object, logger);
        var response = CreateResponse();

        var result = await backfill.ProcessBackfillResponseAsync(
            ValidPodId,
            "peer\r\nforged",
            response,
            CancellationToken.None);

        Assert.False(result.Success);
        var entry = Assert.Single(logger.Entries, item => item.Level == LogLevel.Error);
        Assert.Contains("peer\\r\\nforged", entry.Message);
        Assert.Contains("peer storage failure\\r\\nforged", entry.Message);
        Assert.DoesNotContain("\r", entry.Message);
        Assert.DoesNotContain("\n", entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task SyncOnRejoin_WhenCallerCancelsStorageRead_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.GetMessagesAsync(
                ValidPodId,
                "general",
                It.IsAny<long?>(),
                It.IsAny<int>(),
                cancellation.Token))
            .Returns(Task.FromCanceled<IReadOnlyList<PodMessage>>(cancellation.Token));
        var backfill = CreateBackfill(storage.Object, NullLogger<PodMessageBackfill>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backfill.SyncOnRejoinAsync(
            ValidPodId,
            new Dictionary<string, long> { ["general"] = 1 },
            cancellation.Token));
    }

    [Fact]
    public async Task HandleBackfillRequest_WhenCallerCancelsStorageRead_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.GetMessagesAsync(
                ValidPodId,
                "general",
                It.IsAny<long?>(),
                It.IsAny<int>(),
                cancellation.Token))
            .Returns(Task.FromCanceled<IReadOnlyList<PodMessage>>(cancellation.Token));
        var backfill = CreateBackfill(storage.Object, NullLogger<PodMessageBackfill>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backfill.HandleBackfillRequestAsync(
            ValidPodId,
            "peer-1",
            new Dictionary<string, MessageRange> { ["general"] = new(1, 2, 1) },
            cancellation.Token));
    }

    [Fact]
    public async Task ProcessBackfillResponse_WhenCallerCancelsStorageWrite_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var storage = new Mock<IPodMessageStorage>();
        storage
            .Setup(service => service.StoreMessageAsync(
                ValidPodId,
                "general",
                It.IsAny<PodMessage>(),
                cancellation.Token))
            .Returns(Task.FromCanceled<bool>(cancellation.Token));
        var backfill = CreateBackfill(storage.Object, NullLogger<PodMessageBackfill>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backfill.ProcessBackfillResponseAsync(
            ValidPodId,
            "peer-1",
            CreateResponse(),
            cancellation.Token));
    }

    private static PodMessageBackfill CreateBackfill(
        IPodMessageStorage storage,
        ILogger<PodMessageBackfill> logger)
        => new(
            storage,
            Mock.Of<IPodMessageRouter>(),
            Mock.Of<IOverlayClient>(),
            Mock.Of<IPodService>(),
            Mock.Of<IProfileService>(),
            logger);

    private static PodBackfillResponse CreateResponse()
        => new(
            ValidPodId,
            "peer-1",
            new Dictionary<string, IReadOnlyList<PodMessage>>
            {
                ["general"] = new[]
                {
                    new PodMessage
                    {
                        MessageId = "message-1",
                        PodId = ValidPodId,
                        ChannelId = "general",
                        SenderPeerId = "peer-1",
                        Body = "hello",
                        TimestampUnixMs = 1,
                    },
                },
            },
            false,
            DateTimeOffset.UtcNow);
}
