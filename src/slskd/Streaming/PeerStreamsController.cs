// <copyright file="PeerStreamsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Authentication;
using slskd.Core.Security;

/// <summary>Manual peer-to-browser audio preview streams. Does not persist files or use mesh fanout.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/peer-streams")]
[ApiVersion("0")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class PeerStreamsController : ControllerBase
{
    private readonly IPeerStreamTicketService _tickets;
    private readonly IPeerStreamService _streams;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public PeerStreamsController(
        IPeerStreamTicketService tickets,
        IPeerStreamService streams,
        IOptionsMonitor<slskd.Options> options)
    {
        _tickets = tickets;
        _streams = streams;
        _options = options;
    }

    private bool StreamingEnabled => _options.CurrentValue.Feature.Streaming;
    private string CurrentUserId => _options.CurrentValue.Soulseek.Username ?? string.Empty;

    [HttpPost("tickets")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public IActionResult CreateTicket([FromBody] PeerStreamTicketRequest request)
    {
        if (!StreamingEnabled)
        {
            return NotFound();
        }

        try
        {
            var ticket = _tickets.Create(request, "user:" + GetAuthenticatedOwnerKey(), TimeSpan.FromMinutes(2));
            return Ok(new
            {
                ticket = ticket.Ticket,
                streamUrl = $"/api/v0/peer-streams/{Uri.EscapeDataString(ticket.Ticket)}",
                expiresInSeconds = 120,
                contentType = ticket.ContentType,
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(429, ex.Message);
        }
    }

    [HttpGet("{ticket}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Get([FromRoute] string ticket, CancellationToken cancellationToken)
    {
        if (!StreamingEnabled)
        {
            return NotFound();
        }

        try
        {
            var lease = await _streams.OpenAsync(ticket, cancellationToken);
            if (lease == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";
            Response.Headers.AcceptRanges = "none";
            return File(lease.Stream, lease.ContentType, enableRangeProcessing: false);
        }
        catch (PeerStreamLimitException ex)
        {
            return StatusCode(429, ex.Message);
        }
    }

    private string GetAuthenticatedOwnerKey()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? CurrentUserId;
    }
}
