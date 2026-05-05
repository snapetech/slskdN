// <copyright file="NetInfoTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class NetInfoTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(List<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            parents = parents.ConvertAll(parent => (parent.Username, parent.IPAddress, System.Math.Abs(parent.Port % (IPEndPoint.MaxPort + 1))));
            NetInfoNotification response = null;

            var ex = Record.Exception(() => response = new NetInfoNotification(parents.Count, parents));

            Assert.Null(ex);

            Assert.Equal(parents.Count, response.ParentCount);
            Assert.Equal(parents, response.Parents);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation snapshots parents")]
        public void Instantiation_Snapshots_Parents()
        {
            var parents = new List<(string Username, IPAddress IPAddress, int Port)>
            {
                ("alice", IPAddress.Loopback, 1),
            };

            var response = new NetInfoNotification(parents.Count, parents);

            parents[0] = ("bob", IPAddress.Any, 2);

            var parent = Assert.Single(response.Parents);
            Assert.Equal("alice", parent.Username);
            Assert.Equal(IPAddress.Loopback, parent.IPAddress);
            Assert.Equal(1, parent.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation rejects invalid parent metadata")]
        public void Instantiation_Rejects_Invalid_Parent_Metadata()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetInfoNotification(-1, Array.Empty<(string Username, IPAddress IPAddress, int Port)>()));
            Assert.Throws<ArgumentNullException>(() => new NetInfoNotification(0, null));
            Assert.Throws<ArgumentException>(() => new NetInfoNotification(2, new[] { ("alice", IPAddress.Loopback, 1) }));
            Assert.Throws<ArgumentException>(() => new NetInfoNotification(1, new[] { ((string)null, IPAddress.Loopback, 1) }));
            Assert.Throws<ArgumentException>(() => new NetInfoNotification(1, new[] { ("alice", (IPAddress)null, 1) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetInfoNotification(1, new[] { ("alice", IPAddress.Loopback, -1) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetInfoNotification(1, new[] { ("alice", IPAddress.Loopback, 65536) }));
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => NetInfoNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .Build();

            var ex = Record.Exception(() => NetInfoNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data on failure"), AutoData]
        public void Parse_Returns_Expected_Data_On_Failure(List<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            parents = parents.ConvertAll(parent => (parent.Username, parent.IPAddress, System.Math.Abs(parent.Port % (IPEndPoint.MaxPort + 1))));

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(parents.Count);

            foreach (var parent in parents)
            {
                var ipBytes = parent.IPAddress.GetAddressBytes();
                Array.Reverse(ipBytes);

                builder
                    .WriteString(parent.Username)
                    .WriteBytes(ipBytes)
                    .WriteInteger(parent.Port);
            }

            var response = NetInfoNotification.FromByteArray(builder.Build());

            Assert.Equal(parents.Count, response.ParentCount);
            Assert.Equal(parents, response.Parents);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse throws MessageException on invalid port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Parse_Throws_MessageException_On_Invalid_Port(int port)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(1)
                .WriteString("user")
                .WriteBytes(new byte[] { 1, 0, 0, 127 })
                .WriteInteger(port)
                .Build();

            var ex = Record.Exception(() => NetInfoNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
