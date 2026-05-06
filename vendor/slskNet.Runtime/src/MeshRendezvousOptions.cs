// <copyright file="MeshRendezvousOptions.cs" company="slskdN Team">
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

    /// <summary>
    ///     Options for native Soulseek mesh rendezvous helpers.
    /// </summary>
    public sealed class MeshRendezvousOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MeshRendezvousOptions"/> class.
        /// </summary>
        /// <param name="interestTag">The public interest tag.</param>
        /// <param name="probePeerCapabilities">A value indicating whether to send capability probes to discovered users.</param>
        public MeshRendezvousOptions(string interestTag = SoulseekClient.MeshRendezvousInterestTag, bool probePeerCapabilities = false)
        {
            InterestTag = string.IsNullOrWhiteSpace(interestTag) ? throw new ArgumentException("Interest tag must not be empty.", nameof(interestTag)) : interestTag;
            ProbePeerCapabilities = probePeerCapabilities;
        }

        /// <summary>
        ///     Gets the public interest tag.
        /// </summary>
        public string InterestTag { get; }

        /// <summary>
        ///     Gets a value indicating whether to send capability probes to discovered users.
        /// </summary>
        public bool ProbePeerCapabilities { get; }
    }
}
