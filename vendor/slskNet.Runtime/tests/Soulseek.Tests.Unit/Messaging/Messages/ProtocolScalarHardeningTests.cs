// <copyright file="ProtocolScalarHardeningTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class ProtocolScalarHardeningTests
    {
        [Theory(DisplayName = "User statistics rejects negative counters")]
        [InlineData(-1, 1L, 1, 1)]
        [InlineData(1, -1L, 1, 1)]
        [InlineData(1, 1L, -1, 1)]
        [InlineData(1, 1L, 1, -1)]
        public void User_Statistics_Rejects_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString("user")
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            Assert.Throws<MessageException>(() => UserStatisticsResponseFactory.FromByteArray(msg));
        }

        [Theory(DisplayName = "Watch user rejects negative counters")]
        [InlineData(-1, 1L, 1, 1)]
        [InlineData(1, -1L, 1, 1)]
        [InlineData(1, 1L, -1, 1)]
        [InlineData(1, 1L, 1, -1)]
        public void Watch_User_Rejects_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString("user")
                .WriteByte(1)
                .WriteInteger((int)UserPresence.Online)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            Assert.Throws<MessageException>(() => WatchUserResponse.FromByteArray(msg));
        }

        [Theory(DisplayName = "Joined room user rejects negative counters")]
        [InlineData(-1, 1L, 1, 1, 1)]
        [InlineData(1, -1L, 1, 1, 1)]
        [InlineData(1, 1L, -1, 1, 1)]
        [InlineData(1, 1L, 1, -1, 1)]
        [InlineData(1, 1L, 1, 1, -1)]
        public void Joined_Room_User_Rejects_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount, int slotsFree)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString("room")
                .WriteString("user")
                .WriteInteger((int)UserPresence.Online)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .WriteInteger(slotsFree)
                .WriteString("US")
                .Build();

            Assert.Throws<MessageException>(() => UserJoinedRoomNotification.FromByteArray(msg));
        }

        [Theory(DisplayName = "Join room rejects negative user counters")]
        [InlineData(-1, 1L, 1, 1, 1)]
        [InlineData(1, -1L, 1, 1, 1)]
        [InlineData(1, 1L, -1, 1, 1)]
        [InlineData(1, 1L, 1, -1, 1)]
        [InlineData(1, 1L, 1, 1, -1)]
        public void Join_Room_Rejects_Negative_User_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount, int slotsFree)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString("room")
                .WriteInteger(1)
                .WriteString("user")
                .WriteInteger(1)
                .WriteInteger((int)UserPresence.Online)
                .WriteInteger(1)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .WriteInteger(1)
                .WriteInteger(slotsFree)
                .WriteInteger(1)
                .WriteString("US")
                .Build();

            Assert.Throws<MessageException>(() => JoinRoomResponse.FromByteArray(msg));
        }

        [Theory(DisplayName = "User info rejects negative queue metadata")]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public void User_Info_Rejects_Negative_Queue_Metadata(int uploadSlots, int queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("description")
                .WriteByte(0)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte(0)
                .Build();

            Assert.Throws<MessageException>(() => UserInfoResponseFactory.FromByteArray(msg));
        }

        [Fact(DisplayName = "Place in queue rejects negative position")]
        public void Place_In_Queue_Rejects_Negative_Position()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString("file")
                .WriteInteger(-1)
                .Build();

            Assert.Throws<MessageException>(() => PlaceInQueueResponse.FromByteArray(msg));
        }

        [Theory(DisplayName = "Search response rejects negative queue metadata")]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public void Search_Response_Rejects_Negative_Queue_Metadata(int uploadSpeed, int queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("user")
                .WriteInteger(1)
                .WriteInteger(0)
                .WriteByte(0)
                .WriteInteger(uploadSpeed)
                .WriteInteger(queueLength)
                .Compress()
                .Build();

            Assert.Throws<MessageException>(() => SearchResponseFactory.FromByteArray(msg));
        }
    }
}
