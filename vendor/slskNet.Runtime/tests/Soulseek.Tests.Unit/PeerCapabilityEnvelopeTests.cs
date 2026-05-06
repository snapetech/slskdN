// <copyright file="PeerCapabilityEnvelopeTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
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
    }
}
