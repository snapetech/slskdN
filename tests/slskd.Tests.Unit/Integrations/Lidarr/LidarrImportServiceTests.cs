// <copyright file="LidarrImportServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.Lidarr;

using System.Net.Http;
using System.Text.Json;
using slskd.Events;
using slskd.Integrations.Lidarr;
using Xunit;

public class LidarrImportServiceTests
{
    [Fact]
    public async Task ImportCompletedDirectoryAsync_WithSafeCandidate_QueuesManualImport()
    {
        var client = new FakeLidarrClient
        {
            Candidates =
            [
                SafeCandidate(),
            ],
        };
        var service = CreateService(
            client,
            new Options.IntegrationOptions.LidarrOptions
            {
                Enabled = true,
                Url = "http://lidarr.test",
                ApiKey = "key",
                AutoImportCompleted = true,
                ImportMode = "copy",
                ImportReplaceExistingFiles = true,
            });

        var result = await service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.SafeCandidateCount);
        Assert.Equal(42, result.CommandId);
        Assert.Equal("Copy", result.ImportMode);
        Assert.Single(client.ImportedFiles);
        Assert.True(client.ImportedFiles[0].ReplaceExistingFiles);
        Assert.Equal("Copy", client.LastImportMode);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_WithAmbiguousCandidate_SkipsImport()
    {
        var client = new FakeLidarrClient
        {
            Candidates =
            [
                RejectedCandidate(),
            ],
        };
        var service = CreateService(client, EnabledImportOptions());

        var result = await service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");

        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(0, result.SafeCandidateCount);
        Assert.Equal(["01 Track.flac"], result.RejectedFilenames);
        Assert.Equal("Lidarr candidates had rejections or ambiguous matches", result.SkippedReason);
        Assert.Empty(client.ImportedFiles);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_MapsOnlyPathBoundaryMatches()
    {
        var client = new FakeLidarrClient();
        var service = CreateService(
            client,
            new Options.IntegrationOptions.LidarrOptions
            {
                Enabled = true,
                Url = "http://lidarr.test",
                ApiKey = "key",
                AutoImportCompleted = true,
                ImportPathFrom = "/downloads/music",
                ImportPathTo = "/lidarr/inbox",
            });

        await service.ImportCompletedDirectoryAsync("/downloads/music2/Artist/Album");

        Assert.Equal(Path.GetFullPath("/downloads/music2/Artist/Album"), client.LastCandidateFolder);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_MapsChildPath()
    {
        var client = new FakeLidarrClient();
        var service = CreateService(
            client,
            new Options.IntegrationOptions.LidarrOptions
            {
                Enabled = true,
                Url = "http://lidarr.test",
                ApiKey = "key",
                AutoImportCompleted = true,
                ImportPathFrom = "/downloads/music",
                ImportPathTo = "/lidarr/inbox",
            });

        await service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");

        Assert.Equal("/lidarr/inbox/Artist/Album", client.LastCandidateFolder);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_MapsToWindowsPathWhenRunningOnUnix()
    {
        var client = new FakeLidarrClient();
        var service = CreateService(
            client,
            new Options.IntegrationOptions.LidarrOptions
            {
                Enabled = true,
                Url = "http://lidarr.test",
                ApiKey = "key",
                AutoImportCompleted = true,
                ImportPathFrom = "/music/downloaded",
                ImportPathTo = @"D:\downloaded",
            });

        await service.ImportCompletedDirectoryAsync("/music/downloaded/2 Chainz - T.R.U. REALigion (Anniversary Edition)");

        Assert.Equal(
            @"D:\downloaded\2 Chainz - T.R.U. REALigion (Anniversary Edition)",
            client.LastCandidateFolder);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_DebouncesConcurrentDirectoryAttempts()
    {
        var client = new FakeLidarrClient
        {
            Candidates = [SafeCandidate()],
            CandidateDelay = TimeSpan.FromMilliseconds(50),
        };
        var service = CreateService(client, EnabledImportOptions());

        var first = service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");
        var second = service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");

        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, client.CandidateRequestCount);
        Assert.Single(results, result => result.CommandId == 42);
        Assert.Single(results, result => result.SkippedReason == "Recently processed");
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_DebouncesFailedDirectoryAttempt()
    {
        var client = new FakeLidarrClient
        {
            CandidateException = new TimeoutException("lidarr slow"),
        };
        var service = CreateService(client, EnabledImportOptions());

        await Assert.ThrowsAsync<TimeoutException>(() => service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album"));
        var result = await service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album");

        Assert.Equal(1, client.CandidateRequestCount);
        Assert.Equal("Recently processed", result.SkippedReason);
    }

    [Fact]
    public async Task ImportCompletedDirectoryAsync_SerializesDifferentDirectoryImports()
    {
        var client = new FakeLidarrClient
        {
            Candidates = [SafeCandidate()],
            CandidateDelay = TimeSpan.FromMilliseconds(50),
        };
        var service = CreateService(client, EnabledImportOptions());

        await Task.WhenAll(
            service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album One"),
            service.ImportCompletedDirectoryAsync("/downloads/music/Artist/Album Two"));

        Assert.Equal(2, client.CandidateRequestCount);
        Assert.Equal(1, client.MaxConcurrentCandidateRequests);
    }

    [Fact]
    public void IsExpectedExternalHttpFailure_ReturnsTrue_ForHttpRequestException()
    {
        var ex = new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error).");

        Assert.True(LidarrImportService.IsExpectedExternalHttpFailure(ex));
    }

    [Fact]
    public void IsExpectedExternalHttpFailure_ReturnsTrue_ForWrappedHttpRequestException()
    {
        var ex = new InvalidOperationException(
            "wrapped",
            new HttpRequestException("Connection refused"));

        Assert.True(LidarrImportService.IsExpectedExternalHttpFailure(ex));
    }

    private static LidarrImportService CreateService(FakeLidarrClient client, Options.IntegrationOptions.LidarrOptions lidarrOptions)
        => new(
            client,
            new EventBus(null!),
            new TestOptionsMonitor<Options>(new Options
            {
                Integration = new Options.IntegrationOptions
                {
                    Lidarr = lidarrOptions,
                },
            }));

    private static Options.IntegrationOptions.LidarrOptions EnabledImportOptions()
        => new()
        {
            Enabled = true,
            Url = "http://lidarr.test",
            ApiKey = "key",
            AutoImportCompleted = true,
        };

    private static LidarrManualImportResource SafeCandidate()
        => new()
        {
            Id = 123,
            Path = "/downloads/music/Artist/Album/01 Track.flac",
            Artist = new LidarrArtistResource { Id = 1, ArtistName = "Artist" },
            Album = new LidarrAlbumResource { Id = 2, Title = "Album" },
            AlbumReleaseId = 3,
            Tracks = [new LidarrTrackResource { Id = 4, Title = "Track" }],
            Quality = JsonSerializer.Deserialize<JsonElement>("{}"),
        };

    private static LidarrManualImportResource RejectedCandidate()
        => new()
        {
            Id = 123,
            Path = "/downloads/music/Artist/Album/01 Track.flac",
            Artist = new LidarrArtistResource { Id = 1, ArtistName = "Artist" },
            Album = new LidarrAlbumResource { Id = 2, Title = "Album" },
            AlbumReleaseId = 3,
            Tracks = [new LidarrTrackResource { Id = 4, Title = "Track" }],
            Quality = JsonSerializer.Deserialize<JsonElement>("{}"),
            Rejections = [JsonSerializer.Deserialize<JsonElement>("{\"reason\":\"ambiguous\"}")],
        };

    private sealed class FakeLidarrClient : ILidarrClient
    {
        public IReadOnlyList<LidarrManualImportResource> Candidates { get; init; } = [];

        public TimeSpan CandidateDelay { get; init; } = TimeSpan.Zero;

        public Exception? CandidateException { get; init; }

        public int CandidateRequestCount { get; private set; }

        public int MaxConcurrentCandidateRequests { get; private set; }

        private int _activeCandidateRequests;

        public string LastCandidateFolder { get; private set; } = string.Empty;

        public string LastImportMode { get; private set; } = string.Empty;

        public List<LidarrManualImportResource> ImportedFiles { get; } = [];

        public Task<LidarrSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrSystemStatus());

        public Task<IReadOnlyList<LidarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LidarrQualityProfile>>([]);

        public Task<IReadOnlyList<LidarrTrackResource>> GetAlbumTracksAsync(int albumId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LidarrTrackResource>>([]);

        public Task<IReadOnlyList<LidarrWantedAlbum>> GetWantedMissingAsync(int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LidarrWantedAlbum>>([]);

        public Task<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)> GetWantedMissingPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)>(([], 0));

        public Task<IReadOnlyList<LidarrManualImportResource>> GetManualImportCandidatesAsync(
            string folder,
            bool filterExistingFiles,
            bool replaceExistingFiles,
            CancellationToken cancellationToken = default)
        {
            var activeRequests = Interlocked.Increment(ref _activeCandidateRequests);
            try
            {
                CandidateRequestCount++;
                MaxConcurrentCandidateRequests = Math.Max(MaxConcurrentCandidateRequests, activeRequests);
                LastCandidateFolder = folder;
                if (CandidateDelay > TimeSpan.Zero)
                {
                    return GetManualImportCandidatesWithDelayAsync(cancellationToken);
                }

                if (CandidateException is not null)
                {
                    return Task.FromException<IReadOnlyList<LidarrManualImportResource>>(CandidateException);
                }

                return Task.FromResult(Candidates);
            }
            finally
            {
                if (CandidateDelay == TimeSpan.Zero)
                {
                    Interlocked.Decrement(ref _activeCandidateRequests);
                }
            }
        }

        private async Task<IReadOnlyList<LidarrManualImportResource>> GetManualImportCandidatesWithDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(CandidateDelay, cancellationToken);
                if (CandidateException is not null)
                {
                    throw CandidateException;
                }

                return Candidates;
            }
            finally
            {
                Interlocked.Decrement(ref _activeCandidateRequests);
            }
        }

        public Task<LidarrCommandResponse> StartManualImportAsync(
            IReadOnlyList<LidarrManualImportResource> files,
            string importMode,
            bool replaceExistingFiles,
            CancellationToken cancellationToken = default)
        {
            LastImportMode = importMode;
            ImportedFiles.AddRange(files);
            return Task.FromResult(new LidarrCommandResponse { Id = 42, Name = "ManualImport", Status = "queued" });
        }

        public Task<LidarrCommandResponse> StartCommandAsync(string name, object payload, CancellationToken cancellationToken = default)
            => Task.FromResult(new LidarrCommandResponse { Id = 42, Name = name, Status = "queued" });
    }
}
