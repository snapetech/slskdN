// <copyright file="ContentSafetyTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using slskd.Common.Security;
using Xunit;

public class ContentSafetyTests
{
    [Theory]
    [InlineData(".flac", new byte[] { 0x66, 0x4C, 0x61, 0x43, 0x00 }, "FLAC audio")]
    [InlineData(".mp3", new byte[] { 0x49, 0x44, 0x33, 0x04, 0x00 }, "MP3 ID3v2 tag")]
    [InlineData(".ogg", new byte[] { 0x4F, 0x67, 0x67, 0x53, 0x00 }, "Ogg container")]
    [InlineData(".m4a", new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, "M4A ftyp")]
    public void VerifyHeader_AcceptsKnownMatchingMediaSignatures(string extension, byte[] header, string expectedDetectedType)
    {
        var result = ContentSafety.VerifyHeader(header, extension);

        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
        Assert.Equal(ContentThreatLevel.Safe, result.ThreatLevel);
        Assert.Equal(expectedDetectedType, result.DetectedType);
    }

    [Theory]
    [InlineData(".mp3", new byte[] { 0x4D, 0x5A, 0x90, 0x00 }, "PE/DOS executable")]
    [InlineData(".flac", new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, "ELF executable")]
    [InlineData(".ogg", new byte[] { 0x23, 0x21, 0x2F, 0x62 }, "Shell script (#!)")]
    public void VerifyHeader_FailsExecutableContentMasqueradingAsMedia(string extension, byte[] header, string expectedDetectedType)
    {
        var result = ContentSafety.VerifyHeader(header, extension);

        Assert.False(result.IsValid);
        Assert.False(result.IsWarning);
        Assert.Equal(ContentThreatLevel.Dangerous, result.ThreatLevel);
        Assert.Equal(expectedDetectedType, result.DetectedType);
    }

    [Fact]
    public void VerifyHeader_WarnsWhenKnownExtensionDoesNotMatchSignature()
    {
        var result = ContentSafety.VerifyHeader(new byte[] { 0x25, 0x50, 0x44, 0x46 }, ".flac");

        Assert.True(result.IsValid);
        Assert.True(result.IsWarning);
        Assert.Equal(ContentThreatLevel.Mismatch, result.ThreatLevel);
    }

    [Fact]
    public void VerifyHeader_AllowsUnknownExtensionWithoutPretendingItWasVerified()
    {
        var result = ContentSafety.VerifyHeader(new byte[] { 0x01, 0x02, 0x03, 0x04 }, ".unknown");

        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
        Assert.Equal(ContentThreatLevel.Safe, result.ThreatLevel);
        Assert.Equal("Unknown format", result.DetectedType);
    }

    [Fact]
    public void VerifyHeader_FailsTooShortHeaders()
    {
        var result = ContentSafety.VerifyHeader(new byte[] { 0x01 }, ".mp3");

        Assert.False(result.IsValid);
        Assert.Equal(ContentThreatLevel.Unknown, result.ThreatLevel);
    }

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A }, true)]
    [InlineData(new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, true)]
    [InlineData(new byte[] { 0x66, 0x4C, 0x61, 0x43 }, false)]
    public void IsExecutable_DetectsExecutableHeaders(byte[] header, bool expected)
    {
        Assert.Equal(expected, ContentSafety.IsExecutable(header));
    }

    [Fact]
    public void DetectFileType_ReturnsDetectedSignatureDescription()
    {
        var detected = ContentSafety.DetectFileType(new byte[] { 0x66, 0x4C, 0x61, 0x43 });

        Assert.Equal("FLAC audio", detected);
    }
}
