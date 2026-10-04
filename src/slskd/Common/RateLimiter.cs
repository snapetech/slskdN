// <copyright file="RateLimiter.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

// <copyright file="RateLimiter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using Serilog;

    /// <summary>
    ///     Ensures a minimum interval between successive invocations of a delegate.
    /// </summary>
    public class RateLimiter : IDisposable
    {
        private readonly object _sync = new();
        private readonly SemaphoreSlim? _executionGate;
        private readonly ThreadLocal<int> _executionDepth = new(() => 0);

        /// <summary>
        ///     Initializes a new instance of the <see cref="RateLimiter"/> class.
        /// </summary>
        /// <param name="interval">The minimum interval between invocations.</param>
        /// <param name="concurrencyLimit">
        ///     Limit the number of concurrent executions of the specified action, in cases where the logic runs slower than the configured interval. Set this to null to remove the limit.
        /// </param>
        /// <param name="flushOnDispose">A value indicating whether pending action(s) should be executed during disposal.</param>
        public RateLimiter(int interval, int? concurrencyLimit = 1, bool flushOnDispose = false)
        {
            Timer = new System.Timers.Timer(interval)
            {
                AutoReset = true,
            };

            Timer.Elapsed += Timer_Elapsed;

            FlushOnDispose = flushOnDispose;

            if (concurrencyLimit.HasValue)
            {
                if (concurrencyLimit.Value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(concurrencyLimit));
                }

                _executionGate = new SemaphoreSlim(concurrencyLimit.Value, concurrencyLimit.Value);
            }
        }

        private bool Disposed { get; set; }
        private bool FlushOnDispose { get; }
        private bool Init { get; set; }
        private Action? Staged { get; set; }
        private System.Timers.Timer Timer { get; set; }
        private int _activeExecutions;

        /// <summary>
        ///     Releases all resources used by the <see cref="Component"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Invokes the specified <paramref name="action"/>, dropping invocations created prior to the elapse of the
        ///     configured interval.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public void Invoke(Action action)
        {
            var executeImmediately = false;
            lock (_sync)
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(RateLimiter));
                }

                if (!Init)
                {
                    executeImmediately = TryAcquireExecutionSlot();
                    if (!executeImmediately)
                    {
                        Staged = action;
                    }

                    try
                    {
                        Timer.Start();
                    }
                    catch
                    {
                        Init = false;
                        if (executeImmediately)
                        {
                            ReleaseExecutionSlot();
                        }

                        throw;
                    }

                    Init = true;
                }
                else
                {
                    Staged = action;
                }
            }

            if (executeImmediately)
            {
                try
                {
                    Execute(action);
                }
                finally
                {
                    ReleaseExecutionSlot();
                }
            }
        }

        /// <summary>
        ///     Releases all resources used by the <see cref="Component"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Action? staged;
            lock (_sync)
            {
                if (Disposed)
                {
                    return;
                }

                Disposed = true;
                staged = disposing && FlushOnDispose ? Staged : null;
                Staged = null;
                if (disposing)
                {
                    Timer.Elapsed -= Timer_Elapsed;
                }
            }

            if (disposing)
            {
                Common.TimerDisposer.DisposeWithWait(Timer);

                if (staged is not null)
                {
                    var acquiredExecutionSlot = _executionDepth.Value == 0;
                    if (acquiredExecutionSlot)
                    {
                        AcquireExecutionSlot();
                    }

                    try
                    {
                        Execute(staged);
                    }
                    finally
                    {
                        if (acquiredExecutionSlot)
                        {
                            ReleaseExecutionSlot();
                        }
                    }
                }
            }
        }

        private void Timer_Elapsed(object? sender, EventArgs args)
        {
            if (TryAcquireExecutionSlot())
            {
                Action? staged;
                lock (_sync)
                {
                    if (Disposed)
                    {
                        ReleaseExecutionSlot();
                        return;
                    }

                    staged = Staged;
                    Staged = null;
                }

                try
                {
                    try
                    {
                        if (staged is not null)
                        {
                            Execute(staged);
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        Log.Debug(ex, "RateLimiter staged callback cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "RateLimiter staged callback failed");
                    }
                }
                finally
                {
                    ReleaseExecutionSlot();
                }
            }
        }

        private bool TryAcquireExecutionSlot()
        {
            if (_executionGate is not null && !_executionGate.Wait(0))
            {
                return false;
            }

            Interlocked.Increment(ref _activeExecutions);
            return true;
        }

        private void AcquireExecutionSlot()
        {
            _executionGate?.Wait();
            Interlocked.Increment(ref _activeExecutions);
        }

        private void ReleaseExecutionSlot()
        {
            Interlocked.Decrement(ref _activeExecutions);
            if (_executionGate is not null)
            {
                _executionGate.Release();
            }
        }

        private void Execute(Action action)
        {
            _executionDepth.Value++;
            try
            {
                action();
            }
            finally
            {
                _executionDepth.Value--;
            }
        }
    }
}
