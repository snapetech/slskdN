// <copyright file="TransferMetrics.cs" company="slskdN Team">
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

    internal static class TransferMetrics
    {
        public static TimeSpan? GetRemainingTime(long bytesRemaining, double averageSpeed)
        {
            if (averageSpeed <= 0 || double.IsNaN(averageSpeed) || double.IsInfinity(averageSpeed))
            {
                return null;
            }

            var seconds = bytesRemaining / averageSpeed;

            if (double.IsInfinity(seconds) || seconds >= TimeSpan.MaxValue.TotalSeconds)
            {
                return TimeSpan.MaxValue;
            }

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
