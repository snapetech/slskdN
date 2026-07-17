// <copyright file="ComputeTests.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Xunit;

    [Collection(AllocationTestCollection.Name)]
    public class ComputeTests
    {
        [Fact]
        public void Sha1Hash_RepeatedTypicalInputBoundsAllocation()
        {
            _ = Compute.Sha1Hash("transfer-file-name.mp3");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            string? result = null;
            for (var index = 0; index < 100_000; index++)
            {
                result = Compute.Sha1Hash("transfer-file-name.mp3");
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal("0E5453AC48E3AB17242CCAE2FD63DA7AC19250A7", result);
            Assert.InRange(allocated, 0, 11_500_000);
        }

        [Fact]
        public void Sha256Hash_RepeatedTypicalInputBoundsAllocation()
        {
            _ = Compute.Sha256Hash("/music/artist/album/track.flac|12345678");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            string? result = null;
            for (var index = 0; index < 100_000; index++)
            {
                result = Compute.Sha256Hash("/music/artist/album/track.flac|12345678");
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal("F75ECB7EFD0CC8F31B14912E508BCB1E7064EC933CB52FCBE54A080CD90EC971", result);
            Assert.InRange(allocated, 0, 16_500_000);
        }

        [Fact]
        public void HashHelpers_PreserveKnownUppercaseVectors()
        {
            Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", Compute.Sha1Hash(string.Empty));
            Assert.Equal(
                "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                Compute.Sha256Hash(string.Empty));
        }

        [Fact]
        public void HashHelpers_LongUnicodeInputMatchesFrameworkHashing()
        {
            var input = new string('É', 600) + "🎵\ud800tail";

            var sha1 = Compute.Sha1Hash(input);
            var sha256 = Compute.Sha256Hash(input);

            var bytes = Encoding.UTF8.GetBytes(input);
            Assert.Equal(Convert.ToHexString(SHA1.HashData(bytes)), sha1);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), sha256);
        }

        [Theory]
        [InlineData(1, 300000, 0)]
        [InlineData(2, 300000, 1000)]
        [InlineData(3, 300000, 3000)]
        [InlineData(4, 300000, 7000)]
        [InlineData(5, 300000, 15000)]
        [InlineData(6, 300000, 31000)]
        [InlineData(999999, 300000, 300000)]
        public void ExponentialBackoffDelay(int iteration, int maxDelayInMs, int expectedDelay)
        {
            var (computedDelay, _) = Compute.ExponentialBackoffDelay(iteration, maxDelayInMs);
            Assert.Equal(expectedDelay, computedDelay);
        }
    }
}
