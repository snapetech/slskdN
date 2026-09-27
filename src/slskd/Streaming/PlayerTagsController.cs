// <copyright file="PlayerTagsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Core.Security;
using slskd.Sharing;
using slskd.Shares;

/// <summary>Manual tag edits for local server audio.</summary>
[ApiController]
[ApiVersion("0")]
[Route("api/v{version:apiVersion}/player-tags")]
[Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.AdministratorOnly)]
[ValidateCsrfForCookiesOnly]
public sealed class PlayerTagsController : ControllerBase
{
    private readonly IContentLocator _locator;
    private readonly IShareService _shares;
    private readonly IDbContextFactory<CollectionsDbContext> _collections;
    private readonly ILogger<PlayerTagsController> _logger;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public PlayerTagsController(
        IContentLocator locator,
        IShareService shares,
        IDbContextFactory<CollectionsDbContext> collections,
        ILogger<PlayerTagsController> logger,
        IOptionsMonitor<slskd.Options> options)
    {
        _locator = locator;
        _shares = shares;
        _collections = collections;
        _logger = logger;
        _options = options;
    }

    /// <summary>Updates tags on a local audio file and its collection references.</summary>
    [HttpPut("{contentId}")]
    public async Task<IActionResult> Update([FromRoute] string contentId, [FromBody] PlayerTagEdit edit, CancellationToken ct)
    {
        if (!_options.CurrentValue.Feature.Streaming) return NotFound();
        if (edit == null || string.IsNullOrWhiteSpace(edit.Title) || edit.Title.Length > 300 ||
            edit.Artist?.Length > 300 || edit.Album?.Length > 300) return BadRequest("Title is required and tags must be at most 300 characters.");
        var resolved = _locator.Resolve(contentId, ct);
        if (resolved == null || !resolved.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return NotFound();
        var oldItem = _shares.GetLocalRepository().FindContentItem(contentId);
        if (oldItem == null) return Conflict("This file must be indexed before its tags can be edited.");

        var path = resolved.AbsolutePath;
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}{Path.GetExtension(path)}");
        var backup = temporary + ".backup";
        var replaced = false;
        var committed = false;
        string? newContentId = null;
        try
        {
            // A live stream opens the source for shared reading, so an exclusive probe fails.
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            System.IO.File.Copy(path, temporary);
            using (var file = TagLib.File.Create(temporary))
            {
                file.Tag.Title = edit.Title.Trim();
                file.Tag.Performers = string.IsNullOrWhiteSpace(edit.Artist) ? Array.Empty<string>() : new[] { edit.Artist.Trim() };
                file.Tag.Album = edit.Album?.Trim() ?? string.Empty;
                file.Save();
            }

            ct.ThrowIfCancellationRequested();
            string newHash;
            await using (var hashStream = System.IO.File.OpenRead(temporary))
                newHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(hashStream, ct));
            newContentId = "sha256:" + newHash;
            System.IO.File.Replace(temporary, path, backup);
            replaced = true;
            var repo = _shares.GetLocalRepository();
            var checkedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            repo.UpsertContentItem(newContentId, oldItem.Value.Domain, oldItem.Value.WorkId,
                oldItem.Value.MaskedFilename, oldItem.Value.IsAdvertisable, oldItem.Value.ModerationReason, checkedAt);
            if (!string.Equals(newContentId, contentId, StringComparison.Ordinal))
            {
                repo.UpsertContentItem(contentId, oldItem.Value.Domain, oldItem.Value.WorkId,
                    oldItem.Value.MaskedFilename, false, "Tags changed", checkedAt);
                await using var db = await _collections.CreateDbContextAsync(ct);
                var oldHash = contentId.StartsWith("sha256:", StringComparison.Ordinal) ? contentId[7..] : null;
                await db.CollectionItems.Where(item => item.ContentId == contentId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.ContentId, newContentId)
                        .SetProperty(item => item.ContentHash, item =>
                            oldHash != null && (item.ContentHash == oldHash || item.ContentHash == contentId)
                                ? newHash
                                : item.ContentHash), ct);
            }

            committed = true;
        }
        catch (IOException)
        {
            return Conflict("The file is being played, is not writable, or could not be replaced.");
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return StatusCode(415, "Tag writing is not supported for this audio format.");
        }
        finally
        {
            if (replaced && !committed && System.IO.File.Exists(backup))
            {
                System.IO.File.Move(backup, path, overwrite: true);
                var repo = _shares.GetLocalRepository();
                var checkedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                repo.UpsertContentItem(contentId, oldItem.Value.Domain, oldItem.Value.WorkId,
                    oldItem.Value.MaskedFilename, oldItem.Value.IsAdvertisable, oldItem.Value.ModerationReason, checkedAt);
                if (newContentId != null && newContentId != contentId)
                {
                    repo.UpsertContentItem(newContentId, oldItem.Value.Domain, oldItem.Value.WorkId,
                        oldItem.Value.MaskedFilename, false, "Tag edit rolled back", checkedAt);
                }
            }

            if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary);
            if (committed && System.IO.File.Exists(backup)) System.IO.File.Delete(backup);
        }

        // Working copies must be gone before the share scanner sees this directory.
        var shareScanStarted = true;
        try
        {
            await _shares.ScanAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Audio tags were updated, but the share scan could not start");
            shareScanStarted = false;
        }

        return Ok(new { contentId = newContentId, title = edit.Title.Trim(), artist = edit.Artist?.Trim(), album = edit.Album?.Trim(), shareScanStarted });
    }
}

/// <summary>Editable audio tags.</summary>
public sealed class PlayerTagEdit
{
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string? Album { get; set; }
}
