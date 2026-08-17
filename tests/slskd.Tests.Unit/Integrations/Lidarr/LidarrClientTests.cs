// <copyright file="LidarrClientTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Integrations.Lidarr;

using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using slskd.Integrations.Lidarr;
using Xunit;

public sealed class LidarrClientTests
{
    [Fact]
    public async Task StartManualImportAsync_ProjectsCandidateToLidarrCommandShape()
    {
        var handler = new CapturingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var options = new Options
        {
            Integration = new Options.IntegrationOptions
            {
                Lidarr = new Options.IntegrationOptions.LidarrOptions
                {
                    Url = "http://lidarr.test",
                    ApiKey = "test-key",
                },
            },
        };
        var client = new LidarrClient(new TestHttpClientFactory(httpClient), new TestOptionsMonitor<Options>(options));

        var command = await client.StartManualImportAsync(
        [
            new LidarrManualImportResource
            {
                Id = 10,
                Path = "/downloads/Artist/Album/01 Track.flac",
                Artist = new LidarrArtistResource { Id = 12, ArtistName = "Artist" },
                Album = new LidarrAlbumResource { Id = 34, Title = "Album" },
                AlbumReleaseId = 56,
                Tracks =
                [
                    new LidarrTrackResource { Id = 78, Title = "Track" },
                    new LidarrTrackResource { Id = 79, Title = "Track 2" },
                ],
                Quality = JsonSerializer.Deserialize<JsonElement>("{\"quality\":\"FLAC\"}"),
                IndexerFlags = 3,
                DownloadId = "download-1",
                DisableReleaseSwitching = true,
            },
        ],
        "Copy",
        replaceExistingFiles: true);

        using var document = JsonDocument.Parse(handler.LastRequestBody);
        var root = document.RootElement;
        var file = Assert.Single(root.GetProperty("files").EnumerateArray());

        Assert.Equal(42, command.Id);
        Assert.Equal("ManualImport", command.Name);
        Assert.Equal("ManualImport", root.GetProperty("name").GetString());
        Assert.Equal("Copy", root.GetProperty("importMode").GetString());
        Assert.True(root.GetProperty("replaceExistingFiles").GetBoolean());
        Assert.Equal("/downloads/Artist/Album/01 Track.flac", file.GetProperty("path").GetString());
        Assert.Equal(12, file.GetProperty("artistId").GetInt32());
        Assert.Equal(34, file.GetProperty("albumId").GetInt32());
        Assert.Equal(56, file.GetProperty("albumReleaseId").GetInt32());
        var trackIds = file.GetProperty("trackIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        Assert.Equal(2, trackIds.Length);
        Assert.Equal(78, trackIds[0]);
        Assert.Equal(79, trackIds[1]);
        Assert.Equal(3, file.GetProperty("indexerFlags").GetInt32());
        Assert.Equal("download-1", file.GetProperty("downloadId").GetString());
        Assert.True(file.GetProperty("disableReleaseSwitching").GetBoolean());
        Assert.False(file.TryGetProperty("artist", out _));
        Assert.False(file.TryGetProperty("album", out _));
        Assert.False(file.TryGetProperty("tracks", out _));
    }

    [Fact]
    public async Task GetCommandAsync_ReadsCommandStatusAndError()
    {
        var handler = new CapturingHttpMessageHandler
        {
            CommandResponse = "{\"id\":42,\"status\":\"failed\",\"errorMessage\":\"Artist not found\",\"message\":\"Import failed\"}",
        };
        using var httpClient = new HttpClient(handler);
        var options = new Options
        {
            Integration = new Options.IntegrationOptions
            {
                Lidarr = new Options.IntegrationOptions.LidarrOptions
                {
                    Url = "http://lidarr.test",
                    ApiKey = "test-key",
                },
            },
        };
        var client = new LidarrClient(new TestHttpClientFactory(httpClient), new TestOptionsMonitor<Options>(options));

        var command = await client.GetCommandAsync(42);

        Assert.Equal(42, command.Id);
        Assert.Equal("failed", command.Status);
        Assert.Equal("Artist not found", command.ErrorMessage);
        Assert.Equal("Import failed", command.Message);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public string LastRequestBody { get; private set; } = string.Empty;

        public string CommandResponse { get; init; } = "{\"id\":42,\"name\":\"ManualImport\",\"status\":\"queued\"}";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CommandResponse),
            };
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
            => _client;
    }
}
