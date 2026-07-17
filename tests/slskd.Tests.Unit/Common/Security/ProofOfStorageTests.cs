// <copyright file="ProofOfStorageTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using slskd.Common.Security;
using Xunit;

[Collection(AllocationTestCollection.Name)]
public class ProofOfStorageTests
{
    [Fact]
    public async Task GenerateResponseAsync_ReadsRequestedChunkAndBoundsAllocation()
    {
        var filePath = Path.GetTempFileName();
        try
        {
            var fileBytes = new byte[8192];
            for (var index = 0; index < fileBytes.Length; index++)
            {
                fileBytes[index] = (byte)index;
            }

            await File.WriteAllBytesAsync(filePath, fileBytes);
            using var challenges = new ProofOfStorage();
            var nonce = new string('\u00e9', 16);
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
            var expectedInput = new byte[nonceBytes.Length + 4096];
            Buffer.BlockCopy(nonceBytes, 0, expectedInput, 0, nonceBytes.Length);
            Buffer.BlockCopy(fileBytes, 1024, expectedInput, nonceBytes.Length, 4096);
            var expected = Convert.ToHexStringLower(SHA256.HashData(expectedInput));
            _ = await challenges.GenerateResponseAsync(filePath, 1024, 4096, nonce);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalAllocatedBytes(precise: true);
            for (var index = 0; index < 1000; index++)
            {
                var response = await challenges.GenerateResponseAsync(filePath, 1024, 4096, nonce);
                Assert.Equal(expected, response);
            }

            var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
            Assert.InRange(allocated, 0, 2_000_000);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void CreateChallenge_ReturnsBoundedLowercaseIdentifiers()
    {
        using var challenges = new ProofOfStorage();

        var request = challenges.CreateChallenge("track.flac", 100, "peer");

        Assert.Matches("^[0-9a-f]{16}$", request.ChallengeId);
        Assert.Matches("^[0-9a-f]{32}$", request.Nonce);
        Assert.Equal(0, request.Offset);
        Assert.Equal(100, request.Length);
    }

    [Fact]
    public void VerifyResponse_IgnoresInvariantCaseAndMarksChallengeVerified()
    {
        using var challenges = new ProofOfStorage();
        var request = challenges.CreateChallenge("track.flac", 10_000, "peer");

        var result = challenges.VerifyResponse(
            request.ChallengeId,
            new string('A', 64),
            new string('a', 64));

        Assert.True(result.IsValid);
        Assert.Equal(ChallengeState.Verified, challenges.GetChallenge(request.ChallengeId)!.State);
    }

    [Fact]
    public void VerifyResponse_InvalidProofMarksChallengeFailed()
    {
        using var challenges = new ProofOfStorage();
        var request = challenges.CreateChallenge("track.flac", 10_000, "peer");

        var result = challenges.VerifyResponse(request.ChallengeId, "different", "expected");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid proof - peer may not have the file", result.Error);
        Assert.Equal(ChallengeState.Failed, challenges.GetChallenge(request.ChallengeId)!.State);
    }

    [Fact]
    public void VerifyResponse_LongUnicodeProofUsesInvariantCaseComparison()
    {
        using var challenges = new ProofOfStorage();
        var request = challenges.CreateChallenge("track.flac", 10_000, "peer");

        var result = challenges.VerifyResponse(
            request.ChallengeId,
            new string('\u00c9', 600),
            new string('\u00e9', 600));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void VerifyResponse_DifferentUtf8LengthsFails()
    {
        using var challenges = new ProofOfStorage();
        var request = challenges.CreateChallenge("track.flac", 10_000, "peer");

        var result = challenges.VerifyResponse(request.ChallengeId, "a", "\u00e9");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChallengeCreationAndVerification_WidePopulationBoundsAllocation()
    {
        using var challenges = new ProofOfStorage();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var creationBefore = GC.GetAllocatedBytesForCurrentThread();
        var created = new List<ChallengeRequest>(ProofOfStorage.MaxPendingChallenges);
        for (var index = 0; index < ProofOfStorage.MaxPendingChallenges; index++)
        {
            created.Add(challenges.CreateChallenge("track.flac", 10_000, "peer"));
        }

        var creationAllocated = GC.GetAllocatedBytesForCurrentThread() - creationBefore;
        var response = new string('A', 64);
        var expectedResponse = new string('a', 64);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var verificationBefore = GC.GetAllocatedBytesForCurrentThread();
        var validResponses = 0;
        foreach (var challenge in created)
        {
            if (challenges.VerifyResponse(challenge.ChallengeId, response, expectedResponse).IsValid)
            {
                validResponses++;
            }
        }

        var verificationAllocated = GC.GetAllocatedBytesForCurrentThread() - verificationBefore;

        Assert.Equal(ProofOfStorage.MaxPendingChallenges, challenges.GetStats().TotalChallenges);
        Assert.Equal(ProofOfStorage.MaxPendingChallenges, validResponses);
        Assert.InRange(creationAllocated, 0, 620_000);
        Assert.InRange(verificationAllocated, 0, 220_000);
    }
}
