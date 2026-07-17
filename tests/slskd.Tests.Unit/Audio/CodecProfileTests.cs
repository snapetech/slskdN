// <copyright file="CodecProfileTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Audio;

using slskd.Audio;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public sealed class CodecProfileTests
{
    [Theory]
    [InlineData("FLAC", 96_000, 24, 2)]
    [InlineData("ALAC", 48_000, 16, 6)]
    [InlineData("MP3", 44_100, null, 2)]
    [InlineData("AAC", 48_000, 24, 1)]
    [InlineData("unknown", 22_050, null, 1)]
    public void BuildKey_MatchesMaterializedProfile(string codec, int sampleRate, int? bitDepth, int channels)
    {
        var variant = new AudioVariant
        {
            Codec = codec,
            SampleRateHz = sampleRate,
            BitDepth = bitDepth,
            Channels = channels,
        };

        Assert.Equal(CodecProfile.FromVariant(variant).ToKey(), CodecProfile.BuildKey(variant));
    }

    [Fact]
    public void BuildAndMatchKey_PreserveCurrentCultureNumberFormatting()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "minus";
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
            var variant = new AudioVariant
            {
                Codec = "FLAC",
                SampleRateHz = -96_000,
                BitDepth = -24,
                Channels = -2,
            };
            var expected = CodecProfile.FromVariant(variant).ToKey();

            Assert.Equal("FLAC-minus24bit-minus96000Hz-minus2ch", expected);
            Assert.Equal(expected, CodecProfile.BuildKey(variant));
            Assert.True(CodecProfile.MatchesKey(variant, expected));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void BuildKey_RepeatedCallsHaveBoundedAllocation()
    {
        const int iterations = 10_000;
        var variant = new AudioVariant
        {
            Codec = "FLAC",
            SampleRateHz = 96_000,
            BitDepth = 24,
            Channels = 2,
        };
        _ = CodecProfile.BuildKey(variant);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        string? key = null;
        for (var index = 0; index < iterations; index++)
        {
            key = CodecProfile.BuildKey(variant);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal("FLAC-24bit-96000Hz-2ch", key);
        Assert.True(
            allocatedBytes < 900_000,
            $"Expected direct codec-profile key allocation below 900 KB, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public void MatchesKey_RepeatedComparisonsHaveBoundedAllocation()
    {
        const int iterations = 10_000;
        var variant = new AudioVariant
        {
            Codec = "FLAC",
            SampleRateHz = 96_000,
            BitDepth = 24,
            Channels = 2,
        };
        const string key = "FLAC-24bit-96000Hz-2ch";
        _ = CodecProfile.MatchesKey(variant, key);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var matches = true;
        for (var index = 0; index < iterations; index++)
        {
            matches &= CodecProfile.MatchesKey(variant, key);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(matches);
        Assert.True(
            allocatedBytes < 1_024,
            $"Expected span-based codec-profile matching below 1 KiB, got {allocatedBytes:N0} bytes.");
    }
}
