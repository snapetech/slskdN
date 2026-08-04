// <copyright file="ProbabilisticVerificationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class ProbabilisticVerificationTests
{
    [Fact]
    public async Task SpotCheckFileAsync_MaximumSampleReusesChunkBuffer()
    {
        const int chunkSize = 4096;
        const int chunkCount = 1000;
        var fileBytes = new byte[chunkSize * chunkCount];
        Random.Shared.NextBytes(fileBytes);
        var expectedHashes = new Dictionary<int, string>(chunkCount);
        for (var index = 0; index < chunkCount; index++)
        {
            expectedHashes[index] = Convert.ToHexStringLower(
                SHA256.HashData(fileBytes.AsSpan(index * chunkSize, chunkSize)));
        }

        var filePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(filePath, fileBytes);
            using var verification = new ProbabilisticVerification(
                NullLogger<ProbabilisticVerification>.Instance);

            // Warm the async file-read and hashing operation before measuring steady-state allocation.
            var warmup = await verification.SpotCheckFileAsync(
                filePath,
                chunkSize,
                expectedHashes,
                sampleRate: 1);
            Assert.Equal(verification.MaximumChunksToVerify, warmup.VerifiedChunks);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalAllocatedBytes(precise: true);
            var result = await verification.SpotCheckFileAsync(
                filePath,
                chunkSize,
                expectedHashes,
                sampleRate: 1);

            var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
            Assert.Equal(verification.MaximumChunksToVerify, result.VerifiedChunks);
            Assert.Equal(result.VerifiedChunks, result.PassedChunks);
            Assert.Equal(0, result.FailedChunks);
            Assert.True(result.IsValid);
            Assert.InRange(allocated, 0, 100_000);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
