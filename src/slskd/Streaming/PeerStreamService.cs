// <copyright file="PeerStreamService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soulseek;

/// <summary>
/// Opens manual peer preview streams without creating transfer records or local files.
/// </summary>
public sealed class PeerStreamService : IPeerStreamService
{
    private const int MaxConcurrentPeerStreamsPerOwner = 1;
    private const long PipePauseWriterThreshold = 2 * 1024 * 1024;
    private const long PipeResumeWriterThreshold = 512 * 1024;

    private readonly IPeerStreamTicketService _tickets;
    private readonly IStreamSessionLimiter _limiter;
    private readonly ISoulseekClient _client;
    private readonly ILogger<PeerStreamService> _logger;

    public PeerStreamService(
        IPeerStreamTicketService tickets,
        IStreamSessionLimiter limiter,
        ISoulseekClient client,
        ILogger<PeerStreamService> logger)
    {
        _tickets = tickets;
        _limiter = limiter;
        _client = client;
        _logger = logger;
    }

    public Task<PeerStreamLease?> OpenAsync(string ticket, CancellationToken cancellationToken)
    {
        var claims = _tickets.Validate(ticket);
        if (claims == null)
        {
            return Task.FromResult<PeerStreamLease?>(null);
        }

        if (!_limiter.TryAcquire(claims.OwnerKey, MaxConcurrentPeerStreamsPerOwner))
        {
            throw new PeerStreamLimitException("Too many concurrent peer preview streams.");
        }

        var pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: PipePauseWriterThreshold,
            resumeWriterThreshold: PipeResumeWriterThreshold));
#pragma warning disable CA2000 // Ownership is transferred to ReleaseOnDisposeStream, which disposes it when the HTTP response stream is disposed.
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#pragma warning restore CA2000

        _ = ProduceAsync(claims, pipe.Writer, cancellationTokenSource);

        var stream = new ReleaseOnDisposeStream(
            pipe.Reader.AsStream(),
            () =>
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                _limiter.Release(claims.OwnerKey);
            });

        return Task.FromResult<PeerStreamLease?>(new PeerStreamLease(stream, claims.ContentType, claims.OwnerKey));
    }

    private async Task ProduceAsync(PeerStreamTicket claims, PipeWriter writer, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await using var output = writer.AsStream();
            var options = new TransferOptions(
                progressUpdateInterval: 1000,
                progressUpdateMinimumBytes: 1024 * 1024,
                seekOutputStreamAutomatically: false,
                disposeOutputStreamOnCompletion: false);

            await _client.DownloadAsync(
                username: claims.Username,
                remoteFilename: claims.Filename,
                outputStreamFactory: () => Task.FromResult<Stream>(output),
                size: claims.Size,
                startOffset: 0,
                token: null,
                options: options,
                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);

            await writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsExpectedPeerStreamFailure(ex))
        {
            _logger.LogWarning("Peer preview stream of {Filename} from {Username} ended because the remote peer is unavailable: {Message}", claims.Filename, claims.Username, ex.Message);
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Peer preview stream of {Filename} from {Username} failed: {Message}", claims.Filename, claims.Username, ex.Message);
            await writer.CompleteAsync(ex).ConfigureAwait(false);
        }
    }

    private static bool IsExpectedPeerStreamFailure(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.All(IsExpectedPeerStreamFailure);
        }

        if (ex is TimeoutException ||
            ex is TransferRejectedException ||
            ex is ConnectionException ||
            ex is SoulseekClientException ||
            ex is IOException)
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.Contains("Connection reset by peer", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Remote connection closed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("failed to establish", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("wait timed out", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PeerStreamLimitException : Exception
{
    public PeerStreamLimitException(string message)
        : base(message)
    {
    }
}
