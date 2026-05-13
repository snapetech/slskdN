// <copyright file="SongIdController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.SongID.API;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using slskd.Authentication;
using slskd.Core.Features;
using slskd.Core.Security;

[ApiController]
[Route("api/v{version:apiVersion}/songid")]
[ApiVersion("0")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class SongIdController : ControllerBase
{
    private readonly ISongIdService _songIdService;
    private readonly IFeatureGate? _featureGate;

    public SongIdController(ISongIdService songIdService, IFeatureGate? featureGate = null)
    {
        _songIdService = songIdService;
        _featureGate = featureGate;
    }

    [HttpPost("runs")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.AdministratorOnly)]
    public async Task<IActionResult> CreateRun([FromBody, Required] SongIdRunRequest request, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        if (request == null)
        {
            return BadRequest("SongID source is required.");
        }

        request.Source = request.Source?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            return BadRequest("SongID source is required.");
        }

        try
        {
            var run = await _songIdService.QueueAnalyzeAsync(request.Source, cancellationToken).ConfigureAwait(false);
            return Accepted(run);
        }
        catch (ArgumentException)
        {
            return BadRequest("SongID request is invalid.");
        }
        catch (InvalidOperationException)
        {
            return BadRequest("SongID analysis could not be queued.");
        }
    }

    [HttpGet("runs")]
    public IActionResult ListRuns([FromQuery] int limit = 10)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        limit = limit <= 0 ? 10 : limit;
        return Ok(_songIdService.List(limit));
    }

    [HttpGet("runs/queue")]
    [ProducesResponseType(typeof(SongIdQueueSummary), 200)]
    public IActionResult GetQueueSummary([FromQuery] int activeLimit = 25)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        activeLimit = activeLimit <= 0 ? 25 : activeLimit;
        return Ok(_songIdService.GetQueueSummary(activeLimit));
    }

    [HttpGet("runs/{id:guid}")]
    public IActionResult GetRun(Guid id)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        var run = _songIdService.Get(id);
        return run == null ? NotFound() : Ok(run);
    }

    [HttpGet("runs/{id:guid}/evidence-package")]
    [ProducesResponseType(typeof(SongIdRunEvidencePackage), 200)]
    [ProducesResponseType(404)]
    public IActionResult GetEvidencePackage(Guid id)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        var package = _songIdService.GetEvidencePackage(id);
        return package == null ? NotFound() : Ok(package);
    }

    [HttpGet("runs/{id:guid}/forensic-matrix")]
    public IActionResult GetForensicMatrix(Guid id)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var disabled = DisabledResult();
        if (disabled != null)
        {
            return disabled;
        }

        var run = _songIdService.Get(id);
        if (run == null || run.ForensicMatrix == null)
        {
            return NotFound();
        }

        return Ok(run.ForensicMatrix);
    }

    private ObjectResult? DisabledResult()
    {
        if (_featureGate?.IsEnabled(FeatureId.SongId) != false)
        {
            return null;
        }

        var gate = _featureGate.Get(FeatureId.SongId);
        return StatusCode(StatusCodes.Status404NotFound, new
        {
            feature = gate.Feature.ToString(),
            status = gate.Status.ToString(),
            error = gate.Message,
        });
    }
}

public sealed class SongIdRunRequest
{
    public string Source { get; set; } = string.Empty;
}
