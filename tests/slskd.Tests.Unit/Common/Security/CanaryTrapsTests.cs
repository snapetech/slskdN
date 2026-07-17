// <copyright file="CanaryTrapsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Common.Security;
using Xunit;

public class CanaryTrapsTests
{
    [Fact]
    public void GenerateWatermarkBytes_AvoidsPerBlockArrays()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        _ = canary.GenerateWatermarkBytes("benchmark", "track.flac", 4096);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        byte[]? watermark = null;
        for (var iteration = 0; iteration < 1_000; iteration++)
        {
            watermark = canary.GenerateWatermarkBytes("benchmark", "track.flac", 4096);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(4096, watermark!.Length);
        Assert.InRange(allocated, 0, 6_000_000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(65)]
    public void GenerateWatermarkBytes_ReturnsRequestedLength(int length)
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        var watermark = canary.GenerateWatermarkBytes("peer", "track.flac", length);

        Assert.Equal(length, watermark.Length);
    }

    [Fact]
    public void GenerateWatermarkBytes_DefaultCallsAreUnique()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        var first = canary.GenerateWatermarkBytes("peer", "track.flac");
        var second = canary.GenerateWatermarkBytes("peer", "track.flac");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GenerateWatermarkBytes_RejectsNegativeLength()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        Assert.Throws<ArgumentOutOfRangeException>(() => canary.GenerateWatermarkBytes("peer", "track.flac", -1));
    }

    [Fact]
    public void GenerateCanary_AvoidsHmacAndHexFormattingIntermediates()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        _ = canary.GenerateCanary("benchmark", "track.flac");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        CanaryResult? result = null;
        for (var iteration = 0; iteration < 100_000; iteration++)
        {
            result = canary.GenerateCanary("benchmark", "track.flac");
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.NotNull(result);
        Assert.Same(result!.Record, canary.LookupCanary(result.CanaryId));
        Assert.InRange(allocated, 0, 70_000_000);
    }

    [Fact]
    public void GenerateCanary_ProducesLowercaseIdFromFullHash()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        var result = canary.GenerateCanary("peer", "track.flac");

        Assert.Equal(16, result.CanaryId.Length);
        Assert.Equal(64, result.Record.FullHash.Length);
        Assert.StartsWith(result.CanaryId, result.Record.FullHash, StringComparison.Ordinal);
        Assert.Equal(result.CanaryId.ToLowerInvariant(), result.CanaryId);
        Assert.Equal(result.Record.FullHash.ToLowerInvariant(), result.Record.FullHash);
    }

    [Fact]
    public void GenerateInvisibleSuffix_AvoidsPerNibbleStringsAndBuilderGrowth()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);
        var expected = canary.GenerateInvisibleSuffix("0123456789abcdef");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? encoded = null;
        for (var iteration = 0; iteration < 100_000; iteration++)
        {
            encoded = canary.GenerateInvisibleSuffix("0123456789abcdef");
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(expected, encoded);
        Assert.InRange(allocated, 0, 20_000_000);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0123456789abcdef")]
    [InlineData("ABCDEF")]
    public void InvisibleSuffix_RoundTripsAsLowercaseHex(string canaryId)
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        var decoded = canary.DecodeInvisibleSuffix(canary.GenerateInvisibleSuffix(canaryId));

        Assert.Equal(canaryId.ToLowerInvariant(), decoded);
    }

    [Fact]
    public void GenerateInvisibleSuffix_EmptyIdReturnsEmptySuffix()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        Assert.Equal(string.Empty, canary.GenerateInvisibleSuffix(string.Empty));
    }

    [Fact]
    public void GenerateInvisibleSuffix_RejectsNonHexCharacters()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        Assert.Throws<FormatException>(() => canary.GenerateInvisibleSuffix("g"));
    }

    [Fact]
    public void DecodeInvisibleSuffix_IgnoresNonCanaryCharacters()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);
        var encoded = canary.GenerateInvisibleSuffix("a5");
        var noisy = $"prefix-{string.Join("noise", encoded)}-suffix";

        Assert.Equal("a5", canary.DecodeInvisibleSuffix(noisy));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("\u200B\u200C\u200B")]
    [InlineData("\u200B\u200C\u200B\u200C\u200C")]
    public void DecodeInvisibleSuffix_RejectsMissingOrIncompleteNibbles(string suffix)
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);

        Assert.Null(canary.DecodeInvisibleSuffix(suffix));
    }

    [Fact]
    public void DecodeInvisibleSuffix_AvoidsFilteredBitAndNibbleStrings()
    {
        var canary = new CanaryTraps(NullLogger<CanaryTraps>.Instance, new byte[32]);
        var suffix = $"prefix-{canary.GenerateInvisibleSuffix("0123456789abcdef")}-suffix";

        _ = canary.DecodeInvisibleSuffix(suffix);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? decoded = null;
        for (var iteration = 0; iteration < 100_000; iteration++)
        {
            decoded = canary.DecodeInvisibleSuffix(suffix);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("0123456789abcdef", decoded);
        Assert.InRange(allocated, 0, 20_000_000);
    }

    [Fact]
    public void GenerateCanary_DoesNotKeepReferenceToProvidedSecret()
    {
        var logger = Mock.Of<ILogger<CanaryTraps>>();
        var secretKey = Enumerable.Range(0, 4).Select(i => (byte)i).ToArray();
        var canary = new CanaryTraps(logger, secretKey);

        secretKey[0] = 0xFF;

        var field = typeof(CanaryTraps).GetField("_secretKey", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var storedSecret = (byte[]?)field!.GetValue(canary);
        Assert.NotNull(storedSecret);
        Assert.NotEqual(secretKey[0], storedSecret![0]);
    }
}
