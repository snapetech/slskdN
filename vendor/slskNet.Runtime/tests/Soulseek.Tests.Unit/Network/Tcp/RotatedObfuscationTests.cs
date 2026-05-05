// <copyright file="RotatedObfuscationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
// </copyright>

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class RotatedObfuscationTests
    {
        [Fact]
        public void Encode_Matches_Public_Type1_Vector()
        {
            var plain = Hex("0800000079000000e8030000");
            var expected = Hex("1494ee4a2028dd952850ba2b4aa37457");

            var encoded = RotatedObfuscation.Encode(plain, 0x4aee_9414);

            Assert.Equal(expected, encoded);
            Assert.Equal(plain, RotatedObfuscation.Decode(encoded));
        }

        [Fact]
        public void Encode_RoundTrips_Partial_Final_Block()
        {
            var plain = new byte[] { 1, 2, 3, 4, 5, 6, 7 };

            var encoded = RotatedObfuscation.Encode(plain, 0x1020_3040);

            Assert.Equal(plain, RotatedObfuscation.Decode(encoded));
        }

        [Fact]
        public void Length_Caps_Are_Bounded_For_Protocol_Use()
        {
            Assert.Equal(1024, RotatedObfuscation.MaxInitMessageLength);
            Assert.Equal(8 * 1024 * 1024, RotatedObfuscation.MaxMessageLength);
        }

        [Fact]
        public void Encode_And_Decode_Reject_Null_Input()
        {
            Assert.Throws<ArgumentNullException>(() => RotatedObfuscation.Encode(null));
            Assert.Throws<ArgumentNullException>(() => RotatedObfuscation.Encode(null, 0x1020_3040));
            Assert.Throws<ArgumentNullException>(() => RotatedObfuscation.Decode(null));
        }

        private static byte[] Hex(string input)
        {
            var bytes = new byte[input.Length / 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = System.Convert.ToByte(input.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
