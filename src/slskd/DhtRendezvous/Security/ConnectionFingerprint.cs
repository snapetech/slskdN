// <copyright file="ConnectionFingerprint.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous.Security;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Captures and logs connection fingerprints for forensic analysis.
/// SECURITY: Helps identify patterns of malicious behavior across sessions.
/// </summary>
public sealed class ConnectionFingerprintService
{
    private readonly ILogger<ConnectionFingerprintService> _logger;
    private readonly ConcurrentDictionary<string, ConnectionFingerprint> _recentFingerprints = new();
    private readonly ConcurrentQueue<ConnectionEvent> _eventLog = new();
    private int _eventLogSize;

    /// <summary>
    /// Maximum events to keep in memory.
    /// </summary>
    public const int MaxEventLogSize = 10000;

    /// <summary>
    /// Maximum fingerprints to track.
    /// </summary>
    public const int MaxFingerprints = 1000;

    public ConnectionFingerprintService(ILogger<ConnectionFingerprintService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record a new connection attempt.
    /// </summary>
    public ConnectionFingerprint RecordConnection(
        IPAddress ip,
        int port,
        string? username,
        string? certificateThumbprint,
        IReadOnlyList<string>? features,
        string? clientVersion)
    {
        var fingerprint = new ConnectionFingerprint
        {
            Id = GenerateFingerprintId(),
            RemoteIp = ip.ToString(),
            RemotePort = port,
            Username = username,
            CertificateThumbprint = certificateThumbprint,
            Features = features?.ToList() ?? new List<string>(),
            ClientVersion = clientVersion,
            Timestamp = DateTimeOffset.UtcNow,
            IpHash = HashIp(ip),
        };

        // Store fingerprint
        if (_recentFingerprints.Count >= MaxFingerprints)
        {
            ConnectionFingerprint? oldest = null;
            foreach (var entry in _recentFingerprints)
            {
                if (oldest == null || entry.Value.Timestamp < oldest.Timestamp)
                {
                    oldest = entry.Value;
                }
            }

            if (oldest != null)
            {
                _recentFingerprints.TryRemove(oldest.Id, out _);
            }
        }

        _recentFingerprints[fingerprint.Id] = fingerprint;

        // Log the connection event
        RecordEvent(new ConnectionEvent
        {
            Type = ConnectionEventType.Connected,
            FingerprintId = fingerprint.Id,
            Timestamp = fingerprint.Timestamp,
            IpHash = fingerprint.IpHash,
            Username = username,
        });

        _logger.LogInformation(
            "Connection fingerprint {Id}: IP={IpHash}, User={Username}, Cert={Cert}, Features=[{Features}]",
            fingerprint.Id,
            fingerprint.IpHash,
            username ?? "(none)",
            certificateThumbprint?[..Math.Min(16, certificateThumbprint.Length)] ?? "(none)",
            string.Join(",", fingerprint.Features.Take(5)));

        return fingerprint;
    }

    /// <summary>
    /// Record a disconnection.
    /// </summary>
    public void RecordDisconnection(string fingerprintId, string? reason)
    {
        if (_recentFingerprints.TryGetValue(fingerprintId, out var fingerprint))
        {
            fingerprint.DisconnectedAt = DateTimeOffset.UtcNow;
            fingerprint.DisconnectReason = reason;

            RecordEvent(new ConnectionEvent
            {
                Type = ConnectionEventType.Disconnected,
                FingerprintId = fingerprintId,
                Timestamp = DateTimeOffset.UtcNow,
                IpHash = fingerprint.IpHash,
                Username = fingerprint.Username,
                Details = reason,
            });
        }
    }

    /// <summary>
    /// Record a security event for a connection.
    /// </summary>
    public void RecordSecurityEvent(
        string fingerprintId,
        string eventType,
        string details)
    {
        if (_recentFingerprints.TryGetValue(fingerprintId, out var fingerprint))
        {
            // SecurityEvents is a List<T> shared across threads (multiple concurrent connections
            // can record events for the same fingerprint); lock before mutating.
            lock (fingerprint.SecurityEvents)
            {
                fingerprint.SecurityEvents.Add(new SecurityEvent
                {
                    Type = eventType,
                    Details = details,
                    Timestamp = DateTimeOffset.UtcNow,
                });
            }

            RecordEvent(new ConnectionEvent
            {
                Type = ConnectionEventType.SecurityEvent,
                FingerprintId = fingerprintId,
                Timestamp = DateTimeOffset.UtcNow,
                IpHash = fingerprint.IpHash,
                Username = fingerprint.Username,
                Details = $"{eventType}: {details}",
            });

            _logger.LogWarning(
                "Security event for {Id} ({Username}): {Type} - {Details}",
                fingerprintId,
                fingerprint.Username ?? "(unknown)",
                eventType,
                details);
        }
    }

    /// <summary>
    /// Get fingerprint by ID.
    /// </summary>
    public ConnectionFingerprint? GetFingerprint(string id)
    {
        return _recentFingerprints.TryGetValue(id, out var fp) ? fp : null;
    }

    /// <summary>
    /// Find fingerprints matching criteria.
    /// </summary>
    public IReadOnlyList<ConnectionFingerprint> FindFingerprints(
        string? ipHash = null,
        string? username = null,
        string? certThumbprint = null,
        DateTimeOffset? since = null)
    {
        return _recentFingerprints
            .Select(entry => entry.Value)
            .Where(f =>
                (ipHash == null || f.IpHash == ipHash) &&
                (username == null || f.Username?.Equals(username, StringComparison.OrdinalIgnoreCase) == true) &&
                (certThumbprint == null || f.CertificateThumbprint == certThumbprint) &&
                (since == null || f.Timestamp >= since))
            .OrderByDescending(f => f.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Get recent events.
    /// </summary>
    public IReadOnlyList<ConnectionEvent> GetRecentEvents(int count = 100)
    {
        if (count <= 0)
        {
            return Array.Empty<ConnectionEvent>();
        }

        var recentEvents = new Queue<ConnectionEvent>(Math.Min(count, MaxEventLogSize));
        foreach (var connectionEvent in _eventLog)
        {
            if (recentEvents.Count == count)
            {
                recentEvents.Dequeue();
            }

            recentEvents.Enqueue(connectionEvent);
        }

        var result = recentEvents.ToArray();
        Array.Reverse(result);
        return result;
    }

    /// <summary>
    /// Get statistics about connections.
    /// </summary>
    public FingerprintStats GetStats()
    {
        var now = DateTimeOffset.UtcNow;
        var lastHour = now.AddHours(-1);
        var uniqueIps = new HashSet<string>();
        var uniqueUsernames = new HashSet<string>();
        var totalFingerprints = 0;
        var activeConnections = 0;
        var connectionsLastHour = 0;
        var totalSecurityEvents = 0;

        foreach (var entry in _recentFingerprints)
        {
            var fingerprint = entry.Value;
            totalFingerprints++;
            if (fingerprint.DisconnectedAt == null)
            {
                activeConnections++;
            }

            if (fingerprint.Timestamp >= lastHour)
            {
                connectionsLastHour++;
            }

            uniqueIps.Add(fingerprint.IpHash);
            if (fingerprint.Username != null)
            {
                uniqueUsernames.Add(fingerprint.Username);
            }

            lock (fingerprint.SecurityEvents)
            {
                totalSecurityEvents += fingerprint.SecurityEvents.Count;
            }
        }

        return new FingerprintStats
        {
            TotalFingerprints = totalFingerprints,
            ActiveConnections = activeConnections,
            ConnectionsLastHour = connectionsLastHour,
            UniqueIps = uniqueIps.Count,
            UniqueUsernames = uniqueUsernames.Count,
            TotalSecurityEvents = totalSecurityEvents,
            EventLogSize = Volatile.Read(ref _eventLogSize),
        };
    }

    private void RecordEvent(ConnectionEvent evt)
    {
        _eventLog.Enqueue(evt);

        if (Interlocked.Increment(ref _eventLogSize) > MaxEventLogSize &&
            _eventLog.TryDequeue(out _))
        {
            Interlocked.Decrement(ref _eventLogSize);
        }
    }

    private static string GenerateFingerprintId()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }

    /// <summary>
    /// Hash an IP address for privacy-preserving logging.
    /// </summary>
    private static string HashIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }
}

/// <summary>
/// A connection fingerprint.
/// </summary>
public sealed class ConnectionFingerprint
{
    public required string Id { get; init; }
    public required string RemoteIp { get; init; }
    public required int RemotePort { get; init; }
    public string? Username { get; init; }
    public string? CertificateThumbprint { get; init; }
    public required List<string> Features { get; init; }
    public string? ClientVersion { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string IpHash { get; init; }

    public DateTimeOffset? DisconnectedAt { get; set; }
    public string? DisconnectReason { get; set; }

    public List<SecurityEvent> SecurityEvents { get; } = new();

    public TimeSpan? Duration => DisconnectedAt.HasValue
        ? DisconnectedAt.Value - Timestamp
        : DateTimeOffset.UtcNow - Timestamp;
}

/// <summary>
/// A security event associated with a connection.
/// </summary>
public sealed class SecurityEvent
{
    public required string Type { get; init; }
    public required string Details { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// A connection event for the audit log.
/// </summary>
public sealed class ConnectionEvent
{
    public required ConnectionEventType Type { get; init; }
    public required string FingerprintId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string IpHash { get; init; }
    public string? Username { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// Types of connection events.
/// </summary>
public enum ConnectionEventType
{
    Connected,
    Disconnected,
    SecurityEvent,
    MessageReceived,
    MessageSent,
}

/// <summary>
/// Fingerprint statistics.
/// </summary>
public sealed class FingerprintStats
{
    public int TotalFingerprints { get; init; }
    public int ActiveConnections { get; init; }
    public int ConnectionsLastHour { get; init; }
    public int UniqueIps { get; init; }
    public int UniqueUsernames { get; init; }
    public int TotalSecurityEvents { get; init; }
    public int EventLogSize { get; init; }
}
