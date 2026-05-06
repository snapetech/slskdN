// <copyright file="ProtocolRoundtripFuzz.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Fuzz
{
    using System;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    /// <summary>
    ///     Property-based roundtrip fuzz for selected parsers across the three message families.
    ///
    ///     Property: <c>Parse(Serialize(x)) == x</c> for any valid <c>x</c>. A failing case here is a
    ///     parser/serializer disagreement bug — usually an asymmetric validator on one side.
    ///
    ///     This file is intentionally self-contained (no FsCheck dep). The generators are small;
    ///     coverage breadth is the job of <see cref="ProtocolAdversarialFuzz"/>.
    /// </summary>
    [Trait("Category", "Fuzz")]
    public class ProtocolRoundtripFuzz
    {
        private const int Iterations = 1000;

        [Fact]
        public void DistributedBranchLevel_Roundtrips_Valid_Values()
        {
            var rng = new Random(unchecked((int)0xC0DE_BABE));

            for (var i = 0; i < Iterations; i++)
            {
                // Branch level must be non-negative; the constructor enforces that.
                var level = rng.Next(0, int.MaxValue);
                var encoded = new DistributedBranchLevel(level).ToByteArray();
                var decoded = DistributedBranchLevel.FromByteArray(encoded);

                Assert.Equal(level, decoded.Level);
            }
        }

        [Fact]
        public void DistributedChildDepth_Roundtrips_Valid_Values()
        {
            var rng = new Random(unchecked((int)0xDEAD_BEEF));

            for (var i = 0; i < Iterations; i++)
            {
                var depth = rng.Next(0, int.MaxValue);
                var encoded = new DistributedChildDepth(depth).ToByteArray();
                var decoded = DistributedChildDepth.FromByteArray(encoded);

                Assert.Equal(depth, decoded.Depth);
            }
        }

        [Fact]
        public void DistributedBranchLevel_Rejects_Negative_Values_From_Wire()
        {
            // Hand-build a wire-shaped buffer with a negative branch level. A correct parser must
            // reject this; the value cannot be produced by ToByteArray() because the constructor
            // refuses negatives, so we synthesize the bytes directly.
            var bytes = SynthesizeDistributedScalar(MessageCode.Distributed.BranchLevel, -1);
            Assert.ThrowsAny<Exception>(() => DistributedBranchLevel.FromByteArray(bytes));
        }

        [Fact]
        public void DistributedChildDepth_Rejects_Negative_Values_From_Wire()
        {
            var bytes = SynthesizeDistributedScalar(MessageCode.Distributed.ChildDepth, -1);
            Assert.ThrowsAny<Exception>(() => DistributedChildDepth.FromByteArray(bytes));
        }

        private static byte[] SynthesizeDistributedScalar(MessageCode.Distributed code, int value)
        {
            // Distributed messages: 4-byte length, 1-byte code, payload.
            var payloadLength = 1 + 4; // code byte + integer
            var bytes = new byte[4 + payloadLength];
            bytes[0] = (byte)(payloadLength & 0xFF);
            bytes[1] = (byte)((payloadLength >> 8) & 0xFF);
            bytes[2] = (byte)((payloadLength >> 16) & 0xFF);
            bytes[3] = (byte)((payloadLength >> 24) & 0xFF);
            bytes[4] = (byte)code;
            bytes[5] = (byte)(value & 0xFF);
            bytes[6] = (byte)((value >> 8) & 0xFF);
            bytes[7] = (byte)((value >> 16) & 0xFF);
            bytes[8] = (byte)((value >> 24) & 0xFF);
            return bytes;
        }
    }
}
