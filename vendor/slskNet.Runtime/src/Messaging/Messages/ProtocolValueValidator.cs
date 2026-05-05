// <copyright file="ProtocolValueValidator.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System.Net;

    /// <summary>
    ///     Validates scalar protocol values.
    /// </summary>
    internal static class ProtocolValueValidator
    {
        /// <summary>
        ///     Validates that a protocol port is in the TCP/UDP port range.
        /// </summary>
        /// <param name="port">The port to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidatePort(int port, string fieldName)
        {
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new MessageException($"Invalid {fieldName} port: {port}");
            }
        }
    }
}
