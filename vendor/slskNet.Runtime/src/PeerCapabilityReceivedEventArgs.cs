// <copyright file="PeerCapabilityReceivedEventArgs.cs" company="slskdN Team">
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
    ///     Event arguments for a received peer capability descriptor.
    /// </summary>
    public sealed class PeerCapabilityReceivedEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerCapabilityReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="record">The updated registry record.</param>
        public PeerCapabilityReceivedEventArgs(PeerCapabilityRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
        }

        /// <summary>
        ///     Gets the updated registry record.
        /// </summary>
        public PeerCapabilityRecord Record { get; }
    }
}
