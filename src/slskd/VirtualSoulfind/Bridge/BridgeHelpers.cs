// <copyright file="BridgeHelpers.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.Bridge;

using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using slskd.VirtualSoulfind.ShadowIndex;

/// <summary>
/// Interface for peer ID anonymization for legacy clients.
/// </summary>
public interface IPeerIdAnonymizer
{
    /// <summary>
    /// Get anonymized username for a peer ID.
    /// </summary>
    Task<string> GetAnonymizedUsernameAsync(string peerId, CancellationToken ct = default);

    /// <summary>
    /// Get peer ID from anonymized username.
    /// </summary>
    Task<string?> GetPeerIdFromUsernameAsync(string username, CancellationToken ct = default);
}

/// <summary>
/// Peer ID anonymizer - maps overlay peer IDs to friendly usernames.
/// </summary>
public class PeerIdAnonymizer : IPeerIdAnonymizer
{
    private readonly ILogger<PeerIdAnonymizer> logger;
    private readonly ConcurrentDictionary<string, string> peerIdToUsername = new();
    private readonly ConcurrentDictionary<string, string> usernameToPeerId = new();

    public PeerIdAnonymizer(ILogger<PeerIdAnonymizer> logger)
    {
        this.logger = logger;
    }

    public Task<string> GetAnonymizedUsernameAsync(string peerId, CancellationToken ct)
    {
        if (peerIdToUsername.TryGetValue(peerId, out var username))
        {
            return Task.FromResult(username);
        }

        // Generate friendly username: mesh-peer-abc123
        username = ComputeAnonymizedUsername(peerId);

        peerIdToUsername[peerId] = username;
        usernameToPeerId[username] = peerId;

        logger.LogDebug("[VSF-BRIDGE] Anonymized {PeerId} → {Username}", peerId, username);

        return Task.FromResult(username);
    }

    public Task<string?> GetPeerIdFromUsernameAsync(string username, CancellationToken ct)
    {
        usernameToPeerId.TryGetValue(username, out var peerId);
        return Task.FromResult(peerId);
    }

    private static string ComputeAnonymizedUsername(string peerId)
    {
        var byteCount = Encoding.UTF8.GetByteCount(peerId);
        byte[]? rentedBytes = null;
        Span<byte> bytes = byteCount <= 512
            ? stackalloc byte[byteCount]
            : (rentedBytes = ArrayPool<byte>.Shared.Rent(byteCount));

        try
        {
            _ = Encoding.UTF8.GetBytes(peerId, bytes);
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(bytes[..byteCount], hash);

            Span<char> username = stackalloc char[16];
            "mesh-peer-".AsSpan().CopyTo(username);
            _ = Convert.TryToHexStringLower(hash[..3], username[10..], out _);
            return new string(username);
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

/// <summary>
/// Interface for filename generation from variants.
/// </summary>
public interface IFilenameGenerator
{
    /// <summary>
    /// Generate friendly filename from variant hint.
    /// </summary>
    Task<string> GenerateFilenameAsync(
        string artist,
        string title,
        VariantHint variant,
        CancellationToken ct = default);
}

/// <summary>
/// Filename generator - creates friendly filenames for legacy clients.
/// </summary>
public class FilenameGenerator : IFilenameGenerator
{
    private static readonly char[] InvalidFilenameCharacters = Path.GetInvalidFileNameChars();
    private readonly ILogger<FilenameGenerator> logger;

    public FilenameGenerator(ILogger<FilenameGenerator> logger)
    {
        this.logger = logger;
    }

    public Task<string> GenerateFilenameAsync(
        string artist,
        string title,
        VariantHint variant,
        CancellationToken ct)
    {
        var codec = variant.Codec;
        var culture = CultureInfo.CurrentCulture;
        var bitrateLength = GetFormattedInt32Length(variant.BitrateKbps, culture.NumberFormat);
        var filenameLength = artist.Length + title.Length + (codec.Length * 2) + bitrateLength + 12;
        char[]? rentedCharacters = null;
        Span<char> characters = filenameLength <= 512
            ? stackalloc char[filenameLength]
            : (rentedCharacters = ArrayPool<char>.Shared.Rent(filenameLength));

        try
        {
            var position = 0;
            artist.AsSpan().CopyTo(characters[position..]);
            position += artist.Length;
            " - ".AsSpan().CopyTo(characters[position..]);
            position += 3;
            title.AsSpan().CopyTo(characters[position..]);
            position += title.Length;
            " [".AsSpan().CopyTo(characters[position..]);
            position += 2;
            codec.AsSpan().CopyTo(characters[position..]);
            position += codec.Length;
            characters[position++] = ' ';
            if (!variant.BitrateKbps.TryFormat(
                    characters[position..],
                    out var bitrateCharactersWritten,
                    provider: culture))
            {
                return GenerateFilenameWithExpandedLowercaseExtension(artist, title, variant);
            }

            position += bitrateCharactersWritten;
            "kbps].".AsSpan().CopyTo(characters[position..]);
            position += 6;

            var extensionLength = codec.AsSpan().ToLowerInvariant(characters[position..]);
            if (extensionLength < 0)
            {
                return GenerateFilenameWithExpandedLowercaseExtension(artist, title, variant);
            }

            position += extensionLength;
            var filename = SanitizeFilename(new string(characters[..position]));

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("[VSF-BRIDGE] Generated filename: {Filename}", filename);
            }

            return Task.FromResult(filename);
        }
        finally
        {
            if (rentedCharacters != null)
            {
                ArrayPool<char>.Shared.Return(rentedCharacters, clearArray: true);
            }
        }
    }

    private Task<string> GenerateFilenameWithExpandedLowercaseExtension(
        string artist,
        string title,
        VariantHint variant)
    {
        var filename = SanitizeFilename(
            $"{artist} - {title} [{variant.Codec} {variant.BitrateKbps}kbps].{variant.Codec.ToLowerInvariant()}");
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("[VSF-BRIDGE] Generated filename: {Filename}", filename);
        }

        return Task.FromResult(filename);
    }

    private static int GetFormattedInt32Length(int value, NumberFormatInfo numberFormat)
    {
        var remaining = value < 0 ? (uint)(-(long)value) : (uint)value;
        var length = value < 0 ? numberFormat.NegativeSign.Length : 0;

        do
        {
            length++;
            remaining /= 10;
        }
        while (remaining != 0);

        return length;
    }

    private static string SanitizeFilename(string filename)
    {
        if (filename.AsSpan().IndexOfAny(InvalidFilenameCharacters) < 0)
        {
            return filename;
        }

        return string.Join(
            "_",
            filename.Split(InvalidFilenameCharacters, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Interface for room-scene mapping.
/// </summary>
public interface IRoomSceneMapper
{
    /// <summary>
    /// Map legacy room name to scene ID.
    /// </summary>
    string MapRoomToScene(string roomName);

    /// <summary>
    /// Map scene ID to legacy room name.
    /// </summary>
    string MapSceneToRoom(string sceneId);
}

/// <summary>
/// Room-scene mapper for legacy compatibility.
/// </summary>
public class RoomSceneMapper : IRoomSceneMapper
{
    private readonly ILogger<RoomSceneMapper> logger;

    public RoomSceneMapper(ILogger<RoomSceneMapper> logger)
    {
        this.logger = logger;
    }

    public string MapRoomToScene(string roomName)
    {
        // "warp" → "scene:label:warp-records"
        // "techno" → "scene:genre:techno"
        var normalized = roomName.ToLowerInvariant().Replace(" ", "-");

        // Heuristic: if it looks like a label, treat as label scene
        if (IsLabelRoom(roomName))
        {
            return $"scene:label:{normalized}";
        }

        // Otherwise treat as genre scene
        return $"scene:genre:{normalized}";
    }

    public string MapSceneToRoom(string sceneId)
    {
        // "scene:label:warp-records" → "warp"
        // "scene:genre:techno" → "techno"
        var parts = sceneId.Split(':');
        if (parts.Length >= 3)
        {
            return parts[2].Replace("-", " ");
        }

        return sceneId;
    }

    private bool IsLabelRoom(string roomName)
    {
        // Heuristic: common label keywords
        var labelKeywords = new[] { "records", "music", "label", "recordings" };
        var lower = roomName.ToLowerInvariant();
        return labelKeywords.Any(keyword => lower.Contains(keyword));
    }
}
