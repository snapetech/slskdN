// <copyright file="PeerCapabilityRegistry.cs" company="slskdN Team">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    ///     Thread-safe registry of peer capability descriptors observed on peer-message connections.
    /// </summary>
    public sealed class PeerCapabilityRegistry
    {
        private readonly ConcurrentDictionary<string, PeerCapabilityRecord> records =
            new ConcurrentDictionary<string, PeerCapabilityRecord>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Occurs when a peer capability record is updated.
        /// </summary>
        public event EventHandler<PeerCapabilityReceivedEventArgs> Updated;

        /// <summary>
        ///     Gets a snapshot of known peers.
        /// </summary>
        public IReadOnlyCollection<PeerCapabilityRecord> Records => records.Values.OrderBy(r => r.Username).ToList().AsReadOnly();

        /// <summary>
        ///     Clears all known peers.
        /// </summary>
        public void Clear()
        {
            records.Clear();
        }

        /// <summary>
        ///     Gets a record by username, if one exists.
        /// </summary>
        /// <param name="username">The Soulseek username.</param>
        /// <param name="record">The matching record.</param>
        /// <returns>A value indicating whether a record was found.</returns>
        public bool TryGet(string username, out PeerCapabilityRecord record)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                record = null;
                return false;
            }

            return records.TryGetValue(username, out record);
        }

        /// <summary>
        ///     Adds or updates a peer record.
        /// </summary>
        /// <param name="username">The Soulseek username.</param>
        /// <param name="endpoint">The endpoint from which the descriptor was seen.</param>
        /// <param name="envelope">The received capability envelope.</param>
        /// <returns>The updated record.</returns>
        public PeerCapabilityRecord Update(string username, IPEndPoint endpoint, PeerCapabilityEnvelope envelope)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username must not be empty.", nameof(username));
            }

            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            var record = new PeerCapabilityRecord(
                username,
                endpoint,
                envelope.Descriptor,
                envelope.MessageType,
                envelope.Nonce,
                DateTimeOffset.UtcNow);

            records.AddOrUpdate(username, record, (_, __) => record);
            Updated?.Invoke(this, new PeerCapabilityReceivedEventArgs(record));
            return record;
        }
    }
}
