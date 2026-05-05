// <copyright file="MeshRendezvousResult.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System.Collections.Generic;

    /// <summary>
    ///     Mesh rendezvous discovery result.
    /// </summary>
    public sealed class MeshRendezvousResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MeshRendezvousResult"/> class.
        /// </summary>
        /// <param name="interestTag">The interest tag used.</param>
        /// <param name="similarUsers">The users returned by the server.</param>
        /// <param name="capabilityRecords">Known capability records after probing.</param>
        public MeshRendezvousResult(
            string interestTag,
            IReadOnlyCollection<SimilarUser> similarUsers,
            IReadOnlyCollection<PeerCapabilityRecord> capabilityRecords)
        {
            InterestTag = interestTag;
            SimilarUsers = similarUsers ?? new List<SimilarUser>().AsReadOnly();
            CapabilityRecords = capabilityRecords ?? new List<PeerCapabilityRecord>().AsReadOnly();
        }

        /// <summary>
        ///     Gets the known capability records after probing.
        /// </summary>
        public IReadOnlyCollection<PeerCapabilityRecord> CapabilityRecords { get; }

        /// <summary>
        ///     Gets the interest tag used.
        /// </summary>
        public string InterestTag { get; }

        /// <summary>
        ///     Gets the users returned by the server.
        /// </summary>
        public IReadOnlyCollection<SimilarUser> SimilarUsers { get; }
    }
}
