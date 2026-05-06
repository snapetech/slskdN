// <copyright file="RoomList.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Information about a chat room.
    /// </summary>
    public class RoomList
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomList"/> class.
        /// </summary>
        /// <param name="publicList">The list of public rooms.</param>
        /// <param name="privateList">The list of private rooms.</param>
        /// <param name="ownedList">The list of rooms owned by the currently logged in user.</param>
        /// <param name="moderatedRoomNameList">The list of room names in which the currently logged in user has moderator status.</param>
        public RoomList(
            IEnumerable<RoomInfo> publicList,
            IEnumerable<RoomInfo> privateList,
            IEnumerable<RoomInfo> ownedList,
            IEnumerable<string> moderatedRoomNameList)
        {
            var publicRooms = publicList?.ToList() ?? new List<RoomInfo>();
            var privateRooms = privateList?.ToList() ?? new List<RoomInfo>();
            var ownedRooms = ownedList?.ToList() ?? new List<RoomInfo>();
            var moderatedRoomNames = moderatedRoomNameList?.ToList() ?? new List<string>();

            if (publicRooms.Any(room => room == null))
            {
                throw new System.ArgumentException("The public room list must not contain null entries", nameof(publicList));
            }

            if (privateRooms.Any(room => room == null))
            {
                throw new System.ArgumentException("The private room list must not contain null entries", nameof(privateList));
            }

            if (ownedRooms.Any(room => room == null))
            {
                throw new System.ArgumentException("The owned room list must not contain null entries", nameof(ownedList));
            }

            if (moderatedRoomNames.Any(roomName => roomName == null))
            {
                throw new System.ArgumentException("The moderated room name list must not contain null entries", nameof(moderatedRoomNameList));
            }

            Public = publicRooms.AsReadOnly();
            PublicCount = Public.Count;

            Private = privateRooms.AsReadOnly();
            PrivateCount = Private.Count;

            Owned = ownedRooms.AsReadOnly();
            OwnedCount = Owned.Count;

            ModeratedRoomNames = moderatedRoomNames.AsReadOnly();
            ModeratedRoomNameCount = ModeratedRoomNames.Count;
        }

        /// <summary>
        ///     Gets the number of public rooms.
        /// </summary>
        public int PublicCount { get; }

        /// <summary>
        ///     Gets the number of private rooms.
        /// </summary>
        public int PrivateCount { get; }

        /// <summary>
        ///     Gets the number of rooms owned by the currently logged in user.
        /// </summary>
        public int OwnedCount { get; }

        /// <summary>
        ///     Gets the number of room names in which the currently logged in user has moderator status.
        /// </summary>
        public int ModeratedRoomNameCount { get; }

        /// <summary>
        ///     Gets the list of public rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Public { get; }

        /// <summary>
        ///     Gets the list of private rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Private { get; }

        /// <summary>
        ///     Gets the list of rooms owned by the currently logged in user.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Owned { get; }

        /// <summary>
        ///     Gets the list of room names in which the currently logged in user has moderator status.
        /// </summary>
        public IReadOnlyCollection<string> ModeratedRoomNames { get; }
    }
}
