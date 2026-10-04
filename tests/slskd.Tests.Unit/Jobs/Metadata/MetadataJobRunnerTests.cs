// <copyright file="MetadataJobRunnerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Jobs.Metadata;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Jobs.Metadata;
using Xunit;

public class MetadataJobRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenJobThrowsUnrelatedCancellation_LogsFailureAndKeepsRunnerAlive()
    {
        var logger = new ErrorSignalLogger<MetadataJobRunner>();
        var runner = new MetadataJobRunner(logger);
        var secondJobStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await runner.StartAsync(CancellationToken.None);

        Assert.True(runner.Enqueue(new CancelledJob()));
        var exception = await logger.ErrorLogged.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsType<OperationCanceledException>(exception);
        Assert.True(runner.Enqueue(new CancelledJob("second", secondJobStarted)));
        await secondJobStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await runner.StopAsync(CancellationToken.None);
    }

    private sealed class CancelledJob : IMetadataJob
    {
        private readonly TaskCompletionSource<bool>? started;

        public CancelledJob(string jobId = "cancelled", TaskCompletionSource<bool>? started = null)
        {
            JobId = jobId;
            this.started = started;
        }

        public string JobId { get; }

        public string Kind => "test";

        public Task ExecuteAsync(CancellationToken ct = default)
        {
            started?.TrySetResult(true);
            return Task.FromException(new OperationCanceledException("Cancellation was not requested by the runner"));
        }
    }

    private sealed class ErrorSignalLogger<T> : ILogger<T>
    {
        private readonly TaskCompletionSource<Exception?> errorLogged = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Exception?> ErrorLogged => errorLogged.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => EmptyScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                errorLogged.TrySetResult(exception);
            }
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        public void Dispose()
        {
        }
    }
}
