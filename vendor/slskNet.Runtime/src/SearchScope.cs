// <copyright file="SearchScope.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Search scope definition.
    /// </summary>
    public class SearchScope
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchScope"/> class.
        /// </summary>
        /// <param name="type">The scope type.</param>
        /// <param name="subjects">The scope subjects, if applicable.</param>
        public SearchScope(SearchScopeType type, params string[] subjects)
        {
            if (!Enum.IsDefined(typeof(SearchScopeType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Must be a defined search scope type");
            }

            Type = type;

            var subjectList = (subjects ?? Array.Empty<string>()).ToList();

            if ((Type == SearchScopeType.Network || Type == SearchScopeType.Wishlist) && subjectList.Count > 0)
            {
                throw new ArgumentException($"The {Type} search scope can not be used with subjects", nameof(subjects));
            }

            if (Type == SearchScopeType.Room && (subjectList.Count != 1 || string.IsNullOrWhiteSpace(subjectList[0])))
            {
                throw new ArgumentException($"The Room search scope requires a single, non null, non empty, and non whitespace subject", nameof(subjects));
            }

            if (Type == SearchScopeType.User)
            {
                if (subjectList.Count == 0)
                {
                    throw new ArgumentException($"The User search scope requires at least one subject", nameof(subjects));
                }

                if (subjectList.Any(s => string.IsNullOrWhiteSpace(s)))
                {
                    throw new ArgumentException($"One or more of the supplied User scope subjects is null, empty, or whitespace", nameof(subjects));
                }
            }

            Subjects = subjectList.AsReadOnly();
        }

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Network"/> scope.
        /// </summary>
        public static SearchScope Network => new SearchScope(SearchScopeType.Network);

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Wishlist"/> scope.
        /// </summary>
        public static SearchScope Wishlist => new SearchScope(SearchScopeType.Wishlist);

        /// <summary>
        ///     Gets the scope subjects, if applicable.
        /// </summary>
        /// <remarks>Ignored for <see cref="SearchScopeType.Network"/> and <see cref="SearchScopeType.Wishlist"/>.</remarks>
        public IEnumerable<string> Subjects { get; }

        /// <summary>
        ///     Gets the scope type.
        /// </summary>
        public SearchScopeType Type { get; }

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Room"/> scope with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room to search.</param>
        /// <returns>A Room scope with the specified <paramref name="roomName"/>.</returns>
        public static SearchScope Room(string roomName) => new SearchScope(SearchScopeType.Room, roomName);

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.User"/> scope with the specified <paramref name="usernames"/>.
        /// </summary>
        /// <param name="usernames">The username(s) of the user(s) to search.</param>
        /// <returns>A User scope with the specified <paramref name="usernames"/>.</returns>
        public static SearchScope User(params string[] usernames) => new SearchScope(SearchScopeType.User, usernames);
    }
}
