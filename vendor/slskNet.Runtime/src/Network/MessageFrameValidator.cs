// <copyright file="MessageFrameValidator.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
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
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Network
{
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Validates protocol frame lengths before reading frame bodies.
    /// </summary>
    internal static class MessageFrameValidator
    {
        /// <summary>
        ///     Validates a peer/server message frame length.
        /// </summary>
        /// <param name="length">The declared message body length, including the message code.</param>
        /// <param name="minimumLength">The minimum valid length for the connection type.</param>
        /// <param name="frameName">The diagnostic frame name.</param>
        public static void ValidateMessageLength(int length, int minimumLength, string frameName = "message")
        {
            if (length < minimumLength || length > RotatedObfuscation.MaxMessageLength)
            {
                throw new MessageReadException($"Invalid {frameName} length: {length}");
            }
        }

        /// <summary>
        ///     Validates an initialization frame length.
        /// </summary>
        /// <param name="length">The declared initialization body length, including the message code.</param>
        /// <param name="frameName">The diagnostic frame name.</param>
        public static void ValidateInitMessageLength(int length, string frameName = "initialization message")
        {
            if (length < 4 || length > RotatedObfuscation.MaxInitMessageLength)
            {
                throw new MessageReadException($"Invalid {frameName} length: {length}");
            }
        }
    }
}
