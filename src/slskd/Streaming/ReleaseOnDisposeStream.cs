// <copyright file="ReleaseOnDisposeStream.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Wraps a stream and invokes an action when disposed. Used to release IStreamSessionLimiter when the response completes.</summary>
public sealed class ReleaseOnDisposeStream : Stream
{
    private readonly Stream _inner;
    private readonly Action _onDispose;
    private bool _disposed;

    public ReleaseOnDisposeStream(Stream inner, Action onDispose)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    public override void Flush() => _inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    // Forward async reads/writes to the inner stream. Without these overrides the base
    // Stream implementation runs the synchronous Read on a pooled thread, which for a
    // pipe- or network-backed inner stream blocks that thread for the life of the stream
    // (threadpool starvation under concurrent streaming). ASP.NET's FileStreamResult drives
    // the response through ReadAsync/CopyToAsync, so this is the hot path.
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(buffer, cancellationToken);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(buffer, cancellationToken);

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _inner.FlushAsync(cancellationToken);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _inner.CopyToAsync(destination, bufferSize, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        DisposeCore();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            await base.DisposeAsync().ConfigureAwait(false);
            return;
        }

        _disposed = true;

        try { _onDispose(); } catch { /* best-effort */ }
        await _inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void DisposeCore()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try { _onDispose(); } catch { /* best-effort */ }
        _inner.Dispose();
    }
}
