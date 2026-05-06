// <copyright file="IPeerDescriptorVerifier.cs" company="slskdN Team">
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
    /// <summary>
    ///     Verifies peer capability descriptor signatures.
    /// </summary>
    public interface IPeerDescriptorVerifier
    {
        /// <summary>
        ///     Verifies a descriptor signature.
        /// </summary>
        /// <param name="descriptor">The descriptor to verify.</param>
        /// <returns>A value indicating whether the signature is valid.</returns>
        bool Verify(PeerCapabilityDescriptor descriptor);
    }
}
