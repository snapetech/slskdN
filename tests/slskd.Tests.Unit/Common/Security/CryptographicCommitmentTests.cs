// <copyright file="CryptographicCommitmentTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Security.Cryptography;
using System.Text;
using slskd.Common.Security;
using Xunit;

public class CryptographicCommitmentTests
{
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
    public void CreateAndVerifyCommitment_PreservesLongUnicodeInput()
    {
        using var commitments = new CryptographicCommitment();
        var fileHash = new string('É', 300);
        var result = commitments.CreateCommitment(fileHash, "peer", "track.flac");

        var verification = commitments.VerifyCommitment(result.CommitmentId, fileHash, result.Nonce);

        Assert.True(verification.IsValid);
        Assert.Equal(fileHash.ToLowerInvariant(), commitments.GetCommitment(result.CommitmentId)!.ActualHash);
    }
}
