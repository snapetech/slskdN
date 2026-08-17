// <copyright file="QuicInitialAlpnSniffer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous;

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Best-effort extraction of the ALPN protocol name from a QUIC Initial packet's TLS ClientHello.
/// Used by <see cref="SharedMeshUdpListener"/> to route control-plane vs. data-plane QUIC traffic
/// that share one public UDP socket.
/// </summary>
/// <remarks>
/// QUIC Initial packets are encrypted, but only with "Initial keys" derived from a version-specific
/// public salt (RFC 9001 section 5.2) and the packet's own Destination Connection ID -- both visible
/// on the wire. No peer secret or prior handshake state is required to decrypt them, so any
/// observer (including this listener, sitting in front of both QUIC backends) can do it. This is
/// exactly what real QUIC-aware load balancers do to route by ALPN/SNI before a connection is
/// established.
///
/// This is deliberately conservative: any parsing ambiguity, truncation, or unsupported framing
/// causes <see cref="TryGetAlpn"/> to return false rather than guess, so callers must fall back to
/// a safe default backend. A misroute here only affects which loopback backend a QUIC handshake is
/// proxied to -- both backends are our own processes on the same trust tier, so the worst case of a
/// wrong guess is a failed ALPN negotiation and a dropped connection attempt, not a security
/// boundary crossing.
/// </remarks>
internal static class QuicInitialAlpnSniffer
{
    // RFC 9001 section 5.2 (QUIC version 1).
    private static readonly byte[] QuicV1InitialSalt = Convert.FromHexString("38762cf7f55934b34d179ae6a4c80cadccbb7f0a");

    // RFC 9369 section 3.3.1 (QUIC version 2).
    private static readonly byte[] QuicV2InitialSalt = Convert.FromHexString("0dede3def700a6db819381be6e269dcbf9bd2ed9");

    private const int SampleLength = 16;
    private const int AeadTagLength = 16;
    private const int MaxCryptoFrameLength = 4096;
    private const byte FrameTypePadding = 0x00;
    private const byte FrameTypePing = 0x01;
    private const byte FrameTypeCrypto = 0x06;
    private const byte TlsHandshakeTypeClientHello = 0x01;
    private const int TlsExtensionTypeAlpn = 16;

    /// <summary>
    /// Attempts to extract the negotiated ALPN protocol name offered in <paramref name="datagram"/>.
    /// Returns false (with <paramref name="alpn"/> null) on anything other than a cleanly-parsed,
    /// singly-framed Initial packet carrying a complete ClientHello with an ALPN extension.
    /// </summary>
    public static bool TryGetAlpn(ReadOnlySpan<byte> datagram, out string? alpn)
    {
        try
        {
            return TryGetAlpnCore(datagram, out alpn);
        }
        catch
        {
            // Defense in depth: this parses attacker-controlled bytes from the public internet in
            // a shared receive loop. Never let a malformed/adversarial datagram throw out of here.
            alpn = null;
            return false;
        }
    }

    private static bool TryGetAlpnCore(ReadOnlySpan<byte> datagram, out string? alpn)
    {
        alpn = null;

        if (datagram.Length < 1200 || (datagram[0] & 0xC0) != 0xC0)
        {
            return false;
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(datagram[1..5]);
        var packetType = (datagram[0] & 0x30) >> 4;

        byte[] initialSalt;
        if (version == 0x00000001 && packetType == 0)
        {
            initialSalt = QuicV1InitialSalt;
        }
        else if (version == 0x6b3343cf && packetType == 1)
        {
            initialSalt = QuicV2InitialSalt;
        }
        else
        {
            return false;
        }

        var offset = 5;

        if (offset >= datagram.Length)
        {
            return false;
        }

        var dcidLength = datagram[offset++];
        if (dcidLength > 20 || offset + dcidLength > datagram.Length)
        {
            return false;
        }

        var dcid = datagram.Slice(offset, dcidLength);
        offset += dcidLength;

        if (offset >= datagram.Length)
        {
            return false;
        }

        var scidLength = datagram[offset++];
        if (offset + scidLength > datagram.Length)
        {
            return false;
        }

        offset += scidLength;

        if (!TryReadVarInt(datagram, ref offset, out var tokenLength) ||
            tokenLength > int.MaxValue ||
            offset + (int)tokenLength > datagram.Length)
        {
            return false;
        }

        offset += (int)tokenLength;

        if (!TryReadVarInt(datagram, ref offset, out var lengthValue) || lengthValue > int.MaxValue)
        {
            return false;
        }

        var length = (int)lengthValue;
        var packetNumberOffset = offset;

        if (packetNumberOffset + length > datagram.Length || length < 4 + AeadTagLength)
        {
            return false;
        }

        var sampleOffset = packetNumberOffset + 4;
        if (sampleOffset + SampleLength > datagram.Length)
        {
            return false;
        }

        var clientInitialSecret = DeriveClientInitialSecret(dcid, initialSalt);
        var headerProtectionKey = HkdfExpandLabel(clientInitialSecret, "quic hp", 16);
        var key = HkdfExpandLabel(clientInitialSecret, "quic key", 16);
        var iv = HkdfExpandLabel(clientInitialSecret, "quic iv", 12);

        var mask = ComputeHeaderProtectionMask(headerProtectionKey, datagram.Slice(sampleOffset, SampleLength));

        var firstByte = (byte)(datagram[0] ^ (mask[0] & 0x0F));
        var packetNumberLength = (firstByte & 0x03) + 1;

        if (packetNumberOffset + packetNumberLength > datagram.Length || packetNumberLength + AeadTagLength > length)
        {
            return false;
        }

        Span<byte> packetNumberBytes = stackalloc byte[4];
        for (var i = 0; i < packetNumberLength; i++)
        {
            packetNumberBytes[i] = (byte)(datagram[packetNumberOffset + i] ^ mask[1 + i]);
        }

        long truncatedPacketNumber = 0;
        for (var i = 0; i < packetNumberLength; i++)
        {
            truncatedPacketNumber = (truncatedPacketNumber << 8) | packetNumberBytes[i];
        }

        // This is always the first packet on a brand-new remote endpoint (SharedMeshUdpListener
        // only calls us for datagrams with no existing session), so there is no prior largest
        // packet number to reconstruct against.
        var packetNumber = DecodePacketNumber(largestPacketNumber: -1, truncatedPacketNumber, packetNumberLength * 8);

        var associatedData = new byte[packetNumberOffset + packetNumberLength];
        datagram[..packetNumberOffset].CopyTo(associatedData);
        associatedData[0] = firstByte;
        for (var i = 0; i < packetNumberLength; i++)
        {
            associatedData[packetNumberOffset + i] = packetNumberBytes[i];
        }

        var payloadStart = packetNumberOffset + packetNumberLength;
        var payloadLength = length - packetNumberLength;
        var ciphertextLength = payloadLength - AeadTagLength;

        if (ciphertextLength <= 0 || payloadStart + payloadLength > datagram.Length)
        {
            return false;
        }

        var ciphertext = datagram.Slice(payloadStart, ciphertextLength);
        var tag = datagram.Slice(payloadStart + ciphertextLength, AeadTagLength);

        Span<byte> nonce = stackalloc byte[12];
        iv.CopyTo(nonce);
        for (var i = 0; i < 8; i++)
        {
            var shift = 8 * (7 - i);
            nonce[4 + i] ^= (byte)((packetNumber >> shift) & 0xFF);
        }

        var plaintext = new byte[ciphertext.Length];
        using (var aesGcm = new AesGcm(key, AeadTagLength))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }

        return TryExtractAlpnFromInitialPlaintext(plaintext, out alpn);
    }

    private static byte[] DeriveClientInitialSecret(ReadOnlySpan<byte> destinationConnectionId, byte[] initialSalt)
    {
        var initialSecret = HKDF.Extract(HashAlgorithmName.SHA256, destinationConnectionId.ToArray(), initialSalt);
        return HkdfExpandLabel(initialSecret, "client in", 32);
    }

    private static byte[] ComputeHeaderProtectionMask(byte[] headerProtectionKey, ReadOnlySpan<byte> sample)
    {
        using var aes = Aes.Create();
        aes.Key = headerProtectionKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        var input = sample.ToArray();
        var mask = new byte[16];
        encryptor.TransformBlock(input, 0, input.Length, mask, 0);
        return mask;
    }

    /// <summary>
    /// TLS 1.3 / QUIC HKDF-Expand-Label (RFC 8446 section 7.1), with the empty Context that QUIC
    /// always uses (RFC 9001 section 5.1).
    /// </summary>
    private static byte[] HkdfExpandLabel(byte[] secret, string label, int length)
    {
        var fullLabel = Encoding.ASCII.GetBytes("tls13 " + label);
        var info = new byte[2 + 1 + fullLabel.Length + 1];
        var span = info.AsSpan();
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)length);
        span[2] = (byte)fullLabel.Length;
        fullLabel.CopyTo(span[3..]);
        span[3 + fullLabel.Length] = 0; // zero-length Context

        return HKDF.Expand(HashAlgorithmName.SHA256, secret, length, info);
    }

    /// <summary>RFC 9000 Appendix A: reconstructs a full packet number from its truncated form.</summary>
    private static long DecodePacketNumber(long largestPacketNumber, long truncatedPacketNumber, int packetNumberBits)
    {
        var expectedPacketNumber = largestPacketNumber + 1;
        var window = 1L << packetNumberBits;
        var halfWindow = window / 2;
        var candidate = (expectedPacketNumber & ~(window - 1)) | truncatedPacketNumber;

        if (candidate <= expectedPacketNumber - halfWindow && candidate < (1L << 62) - window)
        {
            return candidate + window;
        }

        if (candidate > expectedPacketNumber + halfWindow && candidate >= window)
        {
            return candidate - window;
        }

        return candidate;
    }

    private static bool TryReadVarInt(ReadOnlySpan<byte> data, ref int offset, out ulong value)
    {
        value = 0;
        if (offset >= data.Length)
        {
            return false;
        }

        var first = data[offset];
        var encodedLength = 1 << (first >> 6);
        if (offset + encodedLength > data.Length)
        {
            return false;
        }

        ulong result = (ulong)(first & 0x3F);
        for (var i = 1; i < encodedLength; i++)
        {
            result = (result << 8) | data[offset + i];
        }

        value = result;
        offset += encodedLength;
        return true;
    }

    private static bool TryExtractAlpnFromInitialPlaintext(ReadOnlySpan<byte> plaintext, out string? alpn)
    {
        alpn = null;
        var offset = 0;

        while (offset < plaintext.Length)
        {
            if (!TryReadVarInt(plaintext, ref offset, out var frameType))
            {
                return false;
            }

            switch (frameType)
            {
                case FrameTypePadding:
                case FrameTypePing:
                    // Both are bare single-byte frames; TryReadVarInt already consumed that byte.
                    continue;
                case FrameTypeCrypto:
                    if (!TryReadVarInt(plaintext, ref offset, out var cryptoOffset) ||
                        !TryReadVarInt(plaintext, ref offset, out var cryptoLength))
                    {
                        return false;
                    }

                    if (cryptoOffset != 0 ||
                        cryptoLength > MaxCryptoFrameLength ||
                        offset + (int)cryptoLength > plaintext.Length)
                    {
                        // A non-zero offset means the ClientHello is fragmented/coalesced across
                        // packets we don't have; bail out rather than guess.
                        return false;
                    }

                    return TryExtractAlpnFromClientHello(plaintext.Slice(offset, (int)cryptoLength), out alpn);
                default:
                    // Any other frame type appearing before CRYPTO in a first Initial packet is
                    // outside what our own clients ever send; stop rather than risk misparsing an
                    // unfamiliar frame body layout.
                    return false;
            }
        }

        return false;
    }

    private static bool TryExtractAlpnFromClientHello(ReadOnlySpan<byte> handshakeMessage, out string? alpn)
    {
        alpn = null;

        if (handshakeMessage.Length < 4 || handshakeMessage[0] != TlsHandshakeTypeClientHello)
        {
            return false;
        }

        var declaredLength = (handshakeMessage[1] << 16) | (handshakeMessage[2] << 8) | handshakeMessage[3];
        var available = handshakeMessage.Length - 4;
        var bodyLength = Math.Min(declaredLength, available);
        var body = handshakeMessage.Slice(4, bodyLength);
        var pos = 0;

        if (pos + 2 + 32 > body.Length)
        {
            return false;
        }

        pos += 2; // legacy_version
        pos += 32; // random

        if (pos >= body.Length)
        {
            return false;
        }

        var sessionIdLength = body[pos++];
        if (pos + sessionIdLength > body.Length)
        {
            return false;
        }

        pos += sessionIdLength;

        if (pos + 2 > body.Length)
        {
            return false;
        }

        var cipherSuitesLength = (body[pos] << 8) | body[pos + 1];
        pos += 2;
        if (pos + cipherSuitesLength > body.Length)
        {
            return false;
        }

        pos += cipherSuitesLength;

        if (pos >= body.Length)
        {
            return false;
        }

        var compressionMethodsLength = body[pos++];
        if (pos + compressionMethodsLength > body.Length)
        {
            return false;
        }

        pos += compressionMethodsLength;

        if (pos + 2 > body.Length)
        {
            return false;
        }

        var extensionsLength = (body[pos] << 8) | body[pos + 1];
        pos += 2;
        if (pos + extensionsLength > body.Length)
        {
            return false;
        }

        var extensionsEnd = pos + extensionsLength;

        while (pos + 4 <= extensionsEnd)
        {
            var extensionType = (body[pos] << 8) | body[pos + 1];
            var extensionLength = (body[pos + 2] << 8) | body[pos + 3];
            pos += 4;

            if (pos + extensionLength > extensionsEnd)
            {
                return false;
            }

            if (extensionType == TlsExtensionTypeAlpn)
            {
                return TryExtractAlpnFromExtensionBody(body.Slice(pos, extensionLength), out alpn);
            }

            pos += extensionLength;
        }

        return false;
    }

    private static bool TryExtractAlpnFromExtensionBody(ReadOnlySpan<byte> extensionBody, out string? alpn)
    {
        alpn = null;

        if (extensionBody.Length < 2)
        {
            return false;
        }

        var protocolListLength = (extensionBody[0] << 8) | extensionBody[1];
        if (2 + protocolListLength > extensionBody.Length || protocolListLength < 1)
        {
            return false;
        }

        // Only the first offered protocol name is needed to distinguish our own two ALPN values.
        var nameLength = extensionBody[2];
        if (3 + nameLength > extensionBody.Length)
        {
            return false;
        }

        alpn = Encoding.ASCII.GetString(extensionBody.Slice(3, nameLength));
        return true;
    }
}
