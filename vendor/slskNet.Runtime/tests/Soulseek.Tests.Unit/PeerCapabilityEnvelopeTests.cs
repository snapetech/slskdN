// <copyright file="PeerCapabilityEnvelopeTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Soulseek.Messaging;
    using Xunit;

    public class PeerCapabilityEnvelopeTests
    {
        [Fact(DisplayName = "Capability envelope round trips descriptor")]
        public void Capability_Envelope_Round_Trips_Descriptor()
        {
            var descriptor = new PeerCapabilityDescriptor(
                peerId: "peer-1",
                features: new[] { "wishlist", "overlay", "wishlist" },
                overlayPort: 4444,
                maxPayloadLength: 2048);
            var envelope = new PeerCapabilityEnvelope(PeerCapabilityMessageType.Hello, descriptor, nonce: "abc");

            var parsed = PeerCapabilityEnvelope.FromByteArray(envelope.ToByteArray());

            Assert.Equal(PeerCapabilityMessageType.Hello, parsed.MessageType);
            Assert.Equal("abc", parsed.Nonce);
            Assert.Equal("peer-1", parsed.Descriptor.PeerId);
            Assert.Equal(4444, parsed.Descriptor.OverlayPort);
            Assert.Equal(2048, parsed.Descriptor.MaxPayloadLength);
            Assert.Equal(new[] { "overlay", "wishlist" }, parsed.Descriptor.Features);
        }

        [Fact(DisplayName = "Capability envelope rejects invalid magic")]
        public void Capability_Envelope_Rejects_Invalid_Magic()
        {
            var bytes = new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor()).ToByteArray();
            bytes[0] = 0;

            Assert.Throws<MessageException>(() => PeerCapabilityEnvelope.FromByteArray(bytes));
        }

        [Fact(DisplayName = "Capability envelope rejects truncated header")]
        public void Capability_Envelope_Rejects_Truncated_Header()
        {
            Assert.Throws<MessageException>(() => PeerCapabilityEnvelope.FromByteArray(new byte[] { 1, 2, 3 }));
        }

        [Fact(DisplayName = "Capability envelope rejects undefined message type")]
        public void Capability_Envelope_Rejects_Undefined_Message_Type()
        {
            var descriptor = new PeerCapabilityDescriptor();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityEnvelope(
                (PeerCapabilityMessageType)99,
                descriptor));

            Assert.Equal("messageType", ex.ParamName);
        }

        [Fact(DisplayName = "Capability envelope parser rejects undefined message type")]
        public void Capability_Envelope_Parser_Rejects_Undefined_Message_Type()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4E44534B);
                writer.Write(PeerCapabilityEnvelope.CurrentVersion);
                writer.Write(99);
                writer.Write(1);
                writer.Write(Encoding.UTF8.GetBytes("n"));
                writer.Write(0);
                writer.Write(-1);
                writer.Write(PeerCapabilityEnvelope.DefaultMaxPayloadLength);
                writer.Write(0);
                writer.Write(false);
                writer.Flush();

                Assert.Throws<MessageException>(() => PeerCapabilityEnvelope.FromByteArray(stream.ToArray()));
            }
        }

        [Fact(DisplayName = "Capability envelope rejects truncated declared byte arrays")]
        public void Capability_Envelope_Rejects_Truncated_Declared_Byte_Arrays()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(0x4E44534B);
                writer.Write(PeerCapabilityEnvelope.CurrentVersion);
                writer.Write((int)PeerCapabilityMessageType.Hello);
                writer.Write(1);
                writer.Write(Encoding.UTF8.GetBytes("n"));
                writer.Write(4);
                writer.Write(Encoding.UTF8.GetBytes("ab"));
                writer.Flush();

                Assert.Throws<MessageException>(() => PeerCapabilityEnvelope.FromByteArray(stream.ToArray()));
            }
        }

        [Fact(DisplayName = "Capability registry updates case-insensitive records")]
        public void Capability_Registry_Updates_Case_Insensitive_Records()
        {
            var registry = new PeerCapabilityRegistry();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var envelope = new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor(features: new[] { "mesh" }),
                nonce: "n1");
            PeerCapabilityRecord raised = null;
            registry.Updated += (_, e) => raised = e.Record;

            registry.Update("Alice", endpoint, envelope);

            Assert.True(registry.TryGet("alice", out var record));
            Assert.Same(record, raised);
            Assert.Equal(endpoint, record.EndPoint);
            Assert.Equal("n1", record.Nonce);
        }

        [Fact(DisplayName = "Capability registry uses ordinal username identity")]
        public void Capability_Registry_Uses_Ordinal_Username_Identity()
        {
            var registry = new PeerCapabilityRegistry();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var envelope = new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor(features: new[] { "mesh" }),
                nonce: "n1");

            registry.Update("user", endpoint, envelope);

            Assert.False(registry.TryGet("u\0ser", out _));
        }

        [Theory(DisplayName = "Capability registry rejects empty usernames")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Capability_Registry_Rejects_Empty_Usernames(string username)
        {
            var registry = new PeerCapabilityRegistry();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var envelope = new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor(features: new[] { "mesh" }),
                nonce: "n1");

            Assert.Throws<ArgumentException>(() => registry.Update(username, endpoint, envelope));
        }

        [Fact(DisplayName = "Capability registry rejects null endpoints")]
        public void Capability_Registry_Rejects_Null_Endpoints()
        {
            var registry = new PeerCapabilityRegistry();
            var envelope = new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor(features: new[] { "mesh" }),
                nonce: "n1");

            Assert.Throws<ArgumentNullException>(() => registry.Update("user", null, envelope));
        }

        [Fact(DisplayName = "Capability record rejects undefined message type")]
        public void Capability_Record_Rejects_Undefined_Message_Type()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var descriptor = new PeerCapabilityDescriptor();
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityRecord(
                "user",
                endpoint,
                descriptor,
                (PeerCapabilityMessageType)99,
                nonce: "n1",
                observedAt: DateTimeOffset.UtcNow));

            Assert.Equal("messageType", ex.ParamName);
        }

        [Fact(DisplayName = "Capability descriptor rejects envelope-unserializable feature count")]
        public void Capability_Descriptor_Rejects_Envelope_Unserializable_Feature_Count()
        {
            var features = Enumerable.Range(0, 257).Select(i => $"feature-{i}");

            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityDescriptor(features: features));
        }

        [Fact(DisplayName = "Capability descriptor rejects envelope-unserializable strings")]
        public void Capability_Descriptor_Rejects_Envelope_Unserializable_Strings()
        {
            var tooLong = new string('a', 4097);

            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityDescriptor(peerId: tooLong));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityDescriptor(features: new[] { tooLong }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerCapabilityEnvelope(
                PeerCapabilityMessageType.Hello,
                new PeerCapabilityDescriptor(),
                nonce: tooLong));
        }

        [Fact(DisplayName = "Capability signature rejects envelope-unserializable payloads")]
        public void Capability_Signature_Rejects_Envelope_Unserializable_Payloads()
        {
            var tooLong = new byte[4097];

            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerDescriptorSignature(tooLong, new byte[64]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerDescriptorSignature(new byte[32], tooLong));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PeerDescriptorSignature(
                new byte[32],
                new byte[64],
                new string('a', 4097)));
        }
    }
}
