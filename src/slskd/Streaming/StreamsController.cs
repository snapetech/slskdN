// <copyright file="StreamsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Authentication;
using slskd.Core.Security;
using slskd.Sharing;

/// <summary>GET /api/v0/streams/{contentId} — range, token or normal auth, single-range only. Requires Feature.Streaming.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/streams")]
[ApiVersion("0")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public class StreamsController : ControllerBase
{
    /// <summary>Max concurrent streams per normal (non-token) user when using normal auth.</summary>
    private const int NormalUserMaxConcurrentStreams = 5;

    private readonly IContentLocator _locator;
    private readonly IShareTokenService _tokens;
    private readonly ISharingService _sharing;
    private readonly IStreamSessionLimiter _limiter;
    private readonly IStreamTicketService _tickets;
    private readonly IOptionsMonitor<slskd.Options> _options;

    public StreamsController(
        IContentLocator locator,
        IShareTokenService tokens,
        ISharingService sharing,
        IStreamSessionLimiter limiter,
        IStreamTicketService tickets,
        IOptionsMonitor<slskd.Options> options)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _sharing = sharing ?? throw new ArgumentNullException(nameof(sharing));
        _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private bool StreamingEnabled => _options.CurrentValue.Feature.Streaming;
    private string CurrentUserId => _options.CurrentValue.Soulseek.Username ?? string.Empty;

    /// <summary>Creates a short-lived stream ticket for browser media element playback.</summary>
    [HttpPost("{contentId}/ticket")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public IActionResult CreateTicket([FromRoute] string contentId)
    {
        if (!StreamingEnabled) return NotFound();

        contentId = contentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return BadRequest("ContentId is required.");
        }

        var resolved = _locator.Resolve(contentId, HttpContext.RequestAborted);
        if (resolved == null) return NotFound();

        var ownerKey = "user:" + GetAuthenticatedOwnerKey();
        var ticket = _tickets.Create(contentId, ownerKey, TimeSpan.FromMinutes(2));
        return Ok(new { ticket, expiresInSeconds = 120 });
    }

    /// <summary>
    ///     Exchanges a share token (passed via <c>Authorization: Bearer share:&lt;token&gt;</c>) for a
    ///     short-lived, content-bound stream ticket. This lets a share recipient stream with a
    ///     <c>?ticket=</c> URL instead of putting the long-lived share token in the URL, so the token
    ///     never lands in browser history, reverse-proxy access logs, or our own request logs.
    /// </summary>
    [HttpPost("{contentId}/share-ticket")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateShareTicket([FromRoute] string contentId, CancellationToken ct)
    {
        if (!StreamingEnabled) return NotFound();

        contentId = contentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return BadRequest("ContentId is required.");
        }

        var shareToken = TryGetShareToken();
        if (string.IsNullOrEmpty(shareToken)) return Unauthorized();

        // Same access checks as the streaming GET share-token path: the token must be valid, permit
        // streaming, and grant access to a collection that actually contains this content id.
        var claims = await _tokens.ValidateAsync(shareToken, ct);
        if (claims == null || !claims.AllowStream) return Unauthorized();
        if (!Guid.TryParse(claims.CollectionId, out var collectionId)) return NotFound();

        if (!await _sharing.CollectionContainsContentAsync(collectionId, contentId, ct)) return NotFound();

        if (_locator.Resolve(contentId, ct) == null) return NotFound();

        var ticket = _tickets.Create(contentId, "share:" + claims.ShareId, TimeSpan.FromMinutes(2));
        return Ok(new { ticket, expiresInSeconds = 120 });
    }

    /// <summary>
    ///     Extracts a share token from the request headers without exposing it in the URL. Prefers the
    ///     dedicated <c>X-Share-Token</c> header (used by the web UI, whose axios client overwrites
    ///     <c>Authorization</c> with the session JWT), and falls back to
    ///     <c>Authorization: Bearer share:&lt;token&gt;</c> for non-UI clients. The <c>share:</c> prefix
    ///     disambiguates a share token from a JWT so a JWT is never mistaken for one.
    /// </summary>
    private string? TryGetShareToken()
    {
        var headerToken = Request.Headers["X-Share-Token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken.Trim();
        }

        var auth = Request.Headers.Authorization.FirstOrDefault();
        if (auth == null || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = auth.Substring("Bearer ".Length).Trim();
        if (!value.StartsWith("share:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = value.Substring("share:".Length).Trim();
        return string.IsNullOrEmpty(token) ? null : token;
    }

    /// <summary>Stream content by ID. Auth: ?ticket=, ?token=, Authorization: Bearer (share:token), or normal [Authorize]. Single byte-range only; multi-range returns 400.</summary>
    [HttpGet("{contentId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Get([FromRoute] string contentId, [FromQuery] string? token, [FromQuery] string? ticket, CancellationToken ct)
    {
        if (!StreamingEnabled) return NotFound();

        contentId = contentId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return BadRequest("ContentId is required.");
        }

        // Reject multi-range before any File/range handling
        var rangeHeader = Request.Headers.Range.FirstOrDefault();
        if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.IndexOf(',') >= 0)
            return BadRequest("Multiple byte ranges are not supported.");

        ShareTokenClaims? claims = null;
        StreamTicketClaims? ticketClaims = null;
        var tokenRaw = token?.Trim();
        var authenticatedNormally = User?.Identity?.IsAuthenticated == true;
        var ticketRaw = ticket?.Trim();
        if (!string.IsNullOrEmpty(ticketRaw))
        {
            ticketClaims = _tickets.Validate(ticketRaw, contentId);
            if (ticketClaims == null) return Unauthorized();
        }
        else if (string.IsNullOrEmpty(tokenRaw) && !authenticatedNormally)
        {
            var auth = Request.Headers.Authorization.FirstOrDefault();
            if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                tokenRaw = auth.Substring("Bearer ".Length).Trim();
        }

        if (ticketClaims != null)
        {
            // Opaque stream tickets are already bound to this content id.
        }
        else if (!string.IsNullOrEmpty(tokenRaw))
        {
            var toValidate = tokenRaw.StartsWith("share:", StringComparison.OrdinalIgnoreCase)
                ? tokenRaw.Substring("share:".Length)
                : tokenRaw;
            claims = await _tokens.ValidateAsync(toValidate, ct);
            if (claims == null) return Unauthorized();
            if (!claims.AllowStream) return Unauthorized();
            if (!Guid.TryParse(claims.CollectionId, out var collectionId)) return NotFound();
            if (!await _sharing.CollectionContainsContentAsync(collectionId, contentId, ct)) return NotFound();
        }
        else
        {
            if (!authenticatedNormally) return Unauthorized();
            if (User?.IsInRole(Role.ReadWrite.ToString()) != true &&
                User?.IsInRole(Role.Administrator.ToString()) != true)
            {
                return Forbid();
            }
        }

        var resolved = _locator.Resolve(contentId, ct);
        if (resolved == null) return NotFound();

        string limiterKey;
        int maxConcurrent;
        if (ticketClaims != null)
        {
            limiterKey = ticketClaims.OwnerKey;
            maxConcurrent = NormalUserMaxConcurrentStreams;
        }
        else if (claims != null)
        {
            limiterKey = claims.ShareId;
            maxConcurrent = claims.MaxConcurrentStreams <= 0 ? 1 : claims.MaxConcurrentStreams;
        }
        else
        {
            limiterKey = "user:" + GetAuthenticatedOwnerKey();
            maxConcurrent = NormalUserMaxConcurrentStreams;
        }

        if (!_limiter.TryAcquire(limiterKey, maxConcurrent))
            return StatusCode(429, "Too many concurrent streams.");

        Stream? stream = null;
        var limiterAcquired = true;
        try
        {
#pragma warning disable CA2000 // Ownership is transferred to ReleaseOnDisposeStream/FileResult on success and disposed in finally on failure.
            stream = new FileStream(resolved.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            Stream? ownedStream = stream;
            var wrapped = new ReleaseOnDisposeStream(ownedStream, () => _limiter.Release(limiterKey));
#pragma warning restore CA2000
            ownedStream = null;
            stream = null;
            limiterAcquired = false;
            return File(wrapped, resolved.ContentType, enableRangeProcessing: true);
        }
        catch (IOException)
        {
            if (limiterAcquired)
            {
                _limiter.Release(limiterKey);
                limiterAcquired = false;
            }

            return NotFound();
        }
        catch
        {
            if (limiterAcquired)
            {
                _limiter.Release(limiterKey);
                limiterAcquired = false;
            }

            throw;
        }
        finally
        {
            stream?.Dispose();
        }
    }

    private string GetAuthenticatedOwnerKey()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? CurrentUserId;
    }
}
