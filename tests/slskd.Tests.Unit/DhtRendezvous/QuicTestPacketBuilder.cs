// <copyright file="QuicTestPacketBuilder.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Independent (test-only) implementation of RFC 9001's Initial packet protection, used to build
/// known-good wire-format QUIC Initial packets for tests. Deliberately not shared code with
/// <see cref="slskd.DhtRendezvous.QuicInitialAlpnSniffer"/> -- a round trip through "this builder
/// encrypts, the sniffer decrypts" exercises the sniffer against packets it did not produce
/// itself, the same relationship it has to real msquic-generated traffic in production.
/// </summary>
internal static class QuicTestPacketBuilder
{
    private static readonly byte[] QuicV1InitialSalt = Convert.FromHexString("38762cf7f55934b34d179ae6a4c80cadccbb7f0a");

    public static byte[] BuildClientInitialPacket(byte[] dcid, string alpn)
    {
        var scid = RandomNumberGenerator.GetBytes(8);

        var clientHello = BuildMinimalClientHello(alpn);
        var handshakeMessage = WrapHandshakeMessage(clientHello);
        var cryptoFrame = BuildCryptoFrame(handshakeMessage);

        // Pad the plaintext (not just the datagram) so the encrypted packet naturally clears
        // the 1200-byte Initial-packet minimum, mirroring how real clients pad with a trailing
        // PADDING frame rather than raw trailing zero bytes outside the AEAD-protected region.
        const int targetDatagramLength = 1232;
        var headerOverheadEstimate = 1 + 4 + 1 + dcid.Length + 1 + scid.Length + 1 + 2 + 1 + 16;
        var paddingLength = Math.Max(0, targetDatagramLength - headerOverheadEstimate - cryptoFrame.Length);
        var plaintext = new byte[cryptoFrame.Length + paddingLength];
        cryptoFrame.CopyTo(plaintext, 0);

        const byte packetNumber = 0x00;
        const int packetNumberLength = 1;
        byte firstByte = 0xC0; // long header, fixed bit, Initial (type 00), reserved 00, pn-length-1 (00)

        var length = packetNumberLength + plaintext.Length + 16; // + AEAD tag

        var header = new MemoryStream();
        header.WriteByte(firstByte);
        Span<byte> versionBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(versionBytes, 1);
        header.Write(versionBytes);
        header.WriteByte((byte)dcid.Length);
        header.Write(dcid);
        header.WriteByte((byte)scid.Length);
        header.Write(scid);
        header.WriteByte(0x00); // token length = 0
        WriteVarInt(header, (ulong)length);
        var headerBeforePn = header.ToArray();

        var associatedData = new byte[headerBeforePn.Length + packetNumberLength];
        headerBeforePn.CopyTo(associatedData, 0);
        associatedData[^1] = packetNumber;

        var clientInitialSecret = DeriveClientInitialSecret(dcid);
        var key = HkdfExpandLabel(clientInitialSecret, "quic key", 16);
        var iv = HkdfExpandLabel(clientInitialSecret, "quic iv", 12);
        var headerProtectionKey = HkdfExpandLabel(clientInitialSecret, "quic hp", 16);

        Span<byte> nonce = stackalloc byte[12];
        iv.CopyTo(nonce);
        nonce[^1] ^= packetNumber;

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        var protectedPayload = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(protectedPayload, 0);
        tag.CopyTo(protectedPayload, ciphertext.Length);

        var sample = protectedPayload.AsSpan(4 - packetNumberLength, 16).ToArray();
        var mask = ComputeHeaderProtectionMask(headerProtectionKey, sample);

        var protectedFirstByte = (byte)(firstByte ^ (mask[0] & 0x0F));
        var protectedPacketNumber = (byte)(packetNumber ^ mask[1]);

        var datagram = new byte[headerBeforePn.Length + packetNumberLength + protectedPayload.Length];
        var offset = 0;
        datagram[offset++] = protectedFirstByte;
        headerBeforePn.AsSpan(1).CopyTo(datagram.AsSpan(offset));
        offset += headerBeforePn.Length - 1;
        datagram[offset++] = protectedPacketNumber;
        protectedPayload.CopyTo(datagram, offset);

        return datagram;
    }

    private static byte[] BuildMinimalClientHello(string alpn)
    {
        var body = new MemoryStream();
        body.Write(new byte[] { 0x03, 0x03 }); // legacy_version = TLS 1.2 wire value
        body.Write(RandomNumberGenerator.GetBytes(32)); // random
        body.WriteByte(0x00); // session_id length = 0

        var cipherSuites = new byte[] { 0x13, 0x01 }; // TLS_AES_128_GCM_SHA256
        WriteUInt16(body, (ushort)cipherSuites.Length);
        body.Write(cipherSuites);

        body.WriteByte(0x01); // compression_methods length
        body.WriteByte(0x00); // null compression

        var alpnBytes = Encoding.ASCII.GetBytes(alpn);
        var alpnEntry = new byte[1 + alpnBytes.Length];
        alpnEntry[0] = (byte)alpnBytes.Length;
        alpnBytes.CopyTo(alpnEntry, 1);

        var alpnExtensionBody = new MemoryStream();
        WriteUInt16(alpnExtensionBody, (ushort)alpnEntry.Length);
        alpnExtensionBody.Write(alpnEntry);
        var alpnExtensionBodyBytes = alpnExtensionBody.ToArray();

        var extensions = new MemoryStream();
        WriteUInt16(extensions, 16); // extension type: application_layer_protocol_negotiation
        WriteUInt16(extensions, (ushort)alpnExtensionBodyBytes.Length);
        extensions.Write(alpnExtensionBodyBytes);
        var extensionsBytes = extensions.ToArray();

        WriteUInt16(body, (ushort)extensionsBytes.Length);
        body.Write(extensionsBytes);

        return body.ToArray();
    }

    private static byte[] WrapHandshakeMessage(byte[] clientHelloBody)
    {
        var message = new byte[4 + clientHelloBody.Length];
        message[0] = 0x01; // Handshake Type: client_hello
        message[1] = (byte)((clientHelloBody.Length >> 16) & 0xFF);
        message[2] = (byte)((clientHelloBody.Length >> 8) & 0xFF);
        message[3] = (byte)(clientHelloBody.Length & 0xFF);
        clientHelloBody.CopyTo(message, 4);
        return message;
    }

    private static byte[] BuildCryptoFrame(byte[] handshakeMessage)
    {
        var stream = new MemoryStream();
        stream.WriteByte(0x06); // CRYPTO frame type
        WriteVarInt(stream, 0); // offset = 0
        WriteVarInt(stream, (ulong)handshakeMessage.Length);
        stream.Write(handshakeMessage);
        return stream.ToArray();
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteVarInt(Stream stream, ulong value)
    {
        if (value <= 0x3F)
        {
            stream.WriteByte((byte)value);
        }
        else if (value <= 0x3FFF)
        {
            Span<byte> bytes = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);
            bytes[0] |= 0x40;
            stream.Write(bytes);
        }
        else if (value <= 0x3FFFFFFF)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value);
            bytes[0] |= 0x80;
            stream.Write(bytes);
        }
        else
        {
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            bytes[0] |= 0xC0;
            stream.Write(bytes);
        }
    }

    private static byte[] DeriveClientInitialSecret(byte[] dcid)
    {
        var initialSecret = HKDF.Extract(HashAlgorithmName.SHA256, dcid, QuicV1InitialSalt);
        return HkdfExpandLabel(initialSecret, "client in", 32);
    }

    private static byte[] HkdfExpandLabel(byte[] secret, string label, int length)
    {
        var fullLabel = Encoding.ASCII.GetBytes("tls13 " + label);
        var info = new byte[2 + 1 + fullLabel.Length + 1];
        var span = info.AsSpan();
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)length);
        span[2] = (byte)fullLabel.Length;
        fullLabel.CopyTo(span[3..]);
        span[3 + fullLabel.Length] = 0;

        return HKDF.Expand(HashAlgorithmName.SHA256, secret, length, info);
    }

    private static byte[] ComputeHeaderProtectionMask(byte[] headerProtectionKey, byte[] sample)
    {
        using var aes = Aes.Create();
        aes.Key = headerProtectionKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        var mask = new byte[16];
        encryptor.TransformBlock(sample, 0, sample.Length, mask, 0);
        return mask;
    }
}
