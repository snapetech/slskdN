// <copyright file="DhtKeyDerivationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.ShadowIndex;

using System;
using System.Security.Cryptography;
using System.Text;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

public class DhtKeyDerivationTests
{
    [Theory]
    [InlineData("release", "slskdn-vsf-mbid-release-v1", "release-id")]
    [InlineData("recording", "slskdn-vsf-mbid-recording-v1", "recording-id")]
    [InlineData("artist", "slskdn-vsf-mbid-artist-v1", "artist-id")]
    [InlineData("scene", "slskdn-vsf-scene-v1", "scene-id")]
    [InlineData("scene-members", "slskdn-vsf-scene-members-v1", "scene-id")]
    public void DeriveKey_PreservesFrozenNamespaceContract(string keyType, string keyNamespace, string id)
    {
        var expected = SHA1.HashData(Encoding.UTF8.GetBytes($"{keyNamespace}:{id}"));

        var actual = keyType switch
        {
            "release" => DhtKeyDerivation.DeriveReleaseKey(id),
            "recording" => DhtKeyDerivation.DeriveRecordingKey(id),
            "artist" => DhtKeyDerivation.DeriveArtistKey(id),
            "scene" => DhtKeyDerivation.DeriveSceneKey(id),
            "scene-members" => DhtKeyDerivation.DeriveSceneMembersKey(id),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType)),
        };

        Assert.Equal(expected, actual);
        Assert.Equal(20, actual.Length);
    }

    [Fact]
    public void DeriveSceneKey_LongUnicodeIdPreservesHash()
    {
        var id = new string('\u00c9', 600);
        var expected = SHA1.HashData(Encoding.UTF8.GetBytes($"slskdn-vsf-scene-v1:{id}"));

        var actual = DhtKeyDerivation.DeriveSceneKey(id);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToHexString_ReturnsLowercaseHex()
    {
        Assert.Equal("00abcdef", DhtKeyDerivation.ToHexString([0x00, 0xab, 0xcd, 0xef]));
    }

    [Fact]
    public void DeriveRecordingKey_RepeatedTypicalIdBoundsAllocation()
    {
        const string recordingId = "12345678-1234-1234-1234-123456789abc";
        var expected = DhtKeyDerivation.DeriveRecordingKey(recordingId);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        byte[]? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = DhtKeyDerivation.DeriveRecordingKey(recordingId);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(expected, result);
        Assert.InRange(allocated, 0, 6_000_000);
    }
}
