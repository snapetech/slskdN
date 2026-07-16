// <copyright file="IntentQueueProcessorBackgroundServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.v2.Processing;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.VirtualSoulfind.v2.Processing;
using Xunit;

public class IntentQueueProcessorBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithDebugDisabled_DoesNotHydrateStats()
    {
        var secondBatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var batchCalls = 0;
        var processor = new Mock<IIntentQueueProcessor>();
        processor
            .Setup(service => service.ProcessBatchAsync(10, It.IsAny<CancellationToken>()))
            .Returns(async (int _, CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref batchCalls) == 1)
                {
                    return 1;
                }

                secondBatchStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            });
        var logger = new Mock<ILogger<IntentQueueProcessorBackgroundService>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Debug)).Returns(false);
        var service = new IntentQueueProcessorBackgroundService(
            processor.Object,
            new TestOptionsMonitor<IntentQueueProcessorOptions>(new IntentQueueProcessorOptions
            {
                BatchSize = 10,
                Enabled = true,
                ProcessingIntervalSeconds = 0,
                StartupDelaySeconds = 0,
            }),
            logger.Object);

        await service.StartAsync(CancellationToken.None);
        try
        {
            await secondBatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            processor.Verify(value => value.GetStatsAsync(), Times.Never);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithDebugEnabled_HydratesStatsAfterProcessedBatch()
    {
        var statsRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = new Mock<IIntentQueueProcessor>();
        processor
            .Setup(service => service.ProcessBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        processor
            .Setup(service => service.GetStatsAsync())
            .Callback(() => statsRequested.TrySetResult())
            .ReturnsAsync(new IntentProcessorStats());
        var logger = new Mock<ILogger<IntentQueueProcessorBackgroundService>>();
        logger.Setup(value => value.IsEnabled(LogLevel.Debug)).Returns(true);
        var service = new IntentQueueProcessorBackgroundService(
            processor.Object,
            new TestOptionsMonitor<IntentQueueProcessorOptions>(new IntentQueueProcessorOptions
            {
                BatchSize = 10,
                Enabled = true,
                ProcessingIntervalSeconds = 3_600,
                StartupDelaySeconds = 0,
            }),
            logger.Object);

        await service.StartAsync(CancellationToken.None);
        try
        {
            await statsRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            processor.Verify(value => value.GetStatsAsync(), Times.Once);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }
}
