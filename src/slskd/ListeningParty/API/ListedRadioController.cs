// <copyright file="ListedRadioController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.ListeningParty.API;

using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using slskd.Authentication;
using slskd.Core.Security;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.Streaming;
using slskd.Transfers.MultiSource.Metrics;

/// <summary>Manual local tickets pinned to the permitted host snapshot; see ADR-0014.</summary>
[ApiController]
[ApiVersion("0")]
[Route("api/v{version:apiVersion}/listed-radio")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class ListedRadioController : ControllerBase
{
    private readonly IListeningPartyService _parties;
    private readonly IMeshServiceClient _client;
    private readonly IMeshStreamTicketService _tickets;
    private readonly IStreamTicketService _localTickets;
    private readonly IFairnessGuard _fairness;
    private readonly IOptionsMonitor<global::slskd.Options> _options;

    public ListedRadioController(IListeningPartyService parties, IMeshServiceClient client, IMeshStreamTicketService tickets, IOptionsMonitor<global::slskd.Options> options, IStreamTicketService localTickets, IFairnessGuard fairness)
    {
        _parties = parties;
        _client = client;
        _tickets = tickets;
        _localTickets = localTickets;
        _fairness = fairness;
        _options = options;
    }

    [HttpPost("{partyId}/tickets")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    public async Task<IActionResult> CreateTicket(string partyId, [FromBody] ListedRadioSelection selection, CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.Feature.Streaming)
        {
            return NotFound();
        }

        var directory = await _parties.ListDirectoryAsync(cancellationToken).ConfigureAwait(false);
        var party = directory.FirstOrDefault(entry => entry.PartyId == partyId && entry.ContentId == selection.ContentId);
        if (party == null || !party.AllowMeshStreaming || party.ExpiresAtUnixMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() ||
            string.IsNullOrWhiteSpace(party.TransportUsername) || string.IsNullOrWhiteSpace(party.StreamTicket))
        {
            return NotFound("This radio snapshot is unavailable. Refresh the directory.");
        }

        if (string.Equals(party.TransportUsername, _options.CurrentValue.Soulseek.Username, StringComparison.OrdinalIgnoreCase))
        {
            var state = await _parties.GetStateByPartyIdAsync(party.PartyId, cancellationToken).ConfigureAwait(false);
            if (state is not { Listed: true, AllowMeshStreaming: true } || state.ContentId != party.ContentId)
            {
                return NotFound("This radio snapshot is no longer available.");
            }

            var localTicket = _localTickets.Create(party.ContentId, $"listening-party:{party.PartyId}", TimeSpan.FromMinutes(2));
            return Ok(new
            {
                streamUrl = $"/api/v0/listening-party/radio/{Uri.EscapeDataString(party.PartyId)}/{Uri.EscapeDataString(party.ContentId)}?ticket={Uri.EscapeDataString(localTicket)}",
                expiresInSeconds = 120,
            });
        }

        if (!_options.CurrentValue.Feature.Mesh)
        {
            return NotFound("Remote radio streaming is disabled.");
        }

        ServiceReply reply;
        try
        {
            reply = await _client.CallAsync(party.TransportUsername, new ServiceCall
            {
                ServiceName = "ListedRadio",
                Method = "Metadata",
                CorrelationId = Guid.NewGuid().ToString("N"),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new ListedRadioRequest(party.PartyId, party.ContentId, party.StreamTicket), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(503, "The radio host could not be reached.");
        }

        if (reply.StatusCode != ServiceStatusCodes.OK)
        {
            return StatusCode(503, "The radio host no longer permits this snapshot.");
        }

        ListedRadioMetadata? metadata;
        try
        {
            if (reply.Payload.Length > 4096)
            {
                return StatusCode(503, "The radio host returned invalid metadata.");
            }

            metadata = JsonSerializer.Deserialize<ListedRadioMetadata>(reply.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (metadata == null || metadata.Length <= 0)
            {
                return StatusCode(503, "The radio host returned invalid metadata.");
            }
        }
        catch (JsonException)
        {
            return StatusCode(503, "The radio host returned invalid metadata.");
        }

        var decision = await _fairness.EvaluateAsync(cancellationToken).ConfigureAwait(false);
        if (!decision.Allowed)
        {
            return StatusCode(429, new { code = "radio_fairness_limited", error = "Remote radio is limited by network fairness." });
        }

        var remainingCapabilityMilliseconds = party.ExpiresAtUnixMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var expiresInSeconds = (int)Math.Min(
            ListeningPartyService.AnnouncementTtlSeconds - 1,
            (remainingCapabilityMilliseconds / 1000) - 1);
        if (expiresInSeconds <= 0)
        {
            return NotFound("This radio snapshot expired while its ticket was being created.");
        }

        try
        {
            var ticket = _tickets.Create(new MeshStreamTicketRequest(party.ContentId, metadata.Filename, party.TransportUsername, metadata.Length, null)
            {
                Radio = new MeshRadioScope(party.PartyId, party.StreamTicket),
            }, "user:" + (User.FindFirstValue(ClaimTypes.Name) ?? string.Empty), TimeSpan.FromSeconds(expiresInSeconds));
            return Ok(new
            {
                streamUrl = $"/api/v0/mesh-streams/{Uri.EscapeDataString(ticket.Ticket)}",
                expiresInSeconds,
                contentType = ticket.ContentType,
            });
        }
        catch (ArgumentException)
        {
            return StatusCode(503, "The radio host returned invalid audio metadata.");
        }
        catch (InvalidOperationException)
        {
            return StatusCode(429, "Too many active radio tickets.");
        }
    }
}

public sealed record ListedRadioSelection(string ContentId);
