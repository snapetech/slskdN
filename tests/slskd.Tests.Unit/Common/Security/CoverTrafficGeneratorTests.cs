// <copyright file="CoverTrafficGeneratorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Common.Security;
using Xunit;

namespace slskd.Tests.Unit.Common.Security;

public class CoverTrafficGeneratorTests
{
    [Fact]
    public async Task GenerateCoverTraffic_WhenSendThrows_EscapesExceptionWithoutAttachingIt()
    {
        var logger = new WarningCaptureLogger();
        using var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.FromException(new InvalidOperationException("send failure\r\nforged")),
            logger);
        using var cancellation = new CancellationTokenSource();
        var method = typeof(CoverTrafficGenerator).GetMethod("GenerateCoverTraffic", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cover traffic worker method was not found.");

        var worker = Task.Run(() => method.Invoke(generator, new object[] { cancellation.Token }));
        var warning = await logger.Warning.WaitAsync(TimeSpan.FromSeconds(4));
        cancellation.Cancel();
        await worker.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Contains("send failure\\r\\nforged", warning.Message);
        Assert.DoesNotContain("\r", warning.Message);
        Assert.DoesNotContain("\n", warning.Message);
        Assert.Null(warning.Exception);
    }

    [Fact]
    public void Dispose_WhenGenerationFailed_EscapesExceptionWithoutAttachingIt()
    {
        var logger = new WarningCaptureLogger();
        var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.CompletedTask,
            logger);
        SetPrivateField(generator, "_generationTask", Task.FromException(
            new InvalidOperationException("dispose failure\r\nforged")));

        generator.Dispose();

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("dispose failure\\r\\nforged", warning.Message);
        Assert.DoesNotContain("\r", warning.Message);
        Assert.DoesNotContain("\n", warning.Message);
        Assert.Null(warning.Exception);
    }

    [Fact]
    public async Task StopAsync_AfterStart_CancelsGenerationPromptly()
    {
        var logger = Mock.Of<ILogger<CoverTrafficGenerator>>();
        using var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 60,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.CompletedTask,
            logger);

        await generator.StartAsync();

        var stopwatch = Stopwatch.StartNew();
        await generator.StopAsync();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(4), $"StopAsync took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task Dispose_WaitsForAnInFlightCoverMessageSend()
    {
        var logger = Mock.Of<ILogger<CoverTrafficGenerator>>();
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () =>
            {
                sendStarted.TrySetResult(true);
                return releaseSend.Task;
            },
            logger);

        await generator.StartAsync();
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var disposeTask = Task.Run(generator.Dispose);
        var disposeBeforeSendFinished = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromMilliseconds(100)));

        Assert.NotSame(disposeTask, disposeBeforeSendFinished);

        releaseSend.TrySetResult(true);
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.False(generator.GetStats().IsActive);
    }

    [Fact]
    public async Task Dispose_WhenSendOutlastsStopTimeout_DefersCancellationSourceDisposal()
    {
        var logger = Mock.Of<ILogger<CoverTrafficGenerator>>();
        var sendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSend = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () =>
            {
                sendStarted.TrySetResult(true);
                return releaseSend.Task;
            },
            logger);

        await generator.StartAsync();
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var generationCts = Assert.IsType<CancellationTokenSource>(GetPrivateField(generator, "_generationCts"));
        var generationTask = Assert.IsAssignableFrom<Task>(GetPrivateField(generator, "_generationTask"));

        await Task.Run(generator.Dispose).WaitAsync(TimeSpan.FromSeconds(7));

        Assert.True(generationCts.IsCancellationRequested);
        _ = generationCts.Token;

        releaseSend.TrySetResult(true);
        await generationTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(await Task.Run(() => SpinWait.SpinUntil(
            () => GetPrivateField(generator, "_generationCts") is null,
            TimeSpan.FromSeconds(2))));
    }

    [Fact]
    public async Task Dispose_WhenGenerationFaultsAfterStopTimeout_EscapesExceptionWithoutAttachingIt()
    {
        var logger = new WarningCaptureLogger();
        var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.CompletedTask,
            logger);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetPrivateField(generator, "_generationTask", generation.Task);

        await Task.Run(generator.Dispose).WaitAsync(TimeSpan.FromSeconds(7));
        generation.SetException(new InvalidOperationException("late cleanup failure\r\nforged"));

        var error = await logger.Error.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Contains("late cleanup failure\\r\\nforged", error.Message);
        Assert.DoesNotContain("\r", error.Message);
        Assert.DoesNotContain("\n", error.Message);
        Assert.Null(error.Exception);
    }

    [Fact]
    public async Task StartAsync_AfterStop_RestartsGeneration()
    {
        var logger = Mock.Of<ILogger<CoverTrafficGenerator>>();
        using var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 60,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.CompletedTask,
            logger);

        await generator.StartAsync();
        await generator.StopAsync();
        Assert.False(generator.GetStats().IsActive);

        await generator.StartAsync();

        Assert.True(generator.GetStats().IsActive);
    }

    [Fact]
    public async Task StartAsync_WhenReplacingPreviousGenerationToken_CancelsOldTokenSource()
    {
        var logger = Mock.Of<ILogger<CoverTrafficGenerator>>();
        using var generator = new CoverTrafficGenerator(
            new CoverTrafficOptions
            {
                Enabled = true,
                IntervalSeconds = 60,
                OnlyWhenIdle = true,
            },
            () => new byte[] { 0x01 },
            () => Task.CompletedTask,
            logger);

        using var previousGenerationCts = new CancellationTokenSource();
        SetPrivateField(generator, "_generationCts", previousGenerationCts);
        SetPrivateField(generator, "_generationTask", Task.CompletedTask);

        await generator.StartAsync();

        Assert.True(previousGenerationCts.IsCancellationRequested);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on {instance.GetType().Name}.");

        field.SetValue(instance, value);
    }

    private static object? GetPrivateField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on {instance.GetType().Name}.");

        return field.GetValue(instance);
    }

    private sealed class WarningCaptureLogger : ILogger<CoverTrafficGenerator>
    {
        private readonly ConcurrentQueue<CapturedLogEntry> entries = new();
        private readonly TaskCompletionSource<CapturedLogEntry> warning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<CapturedLogEntry> error = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CapturedLogEntry> Warning => warning.Task;
        public Task<CapturedLogEntry> Error => error.Task;
        public IReadOnlyCollection<CapturedLogEntry> Entries => entries.ToArray();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullLogger.Instance.BeginScope(state)!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var entry = new CapturedLogEntry(logLevel, formatter(state, exception), exception);
            entries.Enqueue(entry);
            if (logLevel == LogLevel.Warning)
            {
                warning.TrySetResult(entry);
            }

            if (logLevel == LogLevel.Error)
            {
                error.TrySetResult(entry);
            }
        }
    }

    private sealed record CapturedLogEntry(LogLevel Level, string Message, Exception? Exception);
}
