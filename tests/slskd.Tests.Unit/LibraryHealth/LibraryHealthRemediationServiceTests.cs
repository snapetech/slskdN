// <copyright file="LibraryHealthRemediationServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.LibraryHealth;

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.HashDb;
using slskd.Integrations.MusicBrainz;
using slskd.Integrations.MusicBrainz.Models;
using slskd.LibraryHealth;
using slskd.LibraryHealth.Remediation;
using slskd.Transfers.MultiSource;
using Xunit;

public class LibraryHealthRemediationServiceTests
{
    [Fact]
    public async Task CreateRemediationJobAsync_AlbumCompletionStartsDownloadsForMissingRecordings()
    {
        using var metadata = JsonDocument.Parse("""
            [
              { "recording_id": "rec-1" },
              { "recording_id": "rec-2" }
            ]
            """);

        var issue = new LibraryIssue
        {
            IssueId = "issue-1",
            Type = LibraryIssueType.MissingTrackInRelease,
            FilePath = "/music/Artist/Album",
            MusicBrainzReleaseId = "release-1",
            CanAutoFix = true,
            Status = LibraryIssueStatus.Detected,
            Metadata = new Dictionary<string, object>
            {
                ["missing_tracks"] = metadata.RootElement.Clone(),
            },
        };

        var hashDb = new Mock<IHashDbService>();
        hashDb
            .Setup(service => service.GetLibraryIssuesAsync(It.IsAny<LibraryHealthIssueFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LibraryIssue> { issue });
        hashDb
            .Setup(service => service.UpdateLibraryIssueStatusAsync("issue-1", LibraryIssueStatus.Fixing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var musicBrainz = new Mock<IMusicBrainzClient>();
        musicBrainz
            .Setup(service => service.GetRecordingAsync("rec-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackTarget { MusicBrainzRecordingId = "rec-1", Artist = "Artist", Title = "Track One" });
        musicBrainz
            .Setup(service => service.GetRecordingAsync("rec-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackTarget { MusicBrainzRecordingId = "rec-2", Artist = "Artist", Title = "Track Two" });

        var requests = new ConcurrentBag<MultiSourceDownloadRequest>();
        var firstDownloadQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var multiSource = new Mock<IMultiSourceDownloadService>();
        multiSource
            .Setup(service => service.FindVerifiedSourcesAsync(It.IsAny<string>(), 0, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string filename, long _, string? _, CancellationToken _) => new ContentVerificationResult
            {
                Filename = filename,
                FileSize = 1234,
                BestSemanticKey = "rec/flac",
                SourcesByHash = new Dictionary<string, List<VerifiedSource>>
                {
                    ["hash"] = new()
                    {
                        new VerifiedSource { Username = "peer", FullPath = filename, ContentHash = "hash" },
                    },
                },
                SourcesBySemanticKey = new Dictionary<string, List<VerifiedSource>>
                {
                    ["rec/flac"] = new()
                    {
                        new VerifiedSource { Username = "peer", FullPath = filename, ContentHash = "hash" },
                    },
                },
            });
        multiSource
            .Setup(service => service.SelectCanonicalSourcesAsync(It.IsAny<ContentVerificationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentVerificationResult result, CancellationToken _) => result.BestSources);
        multiSource
            .Setup(service => service.DownloadAsync(It.IsAny<MultiSourceDownloadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MultiSourceDownloadRequest, CancellationToken>((request, _) =>
            {
                requests.Add(request);
                firstDownloadQueued.TrySetResult();
            })
            .ReturnsAsync((MultiSourceDownloadRequest request, CancellationToken _) => new MultiSourceDownloadResult
            {
                Id = request.Id,
                Filename = request.Filename,
                OutputPath = request.OutputPath,
                Success = true,
            });

        var service = new LibraryHealthRemediationService(
            Mock.Of<IServiceProvider>(),
            hashDb.Object,
            multiSource.Object,
            musicBrainz.Object,
            NullLogger<LibraryHealthRemediationService>.Instance);

        var jobId = await service.CreateRemediationJobAsync(new List<string> { "issue-1" });

        Assert.True(Guid.TryParse(jobId, out _));
        musicBrainz.Verify(client => client.GetRecordingAsync("rec-1", It.IsAny<CancellationToken>()), Times.Once);
        musicBrainz.Verify(client => client.GetRecordingAsync("rec-2", It.IsAny<CancellationToken>()), Times.Once);
        multiSource.Verify(service => service.FindVerifiedSourcesAsync("Artist - Track One.flac", 0, null, It.IsAny<CancellationToken>()), Times.Once);
        multiSource.Verify(service => service.FindVerifiedSourcesAsync("Artist - Track Two.flac", 0, null, It.IsAny<CancellationToken>()), Times.Once);
        hashDb.Verify(service => service.UpdateLibraryIssueStatusAsync("issue-1", LibraryIssueStatus.Fixing, It.IsAny<CancellationToken>()), Times.Once);

        await firstDownloadQueued.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Contains(requests, request => request.TargetMusicBrainzRecordingId == "rec-1" && request.OutputPath == "/music/Artist/Album/Artist - Track One.flac");
    }
}
