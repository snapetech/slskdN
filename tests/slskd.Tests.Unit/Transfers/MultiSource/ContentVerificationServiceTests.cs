// <copyright file="ContentVerificationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource;

using Moq;
using slskd.Transfers.MultiSource;
using Soulseek;
using Xunit;

public class ContentVerificationServiceTests
{
    [Fact]
    public void ContentVerificationResult_BestSources_ReturnsSnapshot()
    {
        var source = new VerifiedSource { Username = "peer-a", ContentHash = "hash-a" };
        var result = new ContentVerificationResult
        {
            SourcesByHash = new Dictionary<string, List<VerifiedSource>>
            {
                ["hash-a"] = new() { source },
            },
        };

        var bestSources = result.BestSources;

        bestSources.Clear();

        Assert.Single(result.SourcesByHash["hash-a"]);
        Assert.Single(result.BestSources);
    }

    [Fact]
    public void ContentVerificationResult_BestSemanticSources_ReturnsSnapshot()
    {
        var source = new VerifiedSource { Username = "peer-a", MusicBrainzRecordingId = "mbid-a" };
        var result = new ContentVerificationResult
        {
            BestSemanticKey = "mbid-a|flac",
            SourcesBySemanticKey = new Dictionary<string, List<VerifiedSource>>
            {
                ["mbid-a|flac"] = new() { source },
            },
        };

        var bestSources = result.BestSemanticSources.ToList();

        bestSources.Clear();

        Assert.Single(result.SourcesBySemanticKey["mbid-a|flac"]);
        Assert.Single(result.BestSemanticSources);
    }

    [Fact]
    public async Task VerifySourcesAsync_BoundsConcurrentSoulseekProbes()
    {
        var active = 0;
        var maxActive = 0;
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .Returns(async (
                string username,
                string remoteFilename,
                Func<Task<Stream>> outputStreamFactory,
                long size,
                long startOffset,
                int? token,
                TransferOptions options,
                CancellationToken? cancellationToken) =>
            {
                var current = Interlocked.Increment(ref active);
                int observed;
                do
                {
                    observed = Volatile.Read(ref maxActive);
                    if (current <= observed)
                    {
                        break;
                    }
                }
                while (Interlocked.CompareExchange(ref maxActive, current, observed) != observed);

                await Task.Delay(50, cancellationToken ?? CancellationToken.None);
                var stream = await outputStreamFactory();
                await stream.WriteAsync(new byte[ContentVerificationService.VerificationChunkSize], cancellationToken ?? CancellationToken.None);
                Interlocked.Decrement(ref active);
                return new Transfer(TransferDirection.Download, username, remoteFilename, token ?? 1, TransferStates.Completed, size, 0, ContentVerificationService.VerificationChunkSize);
            });

        var service = new ContentVerificationService(soulseekClient.Object);
        var request = new ContentVerificationRequest
        {
            Filename = "song.flac",
            FileSize = ContentVerificationService.VerificationChunkSize + 1,
            TimeoutMs = 5000,
        };

        for (var i = 0; i < 10; i++)
        {
            request.CandidateSources[$"concurrency-peer-{Guid.NewGuid():N}-{i}"] = $"song-{i}.flac";
        }

        await service.VerifySourcesAsync(request, CancellationToken.None);

        Assert.InRange(maxActive, 1, ContentVerificationService.MaxConcurrentVerificationProbes);
    }

    [Fact]
    public async Task VerifySourcesAsync_WhenDownloadThrows_ReturnsSanitizedFailureReason()
    {
        var username = $"test-peer-{Guid.NewGuid():N}";
        var soulseekClient = new Mock<ISoulseekClient>();
        soulseekClient
            .Setup(client => client.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<Task<Stream>>>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<int?>(),
                It.IsAny<TransferOptions>(),
                It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("sensitive verification detail"));

        var service = new ContentVerificationService(soulseekClient.Object);

        var result = await service.VerifySourcesAsync(
            new ContentVerificationRequest
            {
                Filename = "song.flac",
                FileSize = 1234,
                CandidateSources = new Dictionary<string, string>
                {
                    [username] = @"Music\song.flac",
                },
                TimeoutMs = 1000,
            },
            CancellationToken.None);

        var failed = Assert.Single(result.FailedSources);
        Assert.Equal(username, failed.Username);
        Assert.Equal("File too small for verification", failed.Reason);
        Assert.DoesNotContain("sensitive", failed.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
