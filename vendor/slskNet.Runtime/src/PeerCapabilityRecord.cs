// <copyright file="PeerCapabilityRecord.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;
    using System.Net;

    /// <summary>
    ///     A known peer capability registry entry.
    /// </summary>
    public sealed class PeerCapabilityRecord
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerCapabilityRecord"/> class.
        /// </summary>
        /// <param name="username">The Soulseek username.</param>
        /// <param name="endpoint">The endpoint from which the descriptor was seen.</param>
        /// <param name="descriptor">The peer descriptor.</param>
        /// <param name="messageType">The message type that produced this record.</param>
        /// <param name="nonce">The exchange nonce.</param>
        /// <param name="observedAt">The time the descriptor was observed.</param>
        public PeerCapabilityRecord(
            string username,
            IPEndPoint endpoint,
            PeerCapabilityDescriptor descriptor,
            PeerCapabilityMessageType messageType,
            string nonce,
            DateTimeOffset observedAt)
        {
            Username = string.IsNullOrWhiteSpace(username) ? throw new ArgumentException("Username must not be empty", nameof(username)) : username;
            EndPoint = endpoint;
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            MessageType = messageType;
            Nonce = nonce;
            ObservedAt = observedAt;
        }

        /// <summary>
        ///     Gets the peer descriptor.
        /// </summary>
        public PeerCapabilityDescriptor Descriptor { get; }

        /// <summary>
        ///     Gets the endpoint from which the descriptor was seen.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        ///     Gets the message type that produced this record.
        /// </summary>
        public PeerCapabilityMessageType MessageType { get; }

        /// <summary>
        ///     Gets the exchange nonce.
        /// </summary>
        public string Nonce { get; }

        /// <summary>
        ///     Gets the time the descriptor was observed.
        /// </summary>
        public DateTimeOffset ObservedAt { get; }

        /// <summary>
        ///     Gets the Soulseek username.
        /// </summary>
        public string Username { get; }
    }
}
