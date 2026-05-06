// <copyright file="ProtocolArgumentValidator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
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
    using System.Text;

    /// <summary>
    ///     Validates outbound protocol arguments before message emission.
    /// </summary>
    internal static class ProtocolArgumentValidator
    {
        public static void RequireNonNegative(int value, string paramName, string valueName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"The {valueName} must be equal to or greater than zero");
            }
        }

        public static void RequirePositive(int value, string paramName, string valueName)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"The {valueName} must be greater than zero");
            }
        }

        public static string RequireNotNull(string value, string paramName, string valueName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, $"The {valueName} must not be null");
            }

            return value;
        }

        public static string RequireMaximumUtf8Length(string value, string paramName, string valueName, int maximumLength)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, $"The {valueName} must not be null");
            }

            if (Encoding.UTF8.GetByteCount(value) > maximumLength)
            {
                throw new ArgumentOutOfRangeException(paramName, $"The {valueName} must not exceed {maximumLength} UTF-8 bytes.");
            }

            return value;
        }
    }
}
