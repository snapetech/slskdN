// <copyright file="ProtocolCountHardeningTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class ProtocolCountHardeningTests
    {
        [Fact(DisplayName = "Search response rejects impossible file count")]
        public void Search_Response_Rejects_Impossible_File_Count()
        {
            var bytes = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("user")
                .WriteInteger(1)
                .WriteInteger(10)
                .Compress()
                .Build();

            Assert.Throws<MessageException>(() => SearchResponseFactory.FromByteArray(bytes));
        }

        [Fact(DisplayName = "Join room rejects mismatched parallel counts")]
        public void Join_Room_Rejects_Mismatched_Parallel_Counts()
        {
            var bytes = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString("room")
                .WriteInteger(1)
                .WriteString("alice")
                .WriteInteger(0)
                .Build();

            Assert.Throws<MessageException>(() => JoinRoomResponse.FromByteArray(bytes));
        }
    }
}
