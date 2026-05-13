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
    using slskd.HashDb;
    using slskd.Transfers.AutoReplace;
    using slskd.Transfers.Ranking;

    /// <summary>
    ///     Background service that periodically re-enqueues downloads that ended in a retryable failed state
    ///     (TimedOut, Errored, Aborted), skipping Cancelled and Rejected.
    /// </summary>
    public class DownloadAutoRetryService : BackgroundService
    {
        private readonly IDownloadService downloadService;
        private readonly ISoulseekClient client;
        private readonly IOptionsMonitor<slskd.Options> options;
        private readonly IHashDbService? hashDb;
        private readonly IAutoReplaceService? autoReplace;
        private readonly ISourceRankingService? sourceRanking;
        private readonly ILogger log = Log.ForContext<DownloadAutoRetryService>();

        // Tracks transfer IDs already scheduled for retry so we don't double-queue the same failure.
        private readonly HashSet<Guid> retriedIds = [];

        // Tracks how many times each (username, filename) has been auto-retried this process lifetime.
        private readonly ConcurrentDictionary<string, int> retryCounts = new();

        // Keeps retries from repeatedly contacting the same Soulseek peer after failures.
        private readonly ConcurrentDictionary<string, DateTime> peerRetryCooldowns = new(StringComparer.OrdinalIgnoreCase);

        public DownloadAutoRetryService(
            IDownloadService downloadService,
            ISoulseekClient client,
            IOptionsMonitor<slskd.Options> options,
            IHashDbService? hashDb = null,
            IAutoReplaceService? autoReplace = null,
            ISourceRankingService? sourceRanking = null)
        {
            this.downloadService = downloadService;
            this.client = client;
            this.options = options;
            this.hashDb = hashDb;
            this.autoReplace = autoReplace;
            this.sourceRanking = sourceRanking;
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

            var now = DateTime.UtcNow;
            var plan = CreateRetryPlan(
                failed,
                retriedIds,
                retryCounts,
                peerRetryCooldowns,
                opts,
                now);

            if (plan.Count == 0)
            {
                return;
            }

            log.Information(
                "[AUTO-RETRY] Re-queueing {Count} failed download(s) across {PeerCount} peer(s); limits: global={GlobalLimit}, perPeer={PerPeerLimit}, peerCooldown={PeerCooldownSeconds}s",
                plan.Count,
                plan.Select(t => t.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                opts.MaxFilesPerCycle,
                opts.MaxFilesPerPeerPerCycle,
                opts.PeerCooldownSeconds);

            var alternateSearchBudgetRemaining = Math.Max(0, opts.MaxAlternateSourceSearchesPerCycle);
            var retryTargets = new List<RetryTarget>();

            foreach (var transfer in plan)
            {
                var target = await ResolveRetryTargetAsync(
                    transfer,
                    opts,
                    now,
                    alternateSearchBudgetRemaining > 0,
                    ct).ConfigureAwait(false);

                if (target.UsedNetworkSearch)
                {
                    alternateSearchBudgetRemaining--;
                }

                retryTargets.Add(target);
            }

            foreach (var group in retryTargets.GroupBy(t => t.Username, StringComparer.OrdinalIgnoreCase))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var username = group.Key;

                var eligible = group
                    .Where(target =>
                    {
                        var key = RetryKey(target);
                        return IsWithinAttemptBudget(retryCounts.GetOrAdd(key, 0), opts);
                    })
                    .ToList();

                if (eligible.Count == 0)
                {
                    continue;
                }

                // Mark as retried before enqueueing to prevent double-queuing if the service loops fast.
                foreach (var target in eligible)
                {
                    retriedIds.Add(target.Source.Id);
                }

                peerRetryCooldowns[username] = DateTime.UtcNow.AddSeconds(opts.PeerCooldownSeconds);

                try
                {
                    var files = eligible.Select(t => (t.Filename, t.Size)).ToList();
                    await downloadService.EnqueueAsync(username, files, ct);

                    foreach (var target in eligible)
                    {
                        var attempt = retryCounts.AddOrUpdate(RetryKey(target), 1, (_, c) => c + 1);
                        log.Information(
                            "[AUTO-RETRY] Re-queued {Filename} from {Username} via {SourceKind} (auto-retry #{Attempt}/{Max}, original={OriginalUsername}/{OriginalFilename}, state was {State})",
                            target.Filename,
                            username,
                            target.SourceKind,
                            attempt,
                            opts.MaxAttempts == 0 ? "unlimited" : opts.MaxAttempts.ToString(),
                            target.Source.Username,
                            target.Source.Filename,
                            target.Source.State);
                    }
                }
                catch (Exception ex)
                {
                    log.Warning(ex, "[AUTO-RETRY] Failed to re-enqueue from {Username}: {Message}", username, ex.Message);

                    // Remove from retried set so we'll attempt again next cycle.
                    foreach (var target in eligible)
                    {
                        retriedIds.Remove(target.Source.Id);
                    }
                }
            }
        }

        private static string RetryKey(slskd.Transfers.Transfer t) => $"{t.Username}:{t.Filename}";

        private static string RetryKey(RetryTarget t) => $"{t.Username}:{t.Filename}";

        internal async Task<RetryTarget> ResolveRetryTargetAsync(
            slskd.Transfers.Transfer failed,
            slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions opts,
            DateTime now,
            bool allowNetworkSearch,
            CancellationToken cancellationToken)
        {
            if (!opts.AlternateSourcesEnabled)
            {
                return RetryTarget.Original(failed);
            }

            var localCandidate = await FindLocalHashDbCandidateAsync(failed, opts, now, cancellationToken).ConfigureAwait(false);
            if (localCandidate != null)
            {
                return localCandidate;
            }

            if (!allowNetworkSearch || autoReplace == null)
            {
                return RetryTarget.Original(failed);
            }

            try
            {
                var alternatives = await autoReplace.FindAlternativesAsync(
                    new FindAlternativeRequest
                    {
                        Username = failed.Username,
                        Filename = failed.Filename,
                        Size = failed.Size,
                        Threshold = opts.AlternateSourceSizeTolerancePercent,
                    },
                    cancellationToken).ConfigureAwait(false);

                var candidate = alternatives
                    .Where(c => !IsSamePeer(c.Username, failed.Username))
                    .Where(c => !IsPeerCoolingDown(c.Username, now))
                    .FirstOrDefault();

                if (candidate != null)
                {
                    return new RetryTarget(failed, candidate.Username, candidate.Filename, candidate.Size, "search", UsedNetworkSearch: true);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Search rate limit exceeded", StringComparison.OrdinalIgnoreCase))
            {
                log.Information("[AUTO-RETRY] Alternative-source search budget exhausted; using original source for {Filename}", failed.Filename);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Debug(ex, "[AUTO-RETRY] Alternative-source search failed for {Filename}: {Message}", failed.Filename, ex.Message);
            }

            return RetryTarget.Original(failed);
        }

        private async Task<RetryTarget?> FindLocalHashDbCandidateAsync(
            slskd.Transfers.Transfer failed,
            slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions opts,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (hashDb == null || failed.Size <= 0 || !IsAudioFile(failed.Filename))
            {
                return null;
            }

            try
            {
                var expectedExtension = GetExtension(failed.Filename);
                var entries = await hashDb.GetFlacEntriesBySizeAsync(failed.Size, limit: 50, cancellationToken).ConfigureAwait(false);
                var candidates = entries
                    .Where(e => !IsSamePeer(e.PeerId, failed.Username))
                    .Where(e => !IsPeerCoolingDown(e.PeerId, now))
                    .Where(e => string.Equals(GetExtension(e.Path), expectedExtension, StringComparison.OrdinalIgnoreCase))
                    .Select(e => new SourceCandidate
                    {
                        Username = e.PeerId,
                        Filename = e.Path,
                        Size = e.Size,
                        SizeDiffPercent = 0,
                    })
                    .GroupBy(c => $"{c.Username}\u001f{c.Filename}", StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                if (candidates.Count == 0)
                {
                    return null;
                }

                IEnumerable<SourceCandidate> ranked = sourceRanking != null
                    ? await sourceRanking.RankSourcesAsync(candidates, cancellationToken).ConfigureAwait(false)
                    : candidates;

                var best = ranked.FirstOrDefault();
                return best == null
                    ? null
                    : new RetryTarget(failed, best.Username, best.Filename, best.Size, "hashdb", UsedNetworkSearch: false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Debug(ex, "[AUTO-RETRY] Local alternate-source lookup failed for {Filename}: {Message}", failed.Filename, ex.Message);
                return null;
            }
        }

        private bool IsPeerCoolingDown(string username, DateTime now)
            => peerRetryCooldowns.TryGetValue(username, out var retryAfter) && retryAfter > now;

        private static bool IsSamePeer(string left, string right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static bool IsAudioFile(string filename)
        {
            var extension = GetExtension(filename);
            return extension is ".flac" or ".mp3" or ".m4a" or ".aac" or ".ogg" or ".opus" or ".wav" or ".alac";
        }

        private static string GetExtension(string filename)
        {
            var lastSlash = filename.LastIndexOfAny(['\\', '/']);
            var leaf = lastSlash >= 0 ? filename[(lastSlash + 1)..] : filename;
            var lastDot = leaf.LastIndexOf('.');
            return lastDot >= 0 ? leaf[lastDot..].ToLowerInvariant() : string.Empty;
        }

        internal static IReadOnlyList<slskd.Transfers.Transfer> CreateRetryPlan(
            IEnumerable<slskd.Transfers.Transfer> failed,
            ISet<Guid> alreadyRetried,
            ConcurrentDictionary<string, int> retryCounts,
            ConcurrentDictionary<string, DateTime> peerRetryCooldowns,
            slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions opts,
            DateTime now)
        {
            var perPeerLimit = Math.Max(1, opts.MaxFilesPerPeerPerCycle);
            var globalLimit = Math.Max(1, opts.MaxFilesPerCycle);

            return failed
                .Where(t => !alreadyRetried.Contains(t.Id))
                .Where(t => IsWithinAttemptBudget(retryCounts.GetOrAdd(RetryKey(t), 0), opts))
                .Where(t => !peerRetryCooldowns.TryGetValue(t.Username, out var retryAfter) || retryAfter <= now)
                .GroupBy(t => t.Username, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Min(t => t.EndedAt ?? DateTime.MaxValue))
                .SelectMany(g => g
                    .OrderBy(t => t.EndedAt ?? DateTime.MaxValue)
                    .Take(perPeerLimit))
                .Take(globalLimit)
                .ToList();
        }

        private static bool IsWithinAttemptBudget(
            int currentAttempts,
            slskd.Options.GlobalOptions.GlobalDownloadOptions.AutoRetryOptions opts)
            => opts.MaxAttempts == 0 || currentAttempts < opts.MaxAttempts;

        internal sealed record RetryTarget(
            slskd.Transfers.Transfer Source,
            string Username,
            string Filename,
            long Size,
            string SourceKind,
            bool UsedNetworkSearch)
        {
            public static RetryTarget Original(slskd.Transfers.Transfer source)
                => new(source, source.Username, source.Filename, source.Size, "original", UsedNetworkSearch: false);
        }
    }
}
