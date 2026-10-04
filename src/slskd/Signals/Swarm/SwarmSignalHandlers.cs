// <copyright file="SwarmSignalHandlers.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Signals.Swarm;

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using slskd.Signals;
using slskd.Swarm;
using SecurityPolicyContext = slskd.Security.SecurityContext;
using SecurityPolicyEngine = slskd.Security.ISecurityPolicyEngine;

public class StubBitTorrentBackend : IBitTorrentBackend
{
    public bool IsSupported() => false;
    public Task<string?> FetchByInfoHashOrMagnetAsync(string backendRef, string destDirectory, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}

/// <summary>
/// Signal handlers for Swarm control signals.
/// </summary>
public class SwarmSignalHandlers
{
    private readonly ILogger<SwarmSignalHandlers> logger;
    private readonly ISignalBus signalBus;
    private readonly ISwarmJobStore swarmJobStore;
    private readonly SecurityPolicyEngine securityPolicyEngine;
    private readonly IBitTorrentBackend bitTorrentBackend;
    private readonly string localPeerId;

    public SwarmSignalHandlers(
        ILogger<SwarmSignalHandlers> logger,
        ISignalBus signalBus,
        ISwarmJobStore swarmJobStore,
        SecurityPolicyEngine securityPolicyEngine,
        IBitTorrentBackend bitTorrentBackend,
        string localPeerId)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        this.swarmJobStore = swarmJobStore ?? throw new ArgumentNullException(nameof(swarmJobStore));
        this.securityPolicyEngine = securityPolicyEngine ?? throw new ArgumentNullException(nameof(securityPolicyEngine));
        this.bitTorrentBackend = bitTorrentBackend ?? throw new ArgumentNullException(nameof(bitTorrentBackend));
        this.localPeerId = localPeerId ?? throw new ArgumentNullException(nameof(localPeerId));
    }

    /// <summary>
    /// Initialize signal handlers by subscribing to relevant signal types.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await signalBus.SubscribeAsync(HandleSignalAsync, cancellationToken);
        logger.LogInformation("Swarm signal handlers initialized");
    }

    private async Task HandleSignalAsync(Signal signal, CancellationToken cancellationToken)
    {
        if (signal == null)
            return;

        // Only handle signals addressed to us
        if (signal.ToPeerId != localPeerId)
            return;

        switch (signal.Type)
        {
            case "Swarm.RequestBtFallback":
                await HandleRequestBtFallbackAsync(signal, cancellationToken);
                break;

            case "Swarm.RequestBtFallbackAck":
                await HandleRequestBtFallbackAckAsync(signal, cancellationToken);
                break;

            case "Swarm.JobCancel":
                await HandleJobCancelAsync(signal, cancellationToken);
                break;

            default:
                // Unknown signal type, ignore
                break;
        }
    }

    /// <summary>
    /// Handle Swarm.RequestBtFallback signal (receiver side).
    /// </summary>
    private async Task HandleRequestBtFallbackAsync(Signal signal, CancellationToken cancellationToken)
    {
        try
        {
            signal.Body.TryGetValue("jobId", out var jobIdObj);
            signal.Body.TryGetValue("variantId", out var variantIdObj);
            var jobId = jobIdObj?.ToString();
            var variantId = variantIdObj?.ToString();

            if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(variantId))
            {
                await SendBtFallbackAckAsync(signal, accepted: false, reason: "missing-job-id-or-variant-id", cancellationToken, null);
                return;
            }

            // Validate job exists and has this variant
            var job = await swarmJobStore.TryGetJobAsync(jobId, cancellationToken);
            if (job == null || !job.HasVariant(variantId))
            {
                await SendBtFallbackAckAsync(signal, accepted: false, reason: "unknown-job-or-variant", cancellationToken, null);
                return;
            }

            // Evaluate security / trust
            var decision = await securityPolicyEngine.EvaluateAsync(
                new SecurityPolicyContext(signal.FromPeerId, job.File.ContentId, "bt-fallback"),
                cancellationToken);

            if (!decision.Allowed)
            {
                await SendBtFallbackAckAsync(signal, accepted: false, reason: "security-denied", cancellationToken);
                return;
            }

            if (!bitTorrentBackend.IsSupported())
            {
                await SendBtFallbackAckAsync(signal, accepted: false, reason: "bt-backend-disabled", cancellationToken, null);
                return;
            }

            // The receiving half is not useful until the requesting peer can activate and cancel
            // the corresponding transfer. Reject without creating a manager in the meantime.
            await SendBtFallbackAckAsync(signal, accepted: false, reason: "fallback-lifecycle-unavailable", cancellationToken, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RequestBtFallback signal {SignalId}", signal.SignalId);
            await SendBtFallbackAckAsync(signal, accepted: false, reason: "internal-error", cancellationToken, null);
        }
    }

    /// <summary>
    /// Handle Swarm.RequestBtFallbackAck signal (sender side).
    /// </summary>
    private Task HandleRequestBtFallbackAckAsync(Signal signal, CancellationToken cancellationToken)
    {
        try
        {
            signal.Body.TryGetValue("jobId", out var jobIdObj);
            signal.Body.TryGetValue("variantId", out var variantIdObj);
            signal.Body.TryGetValue("accepted", out var acceptedObj);
            signal.Body.TryGetValue("reason", out var reasonObj);
            signal.Body.TryGetValue("btFallbackId", out var btFallbackIdObj);
            var jobId = jobIdObj?.ToString();
            var variantId = variantIdObj?.ToString();
            var accepted = acceptedObj is bool acc && acc;
            var reason = reasonObj?.ToString();
            var btFallbackId = btFallbackIdObj?.ToString();

            if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(variantId))
            {
                logger.LogWarning("Received invalid RequestBtFallbackAck signal {SignalId}", signal.SignalId);
                return Task.CompletedTask;
            }

            // Deferred: Look up pending fallback request and handle ack
            // See memory-bank/triage-todo-fixme.md (defer section) for details
            // Requires: ISwarmJobStore lookup, ISwarmCore integration for BT fallback activation
            if (accepted)
            {
                logger.LogWarning(
                    "Ignoring accepted BT fallback acknowledgement for job {JobId}, variant {VariantId}; sender lifecycle is unavailable",
                    jobId,
                    variantId);
            }
            else
            {
                logger.LogInformation("BT fallback rejected for job {JobId}, variant {VariantId}, reason: {Reason}",
                    jobId, variantId, reason);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling RequestBtFallbackAck signal {SignalId}", signal.SignalId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle Swarm.JobCancel signal.
    /// </summary>
    private Task HandleJobCancelAsync(Signal signal, CancellationToken cancellationToken)
    {
        try
        {
            signal.Body.TryGetValue("jobId", out var jobIdObj);
            var jobId = jobIdObj?.ToString();

            if (string.IsNullOrWhiteSpace(jobId))
            {
                logger.LogWarning("Received invalid JobCancel signal {SignalId}", signal.SignalId);
                return Task.CompletedTask;
            }

            // Deferred: Cancel the job in SwarmCore
            // See memory-bank/triage-todo-fixme.md (defer section) for details
            // Requires: ISwarmJobStore cancellation, ISwarmCore integration
            logger.LogInformation("Job cancellation requested for job {JobId} from peer {PeerId}", jobId, signal.FromPeerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling JobCancel signal {SignalId}", signal.SignalId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a BT fallback acknowledgment signal.
    /// </summary>
    private async Task SendBtFallbackAckAsync(
        Signal requestSignal,
        bool accepted,
        string? reason,
        CancellationToken cancellationToken,
        string? btFallbackId = null)
    {
        requestSignal.Body.TryGetValue("jobId", out var jobIdValue);
        requestSignal.Body.TryGetValue("variantId", out var variantIdValue);
        var ack = new Signal(
            signalId: Guid.NewGuid().ToString("N"),
            fromPeerId: localPeerId,
            toPeerId: requestSignal.FromPeerId,
            sentAt: DateTimeOffset.UtcNow,
            type: "Swarm.RequestBtFallbackAck",
            body: new Dictionary<string, object>
            {
                ["jobId"] = jobIdValue?.ToString() ?? string.Empty,
                ["variantId"] = variantIdValue?.ToString() ?? string.Empty,
                ["accepted"] = accepted,
                ["reason"] = reason ?? string.Empty,
                ["btFallbackId"] = btFallbackId ?? string.Empty
            },
            ttl: TimeSpan.FromMinutes(5),
            preferredChannels: new[]
            {
                SignalChannel.Mesh,
                SignalChannel.BtExtension
            });

        await signalBus.SendAsync(ack, cancellationToken);
    }
}

/// <summary>
/// Interface for BitTorrent backend operations.
/// </summary>
public interface IBitTorrentBackend
{
    bool IsSupported();

    /// <summary>
    ///     Fetches content by infohash or magnet URI. Used by the VirtualSoulfind resolver for
    ///     ContentBackendType.Torrent. Returns the path to the fetched file when complete, or null
    ///     if not supported (e.g. StubBitTorrentBackend) or when the fetch fails.
    /// </summary>
    /// <param name="backendRef">Infohash (40 or 64 hex chars) or magnet URI.</param>
    /// <param name="destDirectory">Directory to write the first/only file into.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Absolute path to the fetched file, or null if not supported or failed.</returns>
    Task<string?> FetchByInfoHashOrMagnetAsync(string backendRef, string destDirectory, CancellationToken ct = default);
}
