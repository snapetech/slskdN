// <copyright file="IPEndPointExtensions.cs" company="slskdN Team">
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
    using System.Net;

    internal static class IPEndPointExtensions
    {
        internal static IPAddress Snapshot(this IPAddress address)
        {
            if (address == null)
            {
                return null;
            }

            var snapshot = new IPAddress(address.GetAddressBytes());

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                snapshot.ScopeId = address.ScopeId;
            }

            return snapshot;
        }

        internal static IPEndPoint Snapshot(this IPEndPoint endPoint)
            => endPoint == null ? null : new IPEndPoint(endPoint.Address.Snapshot(), endPoint.Port);
    }
}
