// <copyright file="TrafficAccountingServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Metrics
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using slskd.HashDb;
    using slskd.HashDb.Models;
    using slskd.Transfers.MultiSource.Metrics;
    using Xunit;

    public class TrafficAccountingServiceTests
    {
        [Fact]
        public async Task EvaluateAsync_IncludesActiveSoulseekWrites_AndCommitDoesNotDoubleCount()
        {
            long persistedSoulseekUploadBytes = 0;
            var databaseWriteStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var allowDatabaseWrite = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetTrafficTotalsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new TrafficTotals
                {
                    OverlayUploadBytes = 400,
                    OverlayDownloadBytes = 100,
                    SoulseekUploadBytes = Interlocked.Read(ref persistedSoulseekUploadBytes),
                });
            hashDb.Setup(database => database.AddTrafficAsync(
                    0,
                    0,
                    It.IsAny<long>(),
                    0,
                    It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    databaseWriteStarted.TrySetResult(true);
                    await allowDatabaseWrite.Task.ConfigureAwait(false);
                    Interlocked.Add(ref persistedSoulseekUploadBytes, 200);
                });

            using var accounting = new TrafficAccountingService(hashDb.Object, NullLogger<TrafficAccountingService>.Instance);
            var fairness = new FairnessGuard(accounting);

            var deniedWithoutReciprocalTraffic = await fairness.EvaluateAsync();
            Assert.False(deniedWithoutReciprocalTraffic.Allowed);

            accounting.RecordSoulseekUploadProgress(200);
            var allowedDuringUpload = await fairness.EvaluateAsync();
            Assert.True(allowedDuringUpload.Allowed);
            Assert.Equal(200, allowedDuringUpload.Totals.SoulseekUploadBytes);

            var commit = accounting.CommitSoulseekUploadAsync(200);
            Task<TrafficTotals>? readDuringCommit = null;
            try
            {
                await databaseWriteStarted.Task.WaitAsync(System.TimeSpan.FromSeconds(5));
                readDuringCommit = accounting.GetTotalsAsync();
                Assert.False(readDuringCommit.IsCompleted);
            }
            finally
            {
                allowDatabaseWrite.TrySetResult(true);
            }

            await commit;
            var afterCommit = await (readDuringCommit ?? accounting.GetTotalsAsync());
            Assert.Equal(200, afterCommit.SoulseekUploadBytes);

            var allowedAfterCommit = await fairness.EvaluateAsync();
            Assert.True(allowedAfterCommit.Allowed);
            Assert.Equal(200, allowedAfterCommit.Totals.SoulseekUploadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(0, 0, 200, 0, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SoulseekDownloadProgress_CountsPayloadAndTerminalRemainderOnce()
        {
            long persistedSoulseekDownloadBytes = 0;
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetTrafficTotalsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new TrafficTotals
                {
                    SoulseekDownloadBytes = Interlocked.Read(ref persistedSoulseekDownloadBytes),
                });
            hashDb.Setup(database => database.AddTrafficAsync(
                    0,
                    0,
                    0,
                    100,
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Interlocked.Add(ref persistedSoulseekDownloadBytes, 100);
                    return Task.CompletedTask;
                });

            using var accounting = new TrafficAccountingService(hashDb.Object, NullLogger<TrafficAccountingService>.Instance);

            accounting.RecordSoulseekDownloadProgress(17, bytesTransferred: 100, startOffset: 100);
            accounting.RecordSoulseekDownloadProgress(17, bytesTransferred: 150, startOffset: 100);
            accounting.RecordSoulseekDownloadProgress(17, bytesTransferred: 150, startOffset: 100);
            accounting.RecordSoulseekDownloadProgress(17, bytesTransferred: 140, startOffset: 100);
            accounting.RecordSoulseekDownloadProgress(17, bytesTransferred: 175, startOffset: 100);

            var activeTotals = await accounting.GetTotalsAsync();
            Assert.Equal(75, activeTotals.SoulseekDownloadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()), Times.Never);

            var commit = accounting.CommitSoulseekDownloadAsync(17, bytesTransferred: 200, startOffset: 100);
            var terminalTotals = await accounting.GetTotalsAsync();
            Assert.Equal(100, terminalTotals.SoulseekDownloadBytes);
            await commit;

            var completedTotals = await accounting.GetTotalsAsync();
            Assert.Equal(100, completedTotals.SoulseekDownloadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(0, 0, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CommitSoulseekDownloadAsync_WhenPersistenceFails_KeepsPendingBytesVisible()
        {
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetTrafficTotalsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TrafficTotals());
            hashDb.Setup(database => database.AddTrafficAsync(
                    0,
                    0,
                    0,
                    90,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.InvalidOperationException("database unavailable"));

            using var accounting = new TrafficAccountingService(hashDb.Object, NullLogger<TrafficAccountingService>.Instance);
            accounting.RecordSoulseekDownloadProgress(23, bytesTransferred: 80, startOffset: 0);

            await accounting.CommitSoulseekDownloadAsync(23, bytesTransferred: 90, startOffset: 0);

            var totals = await accounting.GetTotalsAsync();
            Assert.Equal(90, totals.SoulseekDownloadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(0, 0, 0, 90, It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task CommitSoulseekDownloadAsync_CoalescesNearbyChunkCompletions()
        {
            const long ChunkBytes = 512 * 1024;
            const long BatchBytes = ChunkBytes * 2;
            long persistedSoulseekDownloadBytes = 0;
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetTrafficTotalsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new TrafficTotals
                {
                    SoulseekDownloadBytes = Interlocked.Read(ref persistedSoulseekDownloadBytes),
                });
            hashDb.Setup(database => database.AddTrafficAsync(
                    0,
                    0,
                    0,
                    BatchBytes,
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Interlocked.Add(ref persistedSoulseekDownloadBytes, BatchBytes);
                    return Task.CompletedTask;
                });

            using var accounting = new TrafficAccountingService(hashDb.Object, NullLogger<TrafficAccountingService>.Instance);

            var firstChunk = accounting.CommitSoulseekDownloadAsync(31, ChunkBytes, startOffset: 0);
            var secondChunk = accounting.CommitSoulseekDownloadAsync(32, ChunkBytes, startOffset: 0);

            await Task.WhenAll(firstChunk, secondChunk);

            var totals = await accounting.GetTotalsAsync();
            Assert.Equal(BatchBytes, totals.SoulseekDownloadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(0, 0, 0, BatchBytes, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CommitSoulseekDownloadAsync_SerializesTotalsReadAcrossPersistenceHandoff()
        {
            long persistedSoulseekDownloadBytes = 0;
            var databaseWriteStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var allowDatabaseWrite = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetTrafficTotalsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new TrafficTotals
                {
                    SoulseekDownloadBytes = Interlocked.Read(ref persistedSoulseekDownloadBytes),
                });
            hashDb.Setup(database => database.AddTrafficAsync(
                    0,
                    0,
                    0,
                    100,
                    It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    databaseWriteStarted.TrySetResult(true);
                    await allowDatabaseWrite.Task.ConfigureAwait(false);
                    Interlocked.Add(ref persistedSoulseekDownloadBytes, 100);
                });

            using var accounting = new TrafficAccountingService(hashDb.Object, NullLogger<TrafficAccountingService>.Instance);
            accounting.RecordSoulseekDownloadProgress(29, bytesTransferred: 80, startOffset: 0);

            var commit = accounting.CommitSoulseekDownloadAsync(29, bytesTransferred: 100, startOffset: 0);
            Task<TrafficTotals>? readDuringCommit = null;
            try
            {
                await databaseWriteStarted.Task.WaitAsync(System.TimeSpan.FromSeconds(5));
                readDuringCommit = accounting.GetTotalsAsync();
                Assert.False(readDuringCommit.IsCompleted);
            }
            finally
            {
                allowDatabaseWrite.TrySetResult(true);
            }

            await commit;
            var totals = await (readDuringCommit ?? accounting.GetTotalsAsync());
            Assert.Equal(100, totals.SoulseekDownloadBytes);
            hashDb.Verify(database => database.AddTrafficAsync(0, 0, 0, 100, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
