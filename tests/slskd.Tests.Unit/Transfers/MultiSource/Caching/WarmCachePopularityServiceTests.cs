// <copyright file="WarmCachePopularityServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Caching;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using slskd.HashDb;
using slskd.Transfers.MultiSource.Caching;
using Xunit;

public sealed class WarmCachePopularityServiceTests
{
    [Fact]
    public async Task RecordAccessesAsync_ForwardsOneBatchWhenEnabled()
    {
        var hashDb = new Mock<IHashDbService>();
        var service = new WarmCachePopularityService(
            hashDb.Object,
            new TestOptionsMonitor<WarmCacheOptions>(new WarmCacheOptions { Enabled = true }));
        var contentIds = Enumerable.Range(0, 100).Select(index => $"content-{index}").ToList();

        await service.RecordAccessesAsync(contentIds);

        hashDb.Verify(db => db.IncrementPopularitiesAsync(
            It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(contentIds)),
            It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(db => db.IncrementPopularityAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
