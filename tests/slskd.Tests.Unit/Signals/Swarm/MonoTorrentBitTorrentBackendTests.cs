// <copyright file="MonoTorrentBitTorrentBackendTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Signals.Swarm;

using System.Collections.Generic;
using MonoTorrent;
using slskd.Signals.Swarm;
using slskd.Swarm;
using slskd.VirtualSoulfind.v2.Backends;
using Xunit;

public class MonoTorrentBitTorrentBackendTests
{
    private const string V1Hash = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void BuildEngineSettings_PrivateModeDisablesDhtAndLocalPeerDiscovery()
    {
        var settings = MonoTorrentBitTorrentBackend.BuildEngineSettings(
            "/tmp/slskdn-bt-cache",
            new PrivateTorrentModeOptions { PrivateOnly = true, DisableDht = false });

        Assert.Null(settings.DhtEndPoint);
        Assert.False(settings.AllowLocalPeerDiscovery);
        Assert.False(settings.AutoSaveLoadMagnetLinkMetadata);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void BuildTorrentSettings_AppliesPrivateAndIndividualDiscoveryFlags(
        bool forcePrivateOnly,
        bool disableDht,
        bool disablePex)
    {
        var settings = MonoTorrentBitTorrentBackend.BuildTorrentSettings(
            new PrivateTorrentModeOptions
            {
                PrivateOnly = false,
                DisableDht = disableDht,
                DisablePex = disablePex,
            },
            forcePrivateOnly);

        Assert.Equal(!forcePrivateOnly && !disableDht, settings.AllowDht);
        Assert.Equal(!forcePrivateOnly && !disablePex, settings.AllowPeerExchange);
    }

    [Fact]
    public void BuildTorrentSettings_PrivateOnlyOverridesContradictoryDiscoveryFlags()
    {
        var settings = MonoTorrentBitTorrentBackend.BuildTorrentSettings(
            new PrivateTorrentModeOptions { PrivateOnly = true, DisableDht = false, DisablePex = false },
            forcePrivateOnly: false);

        Assert.False(settings.AllowDht);
        Assert.False(settings.AllowPeerExchange);
    }

    [Fact]
    public void ApplyPrivateMagnetPolicy_RemovesTrackersAndWebSeedsButPreservesIdentity()
    {
        var magnet = new MagnetLink(
            InfoHash.FromHex(V1Hash),
            "example.flac",
            new List<string> { "https://tracker.example/announce" },
            new[] { "https://seed.example/example.flac" },
            1234);

        var sanitized = MonoTorrentBitTorrentBackend.ApplyPrivateMagnetPolicy(magnet, privateOnly: true);

        Assert.NotSame(magnet, sanitized);
        Assert.Equal(magnet.InfoHashes, sanitized.InfoHashes);
        Assert.Equal(magnet.Name, sanitized.Name);
        Assert.Equal(magnet.Size, sanitized.Size);
        Assert.Empty(sanitized.AnnounceUrls);
        Assert.Empty(sanitized.Webseeds);
    }

    [Fact]
    public void ApplyPrivateMagnetPolicy_PublicModeRetainsOriginalMagnet()
    {
        var magnet = new MagnetLink(
            InfoHash.FromHex(V1Hash),
            "example.flac",
            new List<string> { "https://tracker.example/announce" },
            new[] { "https://seed.example/example.flac" },
            1234);

        Assert.Same(magnet, MonoTorrentBitTorrentBackend.ApplyPrivateMagnetPolicy(magnet, privateOnly: false));
    }

    [Fact]
    public void BuildManualPeers_OverlayPolicyAcceptsOnlyMeshTransports()
    {
        var sources = new[]
        {
            new SwarmSource("mesh-peer", "mesh", "127.0.0.1", 4100),
            new SwarmSource("overlay-peer", "OVERLAY", "[::1]", 4200),
            new SwarmSource("soulseek-peer", "soulseek", "127.0.0.1", 4300),
            new SwarmSource("bad-port", "mesh", "127.0.0.1", 70000),
            new SwarmSource("missing-endpoint", "mesh"),
        };

        var peers = MonoTorrentBitTorrentBackend.BuildManualPeers(
            new PrivateTorrentModeOptions
            {
                AllowedPeerSources = PrivatePeerSource.Overlay,
                InviteList = new[] { "127.0.0.1:4400" },
            },
            sources,
            includeInviteList: true);

        Assert.Equal(2, peers.Count);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "127.0.0.1" && peer.ConnectionUri.Port == 4100);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "[::1]" && peer.ConnectionUri.Port == 4200);
    }

    [Fact]
    public void BuildManualPeers_InviteListPolicyRejectsInvalidAndDuplicateEntries()
    {
        var peers = MonoTorrentBitTorrentBackend.BuildManualPeers(
            new PrivateTorrentModeOptions
            {
                AllowedPeerSources = PrivatePeerSource.InviteList,
                InviteList = new[]
                {
                    "127.0.0.1:4400",
                    "127.0.0.1:4400",
                    "[::1]:4500",
                    "host:0",
                    "user@host:4600",
                    "https://host:4700/announce",
                },
            },
            new[] { new SwarmSource("mesh-peer", "mesh", "127.0.0.1", 4100) },
            includeInviteList: true);

        Assert.Equal(2, peers.Count);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "127.0.0.1" && peer.ConnectionUri.Port == 4400);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "[::1]" && peer.ConnectionUri.Port == 4500);
    }

    [Fact]
    public void BuildManualPeers_BothCombinesOverlayAndInvitePeersWithoutDuplicates()
    {
        var peers = MonoTorrentBitTorrentBackend.BuildManualPeers(
            new PrivateTorrentModeOptions
            {
                AllowedPeerSources = PrivatePeerSource.Both,
                InviteList = new[] { "127.0.0.1:4800", "localhost:4900" },
            },
            new[]
            {
                new SwarmSource("mesh-peer", "mesh", "127.0.0.1", 4800),
                new SwarmSource("soulseek-peer", "soulseek", "127.0.0.1", 5000),
            },
            includeInviteList: true);

        Assert.Equal(2, peers.Count);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "127.0.0.1" && peer.ConnectionUri.Port == 4800);
        Assert.Contains(peers, peer => peer.ConnectionUri.Host == "localhost" && peer.ConnectionUri.Port == 4900);
    }

    [Fact]
    public void BuildManualPeers_WithoutPrivateModeAllowsOnlyExplicitOverlaySources()
    {
        var peers = MonoTorrentBitTorrentBackend.BuildManualPeers(
            privateMode: null,
            new[]
            {
                new SwarmSource("mesh-peer", "mesh", "127.0.0.1", 5100),
                new SwarmSource("soulseek-peer", "soulseek", "127.0.0.1", 5200),
            },
            includeInviteList: true);

        Assert.Single(peers);
        Assert.Equal(5100, peers[0].ConnectionUri.Port);
    }
}
