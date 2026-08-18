// <copyright file="LidarrImportService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.Lidarr;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Serilog;
using slskd.Events;
using slskd.Transfers;
using slskd.Wishlist;

public interface ILidarrImportService
{
    Task<LidarrImportResult> ImportCompletedDirectoryAsync(string localDirectory, CancellationToken cancellationToken = default);

    Task<LidarrImportResult> ImportDirectoryAsync(string localDirectory, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LidarrImportHistoryRecord>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task<LidarrImportResult?> RetryImportAsync(Guid historyId, CancellationToken cancellationToken = default);
}

public sealed class LidarrImportService : BackgroundService, ILidarrImportService
{
    private const string SubscriberName = "LidarrImportService.DownloadDirectoryComplete";
    private static readonly TimeSpan CommandPollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CommandPollTimeout = TimeSpan.FromMinutes(30);

    public LidarrImportService(
        ILidarrClient lidarrClient,
        EventBus eventBus,
        IOptionsMonitor<global::slskd.Options> optionsMonitor,
        IDbContextFactory<TransfersDbContext>? transfersContextFactory = null,
        IWishlistService? wishlistService = null,
        IDbContextFactory<WishlistDbContext>? historyContextFactory = null)
    {
        LidarrClient = lidarrClient;
        EventBus = eventBus;
        OptionsMonitor = optionsMonitor;
        TransfersContextFactory = transfersContextFactory;
        WishlistService = wishlistService;
        HistoryContextFactory = historyContextFactory;
    }

    private ILidarrClient LidarrClient { get; }

    private EventBus EventBus { get; }

    private IOptionsMonitor<global::slskd.Options> OptionsMonitor { get; }
    private IDbContextFactory<TransfersDbContext>? TransfersContextFactory { get; }
    private IWishlistService? WishlistService { get; }
    private IDbContextFactory<WishlistDbContext>? HistoryContextFactory { get; }

    private ConcurrentDictionary<string, DateTime> RecentlyProcessed { get; } = new(StringComparer.Ordinal);

    private ConcurrentDictionary<Guid, byte> ActiveCommandMonitors { get; } = new();

    private ConcurrentDictionary<Guid, LidarrImportHistoryRecord> VolatileHistory { get; } = new();

    private SemaphoreSlim ImportGate { get; } = new(1, 1);

    private CancellationTokenSource MonitoringCancellation { get; } = new();

    private ILogger Log { get; } = Serilog.Log.ForContext<LidarrImportService>();

    public async Task<LidarrImportResult> ImportCompletedDirectoryAsync(string localDirectory, CancellationToken cancellationToken = default)
        => await ImportDirectoryAsync(localDirectory, requireAutoImportEnabled: true, bypassDebounce: false, retryOfId: null, cancellationToken: cancellationToken).ConfigureAwait(false);

    public async Task<LidarrImportResult> ImportDirectoryAsync(string localDirectory, CancellationToken cancellationToken = default)
        => await ImportDirectoryAsync(localDirectory, requireAutoImportEnabled: false, bypassDebounce: true, retryOfId: null, cancellationToken: cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LidarrImportHistoryRecord>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        if (HistoryContextFactory is null)
        {
            return VolatileHistory.Values
                .OrderByDescending(record => record.StartedAt)
                .Take(safeLimit)
                .ToList();
        }

        await using var context = await HistoryContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.LidarrImportHistory
            .AsNoTracking()
            .OrderByDescending(record => record.StartedAt)
            .Take(safeLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<LidarrImportResult?> RetryImportAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        LidarrImportHistoryRecord? history;
        if (HistoryContextFactory is null)
        {
            VolatileHistory.TryGetValue(historyId, out history);
        }
        else
        {
            await using var context = await HistoryContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            history = await context.LidarrImportHistory
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == historyId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (history is null)
        {
            return null;
        }

        var sourceDirectory = string.IsNullOrWhiteSpace(history.SourceDirectory)
            ? history.Directory
            : history.SourceDirectory;
        return await ImportDirectoryAsync(
            sourceDirectory,
            requireAutoImportEnabled: false,
            bypassDebounce: true,
            retryOfId: history.Id,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<LidarrImportResult> ImportDirectoryAsync(
        string localDirectory,
        bool requireAutoImportEnabled,
        bool bypassDebounce,
        Guid? retryOfId,
        CancellationToken cancellationToken)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        var sourceDirectory = localDirectory?.Trim() ?? string.Empty;
        var lidarrDirectory = string.IsNullOrWhiteSpace(sourceDirectory)
            ? string.Empty
            : MapPath(sourceDirectory, options.ImportPathFrom, options.ImportPathTo);

        if (requireAutoImportEnabled && options.ImportDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(options.ImportDelaySeconds), cancellationToken).ConfigureAwait(false);
        }

        var history = await BeginHistoryAsync(sourceDirectory, lidarrDirectory, retryOfId, cancellationToken).ConfigureAwait(false);

        try
        {
            if (!options.Enabled)
            {
                var result = new LidarrImportResult
                {
                    Enabled = false,
                    AutoImportEnabled = options.AutoImportCompleted,
                    SkippedReason = "Lidarr integration is disabled",
                    HistoryId = history.Id,
                };
                await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                return result;
            }

            if (requireAutoImportEnabled && !options.AutoImportCompleted)
            {
                var result = new LidarrImportResult
                {
                    Enabled = true,
                    AutoImportEnabled = false,
                    SkippedReason = "Automatic completed-directory import is disabled",
                    HistoryId = history.Id,
                };
                await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                return result;
            }

            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                var result = new LidarrImportResult
                {
                    Enabled = options.Enabled,
                    AutoImportEnabled = options.AutoImportCompleted,
                    SkippedReason = "Directory is empty",
                    HistoryId = history.Id,
                };
                await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                return result;
            }

            if (!bypassDebounce && !TryBeginProcessing(lidarrDirectory))
            {
                var result = new LidarrImportResult
                {
                    Enabled = options.Enabled,
                    AutoImportEnabled = options.AutoImportCompleted,
                    Directory = lidarrDirectory,
                    SkippedReason = "Recently processed",
                    HistoryId = history.Id,
                };
                await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                return result;
            }

            if (options.SkipAlreadyOwnedAlbums && !options.ImportReplaceExistingFiles)
            {
                var ownedReason = await GetAlreadyOwnedSkipReasonAsync(lidarrDirectory, cancellationToken).ConfigureAwait(false);
                if (ownedReason is not null)
                {
                    var result = new LidarrImportResult
                    {
                        Enabled = options.Enabled,
                        AutoImportEnabled = options.AutoImportCompleted,
                        Directory = lidarrDirectory,
                        SkippedReason = ownedReason,
                        HistoryId = history.Id,
                    };
                    Log.Information("Lidarr auto-import skipped {Directory}: {Reason}", lidarrDirectory, ownedReason);
                    await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                    return result;
                }
            }

            var maxAttempts = options.ImportRetryMaxAttempts + 1;
            var retryDelay = TimeSpan.FromSeconds(options.ImportRetryDelaySeconds);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await ImportGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var candidates = await LidarrClient
                        .GetManualImportCandidatesAsync(
                            lidarrDirectory,
                            filterExistingFiles: false,
                            replaceExistingFiles: options.ImportReplaceExistingFiles,
                            cancellationToken)
                        .ConfigureAwait(false);

                    var safeCandidates = candidates
                        .Where(candidate => candidate.IsSafeAutomaticImportCandidate)
                        .ToList();

                    foreach (var candidate in safeCandidates)
                    {
                        candidate.ReplaceExistingFiles = options.ImportReplaceExistingFiles;
                    }

                    var result = new LidarrImportResult
                    {
                        Enabled = options.Enabled,
                        AutoImportEnabled = options.AutoImportCompleted,
                        Directory = lidarrDirectory,
                        CandidateCount = candidates.Count,
                        SafeCandidateCount = safeCandidates.Count,
                        RejectedCandidateCount = candidates.Count - safeCandidates.Count,
                        RejectedFilenames = candidates
                            .Where(candidate => !candidate.IsSafeAutomaticImportCandidate)
                            .Select(candidate => GetPortableFileName(candidate.Path))
                            .Where(filename => !string.IsNullOrWhiteSpace(filename))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray(),
                        HistoryId = history.Id,
                    };

                    if (safeCandidates.Count == 0)
                    {
                        result.SkippedReason = candidates.Count == 0
                            ? "Lidarr found no import candidates"
                            : "Lidarr candidates had rejections or ambiguous matches";
                        Log.Information(
                            "Lidarr auto-import skipped {Directory}: {Reason} ({Candidates} candidates)",
                            lidarrDirectory,
                            result.SkippedReason,
                            candidates.Count);
                        await CompleteHistoryAsync(history.Id, result, LidarrImportStatus.Skipped, cancellationToken).ConfigureAwait(false);
                        return result;
                    }

                    var importMode = NormalizeImportMode(options.ImportMode);
                    var command = await LidarrClient
                        .StartManualImportAsync(safeCandidates, importMode, options.ImportReplaceExistingFiles, cancellationToken)
                        .ConfigureAwait(false);

                    result.CommandId = command.Id;
                    result.ImportMode = importMode;
                    await QueueHistoryAsync(history.Id, result, command, cancellationToken).ConfigureAwait(false);
                    Log.Information(
                        "Queued Lidarr manual import command {CommandId} for {Directory}: {SafeCandidates}/{Candidates} safe candidates",
                        command.Id,
                        lidarrDirectory,
                        safeCandidates.Count,
                        candidates.Count);

                    if (command.Id > 0 && !IsTerminalCommandStatus(command.Status))
                    {
                        StartCommandMonitor(history.Id, command.Id);
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < maxAttempts &&
                    (IsExpectedExternalHttpFailure(ex) || (ex is OperationCanceledException timeoutEx && IsHttpClientTimeout(timeoutEx))))
                {
                    Log.Information(
                        "Lidarr manual import attempt {Attempt}/{MaxAttempts} failed for {Directory}: {Message}; retrying in {Delay}",
                        attempt,
                        maxAttempts,
                        lidarrDirectory,
                        ex.Message,
                        retryDelay);
                }
                finally
                {
                    ImportGate.Release();
                }

                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                retryDelay += retryDelay;
            }

            throw new InvalidOperationException("Lidarr manual import retry loop exited without returning or throwing.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkHistoryFailedAsync(history.Id, "Import canceled.").ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await MarkHistoryFailedAsync(history.Id, ex.Message).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<LidarrImportHistoryRecord> BeginHistoryAsync(
        string sourceDirectory,
        string lidarrDirectory,
        Guid? retryOfId,
        CancellationToken cancellationToken)
    {
        var history = new LidarrImportHistoryRecord
        {
            SourceDirectory = sourceDirectory,
            Directory = lidarrDirectory,
            RetryOfId = retryOfId,
        };
        VolatileHistory[history.Id] = history;

        if (HistoryContextFactory is null)
        {
            return history;
        }

        await using var context = await HistoryContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.LidarrImportHistory.Add(history);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return history;
    }

    private async Task CompleteHistoryAsync(
        Guid historyId,
        LidarrImportResult result,
        string status,
        CancellationToken cancellationToken)
    {
        await UpdateHistoryAsync(
            historyId,
            history =>
            {
                history.Status = status;
                history.Directory = string.IsNullOrWhiteSpace(result.Directory) ? history.Directory : result.Directory;
                history.CandidateCount = result.CandidateCount;
                history.SafeCandidateCount = result.SafeCandidateCount;
                history.RejectedCandidateCount = result.RejectedCandidateCount;
                history.CommandId = result.CommandId > 0 ? result.CommandId : null;
                history.ImportMode = result.ImportMode;
                history.SkippedReason = result.SkippedReason;
                history.CompletedAt = DateTime.UtcNow;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task QueueHistoryAsync(
        Guid historyId,
        LidarrImportResult result,
        LidarrCommandResponse command,
        CancellationToken cancellationToken)
    {
        var status = command.Id <= 0 ? LidarrImportStatus.Failed : MapCommandStatus(command.Status);
        var errorMessage = command.Id <= 0
            ? "Lidarr did not return a command ID."
            : GetCommandError(command, status);
        DateTime? completed = IsTerminalHistoryStatus(status) ? DateTime.UtcNow : null;

        await UpdateHistoryAsync(
            historyId,
            history =>
            {
                history.Status = status;
                history.Directory = string.IsNullOrWhiteSpace(result.Directory) ? history.Directory : result.Directory;
                history.CandidateCount = result.CandidateCount;
                history.SafeCandidateCount = result.SafeCandidateCount;
                history.RejectedCandidateCount = result.RejectedCandidateCount;
                history.CommandId = command.Id > 0 ? command.Id : null;
                history.ImportMode = result.ImportMode;
                history.ErrorMessage = errorMessage;
                history.CompletedAt = completed;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task MarkHistoryFailedAsync(Guid historyId, string errorMessage)
    {
        try
        {
            await UpdateHistoryAsync(
                historyId,
                history =>
                {
                    history.Status = LidarrImportStatus.Failed;
                    history.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Lidarr import failed." : errorMessage;
                    history.CompletedAt = DateTime.UtcNow;
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not persist Lidarr import failure for history entry {HistoryId}", historyId);
        }
    }

    private async Task UpdateHistoryAsync(
        Guid historyId,
        Action<LidarrImportHistoryRecord> update,
        CancellationToken cancellationToken)
    {
        if (VolatileHistory.TryGetValue(historyId, out var volatileHistory))
        {
            update(volatileHistory);
        }

        if (HistoryContextFactory is null)
        {
            return;
        }

        await using var context = await HistoryContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var history = await context.LidarrImportHistory
            .SingleOrDefaultAsync(record => record.Id == historyId, cancellationToken)
            .ConfigureAwait(false);
        if (history is null)
        {
            return;
        }

        update(history);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void StartCommandMonitor(Guid historyId, int commandId)
    {
        if (!ActiveCommandMonitors.TryAdd(historyId, 0))
        {
            return;
        }

        _ = MonitorCommandAsync(historyId, commandId);
    }

    private async Task MonitorCommandAsync(Guid historyId, int commandId)
    {
        try
        {
            var deadline = DateTime.UtcNow + CommandPollTimeout;
            while (DateTime.UtcNow < deadline)
            {
                var command = await LidarrClient.GetCommandAsync(commandId, MonitoringCancellation.Token).ConfigureAwait(false);
                var status = MapCommandStatus(command.Status);
                await UpdateHistoryAsync(
                    historyId,
                    history =>
                    {
                        history.Status = status;
                        history.ErrorMessage = GetCommandError(command, status);
                        if (IsTerminalHistoryStatus(status))
                        {
                            history.CompletedAt = DateTime.UtcNow;
                        }
                    },
                    MonitoringCancellation.Token).ConfigureAwait(false);

                if (IsTerminalHistoryStatus(status))
                {
                    return;
                }

                await Task.Delay(CommandPollInterval, MonitoringCancellation.Token).ConfigureAwait(false);
            }

            await MarkHistoryFailedAsync(historyId, "Lidarr command did not finish within 30 minutes.").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (MonitoringCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await MarkHistoryFailedAsync(historyId, ex.Message).ConfigureAwait(false);
            Log.Warning(ex, "Could not track Lidarr import command {CommandId}", commandId);
        }
        finally
        {
            ActiveCommandMonitors.TryRemove(historyId, out _);
        }
    }

    private async Task ResumePendingImportsAsync(CancellationToken cancellationToken)
    {
        if (HistoryContextFactory is null)
        {
            foreach (var history in VolatileHistory.Values.Where(IsPendingHistory))
            {
                StartCommandMonitor(history.Id, history.CommandId!.Value);
            }

            return;
        }

        await using var context = await HistoryContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var pending = await context.LidarrImportHistory
            .AsNoTracking()
            .Where(history => history.CommandId.HasValue &&
                history.CommandId.Value > 0 &&
                (history.Status == LidarrImportStatus.Queued || history.Status == LidarrImportStatus.Running))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var history in pending)
        {
            StartCommandMonitor(history.Id, history.CommandId!.Value);
        }
    }

    private static bool IsPendingHistory(LidarrImportHistoryRecord history)
        => history.CommandId.HasValue &&
            history.CommandId.Value > 0 &&
            (history.Status == LidarrImportStatus.Queued || history.Status == LidarrImportStatus.Running);

    private static string MapCommandStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "completed" => LidarrImportStatus.Successful,
            "failed" or "aborted" or "cancelled" or "canceled" or "orphaned" => LidarrImportStatus.Failed,
            "started" or "running" or "processing" => LidarrImportStatus.Running,
            _ => LidarrImportStatus.Queued,
        };
    }

    private static bool IsTerminalCommandStatus(string? status)
        => IsTerminalHistoryStatus(MapCommandStatus(status));

    private static bool IsTerminalHistoryStatus(string status)
        => status == LidarrImportStatus.Successful || status == LidarrImportStatus.Failed;

    private static string GetCommandError(LidarrCommandResponse command, string status)
    {
        if (status != LidarrImportStatus.Failed)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(command.ErrorMessage))
        {
            return command.ErrorMessage;
        }

        if (!string.IsNullOrWhiteSpace(command.Message))
        {
            return command.Message;
        }

        return string.IsNullOrWhiteSpace(command.Status)
            ? "Lidarr command failed."
            : $"Lidarr command ended with status '{command.Status}'.";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await ResumePendingImportsAsync(stoppingToken).ConfigureAwait(false);

        EventBus.Subscribe<DownloadDirectoryCompleteEvent>(
            SubscriberName,
            async evt =>
            {
                var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
                if (!options.Enabled || !options.AutoImportCompleted)
                {
                    return;
                }

                try
                {
                    var result = await ImportCompletedDirectoryAsync(evt.LocalDirectoryName, stoppingToken).ConfigureAwait(false);
                    if (result.RejectedCandidateCount > 0)
                    {
                        await ApplyRejectedDownloadPolicyAsync(evt, result, stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (OperationCanceledException ex) when (IsHttpClientTimeout(ex))
                {
                    Log.Information(
                        "Lidarr auto-import unavailable for {Directory}: {Message}",
                        evt.LocalDirectoryName,
                        ex.Message);
                }
                catch (Exception ex) when (IsExpectedExternalHttpFailure(ex))
                {
                    Log.Information(
                        "Lidarr auto-import unavailable for {Directory}: {Message}",
                        evt.LocalDirectoryName,
                        ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Lidarr auto-import failed for {Directory}: {Message}", evt.LocalDirectoryName, ex.Message);
                }
            });

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            EventBus.Unsubscribe<DownloadDirectoryCompleteEvent>(SubscriberName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        MonitoringCancellation.Cancel();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyRejectedDownloadPolicyAsync(
        DownloadDirectoryCompleteEvent evt,
        LidarrImportResult result,
        CancellationToken cancellationToken)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        if (options.BlacklistRejectedDownloads && TransfersContextFactory != null && WishlistService != null && evt.RequestId.HasValue &&
            !string.IsNullOrWhiteSpace(evt.Username) && !string.IsNullOrWhiteSpace(evt.RemoteDirectoryName))
        {
            await using var context = await TransfersContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var wishlistItemId = await context.DownloadRequests
                .Where(request => request.Id == evt.RequestId.Value)
                .Select(request => request.WishlistItemId)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (wishlistItemId.HasValue)
            {
                await WishlistService.IgnoreResultAsync(wishlistItemId.Value, evt.Username, evt.RemoteDirectoryName).ConfigureAwait(false);
                Log.Information(
                    "Excluded Lidarr-rejected Wishlist result from {Username} in {Directory}",
                    evt.Username,
                    evt.RemoteDirectoryName);
            }
        }

        if (options.DeleteRejectedDownloads && Directory.Exists(evt.LocalDirectoryName))
        {
            var directory = Path.GetFullPath(evt.LocalDirectoryName);
            foreach (var filename in result.RejectedFilenames)
            {
                var path = Path.GetFullPath(Path.Combine(directory, filename));
                if (Path.GetDirectoryName(path) == directory && File.Exists(path))
                {
                    File.Delete(path);
                    Log.Information("Deleted completed file rejected by Lidarr: {Filename}", filename);
                }
            }
        }
    }

    private static string GetPortableFileName(string? path)
    {
        var normalized = path?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
        var separator = normalized.LastIndexOf('/');
        return separator < 0 ? normalized : normalized[(separator + 1)..];
    }

    private static string NormalizeImportMode(string importMode)
        => string.Equals(importMode, "copy", StringComparison.OrdinalIgnoreCase) ? "Copy" : "Move";

    private static bool IsHttpClientTimeout(OperationCanceledException exception)
        => exception.Message.Contains("HttpClient.Timeout", StringComparison.Ordinal);

    internal static bool IsExpectedExternalHttpFailure(Exception ex)
        => ex is HttpRequestException || ex.InnerException is HttpRequestException;

    /// <summary>
    ///     Best-effort check for whether the release identified by <paramref name="lidarrDirectory"/>'s
    ///     folder name is already fully present in Lidarr's library, using Lidarr's <c>/parse</c>
    ///     endpoint (identification only, does not touch the filesystem) followed by a per-artist album
    ///     lookup. Some Lidarr versions throw an internal error when the manual-import scan itself
    ///     encounters a duplicate of an already-owned album, so this avoids that call entirely for the
    ///     common case of re-downloading something already owned.
    /// </summary>
    /// <returns>A human-readable skip reason, or <see langword="null"/> if the check found nothing owned
    /// (including when Lidarr couldn't identify the release, or the pre-check itself failed).</returns>
    private async Task<string?> GetAlreadyOwnedSkipReasonAsync(string lidarrDirectory, CancellationToken cancellationToken)
    {
        var releaseTitle = GetPortableFileName(lidarrDirectory);
        if (string.IsNullOrWhiteSpace(releaseTitle))
        {
            return null;
        }

        try
        {
            var parsed = await LidarrClient.ParseAsync(releaseTitle, cancellationToken).ConfigureAwait(false);
            var artist = parsed?.Artist;
            var albumTitle = parsed?.ParsedAlbumInfo?.AlbumTitle;

            if (artist is null || string.IsNullOrWhiteSpace(albumTitle))
            {
                return null;
            }

            var albums = await LidarrClient.GetAlbumsByArtistAsync(artist.Id, cancellationToken).ConfigureAwait(false);
            var match = albums.FirstOrDefault(album => string.Equals(album.Title, albumTitle, StringComparison.OrdinalIgnoreCase));

            if (match?.Statistics is { TotalTrackCount: > 0 } statistics && statistics.TrackFileCount >= statistics.TotalTrackCount)
            {
                return $"Already fully in Lidarr library ({artist.ArtistName} - {match.Title})";
            }
        }
        catch (Exception ex) when (IsExpectedExternalHttpFailure(ex) || (ex is OperationCanceledException timeoutEx && IsHttpClientTimeout(timeoutEx)))
        {
            // This is a best-effort optimization; if Lidarr is unavailable for the pre-check, fall
            // through to the normal manual-import attempt (with its own retry/failure handling)
            // rather than blocking the import on it.
            Log.Debug(ex, "Could not pre-check Lidarr library ownership for {Directory}", lidarrDirectory);
        }

        return null;
    }

    private static string MapPath(string path, string fromPrefix, string toPrefix)
    {
        var fullPath = Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(fromPrefix) || string.IsNullOrWhiteSpace(toPrefix))
        {
            return fullPath;
        }

        var normalizedFrom = Path.GetFullPath(fromPrefix).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!IsSameOrChildPath(fullPath, normalizedFrom))
        {
            return fullPath;
        }

        var relative = fullPath[normalizedFrom.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsWindowsPath(toPrefix))
        {
            var windowsRelative = relative.Replace('/', '\\');
            return toPrefix.TrimEnd('/', '\\') + (windowsRelative.Length == 0 ? string.Empty : "\\" + windowsRelative);
        }

        if (toPrefix.Contains('/') && !toPrefix.Contains('\\'))
        {
            return toPrefix.TrimEnd('/', '\\') + "/" + relative.Replace('\\', '/');
        }

        return Path.Combine(toPrefix, relative);
    }

    private bool TryBeginProcessing(string directory)
    {
        var now = DateTime.UtcNow;
        foreach (var item in RecentlyProcessed.Where(item => now - item.Value > TimeSpan.FromHours(1)).ToArray())
        {
            RecentlyProcessed.TryRemove(item.Key, out _);
        }

        return RecentlyProcessed.TryAdd(directory, now);
    }

    private static bool IsSameOrChildPath(string path, string prefix)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(path, prefix, comparison))
        {
            return true;
        }

        if (!path.StartsWith(prefix, comparison))
        {
            return false;
        }

        var next = path.Length > prefix.Length ? path[prefix.Length] : '\0';
        return next == Path.DirectorySeparatorChar || next == Path.AltDirectorySeparatorChar;
    }

    private static bool IsWindowsPath(string path)
        => (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
            || path.StartsWith("\\\\", StringComparison.Ordinal);
}

public sealed record LidarrImportResult
{
    public Guid HistoryId { get; init; }

    public bool Enabled { get; init; }

    public bool AutoImportEnabled { get; init; }

    public string Directory { get; init; } = string.Empty;

    public int CandidateCount { get; init; }

    public int SafeCandidateCount { get; init; }

    public int RejectedCandidateCount { get; init; }

    public IReadOnlyList<string> RejectedFilenames { get; init; } = [];

    public int CommandId { get; set; }

    public string ImportMode { get; set; } = string.Empty;

    public string SkippedReason { get; set; } = string.Empty;
}
