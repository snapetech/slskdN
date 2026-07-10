// <copyright file="JwtRevocationStoreTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Core.Security;

using System.Text.Json;
using slskd.Core.Security;
using Xunit;

public class JwtRevocationStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slskdn-jwt-revocations-{Guid.NewGuid():N}");

    [Fact]
    public void Revoke_PersistsAcrossStoreRestart()
    {
        var path = Path.Combine(_directory, "jwt-revocations.json");
        var store = new JwtRevocationStore(path);

        store.Revoke("token-id", DateTimeOffset.UtcNow.AddHours(1));

        var restartedStore = new JwtRevocationStore(path);
        Assert.True(restartedStore.IsRevoked("token-id"));
    }

    [Fact]
    public void Constructor_RemovesExpiredRevocationsFromDurableState()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "jwt-revocations.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new Dictionary<string, DateTimeOffset>
        {
            ["expired-token"] = DateTimeOffset.UtcNow.AddMinutes(-1),
        }));

        var store = new JwtRevocationStore(path);

        Assert.False(store.IsRevoked("expired-token"));
        Assert.DoesNotContain("expired-token", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenDurableStateIsCorrupt_FailsClosed()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "jwt-revocations.json");
        File.WriteAllText(path, "not-json");

        Assert.Throws<InvalidDataException>(() => new JwtRevocationStore(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
