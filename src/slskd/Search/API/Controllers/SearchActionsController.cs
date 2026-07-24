// <copyright file="SearchActionsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search.API;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd;
using slskd.Common;
using slskd.Core.Security;
using slskd.Destinations;
using slskd.Mesh;
using slskd.Search.Providers;
using slskd.Streaming;
using slskd.Transfers.Downloads;
using Search = slskd.Search;

/// <summary>
///     Action routing for search results (download/stream based on source).
/// </summary>
[Route("api/v{version:apiVersion}/searches")]
[ApiVersion("0")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
[ValidateCsrfForCookiesOnly]
public class SearchActionsController : ControllerBase
{
    private const int PodDownloadChunkBytes = 2048;
    private const long MaxPodDownloadBytes = 512L * 1024L * 1024L;

    private readonly ISearchService _searchService;
    private readonly IDownloadService _downloadService;
    private readonly IContentLocator _contentLocator;
    private readonly IMeshContentFetcher _meshContentFetcher;
    private readonly IMeshStreamTicketService _meshStreamTickets;
    private readonly IMeshDirectory _meshDirectory;
    private readonly IOptionsMonitor<slskd.Options> _optionsMonitor;
    private readonly ILogger<SearchActionsController> _logger;

    public SearchActionsController(
        ISearchService searchService,
        IDownloadService downloadService,
        IContentLocator contentLocator,
        IMeshContentFetcher meshContentFetcher,
        IMeshStreamTicketService meshStreamTickets,
        IMeshDirectory meshDirectory,
        IOptionsMonitor<slskd.Options> optionsMonitor,
        ILogger<SearchActionsController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _contentLocator = contentLocator ?? throw new ArgumentNullException(nameof(contentLocator));
        _meshContentFetcher = meshContentFetcher ?? throw new ArgumentNullException(nameof(meshContentFetcher));
        _meshStreamTickets = meshStreamTickets ?? throw new ArgumentNullException(nameof(meshStreamTickets));
        _meshDirectory = meshDirectory ?? throw new ArgumentNullException(nameof(meshDirectory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Initiates a download for a search result item, routing to pod or scene based on source.
    /// </summary>
    /// <param name="searchId">The search ID.</param>
    /// <param name="itemId">The item ID (response index or file identifier).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="destination">Optional configured completed-file destination.</param>
    /// <returns>Download result.</returns>
    [HttpPost("{searchId}/items/{itemId}/download")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DownloadItem(
        [FromRoute] Guid searchId,
        [FromRoute] string itemId,
        CancellationToken cancellationToken,
        [FromQuery] string? destination = null)
    {
        _logger.LogDebug("[SearchActions] Download request: searchId={SearchId}, itemId={ItemId}", searchId, itemId);

        var normalizedDestination = DownloadDestinationResolver.NormalizeExplicitPath(_optionsMonitor.CurrentValue, destination);
        if (!string.IsNullOrWhiteSpace(destination) && normalizedDestination == null)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "invalid_destination",
                Title = "Invalid destination",
                Detail = "Destination must be an absolute path inside the configured downloads directory or a configured destination folder"
            });
        }

        // Find the search
        var search = await _searchService.FindAsync(s => s.Id == searchId, includeResponses: true);
        if (search == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "search_not_found",
                Title = "Search not found",
                Detail = "Search not found"
            });
        }

        // Parse itemId (format: "responseIndex:fileIndex" or just response index)
        if (!TryParseItemId(itemId, out var responseIndex, out var fileIndex))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "invalid_item_id",
                Title = "Invalid item ID",
                Detail = "Item ID must be in format 'responseIndex:fileIndex' or 'responseIndex'"
            });
        }

        if (responseIndex < 0 || responseIndex >= search.Responses.Count())
        {
            return NotFound(new ProblemDetails
            {
                Type = "item_not_found",
                Title = "Item not found",
                Detail = "Search result item not found"
            });
        }

        var response = search.Responses.ElementAt(responseIndex);
        var itemParts = itemId.Split(':', StringSplitOptions.TrimEntries);
        var explicitFileIndex = itemParts.Length == 2;
        var file = fileIndex >= 0 && fileIndex < response.Files.Count
            ? response.Files.ElementAt(fileIndex)
            : explicitFileIndex ? null : response.Files.FirstOrDefault();

        if (file == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "file_not_found",
                Title = "File not found",
                Detail = "Search result file not found"
            });
        }

        // Route based on primary source
        var primarySource = response.PrimarySource ?? "scene"; // Default to scene if not set

        if (primarySource == "pod" && response.PodContentRef != null)
        {
            // Pod download - use ContentId-based download
            var contentId = !string.IsNullOrWhiteSpace(file.ContentId)
                ? file.ContentId
                : response.PodContentRef.ContentId;

            return await HandlePodDownloadAsync(contentId, file, response.Username, normalizedDestination, cancellationToken);
        }
        else if (primarySource == "scene" && response.SceneContentRef != null)
        {
            // Scene download - use existing Soulseek download pipeline
            return await HandleSceneDownloadAsync(response.SceneContentRef, file, normalizedDestination, cancellationToken);
        }
        else
        {
            return BadRequest(new ProblemDetails
            {
                Type = "invalid_source",
                Title = "Invalid source",
                Detail = "Cannot determine download source"
            });
        }
    }

    /// <summary>
    ///     Initiates a stream for a search result item (pod only).
    /// </summary>
    /// <param name="searchId">The search ID.</param>
    /// <param name="itemId">The item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream URL or error.</returns>
    [HttpPost("{searchId}/items/{itemId}/stream")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> StreamItem(
        [FromRoute] Guid searchId,
        [FromRoute] string itemId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[SearchActions] Stream request: searchId={SearchId}, itemId={ItemId}", searchId, itemId);

        // Find the search
        var search = await _searchService.FindAsync(s => s.Id == searchId, includeResponses: true);
        if (search == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "search_not_found",
                Title = "Search not found",
                Detail = "Search not found"
            });
        }

        // Parse itemId
        if (!TryParseItemId(itemId, out var responseIndex, out var fileIndex))
        {
            return BadRequest(new ProblemDetails
            {
                Type = "invalid_item_id",
                Title = "Invalid item ID",
                Detail = "Item ID must be in format 'responseIndex:fileIndex' or 'responseIndex'"
            });
        }

        if (responseIndex < 0 || responseIndex >= search.Responses.Count())
        {
            return NotFound(new ProblemDetails
            {
                Type = "item_not_found",
                Title = "Item not found",
                Detail = "Search result item not found"
            });
        }

        var response = search.Responses.ElementAt(responseIndex);
        var itemParts = itemId.Split(':', StringSplitOptions.TrimEntries);
        var explicitFileIndex = itemParts.Length == 2;
        var file = fileIndex >= 0 && fileIndex < response.Files.Count
            ? response.Files.ElementAt(fileIndex)
            : explicitFileIndex ? null : response.Files.FirstOrDefault();
        if (file == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "file_not_found",
                Title = "File not found",
                Detail = "Search result file not found"
            });
        }

        var primarySource = response.PrimarySource ?? "scene";

        if (primarySource != "pod" || response.PodContentRef == null)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "scene_streaming_not_supported",
                Title = "Scene streaming not supported",
                Detail = "Streaming is only supported for pod results. Use download endpoint for scene results."
            });
        }

        var contentId = !string.IsNullOrWhiteSpace(file.ContentId)
            ? file.ContentId
            : response.PodContentRef.ContentId;
        var local = _contentLocator.Resolve(contentId, cancellationToken);
        if (local != null)
        {
            return Ok(new
            {
                stream_url = $"/api/v0/streams/{Uri.EscapeDataString(contentId)}",
                content_id = contentId,
                source = "pod-local"
            });
        }

        var ticket = _meshStreamTickets.Create(
            new MeshStreamTicketRequest(
                contentId,
                file.Filename,
                response.Username,
                file.Size > 0 ? file.Size : null,
                file.Hash),
            "user:" + GetAuthenticatedOwnerKey(),
            TimeSpan.FromMinutes(2));

        return Ok(new
        {
            stream_url = $"/api/v0/mesh-streams/{Uri.EscapeDataString(ticket.Ticket)}",
            content_id = contentId,
            source = "mesh"
        });
    }

    private async Task<IActionResult> HandlePodDownloadAsync(
        string contentId,
        Search.File file,
        string peerId,
        string? destination,
        CancellationToken ct)
    {
        _logger.LogInformation("[SearchActions] Pod download: contentId={ContentId}, filename={Filename}, peerId={PeerId}", contentId, file.Filename, peerId);

        try
        {
            // Check if content is available locally (in our share library)
            var resolved = _contentLocator.Resolve(contentId, ct);
            if (resolved != null)
            {
                // Content is already local - return success
                _logger.LogDebug("[SearchActions] Pod content {ContentId} is already local at {Path}", contentId, resolved.AbsolutePath);
                return Ok(new
                {
                    success = true,
                    content_id = contentId,
                    source = "pod",
                    local = true,
                    path = resolved.AbsolutePath,
                    message = "Content is already available locally"
                });
            }

            // Content is not local - download from pod peers
            _logger.LogInformation("[SearchActions] Pod content {ContentId} is not local - downloading from peer {PeerId}", contentId, peerId);

            // Try to find peers that have this content (fallback if peerId from search is unavailable)
            string targetPeerId = peerId;
            if (string.IsNullOrWhiteSpace(targetPeerId))
            {
                var peers = await _meshDirectory.FindPeersByContentAsync(contentId, ct);
                var fallbackPeer = peers?
                    .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.PeerId));
                if (fallbackPeer == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Type = "pod_peer_not_found",
                        Title = "Pod peer not found",
                        Detail = "No pod peers found hosting content"
                    });
                }

                targetPeerId = fallbackPeer.PeerId;
                _logger.LogDebug("[SearchActions] Using peer {PeerId} from mesh directory lookup", targetPeerId);
            }

            if (file.Size <= 0 || file.Size > MaxPodDownloadBytes)
            {
                _logger.LogWarning("[SearchActions] Refusing pod download {ContentId} with unsupported size {Size}", contentId, file.Size);
                return BadRequest(new ProblemDetails
                {
                    Type = "pod_download_size_invalid",
                    Title = "Pod download size invalid",
                    Detail = "Pod download size is missing or too large"
                });
            }

            var completedRoot = destination ?? DownloadDestinationResolver.GetDefaultPath(_optionsMonitor.CurrentValue);
            var localFilename = file.Filename.ToLocalFilename(baseDirectory: completedRoot);
            var localDirectory = System.IO.Path.GetDirectoryName(localFilename);
            if (!string.IsNullOrEmpty(localDirectory) && !System.IO.Directory.Exists(localDirectory))
            {
                System.IO.Directory.CreateDirectory(localDirectory);
            }

            IActionResult? fetchFailure = null;
            using (var fileStream = System.IO.File.Create(localFilename))
            {
                var offset = 0L;
                while (offset < file.Size && fetchFailure == null)
                {
                    var chunkLength = (int)Math.Min(PodDownloadChunkBytes, file.Size - offset);
                    var fetchResult = await _meshContentFetcher.FetchAsync(
                        peerId: targetPeerId,
                        contentId: contentId,
                        expectedSize: chunkLength,
                        expectedHash: null,
                        offset: offset,
                        length: chunkLength,
                        cancellationToken: ct);

                    if (fetchResult.Error != null || fetchResult.Data == null || fetchResult.Size != chunkLength)
                    {
                        _logger.LogWarning("[SearchActions] Failed to fetch pod content {ContentId} from peer {PeerId}: {Error}",
                            contentId, targetPeerId, fetchResult.Error ?? "Invalid chunk response");
                        fetchResult.Data?.Dispose();
                        fetchFailure = StatusCode(502, new ProblemDetails
                        {
                            Type = "pod_fetch_failed",
                            Title = "Pod content fetch failed",
                            Detail = "Failed to fetch content from pod peer"
                        });
                        continue;
                    }

                    await fetchResult.Data.CopyToAsync(fileStream, ct);
                    fetchResult.Data.Dispose();
                    offset += chunkLength;
                }
            }

            if (fetchFailure != null)
            {
                TryDeletePartialPodDownload(localFilename);
                return fetchFailure;
            }

            _logger.LogInformation("[SearchActions] Successfully downloaded pod content {ContentId} from peer {PeerId} to {Path}",
                contentId, targetPeerId, localFilename);

            return Ok(new
            {
                success = true,
                content_id = contentId,
                source = "pod",
                local = false,
                path = localFilename,
                message = "Content downloaded from pod peer"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchActions] Pod download failed");
            return StatusCode(500, new ProblemDetails
            {
                Type = "pod_download_exception",
                Title = "Pod download exception",
                Detail = "Pod download failed"
            });
        }
    }

    private string GetAuthenticatedOwnerKey()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? _optionsMonitor.CurrentValue.Soulseek.Username ?? string.Empty;
    }

    private static void TryDeletePartialPodDownload(string localFilename)
    {
        try
        {
            System.IO.File.Delete(localFilename);
        }
        catch
        {
        }
    }

    private async Task<IActionResult> HandleSceneDownloadAsync(
        SceneContentRef sceneRef,
        Search.File file,
        string? destination,
        CancellationToken ct)
    {
        _logger.LogInformation("[SearchActions] Scene download: username={Username}, filename={Filename}",
            sceneRef.Username, sceneRef.Filename);

        try
        {
            // Use existing Soulseek download pipeline
            var files = new[]
            {
                new DownloadEnqueueRequest
                {
                    Filename = sceneRef.Filename,
                    Size = file.Size,
                    DestinationDirectory = destination,
                },
            };
            var (enqueued, failed) = await _downloadService.EnqueueAsync(sceneRef.Username, files, ct);

            if (enqueued.Count > 0)
            {
                return Ok(new
                {
                    success = true,
                    download_id = enqueued[0].Id.ToString("N"),
                    source = "scene"
                });
            }
            else if (failed.Count > 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "download_failed",
                    Title = "Download failed",
                    Detail = "Failed to enqueue scene download"
                });
            }
            else
            {
                return StatusCode(500, new ProblemDetails
                {
                    Type = "download_error",
                    Title = "Download error",
                    Detail = "Download enqueue returned no results"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchActions] Scene download failed");
            return StatusCode(500, new ProblemDetails
            {
                Type = "download_exception",
                Title = "Download exception",
                Detail = "Scene download failed"
            });
        }
    }

    private static bool TryParseItemId(string itemId, out int responseIndex, out int fileIndex)
    {
        responseIndex = -1;
        fileIndex = -1;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        var parts = itemId.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            // Just response index
            if (int.TryParse(parts[0], out responseIndex) && responseIndex >= 0)
            {
                fileIndex = 0; // Default to first file
                return true;
            }
        }
        else if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out responseIndex) &&
                int.TryParse(parts[1], out fileIndex) &&
                responseIndex >= 0 &&
                fileIndex >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
