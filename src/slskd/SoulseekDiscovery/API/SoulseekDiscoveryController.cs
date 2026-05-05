// <copyright file="SoulseekDiscoveryController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.SoulseekDiscovery.API;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Common.Security;
using slskd.Core.Security;
using slskd.Mesh;
using Soulseek;

[ApiController]
[Route("api/v{version:apiVersion}/soulseek")]
[ApiVersion("0")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class SoulseekDiscoveryController : ControllerBase
{
    public SoulseekDiscoveryController(
        ISoulseekDiscoveryService discoveryService,
        ISoulseekSafetyLimiter safetyLimiter,
        ILogger<SoulseekDiscoveryController> logger,
        IOptions<MeshOptions>? meshOptions = null)
    {
        DiscoveryService = discoveryService;
        MeshOptions = meshOptions?.Value ?? new MeshOptions();
        SafetyLimiter = safetyLimiter;
        Logger = logger;
    }

    private ISoulseekDiscoveryService DiscoveryService { get; }
    private ILogger<SoulseekDiscoveryController> Logger { get; }
    private MeshOptions MeshOptions { get; }
    private ISoulseekSafetyLimiter SafetyLimiter { get; }

    [HttpPost("interests")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> AddInterest([FromBody] SoulseekInterestRequest? request, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var item = NormalizeItem(request?.Item);
        if (item == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-interest"))
        {
            return StatusCode(429, "Soulseek interest operation rate limit exceeded.");
        }

        await DiscoveryService.AddInterestAsync(item, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("interests/{item}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> RemoveInterest([FromRoute] string item, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var normalizedItem = NormalizeItem(item);
        if (normalizedItem == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-interest"))
        {
            return StatusCode(429, "Soulseek interest operation rate limit exceeded.");
        }

        await DiscoveryService.RemoveInterestAsync(normalizedItem, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("hated-interests")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> AddHatedInterest([FromBody] SoulseekInterestRequest? request, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var item = NormalizeItem(request?.Item);
        if (item == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-interest"))
        {
            return StatusCode(429, "Soulseek interest operation rate limit exceeded.");
        }

        await DiscoveryService.AddHatedInterestAsync(item, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("hated-interests/{item}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> RemoveHatedInterest([FromRoute] string item, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var normalizedItem = NormalizeItem(item);
        if (normalizedItem == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-interest"))
        {
            return StatusCode(429, "Soulseek interest operation rate limit exceeded.");
        }

        await DiscoveryService.RemoveHatedInterestAsync(normalizedItem, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(RecommendationList), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-recommendations"))
        {
            return StatusCode(429, "Soulseek recommendation rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetRecommendationsAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("recommendations/global")]
    [ProducesResponseType(typeof(RecommendationList), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetGlobalRecommendations(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-recommendations"))
        {
            return StatusCode(429, "Soulseek recommendation rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetGlobalRecommendationsAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("users/{username}/interests")]
    [ProducesResponseType(typeof(UserInterests), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetUserInterests([FromRoute] string username, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var normalizedUsername = NormalizeUsername(username);
        if (normalizedUsername == null)
        {
            return BadRequest("username is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-user-interests"))
        {
            return StatusCode(429, "Soulseek user-interest rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetUserInterestsAsync(normalizedUsername, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("users/similar")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SimilarUser>), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetSimilarUsers(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-similar-users"))
        {
            return StatusCode(429, "Soulseek similar-user rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetSimilarUsersAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("mesh-rendezvous/interest")]
    [ProducesResponseType(204)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> AddMeshRendezvousInterest(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!MeshOptions.EnableSoulseekRendezvous)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-mesh-rendezvous"))
        {
            return StatusCode(429, "Soulseek mesh rendezvous operation rate limit exceeded.");
        }

        await DiscoveryService.AddMeshRendezvousInterestAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("mesh-rendezvous/interest")]
    [ProducesResponseType(204)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> RemoveMeshRendezvousInterest(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!MeshOptions.EnableSoulseekRendezvous)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-mesh-rendezvous"))
        {
            return StatusCode(429, "Soulseek mesh rendezvous operation rate limit exceeded.");
        }

        await DiscoveryService.RemoveMeshRendezvousInterestAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("mesh-rendezvous/users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SimilarUser>), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetMeshRendezvousUsers(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!MeshOptions.EnableSoulseekRendezvous)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-mesh-rendezvous"))
        {
            return StatusCode(429, "Soulseek mesh rendezvous operation rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetMeshRendezvousUsersAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("mesh-rendezvous/discover")]
    [ProducesResponseType(typeof(SoulseekMeshRendezvousResponse), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> DiscoverMeshRendezvous(CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        if (!MeshOptions.EnableSoulseekRendezvous)
        {
            return Forbid();
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-mesh-rendezvous"))
        {
            return StatusCode(429, "Soulseek mesh rendezvous operation rate limit exceeded.");
        }

        var result = await DiscoveryService.DiscoverMeshRendezvousAsync(cancellationToken).ConfigureAwait(false);
        return Ok(SoulseekMeshRendezvousResponse.FromResult(result));
    }

    [HttpGet("peer-capabilities")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SoulseekPeerCapabilityResponse>), 200)]
    public IActionResult GetPeerCapabilities()
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        return Ok(DiscoveryService.GetPeerCapabilityRecords().Select(SoulseekPeerCapabilityResponse.FromRecord).ToArray());
    }

    [HttpGet("mesh-rendezvous/status")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetMeshRendezvousStatus()
    {
        return Ok(new
        {
            enabled = MeshOptions.EnableSoulseekRendezvous,
            interestTag = SoulseekClient.MeshRendezvousInterestTag,
            privacy = "When enabled, adding the rendezvous interest publishes a recognizable slskdN mesh tag on this Soulseek account.",
        });
    }

    [HttpGet("items/{item}/recommendations")]
    [ProducesResponseType(typeof(ItemRecommendations), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetItemRecommendations([FromRoute] string item, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var normalizedItem = NormalizeItem(item);
        if (normalizedItem == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-item-recommendations"))
        {
            return StatusCode(429, "Soulseek item-recommendation rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetItemRecommendationsAsync(normalizedItem, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("items/{item}/similar-users")]
    [ProducesResponseType(typeof(ItemSimilarUsers), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> GetItemSimilarUsers([FromRoute] string item, CancellationToken cancellationToken)
    {
        if (Program.IsRelayAgent)
        {
            return Forbid();
        }

        var normalizedItem = NormalizeItem(item);
        if (normalizedItem == null)
        {
            return BadRequest("item is required");
        }

        if (!SafetyLimiter.TryConsumeSearch("soulseek-item-similar-users"))
        {
            return StatusCode(429, "Soulseek item-similar-user rate limit exceeded.");
        }

        return Ok(await DiscoveryService.GetItemSimilarUsersAsync(normalizedItem, cancellationToken).ConfigureAwait(false));
    }

    private static string? NormalizeItem(string? item)
    {
        item = item?.Trim();
        return string.IsNullOrWhiteSpace(item) ? null : item;
    }

    private static string? NormalizeUsername(string? username)
    {
        username = username?.Trim();
        return string.IsNullOrWhiteSpace(username) ? null : username;
    }
}

public sealed class SoulseekInterestRequest
{
    public string? Item { get; set; }
}

public sealed class SoulseekMeshRendezvousResponse
{
    public string InterestTag { get; init; } = string.Empty;
    public IReadOnlyCollection<SimilarUser> SimilarUsers { get; init; } = Array.Empty<SimilarUser>();
    public IReadOnlyCollection<SoulseekPeerCapabilityResponse> CapabilityRecords { get; init; } = Array.Empty<SoulseekPeerCapabilityResponse>();

    public static SoulseekMeshRendezvousResponse FromResult(MeshRendezvousResult result)
        => new()
        {
            InterestTag = result.InterestTag,
            SimilarUsers = result.SimilarUsers,
            CapabilityRecords = result.CapabilityRecords.Select(SoulseekPeerCapabilityResponse.FromRecord).ToArray(),
        };
}

public sealed class SoulseekPeerCapabilityResponse
{
    public string Username { get; init; } = string.Empty;
    public string? PeerId { get; init; }
    public IReadOnlyCollection<string> Features { get; init; } = Array.Empty<string>();
    public int? OverlayPort { get; init; }
    public int MaxPayloadLength { get; init; }
    public string MessageType { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public DateTimeOffset ObservedAt { get; init; }
    public bool Signed { get; init; }

    public static SoulseekPeerCapabilityResponse FromRecord(PeerCapabilityRecord record)
        => new()
        {
            Username = record.Username,
            PeerId = record.Descriptor.PeerId,
            Features = record.Descriptor.Features.ToArray(),
            OverlayPort = record.Descriptor.OverlayPort,
            MaxPayloadLength = record.Descriptor.MaxPayloadLength,
            MessageType = record.MessageType.ToString(),
            Nonce = record.Nonce,
            ObservedAt = record.ObservedAt,
            Signed = record.Descriptor.Signature is not null,
        };
}
