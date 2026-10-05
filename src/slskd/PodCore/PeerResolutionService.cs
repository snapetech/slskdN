// <copyright file="PeerResolutionService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.PodCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Common.Security;
using slskd.Mesh.Dht;

/// <summary>
/// Resolves pod peer IDs to network endpoints (IPEndPoint) and usernames.
/// </summary>
public interface IPeerResolutionService
{
    /// <summary>
    /// Resolves a pod peer ID to a Soulseek username.
    /// </summary>
    Task<string?> ResolvePeerIdToUsernameAsync(string peerId, CancellationToken ct = default);

    /// <summary>
    /// Resolves a pod peer ID to an IPEndPoint for QUIC overlay routing.
    /// </summary>
    Task<IPEndPoint?> ResolvePeerIdToEndpointAsync(string peerId, CancellationToken ct = default);

    /// <summary>
    /// Registers a peer ID to username mapping.
    /// </summary>
    void RegisterPeerMapping(string peerId, string username, IPEndPoint? endpoint = null);
}

/// <summary>
/// Implements peer ID resolution using DHT and in-memory mappings.
/// </summary>
public class PeerResolutionService : IPeerResolutionService
{
    private readonly IMeshDhtClient dht;
    private readonly ILogger<PeerResolutionService> logger;
    private readonly Dictionary<string, PeerMapping> peerMappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly object mappingsLock = new();
    private const string PeerMetadataPrefix = "peer:metadata:";

    public PeerResolutionService(
        IMeshDhtClient dht,
        ILogger<PeerResolutionService> logger)
    {
        this.dht = dht;
        this.logger = logger;
    }

    public async Task<string?> ResolvePeerIdToUsernameAsync(string peerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            return null;
        }

        var normalizedPeerId = peerId.Trim();

        try
        {
            // Check in-memory mapping first
            lock (mappingsLock)
            {
                if (peerMappings.TryGetValue(normalizedPeerId, out var mapping) && !string.IsNullOrWhiteSpace(mapping.Username))
                {
                    logger.LogDebug("[PeerResolution] Found username mapping for peer {PeerId}: {Username}",
                        LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId),
                        LoggingSanitizer.SanitizeExternalIdentifier(mapping.Username));
                    return mapping.Username.Trim();
                }
            }

            // Query DHT for peer metadata
            var dhtKey = $"{PeerMetadataPrefix}{normalizedPeerId}";
            var metadata = await dht.GetAsync<PeerMetadata>(dhtKey, ct);

            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Username))
            {
                var normalizedUsername = metadata.Username.Trim();
                var metadataPeerId = string.IsNullOrWhiteSpace(metadata.PeerId) ? normalizedPeerId : metadata.PeerId.Trim();
                var parsedEndpoint = metadata.Endpoint != null
                    ? await ParseEndpointAsync(metadata.Endpoint, ct).ConfigureAwait(false)
                    : null;

                // Cache the mapping
                lock (mappingsLock)
                {
                    var mapping = new PeerMapping
                    {
                        PeerId = metadataPeerId,
                        Username = normalizedUsername,
                        Endpoint = parsedEndpoint
                    };
                    peerMappings[normalizedPeerId] = mapping;
                    peerMappings[metadataPeerId] = mapping;
                    peerMappings[normalizedUsername] = mapping;
                }

                logger.LogDebug("[PeerResolution] Resolved peer {PeerId} to username {Username} via DHT",
                    LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId),
                    LoggingSanitizer.SanitizeExternalIdentifier(normalizedUsername));
                return normalizedUsername;
            }

            // Fallback: assume peer ID might be a username (for backward compatibility)
            logger.LogDebug("[PeerResolution] No mapping found for peer {PeerId}, using peer ID as username",
                LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId));
            return normalizedPeerId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("[PeerResolution] Error resolving peer {PeerId} to username; exception: {Exception}", LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId), LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));

            // Fallback to peer ID
            return normalizedPeerId;
        }
    }

    public async Task<IPEndPoint?> ResolvePeerIdToEndpointAsync(string peerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            return null;
        }

        var normalizedPeerId = peerId.Trim();

        try
        {
            // Check in-memory mapping first
            lock (mappingsLock)
            {
                if (peerMappings.TryGetValue(normalizedPeerId, out var mapping) && mapping.Endpoint != null)
                {
                    logger.LogDebug("[PeerResolution] Found endpoint mapping for peer {PeerId}: {Endpoint}",
                        LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId), mapping.Endpoint);
                    return mapping.Endpoint;
                }

                var aliasMapping = peerMappings.Values.FirstOrDefault(m =>
                    !string.IsNullOrWhiteSpace(m.Username) &&
                    string.Equals(m.Username, normalizedPeerId, StringComparison.OrdinalIgnoreCase) &&
                    m.Endpoint != null);
                if (aliasMapping?.Endpoint != null)
                {
                    logger.LogDebug("[PeerResolution] Resolved endpoint for alias {PeerId}: {Endpoint}",
                        LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId), aliasMapping.Endpoint);
                    return aliasMapping.Endpoint;
                }
            }

            // Query DHT for peer metadata
            var dhtKey = $"{PeerMetadataPrefix}{normalizedPeerId}";
            var metadata = await dht.GetAsync<PeerMetadata>(dhtKey, ct);

            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Endpoint))
            {
                var endpoint = await ParseEndpointAsync(metadata.Endpoint, ct).ConfigureAwait(false);
                if (endpoint != null)
                {
                    var metadataPeerId = string.IsNullOrWhiteSpace(metadata.PeerId) ? normalizedPeerId : metadata.PeerId.Trim();
                    var normalizedUsername = metadata.Username?.Trim();

                    // Cache the mapping
                    lock (mappingsLock)
                    {
                        if (!peerMappings.TryGetValue(normalizedPeerId, out var existing))
                        {
                            existing = new PeerMapping { PeerId = metadataPeerId };
                        }

                        existing.PeerId = metadataPeerId;
                        existing.Username = normalizedUsername;
                        existing.Endpoint = endpoint;
                        peerMappings[normalizedPeerId] = existing;
                        peerMappings[metadataPeerId] = existing;
                        if (!string.IsNullOrWhiteSpace(normalizedUsername))
                        {
                            peerMappings[normalizedUsername] = existing;
                        }
                    }

                    logger.LogDebug("[PeerResolution] Resolved peer {PeerId} to endpoint {Endpoint} via DHT",
                        LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId), endpoint);
                    return endpoint;
                }
            }

            logger.LogDebug("[PeerResolution] No endpoint found for peer {PeerId}",
                LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId));
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("[PeerResolution] Error resolving peer {PeerId} to endpoint; exception: {Exception}", LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId), LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            return null;
        }
    }

    public void RegisterPeerMapping(string peerId, string username, IPEndPoint? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(peerId) || string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var normalizedPeerId = peerId.Trim();
        var normalizedUsername = username.Trim();

        lock (mappingsLock)
        {
            var mapping = new PeerMapping
            {
                PeerId = normalizedPeerId,
                Username = normalizedUsername,
                Endpoint = endpoint
            };
            peerMappings[normalizedPeerId] = mapping;
            peerMappings[normalizedUsername] = mapping;
        }

        logger.LogDebug("[PeerResolution] Registered mapping: peer {PeerId} -> username {Username}, endpoint {Endpoint}",
            LoggingSanitizer.SanitizeExternalIdentifier(normalizedPeerId),
            LoggingSanitizer.SanitizeExternalIdentifier(normalizedUsername), endpoint?.ToString() ?? "none");
    }

    private static async Task<IPEndPoint?> ParseEndpointAsync(string endpointString, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpointString))
        {
            return null;
        }

        // Support formats: "ip:port", "[ipv6]:port", "udp://ip:port", "tcp://ip:port"
        var normalized = endpointString.Trim();
        if (normalized.StartsWith("udp://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["udp://".Length..];
        }
        else if (normalized.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["tcp://".Length..];
        }

        string hostPart;
        string portPart;
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            var closingBracketIndex = normalized.IndexOf(']');
            if (closingBracketIndex <= 1 ||
                closingBracketIndex + 2 >= normalized.Length ||
                normalized[closingBracketIndex + 1] != ':')
            {
                return null;
            }

            hostPart = normalized[1..closingBracketIndex];
            portPart = normalized[(closingBracketIndex + 2)..];
        }
        else
        {
            var separatorIndex = normalized.LastIndexOf(':');
            if (separatorIndex <= 0 || separatorIndex == normalized.Length - 1)
            {
                return null;
            }

            hostPart = normalized[..separatorIndex];
            portPart = normalized[(separatorIndex + 1)..];
        }

        if (!int.TryParse(portPart, out var port) || port is <= 0 or > ushort.MaxValue)
        {
            return null;
        }

        if (IPAddress.TryParse(hostPart, out var ip))
        {
            return new IPEndPoint(ip, port);
        }

        var hostname = hostPart.Trim();
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return null;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(hostname, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }

        var resolved = addresses.FirstOrDefault(address =>
            address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
        return resolved == null ? null : new IPEndPoint(resolved, port);
    }

    private class PeerMapping
    {
        public string PeerId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public IPEndPoint? Endpoint { get; set; }
    }
}

/// <summary>
/// Peer metadata stored in DHT.
/// </summary>
public class PeerMetadata
{
    public string PeerId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Endpoint { get; set; } // Format: "ip:port" or "udp://ip:port"
    public long UpdatedAt { get; set; } // Unix timestamp in milliseconds
}
