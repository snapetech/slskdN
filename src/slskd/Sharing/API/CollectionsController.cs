// <copyright file="CollectionsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing.API;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Core.Security;
using slskd.Sharing;

/// <summary>Collection CRUD and items. Requires Feature.CollectionsSharing.</summary>
[ApiController]
[ApiVersion("0")]
[Route("api/v{version:apiVersion}/collections")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
[Produces("application/json")]
[Consumes("application/json")]
public class CollectionsController : ControllerBase
{
    private readonly ISharingService _sharing;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public CollectionsController(ISharingService sharing, IOptionsMonitor<slskd.Options> options)
    {
        _sharing = sharing;
        _options = options;
    }

    private string? GetCurrentUserId() => AuthenticatedWebUserId.Resolve(User);

    private bool Enabled => _options.CurrentValue.Feature.CollectionsSharing;

    [HttpGet]
    [ProducesResponseType(typeof(List<Collection>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();

        var list = await _sharing.GetCollectionsByOwnerAsync(currentUserId, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Collection), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null) return NotFound();

        // Owner access
        if (c.OwnerUserId == currentUserId) return Ok(c);

        if (await _sharing.HasCollectionAccessAsync(id, currentUserId, ct))
        {
            return Ok(c);
        }

        return NotFound();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(typeof(Collection), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCollectionRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Request is required.", Detail = "Request is required." });
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Title is required.", Detail = "Title is required." });
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var t = req.Type?.Trim() == CollectionType.Playlist ? CollectionType.Playlist : CollectionType.ShareList;
        var c = new Collection
        {
            Title = req.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Type = t,
            OwnerUserId = currentUserId
        };
        var created = await _sharing.CreateCollectionAsync(c, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCollectionRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        if (req.Title != null)
        {
            var title = req.Title.Trim();
            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Title cannot be blank.");
            c.Title = title;
        }

        if (req.Description != null) c.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        if (req.Type != null) c.Type = req.Type.Trim() == CollectionType.Playlist ? CollectionType.Playlist : CollectionType.ShareList;
        await _sharing.UpdateCollectionAsync(c, ct);
        return Ok(c);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        await _sharing.DeleteCollectionAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(List<CollectionItem>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetItems([FromRoute] Guid id, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        var items = await _sharing.GetCollectionItemsAsync(id, ct);
        return Ok(items);
    }

    [HttpPost("{id}/items")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(typeof(CollectionItem), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddItem([FromRoute] Guid id, [FromBody] AddCollectionItemRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest("Request is required.");
        if (string.IsNullOrWhiteSpace(req.ContentId)) return BadRequest("ContentId is required.");
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        var item = new CollectionItem
        {
            CollectionId = id,
            ContentId = req.ContentId.Trim(),
            MediaKind = string.IsNullOrWhiteSpace(req.MediaKind) ? null : req.MediaKind.Trim(),
            FileName = string.IsNullOrWhiteSpace(req.FileName) ? null : req.FileName.Trim(),
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            Artist = string.IsNullOrWhiteSpace(req.Artist) ? null : req.Artist.Trim(),
            Album = string.IsNullOrWhiteSpace(req.Album) ? null : req.Album.Trim(),
            ContentHash = NormalizeContentHash(req.ContentHash, req.Sha256)
        };
        var created = await _sharing.AddCollectionItemAsync(item, ct);
        return CreatedAtAction(nameof(GetItems), new { id }, created);
    }

    [HttpPut("{id}/items/{itemId}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateItem([FromRoute] Guid id, [FromRoute] Guid itemId, [FromBody] UpdateCollectionItemRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        var it = await _sharing.GetCollectionItemAsync(id, itemId, ct);
        if (it == null) return NotFound();
        if (req.ContentId != null)
        {
            var contentId = req.ContentId.Trim();
            if (string.IsNullOrWhiteSpace(contentId)) return BadRequest("ContentId cannot be blank.");
            it.ContentId = contentId;
        }

        if (req.MediaKind != null)
        {
            it.MediaKind = string.IsNullOrWhiteSpace(req.MediaKind) ? null : req.MediaKind.Trim();
        }

        if (req.ContentHash != null)
        {
            it.ContentHash = string.IsNullOrWhiteSpace(req.ContentHash) ? null : req.ContentHash.Trim();
        }

        if (req.Sha256 != null)
        {
            it.ContentHash = string.IsNullOrWhiteSpace(req.Sha256) ? null : req.Sha256.Trim();
        }

        if (req.FileName != null)
        {
            it.FileName = string.IsNullOrWhiteSpace(req.FileName) ? null : req.FileName.Trim();
        }

        if (req.Title != null)
        {
            it.Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
        }

        if (req.Artist != null)
        {
            it.Artist = string.IsNullOrWhiteSpace(req.Artist) ? null : req.Artist.Trim();
        }

        if (req.Album != null)
        {
            it.Album = string.IsNullOrWhiteSpace(req.Album) ? null : req.Album.Trim();
        }

        await _sharing.UpdateCollectionItemAsync(it, ct);
        return Ok(it);
    }

    [HttpDelete("{id}/items/{itemId}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        var ok = await _sharing.RemoveCollectionItemAsync(itemId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/items/reorder")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReorderItems([FromRoute] Guid id, [FromBody] ReorderRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest("ItemIds is required.");
        if (req.ItemIds == null || req.ItemIds.Count == 0) return BadRequest("ItemIds is required.");
        if (req.ItemIds.Any(itemId => itemId == Guid.Empty) || req.ItemIds.Count != req.ItemIds.Distinct().Count())
        {
            return BadRequest("ItemIds must be non-empty and unique.");
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var c = await _sharing.GetCollectionAsync(id, ct);
        if (c == null || c.OwnerUserId != currentUserId) return NotFound();
        await _sharing.ReorderCollectionItemsAsync(id, req.ItemIds, ct);
        return NoContent();
    }

    private static string? NormalizeContentHash(string? contentHash, string? sha256)
    {
        var raw = !string.IsNullOrWhiteSpace(contentHash) ? contentHash : sha256;
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }
}

public class CreateCollectionRequest
{
    [Required]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }
}

public class UpdateCollectionRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }
}

public class AddCollectionItemRequest
{
    [Required]
    public string? ContentId { get; set; }

    public string? MediaKind { get; set; }

    public string? ContentHash { get; set; }

    public string? Sha256 { get; set; }

    public string? FileName { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Album { get; set; }
}

public class UpdateCollectionItemRequest
{
    public string? ContentId { get; set; }

    public string? MediaKind { get; set; }

    public string? ContentHash { get; set; }

    public string? Sha256 { get; set; }

    public string? FileName { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Album { get; set; }
}

public class ReorderRequest
{
    [Required]
    public List<Guid>? ItemIds { get; set; }
}
