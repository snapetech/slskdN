// <copyright file="SourceRankingService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.Ranking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using slskd.Events;

    /// <summary>
    ///     Service for ranking download sources using smart scoring.
    /// </summary>
    public class SourceRankingService : ISourceRankingService
    {
        // Scoring weights (same as frontend for consistency)
        private const double MaxSpeedScore = 40.0;
        private const double MaxQueueScore = 30.0;
        private const double FreeSlotBonus = 15.0;
        private const double MaxHistoryScore = 15.0;
        private const double MaxSizeMatchScore = 20.0;

        // Thresholds
        private const int MaxSpeedForScoring = 10_000_000; // 10 MB/s = max speed score
        private const int MaxQueueForScoring = 100; // Queue >= 100 = 0 score

        private readonly IDbContextFactory<SourceRankingDbContext> contextFactory;
        private readonly ILogger<SourceRankingService> logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SourceRankingService"/> class.
        /// </summary>
        /// <param name="contextFactory">The database context factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="eventBus">The event bus for subscribing to download events.</param>
        public SourceRankingService(
            IDbContextFactory<SourceRankingDbContext> contextFactory,
            ILogger<SourceRankingService> logger,
            EventBus eventBus)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;

            // Subscribe to download events to track history
            eventBus.Subscribe<DownloadFileCompleteEvent>("SourceRankingService.Success", OnDownloadComplete);
            eventBus.Subscribe<DownloadFileFailedEvent>("SourceRankingService.Failure", OnDownloadFailed);
        }

        private async Task OnDownloadComplete(DownloadFileCompleteEvent evt)
        {
            try
            {
                await RecordSuccessAsync(evt.Transfer.Username);
                logger.LogDebug("Recorded successful download from {Username}", evt.Transfer.Username);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record download success for {Username}", evt.Transfer.Username);
            }
        }

        private async Task OnDownloadFailed(DownloadFileFailedEvent evt)
        {
            try
            {
                await RecordFailureAsync(evt.Transfer.Username);
                logger.LogDebug("Recorded failed download from {Username}: {Error}", evt.Transfer.Username, evt.ErrorMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record download failure for {Username}", evt.Transfer.Username);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RankedSource>> RankSourcesAsync(
            IEnumerable<SourceCandidate> candidates,
            CancellationToken cancellationToken = default)
        {
            var candidateList = candidates.ToList();
            if (candidateList.Count == 0)
            {
                return Enumerable.Empty<RankedSource>();
            }

            var usernames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in candidateList)
            {
                usernames.Add(candidate.Username);
            }

            var histories = await GetExistingHistoryCountsAsync(usernames, cancellationToken).ConfigureAwait(false);
            var ranked = new List<RankedSource>(candidateList.Count);
            foreach (var candidate in candidateList)
            {
                histories.TryGetValue(candidate.Username, out var history);
                ranked.Add(CalculateScore(candidate, history.Successes, history.Failures));
            }

            return ranked.OrderByDescending(r => r.SmartScore).ToList();
        }

        private async Task<Dictionary<string, (int Successes, int Failures)>> GetExistingHistoryCountsAsync(
            HashSet<string> usernames,
            CancellationToken cancellationToken)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT "Username", "Successes", "Failures"
                FROM "DownloadHistory"
                WHERE "Username" IN (
                    SELECT CAST(value AS TEXT)
                    FROM json_each(@usernames)
                )
                """;
            var usernamesParameter = command.CreateParameter();
            usernamesParameter.ParameterName = "@usernames";
            usernamesParameter.Value = JsonSerializer.Serialize(usernames);
            command.Parameters.Add(usernamesParameter);

            var histories = new Dictionary<string, (int Successes, int Failures)>(StringComparer.Ordinal);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                histories[reader.GetString(0)] = (reader.GetInt32(1), reader.GetInt32(2));
            }

            return histories;
        }

        /// <inheritdoc/>
        public async Task RecordSuccessAsync(string username, CancellationToken cancellationToken = default)
        {
            await RecordHistoryAsync(username, isSuccess: true, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task RecordFailureAsync(string username, CancellationToken cancellationToken = default)
        {
            await RecordHistoryAsync(username, isSuccess: false, cancellationToken);
        }

        private async Task RecordHistoryAsync(string username, bool isSuccess, CancellationToken cancellationToken)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var successes = isSuccess ? 1 : 0;
            var failures = isSuccess ? 0 : 1;
            var lastUpdated = DateTime.UtcNow;

            await context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""DownloadHistory"" (""Username"", ""Successes"", ""Failures"", ""LastUpdated"")
                VALUES ({username}, {successes}, {failures}, {lastUpdated})
                ON CONFLICT(""Username"") DO UPDATE SET
                    ""Successes"" = ""DownloadHistory"".""Successes"" + {successes},
                    ""Failures"" = ""DownloadHistory"".""Failures"" + {failures},
                    ""LastUpdated"" = {lastUpdated}",
                cancellationToken);

            logger.LogDebug("Recorded {Type} for {Username}", isSuccess ? "success" : "failure", username);
        }

        /// <inheritdoc/>
        public async Task<UserDownloadHistory> GetHistoryAsync(string username, CancellationToken cancellationToken = default)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var entry = await context.DownloadHistory.FindAsync(new object[] { username }, cancellationToken);
            if (entry == null)
            {
                return new UserDownloadHistory { Username = username, Successes = 0, Failures = 0 };
            }

            return new UserDownloadHistory
            {
                Username = entry.Username,
                Successes = entry.Successes,
                Failures = entry.Failures,
            };
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, UserDownloadHistory>> GetHistoriesAsync(
            IEnumerable<string> usernames,
            CancellationToken cancellationToken = default)
        {
            var distinctUsernames = new List<string>();
            var usernameSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var username in usernames)
            {
                if (usernameSet.Add(username))
                {
                    distinctUsernames.Add(username);
                }
            }

            if (distinctUsernames.Count == 0)
            {
                return new Dictionary<string, UserDownloadHistory>(StringComparer.Ordinal);
            }

            var histories = await GetExistingHistoryCountsAsync(usernameSet, cancellationToken).ConfigureAwait(false);
            var result = new Dictionary<string, UserDownloadHistory>(distinctUsernames.Count, StringComparer.Ordinal);
            foreach (var username in distinctUsernames)
            {
                histories.TryGetValue(username, out var history);
                result[username] = new UserDownloadHistory
                {
                    Username = username,
                    Successes = history.Successes,
                    Failures = history.Failures,
                };
            }

            return result;
        }

        private RankedSource CalculateScore(SourceCandidate candidate, int successes, int failures)
        {
            // Speed score: 0-40 points based on upload speed
            // Scale: 0 B/s = 0, 10 MB/s+ = 40
            var speedScore = Math.Min(MaxSpeedScore, (double)candidate.UploadSpeed / MaxSpeedForScoring * MaxSpeedScore);

            // Queue score: 0-30 points, lower queue = higher score
            // Scale: 0 queue = 30, 100+ queue = 0
            var queueScore = Math.Max(0, MaxQueueScore * (1 - ((double)candidate.QueueLength / MaxQueueForScoring)));

            // Free slot bonus: 15 points if has free slot
            var freeSlotScore = candidate.HasFreeUploadSlot ? FreeSlotBonus : 0;

            // History score: -15 to +15 based on past success rate
            double historyScore = 0;
            if (successes + failures > 0)
            {
                // Center at 0.5 success rate = 0 points
                // 1.0 success rate = +15, 0.0 success rate = -15
                var successRate = (double)successes / (successes + failures);
                historyScore = (successRate - 0.5) * 2 * MaxHistoryScore;
            }

            // Size match score: 0-20 points for auto-replace scenarios
            // Perfect match (0% diff) = 20, 10%+ diff = 0
            double sizeMatchScore = 0;
            if (candidate.SizeDiffPercent.HasValue)
            {
                sizeMatchScore = Math.Max(0, MaxSizeMatchScore * (1 - (candidate.SizeDiffPercent.Value / 10.0)));
            }

            var totalScore = speedScore + queueScore + freeSlotScore + historyScore + sizeMatchScore;

            return new RankedSource
            {
                Username = candidate.Username,
                Filename = candidate.Filename,
                Size = candidate.Size,
                HasFreeUploadSlot = candidate.HasFreeUploadSlot,
                QueueLength = candidate.QueueLength,
                UploadSpeed = candidate.UploadSpeed,
                SizeDiffPercent = candidate.SizeDiffPercent,
                SmartScore = totalScore,
                SpeedScore = speedScore,
                QueueScore = queueScore,
                FreeSlotScore = freeSlotScore,
                HistoryScore = historyScore,
                SizeMatchScore = sizeMatchScore,
            };
        }
    }
}
