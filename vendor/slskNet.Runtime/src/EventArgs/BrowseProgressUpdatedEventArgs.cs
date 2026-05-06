// <copyright file="BrowseProgressUpdatedEventArgs.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
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
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-FileCopyrightText: slskdN Team
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;

    /// <summary>
    ///     Event arguments for events raised by receipt of browse response data.
    /// </summary>
    public class BrowseProgressUpdatedEventArgs : BrowseEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseProgressUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the event.</param>
        /// <param name="bytesTransferred">The total number of bytes transfereed.</param>
        /// <param name="size">The total expected length of the data transfer.</param>
        public BrowseProgressUpdatedEventArgs(string username, long bytesTransferred, long size)
            : base(username)
        {
            if (bytesTransferred < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesTransferred), "Must be greater than or equal to zero");
            }

            if (size >= 0 && bytesTransferred > size)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesTransferred), "Must be less than or equal to size");
            }

            BytesTransferred = bytesTransferred;
            Size = size;
            BytesRemaining = Size <= 0 ? 0 : Size - BytesTransferred;
            PercentComplete = Size <= 0 ? 0 : (BytesTransferred / (double)Size) * 100d;
        }

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining { get; }

        /// <summary>
        ///     Gets the progress of the data transfer as a percentage of current and total data length.
        /// </summary>
        public double PercentComplete { get; }

        /// <summary>
        ///     Gets the total expected length of the data transfer.
        /// </summary>
        public long Size { get; }
    }
}
