// <copyright file="BridgeHelpersTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Bridge;

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.VirtualSoulfind.Bridge;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class BridgeHelpersTests
{
    [Fact]
    public void RoomSceneMapper_RepeatedForwardMappingBoundsAllocation()
    {
        var mapper = new RoomSceneMapper(NullLogger<RoomSceneMapper>.Instance);
        _ = mapper.MapRoomToScene("Warp Records");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = mapper.MapRoomToScene("Warp Records");
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("scene:label:warp-records", result);
        Assert.InRange(allocated, 0, 8_000_000);
    }

    [Fact]
    public void RoomSceneMapper_RepeatedReverseMappingBoundsAllocation()
    {
        var mapper = new RoomSceneMapper(NullLogger<RoomSceneMapper>.Instance);
        _ = mapper.MapSceneToRoom("scene:label:warp-records");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = mapper.MapSceneToRoom("scene:label:warp-records");
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("warp records", result);
        Assert.InRange(allocated, 0, 5_500_000);
    }

    [Fact]
    public void RoomSceneMapper_ForwardMappingPreservesKeywordsAndSpaces()
    {
        var mapper = new RoomSceneMapper(NullLogger<RoomSceneMapper>.Instance);
        var cases = new[]
        {
            (Room: "Warp Records", Scene: "scene:label:warp-records"),
            (Room: "MUSIC Hall", Scene: "scene:label:music-hall"),
            (Room: "Indie LABEL", Scene: "scene:label:indie-label"),
            (Room: "Studio Recordings", Scene: "scene:label:studio-recordings"),
            (Room: " Dark  Techno ", Scene: "scene:genre:-dark--techno-"),
            (Room: string.Empty, Scene: "scene:genre:"),
        };

        foreach (var testCase in cases)
        {
            Assert.Equal(testCase.Scene, mapper.MapRoomToScene(testCase.Room));
        }
    }

    [Fact]
    public void RoomSceneMapper_LongUnicodeForwardMappingPreservesLegacyNormalization()
    {
        var roomName = new string('Ä', 600) + " Music Room";
        var expected = $"scene:label:{roomName.ToLowerInvariant().Replace(" ", "-")}";
        var mapper = new RoomSceneMapper(NullLogger<RoomSceneMapper>.Instance);

        var result = mapper.MapRoomToScene(roomName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void RoomSceneMapper_ReverseMappingPreservesSegmentsAndMalformedIdentity()
    {
        var mapper = new RoomSceneMapper(NullLogger<RoomSceneMapper>.Instance);
        var cases = new[]
        {
            (Scene: "scene:label:warp-records", Room: "warp records"),
            (Scene: "scene:genre:dark--techno:ignored", Room: "dark  techno"),
            (Scene: "a::tail", Room: "tail"),
            (Scene: "scene:genre:", Room: string.Empty),
            (Scene: "::🎵-room", Room: "🎵 room"),
        };

        foreach (var testCase in cases)
        {
            Assert.Equal(testCase.Room, mapper.MapSceneToRoom(testCase.Scene));
        }

        const string malformed = "scene:label";
        Assert.Same(malformed, mapper.MapSceneToRoom(malformed));
    }

    [Fact]
    public void FilenameGenerator_RepeatedCommonFilenameBoundsAllocation()
    {
        var generator = new FilenameGenerator(NullLogger<FilenameGenerator>.Instance);
        var variant = new VariantHint { Codec = "FLAC", BitrateKbps = 1411 };
        _ = generator.GenerateFilenameAsync("Artist", "Track", variant, CancellationToken.None).GetAwaiter().GetResult();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        for (var index = 0; index < 100_000; index++)
        {
            result = generator.GenerateFilenameAsync("Artist", "Track", variant, CancellationToken.None).GetAwaiter().GetResult();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal("Artist - Track [FLAC 1411kbps].flac", result);
        Assert.InRange(allocated, 0, 18_000_000);
    }

    [Fact]
    public void FilenameGenerator_InvalidCharactersPreserveLegacyCollapseBehavior()
    {
        var invalidCharacter = Path.GetInvalidFileNameChars()[0];
        var artist = $"{invalidCharacter}{invalidCharacter}Artist{invalidCharacter}{invalidCharacter}";
        var title = $"{invalidCharacter}Track{invalidCharacter}";
        var variant = new VariantHint
        {
            Codec = $"F{invalidCharacter}LAC",
            BitrateKbps = 1411,
        };
        var generator = new FilenameGenerator(NullLogger<FilenameGenerator>.Instance);
        var unsanitized = $"{artist} - {title} [{variant.Codec} {variant.BitrateKbps}kbps].{variant.Codec.ToLowerInvariant()}";
        var expected = string.Join(
            "_",
            unsanitized.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        var result = generator.GenerateFilenameAsync(artist, title, variant, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FilenameGenerator_UnicodeAndNegativeBitratePreserveFormatting()
    {
        var generator = new FilenameGenerator(NullLogger<FilenameGenerator>.Instance);
        var variant = new VariantHint { Codec = "ÄAC", BitrateKbps = -1 };

        var result = generator.GenerateFilenameAsync("Björk", "Jóga", variant, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal("Björk - Jóga [ÄAC -1kbps].äac", result);
    }

    [Fact]
    public void FilenameGenerator_CustomCulturePreservesExpandedNegativeSign()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            var customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            customCulture.NumberFormat.NegativeSign = new string('~', 40);
            CultureInfo.CurrentCulture = customCulture;
            var generator = new FilenameGenerator(NullLogger<FilenameGenerator>.Instance);
            var variant = new VariantHint { Codec = "FLAC", BitrateKbps = -1 };

            var result = generator.GenerateFilenameAsync("Artist", "Track", variant, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal($"Artist - Track [FLAC {new string('~', 40)}1kbps].flac", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void FilenameGenerator_LongInputPreservesPooledFormatting()
    {
        var artist = new string('A', 600);
        var title = new string('T', 600);
        var generator = new FilenameGenerator(NullLogger<FilenameGenerator>.Instance);
        var variant = new VariantHint { Codec = "FLAC", BitrateKbps = int.MinValue };

        var result = generator.GenerateFilenameAsync(artist, title, variant, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal($"{artist} - {title} [FLAC -2147483648kbps].flac", result);
    }

    [Fact]
    public void PeerIdAnonymizer_PreservesHashCacheAndReverseLookup()
    {
        const string peerId = "peer:overlay:alpha";
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var first = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        var second = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        var reversed = anonymizer.GetPeerIdFromUsernameAsync(first, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedUsername(peerId), first);
        Assert.Same(first, second);
        Assert.Equal(peerId, reversed);
        Assert.Matches("^mesh-peer-[0-9a-f]{6}$", first);
    }

    [Fact]
    public void PeerIdAnonymizer_LongUnicodePeerIdPreservesHash()
    {
        var peerId = new string('\u00c9', 600);
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var result = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedUsername(peerId), result);
    }

    [Fact]
    public void PeerIdAnonymizer_UnknownUsernameReturnsNull()
    {
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var result = anonymizer.GetPeerIdFromUsernameAsync("mesh-peer-000000", CancellationToken.None).GetAwaiter().GetResult();

        Assert.Null(result);
    }

    [Fact]
    public void PeerIdAnonymizer_UncachedWidePopulationBoundsAllocation()
    {
        const int peerCount = 10_000;
        var peerIds = new string[peerCount];
        for (var index = 0; index < peerIds.Length; index++)
        {
            peerIds[index] = $"peer:overlay:{index:D5}";
        }

        var warmup = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);
        _ = warmup.GetAnonymizedUsernameAsync("warmup", CancellationToken.None).GetAwaiter().GetResult();
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        foreach (var peerId in peerIds)
        {
            result = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(ExpectedUsername(peerIds[^1]), result);
        Assert.InRange(allocated, 0, 6_500_000);
    }

    private static string ExpectedUsername(string peerId)
        => $"mesh-peer-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(peerId)).AsSpan(0, 3))}";
}
