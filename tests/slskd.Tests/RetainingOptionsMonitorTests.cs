// <copyright file="RetainingOptionsMonitorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using slskd.Common.Configuration;
using Xunit;

public class RetainingOptionsMonitorTests
{
    [Fact]
    public void Invalid_reload_retains_last_valid_value_and_later_valid_reload_applies()
    {
        var source = new TestChangeTokenSource<TestOptions>();
        var factory = new TestOptionsFactory(new TestOptions { Value = 1 });
        using var monitor = new RetainingOptionsMonitor<TestOptions>(factory, new[] { source });
        var changes = new List<int>();
        using var registration = monitor.OnChange((options, _) => changes.Add(options.Value));

        Assert.Equal(1, monitor.CurrentValue.Value);

        factory.Exception = new OptionsValidationException(
            Options.DefaultName,
            typeof(TestOptions),
            new[] { "invalid" });
        source.SignalChange();

        Assert.Equal(1, monitor.CurrentValue.Value);
        Assert.Empty(changes);

        factory.Exception = null;
        factory.Value = new TestOptions { Value = 2 };
        source.SignalChange();

        Assert.Equal(2, monitor.CurrentValue.Value);
        Assert.Equal(new[] { 2 }, changes);
    }

    [Fact]
    public void Invalid_first_reload_retains_value_primed_during_construction()
    {
        var source = new TestChangeTokenSource<TestOptions>();
        var factory = new TestOptionsFactory(new TestOptions { Value = 1 });
        using var monitor = new RetainingOptionsMonitor<TestOptions>(factory, new[] { source });

        factory.Exception = new OptionsValidationException(
            Options.DefaultName,
            typeof(TestOptions),
            new[] { "invalid" });
        source.SignalChange();

        Assert.Equal(1, monitor.CurrentValue.Value);
    }

    [Fact]
    public void Unexpected_factory_failure_is_not_swallowed_during_reload()
    {
        var source = new TestChangeTokenSource<TestOptions>();
        var factory = new TestOptionsFactory(new TestOptions { Value = 1 });
        using var monitor = new RetainingOptionsMonitor<TestOptions>(factory, new[] { source });

        factory.Exception = new IOException("unexpected failure");

        var exception = Assert.Throws<AggregateException>(source.SignalChange);
        Assert.Contains(exception.InnerExceptions, inner => inner is IOException);
    }

    private sealed class TestOptions
    {
        public int Value { get; init; }
    }

    private sealed class TestOptionsFactory : IOptionsFactory<TestOptions>
    {
        public TestOptionsFactory(TestOptions value)
        {
            Value = value;
        }

        public TestOptions Value { get; set; }
        public Exception? Exception { get; set; }

        public TestOptions Create(string name)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            return Value;
        }
    }

    private sealed class TestChangeTokenSource<TOptions> : IOptionsChangeTokenSource<TOptions>
    {
        private CancellationTokenSource cancellationTokenSource = new();

        public string Name => Options.DefaultName;

        public IChangeToken GetChangeToken() => new CancellationChangeToken(cancellationTokenSource.Token);

        public void SignalChange()
        {
            var previous = cancellationTokenSource;
            cancellationTokenSource = new CancellationTokenSource();
            previous.Cancel();
            previous.Dispose();
        }
    }
}
