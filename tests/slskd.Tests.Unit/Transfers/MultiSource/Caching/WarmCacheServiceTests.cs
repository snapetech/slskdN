// <copyright file="WarmCacheServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource.Caching;

using System.Threading;
using System.Threading.Tasks;
using Moq;
using slskd.HashDb;
using slskd.Transfers.MultiSource.Caching;
using Xunit;

public sealed class WarmCacheServiceTests
{
    [Fact]
    public async Task EvictIfNeededAsync_DelegatesCapacityCheckAndEvictionOnce()
    {
        var hashDb = new Mock<IHashDbService>();
        var service = new WarmCacheService(
            hashDb.Object,
            new TestOptionsMonitor<WarmCacheOptions>(new WarmCacheOptions
            {
                Enabled = true,
                MaxStorageGb = 2,
            }));

        await service.EvictIfNeededAsync();

        hashDb.Verify(db => db.EvictWarmCacheEntriesAsync(
            2L * 1024 * 1024 * 1024,
            It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(db => db.GetWarmCacheTotalSizeAsync(It.IsAny<CancellationToken>()), Times.Never);
        hashDb.Verify(db => db.ListWarmCacheEntriesAsync(It.IsAny<CancellationToken>()), Times.Never);
        hashDb.Verify(db => db.DeleteWarmCacheEntryAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
