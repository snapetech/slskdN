// <copyright file="CalculateChunksFixedTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource;

using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Transfers.MultiSource;
using Soulseek;
using Xunit;

public class CalculateChunksFixedTests
{
    private static MultiSourceDownloadService CreateService()
        => new(
            NullLogger<MultiSourceDownloadService>.Instance,
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IContentVerificationService>());

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void NonPositiveChunkSize_FallsBackToDefaultInsteadOfLoopingForever(long chunkSize)
    {
        var service = CreateService();

        // A non-positive chunk size previously left the loop offset unchanged, spinning
        // forever and growing the list until OOM. It must instead produce a finite,
        // fully-covering chunk set using the default chunk size.
        var chunks = service.CalculateChunksFixed(fileSize: 3_000_000, chunkSize);

        Assert.NotEmpty(chunks);
        Assert.Equal(0, chunks[0].StartOffset);
        Assert.Equal(3_000_000, chunks[^1].EndOffset);

        // Chunks are contiguous, non-empty, and cover the whole file exactly once.
        for (var i = 0; i < chunks.Count; i++)
        {
            Assert.True(chunks[i].EndOffset > chunks[i].StartOffset);
            if (i > 0)
            {
                Assert.Equal(chunks[i - 1].EndOffset, chunks[i].StartOffset);
            }
        }
    }

    [Fact]
    public void PositiveChunkSize_ProducesExpectedContiguousChunks()
    {
        var service = CreateService();

        var chunks = service.CalculateChunksFixed(fileSize: 250, chunkSize: 100);

        Assert.Equal(3, chunks.Count);
        Assert.Equal((0, 0L, 100L), chunks[0]);
        Assert.Equal((1, 100L, 200L), chunks[1]);
        Assert.Equal((2, 200L, 250L), chunks[2]);
    }

    [Fact]
    public void ZeroFileSize_ProducesNoChunks()
    {
        var service = CreateService();

        Assert.Empty(service.CalculateChunksFixed(fileSize: 0, chunkSize: 100));
    }
}
