// <copyright file="ListedRadioMeshService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Mesh.ServiceFabric.Services;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.ListeningParty;
using slskd.Streaming;

/// <summary>Party-scoped host reads. See ADR-0014 for transport and permission boundaries.</summary>
public sealed class ListedRadioMeshService : IMeshService
{
    public const int MaxChunkBytes = 44 * 1024;
    private readonly IListeningPartyService _parties;
    private readonly IStreamTicketService _tickets;
    private readonly IContentLocator _locator;
    private readonly IOptionsMonitor<global::slskd.Options> _options;
    private readonly ILogger<ListedRadioMeshService> _logger;

    public ListedRadioMeshService(IListeningPartyService parties, IStreamTicketService tickets, IContentLocator locator, IOptionsMonitor<global::slskd.Options> options, ILogger<ListedRadioMeshService> logger)
    {
        _parties = parties;
        _tickets = tickets;
        _locator = locator;
        _options = options;
        _logger = logger;
    }

    public string ServiceName => "ListedRadio";

    public Task HandleStreamAsync(MeshServiceStream stream, MeshServiceContext context, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Use bounded radio reads.");

    public async Task<ServiceReply> HandleCallAsync(ServiceCall call, MeshServiceContext context, CancellationToken cancellationToken = default)
    {
        ServiceReply Reply(int status, string? error = null, byte[]? payload = null) => new()
        {
            CorrelationId = call.CorrelationId,
            StatusCode = status,
            ErrorMessage = error,
            Payload = payload ?? Array.Empty<byte>(),
        };

        if (!_options.CurrentValue.Feature.Mesh || !_options.CurrentValue.Feature.Streaming)
        {
            return Reply(ServiceStatusCodes.ServiceUnavailable, "Radio streaming is unavailable.");
        }

        if (call.Method is not ("Metadata" or "Read"))
        {
            return Reply(ServiceStatusCodes.MethodNotFound, "Unknown method");
        }

        var (request, error) = ServicePayloadParser.TryParseJson<ListedRadioRequest>(call, 4096);
        if (error != null)
        {
            return error;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.PartyId) || request.PartyId.Length > 512 ||
            string.IsNullOrWhiteSpace(request.ContentId) || request.ContentId.Length > 512 ||
            string.IsNullOrWhiteSpace(request.Ticket) || request.Ticket.Length > 512 ||
            request.Offset < 0 || request.Length < 0 || request.Length > MaxChunkBytes ||
            (call.Method == "Read" && request.Length == 0))
        {
            return Reply(ServiceStatusCodes.InvalidPayload, "Invalid radio request.");
        }

        var state = await _parties.GetStateByPartyIdAsync(request.PartyId, cancellationToken).ConfigureAwait(false);
        if (state is not { Listed: true, AllowMeshStreaming: true } ||
            !string.Equals(state.ContentId, request.ContentId, StringComparison.Ordinal))
        {
            return Reply(404, "Radio snapshot is no longer available.");
        }

        var claims = _tickets.Validate(request.Ticket, request.ContentId);
        if (claims == null || !string.Equals(claims.OwnerKey, $"listening-party:{request.PartyId}", StringComparison.Ordinal))
        {
            return Reply(403, "Radio capability is invalid or expired.");
        }

        try
        {
            var content = _locator.Resolve(request.ContentId, cancellationToken);
            if (content == null)
            {
                return Reply(404, "Radio content is unavailable.");
            }

            if (call.Method == "Metadata")
            {
                return Reply(ServiceStatusCodes.OK, payload: JsonSerializer.SerializeToUtf8Bytes(
                    new ListedRadioMetadata(Path.GetFileName(content.AbsolutePath), content.Length),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            }

            await using var file = new FileStream(content.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (request.Offset > file.Length)
            {
                return Reply(ServiceStatusCodes.InvalidPayload, "Invalid radio range.");
            }

            file.Seek(request.Offset, SeekOrigin.Begin);
            var bytes = new byte[(int)Math.Min(request.Length, file.Length - request.Offset)];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = await file.ReadAsync(bytes.AsMemory(count), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                count += read;
            }

            if (count < bytes.Length)
            {
                Array.Resize(ref bytes, count);
            }

            return Reply(ServiceStatusCodes.OK, payload: bytes);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Listed radio content is unavailable.");
            return Reply(404, "Radio content is unavailable.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogDebug(ex, "Listed radio content cannot be read.");
            return Reply(404, "Radio content is unavailable.");
        }
    }
}

public sealed record ListedRadioRequest(
    [property: JsonPropertyName("partyId")] string PartyId,
    [property: JsonPropertyName("contentId")] string ContentId,
    [property: JsonPropertyName("ticket")] string Ticket,
    [property: JsonPropertyName("offset")] long Offset = 0,
    [property: JsonPropertyName("length")] int Length = 0);

public sealed record ListedRadioMetadata(string Filename, long Length);
