// <copyright file="MeshStreamsController.cs" company="slskdN Team">
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

/// <summary>Manual mesh-to-browser audio preview streams. Does not persist files or use multisource fanout.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/mesh-streams")]
[ApiVersion("0")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class MeshStreamsController : ControllerBase
{
    private readonly IMeshStreamTicketService _tickets;
    private readonly IMeshStreamService _streams;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public MeshStreamsController(
        IMeshStreamTicketService tickets,
        IMeshStreamService streams,
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
    [ProducesResponseType(429)]
    public IActionResult CreateTicket([FromBody] MeshStreamTicketRequest request)
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
                streamUrl = $"/api/v0/mesh-streams/{Uri.EscapeDataString(ticket.Ticket)}",
                expiresInSeconds = 120,
                contentType = ticket.ContentType,
                source = "mesh",
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ToTicketValidationMessage(ex));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(429, "Mesh stream limit reached.");
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
            var claims = _tickets.Validate(ticket);
            if (claims?.Radio != null && !_options.CurrentValue.Feature.Mesh)
            {
                return NotFound();
            }

            long start = 0;
            long? endExclusive = null;
            if (claims?.Radio != null && claims.ExpectedSize is > 0)
            {
                var size = claims.ExpectedSize.Value;
                endExclusive = size;
                var header = Request.Headers.Range.ToString();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    if (!Microsoft.Net.Http.Headers.RangeHeaderValue.TryParse(header, out var range) ||
                        !string.Equals(range.Unit.ToString(), "bytes", StringComparison.OrdinalIgnoreCase) ||
                        range.Ranges.Count != 1)
                    {
                        return BadRequest("A single byte range is required.");
                    }

                    var requested = range.Ranges.Single();
                    if (requested.From.HasValue)
                    {
                        start = requested.From.Value;
                        endExclusive = requested.To.HasValue ? Math.Min(size, requested.To.Value == long.MaxValue ? size : requested.To.Value + 1) : size;
                    }
                    else if (requested.To is > 0)
                    {
                        start = Math.Max(0, size - requested.To.Value);
                    }
                    else
                    {
                        Response.Headers.ContentRange = $"bytes */{size}";
                        return StatusCode(416);
                    }

                    if (start >= size || endExclusive <= start)
                    {
                        Response.Headers.ContentRange = $"bytes */{size}";
                        return StatusCode(416);
                    }

                    Response.StatusCode = 206;
                    Response.Headers.ContentRange = $"bytes {start}-{endExclusive - 1}/{size}";
                }
            }

            var lease = endExclusive.HasValue
                ? await _streams.OpenRangeAsync(ticket, start, endExclusive.Value, cancellationToken).ConfigureAwait(false)
                : await _streams.OpenAsync(ticket, cancellationToken).ConfigureAwait(false);
            if (lease == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";
            Response.Headers.AcceptRanges = endExclusive.HasValue ? "bytes" : "none";
            if (endExclusive.HasValue)
            {
                Response.ContentLength = endExclusive.Value - start;
            }

            return File(lease.Stream, lease.ContentType, enableRangeProcessing: false);
        }
        catch (MeshStreamLimitException)
        {
            return StatusCode(429, "Mesh stream limit reached.");
        }
    }

    private string GetAuthenticatedOwnerKey()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? CurrentUserId;
    }

    private static string ToTicketValidationMessage(ArgumentException exception)
        => exception.Message switch
        {
            "Expected size must be greater than or equal to zero." => "Expected size must be greater than or equal to zero.",
            "ContentId is required." => "ContentId is required.",
            "PeerId is required." => "PeerId is required.",
            "Filename is required." => "Filename is required.",
            "Expected hash must be a SHA-256 hex digest." => "Expected hash must be a SHA-256 hex digest.",
            "Only audio files can be preview streamed from mesh peers." => "Only audio files can be preview streamed from mesh peers.",
            _ => "Invalid mesh stream ticket request.",
        };
}
