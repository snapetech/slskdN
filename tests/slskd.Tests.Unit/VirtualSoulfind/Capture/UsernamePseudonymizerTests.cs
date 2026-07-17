// <copyright file="UsernamePseudonymizerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Capture;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.VirtualSoulfind.Capture;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class UsernamePseudonymizerTests
{
    [Fact]
    public void GetPeerIdAsync_PreservesSaltedNormalizedHashAndReverseLookup()
    {
        var pseudonymizer = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);

        var first = pseudonymizer.GetPeerIdAsync("Alice", CancellationToken.None).GetAwaiter().GetResult();
        var second = pseudonymizer.GetPeerIdAsync("alice", CancellationToken.None).GetAwaiter().GetResult();
        var reversed = pseudonymizer.GetUsernameAsync(first, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedPeerId("Alice"), first);
        Assert.Equal(first, second);
        Assert.Equal("Alice", reversed);
        Assert.Matches("^peer:vsf:[0-9a-f]{40}$", first);
    }

    [Fact]
    public void GetPeerIdAsync_LongUnicodeUsernamePreservesHash()
    {
        var username = new string('\u00c9', 600);
        var pseudonymizer = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);

        var result = pseudonymizer.GetPeerIdAsync(username, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedPeerId(username), result);
    }

    [Fact]
    public void GetPeerIdAsync_BlankUsernameThrows()
    {
        var pseudonymizer = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);

        Assert.Throws<ArgumentException>(
            () => pseudonymizer.GetPeerIdAsync(" ", CancellationToken.None).GetAwaiter().GetResult());
    }

    [Fact]
    public void GetUsernameAsync_UnknownPeerReturnsNull()
    {
        var pseudonymizer = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);

        var result = pseudonymizer.GetUsernameAsync("peer:vsf:unknown", CancellationToken.None).GetAwaiter().GetResult();

        Assert.Null(result);
    }

    [Fact]
    public void GetPeerIdAsync_UncachedWidePopulationBoundsAllocation()
    {
        const int usernameCount = 10_000;
        var usernames = new string[usernameCount];
        for (var index = 0; index < usernames.Length; index++)
        {
            usernames[index] = $"listener-{index:D5}";
        }

        var warmup = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);
        _ = warmup.GetPeerIdAsync("warmup", CancellationToken.None).GetAwaiter().GetResult();
        var pseudonymizer = new UsernamePseudonymizer(NullLogger<UsernamePseudonymizer>.Instance);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        foreach (var username in usernames)
        {
            result = pseudonymizer.GetPeerIdAsync(username, CancellationToken.None).GetAwaiter().GetResult();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(ExpectedPeerId(usernames[^1]), result);
        Assert.InRange(allocated, 0, 7_000_000);
    }

    private static string ExpectedPeerId(string username)
    {
        var input = Encoding.UTF8.GetBytes(
            username.ToLowerInvariant() + "slskdn-vsf-pseudonymization-salt-v1");
        return $"peer:vsf:{Convert.ToHexStringLower(SHA256.HashData(input).AsSpan(0, 20))}";
    }
}
