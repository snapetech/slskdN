// <copyright file="AutoReplaceService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.AutoReplace
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Options;
    using Serilog;
    using slskd.Search;
    using slskd.Transfers.API;
    using slskd.Transfers.Ranking;
    using Soulseek;
    using SlskdTransfer = slskd.Transfers.Transfer;

    /// <summary>
    ///     Service for automatically replacing stuck downloads with alternative sources.
    /// </summary>
    public interface IAutoReplaceService
    {
        /// <summary>
        ///     Gets all stuck downloads.
        /// </summary>
        /// <returns>A list of stuck downloads.</returns>
        IEnumerable<SlskdTransfer> GetStuckDownloads();

        /// <summary>
        ///     Finds alternative sources for a download.
        /// </summary>
        /// <param name="request">The request containing download details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of alternative candidates.</returns>
        Task<List<AlternativeCandidate>> FindAlternativesAsync(FindAlternativeRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Replaces a stuck download with an alternative source.
        /// </summary>
        /// <param name="request">The replacement request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if replacement was successful.</returns>
        Task<bool> ReplaceDownloadAsync(ReplaceDownloadRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Processes all stuck downloads and attempts auto-replacement.
        /// </summary>
        /// <param name="request">The auto-replace request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the auto-replace operation.</returns>
        Task<AutoReplaceResult> ProcessStuckDownloadsAsync(AutoReplaceRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Request for finding an alternative source for a download.
    /// </summary>
    public class FindAlternativeRequest
    {
        /// <summary>
        ///     Gets or sets the username of the original source.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the filename to find an alternative for.
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the expected file size.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        ///     Gets or sets the maximum size difference percentage for alternatives.
        /// </summary>
        public double Threshold { get; set; } = 5.0;
    }

    /// <summary>
    ///     Request for replacing a stuck download.
    /// </summary>
    public class ReplaceDownloadRequest
    {
        /// <summary>
        ///     Gets or sets the ID of the original download.
        /// </summary>
        public string OriginalId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the username of the original source.
        /// </summary>
        public string OriginalUsername { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the username of the new source.
        /// </summary>
        public string NewUsername { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the filename from the new source.
        /// </summary>
        public string NewFilename { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the size of the new file.
        /// </summary>
        public long NewSize { get; set; }
    }

    /// <summary>
    ///     Request for auto-replacing stuck downloads.
    /// </summary>
    public class AutoReplaceRequest
    {
        /// <summary>
        ///     Gets or sets the maximum size difference percentage for auto-replacement.
        /// </summary>
        public double Threshold { get; set; } = 5.0;
    }

    /// <summary>
    ///     Result of an auto-replace operation.
    /// </summary>
    public class AutoReplaceResult
    {
        /// <summary>
        ///     Gets or sets the number of downloads that were replaced.
        /// </summary>
        public int Replaced { get; set; }

        /// <summary>
        ///     Gets or sets the number of downloads that could not be replaced.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        ///     Gets or sets the number of downloads that were skipped.
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        ///     Gets or sets details about each replacement.
        /// </summary>
        public List<ReplacementDetail> Details { get; set; } = new List<ReplacementDetail>();
    }

    /// <summary>
    ///     Details about a specific replacement.
    /// </summary>
    public class ReplacementDetail
    {
        /// <summary>
        ///     Gets or sets the original filename.
        /// </summary>
        public string OriginalFilename { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the original username.
        /// </summary>
        public string OriginalUsername { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the new username.
        /// </summary>
        public string NewUsername { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the new filename.
        /// </summary>
        public string NewFilename { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the size difference percentage.
        /// </summary>
        public double SizeDiffPercent { get; set; }

        /// <summary>
        ///     Gets or sets whether the replacement was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the error message if the replacement failed.
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    ///     An alternative candidate for a stuck download.
    /// </summary>
    public class AlternativeCandidate
    {
        /// <summary>
        ///     Gets or sets the username of the alternative source.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the filename from the alternative source.
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the file size.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        ///     Gets or sets the size difference percentage from the original.
        /// </summary>
        public double SizeDiffPercent { get; set; }

        /// <summary>
        ///     Gets or sets whether the user has a free upload slot.
        /// </summary>
        public bool HasFreeUploadSlot { get; set; }

        /// <summary>
        ///     Gets or sets the user's queue length.
        /// </summary>
        public int QueueLength { get; set; }

        /// <summary>
        ///     Gets or sets the user's upload speed.
        /// </summary>
        public int UploadSpeed { get; set; }
    }

    /// <summary>
    ///     Implementation of <see cref="IAutoReplaceService"/>.
    /// </summary>
    public sealed class AutoReplaceService : IAutoReplaceService, IDisposable
    {
        private static readonly TransferStates[] StuckStates = new[]
        {
            TransferStates.Completed | TransferStates.TimedOut,
            TransferStates.Completed | TransferStates.Errored,
            TransferStates.Completed | TransferStates.Rejected,
            TransferStates.Completed | TransferStates.Cancelled,
        };

        private static readonly TimeSpan DefaultSearchCompletionTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan DefaultSearchPollInterval = TimeSpan.FromSeconds(2);
        private readonly SemaphoreSlim _searchBudgetGate = new(1, 1);
        private DateTimeOffset _nextAlternativeSearchNotBeforeUtc = DateTimeOffset.MinValue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AutoReplaceService"/> class.
        /// </summary>
        /// <param name="transferService">The transfer service.</param>
        /// <param name="searchService">The search service.</param>
        /// <param name="soulseekClient">The Soulseek client.</param>
        /// <param name="optionsMonitor">The options monitor.</param>
        /// <param name="rankingService">The source ranking service.</param>
        /// <param name="searchCompletionTimeout">How long to wait for the search finalizer to persist responses.</param>
        /// <param name="searchPollInterval">How often to poll for finalized search responses.</param>
        /// <param name="minimumSearchInterval">Optional override for the minimum interval between alternative searches.</param>
        /// <param name="transfersHub">Optional transfers hub used to emit immediate replacement UI removals.</param>
        public AutoReplaceService(
            ITransferService transferService,
            ISearchService searchService,
            ISoulseekClient soulseekClient,
            IOptionsMonitor<slskd.Options> optionsMonitor,
            ISourceRankingService rankingService,
            TimeSpan? searchCompletionTimeout = null,
            TimeSpan? searchPollInterval = null,
            TimeSpan? minimumSearchInterval = null,
            IHubContext<TransfersHub>? transfersHub = null)
        {
            Transfers = transferService;
            Searches = searchService;
            Client = soulseekClient;
            OptionsMonitor = optionsMonitor;
            RankingService = rankingService;
            SearchCompletionTimeout = searchCompletionTimeout ?? DefaultSearchCompletionTimeout;
            SearchPollInterval = searchPollInterval ?? DefaultSearchPollInterval;
            MinimumSearchIntervalOverride = minimumSearchInterval;
            TransfersHub = transfersHub;
        }

        private ITransferService Transfers { get; }

        private IHubContext<TransfersHub>? TransfersHub { get; }

        private ISearchService Searches { get; }

        private ISoulseekClient Client { get; }

        private IOptionsMonitor<slskd.Options> OptionsMonitor { get; }

        private ISourceRankingService RankingService { get; }

        private TimeSpan SearchCompletionTimeout { get; }

        private TimeSpan SearchPollInterval { get; }

        private TimeSpan? MinimumSearchIntervalOverride { get; }

        private ILogger Log { get; } = Serilog.Log.ForContext<AutoReplaceService>();

        /// <inheritdoc/>
        public IEnumerable<SlskdTransfer> GetStuckDownloads()
        {
            var stuckDownloads = Transfers.Downloads.List(t =>
                StuckStates.Any(s => t.State == s));

            var maxRetries = OptionsMonitor.CurrentValue?.AutoReplace.MaxRetries ?? 0;
            if (maxRetries == 0)
            {
                return stuckDownloads;
            }

            var downloads = stuckDownloads.ToList();
            var requestIds = downloads
                .Where(download => download.RequestId.HasValue)
                .Select(download => download.RequestId!.Value)
                .Distinct()
                .ToArray();

            var attemptsByRequest = requestIds.Length == 0
                ? new Dictionary<Guid, int>()
                : Transfers.Downloads
                    .List(
                        transfer => transfer.RequestId.HasValue && requestIds.Contains(transfer.RequestId.Value),
                        includeRemoved: true)
                    .GroupBy(transfer => transfer.RequestId!.Value)
                    .ToDictionary(group => group.Key, group => group.Count());

            return downloads.Where(download =>
            {
                var replacementCount = download.AutoReplaceAttempts;
                if (download.RequestId.HasValue && attemptsByRequest.TryGetValue(download.RequestId.Value, out var attemptCount))
                {
                    replacementCount = Math.Max(replacementCount, Math.Max(0, attemptCount - 1));
                }

                if (replacementCount < maxRetries)
                {
                    return true;
                }

                Log.Information(
                    "Auto-replace retry limit reached for {Filename} ({Attempts} replacement cycles, max retries {MaxRetries})",
                    CleanTrackTitle(download.Filename),
                    replacementCount,
                    maxRetries);
                return false;
            });
        }

        /// <inheritdoc/>
        public async Task<List<AlternativeCandidate>> FindAlternativesAsync(
            FindAlternativeRequest request,
            CancellationToken cancellationToken = default)
        {
            var (candidates, _) = await FindAlternativesWithStatusAsync(request, cancellationToken);
            return candidates;
        }

        private async Task<(List<AlternativeCandidate> Candidates, bool SearchBudgetExceeded)> FindAlternativesWithStatusAsync(
            FindAlternativeRequest request,
            CancellationToken cancellationToken = default)
        {
            var candidates = new List<AlternativeCandidate>();

            // Build search query from filename
            var searchText = CleanTrackTitle(request.Filename);
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Log.Warning("Could not build search text from filename: {Filename}", request.Filename);
                return (candidates, SearchBudgetExceeded: false);
            }

            Log.Debug("Searching for alternatives: {SearchText}", searchText);

            var searchId = Guid.NewGuid();
            var searchOptions = new Soulseek.SearchOptions(
                searchTimeout: 15000,
                responseLimit: 100,
                fileLimit: 1000);

            try
            {
                await WaitForSearchBudgetAsync(cancellationToken);

                await Searches.StartAsync(
                    searchId,
                    SearchQuery.FromText(searchText),
                    SearchScope.Network,
                    searchOptions,
                    requestedProviders: null,
                    safetySource: "auto-replace");

                var waited = TimeSpan.Zero;
                slskd.Search.Search? searchState = null;

                while (waited < SearchCompletionTimeout)
                {
                    await Task.Delay(SearchPollInterval, cancellationToken);
                    waited += SearchPollInterval;

                    searchState = await Searches.FindAsync(s => s.Id == searchId, includeResponses: false);

                    if (searchState?.State.HasFlag(SearchStates.Completed) == true)
                    {
                        break;
                    }
                }

                if (searchState?.State.HasFlag(SearchStates.Completed) != true)
                {
                    Log.Warning("Search for alternatives did not complete within {TimeoutSeconds}s: {SearchText}", SearchCompletionTimeout.TotalSeconds, searchText);
                    return (candidates, SearchBudgetExceeded: false);
                }

                var searchWithResponses = await Searches.FindAsync(s => s.Id == searchId, includeResponses: true);

                if (searchWithResponses?.Responses == null || !searchWithResponses.Responses.Any())
                {
                    Log.Debug("No search responses found for: {SearchText}", searchText);
                    return (candidates, SearchBudgetExceeded: false);
                }

                // Get expected extension
                var expectedExt = GetExtension(request.Filename)?.ToLowerInvariant();
                var expectedMatchTokens = GetMatchTokens(request.Filename);

                foreach (var response in searchWithResponses.Responses)
                {
                    // Skip the original source
                    if (response.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)
                        || IsOwnUsername(response.Username))
                    {
                        continue;
                    }

                    foreach (var file in response.Files)
                    {
                        // Check extension match
                        var fileExt = GetExtension(file.Filename)?.ToLowerInvariant();
                        if (!string.IsNullOrEmpty(expectedExt) && !string.IsNullOrEmpty(fileExt) && fileExt != expectedExt)
                        {
                            continue;
                        }

                        // Check size difference
                        if (file.Size <= 0)
                        {
                            continue;
                        }

                        var sizeDiff = Math.Abs(file.Size - request.Size) / (double)request.Size * 100;
                        if (sizeDiff > request.Threshold * 2)
                        {
                            continue;
                        }

                        if (!IsPlausibleFilenameMatch(expectedMatchTokens, file.Filename))
                        {
                            continue;
                        }

                        candidates.Add(new AlternativeCandidate
                        {
                            Username = response.Username,
                            Filename = file.Filename,
                            Size = file.Size,
                            SizeDiffPercent = sizeDiff,
                            HasFreeUploadSlot = response.HasFreeUploadSlot,
                            QueueLength = (int)response.QueueLength,
                            UploadSpeed = response.UploadSpeed,
                        });
                    }
                }

                // Use smart ranking service for scoring
                var sourceCandidates = candidates.Select(c => new SourceCandidate
                {
                    Username = c.Username,
                    Filename = c.Filename,
                    Size = c.Size,
                    HasFreeUploadSlot = c.HasFreeUploadSlot,
                    QueueLength = c.QueueLength,
                    UploadSpeed = c.UploadSpeed,
                    SizeDiffPercent = c.SizeDiffPercent,
                });

                var rankedSources = await RankingService.RankSourcesAsync(sourceCandidates, cancellationToken);

                // Convert back to AlternativeCandidate, taking top 10
                candidates = rankedSources.Take(10).Select(r => new AlternativeCandidate
                {
                    Username = r.Username,
                    Filename = r.Filename,
                    Size = r.Size,
                    SizeDiffPercent = r.SizeDiffPercent ?? 0,
                    HasFreeUploadSlot = r.HasFreeUploadSlot,
                    QueueLength = r.QueueLength,
                    UploadSpeed = r.UploadSpeed,
                }).ToList();

                if (candidates.Count > 0)
                {
                    Log.Information("Found {Count} alternative candidates for: {SearchText} (using smart ranking)", candidates.Count, searchText);
                }
                else
                {
                    Log.Debug("Found no alternative candidates for: {SearchText} (using smart ranking)", searchText);
                }
            }
            catch (InvalidOperationException ex) when (IsSearchRateLimitExceeded(ex))
            {
                Log.Warning("Search safety budget exhausted while finding alternatives for: {SearchText}. Deferring remaining auto-replace work.", searchText);
                return (candidates, SearchBudgetExceeded: true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching for alternatives: {Message}", ex.Message);
            }

            return (candidates, SearchBudgetExceeded: false);
        }

        private bool IsOwnUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(Client.Username)
                && string.Equals(username, Client.Username, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task<bool> ReplaceDownloadAsync(
            ReplaceDownloadRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(request.OriginalId, out var originalGuid))
                {
                    Log.Warning("Invalid original download ID: {Id}", request.OriginalId);
                    return false;
                }

                // Capture the original before removal so the UI can drop the
                // exact row the instant the replacement is enqueued, rather
                // than waiting for the periodic reconcile.
                var original = Transfers.Downloads.Find(t => t.Id == originalGuid);

                // Cancel and remove the original download
                Transfers.Downloads.TryCancel(originalGuid);
                Transfers.Downloads.Remove(originalGuid);

                if (original != default && TransfersHub != null)
                {
                    _ = TransfersHub.EmitTransferRemovedAsync(new TransferRemoved
                    {
                        Id = original.Id,
                        RequestId = original.RequestId,
                        Direction = original.Direction,
                        Username = original.Username,
                        Filename = original.Filename,
                    });
                }

                Log.Information("Removed stuck download from {Username}: {Filename}",
                    request.OriginalUsername,
                    CleanTrackTitle(request.NewFilename));

                // Enqueue the new download under the original request so the UI row stays stable.
                var (enqueued, failed) = await Transfers.Downloads.EnqueueAsync(
                    request.NewUsername,
                    new[]
                    {
                        new global::slskd.Transfers.Downloads.DownloadEnqueueRequest
                        {
                            Filename = request.NewFilename,
                            Size = request.NewSize,
                            RequestId = original?.RequestId,
                            BatchId = original?.BatchId,
                            DestinationDirectory = original?.DestinationDirectory,
                            AutoReplaceAttempts = original?.AutoReplaceAttempts ?? 0,
                        },
                    },
                    cancellationToken);

                if (enqueued.Count > 0)
                {
                    Log.Information("Enqueued replacement from {Username}: {Filename}",
                        request.NewUsername,
                        CleanTrackTitle(request.NewFilename));
                    return true;
                }
                else
                {
                    Log.Warning("Failed to enqueue replacement from {Username}: {Filename}",
                        request.NewUsername,
                        CleanTrackTitle(request.NewFilename));
                    return false;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error replacing download: {Message}", ex.Message);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<AutoReplaceResult> ProcessStuckDownloadsAsync(
            AutoReplaceRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new AutoReplaceResult();

            var stuckDownloads = GetStuckDownloads().ToList();
            if (stuckDownloads.Count == 0)
            {
                return result;
            }

            Log.Information("Processing {Count} stuck downloads for auto-replacement", stuckDownloads.Count);

            // Track processed to avoid duplicates
            var processedTracks = new HashSet<string>();

            foreach (var download in stuckDownloads)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var trackKey = download.Filename.ToLowerInvariant();
                if (processedTracks.Contains(trackKey))
                {
                    result.Skipped++;
                    continue;
                }

                processedTracks.Add(trackKey);

                var detail = new ReplacementDetail
                {
                    OriginalFilename = download.Filename,
                    OriginalUsername = download.Username,
                };

                try
                {
                    // Record this as a failure for ranking purposes
                    await RankingService.RecordFailureAsync(download.Username, cancellationToken);

                    // Find alternatives
                    var (alternatives, searchBudgetExceeded) = await FindAlternativesWithStatusAsync(
                        new FindAlternativeRequest
                        {
                            Username = download.Username,
                            Filename = download.Filename,
                            Size = download.Size,
                            Threshold = request.Threshold,
                        },
                        cancellationToken);

                    if (searchBudgetExceeded)
                    {
                        detail.Error = "Search safety budget exhausted; deferred to a later auto-replace cycle";
                        result.Skipped++;
                        result.Details.Add(detail);
                        Log.Information("Stopping auto-replace cycle early because the Soulseek search safety budget is exhausted");
                        break;
                    }

                    RecordAutoReplaceAttempt(download);

                    // Find the best candidate within threshold
                    var bestCandidate = alternatives
                        .Where(c => c.SizeDiffPercent <= request.Threshold)
                        .FirstOrDefault();

                    if (bestCandidate == null)
                    {
                        detail.Error = "No suitable alternative found";
                        result.Failed++;
                        result.Details.Add(detail);
                        continue;
                    }

                    detail.NewUsername = bestCandidate.Username;
                    detail.NewFilename = bestCandidate.Filename;
                    detail.SizeDiffPercent = bestCandidate.SizeDiffPercent;

                    // Replace the download
                    var replaced = await ReplaceDownloadAsync(
                        new ReplaceDownloadRequest
                        {
                            OriginalId = download.Id.ToString(),
                            OriginalUsername = download.Username,
                            NewUsername = bestCandidate.Username,
                            NewFilename = bestCandidate.Filename,
                            NewSize = bestCandidate.Size,
                        },
                        cancellationToken);

                    if (replaced)
                    {
                        detail.Success = true;
                        result.Replaced++;
                        Log.Information("Replaced: {Original} -> {New} (diff: {Diff:F1}%)",
                            CleanTrackTitle(download.Filename),
                            CleanTrackTitle(bestCandidate.Filename),
                            bestCandidate.SizeDiffPercent);
                    }
                    else
                    {
                        detail.Error = "Failed to enqueue replacement";
                        result.Failed++;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    detail.Error = "Auto-replace processing failed";
                    result.Failed++;
                    Log.Error(ex, "Error processing: {Filename}", CleanTrackTitle(download.Filename));
                }

                result.Details.Add(detail);

                // Brief delay between operations
                await Task.Delay(500, cancellationToken);
            }

            Log.Information("Auto-replace complete: {Replaced} replaced, {Failed} failed, {Skipped} skipped",
                result.Replaced,
                result.Failed,
                result.Skipped);

            return result;
        }

        private void RecordAutoReplaceAttempt(SlskdTransfer download)
        {
            download.AutoReplaceAttempts++;
            Transfers.Downloads.Update(download);
        }

        /// <summary>
        ///     Clean a track title for searching.
        /// </summary>
        private static string CleanTrackTitle(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return string.Empty;
            }

            // Handle both Windows and Unix path separators
            var name = filename;
            var lastBackslash = name.LastIndexOf('\\');
            var lastSlash = name.LastIndexOf('/');
            var lastSep = Math.Max(lastBackslash, lastSlash);
            if (lastSep >= 0)
            {
                name = name.Substring(lastSep + 1);
            }

            // Remove extension
            var lastDot = name.LastIndexOf('.');
            if (lastDot > 0)
            {
                name = name.Substring(0, lastDot);
            }

            // Replace underscores with spaces
            name = name.Replace("_", " ");

            // Strip quality/bitrate info
            name = Regex.Replace(name, @"\s*\(?\[?(?:FLAC|MP3|AAC|ALAC|WAV|OGG|WMA)[\s\d]*(?:kbps|kHz|bit)?\]?\)?", string.Empty, RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\s*\(?\[?\d+\s*kbps\]?\)?", string.Empty, RegexOptions.IgnoreCase);

            // Strip leading track numbers
            name = Regex.Replace(name, @"^[0-9]{1,4}[\s.\-)_]+", string.Empty);

            // Strip year patterns
            name = Regex.Replace(name, @"\s*[\(\[]?\d{4}[\)\]]?\s*", " ");

            // Collapse whitespace and trim
            name = Regex.Replace(name, @"\s+", " ").Trim();
            name = name.Trim('-', ' ');

            return name;
        }

        internal static bool IsPlausibleFilenameMatch(string expectedFilename, string candidateFilename)
        {
            var expected = GetMatchTokens(expectedFilename);

            return IsPlausibleFilenameMatch(expected, candidateFilename);
        }

        internal static bool IsPlausibleFilenameMatch(HashSet<string> expected, string candidateFilename)
        {
            if (expected.Count == 0)
            {
                return false;
            }

            var cleanCandidate = CleanTrackTitle(candidateFilename).ToLowerInvariant();
            if (expected.Count <= 2)
            {
                foreach (var token in expected)
                {
                    if (!ContainsMatchToken(cleanCandidate, token))
                    {
                        return false;
                    }
                }

                return true;
            }

            var requiredOverlap = Math.Max(2, (expected.Count + 1) / 2);
            var overlap = 0;
            foreach (var token in expected)
            {
                if (ContainsMatchToken(cleanCandidate, token) && ++overlap >= requiredOverlap)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsMatchToken(string candidate, string expectedToken)
        {
            var index = 0;
            while (index < candidate.Length)
            {
                while (index < candidate.Length && !IsAsciiLetterOrDigit(candidate[index]))
                {
                    index++;
                }

                var tokenStart = index;
                while (index < candidate.Length && IsAsciiLetterOrDigit(candidate[index]))
                {
                    index++;
                }

                var tokenLength = index - tokenStart;
                if (tokenLength > 1 && candidate.AsSpan(tokenStart, tokenLength).SequenceEqual(expectedToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAsciiLetterOrDigit(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9');
        }

        internal static HashSet<string> GetMatchTokens(string filename)
        {
            var clean = CleanTrackTitle(filename).ToLowerInvariant();
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in Regex.Matches(clean, "[a-z0-9]+"))
            {
                var token = match.Value;
                if (token.Length > 1 && !IgnoredMatchTokens.Contains(token))
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private static readonly HashSet<string> IgnoredMatchTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "the",
            "and",
            "feat",
            "ft",
            "with",
            "remaster",
            "remastered",
            "version",
            "explicit",
            "clean",
            "mono",
            "stereo",
            "disc",
            "cd",
        };

        private async Task WaitForSearchBudgetAsync(CancellationToken cancellationToken)
        {
            var minimumInterval = GetMinimumSearchInterval();
            if (minimumInterval <= TimeSpan.Zero)
            {
                return;
            }

            await _searchBudgetGate.WaitAsync(cancellationToken);

            try
            {
                var now = DateTimeOffset.UtcNow;
                if (_nextAlternativeSearchNotBeforeUtc > now)
                {
                    var delay = _nextAlternativeSearchNotBeforeUtc - now;
                    Log.Debug("Pacing alternative search for {DelaySeconds:F1}s to respect Soulseek search safety budget", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                    now = DateTimeOffset.UtcNow;
                }

                _nextAlternativeSearchNotBeforeUtc = now + minimumInterval;
            }
            finally
            {
                _searchBudgetGate.Release();
            }
        }

        private TimeSpan GetMinimumSearchInterval()
        {
            if (MinimumSearchIntervalOverride.HasValue)
            {
                return MinimumSearchIntervalOverride.Value;
            }

            var safetyOptions = OptionsMonitor.CurrentValue?.Soulseek.Safety;
            if (safetyOptions?.Enabled != true || safetyOptions.MaxSearchesPerMinute <= 0)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromMinutes(1.0 / safetyOptions.MaxSearchesPerMinute);
        }

        private static bool IsSearchRateLimitExceeded(Exception exception)
        {
            return exception.Message.Contains("Search rate limit exceeded", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _searchBudgetGate.Dispose();
        }

        /// <summary>
        ///     Get file extension handling both Windows and Unix paths.
        /// </summary>
        private static string? GetExtension(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            var lastBackslash = filename.LastIndexOf('\\');
            var lastSlash = filename.LastIndexOf('/');
            var lastSep = Math.Max(lastBackslash, lastSlash);
            var lastDot = filename.LastIndexOf('.');

            if (lastDot > lastSep && lastDot < filename.Length - 1)
            {
                return filename.Substring(lastDot + 1);
            }

            return null;
        }
    }
}
