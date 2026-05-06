// <copyright file="ProtocolScalarEmissionTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using Soulseek.Messaging;
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
            () => new PeerSearchRequest(-1, "query"),
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
            () => new ServerSearchRequest("user", -1, "query"),
            () => new UserSearchRequest("user", "query", -1),
            () => new RoomSearchRequest("room", "query", -1),
            () => new WishlistSearchRequest("query", -1),
        };

        public static TheoryData<Action> OutboundStringCommands => new TheoryData<Action>
        {
            () => new PeerInit(null, Constants.ConnectionType.Peer, 1),
            () => new PeerInit("user", null, 1),
            () => new DistributedBranchRoot(null),
            () => new DistributedSearchRequest(null, 1, "query"),
            () => new DistributedSearchRequest("user", 1, null),
            () => new FolderContentsRequest(1, null),
            () => new FolderContentsResponse(1, null, Array.Empty<Directory>()),
            () => new PlaceInQueueRequest(null),
            () => new PlaceInQueueResponse(null, 1),
            () => new QueueDownloadRequest(null),
            () => new TransferRequest(TransferDirection.Download, 1, null),
            () => new TransferResponse(1, null),
            () => new UploadDenied(null, "denied"),
            () => new UploadDenied("file", null),
            () => new UploadFailed(null),
            () => new PeerSearchRequest(1, null),
            () => new ServerSearchRequest(null, 1, "query"),
            () => new ServerSearchRequest("user", 1, null),
            () => new BranchRootCommand(null),
            () => new ConnectToPeerRequest(1, null, Constants.ConnectionType.Peer),
            () => new ConnectToPeerRequest(1, "user", null),
            () => new GivePrivilegesCommand(null, 1),
            () => new InterestCommand(MessageCode.Server.InterestAdd, null),
            () => new ItemRecommendationsRequest(MessageCode.Server.GetItemRecommendations, null),
            () => new JoinRoomRequest(null),
            () => new LeaveRoomRequest(null),
            () => new LoginRequest(1, null, "password"),
            () => new LoginRequest(1, "user", null),
            () => new NewPassword(null),
            () => new PrivateMessageCommand(null, "message"),
            () => new PrivateMessageCommand("user", null),
            () => new PrivateRoomAddOperator(null, "user"),
            () => new PrivateRoomAddOperator("room", null),
            () => new PrivateRoomAddUser(null, "user"),
            () => new PrivateRoomAddUser("room", null),
            () => new PrivateRoomDropMembershipCommand(null),
            () => new PrivateRoomDropOwnershipCommand(null),
            () => new PrivateRoomRemoveOperator(null, "user"),
            () => new PrivateRoomRemoveOperator("room", null),
            () => new PrivateRoomRemoveUser(null, "user"),
            () => new PrivateRoomRemoveUser("room", null),
            () => new RoomMessageCommand(null, "message"),
            () => new RoomMessageCommand("room", null),
            () => new RoomSearchRequest(null, "query", 1),
            () => new RoomSearchRequest("room", null, 1),
            () => new SearchRequest(null, 1),
            () => new SetRoomTickerCommand(null, "message"),
            () => new SetRoomTickerCommand("room", null),
            () => new UserAddressRequest(null),
            () => new UserInterestsRequest(null),
            () => new UserPrivilegesRequest(null),
            () => new UserSearchRequest(null, "query", 1),
            () => new UserSearchRequest("user", null, 1),
            () => new UserStatisticsRequest(null),
            () => new UserStatusRequest(null),
            () => new UnwatchUserCommand(null),
            () => new WatchUserRequest(null),
            () => new WishlistSearchRequest(null, 1),
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

        [Theory(DisplayName = "Outbound string commands reject null values before emission")]
        [MemberData(nameof(OutboundStringCommands))]
        public void Outbound_String_Commands_Reject_Null_Values_Before_Emission(Action action)
        {
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
