// <copyright file="ContentLocator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Shares;
using slskd.Common.Security;

/// <summary>
/// Resolves contentId via the local IShareRepository (FindContentItem → FindFileInfo). Path comes from originalFilename (local path).
/// Never uses client-supplied paths. Enforces IsAdvertisable.
/// </summary>
public sealed class ContentLocator : IContentLocator
{
    private const int MaxFallbackFilesToScan = 5000;
    private const int MaxFallbackMissCacheEntries = 4096;
    private static readonly ConcurrentDictionary<string, DateTimeOffset> FallbackMissCache = new(StringComparer.Ordinal);
    private static DateTimeOffset _nextFallbackScanUtc = DateTimeOffset.MinValue;

    private readonly ConcurrentDictionary<string, ResolvedContent> _fallbackHits = new(StringComparer.Ordinal);
    private readonly IShareService _shareService;
    private readonly ILogger<ContentLocator> _log;
    private readonly IOptionsMonitor<slskd.Options>? _options;

    public ContentLocator(
        IShareService shareService,
        ILogger<ContentLocator> log,
        IOptionsMonitor<slskd.Options>? options = null)
    {
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _options = options;
    }

    /// <inheritdoc />
    public ResolvedContent? Resolve(string contentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            return null;

        var repo = _shareService.GetLocalRepository();
        var ci = repo.FindContentItem(contentId);
        if (ci == null)
        {
            _log.LogDebug("[ContentLocator] ContentId {ContentId} not found", contentId);
            return ResolveFromAllowedLocalRoots(contentId, cancellationToken);
        }

        if (!ci.Value.IsAdvertisable)
        {
            _log.LogDebug("[ContentLocator] ContentId {ContentId} is not advertisable", contentId);
            return null;
        }

        var finfo = repo.FindFileInfo(ci.Value.MaskedFilename);
        if (string.IsNullOrEmpty(finfo.Filename) &&
            Path.IsPathFullyQualified(ci.Value.MaskedFilename))
        {
            var maskedLocalPath = NormalizeAllowedLocalPath(ci.Value.MaskedFilename);
            if (maskedLocalPath != null && File.Exists(maskedLocalPath))
            {
                var info = new FileInfo(maskedLocalPath);
                finfo = (maskedLocalPath, info.Length);
            }
        }

        if (string.IsNullOrEmpty(finfo.Filename))
        {
            _log.LogDebug("[ContentLocator] FindFileInfo returned no path for {Masked}", ci.Value.MaskedFilename);
            return null;
        }

        var allowedPath = NormalizeAllowedLocalPath(finfo.Filename);
        if (allowedPath == null)
        {
            _log.LogDebug("[ContentLocator] File is outside configured local roots: {Path}", finfo.Filename);
            return null;
        }

        if (!File.Exists(allowedPath))
        {
            _log.LogDebug("[ContentLocator] File no longer on disk: {Path}", finfo.Filename);
            return null;
        }

        var currentSize = new FileInfo(allowedPath).Length;
        if (currentSize <= 0) return null;
        var contentType = GetContentType(allowedPath);
        return new ResolvedContent(allowedPath, currentSize, contentType);
    }

    /// <inheritdoc />
    public string? RegisterLocalFile(string absolutePath)
    {
        // ADR-0013: a picker page supplies known paths without weakening fallback scan limits.
        var allowedPath = NormalizeAllowedLocalPath(absolutePath);
        if (allowedPath == null || !File.Exists(allowedPath)) return null;
        var info = new FileInfo(allowedPath);
        if (info.Length <= 0) return null;
        var contentId = $"path:{slskd.Compute.Sha256Hash($"{allowedPath}|{info.Length}")}";
        var item = _shareService.GetLocalRepository().FindContentItem(contentId);
        if (item.HasValue && !item.Value.IsAdvertisable) return null;
        if (_fallbackHits.Count >= MaxFallbackMissCacheEntries) _fallbackHits.Clear();
        _fallbackHits[contentId] = new ResolvedContent(allowedPath, info.Length, GetContentType(allowedPath));
        FallbackMissCache.TryRemove(contentId, out _);
        return contentId;
    }

    private ResolvedContent? ResolveFromAllowedLocalRoots(string contentId, CancellationToken cancellationToken)
    {
        if (_options == null)
        {
            return null;
        }

        if (_fallbackHits.TryGetValue(contentId, out var cached))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var allowedCachedPath = NormalizeAllowedLocalPath(cached.AbsolutePath);
            if (allowedCachedPath != null && File.Exists(allowedCachedPath))
            {
                var size = new FileInfo(allowedCachedPath).Length;
                if (size > 0 && size == cached.Length)
                    return new ResolvedContent(allowedCachedPath, size, cached.ContentType);
            }

            _fallbackHits.TryRemove(contentId, out _);
        }

        if (!contentId.StartsWith("path:", StringComparison.Ordinal) ||
            (FallbackMissCache.TryGetValue(contentId, out var cachedMissUntil) && cachedMissUntil > DateTimeOffset.UtcNow) ||
            DateTimeOffset.UtcNow < _nextFallbackScanUtc)
        {
            return null;
        }

        _nextFallbackScanUtc = DateTimeOffset.UtcNow.AddSeconds(5);

        var roots = GetAllowedLocalRoots();
        if (roots.Count == 0)
        {
            return null;
        }

        foreach (var path in EnumerateAllowedLocalFiles(roots, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var allowedPath = NormalizeAllowedLocalPath(path);
            if (allowedPath == null || !File.Exists(allowedPath))
            {
                continue;
            }

            var info = new FileInfo(allowedPath);
            if (info.Length <= 0)
            {
                continue;
            }

            var pathContentId = $"path:{slskd.Compute.Sha256Hash($"{allowedPath}|{info.Length}")}";
            if (!string.Equals(contentId, pathContentId, StringComparison.Ordinal))
            {
                continue;
            }

            var resolved = new ResolvedContent(allowedPath, info.Length, GetContentType(allowedPath));
            if (_fallbackHits.Count >= MaxFallbackMissCacheEntries) _fallbackHits.Clear();
            _fallbackHits[contentId] = resolved;
            return resolved;
        }

        PruneAndRecordFallbackMiss(contentId);
        return null;
    }

    private static void PruneAndRecordFallbackMiss(string contentId)
    {
        // Expired entries are only ever checked on read, so without periodic eviction the
        // static cache grows without bound. Prune when it approaches the cap.
        if (FallbackMissCache.Count >= MaxFallbackMissCacheEntries)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in FallbackMissCache)
            {
                if (entry.Value <= now)
                {
                    FallbackMissCache.TryRemove(entry.Key, out _);
                }
            }

            // Still over the cap after removing expired entries: skip caching this miss
            // rather than letting the dictionary grow unbounded.
            if (FallbackMissCache.Count >= MaxFallbackMissCacheEntries)
            {
                return;
            }
        }

        FallbackMissCache[contentId] = DateTimeOffset.UtcNow.AddMinutes(5);
    }

    private IReadOnlyList<string> GetAllowedLocalRoots()
    {
        var options = _options!.CurrentValue;
        var roots = options.Shares.Directories
            .Select(raw => new Share(raw))
            .Where(share => !share.IsExcluded)
            .Select(share => share.LocalPath)
            .Concat(new[] { options.Directories.Downloads })
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return roots;
    }

    private static IEnumerable<string> EnumerateAllowedLocalFiles(
        IReadOnlyList<string> roots,
        CancellationToken cancellationToken)
    {
        foreach (var root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", CreateFallbackEnumerationOptions());
            }
            catch
            {
                continue;
            }

            foreach (var file in files.Take(MaxFallbackFilesToScan))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return file;
            }
        }
    }

    internal static EnumerationOptions CreateFallbackEnumerationOptions()
    {
        return new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        };
    }

    private string? NormalizeAllowedLocalPath(string? path)
    {
        if (_options == null)
        {
            return null;
        }

        return PathGuard.NormalizeAbsolutePathWithinRoots(path, GetAllowedLocalRoots());
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return "application/octet-stream";
        return MimeByExtension.TryGetValue(ext.ToLowerInvariant(), out var mime) ? mime : "application/octet-stream";
    }

    private static readonly IReadOnlyDictionary<string, string> MimeByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".flac"] = "audio/flac",
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".wav"] = "audio/wav",
        [".aif"] = "audio/aiff",
        [".aiff"] = "audio/aiff",
        [".alac"] = "audio/mp4",
        [".ape"] = "audio/x-ape",
        [".m4b"] = "audio/mp4",
        [".wma"] = "audio/x-ms-wma",
        [".webm"] = "video/webm",
        [".mp4"] = "video/mp4",
        [".mkv"] = "video/x-matroska",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".txt"] = "text/plain",
        [".pdf"] = "application/pdf",
    };
}
