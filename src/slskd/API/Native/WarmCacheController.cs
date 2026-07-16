// <copyright file="WarmCacheController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.API.Native;

using slskd.Core.Security;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using slskd;
using slskd.Transfers.MultiSource.Caching;
using OptionsModel = slskd.Options;

/// <summary>
/// Provides slskdn-native warm cache hints API.
/// </summary>
[ApiController]
[Route("api/slskdn/warm-cache")]
[Route("api/v{version:apiVersion}/slskdn/warm-cache")]
[ApiVersion("0")]
[Produces("application/json")]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
public class WarmCacheController : ControllerBase
{
    private const int MaxHintsPerRequest = 100;
    private const int MaxIdentifierLength = 128;

    private readonly IWarmCachePopularityService popularityService;
    private readonly IOptionsMonitor<OptionsModel> optionsMonitor;
    private readonly ILogger<WarmCacheController> logger;

    public WarmCacheController(
        IWarmCachePopularityService popularityService,
        IOptionsMonitor<OptionsModel> optionsMonitor,
        ILogger<WarmCacheController> logger)
    {
        this.popularityService = popularityService;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
    }

    /// <summary>
    /// Submit popularity hints for warm cache prefetching.
    /// </summary>
    [HttpPost("hints")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [Authorize(Policy = AuthPolicy.Any)]
    public async Task<IActionResult> SubmitHints(
        [FromBody] WarmCacheHintsRequest request,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (options.WarmCache?.Enabled != true)
        {
            return BadRequest(new { error = "Warm cache not enabled" });
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request is required" });
        }

        var rawHintCount = (request.MbReleaseIds?.Count ?? 0)
            + (request.MbArtistIds?.Count ?? 0)
            + (request.MbLabelIds?.Count ?? 0);
        if (rawHintCount > MaxHintsPerRequest)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = $"At most {MaxHintsPerRequest} hints are accepted per request" });
        }

        var releaseIds = (request.MbReleaseIds ?? new List<string>())
            .Select(id => id?.Trim() ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var artistIds = (request.MbArtistIds ?? new List<string>())
            .Select(id => id?.Trim() ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var labelIds = (request.MbLabelIds ?? new List<string>())
            .Select(id => id?.Trim() ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (releaseIds.Count == 0 && artistIds.Count == 0 && labelIds.Count == 0)
        {
            return BadRequest(new { error = "At least one MusicBrainz identifier is required" });
        }

        if (releaseIds.Concat(artistIds).Concat(labelIds).Any(id => id.Length > MaxIdentifierLength))
        {
            return BadRequest(new { error = $"MusicBrainz identifiers cannot exceed {MaxIdentifierLength} characters" });
        }

        logger.LogInformation("Received warm cache hints: {ReleaseCount} releases, {ArtistCount} artists, {LabelCount} labels",
            releaseIds.Count,
            artistIds.Count,
            labelIds.Count);

        var hints = releaseIds.Select(id => $"mb:release:{id}")
            .Concat(artistIds.Select(id => $"mb:artist:{id}"))
            .Concat(labelIds.Select(id => $"mb:label:{id}"))
            .ToArray();
        await popularityService.RecordAccessesAsync(hints, cancellationToken);

        return Ok(new { accepted = true });
    }
}

public record WarmCacheHintsRequest(
    [property: JsonPropertyName("mb_release_ids")] List<string>? MbReleaseIds = null,
    [property: JsonPropertyName("mb_artist_ids")] List<string>? MbArtistIds = null,
    [property: JsonPropertyName("mb_label_ids")] List<string>? MbLabelIds = null);
