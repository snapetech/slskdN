// <copyright file="WishlistSearchSchedulerOptions.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Options for <see cref="WishlistSearchScheduler"/>.
    /// </summary>
    public sealed class WishlistSearchSchedulerOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WishlistSearchSchedulerOptions"/> class.
        /// </summary>
        /// <param name="intervalOverride">The optional interval override.</param>
        /// <param name="minimumInterval">The minimum allowed interval.</param>
        /// <param name="searchOptionsFactory">The optional search options factory.</param>
        public WishlistSearchSchedulerOptions(
            TimeSpan? intervalOverride = null,
            TimeSpan? minimumInterval = null,
            Func<string, SearchOptions> searchOptionsFactory = null)
        {
            if (intervalOverride.HasValue && intervalOverride.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalOverride), "Must be greater than zero");
            }

            if (minimumInterval.HasValue && minimumInterval.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumInterval), "Must be greater than zero");
            }

            IntervalOverride = intervalOverride;
            MinimumInterval = minimumInterval ?? TimeSpan.FromSeconds(30);
            SearchOptionsFactory = searchOptionsFactory;
        }

        /// <summary>
        ///     Gets the optional interval override.
        /// </summary>
        public TimeSpan? IntervalOverride { get; }

        /// <summary>
        ///     Gets the minimum allowed interval.
        /// </summary>
        public TimeSpan MinimumInterval { get; }

        /// <summary>
        ///     Gets the optional search options factory.
        /// </summary>
        public Func<string, SearchOptions> SearchOptionsFactory { get; }
    }
}
