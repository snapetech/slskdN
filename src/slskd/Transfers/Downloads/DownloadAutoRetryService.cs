// <copyright file="DownloadAutoRetryService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.Downloads
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Soulseek;

    /// <summary>
    ///     Background service that periodically re-enqueues downloads that ended in a retryable failed state
    ///     (TimedOut, Errored, Aborted), skipping Cancelled and Rejected.
    /// </summary>
    public class DownloadAutoRetryService : BackgroundService
    {
        private readonly IDownloadService downloadService;
        private readonly ISoulseekClient client;
        private readonly IOptionsMonitor<slskd.Options> options;
        private readonly ILogger log = Log.ForContext<DownloadAutoRetryService>();

        // Tracks transfer IDs already scheduled for retry so we don't double-queue the same failure.
        private readonly HashSet<Guid> retriedIds = [];

        // Tracks how many times each (username, filename) has been auto-retried this process lifetime.
        private readonly ConcurrentDictionary<string, int> retryCounts = new();

        public DownloadAutoRetryService(
            IDownloadService downloadService,
            ISoulseekClient client,
            IOptionsMonitor<slskd.Options> options)
        {
            this.downloadService = downloadService;
            this.client = client;
            this.options = options;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            log.Information("[AUTO-RETRY] Download auto-retry service started");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var opts = options.CurrentValue?.Global?.Download?.AutoRetry;
                    var intervalSeconds = opts?.CheckIntervalSeconds ?? 60;

                    try
                    {
                        if (opts?.Enabled == true && client.State.HasFlag(SoulseekClientStates.Connected))
                        {
                            await RetryFailedDownloadsAsync(opts, stoppingToken);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "[AUTO-RETRY] Error during auto-retry cycle: {Message}", ex.Message);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            log.Information("[AUTO-RETRY] Download auto-retry service stopped");
        }

        private async Task RetryFailedDownloadsAsync(slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions opts, CancellationToken ct)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(opts.RetryDelaySeconds);

            var failed = downloadService.List(
                t => t.State.HasFlag(TransferStates.Completed)
                     && !t.State.HasFlag(TransferStates.Succeeded)
                     && !t.State.HasFlag(TransferStates.Cancelled)
                     && !t.State.HasFlag(TransferStates.Rejected)
                     && t.EndedAt != null
                     && t.EndedAt < cutoff,
                includeRemoved: false);

            var toRetry = failed.Where(t => !retriedIds.Contains(t.Id)).ToList();

            if (toRetry.Count == 0)
            {
                return;
            }

            log.Information("[AUTO-RETRY] Found {Count} failed downloads eligible for retry", toRetry.Count);

            foreach (var group in toRetry.GroupBy(t => t.Username))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var username = group.Key;

                var eligible = group
                    .Where(t =>
                    {
                        var key = RetryKey(t);
                        return retryCounts.GetOrAdd(key, 0) < opts.MaxAttempts;
                    })
                    .ToList();

                if (eligible.Count == 0)
                {
                    continue;
                }

                // Mark as retried before enqueueing to prevent double-queuing if the service loops fast.
                foreach (var t in eligible)
                {
                    retriedIds.Add(t.Id);
                }

                try
                {
                    var files = eligible.Select(t => (t.Filename, t.Size)).ToList();
                    await downloadService.EnqueueAsync(username, files, ct);

                    foreach (var t in eligible)
                    {
                        var attempt = retryCounts.AddOrUpdate(RetryKey(t), 1, (_, c) => c + 1);
                        log.Information(
                            "[AUTO-RETRY] Re-queued {Filename} from {Username} (auto-retry #{Attempt}/{Max}, state was {State})",
                            t.Filename,
                            username,
                            attempt,
                            opts.MaxAttempts,
                            t.State);
                    }
                }
                catch (Exception ex)
                {
                    log.Warning(ex, "[AUTO-RETRY] Failed to re-enqueue from {Username}: {Message}", username, ex.Message);

                    // Remove from retried set so we'll attempt again next cycle.
                    foreach (var t in eligible)
                    {
                        retriedIds.Remove(t.Id);
                    }
                }
            }
        }

        private static string RetryKey(slskd.Transfers.Transfer t) => $"{t.Username}:{t.Filename}";
    }
}
