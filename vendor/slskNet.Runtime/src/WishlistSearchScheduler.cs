// <copyright file="WishlistSearchScheduler.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Runs opt-in wishlist searches at the interval announced by the Soulseek server.
    /// </summary>
    public sealed class WishlistSearchScheduler : IDisposable
    {
        private static readonly TimeSpan MaximumDelayInterval = TimeSpan.FromMilliseconds(int.MaxValue);

        private readonly ISoulseekClient client;
        private readonly WishlistSearchSchedulerOptions options;
        private readonly object syncRoot = new object();
        private readonly IReadOnlyCollection<string> terms;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private Task loopTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WishlistSearchScheduler"/> class.
        /// </summary>
        /// <param name="client">The client to use.</param>
        /// <param name="terms">The wishlist terms to search.</param>
        /// <param name="options">The scheduler options.</param>
        public WishlistSearchScheduler(ISoulseekClient client, IEnumerable<string> terms, WishlistSearchSchedulerOptions options = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.options = options ?? new WishlistSearchSchedulerOptions();
            this.terms = (terms ?? throw new ArgumentNullException(nameof(terms)))
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Select(term => term.Trim())
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList()
                .AsReadOnly();

            if (this.terms.Count == 0)
            {
                throw new ArgumentException("At least one wishlist term is required.", nameof(terms));
            }
        }

        /// <summary>
        ///     Occurs when a wishlist search completes or fails.
        /// </summary>
        public event EventHandler<WishlistSearchCompletedEventArgs> SearchCompleted;

        /// <summary>
        ///     Gets a value indicating whether the scheduler is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (syncRoot)
                {
                    return loopTask != null && !loopTask.IsCompleted;
                }
            }
        }

        /// <summary>
        ///     Starts the scheduler.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public void Start(CancellationToken cancellationToken = default)
        {
            lock (syncRoot)
            {
                ThrowIfDisposed();

                if (loopTask != null && !loopTask.IsCompleted)
                {
                    if (cancellationTokenSource == null || !cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    var previousTask = loopTask;
                    var previousSource = cancellationTokenSource;
                    var nextSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    cancellationTokenSource = nextSource;
                    loopTask = previousTask.ContinueWith(
                        _ =>
                        {
                            previousSource?.Dispose();

                            lock (syncRoot)
                            {
                                if (disposed || cancellationTokenSource != nextSource || nextSource.IsCancellationRequested)
                                {
                                    nextSource.Dispose();

                                    if (cancellationTokenSource == nextSource)
                                    {
                                        cancellationTokenSource = null;
                                    }

                                    return Task.CompletedTask;
                                }
                            }

                            return RunAsync(nextSource.Token);
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default).Unwrap();

                    return;
                }

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                loopTask = RunAsync(cancellationTokenSource.Token);
            }
        }

        /// <summary>
        ///     Stops the scheduler.
        /// </summary>
        public void Stop()
        {
            lock (syncRoot)
            {
                cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        ///     Disposes this scheduler.
        /// </summary>
        public void Dispose()
        {
            CancellationTokenSource source;
            Task task;

            lock (syncRoot)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                source = cancellationTokenSource;
                task = loopTask;
                source?.Cancel();
            }

            if (source == null)
            {
                return;
            }

            if (task == null || task.IsCompleted)
            {
                source.Dispose();
            }
            else
            {
                task.ContinueWith(
                    _ => source.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        private TimeSpan GetInterval()
        {
            var wishlistInterval = client.ServerInfo?.WishlistInterval ?? (int)options.MinimumInterval.TotalSeconds;
            var interval = options.IntervalOverride ?? TimeSpan.FromSeconds(wishlistInterval);

            if (interval < options.MinimumInterval)
            {
                return options.MinimumInterval;
            }

            return interval > MaximumDelayInterval ? MaximumDelayInterval : interval;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var term in terms)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await RunSearchAsync(term, cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(GetInterval(), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RunSearchAsync(string term, CancellationToken cancellationToken)
        {
            try
            {
                var result = await client.SearchAsync(
                    SearchQuery.FromText(term),
                    SearchScope.Wishlist,
                    options: options.SearchOptionsFactory?.Invoke(term),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, result.Search, result.Responses, null));
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, null, Array.Empty<SearchResponse>(), ex));
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WishlistSearchScheduler));
            }
        }
    }
}
