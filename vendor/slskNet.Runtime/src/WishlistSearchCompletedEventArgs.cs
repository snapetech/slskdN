// <copyright file="WishlistSearchCompletedEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Event arguments for a completed wishlist search.
    /// </summary>
    public sealed class WishlistSearchCompletedEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WishlistSearchCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="term">The wishlist search term.</param>
        /// <param name="search">The completed search, if available.</param>
        /// <param name="responses">The collected responses.</param>
        /// <param name="exception">The exception, if the search failed.</param>
        public WishlistSearchCompletedEventArgs(string term, Search search, IReadOnlyCollection<SearchResponse> responses, Exception exception)
        {
            Term = term;
            Search = search;
            Responses = responses ?? Array.Empty<SearchResponse>();
            Exception = exception;
        }

        /// <summary>
        ///     Gets the exception, if the search failed.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the collected responses.
        /// </summary>
        public IReadOnlyCollection<SearchResponse> Responses { get; }

        /// <summary>
        ///     Gets the completed search, if available.
        /// </summary>
        public Search Search { get; }

        /// <summary>
        ///     Gets the wishlist search term.
        /// </summary>
        public string Term { get; }
    }
}
