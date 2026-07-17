// <copyright file="LibraryItemsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.API.Native;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Core.Security;
using slskd.HashDb;
using slskd.Shares;
using slskd.VirtualSoulfind.Core;
using Soulseek;

/// <summary>
/// Provides library items search API for E2E tests and Collections UI.
/// Returns shared files with stable contentId (sha256-based) for deterministic testing.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/library/items")]
[ApiVersion("0")]
[Produces("application/json")]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
public class LibraryItemsController : ControllerBase
{
    private readonly IShareService shareService;
    private readonly IHashDbService? hashDbService;
    private readonly ILogger<LibraryItemsController>? logger;
    private readonly IOptionsSnapshot<slskd.Options>? options;

    public LibraryItemsController(
        IShareService shareService,
        IHashDbService? hashDbService = null,
        ILogger<LibraryItemsController>? logger = null,
        IOptionsSnapshot<slskd.Options>? options = null)
    {
        this.shareService = shareService;
        this.hashDbService = hashDbService;
        this.logger = logger;
        this.options = options;
    }

    /// <summary>
    /// Search library items (shared files) by query string.
    /// </summary>
    /// <param name="query">Search query (matches filename).</param>
    /// <param name="kinds">Optional comma-separated list of media kinds (Audio, Video, Book, etc.).</param>
    /// <param name="limit">Maximum number of results (default: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of library items with stable contentId.</returns>
    [HttpGet]
    [Authorize(Policy = AuthPolicy.Any)]
    public async Task<IActionResult> SearchItems(
        [FromQuery] string? query = null,
        [FromQuery] string? kinds = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        kinds = string.IsNullOrWhiteSpace(kinds) ? null : kinds.Trim();
        limit = Math.Clamp(limit, 1, 100);
        logger?.LogInformation("Library items search: query={Query}, kinds={Kinds}, limit={Limit}", query, kinds, limit);

        try
        {
            // Get all shared files
            var allFiles = new List<Soulseek.File>();
            var directories = await shareService.BrowseAsync();

            foreach (var dir in directories)
            {
                if (dir.Files != null)
                {
                    allFiles.AddRange(dir.Files);
                }
            }

            // Filter by query if provided
            IEnumerable<Soulseek.File> filtered = allFiles;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryLower = query.ToLowerInvariant();
                filtered = allFiles.Where(f =>
                    f.Filename.ToLowerInvariant().Contains(queryLower));
            }

            // Filter by media kind if provided
            if (!string.IsNullOrWhiteSpace(kinds))
            {
                var kindSet = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(k => k.ToLowerInvariant())
                    .ToHashSet();

                filtered = filtered.Where(f =>
                {
                    var ext = Path.GetExtension(f.Filename).TrimStart('.').ToLowerInvariant();
                    var kind = GetMediaKind(ext);
                    return kindSet.Contains(kind.ToLowerInvariant());
                });
            }

            // Limit results
            var results = filtered.Take(limit).ToList();

            var codeToMasked = BuildCodeToMaskedFilenameMap();
            var items = await ConvertToLibraryItemsAsync(
                results.Select(file => new LibraryItemCandidate(
                    File: file,
                    RemoteFilename: GetMaskedFilename(file, codeToMasked),
                    DisplayPath: null,
                    DuplicateCount: 1)),
                cancellationToken).ConfigureAwait(false);

            if (items.Count == 0 && options != null)
            {
                var fallbackItems = await SearchLocalDirectoriesAsync(
                    query,
                    kinds,
                    limit,
                    cancellationToken);
                return Ok(new { items = fallbackItems });
            }

            return Ok(new { items });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error searching library items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Browse library items by virtual share path.
    /// </summary>
    /// <param name="path">Virtual share path to browse. Empty path lists roots.</param>
    /// <param name="query">Optional filename search. When present, searches recursively.</param>
    /// <param name="kinds">Optional comma-separated list of media kinds.</param>
    /// <param name="limit">Maximum number of files to return.</param>
    /// <param name="offset">Zero-based file offset for paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Folder entries and a paged file result set.</returns>
    [HttpGet("browser")]
    [Authorize(Policy = AuthPolicy.Any)]
    public async Task<IActionResult> BrowseItems(
        [FromQuery] string? path = null,
        [FromQuery] string? query = null,
        [FromQuery] string? kinds = "Audio",
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var browserPath = NormalizeVirtualPath(path);
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        kinds = string.IsNullOrWhiteSpace(kinds) ? null : kinds.Trim();
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        try
        {
            var directories = (await shareService.BrowseAsync()).ToList();
            var codeToMasked = BuildCodeToMaskedFilenameMap();
            var directoryEntries = query == null
                ? BuildDirectoryEntries(directories, browserPath)
                : new List<LibraryDirectoryResponse>();
            var filePage = BuildFilePage(
                directories,
                codeToMasked,
                browserPath,
                query,
                kinds,
                offset,
                limit);
            var items = await ConvertToLibraryItemsAsync(
                filePage.Files.Select(file => new LibraryItemCandidate(
                    File: file.File,
                    RemoteFilename: file.RemoteFilename,
                    DisplayPath: file.Path,
                    DuplicateCount: file.DuplicateCount)),
                cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                path = browserPath,
                breadcrumbs = BuildBreadcrumbs(browserPath),
                directories = directoryEntries,
                files = items,
                totalFiles = filePage.TotalFiles,
                totalDirectories = directoryEntries.Count,
                offset,
                limit,
                hasMore = offset + items.Count < filePage.TotalFiles,
                duplicatesRemoved = filePage.CandidateCount - filePage.TotalFiles,
            });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error browsing library items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<List<LibraryItemResponse>> SearchLocalDirectoriesAsync(
        string? query,
        string? kinds,
        int limit,
        CancellationToken cancellationToken)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        kinds = string.IsNullOrWhiteSpace(kinds) ? null : kinds.Trim();
        if (options == null)
        {
            return new List<LibraryItemResponse>();
        }

        var localDirs = GetAllowedLocalDirectories();

        if (!localDirs.Any())
        {
            return new List<LibraryItemResponse>();
        }

        var regexOptions = options.Value.Flags.CaseSensitiveRegEx
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;
        var filters = options.Value.Shares.Filters
            .Select(filter => new Regex(filter, regexOptions))
            .ToList();

        var files = localDirs.SelectMany(localDir =>
        {
            try
            {
                return System.IO.Directory.EnumerateFiles(
                    localDir,
                    "*",
                    SearchOption.AllDirectories);
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });

        if (!string.IsNullOrWhiteSpace(query))
        {
            var queryLower = query.ToLowerInvariant();
            files = files.Where(file =>
                Path.GetFileName(file).ToLowerInvariant().Contains(queryLower));
        }

        if (!string.IsNullOrWhiteSpace(kinds))
        {
            var kindSet = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant())
                .ToHashSet();
            files = files.Where(file =>
            {
                var ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                var kind = GetMediaKind(ext);
                return kindSet.Contains(kind.ToLowerInvariant());
            });
        }

        files = files.Where(file => !filters.Any(filter => filter.IsMatch(file)));

        var items = new List<LibraryItemResponse>();
        foreach (var file in files.Take(limit))
        {
            var item = await ConvertToLibraryItemFromPathAsync(file, localDirs, cancellationToken);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private IReadOnlyList<string> GetAllowedLocalDirectories()
    {
        if (options == null)
        {
            return Array.Empty<string>();
        }

        return options.Value.Shares.Directories
            .Select(raw => new Share(raw))
            .Where(share => !share.IsExcluded)
            .Select(share => share.LocalPath)
            .Concat(new[] { options.Value.Directories.Downloads })
            .Where(path => !string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<LibraryItemResponse?> ConvertToLibraryItemFromPathAsync(
        string filename,
        IReadOnlyList<string> localDirs,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!System.IO.File.Exists(filename))
            {
                return null;
            }

            var info = new FileInfo(filename);
            var size = info.Length;
            string? sha256 = null;

            if (hashDbService != null)
            {
                try
                {
                    var flacKey = HashDb.Models.HashDbEntry.GenerateFlacKey(filename, size);
                    var hashEntry = await hashDbService.LookupHashAsync(flacKey, cancellationToken);
                    if (hashEntry != null && !string.IsNullOrEmpty(hashEntry.FileSha256))
                    {
                        sha256 = hashEntry.FileSha256;
                    }
                }
                catch
                {
                    // HashDb lookup failed, will compute on-demand if needed
                }
            }

            if (string.IsNullOrEmpty(sha256))
            {
                try
                {
                    sha256 = await ComputeSha256Async(filename, cancellationToken);
                }
                catch
                {
                    // File may not be accessible, skip sha256
                }
            }

            var contentId = !string.IsNullOrEmpty(sha256)
                ? $"sha256:{sha256}"
                : $"path:{slskd.Compute.Sha256Hash($"{filename}|{size}")}";

            try
            {
                shareService.GetLocalRepository().UpsertContentItem(
                    contentId,
                    ContentDomain.GenericFile.ToString(),
                    string.Empty,
                    filename,
                    true,
                    string.Empty,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to upsert local file content item for {Filename}", filename);
            }

            var ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            var mediaKind = GetMediaKind(ext);
            var fileName = Path.GetFileName(filename);

            return new LibraryItemResponse
            {
                ContentId = contentId,
                Path = ToDisplayPath(filename, localDirs),
                FileName = fileName,
                Bytes = size,
                MediaKind = mediaKind,
                Sha256 = sha256,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get library item metadata by contentId.
    /// </summary>
    /// <param name="contentId">Content ID (sha256:... or path-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Library item metadata.</returns>
    [HttpGet("{contentId}")]
    [Authorize(Policy = AuthPolicy.Any)]
    public async Task<IActionResult> GetItem(
            string contentId,
            CancellationToken cancellationToken = default)
    {
        contentId = contentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return BadRequest(new { error = "ContentId is required" });
        }

        logger?.LogInformation("Get library item: contentId={ContentId}", contentId);

        try
        {
            // Search all files to find one matching the contentId
            var directories = await shareService.BrowseAsync();
            Soulseek.File? foundFile = null;

            var codeToMasked = BuildCodeToMaskedFilenameMap();
            foreach (var dir in directories)
            {
                if (dir.Files != null)
                {
                    foreach (var file in dir.Files)
                    {
                        var maskedFilename = GetMaskedFilename(file, codeToMasked);
                        var item = await ConvertToLibraryItemAsync(file, maskedFilename, cancellationToken);
                        if (item != null && item.ContentId == contentId)
                        {
                            foundFile = file;
                            break;
                        }
                    }

                    if (foundFile != null)
                    {
                        break;
                    }
                }
            }

            if (foundFile == null)
            {
                return NotFound(new { error = "Item not found" });
            }

            var foundItem = await ConvertToLibraryItemAsync(
                foundFile,
                GetMaskedFilename(foundFile, codeToMasked),
                cancellationToken);
            return Ok(foundItem);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting library item");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private IReadOnlyDictionary<int, string> BuildCodeToMaskedFilenameMap()
    {
        var files = shareService.GetLocalRepository().ListFiles(includeFullPath: true);
        return files
            .GroupBy(file => file.Code)
            .ToDictionary(group => group.Key, group => group.First().Filename);
    }

    private static string GetMaskedFilename(
        Soulseek.File file,
        IReadOnlyDictionary<int, string> codeToMasked)
    {
        if (codeToMasked.TryGetValue(file.Code, out var masked))
        {
            return masked;
        }

        return file.Filename;
    }

    private async Task<LibraryItemResponse?> ConvertToLibraryItemAsync(
        Soulseek.File file,
        string maskedFilename,
        CancellationToken cancellationToken,
        string? displayPath = null)
    {
        try
        {
            // Resolve local file path
            var (_, filename, size) = await shareService.ResolveFileAsync(maskedFilename);

            // Try to get sha256 from HashDb first
            HashDb.Models.HashDbEntry? hashEntry = null;
            if (hashDbService != null)
            {
                try
                {
                    // Try to lookup by size (HashDb uses FlacKey which is based on filename+size)
                    var flacKey = HashDb.Models.HashDbEntry.GenerateFlacKey(filename, size);
                    hashEntry = await hashDbService.LookupHashAsync(flacKey, cancellationToken);
                }
                catch
                {
                    // HashDb lookup failed, will compute on-demand if needed
                }
            }

            return await ConvertResolvedLibraryItemAsync(
                new ResolvedLibraryItem(file, maskedFilename, displayPath, DuplicateCount: 1, filename, size),
                hashEntry,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to convert file to library item: {Filename}", file.Filename);
            return null;
        }
    }

    private async Task<List<LibraryItemResponse>> ConvertToLibraryItemsAsync(
        IEnumerable<LibraryItemCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedLibraryItem>();
        foreach (var candidate in candidates)
        {
            try
            {
                var (_, filename, size) = await shareService
                    .ResolveFileAsync(candidate.RemoteFilename)
                    .ConfigureAwait(false);
                resolved.Add(new ResolvedLibraryItem(
                    candidate.File,
                    candidate.RemoteFilename,
                    candidate.DisplayPath,
                    candidate.DuplicateCount,
                    filename,
                    size));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to resolve file: {Filename}", candidate.File.Filename);
            }
        }

        var hashesByFlacKey = new Dictionary<string, HashDb.Models.HashDbEntry>(StringComparer.Ordinal);
        if (hashDbService != null && resolved.Count > 0)
        {
            try
            {
                hashesByFlacKey = (await hashDbService
                        .LookupHashesByFlacKeysAsync(
                            resolved.Select(item => HashDb.Models.HashDbEntry.GenerateFlacKey(item.Filename, item.Size)),
                            cancellationToken)
                        .ConfigureAwait(false))
                    .ToDictionary(entry => entry.FlacKey, StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to batch lookup library item hashes");
            }
        }

        var items = new List<LibraryItemResponse>(resolved.Count);
        foreach (var item in resolved)
        {
            var flacKey = HashDb.Models.HashDbEntry.GenerateFlacKey(item.Filename, item.Size);
            var converted = await ConvertResolvedLibraryItemAsync(
                item,
                hashesByFlacKey.GetValueOrDefault(flacKey),
                cancellationToken).ConfigureAwait(false);
            if (converted != null)
            {
                converted.DuplicateCount = item.DuplicateCount;
                items.Add(converted);
            }
        }

        return items;
    }

    private async Task<LibraryItemResponse?> ConvertResolvedLibraryItemAsync(
        ResolvedLibraryItem item,
        HashDb.Models.HashDbEntry? hashEntry,
        CancellationToken cancellationToken)
    {
        try
        {
            var filename = item.Filename;
            var size = item.Size;
            var maskedFilename = item.RemoteFilename;
            var displayPath = item.DisplayPath;
            var sha256 = string.IsNullOrEmpty(hashEntry?.FileSha256) ? null : hashEntry.FileSha256;

            // If no sha256 from HashDb and file exists, compute it (for test fixtures)
            if (string.IsNullOrEmpty(sha256) && System.IO.File.Exists(filename))
            {
                try
                {
                    sha256 = await ComputeSha256Async(filename, cancellationToken);
                }
                catch
                {
                    // File may not be accessible, skip sha256
                }
            }

            // Generate stable contentId: prefer sha256, fallback to path-based
            var contentId = !string.IsNullOrEmpty(sha256)
                ? $"sha256:{sha256}"
                : $"path:{slskd.Compute.Sha256Hash($"{filename}|{size}")}";

            try
            {
                var checkedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var repo = shareService.GetLocalRepository();
                repo.UpsertContentItem(
                    contentId,
                    ContentDomain.GenericFile.ToString(),
                    string.Empty,
                    maskedFilename,
                    true,
                    string.Empty,
                    checkedAt);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to upsert content item for {Filename}", maskedFilename);
            }

            var ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            var mediaKind = GetMediaKind(ext);
            var fileName = GetVirtualFileName(displayPath ?? maskedFilename);

            return new LibraryItemResponse
            {
                ContentId = contentId,
                Path = displayPath ?? maskedFilename,
                FileName = fileName,
                Bytes = size,
                MediaKind = mediaKind,
                Sha256 = sha256,
            };
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to convert file to library item: {Filename}", item.File.Filename);
            return null;
        }
    }

    private static string GetMediaKind(string extension)
    {
        return extension switch
        {
            "mp3" or "flac" or "ogg" or "opus" or "aac" or "m4a" or "wav" => "Audio",
            "mp4" or "mkv" or "avi" or "mov" or "webm" => "Video",
            "txt" or "pdf" or "epub" or "mobi" => "Book",
            _ => "File",
        };
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

        var buffer = new byte[32768]; // 32KB chunks
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        var hashBytes = sha256.Hash ?? throw new InvalidOperationException("SHA256 hash computation failed");
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string ToDisplayPath(string filename, IReadOnlyList<string> roots)
    {
        var fullPath = Path.GetFullPath(filename);
        var root = roots.FirstOrDefault(candidate =>
            fullPath.StartsWith(candidate.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || string.Equals(fullPath, candidate, StringComparison.Ordinal));

        return root == null
            ? Path.GetFileName(filename)
            : Path.GetRelativePath(root, fullPath);
    }

    private static IReadOnlyList<LibraryBreadcrumbResponse> BuildBreadcrumbs(string path)
    {
        var breadcrumbs = new List<LibraryBreadcrumbResponse>
        {
            new() { Name = "Library", Path = string.Empty },
        };
        var current = string.Empty;
        foreach (var part in SplitVirtualPath(path))
        {
            current = string.IsNullOrEmpty(current) ? part : $"{current}\\{part}";
            breadcrumbs.Add(new LibraryBreadcrumbResponse { Name = part, Path = current });
        }

        return breadcrumbs;
    }

    private static List<LibraryDirectoryResponse> BuildDirectoryEntries(
        IReadOnlyList<Soulseek.Directory> directories,
        string path)
    {
        var indexed = new List<LibraryDirectoryIndexEntry>(directories.Count);
        var fileCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var childDirectoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in directories)
        {
            var directoryPath = NormalizeVirtualPath(directory.Name);
            var parentPath = GetParentVirtualPath(directoryPath);
            indexed.Add(new LibraryDirectoryIndexEntry(directoryPath, parentPath));
            fileCounts[directoryPath] = fileCounts.GetValueOrDefault(directoryPath) +
                (directory.Files?.Count() ?? 0);
            childDirectoryCounts[parentPath] = childDirectoryCounts.GetValueOrDefault(parentPath) + 1;
        }

        return indexed
            .Where(directory => !string.Equals(directory.Path, path, StringComparison.OrdinalIgnoreCase))
            .Where(directory => string.Equals(directory.ParentPath, path, StringComparison.OrdinalIgnoreCase))
            .Select(directory => new LibraryDirectoryResponse
            {
                Name = SplitVirtualPath(directory.Path).LastOrDefault() ?? directory.Path,
                Path = directory.Path,
                FileCount = fileCounts[directory.Path],
                ChildDirectoryCount = childDirectoryCounts.GetValueOrDefault(directory.Path),
            })
            .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetParentVirtualPath(string path)
    {
        var separatorIndex = path.LastIndexOf('\\');
        return separatorIndex < 0 ? string.Empty : path[..separatorIndex];
    }

    private static LibraryFilePage BuildFilePage(
        IReadOnlyList<Soulseek.Directory> directories,
        IReadOnlyDictionary<int, string> codeToMasked,
        string path,
        string? query,
        string? kinds,
        int offset,
        int limit)
    {
        var queryLower = query?.ToLowerInvariant();
        var kindSet = string.IsNullOrWhiteSpace(kinds)
            ? null
            : kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.ToLowerInvariant())
                .ToHashSet();
        var groups = new Dictionary<LibraryFileGroupKey, LibraryFileGroup>(LibraryFileGroupKeyComparer.Instance);
        var candidateCount = 0;
        var sequence = 0;

        foreach (var directory in directories)
        {
            var directoryPath = NormalizeVirtualPath(directory.Name);
            foreach (var file in directory.Files ?? Enumerable.Empty<Soulseek.File>())
            {
                string remoteFilename;
                string displayPath;
                if (codeToMasked.TryGetValue(file.Code, out var masked))
                {
                    remoteFilename = masked;
                    displayPath = NormalizeVirtualPath(remoteFilename);
                }
                else
                {
                    remoteFilename = JoinVirtualPath(directoryPath, file.Filename);
                    displayPath = remoteFilename;
                }

                if (queryLower == null
                    ? !IsDirectChildFile(displayPath, path)
                    : !ContainsLowerInvariant(displayPath, queryLower))
                {
                    continue;
                }

                var fileName = GetNormalizedVirtualFileName(displayPath);
                if (kindSet == null)
                {
                    AddCandidate(file, remoteFilename, displayPath, fileName);
                    continue;
                }

                var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
                if (kindSet.Contains(GetMediaKind(ext).ToLowerInvariant()))
                {
                    AddCandidate(file, remoteFilename, displayPath, fileName);
                }
            }
        }

        if (offset >= groups.Count)
        {
            return new LibraryFilePage(new List<LibraryFileCandidate>(), groups.Count, candidateCount);
        }

        var selectionLimit = (int)Math.Min(groups.Count, (long)offset + limit);
        var selected = new PriorityQueue<LibraryFileGroup, LibraryFileGroup>(LibraryFileGroupWorstFirstComparer.Instance);
        foreach (var group in groups.Values)
        {
            if (selected.Count < selectionLimit)
            {
                selected.Enqueue(group, group);
            }
            else if (LibraryFileGroupWorstFirstComparer.Instance.Compare(group, selected.Peek()) > 0)
            {
                selected.Dequeue();
                selected.Enqueue(group, group);
            }
        }

        var files = new List<LibraryFileCandidate>(Math.Min(limit, groups.Count - offset));
        var selectedPosition = selectionLimit;
        while (selected.Count > 0)
        {
            selectedPosition--;
            if (selectedPosition < offset)
            {
                break;
            }

            var group = selected.Dequeue();
            files.Add(group.Candidate with { DuplicateCount = group.DuplicateCount });
        }

        files.Reverse();
        return new LibraryFilePage(files, groups.Count, candidateCount);

        void AddCandidate(Soulseek.File file, string remoteFilename, string displayPath, string fileName)
        {
            candidateCount++;
            var candidate = new LibraryFileCandidate(file, remoteFilename, displayPath, file.Size, fileName);
            var key = query == null
                ? new LibraryFileGroupKey(candidate.Path, 0)
                : new LibraryFileGroupKey(candidate.FileName, candidate.Bytes);
            if (groups.TryGetValue(key, out var group))
            {
                groups[key] = group with { DuplicateCount = group.DuplicateCount + 1 };
                return;
            }

            groups.Add(key, new LibraryFileGroup(candidate, 1, sequence++));
        }
    }

    private static bool IsDirectChildFile(string filePath, string directoryPath)
    {
        filePath = NormalizeVirtualPath(filePath);
        var separatorIndex = filePath.LastIndexOf('\\');
        var parent = separatorIndex < 0 ? string.Empty : filePath[..separatorIndex];
        return string.Equals(parent, directoryPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string JoinVirtualPath(string directory, string filename)
    {
        directory = NormalizeVirtualPath(directory);
        filename = NormalizeVirtualPath(filename);
        return string.IsNullOrEmpty(directory) ? filename : $"{directory}\\{filename}";
    }

    private static string GetVirtualFileName(string path)
    {
        return SplitVirtualPath(path).LastOrDefault() ?? path;
    }

    private static string GetNormalizedVirtualFileName(string path)
    {
        var separatorIndex = path.LastIndexOf('\\');
        return separatorIndex < 0 ? path : path[(separatorIndex + 1)..];
    }

    private static bool ContainsLowerInvariant(string value, string lowerValue)
    {
        const int StackBufferLength = 256;
        char[]? rentedBuffer = null;
        var buffer = value.Length <= StackBufferLength
            ? stackalloc char[value.Length]
            : rentedBuffer = ArrayPool<char>.Shared.Rent(value.Length);

        try
        {
            var written = value.AsSpan().ToLowerInvariant(buffer);
            return written >= 0
                ? buffer[..written].IndexOf(lowerValue.AsSpan()) >= 0
                : value.ToLowerInvariant().Contains(lowerValue);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }
    }

    private static string NormalizeVirtualPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (IsNormalizedVirtualPath(path))
        {
            return path;
        }

        return string.Join(
            "\\",
            path.Replace('/', '\\')
                .Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static bool IsNormalizedVirtualPath(string path)
    {
        var atSegmentStart = true;
        var previousWasWhitespace = false;
        foreach (var character in path)
        {
            if (character == '/' ||
                (character == '\\' && (atSegmentStart || previousWasWhitespace)) ||
                (atSegmentStart && char.IsWhiteSpace(character)))
            {
                return false;
            }

            if (character == '\\')
            {
                atSegmentStart = true;
                previousWasWhitespace = false;
                continue;
            }

            atSegmentStart = false;
            previousWasWhitespace = char.IsWhiteSpace(character);
        }

        return !atSegmentStart && !previousWasWhitespace;
    }

    private static IReadOnlyList<string> SplitVirtualPath(string path)
    {
        return NormalizeVirtualPath(path)
            .Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private readonly record struct LibraryFileCandidate(
        Soulseek.File File,
        string RemoteFilename,
        string Path,
        long Bytes,
        string FileName)
    {
        public int DuplicateCount { get; init; } = 1;
    }

    private readonly record struct LibraryFilePage(
        List<LibraryFileCandidate> Files,
        int TotalFiles,
        int CandidateCount);

    private readonly record struct LibraryFileGroupKey(string Value, long Bytes);

    private readonly record struct LibraryFileGroup(
        LibraryFileCandidate Candidate,
        int DuplicateCount,
        int Sequence);

    private sealed class LibraryFileGroupKeyComparer : IEqualityComparer<LibraryFileGroupKey>
    {
        public static LibraryFileGroupKeyComparer Instance { get; } = new();

        public bool Equals(LibraryFileGroupKey left, LibraryFileGroupKey right)
            => left.Bytes == right.Bytes &&
                string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(LibraryFileGroupKey value)
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(value.Value), value.Bytes);
    }

    private sealed class LibraryFileGroupWorstFirstComparer : IComparer<LibraryFileGroup>
    {
        public static LibraryFileGroupWorstFirstComparer Instance { get; } = new();

        public int Compare(LibraryFileGroup left, LibraryFileGroup right)
        {
            var pathComparison = StringComparer.OrdinalIgnoreCase.Compare(
                right.Candidate.Path,
                left.Candidate.Path);
            return pathComparison != 0
                ? pathComparison
                : right.Sequence.CompareTo(left.Sequence);
        }
    }

    private sealed record LibraryItemCandidate(
        Soulseek.File File,
        string RemoteFilename,
        string? DisplayPath,
        int DuplicateCount);

    private sealed record ResolvedLibraryItem(
        Soulseek.File File,
        string RemoteFilename,
        string? DisplayPath,
        int DuplicateCount,
        string Filename,
        long Size);

    private sealed record LibraryDirectoryIndexEntry(string Path, string ParentPath);

    private class LibraryItemResponse
    {
        public string ContentId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Bytes { get; set; }
        public string MediaKind { get; set; } = string.Empty;
        public string? Sha256 { get; set; }
        public int DuplicateCount { get; set; } = 1;
    }

    private class LibraryDirectoryResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public int ChildDirectoryCount { get; set; }
    }

    private class LibraryBreadcrumbResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
