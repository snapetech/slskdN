// <copyright file="DomainModelValidationTests.cs" company="slskdN Team">
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
    using System.Linq;
    using System.Net;
    using Xunit;

    public class DomainModelValidationTests
    {
        [Fact(DisplayName = "File rejects negative size")]
        public void File_Rejects_Negative_Size()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new File(1, "file.mp3", -1, ".mp3"));

        [Fact(DisplayName = "File rejects null attributes")]
        public void File_Rejects_Null_Attributes()
            => Assert.Throws<ArgumentException>(() => new File(1, "file.mp3", 1, ".mp3", new FileAttribute[] { null }));

        [Fact(DisplayName = "Directory rejects null files")]
        public void Directory_Rejects_Null_Files()
            => Assert.Throws<ArgumentException>(() => new Directory("dir", new File[] { null }));

        [Theory(DisplayName = "BrowseResponse rejects null directories")]
        [InlineData(false)]
        [InlineData(true)]
        public void BrowseResponse_Rejects_Null_Directories(bool locked)
        {
            var directories = new Directory[] { null };

            if (locked)
            {
                Assert.Throws<ArgumentException>(() => new BrowseResponse(lockedDirectoryList: directories));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new BrowseResponse(directoryList: directories));
            }
        }

        [Theory(DisplayName = "SearchResponse rejects negative peer metadata")]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        public void SearchResponse_Rejects_Negative_Peer_Metadata(int uploadSpeed, int queueLength)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new SearchResponse("user", 1, true, uploadSpeed, queueLength, null));

        [Theory(DisplayName = "SearchResponse rejects null files")]
        [InlineData(false)]
        [InlineData(true)]
        public void SearchResponse_Rejects_Null_Files(bool locked)
        {
            var files = new File[] { null };

            if (locked)
            {
                Assert.Throws<ArgumentException>(() => new SearchResponse("user", 1, true, 0, 0, null, files));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new SearchResponse("user", 1, true, 0, 0, files));
            }
        }

        [Theory(DisplayName = "UserInfo rejects negative peer metadata")]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        public void UserInfo_Rejects_Negative_Peer_Metadata(int uploadSlots, int queueLength)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new UserInfo("description", uploadSlots, queueLength, false));

        [Theory(DisplayName = "UserData rejects invalid peer metadata")]
        [InlineData((UserPresence)99, 0, 0L, 0, 0, null)]
        [InlineData(UserPresence.Online, -1, 0L, 0, 0, null)]
        [InlineData(UserPresence.Online, 0, -1L, 0, 0, null)]
        [InlineData(UserPresence.Online, 0, 0L, -1, 0, null)]
        [InlineData(UserPresence.Online, 0, 0L, 0, -1, null)]
        [InlineData(UserPresence.Online, 0, 0L, 0, 0, -1)]
        public void UserData_Rejects_Invalid_Peer_Metadata(
            UserPresence status,
            int averageSpeed,
            long uploadCount,
            int fileCount,
            int directoryCount,
            int? slotsFree)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new UserData("user", status, averageSpeed, uploadCount, fileCount, directoryCount, "US", slotsFree));

        [Theory(DisplayName = "UserStatistics rejects negative peer metadata")]
        [InlineData(-1, 0L, 0, 0)]
        [InlineData(0, -1L, 0, 0)]
        [InlineData(0, 0L, -1, 0)]
        [InlineData(0, 0L, 0, -1)]
        public void UserStatistics_Rejects_Negative_Peer_Metadata(int averageSpeed, long uploadCount, int fileCount, int directoryCount)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new UserStatistics("user", averageSpeed, uploadCount, fileCount, directoryCount));

        [Theory(DisplayName = "Transfer rejects invalid progress metadata")]
        [InlineData((TransferDirection)99, 1, 0, 0, 0)]
        [InlineData(TransferDirection.Download, -1, 0, 0, 0)]
        [InlineData(TransferDirection.Download, 1, -1, 0, 0)]
        [InlineData(TransferDirection.Download, 1, 2, 0, 0)]
        [InlineData(TransferDirection.Download, 1, 0, -1, 0)]
        [InlineData(TransferDirection.Download, 1, 0, 2, 0)]
        [InlineData(TransferDirection.Download, 1, 0, 0, -1)]
        public void Transfer_Rejects_Invalid_Progress_Metadata(
            TransferDirection direction,
            long size,
            long startOffset,
            long bytesTransferred,
            double averageSpeed)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(
                direction,
                "user",
                "file.mp3",
                1,
                TransferStates.None,
                size,
                startOffset,
                bytesTransferred,
                averageSpeed));

        [Fact(DisplayName = "Transfer rejects invalid state flags")]
        public void Transfer_Rejects_Invalid_State_Flags()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Transfer(
                TransferDirection.Download,
                "user",
                "file.mp3",
                1,
                (TransferStates)8192,
                1,
                0));

        [Theory(DisplayName = "Search rejects invalid counters and states")]
        [InlineData((SearchStates)512, 0, 0, 0)]
        [InlineData(SearchStates.InProgress, -1, 0, 0)]
        [InlineData(SearchStates.InProgress, 0, -1, 0)]
        [InlineData(SearchStates.InProgress, 0, 0, -1)]
        public void Search_Rejects_Invalid_Counters_And_States(SearchStates state, int responseCount, int fileCount, int lockedFileCount)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Search(SearchQuery.FromText("query"), SearchScope.Network, 1, state, responseCount, fileCount, lockedFileCount));

        [Fact(DisplayName = "UserStatus rejects invalid presence")]
        public void UserStatus_Rejects_Invalid_Presence()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new UserStatus("user", (UserPresence)99, false));

        [Fact(DisplayName = "RoomInfo rejects negative user count")]
        public void RoomInfo_Rejects_Negative_User_Count()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new RoomInfo("room", -1));

        [Theory(DisplayName = "Distributed network info rejects invalid topology metadata")]
        [InlineData(-0.1d, 0, 0)]
        [InlineData(0d, -1, 0)]
        [InlineData(0d, 0, -1)]
        public void DistributedNetworkInfo_Rejects_Invalid_Topology_Metadata(double averageBroadcastLatency, int branchLevel, int childLimit)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DistributedNetworkInfo(
                averageBroadcastLatency,
                branchLevel,
                "root",
                false,
                childLimit,
                true,
                null,
                default,
                false));

        [Fact(DisplayName = "Distributed network info snapshots endpoints")]
        public void DistributedNetworkInfo_Snapshots_Endpoints()
        {
            var childEndpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var parentEndpoint = new IPEndPoint(IPAddress.Loopback, 2345);
            var info = new DistributedNetworkInfo(
                0,
                1,
                "root",
                false,
                1,
                true,
                new[] { ("child", childEndpoint) },
                ("parent", parentEndpoint),
                true);

            childEndpoint.Port = 4321;
            parentEndpoint.Port = 5432;
            info.Children.Single().IPEndPoint.Port = 1111;
            info.Parent.IPEndPoint.Port = 2222;

            Assert.Equal(1234, info.Children.Single().IPEndPoint.Port);
            Assert.Equal(2345, info.Parent.IPEndPoint.Port);
        }

        [Fact(DisplayName = "Distributed parent event args reject negative branch level")]
        public void DistributedParentEventArgs_Rejects_Negative_Branch_Level()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new DistributedParentEventArgs("user", null, -1, "root"));

        [Theory(DisplayName = "Browse progress event args reject invalid progress metadata")]
        [InlineData(-1, 1)]
        [InlineData(2, 1)]
        public void BrowseProgressEventArgs_Reject_Invalid_Progress_Metadata(long bytesTransferred, long size)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new BrowseProgressUpdatedEventArgs("user", bytesTransferred, size));

        [Theory(DisplayName = "File attribute rejects invalid metadata")]
        [InlineData((FileAttributeType)99, 0)]
        [InlineData(FileAttributeType.BitRate, -1)]
        public void FileAttribute_Rejects_Invalid_Metadata(FileAttributeType type, int value)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new FileAttribute(type, value));

        [Fact(DisplayName = "Diagnostic event args reject invalid level")]
        public void DiagnosticEventArgs_Rejects_Invalid_Level()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new Diagnostics.DiagnosticEventArgs((Diagnostics.DiagnosticLevel)99, "message"));

        [Theory(DisplayName = "Client state changed event args reject invalid flags")]
        [InlineData((SoulseekClientStates)64, SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.Connected, (SoulseekClientStates)64)]
        public void SoulseekClientStateChangedEventArgs_Rejects_Invalid_Flags(SoulseekClientStates previousState, SoulseekClientStates state)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new SoulseekClientStateChangedEventArgs(previousState, state));

        [Theory(DisplayName = "RoomList rejects null entries")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("owned")]
        [InlineData("moderated")]
        public void RoomList_Rejects_Null_Entries(string list)
        {
            var roomInfo = new RoomInfo("room", 0);

            Assert.Throws<ArgumentException>(() => new RoomList(
                list == "public" ? new RoomInfo[] { null } : new[] { roomInfo },
                list == "private" ? new RoomInfo[] { null } : new[] { roomInfo },
                list == "owned" ? new RoomInfo[] { null } : new[] { roomInfo },
                list == "moderated" ? new string[] { null } : new[] { "room" }));
        }

        [Theory(DisplayName = "RoomData rejects null entries")]
        [InlineData(false)]
        [InlineData(true)]
        public void RoomData_Rejects_Null_Entries(bool operators)
        {
            if (operators)
            {
                Assert.Throws<ArgumentException>(() => new RoomData("room", null, isPrivate: true, operatorList: new string[] { null }));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new RoomData("room", new UserData[] { null }));
            }
        }

        [Fact(DisplayName = "RoomTickerListReceivedEventArgs rejects null tickers")]
        public void RoomTickerListReceivedEventArgs_Rejects_Null_Tickers()
            => Assert.Throws<ArgumentException>(() => new RoomTickerListReceivedEventArgs("room", new RoomTicker[] { null }));

        [Fact(DisplayName = "ItemSimilarUsers rejects null usernames")]
        public void ItemSimilarUsers_Rejects_Null_Usernames()
            => Assert.Throws<ArgumentException>(() => new ItemSimilarUsers("item", new string[] { null }));

        [Theory(DisplayName = "RecommendationList rejects null entries")]
        [InlineData(false)]
        [InlineData(true)]
        public void RecommendationList_Rejects_Null_Entries(bool unrecommendations)
        {
            if (unrecommendations)
            {
                Assert.Throws<ArgumentException>(() => new RecommendationList(null, new Recommendation[] { null }));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new RecommendationList(new Recommendation[] { null }, null));
            }
        }

        [Theory(DisplayName = "MeshRendezvousResult rejects null entries")]
        [InlineData(false)]
        [InlineData(true)]
        public void MeshRendezvousResult_Rejects_Null_Entries(bool records)
        {
            if (records)
            {
                Assert.Throws<ArgumentException>(() => new MeshRendezvousResult("tag", null, new PeerCapabilityRecord[] { null }));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new MeshRendezvousResult("tag", new SimilarUser[] { null }, null));
            }
        }

        [Theory(DisplayName = "MeshRendezvousResult rejects empty interest tags")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void MeshRendezvousResult_Rejects_Empty_Interest_Tags(string interestTag)
            => Assert.Throws<ArgumentException>(() => new MeshRendezvousResult(interestTag, null, null));

        [Theory(DisplayName = "UserInterests rejects null entries")]
        [InlineData(false)]
        [InlineData(true)]
        public void UserInterests_Rejects_Null_Entries(bool hated)
        {
            if (hated)
            {
                Assert.Throws<ArgumentException>(() => new UserInterests("user", null, new string[] { null }));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => new UserInterests("user", new string[] { null }, null));
            }
        }

        [Fact(DisplayName = "ItemRecommendations rejects null recommendations")]
        public void ItemRecommendations_Rejects_Null_Recommendations()
            => Assert.Throws<ArgumentException>(() => new ItemRecommendations("item", new Recommendation[] { null }));

        [Fact(DisplayName = "RoomInfo rejects null users")]
        public void RoomInfo_Rejects_Null_Users()
            => Assert.Throws<ArgumentException>(() => new RoomInfo("room", new string[] { null }));

        [Fact(DisplayName = "WishlistSearchCompletedEventArgs rejects null responses")]
        public void WishlistSearchCompletedEventArgs_Rejects_Null_Responses()
            => Assert.Throws<ArgumentException>(() => new WishlistSearchCompletedEventArgs("term", null, new SearchResponse[] { null }, null));
    }
}
