// <copyright file="WebApiTrackerTests.cs" company="slskdN Team">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using WebAPI;
    using WebAPI.Entities;
    using WebAPI.Trackers;
    using Xunit;

    public class WebApiTrackerTests
    {
        [Theory(DisplayName = "RoomTracker rejects invalid message limit")]
        [InlineData(0)]
        [InlineData(-1)]
        public void RoomTracker_Rejects_Invalid_Message_Limit(int messageLimit)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new RoomTracker(messageLimit));

        [Fact(DisplayName = "RoomTracker normalizes missing message list")]
        public void RoomTracker_Normalizes_Missing_Message_List()
        {
            var tracker = new RoomTracker(messageLimit: 1);
            tracker.TryAdd("room", new Room { Messages = null });

            tracker.AddOrUpdateMessage("room", new RoomMessage { RoomName = "room", Username = "user", Message = "message" });

            Assert.True(tracker.TryGet("room", out var room));
            Assert.Single(room.Messages);
        }

        [Fact(DisplayName = "RoomTracker normalizes missing user list")]
        public void RoomTracker_Normalizes_Missing_User_List()
        {
            var tracker = new RoomTracker();
            tracker.TryAdd("room", new Room { Users = null });

            tracker.TryAddUser("room", new UserData("user", UserPresence.Online, 0, 0, 0, 0, "US"));

            Assert.True(tracker.TryGet("room", out var room));
            Assert.Single(room.Users);
        }

        [Fact(DisplayName = "RoomTracker tolerates missing user list when removing users")]
        public void RoomTracker_Tolerates_Missing_User_List_When_Removing_Users()
        {
            var tracker = new RoomTracker();
            tracker.TryAdd("room", new Room { Users = null });

            tracker.TryRemoveUser("room", "user");

            Assert.True(tracker.TryGet("room", out var room));
            Assert.Null(room.Users);
        }

        [Fact(DisplayName = "RoomTracker rejects null payloads")]
        public void RoomTracker_Rejects_Null_Payloads()
        {
            var tracker = new RoomTracker();

            Assert.Throws<ArgumentNullException>(() => tracker.TryAdd("room", null));
            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdateMessage("room", null));
            Assert.Throws<ArgumentNullException>(() => tracker.TryAddUser("room", null));
        }

        [Fact(DisplayName = "ConversationTracker rejects null messages")]
        public void ConversationTracker_Rejects_Null_Messages()
        {
            var tracker = new ConversationTracker();

            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
        }

        [Fact(DisplayName = "ConversationTracker normalizes null message lists")]
        public void ConversationTracker_Normalizes_Null_Message_Lists()
        {
            var tracker = new ConversationTracker();
            tracker.Conversations.TryAdd("user", null);

            tracker.AddOrUpdate("user", new PrivateMessage { Username = "user", Message = "message" });

            Assert.True(tracker.TryGet("user", out var messages));
            Assert.Single(messages);
        }

        [Fact(DisplayName = "BrowseTracker rejects null progress")]
        public void BrowseTracker_Rejects_Null_Progress()
        {
            var tracker = new BrowseTracker();

            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
        }
    }
}
