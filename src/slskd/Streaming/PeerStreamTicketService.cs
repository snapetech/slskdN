// <copyright file="PeerStreamTicketService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// In-memory ticket service for manual peer preview streams.
/// </summary>
public sealed class PeerStreamTicketService : IPeerStreamTicketService
{
    private const int MaxTickets = 1000;
    private const int MaxUsernameLength = 256;
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

    private readonly ConcurrentDictionary<string, PeerStreamTicket> _tickets = new();

    public PeerStreamTicket Create(PeerStreamTicketRequest request, string ownerKey, TimeSpan lifetime)
    {
        CleanupExpired();
        if (_tickets.Count >= MaxTickets)
        {
            throw new InvalidOperationException("Too many active peer stream tickets.");
        }

        var username = NormalizeUsername(request.Username);
        var filename = NormalizeFilename(request.Filename);
        var contentType = ResolveAudioContentType(filename);
        if (request.Size is < 0)
        {
            throw new ArgumentException("Size must be greater than or equal to zero.", nameof(request));
        }

        var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var claims = new PeerStreamTicket(
            ticket,
            username,
            filename,
            request.Size,
            ownerKey,
            DateTimeOffset.UtcNow.Add(lifetime),
            contentType);

        _tickets[ticket] = claims;
        return claims;
    }

    public PeerStreamTicket? Validate(string ticket)
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

    private static string NormalizeUsername(string username)
    {
        username = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || username.Length > MaxUsernameLength || username.Any(char.IsControl))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        return username;
    }

    private static string NormalizeFilename(string filename)
    {
        filename = filename?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filename) || filename.Length > MaxFilenameLength || filename.Any(char.IsControl))
        {
            throw new ArgumentException("Filename is required.", nameof(filename));
        }

        return filename;
    }

    private static string ResolveAudioContentType(string filename)
    {
        var extension = Path.GetExtension(filename);
        if (!AudioContentTypes.TryGetValue(extension, out var contentType))
        {
            throw new ArgumentException("Only audio files can be preview streamed from peers.", nameof(filename));
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
}
