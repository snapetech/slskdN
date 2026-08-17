// <copyright file="QuicInitialAlpnSnifferTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System.Security.Cryptography;
using slskd.DhtRendezvous;
using Xunit;

/// <summary>
/// Validates <see cref="QuicInitialAlpnSniffer"/> against hand-built, wire-format QUIC Initial
/// packets produced by <see cref="QuicTestPacketBuilder"/> -- an independent (not shared-code)
/// implementation of the RFC 9001 Initial-packet protection scheme written purely for tests, so a
/// round trip through "test builder encrypts -> sniffer decrypts" exercises the sniffer's key
/// derivation, header protection removal, AEAD decryption, and TLS ClientHello/ALPN parsing
/// against packets it did not produce itself -- the same relationship it has to real
/// msquic-generated traffic in production, just without requiring a msquic runtime to be present
/// in the test environment.
/// </summary>
public class QuicInitialAlpnSnifferTests
{
    [Theory]
    [InlineData("slskdn-overlay")]
    [InlineData("slskdn-overlay-data")]
    [InlineData("some-other-protocol")]
    public void TryGetAlpn_RecoversAlpnFromSyntheticClientInitialPacket(string alpn)
    {
        var dcid = RandomNumberGenerator.GetBytes(8);
        var datagram = QuicTestPacketBuilder.BuildClientInitialPacket(dcid, alpn);

        var result = QuicInitialAlpnSniffer.TryGetAlpn(datagram, out var recovered);

        Assert.True(result);
        Assert.Equal(alpn, recovered);
    }

    [Fact]
    public void TryGetAlpn_ReturnsFalse_ForShortDatagram()
    {
        var result = QuicInitialAlpnSniffer.TryGetAlpn(new byte[] { 0xc0, 0x00, 0x00, 0x00, 0x01 }, out var alpn);

        Assert.False(result);
        Assert.Null(alpn);
    }

    [Fact]
    public void TryGetAlpn_ReturnsFalse_ForDhtPacket()
    {
        var dhtPacket = new byte[1200];
        dhtPacket[0] = (byte)'d';

        var result = QuicInitialAlpnSniffer.TryGetAlpn(dhtPacket, out var alpn);

        Assert.False(result);
        Assert.Null(alpn);
    }

    [Fact]
    public void TryGetAlpn_ReturnsFalse_ForShortHeaderQuicPacket()
    {
        var shortHeaderPacket = new byte[1200];
        shortHeaderPacket[0] = 0x41;

        var result = QuicInitialAlpnSniffer.TryGetAlpn(shortHeaderPacket, out var alpn);

        Assert.False(result);
        Assert.Null(alpn);
    }

    [Fact]
    public void TryGetAlpn_ReturnsFalse_ForCorruptedCiphertext()
    {
        var dcid = RandomNumberGenerator.GetBytes(8);
        var datagram = QuicTestPacketBuilder.BuildClientInitialPacket(dcid, "slskdn-overlay-data");

        // Flip a bit deep in the ciphertext; AEAD authentication must reject this rather than
        // return garbage that happens to parse as an ALPN string.
        datagram[datagram.Length / 2] ^= 0xFF;

        var result = QuicInitialAlpnSniffer.TryGetAlpn(datagram, out var alpn);

        Assert.False(result);
        Assert.Null(alpn);
    }
}
