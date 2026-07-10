// <copyright file="JwtRevocationStore.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Core.Security;

using System.Text.Json;
using slskd.Common.IO;

/// <summary>
/// Durable, expiry-bounded JWT revocation storage.
/// </summary>
public sealed class JwtRevocationStore
{
    private readonly object _lock = new();
    private readonly string _path;
    private readonly Dictionary<string, DateTimeOffset> _revocations;
    private DateTimeOffset _lastSweep = DateTimeOffset.UtcNow;

    public JwtRevocationStore(string path)
    {
        _path = path;
        _revocations = Load(path);
        RemoveExpired(DateTimeOffset.UtcNow, persist: true);
    }

    public void Revoke(string jti, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(jti) || expiresAt <= DateTimeOffset.UtcNow)
        {
            return;
        }

        lock (_lock)
        {
            _revocations[jti] = expiresAt;
            Persist();
        }
    }

    public bool IsRevoked(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastSweep >= TimeSpan.FromMinutes(10))
            {
                RemoveExpired(now, persist: true);
                _lastSweep = now;
            }

            return _revocations.TryGetValue(jti, out var expiresAt) && expiresAt > now;
        }
    }

    private static Dictionary<string, DateTimeOffset> Load(string path)
    {
        if (!File.Exists(path))
        {
            return new(StringComparer.Ordinal);
        }

        try
        {
            var revocations = JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(File.ReadAllText(path));
            return revocations is null
                ? new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal)
                : new Dictionary<string, DateTimeOffset>(revocations, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new InvalidDataException("JWT revocation state could not be loaded safely.", ex);
        }
    }

    private void RemoveExpired(DateTimeOffset now, bool persist)
    {
        var expired = _revocations
            .Where(entry => entry.Value <= now)
            .Select(entry => entry.Key)
            .ToList();
        foreach (var jti in expired)
        {
            _revocations.Remove(jti);
        }

        if (persist && expired.Count > 0)
        {
            Persist();
        }
    }

    private void Persist()
    {
        AtomicFileWriter.WriteAllText(
            _path,
            JsonSerializer.Serialize(_revocations),
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
