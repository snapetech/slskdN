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
    using System;
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

        /// <summary>
        ///     Validates that an advertised protocol port is usable for outbound connections.
        /// </summary>
        /// <param name="port">The port to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidateAdvertisedPort(int port, string fieldName)
        {
            if (port <= IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new MessageException($"Invalid {fieldName} port: {port}");
            }
        }

        /// <summary>
        ///     Validates that a protocol scalar is not negative.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidateNonNegative(int value, string fieldName)
        {
            if (value < 0)
            {
                throw new MessageException($"Invalid {fieldName}: {value}");
            }
        }

        /// <summary>
        ///     Validates that a protocol scalar is not negative.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidateNonNegative(long value, string fieldName)
        {
            if (value < 0)
            {
                throw new MessageException($"Invalid {fieldName}: {value}");
            }
        }

        /// <summary>
        ///     Validates that a protocol enum value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The enum value to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidateDefinedEnum<TEnum>(TEnum value, string fieldName)
            where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new MessageException($"Invalid {fieldName}: {value}");
            }
        }

        /// <summary>
        ///     Validates that a protocol byte is a boolean flag.
        /// </summary>
        /// <param name="value">The flag value to validate.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        public static void ValidateBooleanFlag(int value, string fieldName)
        {
            if (value != 0 && value != 1)
            {
                throw new MessageException($"Invalid {fieldName}: {value}");
            }
        }
    }
}
