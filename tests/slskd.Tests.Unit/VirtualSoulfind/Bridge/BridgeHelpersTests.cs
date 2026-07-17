// <copyright file="BridgeHelpersTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.Bridge;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.VirtualSoulfind.Bridge;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class BridgeHelpersTests
{
    [Fact]
    public void PeerIdAnonymizer_PreservesHashCacheAndReverseLookup()
    {
        const string peerId = "peer:overlay:alpha";
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var first = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        var second = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        var reversed = anonymizer.GetPeerIdFromUsernameAsync(first, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedUsername(peerId), first);
        Assert.Same(first, second);
        Assert.Equal(peerId, reversed);
        Assert.Matches("^mesh-peer-[0-9a-f]{6}$", first);
    }

    [Fact]
    public void PeerIdAnonymizer_LongUnicodePeerIdPreservesHash()
    {
        var peerId = new string('\u00c9', 600);
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var result = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(ExpectedUsername(peerId), result);
    }

    [Fact]
    public void PeerIdAnonymizer_UnknownUsernameReturnsNull()
    {
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        var result = anonymizer.GetPeerIdFromUsernameAsync("mesh-peer-000000", CancellationToken.None).GetAwaiter().GetResult();

        Assert.Null(result);
    }

    [Fact]
    public void PeerIdAnonymizer_UncachedWidePopulationBoundsAllocation()
    {
        const int peerCount = 10_000;
        var peerIds = new string[peerCount];
        for (var index = 0; index < peerIds.Length; index++)
        {
            peerIds[index] = $"peer:overlay:{index:D5}";
        }

        var warmup = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);
        _ = warmup.GetAnonymizedUsernameAsync("warmup", CancellationToken.None).GetAwaiter().GetResult();
        var anonymizer = new PeerIdAnonymizer(NullLogger<PeerIdAnonymizer>.Instance);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        string? result = null;
        foreach (var peerId in peerIds)
        {
            result = anonymizer.GetAnonymizedUsernameAsync(peerId, CancellationToken.None).GetAwaiter().GetResult();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(ExpectedUsername(peerIds[^1]), result);
        Assert.InRange(allocated, 0, 6_500_000);
    }

    private static string ExpectedUsername(string peerId)
        => $"mesh-peer-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(peerId)).AsSpan(0, 3))}";
}
