// <copyright file="TokenFactory.cs" company="JP Dillingham">
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
    ///     Generates unique tokens for network operations.
    /// </summary>
    /// <remarks>
    ///     Generated tokens skip zero because some Soulseek peers treat zero as a sentinel and do not return search responses
    ///     for requests using it.
    /// </remarks>
    internal sealed class TokenFactory : ITokenFactory
    {
        private readonly object syncRoot = new object();
        private int current;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenFactory"/> class.
        /// </summary>
        /// <param name="start">The optional starting value.</param>
        public TokenFactory(int start = 0)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Must be greater than or equal to zero");
            }

            current = start;
        }

        /// <summary>
        ///     Gets the next token.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 1 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        public int NextToken()
        {
            lock (syncRoot)
            {
                if (current == 0)
                {
                    current = 1;
                }

                var retVal = current;
                current = current == int.MaxValue ? 1 : current + 1;
                return retVal;
            }
        }
    }
}
