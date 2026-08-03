// <copyright file="MeshSearchMetadataTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind;

using Microsoft.Extensions.Logging.Abstractions;
using slskd.Mesh;
using slskd.Search;
using slskd.VirtualSoulfind.DisasterMode;
using Xunit;

public sealed class MeshSearchMetadataTests
{
    [Fact]
    public async Task SearchByMbidAsync_PropagatesDescriptorBitrateWithoutInventingOne()
    {
        const string mbid = "11111111-1111-1111-1111-111111111111";
        var directory = new FakeMeshDirectory
        {
            Peers = [new MeshPeerDescriptor("mesh-peer")],
            Content =
            [
                new MeshContentDescriptor(
                    $"mbid:recording:{mbid}",
                    SizeBytes: 10_000,
                    Codec: "mp3",
                    BitrateKbps: 256),
                new MeshContentDescriptor(
                    $"mbid:recording:{mbid}:unknown",
                    SizeBytes: 11_000,
                    Codec: "aac"),
            ],
        };
        var service = new MeshSearchService(NullLogger<MeshSearchService>.Instance, directory);

        var result = await service.SearchByMbidAsync(mbid);

        Assert.Equal(2, result.PeerResults.Single().Files.Count);
        Assert.Equal(256, result.PeerResults.Single().Files[0].BitrateKbps);
        Assert.Null(result.PeerResults.Single().Files[1].BitrateKbps);
    }

    [Fact]
    public void ConvertMeshFile_PreservesKnownBitrateAndLeavesUnknownDurationUnknown()
    {
        var known = SearchService.ConvertMeshFile(new MeshFileResult
        {
            Filename = "Music/Track.mp3",
            Size = 10_000,
            BitrateKbps = 256,
        });
        var unknown = SearchService.ConvertMeshFile(new MeshFileResult
        {
            Filename = "Music/Track.aac",
            Size = 10_000,
        });

        Assert.Equal(256, known.BitRate);
        Assert.Null(known.Length);
        Assert.Null(unknown.BitRate);
        Assert.Null(unknown.Length);
    }

    private sealed class FakeMeshDirectory : IMeshDirectory
    {
        public IReadOnlyList<MeshPeerDescriptor> Peers { get; init; } = [];

        public IReadOnlyList<MeshContentDescriptor> Content { get; init; } = [];

        public Task<MeshPeerDescriptor?> FindPeerByIdAsync(string peerId, CancellationToken ct = default)
            => Task.FromResult<MeshPeerDescriptor?>(null);

        public Task<IReadOnlyList<MeshPeerDescriptor>> FindPeersByContentAsync(string contentId, CancellationToken ct = default)
            => Task.FromResult(Peers);

        public Task<IReadOnlyList<MeshContentDescriptor>> FindContentByPeerAsync(string peerId, CancellationToken ct = default)
            => Task.FromResult(Content);
    }
}
