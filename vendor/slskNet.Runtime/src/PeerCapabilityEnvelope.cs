// <copyright file="PeerCapabilityEnvelope.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Versioned slskdN peer capability envelope.
    /// </summary>
    public sealed class PeerCapabilityEnvelope
    {
        /// <summary>
        ///     The custom peer message code reserved by this runtime for slskdN peer capability exchange.
        /// </summary>
        public const int MessageCode = 0x534C534B;

        /// <summary>
        ///     The current envelope version.
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        ///     The default maximum custom peer-message payload length.
        /// </summary>
        public const int DefaultMaxPayloadLength = 65536;

        internal const int MaximumFeatureCount = 256;
        internal const int MaximumStringLength = 4096;
        internal const int MaximumSignatureLength = 4096;

        private const int Magic = 0x4E44534B;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerCapabilityEnvelope"/> class.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="descriptor">The capability descriptor.</param>
        /// <param name="nonce">The exchange nonce.</param>
        /// <param name="version">The envelope version.</param>
        public PeerCapabilityEnvelope(
            PeerCapabilityMessageType messageType,
            PeerCapabilityDescriptor descriptor,
            string nonce = null,
            int version = CurrentVersion)
        {
            if (version < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero.");
            }

            if (!Enum.IsDefined(typeof(PeerCapabilityMessageType), messageType))
            {
                throw new ArgumentOutOfRangeException(nameof(messageType), "Message type is not defined.");
            }

            Version = version;
            MessageType = messageType;
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Nonce = string.IsNullOrWhiteSpace(nonce)
                ? Guid.NewGuid().ToString("N")
                : ProtocolArgumentValidator.RequireMaximumUtf8Length(nonce, nameof(nonce), "nonce", MaximumStringLength);
        }

        /// <summary>
        ///     Gets the capability descriptor.
        /// </summary>
        public PeerCapabilityDescriptor Descriptor { get; }

        /// <summary>
        ///     Gets the message type.
        /// </summary>
        public PeerCapabilityMessageType MessageType { get; }

        /// <summary>
        ///     Gets the exchange nonce.
        /// </summary>
        public string Nonce { get; }

        /// <summary>
        ///     Gets the envelope version.
        /// </summary>
        public int Version { get; }

        /// <summary>
        ///     Parses an envelope from a custom peer-message payload.
        /// </summary>
        /// <param name="bytes">The payload bytes.</param>
        /// <returns>The parsed envelope.</returns>
        public static PeerCapabilityEnvelope FromByteArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var magic = reader.ReadInt32();

                    if (magic != Magic)
                    {
                        throw new MessageException($"Peer capability magic mismatch (expected: {Magic}, received: {magic})");
                    }

                    var version = reader.ReadInt32();
                    var type = (PeerCapabilityMessageType)reader.ReadInt32();
                    var nonce = ReadString(reader);
                    var peerId = ReadString(reader);
                    var overlayPort = reader.ReadInt32();
                    var maxPayloadLength = reader.ReadInt32();
                    var featureCount = ReadCount(reader, nameof(PeerCapabilityDescriptor.Features), MaximumFeatureCount);
                    var features = new List<string>();

                    for (var i = 0; i < featureCount; i++)
                    {
                        features.Add(ReadString(reader));
                    }

                    PeerDescriptorSignature signature = null;

                    if (reader.ReadBoolean())
                    {
                        var algorithm = ReadString(reader);
                        var publicKey = ReadBytes(reader, MaximumSignatureLength);
                        var signatureBytes = ReadBytes(reader, MaximumSignatureLength);
                        signature = new PeerDescriptorSignature(publicKey, signatureBytes, algorithm);
                    }

                    var descriptor = new PeerCapabilityDescriptor(
                        peerId: string.IsNullOrEmpty(peerId) ? null : peerId,
                        features: features,
                        overlayPort: overlayPort < 0 ? (int?)null : overlayPort,
                        maxPayloadLength: maxPayloadLength,
                        signature: signature);

                    return new PeerCapabilityEnvelope(type, descriptor, nonce, version);
                }
            }
            catch (MessageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MessageException($"Failed to parse peer capability envelope: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Constructs a byte array from this envelope.
        /// </summary>
        /// <returns>The payload bytes.</returns>
        public byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write((int)MessageType);
                WriteString(writer, Nonce);
                WriteString(writer, Descriptor.PeerId);
                writer.Write(Descriptor.OverlayPort ?? -1);
                writer.Write(Descriptor.MaxPayloadLength);
                writer.Write(Descriptor.Features.Count);

                foreach (var feature in Descriptor.Features)
                {
                    WriteString(writer, feature);
                }

                writer.Write(Descriptor.Signature != null);

                if (Descriptor.Signature != null)
                {
                    WriteString(writer, Descriptor.Signature.Algorithm);
                    WriteBytes(writer, Descriptor.Signature.PublicKey);
                    WriteBytes(writer, Descriptor.Signature.Signature);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static int ReadCount(BinaryReader reader, string collectionName, int maximum)
        {
            var count = reader.ReadInt32();

            if (count < 0 || count > maximum)
            {
                throw new MessageException($"Invalid {collectionName} count: {count}");
            }

            return count;
        }

        private static byte[] ReadBytes(BinaryReader reader, int maximumLength)
        {
            var length = ReadCount(reader, "byte array", maximumLength);
            var bytes = reader.ReadBytes(length);

            if (bytes.Length != length)
            {
                throw new MessageException($"Invalid byte array length: expected {length}, received {bytes.Length}");
            }

            return bytes;
        }

        private static string ReadString(BinaryReader reader)
        {
            var bytes = ReadBytes(reader, MaximumStringLength);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            bytes ??= Array.Empty<byte>();
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            WriteBytes(writer, Encoding.UTF8.GetBytes(value ?? string.Empty));
        }
    }
}
