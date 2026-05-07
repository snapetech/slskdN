// <copyright file="DistributedChildDepthTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//     Copyright (c) slskdN Team.
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class DistributedChildDepthTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(int depth)
        {
            depth = Math.Abs(depth == int.MinValue ? 0 : depth);

            var r = new DistributedChildDepth(depth);

            Assert.Equal(depth, r.Depth);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation accepts negative depth from the wire")]
        public void Instantiation_Accepts_Negative_Depth()
        {
            var r = new DistributedChildDepth(-1);

            Assert.Equal(-1, r.Depth);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray Constructs the correct Message"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message(int depth)
        {
            depth = Math.Abs(depth == int.MinValue ? 0 : depth);

            var msg = new DistributedChildDepth(depth).ToByteArray();

            var reader = new MessageReader<MessageCode.Distributed>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Distributed.ChildDepth, code);
            Assert.Equal(4 + 1 + 4, msg.Length);

            Assert.Equal(depth, reader.ReadInteger());
        }

        [Trait("Category", "FromByteArray")]
        [Theory(DisplayName = "FromByteArray returns the expected data"), AutoData]
        public void FromByteArray_Returns_Expected_Data(int depth)
        {
            depth = Math.Abs(depth == int.MinValue ? 0 : depth);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(depth)
                .Build();

            var response = DistributedChildDepth.FromByteArray(msg);

            Assert.Equal(depth, response.Depth);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray accepts negative depth from the wire")]
        public void FromByteArray_Accepts_Negative_Depth()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(-1)
                .Build();

            var response = DistributedChildDepth.FromByteArray(msg);

            Assert.Equal(-1, response.Depth);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray throws MessageException on code mismatch")]
        public void FromByteArray_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => DistributedChildDepth.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
