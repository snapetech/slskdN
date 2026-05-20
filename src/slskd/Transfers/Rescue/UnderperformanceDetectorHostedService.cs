// <copyright file="UnderperformanceDetectorHostedService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.Rescue
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Serilog;
    using slskd.Transfers.Downloads;
    using Soulseek;

    /// <summary>
    ///     Hosted service that periodically checks active downloads for underperformance
    ///     (queued too long, throughput too low, stalled) and triggers rescue mode.
    /// </summary>
    public sealed class UnderperformanceDetectorHostedService : IHostedService, IDisposable
    {
        private readonly IDownloadService downloadService;
        private readonly IRescueService rescueService;
        private readonly IAcceleratedDownloadService acceleratedDownloads;
        private readonly IOptionsMonitor<slskd.Options> options;
        private readonly ILogger log = Log.ForContext<UnderperformanceDetectorHostedService>();
        private CancellationTokenSource? loopCts;
        private Task? loopTask;
        private static readonly TimeSpan NoRecordingCooldown = TimeSpan.FromHours(2);
        private static readonly TimeSpan NoOverlayPeersCooldown = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan GuardrailCooldown = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MultiSourceUnavailableCooldown = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan FailedActivationCooldown = TimeSpan.FromMinutes(5);

        // For Stalled rule: transferId -> (lastBytesTransferred, first time we saw no progress)
        private readonly ConcurrentDictionary<Guid, (long LastBytes, DateTime? FirstNoProgressAt)> stalledState = new();
        private readonly ConcurrentDictionary<Guid, RescueAttemptState> rescueAttempts = new();

        public UnderperformanceDetectorHostedService(
            IDownloadService downloadService,
            IRescueService rescueService,
            IAcceleratedDownloadService acceleratedDownloads,
            IOptionsMonitor<slskd.Options> options)
        {
            this.downloadService = downloadService;
            this.rescueService = rescueService;
            this.acceleratedDownloads = acceleratedDownloads;
            this.options = options;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            log.Information("[RESCUE] Underperformance detector started");
            loopCts?.Cancel();
            loopCts?.Dispose();
            loopCts = new CancellationTokenSource();
            loopTask = Task.Run(() => RunLoopAsync(loopCts.Token), CancellationToken.None);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            loopCts?.Cancel();

            if (loopTask != null)
            {
                try
                {
                    await loopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown.
                }
            }

            loopTask = null;
            loopCts?.Dispose();
            loopCts = null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            loopCts?.Cancel();
            loopCts?.Dispose();
            loopCts = null;
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var rescue = options.CurrentValue?.RescueMode;
                    if (rescue == null || !rescue.Enabled || !acceleratedDownloads.IsEnabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(rescue?.CheckIntervalSeconds ?? 45), ct).ConfigureAwait(false);
                        continue;
                    }

                    var active = downloadService.List(t => !t.State.HasFlag(TransferStates.Completed), includeRemoved: false);
                    var activeIds = active.Select(t => t.Id).ToHashSet();

                    // Prune stalled state for transfers that are no longer active
                    foreach (var id in stalledState.Keys.ToArray())
                    {
                        if (!activeIds.Contains(id))
                            stalledState.TryRemove(id, out _);
                    }

                    foreach (var id in rescueAttempts.Keys.ToArray())
                    {
                        if (!activeIds.Contains(id))
                            rescueAttempts.TryRemove(id, out _);
                    }

                    foreach (var t in active)
                    {
                        if (ct.IsCancellationRequested) break;
                        var idStr = t.Id.ToString();
                        if (rescueService.IsRescueActive(idStr)) continue;
                        if (ShouldSkipRecentRescueAttempt(t.Id, DateTime.UtcNow)) continue;

                        // 1) QueuedTooLong
                        if (t.State.HasFlag(TransferStates.Queued))
                        {
                            var since = t.EnqueuedAt ?? t.RequestedAt;
                            var elapsed = (DateTime.UtcNow - since).TotalSeconds;
                            if (elapsed >= rescue.MaxQueueTimeSeconds)
                            {
                                await TriggerRescueAsync(t, UnderperformanceReason.QueuedTooLong, ct).ConfigureAwait(false);
                                continue;
                            }
                        }

                        // 2) ThroughputTooLow and 3) Stalled — only for InProgress
                        if (t.State != TransferStates.InProgress) continue;

                        var minBytesPerSec = rescue.MinThroughputKBps * 1024L;
                        var duration = t.StartedAt.HasValue ? (DateTime.UtcNow - t.StartedAt.Value).TotalSeconds : 0;

                        // 2) ThroughputTooLow: require MinDurationSeconds before judging
                        if (duration >= rescue.MinDurationSeconds && t.AverageSpeed < minBytesPerSec)
                        {
                            await TriggerRescueAsync(t, UnderperformanceReason.ThroughputTooLow, ct).ConfigureAwait(false);
                            continue;
                        }

                        // 3) Stalled: no increase in BytesTransferred for StalledTimeoutSeconds
                        var last = stalledState.AddOrUpdate(
                            t.Id,
                            (t.BytesTransferred, t.BytesTransferred > 0 ? null : DateTime.UtcNow),
                            (_, prev) =>
                            {
                                if (t.BytesTransferred > prev.LastBytes)
                                    return (t.BytesTransferred, (DateTime?)null);
                                var first = prev.FirstNoProgressAt ?? DateTime.UtcNow;
                                return (prev.LastBytes, first);
                            });

                        if (last.FirstNoProgressAt.HasValue)
                        {
                            var stalledElapsed = (DateTime.UtcNow - last.FirstNoProgressAt.Value).TotalSeconds;
                            if (stalledElapsed >= rescue.StalledTimeoutSeconds)
                            {
                                await TriggerRescueAsync(t, UnderperformanceReason.Stalled, ct).ConfigureAwait(false);
                                stalledState.TryRemove(t.Id, out _);
                            }
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(rescue.CheckIntervalSeconds), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.Warning(ex, "[RESCUE] Underperformance detector loop error: {Message}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                }
            }
        }

        private async Task TriggerRescueAsync(slskd.Transfers.Transfer t, UnderperformanceReason reason, CancellationToken ct)
        {
            try
            {
                log.Information("[RESCUE] Triggering rescue for {File} from {User} (reason: {Reason})", t.Filename, t.Username, reason);
                var result = await rescueService.ActivateRescueModeAsync(
                    t.Id.ToString(),
                    t.Username,
                    t.Filename,
                    t.Size,
                    t.BytesTransferred,
                    reason,
                    ct).ConfigureAwait(false);
                RecordRescueAttempt(t.Id, result.Outcome, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                RecordRescueAttempt(t.Id, RescueActivationOutcome.Failed, DateTime.UtcNow);
                log.Warning(ex, "[RESCUE] ActivateRescueModeAsync failed for {TransferId}: {Message}", t.Id, ex.Message);
            }
        }

        internal bool ShouldSkipRecentRescueAttempt(Guid transferId, DateTime now)
        {
            if (!rescueAttempts.TryGetValue(transferId, out var attempt))
            {
                return false;
            }

            if (now - attempt.AttemptedAt < GetCooldown(attempt.Outcome))
            {
                return true;
            }

            rescueAttempts.TryRemove(transferId, out _);
            return false;
        }

        internal void RecordRescueAttempt(Guid transferId, RescueActivationOutcome outcome, DateTime now)
        {
            rescueAttempts[transferId] = new RescueAttemptState(now, outcome);
        }

        private static TimeSpan GetCooldown(RescueActivationOutcome outcome)
        {
            return outcome switch
            {
                RescueActivationOutcome.Activated => NoOverlayPeersCooldown,
                RescueActivationOutcome.NoRecordingId => NoRecordingCooldown,
                RescueActivationOutcome.NoOverlayPeers => NoOverlayPeersCooldown,
                RescueActivationOutcome.GuardrailDenied => GuardrailCooldown,
                RescueActivationOutcome.MultiSourceDenied => GuardrailCooldown,
                RescueActivationOutcome.MultiSourceUnavailable => MultiSourceUnavailableCooldown,
                RescueActivationOutcome.Failed => FailedActivationCooldown,
                _ => FailedActivationCooldown,
            };
        }

        private sealed record RescueAttemptState(DateTime AttemptedAt, RescueActivationOutcome Outcome);
    }
}
