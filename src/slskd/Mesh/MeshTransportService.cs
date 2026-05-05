// <copyright file="MeshTransportService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Common.Security;

namespace slskd.Mesh;

/// <summary>
/// Mesh transport service that honors user-configured transport preference and anonymity settings.
/// </summary>
public interface IMeshTransportService
{
    MeshTransportPreference Preference { get; }
    Task<MeshTransportDecision> ChooseTransportAsync(string contentId, CancellationToken ct = default);
    Task<MeshTransportDecision> ChooseTransportAsync(string peerId, string? podId, string contentId, CancellationToken ct = default);
}

public record MeshTransportDecision(MeshTransportPreference Preference, string Reason, AnonymityTransportType? AnonymityTransport = null);

public class MeshTransportService : IMeshTransportService
{
    private readonly ILogger<MeshTransportService> logger;
    private readonly IOptions<MeshOptions> options;
    private readonly IAnonymityTransportSelector? anonymitySelector;
    private readonly IOptions<AdversarialOptions>? adversarialOptions;

    public MeshTransportService(
        ILogger<MeshTransportService> logger,
        IOptions<MeshOptions> options,
        IAnonymityTransportSelector? anonymitySelector = null,
        IOptions<AdversarialOptions>? adversarialOptions = null)
    {
        this.logger = logger;
        this.options = options;
        this.anonymitySelector = anonymitySelector;
        this.adversarialOptions = adversarialOptions;
    }

    public MeshTransportPreference Preference => options.Value.TransportPreference;

    /// <summary>
    /// Legacy method for backward compatibility - chooses transport without peer/pod context.
    /// </summary>
    public Task<MeshTransportDecision> ChooseTransportAsync(string contentId, CancellationToken ct = default)
    {
        return ChooseTransportAsync(peerId: null, podId: null, contentId, ct);
    }

    /// <summary>
    /// Chooses the appropriate transport considering anonymity settings and per-peer policies.
    /// </summary>
    /// <param name="peerId">The target peer ID (optional, for policy-aware selection).</param>
    /// <param name="podId">The pod ID (optional, for policy-aware selection).</param>
    /// <param name="contentId">The content ID being transported.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The transport decision.</returns>
    public async Task<MeshTransportDecision> ChooseTransportAsync(string? peerId, string? podId, string contentId, CancellationToken ct = default)
    {
        var opt = options.Value;
        var basePreference = Preference;

        // Check if anonymity or obfuscated transport features should override transport selection.
        AnonymityTransportType? anonymityTransport = null;
        if (anonymitySelector != null && ShouldPreferPrivateOrObfuscatedTransport(adversarialOptions?.Value))
        {
            try
            {
                anonymityTransport = await anonymitySelector.SelectTransportTypeAsync(peerId ?? "unknown", podId, ct).ConfigureAwait(false);

                if (anonymityTransport.HasValue && anonymityTransport.Value != AnonymityTransportType.Direct)
                {
                    logger.LogDebug(
                        "[MeshTransport] {ContentId}: Selected private/obfuscated transport {AnonymityTransport} for peer {PeerId}",
                        LoggingSanitizer.SanitizeHash(contentId),
                        anonymityTransport,
                        LoggingSanitizer.SanitizeExternalIdentifier(peerId ?? "unknown"));

                    // Keep actual mesh payloads on the overlay when using private or anti-DPI transports.
                    basePreference = MeshTransportPreference.OverlayFirst;
                }
                else
                {
                    anonymityTransport = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "[MeshTransport] Failed to select private/obfuscated transport for {ContentId}, falling back to standard routing",
                    LoggingSanitizer.SanitizeHash(contentId));
            }
        }

        var reason = basePreference switch
        {
            MeshTransportPreference.DhtFirst => anonymityTransport.HasValue
                ? $"DHT-first with {anonymityTransport.Value} anonymity"
                : "DHT-first for efficiency",
            MeshTransportPreference.Mirrored => anonymityTransport.HasValue
                ? $"Mirrored with {anonymityTransport.Value} anonymity"
                : "Mirrored DHT+overlay for resiliency",
            MeshTransportPreference.OverlayFirst => anonymityTransport.HasValue
                ? $"Overlay-first with {anonymityTransport.Value} anonymity"
                : "Overlay-first for private paths",
            _ => "Default"
        };

        logger.LogDebug("[MeshTransport] {ContentId}: {Preference} ({Reason})", LoggingSanitizer.SanitizeHash(contentId), basePreference, reason);
        return new MeshTransportDecision(basePreference, reason, anonymityTransport);
    }

    private static bool ShouldPreferPrivateOrObfuscatedTransport(AdversarialOptions? options)
    {
        if (options == null)
        {
            return false;
        }

        var anonymityEnabled = options.AnonymityLayer.Enabled && options.AnonymityLayer.Mode != AnonymityMode.Direct;
        var obfuscatedEnabled = options.ObfuscatedTransports.Enabled &&
            options.ObfuscatedTransports.Mode != ObfuscatedTransportMode.Direct;

        return anonymityEnabled || obfuscatedEnabled;
    }
}
