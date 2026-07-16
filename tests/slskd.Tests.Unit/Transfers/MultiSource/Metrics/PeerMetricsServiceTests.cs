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
    }
}
