// <copyright file="PeerMetricsServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Metrics
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using slskd.HashDb;
    using slskd.Telemetry;
    using slskd.Transfers.MultiSource.Metrics;
    using Xunit;

    public class PeerMetricsServiceTests
    {
        [Fact]
        public async Task GetMetricsAsync_WhenHashDbHasNoRow_ReturnsNewDefaultMetrics()
        {
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(m => m.GetPeerMetricsAsync("peer-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(PeerPerformanceMetrics));

            var service = new PeerMetricsService(hashDb.Object, NullLogger<PeerMetricsService>.Instance);

            var metrics = await service.GetMetricsAsync("peer-1", PeerSource.Overlay);

            Assert.NotNull(metrics);
            Assert.Equal("peer-1", metrics.PeerId);
            Assert.Equal(PeerSource.Overlay, metrics.Source);
            Assert.Equal(0.5, metrics.ReputationScore);
        }

        [Fact]
        public async Task GetMetricsAsync_BatchesMissingPeersAndReusesCache()
        {
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetPeerMetricsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PeerPerformanceMetrics>
                {
                    new() { PeerId = "persisted", Source = PeerSource.Soulseek, ReputationScore = 0.9 },
                });
            var service = new PeerMetricsService(hashDb.Object, NullLogger<PeerMetricsService>.Instance);
            var requests = new[]
            {
                (PeerId: "persisted", Source: PeerSource.Overlay),
                (PeerId: "new-peer", Source: PeerSource.Overlay),
                (PeerId: "persisted", Source: PeerSource.Overlay),
            };

            var first = await service.GetMetricsAsync(requests);
            var second = await service.GetMetricsAsync(requests);

            Assert.Equal(2, first.Count);
            Assert.Equal(PeerSource.Soulseek, first["persisted"]!.Source);
            Assert.Equal(PeerSource.Overlay, first["new-peer"]!.Source);
            Assert.Same(first["persisted"], second["persisted"]);
            hashDb.Verify(database => database.GetPeerMetricsAsync(
                It.Is<IEnumerable<string>>(peerIds => peerIds.Count() == 2),
                It.IsAny<CancellationToken>()), Times.Once);
            hashDb.Verify(database => database.GetPeerMetricsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetRankedPeersAsync_UsesBoundedDatabaseRankingAndCanonicalReturnOrder()
        {
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(m => m.GetTopPeerMetricsAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PeerPerformanceMetrics>
                {
                    new() { PeerId = "slower", ThroughputAvgBytesPerSec = 1_000_000 },
                    new() { PeerId = "faster", ThroughputAvgBytesPerSec = 2_000_000 },
                });
            var service = new PeerMetricsService(hashDb.Object, NullLogger<PeerMetricsService>.Instance);

            var peers = await service.GetRankedPeersAsync(2);

            Assert.Equal(new[] { "faster", "slower" }, peers.Select(peer => peer.PeerId));
            hashDb.Verify(m => m.GetTopPeerMetricsAsync(2, It.IsAny<CancellationToken>()), Times.Once);
            hashDb.Verify(m => m.GetAllPeerMetricsAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetRankedPeersAsync_NonPositiveLimitSkipsDatabase(int limit)
        {
            var hashDb = new Mock<IHashDbService>();
            var service = new PeerMetricsService(hashDb.Object, NullLogger<PeerMetricsService>.Instance);

            var peers = await service.GetRankedPeersAsync(limit);

            Assert.Empty(peers);
            hashDb.Verify(m => m.GetTopPeerMetricsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RecordPerformanceSamples_ComputesPopulationStandardDeviationOverSlidingWindows()
        {
            var metrics = new PeerPerformanceMetrics
            {
                PeerId = "peer-1",
                Source = PeerSource.Soulseek,
            };
            var hashDb = new Mock<IHashDbService>();
            hashDb.Setup(database => database.GetPeerMetricsAsync("peer-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(metrics);
            hashDb.Setup(database => database.UpsertPeerMetricsAsync(metrics, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var service = new PeerMetricsService(hashDb.Object, NullLogger<PeerMetricsService>.Instance);

            foreach (var rttMs in Enumerable.Range(1, 35))
            {
                await service.RecordRttSampleAsync("peer-1", rttMs);
            }

            await service.RecordThroughputSampleAsync("peer-1", 100, System.TimeSpan.FromSeconds(1));
            await service.RecordThroughputSampleAsync("peer-1", 200, System.TimeSpan.FromSeconds(1));
            await service.RecordThroughputSampleAsync("peer-1", 300, System.TimeSpan.FromSeconds(1));

            Assert.Equal(30, metrics.RecentRttSamples.Count);
            Assert.Equal(6, metrics.RecentRttSamples.Peek().RttMs);
            Assert.Equal(System.Math.Sqrt(899.0 / 12), metrics.RttStdDevMs, 10);
            Assert.Equal(System.Math.Sqrt(20_000.0 / 3), metrics.ThroughputStdDevBytesPerSec, 10);
        }
    }
}
