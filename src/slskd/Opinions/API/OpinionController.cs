// <copyright file="OpinionController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Opinions.API;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using slskd.Core.Security;

[ApiController]
[Route("api/v{version:apiVersion}/opinions")]
[ApiVersion("0")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class OpinionController : ControllerBase
{
    private readonly IOpinionService opinionService;

    public OpinionController(IOpinionService opinionService)
    {
        this.opinionService = opinionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OpinionRecord>), 200)]
    public async Task<IActionResult> List(
        [FromQuery] OpinionSubjectType? subjectType,
        [FromQuery] string? subjectId,
        [FromQuery] OpinionKind? kind,
        [FromQuery] string? issuer,
        [FromQuery] string? scope,
        [FromQuery] string? source,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var records = await opinionService.ListAsync(new OpinionQuery
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            Kind = kind,
            Issuer = issuer,
            Scope = scope,
            Source = source,
            IncludeExpired = includeExpired,
            Limit = limit,
        }, cancellationToken).ConfigureAwait(false);

        return Ok(records);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(OpinionSummary), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Summary(
        [FromQuery] OpinionSubjectType subjectType,
        [FromQuery] string subjectId,
        [FromQuery] string scope = "global",
        CancellationToken cancellationToken = default)
    {
        if (subjectType == OpinionSubjectType.Unknown || string.IsNullOrWhiteSpace(subjectId))
        {
            return BadRequest("subjectType and subjectId are required");
        }

        return Ok(await opinionService.SummarizeAsync(subjectType, subjectId, scope, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(typeof(OpinionRecord), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Submit([FromBody] OpinionRecord? opinion, CancellationToken cancellationToken)
    {
        if (opinion == null)
        {
            return BadRequest("opinion is required");
        }

        var validation = opinionService.Validate(opinion);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);
        }

        return Ok(await opinionService.SubmitAsync(opinion, cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken)
    {
        return await opinionService.RemoveAsync(id, cancellationToken).ConfigureAwait(false)
            ? NoContent()
            : NotFound();
    }
}
