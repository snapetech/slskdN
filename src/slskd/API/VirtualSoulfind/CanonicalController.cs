// <copyright file="CanonicalController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.API.VirtualSoulfind;

using slskd.Core.Security;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides canonical variant selection API.
/// </summary>
[ApiController]
[Route("api/virtualsoulfind/canonical")]
[Route("api/v{version:apiVersion}/virtualsoulfind/canonical")]
[ApiVersion("0")]
[Produces("application/json")]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
[FeatureGate(FeatureId.VirtualSoulfind)]
public class CanonicalController : ControllerBase
{
    private readonly ILogger<CanonicalController> logger;
    private readonly slskd.VirtualSoulfind.ShadowIndex.IShadowIndexQuery shadowIndexQuery;

    public CanonicalController(
        ILogger<CanonicalController> logger,
        slskd.VirtualSoulfind.ShadowIndex.IShadowIndexQuery shadowIndexQuery)
    {
        this.logger = logger;
        this.shadowIndexQuery = shadowIndexQuery;
    }

    /// <summary>
    /// Get canonical variant for a MusicBrainz recording ID.
    /// </summary>
    [HttpGet("{mbid}")]
    [Authorize]
    public async Task<IActionResult> GetCanonical(string mbid, CancellationToken ct)
    {
        mbid = mbid?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(mbid))
        {
            return BadRequest(new { error = "MBID is required" });
        }

        logger.LogDebug("Canonical variant requested for MBID: {Mbid}", mbid);

        try
        {
            var result = await shadowIndexQuery.QueryAsync(mbid, ct);

            if (result == null || !result.CanonicalVariants.Any())
            {
                return Ok(new
                {
                    canonical_variant = (object?)null,
                    available_variants = 0,
                    selection_reason = "No variants found in shadow index"
                });
            }

            // Select canonical variant based on quality criteria
            var canonicalVariant = SelectCanonicalVariant(result.CanonicalVariants);

            return Ok(new
            {
                canonical_variant = canonicalVariant is null ? null : new
                {
                    codec = canonicalVariant.Codec,
                    bitrate = canonicalVariant.BitrateKbps,
                    fileSize = canonicalVariant.SizeBytes,
                    qualityScore = canonicalVariant.QualityScore
                },
                available_variants = result.CanonicalVariants.Count,
                selection_reason = canonicalVariant != null ? "Selected highest quality variant from shadow index" : "No suitable variant found"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to select canonical variant for MBID: {Mbid}", mbid);
            return StatusCode(500, new { error = "Failed to select canonical variant" });
        }
    }

    internal static slskd.VirtualSoulfind.ShadowIndex.VariantHint? SelectCanonicalVariant(
        IReadOnlyList<slskd.VirtualSoulfind.ShadowIndex.VariantHint> variants)
    {
        slskd.VirtualSoulfind.ShadowIndex.VariantHint? canonicalVariant = null;
        var canonicalCodecPriority = -1;

        foreach (var variant in variants)
        {
            var codecPriority = GetCodecPriority(variant.Codec);
            if (canonicalVariant == null ||
                codecPriority > canonicalCodecPriority ||
                (codecPriority == canonicalCodecPriority && variant.QualityScore > canonicalVariant.QualityScore))
            {
                canonicalVariant = variant;
                canonicalCodecPriority = codecPriority;
            }
        }

        return canonicalVariant;
    }

    private static int GetCodecPriority(string codec)
    {
        if (string.Equals(codec, "FLAC", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(codec, "ALAC", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(codec, "AAC", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(codec, "MP3", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
