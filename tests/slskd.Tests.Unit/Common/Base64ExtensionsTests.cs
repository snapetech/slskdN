// <copyright file="Base64ExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common;

using System;
using System.Text;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class Base64ExtensionsTests
{
    [Fact]
    public void ToBase64_RepeatedTypicalValueBoundsAllocation()
    {
        const string value = "relay/artist/track É.mp3";
        _ = value.ToBase64();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = value.ToBase64();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("cmVsYXkvYXJ0aXN0L3RyYWNrIMOJLm1wMw==", result);
        Assert.InRange(allocated, 0, 10_500_000);
    }

    [Fact]
    public void FromBase64_RepeatedTypicalValueBoundsAllocation()
    {
        const string value = "cmVsYXkvYXJ0aXN0L3RyYWNrIMOJLm1wMw==";
        _ = value.FromBase64();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = value.FromBase64();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("relay/artist/track É.mp3", result);
        Assert.InRange(allocated, 0, 8_000_000);
    }

    [Fact]
    public void Base64Extensions_MatchFrameworkAcrossUtf8Inputs()
    {
        var values = new[]
        {
            string.Empty,
            "plain ASCII",
            "Éclair 🎵",
            "malformed \ud800 tail",
            new string('界', 600) + "🎵",
        };

        foreach (var value in values)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(value);
            var expectedEncoded = Convert.ToBase64String(expectedBytes);
            var expectedDecoded = Encoding.UTF8.GetString(expectedBytes);

            Assert.Equal(expectedEncoded, value.ToBase64());
            Assert.Equal(expectedDecoded, expectedEncoded.FromBase64());
        }
    }

    [Fact]
    public void FromBase64_PreservesWhitespaceInvalidUtf8AndMalformedContracts()
    {
        Assert.Equal("a", " YQ== \r\n".FromBase64());
        Assert.Equal(Encoding.UTF8.GetString(new byte[] { 0xFF }), "/w==".FromBase64());
        Assert.Throws<FormatException>(() => "not-valid-base64!!!".FromBase64());
        Assert.Throws<ArgumentNullException>(() => ((string)null!).FromBase64());
    }
}
