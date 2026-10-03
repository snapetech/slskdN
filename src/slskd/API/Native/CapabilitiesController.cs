// <copyright file="CapabilitiesController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.API.Native;

using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd;
using slskd.Core.Features;
using slskd.Core.Security;
using OptionsModel = slskd.Options;

/// <summary>
/// Provides slskdn-native capabilities detection API.
/// </summary>
[ApiController]
[Route("api/slskdn")]
[Route("api/v{version:apiVersion}/slskdn")]
[ApiVersion("0")]
[Produces("application/json")]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
public class CapabilitiesController : ControllerBase
{
    private readonly IOptionsMonitor<OptionsModel> optionsMonitor;
    private readonly ILogger<CapabilitiesController> logger;
    private readonly IFeatureGate featureGate;

    public CapabilitiesController(
        IOptionsMonitor<OptionsModel> optionsMonitor,
        ILogger<CapabilitiesController> logger,
        IFeatureGate featureGate)
    {
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
        this.featureGate = featureGate;
    }

    /// <summary>
    /// Get slskdn capabilities and feature flags.
    /// </summary>
    [HttpGet("capabilities")]
    [Authorize(Policy = AuthPolicy.Any)]
    public IActionResult GetCapabilities()
    {
        logger.LogDebug("Capabilities endpoint called");

        var features = new List<string>
        {
            "mbid_jobs",
            "discography_jobs",
            "label_crate_jobs",
            "canonical_scoring",
            "rescue_mode",
            "library_health",
            "warm_cache",
            "job_manifests",
            "session_traces",
            "playback_aware",
            "soulseek_type1_obfuscation_options",
            "soulseek_type1_obfuscated_distributed_messages",
            "soulseek_type1_obfuscated_file_transfers"
        };

        if (optionsMonitor.CurrentValue.Feature.ScenePodBridge)
        {
            features.Add("scene_pod_bridge");
        }

        var version = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        return Ok(new
        {
            impl = "slskdn",
            compat = "slskd",
            version,
            features,
            obfuscation = SoulseekObfuscationSupport.BuildPlan(optionsMonitor.CurrentValue.Soulseek),
            featureGates = new
            {
                songId = GetFeatureGateStatus(FeatureId.SongId),
                mesh = GetFeatureGateStatus(FeatureId.Mesh),
                dht = GetFeatureGateStatus(FeatureId.Dht),
                pods = GetFeatureGateStatus(FeatureId.Pods),
                socialFederation = GetFeatureGateStatus(FeatureId.SocialFederation),
                virtualSoulfind = GetFeatureGateStatus(FeatureId.VirtualSoulfind),
                multiSourceDownloads = GetFeatureGateStatus(FeatureId.MultiSourceDownloads),
            },
            feature = new
            {
                scenePodBridge = optionsMonitor.CurrentValue.Feature.ScenePodBridge
            }
        });
    }

    private object GetFeatureGateStatus(FeatureId feature)
    {
        var result = featureGate.Get(feature);
        return new
        {
            status = result.Status.ToString(),
            enabled = result.IsEnabled,
            message = result.Message,
        };
    }
}
