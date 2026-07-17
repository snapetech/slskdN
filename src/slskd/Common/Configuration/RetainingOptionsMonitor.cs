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
    private readonly IOptionsFactory<TOptions> _factory;
    private readonly object _sync = new();
    private readonly Dictionary<string, TOptions> _values = new(StringComparer.Ordinal);
    private readonly List<IDisposable> _changeTokenRegistrations = new();
    private Action<TOptions, string?>? _listeners;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RetainingOptionsMonitor{TOptions}"/> class.
    /// </summary>
    /// <param name="factory">The options factory.</param>
    /// <param name="sources">The configuration change token sources.</param>
    public RetainingOptionsMonitor(
        IOptionsFactory<TOptions> factory,
        IEnumerable<IOptionsChangeTokenSource<TOptions>> sources)
    {
        _factory = factory;
        _values.Add(Options.DefaultName, _factory.Create(Options.DefaultName));

        foreach (var source in sources)
        {
            _changeTokenRegistrations.Add(ChangeToken.OnChange(
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

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_values.TryGetValue(name, out var value))
            {
                return value;
            }
        }

        var created = _factory.Create(name);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_values.TryGetValue(name, out var value))
            {
                return value;
            }

            _values.Add(name, created);
            return created;
        }
    }

    /// <inheritdoc />
    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _listeners += listener;
        }

        return new Subscription(this, listener);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<IDisposable> registrations;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _listeners = null;
            registrations = new List<IDisposable>(_changeTokenRegistrations);
            _changeTokenRegistrations.Clear();
            _values.Clear();
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
            created = _factory.Create(name);
        }
        catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException or FormatException or OverflowException)
        {
            Log.Warning(ex, "Options reload rejected; retaining the last valid configuration");
            return;
        }

        Action<TOptions, string?>? callbacks;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _values[name] = created;
            callbacks = _listeners;
        }

        callbacks?.Invoke(created, name);
    }

    private void Unsubscribe(Action<TOptions, string?> listener)
    {
        lock (_sync)
        {
            _listeners -= listener;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private RetainingOptionsMonitor<TOptions>? _monitor;
        private Action<TOptions, string?>? _listener;

        public Subscription(RetainingOptionsMonitor<TOptions> monitor, Action<TOptions, string?> listener)
        {
            _monitor = monitor;
            _listener = listener;
        }

        public void Dispose()
        {
            var currentMonitor = Interlocked.Exchange(ref _monitor, null);
            var currentListener = Interlocked.Exchange(ref _listener, null);

            if (currentMonitor is not null && currentListener is not null)
            {
                currentMonitor.Unsubscribe(currentListener);
            }
        }
    }
}
