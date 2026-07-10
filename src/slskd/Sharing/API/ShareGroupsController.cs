// <copyright file="ShareGroupsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing.API;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Core.Security;
using slskd.Sharing;

/// <summary>Share group CRUD and members. Requires Feature.CollectionsSharing.</summary>
[ApiController]
[ApiVersion("0")]
[Route("api/v{version:apiVersion}/sharegroups")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
[Produces("application/json")]
[Consumes("application/json")]
public class ShareGroupsController : ControllerBase
{
    private readonly ISharingService _sharing;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public ShareGroupsController(ISharingService sharing, IOptionsMonitor<slskd.Options> options)
    {
        _sharing = sharing;
        _options = options;
    }

    private string? GetCurrentUserId() => AuthenticatedWebUserId.Resolve(User);

    private bool Enabled => _options.CurrentValue.Feature.CollectionsSharing;

    [HttpGet]
    [ProducesResponseType(typeof(List<ShareGroup>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();

        var list = await _sharing.GetShareGroupsByOwnerAsync(currentUserId, ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShareGroup), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null) return NotFound();
        if (g.OwnerUserId != currentUserId) return NotFound();
        return Ok(g);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(typeof(ShareGroup), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateShareGroupRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null)
        {
            return Problem(
                title: "Request is required.",
                detail: "Request is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return Problem(
                title: "Name is required.",
                detail: "Name is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = new ShareGroup { Name = req.Name.Trim(), OwnerUserId = currentUserId };
        var created = await _sharing.CreateShareGroupAsync(g, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateShareGroupRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null) return BadRequest();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null || g.OwnerUserId != currentUserId) return NotFound();
        if (req.Name != null)
        {
            var name = req.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return Problem(
                    title: "Name is required.",
                    detail: "Name is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            g.Name = name;
        }

        await _sharing.UpdateShareGroupAsync(g, ct);
        return Ok(g);
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
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null || g.OwnerUserId != currentUserId) return NotFound();
        await _sharing.DeleteShareGroupAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id}/members")]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(typeof(List<ShareGroupMemberInfo>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMembers([FromRoute] Guid id, [FromQuery] bool detailed = false, CancellationToken ct = default)
    {
        if (!Enabled) return NotFound();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null || g.OwnerUserId != currentUserId) return NotFound();

        if (detailed)
        {
            var members = await _sharing.GetShareGroupMemberInfosAsync(id, ct);
            return Ok(members);
        }
        else
        {
            var members = await _sharing.GetShareGroupMembersAsync(id, ct);
            return Ok(members);
        }
    }

    [HttpPost("{id}/members")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddMember([FromRoute] Guid id, [FromBody] AddMemberRequest req, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        if (req == null)
            return BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "Request is required.", Detail = "Request is required." });
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null || g.OwnerUserId != currentUserId) return NotFound();

        // Support both UserId (legacy) and PeerId/ContactId (Identity & Friends)
        var peerId = req.PeerId?.Trim();
        var userId = req.UserId?.Trim();

        if (!string.IsNullOrWhiteSpace(peerId))
        {
            await _sharing.AddShareGroupMemberByPeerIdAsync(id, peerId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(userId))
        {
            await _sharing.AddShareGroupMemberAsync(id, userId, ct);
        }
        else
        {
            return BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = 400, Title = "UserId or PeerId is required.", Detail = "UserId or PeerId is required." });
        }

        return NoContent();
    }

    [HttpDelete("{id}/members/{userId}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid id, [FromRoute] string userId, CancellationToken ct)
    {
        if (!Enabled) return NotFound();
        userId = userId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userId)) return BadRequest();
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Forbid();
        var g = await _sharing.GetShareGroupAsync(id, ct);
        if (g == null || g.OwnerUserId != currentUserId) return NotFound();
        await _sharing.RemoveShareGroupMemberAsync(id, userId, ct);
        return NoContent();
    }
}

public class CreateShareGroupRequest
{
    [Required]
    public string? Name { get; set; }
}

public class UpdateShareGroupRequest
{
    public string? Name { get; set; }
}

public class AddMemberRequest
{
    /// <summary>Authenticated web-account ID.</summary>
    public string? UserId { get; set; }
    /// <summary>Contact PeerId (Identity &amp; Friends). Takes precedence over UserId when set.</summary>
    public string? PeerId { get; set; }
}
