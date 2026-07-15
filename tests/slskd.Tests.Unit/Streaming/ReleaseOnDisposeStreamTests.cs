// <copyright file="ReleaseOnDisposeStreamTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System;
using System.IO;
using System.Threading.Tasks;
using slskd.Streaming;
using Xunit;

public class ReleaseOnDisposeStreamTests
{
    [Fact]
    public void Constructor_NullInner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseOnDisposeStream(null!, () => { }));
    }

    [Fact]
    public void Constructor_NullOnDispose_Throws()
    {
        using var ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseOnDisposeStream(ms, null!));
    }

    [Fact]
    public void Dispose_InvokesOnDispose()
    {
        var invoked = false;
        using (var inner = new MemoryStream(new byte[] { 1, 2, 3 }))
        using (var wrapped = new ReleaseOnDisposeStream(inner, () => invoked = true))
        {
            Assert.False(invoked);
            _ = wrapped.ReadByte();
        }
        Assert.True(invoked);
    }

    [Fact]
    public void Dispose_DisposesInner()
    {
        using var inner = new MemoryStream(new byte[] { 1 });
        using var wrapped = new ReleaseOnDisposeStream(inner, () => { });
        wrapped.Dispose();
        Assert.Throws<ObjectDisposedException>(() => inner.ReadByte());
    }

    [Fact]
    public void DoubleDispose_InvokesOnDisposeOnce()
    {
        var count = 0;
        using var inner = new MemoryStream(new byte[] { 1 });
        using var wrapped = new ReleaseOnDisposeStream(inner, () => count++);
        wrapped.Dispose();
        wrapped.Dispose();
        Assert.Equal(1, count);
    }

    [Fact]
    public void Read_DelegatesToInner()
    {
        var buf = new byte[] { 10, 20, 30 };
        using var inner = new MemoryStream(buf);
        using var wrapped = new ReleaseOnDisposeStream(inner, () => { });
        var outBuf = new byte[3];
        var n = wrapped.Read(outBuf, 0, 3);
        Assert.Equal(3, n);
        Assert.Equal(10, outBuf[0]);
        Assert.Equal(20, outBuf[1]);
        Assert.Equal(30, outBuf[2]);
    }

    [Fact]
    public async Task ReadAsync_Memory_DelegatesToInner()
    {
        using var inner = new MemoryStream(new byte[] { 10, 20, 30 });
        using var wrapped = new ReleaseOnDisposeStream(inner, () => { });
        var outBuf = new byte[3];
        var n = await wrapped.ReadAsync(outBuf.AsMemory());
        Assert.Equal(3, n);
        Assert.Equal(new byte[] { 10, 20, 30 }, outBuf);
    }

    [Fact]
    public async Task ReadAsync_ByteArray_DelegatesToInner()
    {
        using var inner = new MemoryStream(new byte[] { 1, 2 });
        using var wrapped = new ReleaseOnDisposeStream(inner, () => { });
        var outBuf = new byte[2];
        var n = await wrapped.ReadAsync(outBuf, 0, 2);
        Assert.Equal(2, n);
        Assert.Equal(new byte[] { 1, 2 }, outBuf);
    }

    [Fact]
    public async Task CopyToAsync_CopiesFullContentFromInner()
    {
        var payload = new byte[] { 5, 6, 7, 8, 9 };
        using var inner = new MemoryStream(payload);
        using var wrapped = new ReleaseOnDisposeStream(inner, () => { });
        using var dest = new MemoryStream();
        await wrapped.CopyToAsync(dest);
        Assert.Equal(payload, dest.ToArray());
    }

    [Fact]
    public async Task ReadAsync_PipeBacked_CompletesWithoutBlockingOnProducer()
    {
        // A pipe reader stream only yields data once the async producer writes it.
        // Forwarding ReadAsync to the inner stream must observe the produced bytes
        // rather than blocking a pooled thread on a synchronous Read.
        var pipe = new System.IO.Pipelines.Pipe();
        using var wrapped = new ReleaseOnDisposeStream(pipe.Reader.AsStream(), () => { });

        var readTask = wrapped.ReadAsync(new byte[4].AsMemory()).AsTask();
        Assert.False(readTask.IsCompleted);

        await pipe.Writer.WriteAsync(new byte[] { 42 });
        await pipe.Writer.CompleteAsync();

        var n = await readTask;
        Assert.Equal(1, n);
    }

    [Fact]
    public async Task DisposeAsync_InvokesOnDisposeOnce()
    {
        var count = 0;
        using var inner = new MemoryStream(new byte[] { 1, 2, 3 });
        using var wrapped = new ReleaseOnDisposeStream(inner, () => count++);

        await wrapped.DisposeAsync();
        await wrapped.DisposeAsync();

        Assert.Equal(1, count);
    }
}
