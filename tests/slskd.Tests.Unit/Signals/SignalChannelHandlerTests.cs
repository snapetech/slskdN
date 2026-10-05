// <copyright file="SignalChannelHandlerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Signals;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using slskd.Signals;
using Xunit;

public class SignalChannelHandlerTests
{
    [Fact]
    public async Task MeshSignalChannelHandler_SendAsync_CallerCancellationDoesNotLogTransportFailure()
    {
        var messages = new List<string>();
        var sender = new TestMeshMessageSender();
        using var handler = new MeshSignalChannelHandler(
            new CapturingLogger<MeshSignalChannelHandler>(messages),
            CreateOptions(),
            sender,
            "local-peer");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var signal = new Signal(
            "signal-mesh-cancel",
            "local-peer",
            "remote-peer",
            DateTimeOffset.UtcNow,
            "test",
            new Dictionary<string, object>(),
            TimeSpan.FromMinutes(1),
            new[] { SignalChannel.Mesh });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.SendAsync(signal, cancellationTokenSource.Token));

        Assert.Empty(messages);
    }

    [Fact]
    public async Task MeshSignalChannelHandler_Dispose_DetachesSenderSubscription()
    {
        var sender = new TestMeshMessageSender();
        var handler = new MeshSignalChannelHandler(
            NullLogger<MeshSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        await handler.StartReceivingAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.Equal(1, sender.SubscriptionCount);

        handler.Dispose();

        Assert.Equal(0, sender.SubscriptionCount);
    }

    [Fact]
    public async Task MeshSignalChannelHandler_StartReceivingTwice_DoesNotDuplicateDelivery()
    {
        var sender = new TestMeshMessageSender();
        var handler = new MeshSignalChannelHandler(
            NullLogger<MeshSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        var deliveries = 0;
        Task OnSignalReceived(Signal _, CancellationToken __)
        {
            deliveries++;
            return Task.CompletedTask;
        }

        await handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None);
        await handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None);

        await sender.RaiseAsync(new SlskdnSignalMessage
        {
            SignalId = "sig-1",
            FromPeerId = "remote-peer",
            ToPeerId = "local-peer",
            Type = "test",
            Body = "{}",
            SentAt = DateTimeOffset.UtcNow,
            Ttl = TimeSpan.FromMinutes(1),
        });

        Assert.Equal(1, deliveries);
    }

    [Fact]
    public async Task MeshSignalChannelHandler_EscapesRemoteEnvelopeIdentifiersBeforeLogging()
    {
        var messages = new List<string>();
        var sender = new TestMeshMessageSender();
        using var handler = new MeshSignalChannelHandler(
            new CapturingLogger<MeshSignalChannelHandler>(messages),
            CreateOptions(),
            sender,
            "local-peer");
        await handler.StartReceivingAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        await sender.RaiseAsync(new SlskdnSignalMessage
        {
            SignalId = "signal\r\ninjected",
            FromPeerId = "remote-peer",
            ToPeerId = "wrong\r\ntarget",
            Type = "test",
            Body = "{}",
            SentAt = DateTimeOffset.UtcNow,
            Ttl = TimeSpan.FromMinutes(1),
        });

        var message = Assert.Single(messages, value => value.StartsWith("Ignoring signal", StringComparison.Ordinal));
        Assert.Contains("signal\\r\\ninjected", message);
        Assert.Contains("wrong\\r\\ntarget", message);
        Assert.DoesNotContain("\r\n", message);
    }

    [Fact]
    public async Task MeshSignalChannelHandler_ConcurrentStartReceiving_DoesNotDuplicateDelivery()
    {
        var sender = new TestMeshMessageSender();
        var handler = new MeshSignalChannelHandler(
            NullLogger<MeshSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        var deliveries = 0;
        Task OnSignalReceived(Signal _, CancellationToken __)
        {
            Interlocked.Increment(ref deliveries);
            return Task.CompletedTask;
        }

        await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None)));

        await sender.RaiseAsync(new SlskdnSignalMessage
        {
            SignalId = "sig-concurrent-1",
            FromPeerId = "remote-peer",
            ToPeerId = "local-peer",
            Type = "test",
            Body = "{}",
            SentAt = DateTimeOffset.UtcNow,
            Ttl = TimeSpan.FromMinutes(1),
        });

        Assert.Equal(1, deliveries);
    }

    [Fact]
    public async Task BtExtensionSignalChannelHandler_SendAsync_CallerCancellationDoesNotLogTransportFailure()
    {
        var messages = new List<string>();
        var sender = new TestBtExtensionSender();
        using var handler = new BtExtensionSignalChannelHandler(
            new CapturingLogger<BtExtensionSignalChannelHandler>(messages),
            CreateOptions(),
            sender,
            "local-peer");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var signal = new Signal(
            "signal-bt-cancel",
            "local-peer",
            "remote-peer",
            DateTimeOffset.UtcNow,
            "test",
            new Dictionary<string, object>(),
            TimeSpan.FromMinutes(1),
            new[] { SignalChannel.BtExtension });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.SendAsync(signal, cancellationTokenSource.Token));

        Assert.Empty(messages);
    }

    [Fact]
    public async Task BtExtensionSignalChannelHandler_Dispose_DetachesSenderSubscription()
    {
        var sender = new TestBtExtensionSender();
        var handler = new BtExtensionSignalChannelHandler(
            NullLogger<BtExtensionSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        await handler.StartReceivingAsync((_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.Equal(1, sender.SubscriptionCount);

        handler.Dispose();

        Assert.Equal(0, sender.SubscriptionCount);
    }

    [Fact]
    public async Task BtExtensionSignalChannelHandler_StartReceivingTwice_DoesNotDuplicateDelivery()
    {
        var sender = new TestBtExtensionSender();
        var handler = new BtExtensionSignalChannelHandler(
            NullLogger<BtExtensionSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        var deliveries = 0;
        Task OnSignalReceived(Signal _, CancellationToken __)
        {
            deliveries++;
            return Task.CompletedTask;
        }

        await handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None);
        await handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None);

        var signal = new Signal(
            signalId: "sig-1",
            fromPeerId: "remote-peer",
            toPeerId: "local-peer",
            sentAt: DateTimeOffset.UtcNow,
            type: "test",
            body: new Dictionary<string, object>(),
            ttl: TimeSpan.FromMinutes(1),
            preferredChannels: new[] { SignalChannel.BtExtension });

        await sender.RaiseAsync(
            new SlskdnExtensionMessage
            {
                Kind = SlskdnSignalKind.SignalEnvelope,
                Payload = JsonSerializer.Serialize(signal),
            },
            "remote-peer");

        Assert.Equal(1, deliveries);
    }

    [Fact]
    public async Task BtExtensionSignalChannelHandler_EscapesRemoteEnvelopeIdentifiersBeforeLogging()
    {
        var messages = new List<string>();
        var sender = new TestBtExtensionSender();
        using var handler = new BtExtensionSignalChannelHandler(
            new CapturingLogger<BtExtensionSignalChannelHandler>(messages),
            CreateOptions(),
            sender,
            "local-peer");
        await handler.StartReceivingAsync((_, _) => Task.CompletedTask, CancellationToken.None);
        var signal = new Signal(
            signalId: "signal\r\ninjected",
            fromPeerId: "remote-peer",
            toPeerId: "wrong\r\ntarget",
            sentAt: DateTimeOffset.UtcNow,
            type: "test",
            body: new Dictionary<string, object>(),
            ttl: TimeSpan.FromMinutes(1),
            preferredChannels: new[] { SignalChannel.BtExtension });

        await sender.RaiseAsync(
            new SlskdnExtensionMessage
            {
                Kind = SlskdnSignalKind.SignalEnvelope,
                Payload = JsonSerializer.Serialize(signal),
            },
            "remote-peer");

        var message = Assert.Single(messages, value => value.StartsWith("Ignoring signal", StringComparison.Ordinal));
        Assert.Contains("signal\\r\\ninjected", message);
        Assert.Contains("wrong\\r\\ntarget", message);
        Assert.DoesNotContain("\r\n", message);
    }

    [Fact]
    public async Task BtExtensionSignalChannelHandler_ConcurrentStartReceiving_DoesNotDuplicateDelivery()
    {
        var sender = new TestBtExtensionSender();
        var handler = new BtExtensionSignalChannelHandler(
            NullLogger<BtExtensionSignalChannelHandler>.Instance,
            CreateOptions(),
            sender,
            "local-peer");

        var deliveries = 0;
        Task OnSignalReceived(Signal _, CancellationToken __)
        {
            Interlocked.Increment(ref deliveries);
            return Task.CompletedTask;
        }

        await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => handler.StartReceivingAsync(OnSignalReceived, CancellationToken.None)));

        var signal = new Signal(
            signalId: "sig-concurrent-2",
            fromPeerId: "remote-peer",
            toPeerId: "local-peer",
            sentAt: DateTimeOffset.UtcNow,
            type: "test",
            body: new Dictionary<string, object>(),
            ttl: TimeSpan.FromMinutes(1),
            preferredChannels: new[] { SignalChannel.BtExtension });

        await sender.RaiseAsync(
            new SlskdnExtensionMessage
            {
                Kind = SlskdnSignalKind.SignalEnvelope,
                Payload = JsonSerializer.Serialize(signal),
            },
            "remote-peer");

        Assert.Equal(1, deliveries);
    }

    private static IOptionsMonitor<SignalSystemOptions> CreateOptions()
    {
        var monitor = new TestOptionsMonitor<SignalSystemOptions>(new SignalSystemOptions
        {
            MeshChannel = new SignalChannelOptions
            {
                Enabled = true,
            },
            BtExtensionChannel = new SignalChannelOptions
            {
                Enabled = true,
                RequireActiveSession = false,
            },
        });

        return monitor;
    }

    private sealed class TestMeshMessageSender : IMeshMessageSender
    {
        public event Func<SlskdnSignalMessage, CancellationToken, Task>? OnSlskdnSignalReceived;
        public int SubscriptionCount => OnSlskdnSignalReceived?.GetInvocationList().Length ?? 0;

        public Task SendToPeerAsync(string peerId, object message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task RaiseAsync(SlskdnSignalMessage message, CancellationToken cancellationToken = default)
        {
            return OnSlskdnSignalReceived?.Invoke(message, cancellationToken) ?? Task.CompletedTask;
        }
    }

    private sealed class TestBtExtensionSender : IBtExtensionSender
    {
        public event Func<SlskdnExtensionMessage, string, CancellationToken, Task>? OnSlskdnExtensionMessageReceived;
        public int SubscriptionCount => OnSlskdnExtensionMessageReceived?.GetInvocationList().Length ?? 0;

        public bool HasActiveSession(string peerId) => true;

        public Task SendExtensionMessageAsync(string peerId, SlskdnExtensionMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task RaiseAsync(SlskdnExtensionMessage message, string fromPeerId, CancellationToken cancellationToken = default)
        {
            return OnSlskdnExtensionMessageReceived?.Invoke(message, fromPeerId, cancellationToken) ?? Task.CompletedTask;
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly ICollection<string> _messages;

        public CapturingLogger(ICollection<string> messages)
        {
            _messages = messages;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }
    }
}
