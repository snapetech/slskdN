// <copyright file="MeshStreamTicketService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using slskd.Common.Security;

/// <summary>
/// In-memory ticket service for manual mesh preview streams.
/// </summary>
public sealed partial class MeshStreamTicketService : IMeshStreamTicketService
{
    private const int MaxTickets = 1000;
    private const int MaxIdLength = 512;
    private const int MaxFilenameLength = 4096;

    private static readonly IReadOnlyDictionary<string, string> AudioContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".flac"] = "audio/flac",
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/opus",
        [".wav"] = "audio/wav",
    };

    private readonly ConcurrentDictionary<string, MeshStreamTicket> _tickets = new();

    public MeshStreamTicket Create(MeshStreamTicketRequest request, string ownerKey, TimeSpan lifetime)
    {
        CleanupExpired();
        if (_tickets.Count >= MaxTickets)
        {
            throw new InvalidOperationException("Too many active mesh stream tickets.");
        }

        var contentId = NormalizeId(request.ContentId, nameof(request.ContentId));
        var filename = NormalizeFilename(request.Filename);
        var peerId = string.IsNullOrWhiteSpace(request.PeerId)
            ? null
            : NormalizeId(request.PeerId, nameof(request.PeerId));
        var contentType = ResolveAudioContentType(filename);
        if (request.ExpectedSize is < 0)
        {
            throw new ArgumentException("Expected size must be greater than or equal to zero.", nameof(request));
        }

        var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var claims = new MeshStreamTicket(
            ticket,
            contentId,
            filename,
            peerId,
            request.ExpectedSize,
            NormalizeExpectedHash(request.ExpectedHash),
            ownerKey,
            DateTimeOffset.UtcNow.Add(lifetime),
            contentType);

        _tickets[ticket] = claims;
        return claims;
    }

    public MeshStreamTicket? Validate(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        CleanupExpired();
        var key = ticket.Trim();
        if (!_tickets.TryGetValue(key, out var claims))
        {
            return null;
        }

        if (claims.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return claims;
        }

        _tickets.TryRemove(key, out _);
        return null;
    }

    private static string NormalizeId(string value, string name)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxIdLength || value.Any(char.IsControl))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value;
    }

    private static string NormalizeFilename(string filename)
    {
        filename = filename?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filename) ||
            filename.Length > MaxFilenameLength ||
            filename.Any(char.IsControl) ||
            PathGuard.ContainsTraversal(filename) ||
            Path.IsPathRooted(filename) ||
            WindowsDriveRegex().IsMatch(filename))
        {
            throw new ArgumentException("Filename is required.", nameof(filename));
        }

        return filename;
    }

    private static string? NormalizeExpectedHash(string? expectedHash)
    {
        expectedHash = expectedHash?.Trim();
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return null;
        }

        if (!Sha256HexRegex().IsMatch(expectedHash))
        {
            throw new ArgumentException("Expected hash must be a SHA-256 hex digest.", nameof(expectedHash));
        }

        return expectedHash.ToLowerInvariant();
    }

    private static string ResolveAudioContentType(string filename)
    {
        var extension = Path.GetExtension(filename);
        if (!AudioContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new ArgumentException("Only audio files can be preview streamed from mesh peers.", nameof(filename));
        }

        return contentType;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _tickets)
        {
            if (item.Value.ExpiresAtUtc <= now)
            {
                _tickets.TryRemove(item.Key, out _);
            }
        }
    }

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex Sha256HexRegex();

    [GeneratedRegex("^[a-zA-Z]:")]
    private static partial Regex WindowsDriveRegex();
}
