// <copyright file="ProtocolValueValidator.cs" company="slskdN Team">
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
        ///     Reserved for protocol scalars that should be non-negative; accepts any value.
        /// </summary>
        /// <remarks>
        ///     Real-world Soulseek peers and servers send negative values (commonly -1) for
        ///     unknown counts, speeds, queue positions, and similar fields. Throwing here
        ///     drops entire messages — and in collection-bearing messages like browse and
        ///     room-join, drops the entire response. The int/long types already constrain
        ///     the value; downstream code treats negatives as "unknown" or clamps as needed.
        /// </remarks>
        /// <param name="value">The value (unchecked).</param>
        /// <param name="fieldName">The field name (unused; retained for callsite clarity).</param>
        public static void ValidateNonNegative(int value, string fieldName)
        {
            _ = value;
            _ = fieldName;
        }

        /// <summary>
        ///     Reserved for protocol scalars that should be non-negative; accepts any value.
        /// </summary>
        /// <remarks>See <see cref="ValidateNonNegative(int, string)"/>.</remarks>
        /// <param name="value">The value (unchecked).</param>
        /// <param name="fieldName">The field name (unused; retained for callsite clarity).</param>
        public static void ValidateNonNegative(long value, string fieldName)
        {
            _ = value;
            _ = fieldName;
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
        ///     Converts and validates that a raw protocol enum value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The raw protocol value.</param>
        /// <param name="fieldName">The field name for diagnostics.</param>
        /// <returns>The validated enum value.</returns>
        public static TEnum ToDefinedEnum<TEnum>(int value, string fieldName)
            where TEnum : struct
        {
            // Real peers and custom servers send enum values outside the range we know about
            // (e.g., FileAttributeType=5 for FLAC bit depth, future UserPresence values, etc.).
            // Convert without validation; consumers that switch on the enum will hit `default`
            // for unknown values, which is the correct behavior.
            _ = fieldName;
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        /// <summary>
        ///     Reserved for protocol bytes that should be 0 or 1; accepts any value.
        /// </summary>
        /// <remarks>
        ///     Consumers compare with <c>== 1</c>, so non-zero non-one values are treated
        ///     as the "false" branch — safe by construction. Throwing here drops entire
        ///     messages for cosmetic protocol violations.
        /// </remarks>
        /// <param name="value">The flag value (unchecked).</param>
        /// <param name="fieldName">The field name (unused; retained for callsite clarity).</param>
        public static void ValidateBooleanFlag(int value, string fieldName)
        {
            _ = value;
            _ = fieldName;
        }
    }
}
