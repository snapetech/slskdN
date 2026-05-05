// <copyright file="DomainModelValidationTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using Xunit;

    public class DomainModelValidationTests
    {
        [Fact(DisplayName = "File rejects negative size")]
        public void File_Rejects_Negative_Size()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new File(1, "file.mp3", -1, ".mp3"));

        [Theory(DisplayName = "SearchResponse rejects negative peer metadata")]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        public void SearchResponse_Rejects_Negative_Peer_Metadata(int uploadSpeed, int queueLength)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new SearchResponse("user", 1, true, uploadSpeed, queueLength, null));

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
    }
}
