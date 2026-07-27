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
}

public sealed class LidarrImportService : BackgroundService, ILidarrImportService
{
    private const string SubscriberName = "LidarrImportService.DownloadDirectoryComplete";

    public LidarrImportService(
        ILidarrClient lidarrClient,
        EventBus eventBus,
        IOptionsMonitor<global::slskd.Options> optionsMonitor,
        IDbContextFactory<TransfersDbContext>? transfersContextFactory = null,
        IWishlistService? wishlistService = null)
    {
        LidarrClient = lidarrClient;
        EventBus = eventBus;
        OptionsMonitor = optionsMonitor;
        TransfersContextFactory = transfersContextFactory;
        WishlistService = wishlistService;
    }

    private ILidarrClient LidarrClient { get; }

    private EventBus EventBus { get; }

    private IOptionsMonitor<global::slskd.Options> OptionsMonitor { get; }
    private IDbContextFactory<TransfersDbContext>? TransfersContextFactory { get; }
    private IWishlistService? WishlistService { get; }

    private ConcurrentDictionary<string, DateTime> RecentlyProcessed { get; } = new(StringComparer.Ordinal);

    private SemaphoreSlim ImportGate { get; } = new(1, 1);

    private ILogger Log { get; } = Serilog.Log.ForContext<LidarrImportService>();

    public async Task<LidarrImportResult> ImportCompletedDirectoryAsync(string localDirectory, CancellationToken cancellationToken = default)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        if (!options.Enabled || !options.AutoImportCompleted)
        {
            return new LidarrImportResult { Enabled = options.Enabled, AutoImportEnabled = options.AutoImportCompleted };
        }

        if (string.IsNullOrWhiteSpace(localDirectory))
        {
            return new LidarrImportResult { Enabled = true, AutoImportEnabled = true, SkippedReason = "Directory is empty" };
        }

        var lidarrDirectory = MapPath(localDirectory, options.ImportPathFrom, options.ImportPathTo);
        if (!TryBeginProcessing(lidarrDirectory))
        {
            return new LidarrImportResult { Enabled = true, AutoImportEnabled = true, Directory = lidarrDirectory, SkippedReason = "Recently processed" };
        }

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
                Enabled = true,
                AutoImportEnabled = true,
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
                return result;
            }

            var importMode = NormalizeImportMode(options.ImportMode);
            var command = await LidarrClient
                .StartManualImportAsync(safeCandidates, importMode, options.ImportReplaceExistingFiles, cancellationToken)
                .ConfigureAwait(false);

            result.CommandId = command.Id;
            result.ImportMode = importMode;
            Log.Information(
                "Queued Lidarr manual import command {CommandId} for {Directory}: {SafeCandidates}/{Candidates} safe candidates",
                command.Id,
                lidarrDirectory,
                safeCandidates.Count,
                candidates.Count);

            return result;
        }
        finally
        {
            ImportGate.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

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
