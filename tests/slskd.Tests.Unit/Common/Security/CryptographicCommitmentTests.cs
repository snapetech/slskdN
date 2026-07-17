// <copyright file="CryptographicCommitmentTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using slskd.Common.Security;
using Xunit;

public class CryptographicCommitmentTests
{
    [Fact]
    public void VerifyCommitment_AvoidsRevealedHashComparisonArrays()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('a', 64);
        var created = new List<CommitmentResult>(10_000);
        for (var index = 0; index < 10_000; index++)
        {
            created.Add(commitments.CreateCommitment(fileHash, "peer", "track.flac"));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var validCount = 0;
        foreach (var result in created)
        {
            if (commitments.VerifyCommitment(result.CommitmentId, fileHash, result.Nonce).IsValid)
            {
                validCount++;
            }
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(created.Count, validCount);
        Assert.InRange(allocated, 0, 2_300_000);
    }

    [Fact]
    public void VerifyContent_RepeatedUppercaseHashBoundsAllocation()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('A', 64);
        var created = commitments.CreateCommitment(fileHash, "peer", "track.flac");
        _ = commitments.VerifyCommitment(created.CommitmentId, fileHash, created.Nonce);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var validCount = 0;
        for (var index = 0; index < 100_000; index++)
        {
            if (commitments.VerifyContent(created.CommitmentId, fileHash))
            {
                validCount++;
            }
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(100_000, validCount);
        Assert.InRange(allocated, 0, 17_000_000);
    }

    [Fact]
    public void CreateCommitment_PreservesNormalizedHashContract()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('A', 64);

        var result = commitments.CreateCommitment(fileHash, "peer", "track.flac");
        var stored = commitments.GetCommitment(result.CommitmentId);
        var expectedCommitment = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(fileHash.ToLowerInvariant() + result.Nonce)));

        Assert.NotNull(stored);
        Assert.Matches("^[a-f0-9]{16}$", result.CommitmentId);
        Assert.Matches("^[a-f0-9]{64}$", result.Nonce);
        Assert.Equal(expectedCommitment, result.CommitmentHash);
        Assert.Equal(fileHash.ToLowerInvariant(), stored!.ActualHash);
        Assert.Equal(result.Nonce, stored.Nonce);
    }

    [Fact]
    public void VerifyCommitment_AcceptsOriginalHashAndNonce()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('A', 64);
        var result = commitments.CreateCommitment(fileHash, "peer", "track.flac");

        var verification = commitments.VerifyCommitment(result.CommitmentId, fileHash, result.Nonce);

        Assert.True(verification.IsValid);
        Assert.Equal(CommitmentState.Verified, commitments.GetCommitment(result.CommitmentId)!.State);
    }

    [Fact]
    public void VerifyCommitment_RejectsWrongNonce()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('a', 64);
        var result = commitments.CreateCommitment(fileHash, "peer", "track.flac");

        var verification = commitments.VerifyCommitment(result.CommitmentId, fileHash, new string('0', 64));

        Assert.False(verification.IsValid);
        Assert.Equal(CommitmentState.Failed, commitments.GetCommitment(result.CommitmentId)!.State);
    }

    [Fact]
    public void CreateAndVerifyCommitment_PreservesLongUnicodeInput()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('É', 300);
        var result = commitments.CreateCommitment(fileHash, "peer", "track.flac");

        var verification = commitments.VerifyCommitment(result.CommitmentId, fileHash, result.Nonce);

        Assert.True(verification.IsValid);
        Assert.Equal(fileHash.ToLowerInvariant(), commitments.GetCommitment(result.CommitmentId)!.ActualHash);
        Assert.True(commitments.VerifyContent(result.CommitmentId, fileHash));
        Assert.False(commitments.VerifyContent(result.CommitmentId, "different"));
    }
}
