// <copyright file="UsernamePseudonymizer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.Capture;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IUsernamePseudonymizer
{
    Task<string> GetPeerIdAsync(string soulseekUsername, CancellationToken ct = default);
    Task<string?> GetUsernameAsync(string peerId, CancellationToken ct = default);
}

/// <summary>
/// Deterministic pseudonymization of Soulseek usernames to peer IDs.
/// Phase 6A: T-802 - Real implementation with deterministic hashing.
/// </summary>
public class UsernamePseudonymizer : IUsernamePseudonymizer
{
    private readonly ILogger<UsernamePseudonymizer> logger;
    private readonly ConcurrentDictionary<string, string> usernameToPeerId = new();
    private readonly ConcurrentDictionary<string, string> peerIdToUsername = new();

    // Salt for pseudonymization (prevents rainbow table attacks)
    // In production, this should be configurable per-instance
    private static readonly byte[] PseudonymizationSalt = Encoding.UTF8.GetBytes("slskdn-vsf-pseudonymization-salt-v1");

    public UsernamePseudonymizer(ILogger<UsernamePseudonymizer> logger)
    {
        this.logger = logger;
    }

    public Task<string> GetPeerIdAsync(string soulseekUsername, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(soulseekUsername))
        {
            throw new ArgumentException("Username cannot be null or empty", nameof(soulseekUsername));
        }

        // Check cache first
        if (usernameToPeerId.TryGetValue(soulseekUsername, out var cachedPeerId))
        {
            return Task.FromResult(cachedPeerId);
        }

        // Generate deterministic peer ID using SHA256 hash
        var peerId = ComputePeerId(soulseekUsername);

        // Cache both directions
        usernameToPeerId.TryAdd(soulseekUsername, peerId);
        peerIdToUsername.TryAdd(peerId, soulseekUsername);

        logger.LogTrace("[VSF-PSEUDO] Pseudonymized {Username} -> {PeerId}", soulseekUsername, peerId);

        return Task.FromResult(peerId);
    }

    public Task<string?> GetUsernameAsync(string peerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            return Task.FromResult<string?>(null);
        }

        // Check cache
        if (peerIdToUsername.TryGetValue(peerId, out var username))
        {
            return Task.FromResult<string?>(username);
        }

        // Cannot reverse hash, so return null
        return Task.FromResult<string?>(null);
    }

    private static string ComputePeerId(string username)
    {
        // Use SHA256 to create deterministic but non-reversible peer ID
        var normalizedUsername = username.ToLowerInvariant();
        var byteCount = Encoding.UTF8.GetByteCount(normalizedUsername) + PseudonymizationSalt.Length;
        byte[]? rentedBytes = null;
        Span<byte> bytes = byteCount <= 512
            ? stackalloc byte[byteCount]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount));

        try
        {
            var bytesWritten = Encoding.UTF8.GetBytes(normalizedUsername, bytes);
            PseudonymizationSalt.CopyTo(bytes[bytesWritten..]);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(bytes[..byteCount], hash);

            // Take first 20 bytes (160 bits) and encode as hex
            Span<char> peerId = stackalloc char[49];
            "peer:vsf:".AsSpan().CopyTo(peerId);
            _ = Convert.TryToHexStringLower(hash[..20], peerId[9..], out _);
            return new string(peerId);
        }
        finally
        {
            if (rentedBytes != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBytes, clearArray: true);
            }
        }
    }
}
