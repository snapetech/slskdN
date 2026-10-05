// <copyright file="MeshSearchRpcHandler.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous.Search;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Common.Security;
using slskd.DhtRendezvous.Messages;
using slskd.DhtRendezvous.Security;
using slskd.Shares;
using Soulseek;

/// <summary>
/// Handles inbound mesh_search_req on the overlay: runs local share search and returns mesh_search_resp.
/// </summary>
public interface IMeshSearchRpcHandler
{
    /// <summary>
    /// Executes a local share search and returns a mesh search response.
    /// </summary>
    /// <param name="request">The mesh search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response message to send back to the initiator.</returns>
    Task<MeshSearchResponseMessage> HandleAsync(MeshSearchRequestMessage request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles mesh_search_req by searching the local share repository and mapping results to MeshSearchResponseMessage.
/// Never returns absolute paths; uses virtual/masked paths from the repository.
/// </summary>
public sealed class MeshSearchRpcHandler : IMeshSearchRpcHandler
{
    private readonly IShareService _shareService;
    private readonly ILogger<MeshSearchRpcHandler> _logger;

    public MeshSearchRpcHandler(IShareService shareService, ILogger<MeshSearchRpcHandler> logger)
    {
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MeshSearchResponseMessage> HandleAsync(MeshSearchRequestMessage request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Query length cap (prevent abuse)
            const int MaxQueryLength = 256;
            if (request.SearchText.Length > MaxQueryLength)
            {
                return new MeshSearchResponseMessage
                {
                    RequestId = request.RequestId,
                    Files = new List<MeshSearchFileDto>(),
                    Truncated = false,
                    Error = $"Query too long: {request.SearchText.Length} > {MaxQueryLength}",
                };
            }

            // Apply the caller and five-second budget wherever the search path supports cancellation.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 second cap
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var operationToken = linkedCts.Token;

            var query = SearchQuery.FromText(request.SearchText);
            var maxResults = Math.Clamp(request.MaxResults, MessageValidator.MinMeshSearchMaxResults, MessageValidator.MaxMeshSearchMaxResults);

            operationToken.ThrowIfCancellationRequested();
            var files = await _shareService.SearchLocalAsync(query).WaitAsync(operationToken).ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();

            // Deterministic ordering by filename; take up to maxResults+1 to detect truncation
            var ordered = files
                .Select(file =>
                {
                    operationToken.ThrowIfCancellationRequested();
                    return file;
                })
                .OrderBy(f => f.Filename, StringComparer.Ordinal)
                .Take(maxResults + 1)
                .ToList();
            operationToken.ThrowIfCancellationRequested();

            var truncated = ordered.Count > maxResults;
            var toReturn = truncated ? ordered.Take(maxResults).ToList() : ordered;

            var repo = _shareService.GetLocalRepository();
            var dtos = new List<MeshSearchFileDto>(toReturn.Count);
            foreach (var f in toReturn)
            {
                operationToken.ThrowIfCancellationRequested();
                string? contentId = null;
                try
                {
                    var hasFallback = false;
                    string? fallbackContentId = null;

                    foreach (var contentItem in repo.ListContentItemsForFile(f.Filename))
                    {
                        operationToken.ThrowIfCancellationRequested();

                        if (!hasFallback)
                        {
                            fallbackContentId = contentItem.ContentId;
                            hasFallback = true;
                        }

                        if (contentItem.IsAdvertisable)
                        {
                            contentId = contentItem.ContentId;
                            break;
                        }
                    }

                    contentId ??= fallbackContentId;
                }
                catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        "Failed to look up ContentId for file {Filename}: {Exception}",
                        LoggingSanitizer.SanitizeFilePath(f.Filename),
                        LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                }

                dtos.Add(new MeshSearchFileDto
                {
                    Filename = f.Filename, // Virtual share path only; repository must not expose absolute paths
                    Size = f.Size,
                    Extension = string.IsNullOrEmpty(f.Extension) ? null : f.Extension,
                    Bitrate = f.BitRate,
                    Duration = f.Length,
                    Codec = DeriveCodec(f.Extension),
                    MediaKinds = DeriveMediaKinds(f.Extension),
                    ContentId = contentId,

                    // Hash lookup deferred: requires HashDb integration or on-demand computation
                    // See memory-bank/triage-todo-fixme.md for details
                    Hash = null,
                });
            }

            // Enforce response amplification limit
            if (dtos.Count > MessageValidator.MaxMeshSearchResponseFiles)
            {
                dtos = dtos.Take(MessageValidator.MaxMeshSearchResponseFiles).ToList();
                truncated = true;
            }

            return new MeshSearchResponseMessage
            {
                RequestId = request.RequestId,
                Files = dtos,
                Truncated = truncated,
                Error = null,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Mesh search failed for request {RequestId}: {Exception}",
                LoggingSanitizer.SanitizeExternalIdentifier(request.RequestId),
                LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
            return new MeshSearchResponseMessage
            {
                RequestId = request.RequestId,
                Files = new List<MeshSearchFileDto>(),
                Truncated = false,
                Error = "Search failed",
            };
        }
    }

    private static string? DeriveCodec(string? extension)
    {
        if (string.IsNullOrEmpty(extension)) return null;
        return extension.TrimStart('.').ToLowerInvariant() switch
        {
            "flac" => "FLAC",
            "mp3" => "MP3",
            "m4a" or "aac" => "AAC",
            "opus" => "Opus",
            "ogg" => "Vorbis",
            "wav" => "WAV",
            _ => null,
        };
    }

    private static List<string>? DeriveMediaKinds(string? extension)
    {
        if (string.IsNullOrEmpty(extension)) return null;
        var ext = extension.TrimStart('.').ToLowerInvariant();
        var kinds = new List<string>();

        // Music
        if (ext is "mp3" or "flac" or "m4a" or "aac" or "opus" or "ogg" or "wav" or "wma" or "ape" or "mka")
        {
            kinds.Add("Music");
        }

        // Video
        if (ext is "mp4" or "mkv" or "avi" or "mov" or "wmv" or "flv" or "webm" or "m4v" or "mpg" or "mpeg")
        {
            kinds.Add("Video");
        }

        // Image
        if (ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" or "svg" or "ico")
        {
            kinds.Add("Image");
        }

        return kinds.Count > 0 ? kinds : null;
    }

}
