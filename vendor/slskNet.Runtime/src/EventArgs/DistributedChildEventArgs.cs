// <copyright file="DistributedChildEventArgs.cs" company="JP Dillingham">
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
    using System.Net;

    /// <summary>
    ///     Event arguments for the event raised when a distributed child connection changes.
    /// </summary>
    public class DistributedChildEventArgs : SoulseekClientEventArgs
    {
        private readonly IPEndPoint ipEndPoint;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedChildEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the connection.</param>
        /// <param name="ipEndPoint">The IP endpoint of the connection.</param>
        public DistributedChildEventArgs(string username, IPEndPoint ipEndPoint)
        {
            Username = username;
            this.ipEndPoint = ipEndPoint.Snapshot();
        }

        /// <summary>
        ///     Gets the IP endpoint of the connection.
        /// </summary>
        public IPEndPoint IPEndPoint => ipEndPoint.Snapshot();

        /// <summary>
        ///     Gets the username associated with the connection.
        /// </summary>
        public string Username { get; }
    }
}
