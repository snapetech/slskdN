// <copyright file="PeerCapabilityMessageType.cs" company="JP Dillingham">
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
    /// <summary>
    ///     slskdN peer capability exchange message type.
    /// </summary>
    public enum PeerCapabilityMessageType
    {
        /// <summary>
        ///     Capability hello.
        /// </summary>
        Hello = 1,

        /// <summary>
        ///     Capability acknowledgement.
        /// </summary>
        Acknowledgement = 2,
    }
}
