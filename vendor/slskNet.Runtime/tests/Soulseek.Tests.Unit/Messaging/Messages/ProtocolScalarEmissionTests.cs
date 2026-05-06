// <copyright file="ProtocolScalarEmissionTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class ProtocolScalarEmissionTests
    {
        public static TheoryData<Action> PeerAndInitializationScalarCommands => new TheoryData<Action>
        {
            () => new FolderContentsRequest(-1, "dir"),
            () => new FolderContentsResponse(-1, "dir", Array.Empty<Directory>()),
            () => new TransferRequest(TransferDirection.Download, -1, "file.mp3"),
            () => new TransferResponse(-1),
            () => new TransferResponse(-1, 1L),
            () => new TransferResponse(-1, "denied"),
            () => new PeerInit("user", Constants.ConnectionType.Peer, -1),
            () => new PierceFirewall(-1),
            () => new DistributedPingResponse(-1),
            () => new DistributedSearchRequest("user", -1, "query"),
        };

        public static TheoryData<Action> ServerScalarCommands => new TheoryData<Action>
        {
            () => new AcknowledgePrivateMessageCommand(-1),
            () => new AcknowledgePrivilegeNotificationCommand(-1),
            () => new CannotConnect(-1),
            () => new ConnectToPeerRequest(-1, "user", Constants.ConnectionType.Peer),
            () => new GivePrivilegesCommand("user", 0),
            () => new LoginRequest(-1, "user", "password"),
            () => new SearchRequest("query", -1),
            () => new UserSearchRequest("user", "query", -1),
            () => new RoomSearchRequest("room", "query", -1),
            () => new WishlistSearchRequest("query", -1),
        };

        [Theory(DisplayName = "Outbound server scalar commands reject invalid values before emission")]
        [MemberData(nameof(ServerScalarCommands))]
        public void Outbound_Server_Scalar_Commands_Reject_Invalid_Values_Before_Emission(Action action)
        {
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Theory(DisplayName = "Outbound peer and initialization scalar commands reject invalid tokens before emission")]
        [MemberData(nameof(PeerAndInitializationScalarCommands))]
        public void Outbound_Peer_And_Initialization_Scalar_Commands_Reject_Invalid_Tokens_Before_Emission(Action action)
        {
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }
}
