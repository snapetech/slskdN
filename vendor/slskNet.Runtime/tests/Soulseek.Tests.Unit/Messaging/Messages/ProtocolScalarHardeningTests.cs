// <copyright file="ProtocolScalarHardeningTests.cs" company="slskdN Team">
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
    using System.Collections.Generic;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class ProtocolScalarHardeningTests
    {
        // Outgoing message constructors validate locally-generated tokens. These remain strict.
        public static IEnumerable<object[]> NegativeTokenConstructors()
        {
            yield return new object[] { "CannotConnect", new Action(() => new CannotConnect(-1, "user")) };
            yield return new object[] { "ConnectToPeerRequest", new Action(() => new ConnectToPeerRequest(-1, "user", "P")) };
            yield return new object[] { "DistributedPingResponse", new Action(() => new DistributedPingResponse(-1)) };
            yield return new object[] { "DistributedSearchRequest", new Action(() => new DistributedSearchRequest("user", -1, "query")) };
            yield return new object[] { "FolderContentsRequest", new Action(() => new FolderContentsRequest(-1, "dir")) };
            yield return new object[] { "FolderContentsResponse", new Action(() => new FolderContentsResponse(-1, "dir", Array.Empty<Soulseek.Directory>())) };
            yield return new object[] { "LoginRequest", new Action(() => new LoginRequest(-1, "user", "pass")) };
            yield return new object[] { "PeerInit", new Action(() => new PeerInit("user", "P", -1)) };
            yield return new object[] { "PeerSearchRequest", new Action(() => new PeerSearchRequest(-1, "query")) };
            yield return new object[] { "PierceFirewall", new Action(() => new PierceFirewall(-1)) };
            yield return new object[] { "RoomSearchRequest", new Action(() => new RoomSearchRequest("room", "query", -1)) };
            yield return new object[] { "SearchRequest", new Action(() => new SearchRequest("query", -1)) };
            yield return new object[] { "ServerSearchRequest", new Action(() => new ServerSearchRequest("user", -1, "query")) };
            yield return new object[] { "TransferRequest", new Action(() => new TransferRequest(TransferDirection.Download, -1, "file", 0)) };
            yield return new object[] { "TransferResponse denied", new Action(() => new TransferResponse(-1, "denied")) };
            yield return new object[] { "TransferResponse allowed with size", new Action(() => new TransferResponse(-1, 0L)) };
            yield return new object[] { "TransferResponse allowed", new Action(() => new TransferResponse(-1)) };
            yield return new object[] { "UserSearchRequest", new Action(() => new UserSearchRequest("user", "query", -1)) };
            yield return new object[] { "WishlistSearchRequest", new Action(() => new WishlistSearchRequest("query", -1)) };
        }

        [Theory(DisplayName = "Outgoing message constructors reject negative tokens")]
        [MemberData(nameof(NegativeTokenConstructors))]
        public void Outgoing_Message_Constructors_Reject_Negative_Tokens(string caseName, Action constructor)
        {
            Assert.NotNull(caseName);
            Assert.Throws<ArgumentOutOfRangeException>(constructor);
        }

        [Fact(DisplayName = "Peer search request rejects negative token")]
        public void Peer_Search_Request_Rejects_Negative_Token()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(-1)
                .WriteString("query")
                .Build();

            Assert.Throws<ArgumentOutOfRangeException>(() => PeerSearchRequest.FromByteArray(msg));
        }

        [Fact(DisplayName = "Server search request rejects negative token")]
        public void Server_Search_Request_Rejects_Negative_Token()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteString("user")
                .WriteInteger(-1)
                .WriteString("query")
                .Build();

            Assert.Throws<ArgumentOutOfRangeException>(() => ServerSearchRequest.FromByteArray(msg));
        }

        // The remainder of these tests document the LENIENT-on-the-wire policy. Real Soulseek peers
        // and servers send -1 for unknown counts/speeds/queues, undefined enum values for newer
        // attribute types (e.g. FLAC bit depth), and non-0/1 bytes in flag fields. The runtime
        // accepts these without throwing so a single misformatted field doesn't drop the entire
        // message and silently break browse, search, room joins, etc.

        [Theory(DisplayName = "User statistics accepts negative counters from the wire")]
        [InlineData(-1, 1L, 1, 1)]
        [InlineData(1, -1L, 1, 1)]
        [InlineData(1, 1L, -1, 1)]
        [InlineData(1, 1L, 1, -1)]
        public void User_Statistics_Accepts_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString("user")
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            var ex = Record.Exception(() => UserStatisticsResponseFactory.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Theory(DisplayName = "Watch user accepts negative counters from the wire")]
        [InlineData(-1, 1L, 1, 1)]
        [InlineData(1, -1L, 1, 1)]
        [InlineData(1, 1L, -1, 1)]
        [InlineData(1, 1L, 1, -1)]
        public void Watch_User_Accepts_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount)
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

            var ex = Record.Exception(() => WatchUserResponse.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Theory(DisplayName = "Joined room user accepts negative counters from the wire")]
        [InlineData(-1, 1L, 1, 1, 1)]
        [InlineData(1, -1L, 1, 1, 1)]
        [InlineData(1, 1L, -1, 1, 1)]
        [InlineData(1, 1L, 1, -1, 1)]
        [InlineData(1, 1L, 1, 1, -1)]
        public void Joined_Room_User_Accepts_Negative_Counters(int averageSpeed, long uploadCount, int fileCount, int directoryCount, int slotsFree)
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

            var ex = Record.Exception(() => UserJoinedRoomNotification.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Theory(DisplayName = "User info accepts negative queue metadata from the wire")]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public void User_Info_Accepts_Negative_Queue_Metadata(int uploadSlots, int queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("description")
                .WriteByte(0)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte(0)
                .Build();

            var ex = Record.Exception(() => UserInfoResponseFactory.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "Place in queue accepts negative position from the wire")]
        public void Place_In_Queue_Accepts_Negative_Position()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString("file")
                .WriteInteger(-1)
                .Build();

            var response = PlaceInQueueResponse.FromByteArray(msg);
            Assert.Equal(-1, response.PlaceInQueue);
        }

        [Theory(DisplayName = "Search response accepts negative queue metadata from the wire")]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public void Search_Response_Accepts_Negative_Queue_Metadata(int uploadSpeed, int queueLength)
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

            var ex = Record.Exception(() => SearchResponseFactory.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "User status accepts undefined presence values from the wire")]
        public void User_Status_Accepts_Undefined_Presence()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString("user")
                .WriteInteger(99)
                .WriteByte(0)
                .Build();

            var ex = Record.Exception(() => UserStatusResponseFactory.FromByteArray(msg));
            Assert.Null(ex);
        }

        [Fact(DisplayName = "User status accepts non-0/1 privileged flag")]
        public void User_Status_Accepts_NonStandard_Privileged_Flag()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString("user")
                .WriteInteger((int)UserPresence.Online)
                .WriteByte(2)
                .Build();

            var ex = Record.Exception(() => UserStatusResponseFactory.FromByteArray(msg));
            Assert.Null(ex);
        }
    }
}
