// <copyright file="PodsMeshService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.Transport;
using slskd.PodCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace slskd.Mesh.ServiceFabric.Services;

/// <summary>
/// Mesh service adapter for Pod/Chat functionality.
/// Wraps existing IPodService and IPodMessaging.
/// </summary>
public class PodsMeshService : IMeshService
{
    private readonly ILogger<PodsMeshService> _logger;
    private readonly IPodService _podService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly int _maxPayload;
    private const int MaxMessageBodyBytes = 4096;

    public PodsMeshService(
        ILogger<PodsMeshService> logger,
        IPodService podService,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MeshOptions>? meshOptions = null)
    {
        _logger = logger;
        _podService = podService;
        _serviceScopeFactory = serviceScopeFactory;
        _maxPayload = meshOptions?.Value?.Security?.GetEffectiveMaxPayloadSize() ?? SecurityUtils.MaxRemotePayloadSize;
    }

    public string ServiceName => "pods";

    public async Task<ServiceReply> HandleCallAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.RemotePeerId))
            {
                return new ServiceReply
                {
                    CorrelationId = call.CorrelationId,
                    StatusCode = ServiceStatusCodes.Unauthorized,
                    ErrorMessage = "Remote peer identity is required"
                };
            }

            _logger.LogDebug(
                "[PodsMeshService] Handling call: {Method} from {PeerId}",
                call.Method, context.RemotePeerId);

            return call.Method switch
            {
                "List" => await HandleListAsync(call, context, cancellationToken),
                "Get" => await HandleGetAsync(call, context, cancellationToken),
                "Join" => await HandleJoinAsync(call, context, cancellationToken),
                "Leave" => await HandleLeaveAsync(call, context, cancellationToken),
                "PostMessage" => await HandlePostMessageAsync(call, context, cancellationToken),
                "GetMessages" => await HandleGetMessagesAsync(call, context, cancellationToken),
                _ => new ServiceReply
                {
                    CorrelationId = call.CorrelationId,
                    StatusCode = ServiceStatusCodes.MethodNotFound,
                    ErrorMessage = "Unknown method"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PodsMeshService] Error handling call: {Method}", call.Method);
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.UnknownError,
                ErrorMessage = "Internal error"
            };
        }
    }

    public Task HandleStreamAsync(
        MeshServiceStream stream,
        MeshServiceContext context,
        CancellationToken cancellationToken = default)
    {
        return HandleMessageStreamAsync(stream, context, cancellationToken);
    }

    private async Task HandleMessageStreamAsync(
        MeshServiceStream stream,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.RemotePeerId))
            {
                await stream.CloseAsync(cancellationToken);
                return;
            }

            var requestPayload = await stream.ReceiveAsync(cancellationToken);
            if (requestPayload == null || requestPayload.Length == 0 || requestPayload.Length > _maxPayload)
            {
                _logger.LogWarning("[PodsMeshService] Invalid stream payload from {PeerId}", context.RemotePeerId);
                await stream.CloseAsync(cancellationToken);
                return;
            }

            var request = JsonSerializer.Deserialize<GetMessagesRequest>(requestPayload);
            var podId = request?.PodId?.Trim() ?? string.Empty;
            var channelId = request?.ChannelId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(podId) || string.IsNullOrWhiteSpace(channelId))
            {
                _logger.LogWarning("[PodsMeshService] Invalid stream request from {PeerId}: missing pod or channel ID", context.RemotePeerId);
                await stream.CloseAsync(cancellationToken);
                return;
            }

            var member = await GetMemberAsync(podId, context, cancellationToken);
            if (member == null || member.IsBanned)
            {
                await stream.CloseAsync(cancellationToken);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var podMessaging = scope.ServiceProvider.GetRequiredService<IPodMessaging>();
            var messages = await podMessaging.GetMessagesAsync(
                podId,
                channelId,
                request!.SinceTimestamp,
                cancellationToken);

            await stream.SendAsync(JsonSerializer.SerializeToUtf8Bytes(messages), cancellationToken);
            await stream.CloseAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await stream.CloseAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PodsMeshService] Stream handling failed for {PeerId}", context.RemotePeerId);
            await stream.CloseAsync(CancellationToken.None);
        }
    }

    private async Task<ServiceReply> HandleListAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var pods = await _podService.ListAsync(cancellationToken);

        var visiblePods = new List<Pod>();
        foreach (var pod in pods.Where(p => p.Visibility == PodVisibility.Listed))
        {
            var member = pod.IsPublic ? null : await GetMemberAsync(pod.PodId, context, cancellationToken);
            if (pod.IsPublic || (member != null && !member.IsBanned))
            {
                visiblePods.Add(pod);
            }
        }

        var response = JsonSerializer.Serialize(visiblePods);

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = ServiceStatusCodes.OK,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<ServiceReply> HandleGetAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var (request, err) = ServicePayloadParser.TryParseJson<GetPodRequest>(call, _maxPayload);
        if (err != null) return err;
        var podId = request?.PodId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "PodId is required"
            };
        }

        var pod = await _podService.GetPodAsync(podId, cancellationToken);
        if (pod == null)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.ServiceNotFound,
                ErrorMessage = "Pod not found"
            };
        }

        if (!pod.IsPublic)
        {
            var member = await GetMemberAsync(podId, context, cancellationToken);
            if (member == null || member.IsBanned)
            {
                return new ServiceReply
                {
                    CorrelationId = call.CorrelationId,
                    StatusCode = ServiceStatusCodes.Forbidden,
                    ErrorMessage = "Pod membership is required"
                };
            }
        }

        var response = JsonSerializer.Serialize(pod);

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = ServiceStatusCodes.OK,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<ServiceReply> HandleJoinAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var (request, err) = ServicePayloadParser.TryParseJson<JoinPodRequest>(call, _maxPayload);
        if (err != null) return err;
        var podId = request?.PodId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "PodId is required"
            };
        }

        var pod = await _podService.GetPodAsync(podId, cancellationToken);
        if (pod == null)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.ServiceNotFound,
                ErrorMessage = "Pod not found"
            };
        }

        var existing = await GetMemberAsync(podId, context, cancellationToken);
        if ((existing != null && existing.IsBanned) || (existing == null && (!pod.IsPublic || pod.RequireApproval)))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.Forbidden,
                ErrorMessage = "Pod admission is not permitted"
            };
        }

        if (existing != null)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.OK,
                Payload = JsonSerializer.SerializeToUtf8Bytes(new { Success = true })
            };
        }

        var member = new PodMember
        {
            PeerId = context.RemotePeerId?.Trim() ?? string.Empty,
            PublicKey = context.RemotePublicKey ?? string.Empty,
            Role = "member"
        };

        var success = await _podService.JoinAsync(podId, member, cancellationToken);

        var response = JsonSerializer.Serialize(new { Success = success });

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = success ? ServiceStatusCodes.OK : ServiceStatusCodes.Forbidden,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<ServiceReply> HandleLeaveAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var (request, err) = ServicePayloadParser.TryParseJson<LeavePodRequest>(call, _maxPayload);
        if (err != null) return err;
        var podId = request?.PodId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "PodId is required"
            };
        }

        var member = await GetMemberAsync(podId, context, cancellationToken);
        if (member == null || member.IsBanned)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.Forbidden,
                ErrorMessage = "Pod membership is required"
            };
        }

        var success = await _podService.LeaveAsync(podId, context.RemotePeerId?.Trim() ?? string.Empty, cancellationToken);

        var response = JsonSerializer.Serialize(new { Success = success });

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = success ? ServiceStatusCodes.OK : ServiceStatusCodes.UnknownError,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<ServiceReply> HandlePostMessageAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var (request, err) = ServicePayloadParser.TryParseJson<PostMessageRequest>(call, _maxPayload);
        if (err != null) return err;
        if (request == null)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "Invalid request"
            };
        }

        var podId = request.PodId?.Trim() ?? string.Empty;
        var channelId = request.ChannelId?.Trim() ?? string.Empty;
        var senderPeerId = context.RemotePeerId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId) || string.IsNullOrWhiteSpace(channelId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "PodId and ChannelId are required"
            };
        }

        if (string.IsNullOrWhiteSpace(senderPeerId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "Remote peer identity is required"
            };
        }

        if (Encoding.UTF8.GetByteCount(request.Body ?? string.Empty) > MaxMessageBodyBytes)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.PayloadTooLarge,
                ErrorMessage = "Message too large (max 4KB)"
            };
        }

        var message = new PodMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            PodId = podId,
            ChannelId = channelId,
            SenderPeerId = senderPeerId,
            Body = request.Body ?? string.Empty,
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Signature = request.Signature?.Trim() ?? string.Empty
        };

        var member = await GetMemberAsync(podId, context, cancellationToken);
        if (member == null || member.IsBanned)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.Forbidden,
                ErrorMessage = "Pod membership is required"
            };
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var podMessaging = scope.ServiceProvider.GetRequiredService<IPodMessaging>();
        var success = await podMessaging.SendAsync(message, cancellationToken);

        var response = JsonSerializer.Serialize(new { Success = success, MessageId = message.MessageId });

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = success ? ServiceStatusCodes.OK : ServiceStatusCodes.UnknownError,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<ServiceReply> HandleGetMessagesAsync(
        ServiceCall call,
        MeshServiceContext context,
        CancellationToken cancellationToken)
    {
        var (request, err) = ServicePayloadParser.TryParseJson<GetMessagesRequest>(call, _maxPayload);
        if (err != null) return err;
        var podId = request?.PodId?.Trim() ?? string.Empty;
        var channelId = request?.ChannelId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(podId) || string.IsNullOrWhiteSpace(channelId))
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.InvalidPayload,
                ErrorMessage = "PodId and ChannelId are required"
            };
        }

        var member = await GetMemberAsync(podId, context, cancellationToken);
        if (member == null || member.IsBanned)
        {
            return new ServiceReply
            {
                CorrelationId = call.CorrelationId,
                StatusCode = ServiceStatusCodes.Forbidden,
                ErrorMessage = "Pod membership is required"
            };
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var podMessaging = scope.ServiceProvider.GetRequiredService<IPodMessaging>();
        var messages = await podMessaging.GetMessagesAsync(
            podId,
            channelId,
            request!.SinceTimestamp,
            cancellationToken);

        var response = JsonSerializer.Serialize(messages);

        return new ServiceReply
        {
            CorrelationId = call.CorrelationId,
            StatusCode = ServiceStatusCodes.OK,
            Payload = System.Text.Encoding.UTF8.GetBytes(response)
        };
    }

    private async Task<PodMember?> GetMemberAsync(string podId, MeshServiceContext context, CancellationToken cancellationToken)
    {
        var members = await _podService.GetMembersAsync(podId, cancellationToken);
        return members.FirstOrDefault(member => string.Equals(member.PeerId, context.RemotePeerId.Trim(), StringComparison.Ordinal));
    }

}

// Request/Response DTOs
public record GetPodRequest
{
    public string PodId { get; init; } = string.Empty;
}

public record JoinPodRequest
{
    public string PodId { get; init; } = string.Empty;
    public string? Role { get; init; }
}

public record LeavePodRequest
{
    public string PodId { get; init; } = string.Empty;
}

public record PostMessageRequest
{
    public string PodId { get; init; } = string.Empty;
    public string ChannelId { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Signature { get; init; }
}

public record GetMessagesRequest
{
    public string PodId { get; init; } = string.Empty;
    public string ChannelId { get; init; } = string.Empty;
    public long? SinceTimestamp { get; init; }
}
