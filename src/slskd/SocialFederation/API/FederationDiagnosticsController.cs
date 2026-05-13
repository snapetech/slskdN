// <copyright file="FederationDiagnosticsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.SocialFederation.API;

using Asp.Versioning;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Common;
using slskd.Core.Security;
using slskd.Mesh;
using slskd.PodCore;
using slskd.SocialFederation;

/// <summary>
///     Read-only federation and pod security diagnostics for operators.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/federation/diagnostics")]
[ApiVersion("0")]
[Produces("application/json")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
[FeatureGate(FeatureId.SocialFederation)]
public sealed class FederationDiagnosticsController : ControllerBase
{
    private readonly IOptionsMonitor<SocialFederationOptions> federationOptions;
    private readonly IOptionsMonitor<FederationPublishingOptions> publishingOptions;
    private readonly IOptionsMonitor<PodJoinOptions> podJoinOptions;
    private readonly IOptionsMonitor<PodMessageSignerOptions> podMessageSignerOptions;
    private readonly IOptions<MeshOptions>? meshOptions;

    public FederationDiagnosticsController(
        IOptionsMonitor<SocialFederationOptions> federationOptions,
        IOptionsMonitor<FederationPublishingOptions> publishingOptions,
        IOptionsMonitor<PodJoinOptions> podJoinOptions,
        IOptionsMonitor<PodMessageSignerOptions> podMessageSignerOptions,
        IOptions<MeshOptions>? meshOptions = null)
    {
        this.federationOptions = federationOptions;
        this.publishingOptions = publishingOptions;
        this.podJoinOptions = podJoinOptions;
        this.podMessageSignerOptions = podMessageSignerOptions;
        this.meshOptions = meshOptions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetDiagnostics()
    {
        var federation = federationOptions.CurrentValue;
        var publishing = publishingOptions.CurrentValue;
        var join = podJoinOptions.CurrentValue;
        var signer = podMessageSignerOptions.CurrentValue;
        var mesh = meshOptions?.Value ?? new MeshOptions();
        var warnings = BuildWarnings(federation, publishing, join, signer);

        return Ok(new
        {
            federation = new
            {
                federation.Enabled,
                federation.Mode,
                domainConfigured = !string.IsNullOrWhiteSpace(federation.Domain),
                baseUrlConfigured = !string.IsNullOrWhiteSpace(federation.BaseUrl),
                approvedPeerCount = federation.ApprovedPeers?.Length ?? 0,
                federation.VerifySignatures,
                federation.HttpTimeoutSeconds,
                federation.PageSize,
                exposure = GetExposure(federation),
            },
            publishing = new
            {
                publishing.Enabled,
                publishing.PublishableDomains,
                publishing.DefaultVisibility,
                approvedCircleCount = publishing.ApprovedCircles?.Length ?? 0,
                publishing.RequireModerationApproval,
                publishing.IncludeExternalLinks,
                publishing.MaxMetadataSizeKb,
            },
            pods = new
            {
                joinSignatureMode = join.SignatureMode.ToString(),
                messageSignatureMode = signer.SignatureMode.ToString(),
            },
            mesh = new
            {
                selfPeerIdConfigured = !string.IsNullOrWhiteSpace(mesh.SelfPeerId),
                soulseekRendezvousEnabled = mesh.EnableSoulseekRendezvous,
            },
            warnings,
        });
    }

    private static IReadOnlyList<string> BuildWarnings(
        SocialFederationOptions federation,
        FederationPublishingOptions publishing,
        PodJoinOptions join,
        PodMessageSignerOptions signer)
    {
        var warnings = new List<string>();

        if (federation.Enabled && string.IsNullOrWhiteSpace(federation.BaseUrl))
        {
            warnings.Add("Federation is enabled but federation.baseUrl is not configured.");
        }

        if (federation.Enabled && federation.IsPublic && !federation.VerifySignatures)
        {
            warnings.Add("Public federation is enabled while HTTP signature verification is disabled.");
        }

        if (publishing.Enabled && !federation.Enabled)
        {
            warnings.Add("Federation publishing is enabled while social federation is disabled.");
        }

        if (publishing.Enabled && !publishing.RequireModerationApproval && string.Equals(publishing.DefaultVisibility, "public", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Federation publishing defaults to public without moderation approval.");
        }

        if (join.SignatureMode == SignatureMode.Off)
        {
            warnings.Add("Pod join signatures are not enforced.");
        }

        if (signer.SignatureMode == SignatureMode.Off)
        {
            warnings.Add("Pod message signatures are not enforced.");
        }

        return warnings;
    }

    private static string GetExposure(SocialFederationOptions federation)
    {
        if (!federation.Enabled || federation.IsHermit)
        {
            return "Hermit";
        }

        if (federation.IsFriendsOnly)
        {
            return "FriendsOnly";
        }

        return federation.IsPublic ? "Public" : "Custom";
    }
}
