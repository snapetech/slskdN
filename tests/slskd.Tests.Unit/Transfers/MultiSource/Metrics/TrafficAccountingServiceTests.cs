// <copyright file="TrafficAccountingServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Metrics
{
    using System.Threading;
    using System.Threading.Tasks;
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

            using var accounting = new TrafficAccountingService(hashDb.Object);
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
    }
}
