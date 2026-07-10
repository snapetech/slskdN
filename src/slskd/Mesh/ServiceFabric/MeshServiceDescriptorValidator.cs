// <copyright file="MeshServiceDescriptorValidator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace slskd.Mesh.ServiceFabric;

/// <summary>
/// Validates mesh service descriptors for security and correctness.
/// </summary>
public interface IMeshServiceDescriptorValidator
{
    /// <summary>
    /// Validates a service descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor to validate.</param>
    /// <returns>A tuple indicating whether the descriptor is valid and the validation failure reason, if any.</returns>
    Task<(bool IsValid, string Reason)> ValidateAsync(MeshServiceDescriptor descriptor);

    /// <summary>
    /// Checks if a peer is banned or quarantined.
    /// </summary>
    Task<bool> IsPeerAllowedAsync(string peerId);
}

public class MeshServiceDescriptorValidator : IMeshServiceDescriptorValidator
{
    private readonly ILogger<MeshServiceDescriptorValidator> _logger;
    private readonly MeshServiceFabricOptions _options;
    private readonly IMeshPeerPublicKeyResolver _publicKeyResolver;

    private readonly Common.Moderation.PeerReputationService _peerReputationService;

    public MeshServiceDescriptorValidator(
        ILogger<MeshServiceDescriptorValidator> logger,
        Microsoft.Extensions.Options.IOptions<MeshServiceFabricOptions> options,
        Common.Moderation.PeerReputationService peerReputationService,
        IMeshPeerPublicKeyResolver publicKeyResolver)
    {
        _logger = logger;
        _options = options.Value;
        _peerReputationService = peerReputationService;
        _publicKeyResolver = publicKeyResolver;
    }

    public async Task<(bool IsValid, string Reason)> ValidateAsync(MeshServiceDescriptor descriptor)
    {
        // 1. Check required fields
        if (string.IsNullOrWhiteSpace(descriptor.ServiceId))
        {
            return (false, "ServiceId is empty");
        }

        if (string.IsNullOrWhiteSpace(descriptor.ServiceName))
        {
            return (false, "ServiceName is empty");
        }

        if (string.IsNullOrWhiteSpace(descriptor.OwnerPeerId))
        {
            return (false, "OwnerPeerId is empty");
        }

        // 2. Validate ServiceId derivation
        var expectedId = MeshServiceDescriptor.DeriveServiceId(descriptor.ServiceName, descriptor.OwnerPeerId);
        if (descriptor.ServiceId != expectedId)
        {
            return (false, $"ServiceId mismatch: expected {expectedId}, got {descriptor.ServiceId}");
        }

        // 3. Check timestamps
        var now = DateTimeOffset.UtcNow;
        var skew = TimeSpan.FromSeconds(_options.MaxTimestampSkewSeconds);

        if (descriptor.CreatedAt > now.Add(skew))
        {
            return (false, $"CreatedAt is in the future (skew > {_options.MaxTimestampSkewSeconds}s)");
        }

        if (descriptor.ExpiresAt < now.Subtract(skew))
        {
            return (false, $"Descriptor has expired (ExpiresAt={descriptor.ExpiresAt}, now={now})");
        }

        if (descriptor.CreatedAt >= descriptor.ExpiresAt)
        {
            return (false, "CreatedAt must be before ExpiresAt");
        }

        // 4. Check metadata size
        if (descriptor.Metadata != null)
        {
            if (descriptor.Metadata.Count > _options.MaxMetadataEntries)
            {
                return (false, $"Too many metadata entries ({descriptor.Metadata.Count} > {_options.MaxMetadataEntries})");
            }

            foreach (var kvp in descriptor.Metadata)
            {
                if (kvp.Key.Contains("username", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("ip", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"Metadata key '{kvp.Key}' may contain PII");
                }
            }
        }

        // 5. Check serialized size
        try
        {
            var serialized = MessagePackSerializer.Serialize(descriptor);
            if (serialized.Length > _options.MaxDescriptorBytes)
            {
                return (false, $"Descriptor too large ({serialized.Length} > {_options.MaxDescriptorBytes} bytes)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize descriptor {ServiceId}", descriptor.ServiceId);
            return (false, "Failed to serialize descriptor");
        }

        // 6. Validate the signature against a key authenticated by the owner's self-certifying peer descriptor.
        if (descriptor.Signature != null && descriptor.Signature.Length > 0)
        {
            if (descriptor.Signature.Length != 64)
            {
                return (false, $"Invalid signature length ({descriptor.Signature.Length}, expected 64)");
            }

            var trustedKeys = await _publicKeyResolver.ResolveTrustedKeysAsync(descriptor.OwnerPeerId);
            using var signer = new Transport.Ed25519Signer();
            if (!trustedKeys.Any(key => signer.Verify(descriptor.GetBytesForSigning(), descriptor.Signature, key)))
            {
                return (false, "Descriptor signature is not valid for the owner peer");
            }
        }
        else if (_options.ValidateDhtSignatures)
        {
            return (false, "Signature required but not provided");
        }

        // 7. Check if peer is allowed (ban list integration)
        if (!await IsPeerAllowedAsync(descriptor.OwnerPeerId))
        {
            return (false, $"Peer {descriptor.OwnerPeerId} is banned or quarantined");
        }

        return (true, string.Empty);
    }

    public async Task<bool> IsPeerAllowedAsync(string peerId)
    {
        if (string.IsNullOrWhiteSpace(peerId))
            return false;

        // Check with PeerReputationService
        return await _peerReputationService.IsPeerAllowedForPlanningAsync(peerId);
    }
}
