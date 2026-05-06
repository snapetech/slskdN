// <copyright file="IPeerMessageHandler.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from peer connections.
    /// </summary>
    internal interface IPeerMessageHandler : IMessageHandler
    {
        /// <summary>
        ///     Occurs when a user reports that a download has been denied.
        /// </summary>
        event EventHandler<DownloadDeniedEventArgs> DownloadDenied;

        /// <summary>
        ///     Occurs when a user reports that a download has failed.
        /// </summary>
        event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        ///     Registers a handler for custom peer message codes.
        /// </summary>
        /// <param name="messageCode">The peer message code.</param>
        /// <param name="handler">A handler invoked with sender username, sender endpoint, and peer payload.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="messageCode"/> is less than zero.</exception>
        void RegisterPeerMessageHandler(int messageCode, Func<string, IPEndPoint, byte[], Task> handler);

        /// <summary>
        ///     Unregisters a custom peer message handler.
        /// </summary>
        /// <param name="messageCode">The peer message code.</param>
        /// <returns>A value indicating whether a handler was removed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="messageCode"/> is less than zero.</exception>
        bool UnregisterPeerMessageHandler(int messageCode);

        /// <summary>
        ///     Handles the receipt of incoming messages, prior to the body having been read and parsed.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="args">The message receipt event args.</param>
        void HandleMessageReceived(object sender, MessageReceivedEventArgs args);
    }
}
