// <copyright file="MeshStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.Mesh;
using slskd.Transfers.MultiSource.Metrics;

/// <summary>
/// Opens manual mesh preview streams without saving content locally.
/// </summary>
public sealed class MeshStreamService : IMeshStreamService
{
    private const int MaxConcurrentMeshStreamsPerOwner = 1;
    private const int MeshStreamChunkBytes = 2048;
    private const long PipePauseWriterThreshold = 512 * 1024;
    private const long PipeResumeWriterThreshold = 128 * 1024;

    private readonly IMeshStreamTicketService _tickets;
    private readonly IStreamSessionLimiter _limiter;
    private readonly IMeshDirectory _meshDirectory;
    private readonly IMeshContentFetcher _contentFetcher;
    private readonly IFairnessGuard? _fairnessGuard;
    private readonly ITrafficAccountingService? _trafficAccounting;
    private readonly ILogger<MeshStreamService> _logger;

    public MeshStreamService(
        IMeshStreamTicketService tickets,
        IStreamSessionLimiter limiter,
        IMeshDirectory meshDirectory,
        IMeshContentFetcher contentFetcher,
        ILogger<MeshStreamService> logger,
        IFairnessGuard? fairnessGuard = null,
        ITrafficAccountingService? trafficAccounting = null)
    {
        _tickets = tickets;
        _limiter = limiter;
        _meshDirectory = meshDirectory;
        _contentFetcher = contentFetcher;
        _logger = logger;
        _fairnessGuard = fairnessGuard;
        _trafficAccounting = trafficAccounting;
    }

    public async Task<MeshStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken)
    {
        var claims = _tickets.Validate(ticket);
        if (claims == null)
        {
            return null;
        }

        if (_fairnessGuard != null)
        {
            var fairness = await _fairnessGuard.EvaluateAsync(cancellationToken).ConfigureAwait(false);
            if (!fairness.Allowed)
            {
                throw new MeshStreamLimitException($"Mesh preview stream blocked by fairness policy: {fairness.Reason}");
            }
        }

        if (!_limiter.TryAcquire(claims.OwnerKey, MaxConcurrentMeshStreamsPerOwner))
        {
            throw new MeshStreamLimitException("Too many concurrent mesh preview streams.");
        }

        var pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: PipePauseWriterThreshold,
            resumeWriterThreshold: PipeResumeWriterThreshold));
#pragma warning disable CA2000 // Ownership is transferred to ReleaseOnDisposeStream, which disposes it when the HTTP response stream is disposed.
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#pragma warning restore CA2000

        _ = ProduceAsync(claims, pipe.Writer, cancellationTokenSource.Token);

        var stream = new ReleaseOnDisposeStream(
            pipe.Reader.AsStream(),
            () =>
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                _limiter.Release(claims.OwnerKey);
            });

        return new MeshStreamLease(stream, claims.ContentType, claims.OwnerKey);
    }

    private async Task ProduceAsync(MeshStreamTicket claims, PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            var peerId = await ResolvePeerIdAsync(claims, cancellationToken).ConfigureAwait(false);
            if (peerId == null)
            {
                await writer.CompleteAsync(new MeshStreamException("No fresh mesh peer is advertising this content.")).ConfigureAwait(false);
                return;
            }

            await using var output = writer.AsStream();
            using var hash = string.IsNullOrWhiteSpace(claims.ExpectedHash) ? null : IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var expectedSize = claims.ExpectedSize;
            long offset = 0;
            while (!expectedSize.HasValue || offset < expectedSize.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remaining = expectedSize.HasValue ? expectedSize.Value - offset : MeshStreamChunkBytes;
                var length = (int)Math.Min(MeshStreamChunkBytes, remaining);
                if (length <= 0)
                {
                    break;
                }

                var result = await _contentFetcher.FetchAsync(
                    peerId,
                    claims.ContentId,
                    expectedSize: length,
                    expectedHash: null,
                    offset: offset,
                    length: length,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result.Error != null || result.Data == null || !result.SizeValid)
                {
                    throw new MeshStreamException(result.Error ?? "Mesh content chunk validation failed.");
                }

                using (result.Data)
                {
                    var buffer = new byte[MeshStreamChunkBytes];
                    int read;
                    while ((read = await result.Data.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        hash?.AppendData(buffer, 0, read);
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }
                }

                offset += result.Size;
                if (_trafficAccounting != null && result.Size > 0)
                {
                    await _trafficAccounting.AddOverlayDownloadAsync(result.Size, cancellationToken).ConfigureAwait(false);
                }

                if (!expectedSize.HasValue && result.Size < MeshStreamChunkBytes)
                {
                    break;
                }
            }

            if (hash != null)
            {
                var actualHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                if (!string.Equals(actualHash, claims.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MeshStreamException("Mesh content hash validation failed.");
                }
            }

            await writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsExpectedMeshStreamFailure(ex))
        {
            _logger.LogWarning("Mesh preview stream of {ContentId} ended because the mesh peer is unavailable: {Message}", claims.ContentId, ex.Message);
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesh preview stream of {ContentId} failed: {Message}", claims.ContentId, ex.Message);
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
    }

    private async Task<string?> ResolvePeerIdAsync(MeshStreamTicket claims, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(claims.PeerId))
        {
            return claims.PeerId;
        }

        var peers = await _meshDirectory.FindPeersByContentAsync(claims.ContentId, cancellationToken).ConfigureAwait(false);
        return peers.FirstOrDefault()?.PeerId;
    }

    private static bool IsExpectedMeshStreamFailure(Exception ex)
    {
        return ex is MeshStreamException ||
            ex is TimeoutException ||
            ex is IOException ||
            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class MeshStreamLimitException : Exception
{
    public MeshStreamLimitException(string message)
        : base(message)
    {
    }
}

public sealed class MeshStreamException : Exception
{
    public MeshStreamException(string message)
        : base(message)
    {
    }
}
