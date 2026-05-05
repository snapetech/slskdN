// <copyright file="MessageFrameValidatorTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Network
{
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class MessageFrameValidatorTests
    {
        [Theory(DisplayName = "Message frame validator accepts valid message lengths")]
        [InlineData(1, 1)]
        [InlineData(4, 4)]
        [InlineData(RotatedObfuscation.MaxMessageLength, 4)]
        public void Message_Frame_Validator_Accepts_Valid_Message_Lengths(int length, int minimumLength)
        {
            var ex = Record.Exception(() => MessageFrameValidator.ValidateMessageLength(length, minimumLength));

            Assert.Null(ex);
        }

        [Theory(DisplayName = "Message frame validator rejects invalid message lengths")]
        [InlineData(-1, 4)]
        [InlineData(0, 1)]
        [InlineData(3, 4)]
        [InlineData(RotatedObfuscation.MaxMessageLength + 1, 4)]
        public void Message_Frame_Validator_Rejects_Invalid_Message_Lengths(int length, int minimumLength)
        {
            Assert.Throws<MessageReadException>(() => MessageFrameValidator.ValidateMessageLength(length, minimumLength));
        }

        [Theory(DisplayName = "Message frame validator accepts valid init lengths")]
        [InlineData(4)]
        [InlineData(RotatedObfuscation.MaxInitMessageLength)]
        public void Message_Frame_Validator_Accepts_Valid_Init_Lengths(int length)
        {
            var ex = Record.Exception(() => MessageFrameValidator.ValidateInitMessageLength(length));

            Assert.Null(ex);
        }

        [Theory(DisplayName = "Message frame validator rejects invalid init lengths")]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(RotatedObfuscation.MaxInitMessageLength + 1)]
        public void Message_Frame_Validator_Rejects_Invalid_Init_Lengths(int length)
        {
            Assert.Throws<MessageReadException>(() => MessageFrameValidator.ValidateInitMessageLength(length));
        }
    }
}
