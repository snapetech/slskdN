// <copyright file="LidarrClient.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.Lidarr;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

public interface ILidarrClient
{
    Task<LidarrSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LidarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LidarrTrackResource>> GetAlbumTracksAsync(int albumId, CancellationToken cancellationToken = default);

    Task<LidarrAlbumDetail?> GetAlbumAsync(int albumId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LidarrWantedAlbum>> GetWantedMissingAsync(int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)> GetWantedMissingPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LidarrManualImportResource>> GetManualImportCandidatesAsync(
        string folder,
        bool filterExistingFiles,
        bool replaceExistingFiles,
        CancellationToken cancellationToken = default);

    Task<LidarrCommandResponse> StartManualImportAsync(
        IReadOnlyList<LidarrManualImportResource> files,
        string importMode,
        bool replaceExistingFiles,
        CancellationToken cancellationToken = default);

    Task<LidarrCommandResponse> StartCommandAsync(string name, object payload, CancellationToken cancellationToken = default);

    Task<LidarrCommandResponse> GetCommandAsync(int commandId, CancellationToken cancellationToken = default);
}

public sealed class LidarrClient : ILidarrClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public LidarrClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<global::slskd.Options> optionsMonitor)
    {
        HttpClientFactory = httpClientFactory;
        OptionsMonitor = optionsMonitor;
    }

    private IHttpClientFactory HttpClientFactory { get; }

    private IOptionsMonitor<global::slskd.Options> OptionsMonitor { get; }

    public async Task<LidarrSystemStatus> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/v1/system/status");
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<LidarrSystemStatus>(response, cancellationToken).ConfigureAwait(false)
            ?? new LidarrSystemStatus();
    }

    public async Task<IReadOnlyList<LidarrQualityProfile>> GetQualityProfilesAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/v1/qualityprofile");
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<List<LidarrQualityProfile>>(response, cancellationToken).ConfigureAwait(false)
            ?? [];
    }

    public async Task<IReadOnlyList<LidarrTrackResource>> GetAlbumTracksAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var relative = $"api/v1/track?albumId={albumId.ToString(CultureInfo.InvariantCulture)}";
        using var request = CreateRequest(HttpMethod.Get, relative);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<List<LidarrTrackResource>>(response, cancellationToken).ConfigureAwait(false)
            ?? [];
    }

    public async Task<LidarrAlbumDetail?> GetAlbumAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var relative = $"api/v1/album/{albumId.ToString(CultureInfo.InvariantCulture)}";
        using var request = CreateRequest(HttpMethod.Get, relative);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<LidarrAlbumDetail>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LidarrWantedAlbum>> GetWantedMissingAsync(int pageSize, CancellationToken cancellationToken = default)
    {
        var wanted = new List<LidarrWantedAlbum>();
        var page = 1;
        var safePageSize = Math.Clamp(pageSize, 1, 1000);

        while (wanted.Count < pageSize)
        {
            var remaining = Math.Min(safePageSize, pageSize - wanted.Count);
            var relative = $"api/v1/wanted/missing?page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={remaining.ToString(CultureInfo.InvariantCulture)}&includeArtist=true&monitored=true";
            using var request = CreateRequest(HttpMethod.Get, relative);
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
            var pageResult = await ReadJsonAsync<LidarrPagingResource<LidarrWantedAlbum>>(response, cancellationToken).ConfigureAwait(false);

            if (pageResult?.Records == null || pageResult.Records.Count == 0)
            {
                break;
            }

            wanted.AddRange(pageResult.Records);

            if (wanted.Count >= pageResult.TotalRecords || pageResult.Records.Count < remaining)
            {
                break;
            }

            page++;
        }

        return wanted;
    }

    public async Task<(IReadOnlyList<LidarrWantedAlbum> Records, int TotalRecords)> GetWantedMissingPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 250);
        var relative = $"api/v1/wanted/missing?page={safePage.ToString(CultureInfo.InvariantCulture)}&pageSize={safePageSize.ToString(CultureInfo.InvariantCulture)}&includeArtist=true&monitored=true";
        using var request = CreateRequest(HttpMethod.Get, relative);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        var pageResult = await ReadJsonAsync<LidarrPagingResource<LidarrWantedAlbum>>(response, cancellationToken).ConfigureAwait(false);
        return (pageResult?.Records ?? [], pageResult?.TotalRecords ?? 0);
    }

    public async Task<IReadOnlyList<LidarrManualImportResource>> GetManualImportCandidatesAsync(
        string folder,
        bool filterExistingFiles,
        bool replaceExistingFiles,
        CancellationToken cancellationToken = default)
    {
        var relative = string.Join(
            "&",
            "api/v1/manualimport?folder=" + Uri.EscapeDataString(folder),
            "filterExistingFiles=" + filterExistingFiles.ToString().ToLowerInvariant(),
            "replaceExistingFiles=" + replaceExistingFiles.ToString().ToLowerInvariant());

        using var request = CreateRequest(HttpMethod.Get, relative);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<List<LidarrManualImportResource>>(response, cancellationToken).ConfigureAwait(false)
            ?? [];
    }

    public Task<LidarrCommandResponse> StartManualImportAsync(
        IReadOnlyList<LidarrManualImportResource> files,
        string importMode,
        bool replaceExistingFiles,
        CancellationToken cancellationToken = default)
        => StartCommandAsync(
            "ManualImport",
            new
            {
                Files = files.Select(ToManualImportFile).ToList(),
                ImportMode = importMode,
                ReplaceExistingFiles = replaceExistingFiles,
            },
            cancellationToken);

    public async Task<LidarrCommandResponse> StartCommandAsync(string name, object payload, CancellationToken cancellationToken = default)
    {
        var command = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["sendUpdatesToClient"] = false,
        };

        foreach (var property in payload.GetType().GetProperties())
        {
            command[property.Name[..1].ToLowerInvariant() + property.Name[1..]] = property.GetValue(payload);
        }

        using var request = CreateRequest(HttpMethod.Post, "api/v1/command");
        request.Content = JsonContent.Create(command, options: JsonOptions);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (int.TryParse(body.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return new LidarrCommandResponse { Id = id, Name = name, Status = "queued" };
        }

        return JsonSerializer.Deserialize<LidarrCommandResponse>(body, JsonOptions)
            ?? new LidarrCommandResponse { Name = name };
    }

    public async Task<LidarrCommandResponse> GetCommandAsync(int commandId, CancellationToken cancellationToken = default)
    {
        var relative = $"api/v1/command/{commandId.ToString(CultureInfo.InvariantCulture)}";
        using var request = CreateRequest(HttpMethod.Get, relative);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<LidarrCommandResponse>(response, cancellationToken).ConfigureAwait(false)
            ?? new LidarrCommandResponse { Id = commandId };
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static LidarrManualImportFile ToManualImportFile(LidarrManualImportResource file)
        => new()
        {
            Path = file.Path!,
            ArtistId = file.Artist!.Id,
            AlbumId = file.Album!.Id,
            AlbumReleaseId = file.AlbumReleaseId,
            TrackIds = file.Tracks!.Select(track => track.Id).ToList(),
            Quality = file.Quality,
            IndexerFlags = file.IndexerFlags,
            DownloadId = file.DownloadId,
            DisableReleaseSwitching = file.DisableReleaseSwitching,
        };

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        var baseUri = new Uri(options.Url.TrimEnd('/') + "/", UriKind.Absolute);
        var request = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
        request.Headers.TryAddWithoutValidation("X-Api-Key", options.ApiKey);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = OptionsMonitor.CurrentValue.Integration.Lidarr;
        var client = HttpClientFactory.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }
}

public sealed record LidarrSystemStatus
{
    public string AppName { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;
}

public sealed record LidarrPagingResource<T>
{
    public int TotalRecords { get; init; }

    public List<T> Records { get; init; } = [];
}

public sealed record LidarrWantedAlbum
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ForeignAlbumId { get; init; } = string.Empty;

    public int ProfileId { get; init; }

    public LidarrAlbumStatistics? Statistics { get; init; }

    public LidarrArtistResource? Artist { get; init; }

    public string SearchText => string.Join(
        " ",
        new[] { Artist?.ArtistName, Title }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed record LidarrArtistResource
{
    public int Id { get; init; }

    public string ArtistName { get; init; } = string.Empty;

    public string ForeignArtistId { get; init; } = string.Empty;

    public int QualityProfileId { get; init; }
}

public sealed record LidarrAlbumStatistics
{
    public int TrackFileCount { get; init; }

    public int TrackCount { get; init; }

    public int TotalTrackCount { get; init; }
}

public sealed record LidarrQualityProfile
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public List<LidarrQualityProfileItem>? Items { get; init; }
}

public sealed record LidarrQualityProfileItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public LidarrQuality? Quality { get; init; }

    public List<LidarrQualityProfileItem>? Items { get; init; }

    public bool Allowed { get; init; }
}

public sealed record LidarrQuality
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed record LidarrCommandResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

public sealed class LidarrManualImportResource
{
    public int Id { get; init; }

    public string? Path { get; init; }

    public string? Name { get; init; }

    public LidarrArtistResource? Artist { get; init; }

    public LidarrAlbumResource? Album { get; init; }

    public int AlbumReleaseId { get; init; }

    public List<LidarrTrackResource>? Tracks { get; init; }

    public JsonElement? Quality { get; init; }

    public string? ReleaseGroup { get; init; }

    public string? DownloadId { get; init; }

    public int IndexerFlags { get; init; }

    public List<JsonElement>? Rejections { get; init; }

    public bool AdditionalFile { get; init; }

    public bool ReplaceExistingFiles { get; set; }

    public bool DisableReleaseSwitching { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }

    [JsonIgnore]
    public bool IsSafeAutomaticImportCandidate =>
        Id > 0 &&
        !string.IsNullOrWhiteSpace(Path) &&
        Artist?.Id > 0 &&
        Album?.Id > 0 &&
        AlbumReleaseId > 0 &&
        Tracks?.Count > 0 &&
        Tracks.All(track => track.Id > 0) &&
        Quality.HasValue &&
        Quality.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined &&
        AdditionalFile == false &&
        (Rejections == null || Rejections.All(r =>
        {
            var reason = r.TryGetProperty("reason", out var p) ? p.GetString() ?? string.Empty : string.Empty;
            return reason.Contains("missing tracks", StringComparison.OrdinalIgnoreCase);
        }));
}

public sealed record LidarrManualImportFile
{
    public string Path { get; init; } = string.Empty;

    public int ArtistId { get; init; }

    public int AlbumId { get; init; }

    public int AlbumReleaseId { get; init; }

    public List<int> TrackIds { get; init; } = [];

    public JsonElement? Quality { get; init; }

    public int IndexerFlags { get; init; }

    public string? DownloadId { get; init; }

    public bool DisableReleaseSwitching { get; init; }
}

public sealed record LidarrAlbumResource
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;
}

public sealed record LidarrTrackResource
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string TrackNumber { get; init; } = string.Empty;

    public int TrackFileId { get; init; }

    public bool HasFile { get; init; }

    /// <summary>
    ///     Track duration in milliseconds, as reported by Lidarr.
    /// </summary>
    public int Duration { get; init; }
}

/// <summary>
///     The full album resource returned by <c>GET api/v1/album/{id}</c>, including all known
///     releases (editions) so the monitored one can be used to validate Soulseek candidates.
/// </summary>
public sealed record LidarrAlbumDetail
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public List<LidarrAlbumRelease>? Releases { get; init; }
}

public sealed record LidarrAlbumRelease
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Disambiguation { get; init; } = string.Empty;

    public int TrackCount { get; init; }

    /// <summary>
    ///     Release duration in milliseconds, as reported by Lidarr.
    /// </summary>
    public int Duration { get; init; }

    public bool Monitored { get; init; }
}
