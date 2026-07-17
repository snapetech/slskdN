// <copyright file="RetainingOptionsMonitor.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Common.Configuration;

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;

/// <summary>
///     An options monitor that retains the last valid value when a configuration reload is invalid.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
public sealed class RetainingOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>, IDisposable
    where TOptions : class
{
    private readonly IOptionsFactory<TOptions> factory;
    private readonly object sync = new();
    private readonly Dictionary<string, TOptions> values = new(StringComparer.Ordinal);
    private readonly List<IDisposable> changeTokenRegistrations = new();
    private Action<TOptions, string?>? listeners;
    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RetainingOptionsMonitor{TOptions}"/> class.
    /// </summary>
    /// <param name="factory">The options factory.</param>
    /// <param name="sources">The configuration change token sources.</param>
    public RetainingOptionsMonitor(
        IOptionsFactory<TOptions> factory,
        IEnumerable<IOptionsChangeTokenSource<TOptions>> sources)
    {
        this.factory = factory;

        foreach (var source in sources)
        {
            changeTokenRegistrations.Add(ChangeToken.OnChange(
                source.GetChangeToken,
                InvokeChanged,
                source.Name));
        }
    }

    /// <inheritdoc />
    public TOptions CurrentValue => Get(Options.DefaultName);

    /// <inheritdoc />
    public TOptions Get(string? name)
    {
        name ??= Options.DefaultName;

        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (values.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        var created = factory.Create(name);

        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (values.TryGetValue(name, out var value))
            {
                return value;
            }

            values.Add(name, created);
            return created;
        }
    }

    /// <inheritdoc />
    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            listeners += listener;
        }

        return new Subscription(this, listener);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<IDisposable> registrations;

        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            listeners = null;
            registrations = new List<IDisposable>(changeTokenRegistrations);
            changeTokenRegistrations.Clear();
            values.Clear();
        }

        foreach (var registration in registrations)
        {
            registration.Dispose();
        }
    }

    private void InvokeChanged(string? name)
    {
        name ??= Options.DefaultName;

        TOptions created;
        try
        {
            created = factory.Create(name);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Options reload rejected; retaining the last valid configuration");
            return;
        }

        Action<TOptions, string?>? callbacks;
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            values[name] = created;
            callbacks = listeners;
        }

        callbacks?.Invoke(created, name);
    }

    private void Unsubscribe(Action<TOptions, string?> listener)
    {
        lock (sync)
        {
            listeners -= listener;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private RetainingOptionsMonitor<TOptions>? monitor;
        private Action<TOptions, string?>? listener;

        public Subscription(RetainingOptionsMonitor<TOptions> monitor, Action<TOptions, string?> listener)
        {
            this.monitor = monitor;
            this.listener = listener;
        }

        public void Dispose()
        {
            var currentMonitor = Interlocked.Exchange(ref monitor, null);
            var currentListener = Interlocked.Exchange(ref listener, null);

            if (currentMonitor is not null && currentListener is not null)
            {
                currentMonitor.Unsubscribe(currentListener);
            }
        }
    }
}
