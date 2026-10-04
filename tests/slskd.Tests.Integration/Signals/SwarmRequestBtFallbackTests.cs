// <copyright file="SwarmRequestBtFallbackTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Integration.Signals;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Security;
using slskd.Signals;
using slskd.Signals.Swarm;
using slskd.Swarm;
using Xunit;

public class SwarmRequestBtFallbackTests
{
    [Fact]
    public async Task SignalBus_SendsFallbackRequestOverMeshWhenChannelIsAvailable()
    {
        using var fixture = new SignalSystemTestFixture();
        var signalBus = fixture.ServiceProvider.GetRequiredService<ISignalBus>();
        var signal = CreateRequest("peer-bob");

        await signalBus.SendAsync(signal);

        var sentSignal = Assert.Single(fixture.SentSignals);
        Assert.Equal("Swarm.RequestBtFallback", sentSignal.Type);
        Assert.Equal("peer-bob", sentSignal.ToPeerId);
        Assert.Equal("job-123", sentSignal.Body["jobId"]);
    }

    [Fact]
    public async Task RequestBtFallback_WhenJobIsUnknown_RejectsWithAcknowledgement()
    {
        using var fixture = new SignalSystemTestFixture();
        var signalBus = Assert.IsType<SignalBus>(fixture.ServiceProvider.GetRequiredService<ISignalBus>());
        fixture.JobStoreMock
            .Setup(store => store.TryGetJobAsync("job-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SwarmJob?)null);

        await signalBus.OnSignalReceivedAsync(CreateRequest(fixture.LocalPeerId), CancellationToken.None);

        var ack = Assert.Single(fixture.SentSignals);
        Assert.Equal("Swarm.RequestBtFallbackAck", ack.Type);
        Assert.Equal("peer-alice", ack.ToPeerId);
        Assert.Equal("job-123", ack.Body["jobId"]);
        Assert.Equal("variant-abc", ack.Body["variantId"]);
        Assert.False(Assert.IsType<bool>(ack.Body["accepted"]));
        Assert.Equal("unknown-job-or-variant", ack.Body["reason"]);
    }

    [Fact]
    public async Task RequestBtFallback_WhenBodyIsMalformed_SendsSafeRejectionAcknowledgement()
    {
        using var fixture = new SignalSystemTestFixture();
        var signalBus = Assert.IsType<SignalBus>(fixture.ServiceProvider.GetRequiredService<ISignalBus>());

        await signalBus.OnSignalReceivedAsync(
            CreateRequest(fixture.LocalPeerId, new Dictionary<string, object>()),
            CancellationToken.None);

        var ack = Assert.Single(fixture.SentSignals);
        Assert.Equal("Swarm.RequestBtFallbackAck", ack.Type);
        Assert.Equal(string.Empty, ack.Body["jobId"]);
        Assert.Equal(string.Empty, ack.Body["variantId"]);
        Assert.False(Assert.IsType<bool>(ack.Body["accepted"]));
        Assert.Equal("missing-job-id-or-variant-id", ack.Body["reason"]);
    }

    [Fact]
    public async Task RequestBtFallback_WhenPolicyAllows_StillRejectsUntilLifecycleExists()
    {
        using var fixture = new SignalSystemTestFixture();
        var signalBus = Assert.IsType<SignalBus>(fixture.ServiceProvider.GetRequiredService<ISignalBus>());
        fixture.JobStoreMock
            .Setup(store => store.TryGetJobAsync("job-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SwarmJob(
                "job-123",
                new SwarmFile("content-123", "0123456789abcdef0123456789abcdef01234567", 1),
                Array.Empty<SwarmSource>()));
        fixture.SecurityPolicyEngineMock
            .Setup(engine => engine.EvaluateAsync(It.IsAny<SecurityContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityDecision(true));
        fixture.BitTorrentBackendMock.Setup(backend => backend.IsSupported()).Returns(true);

        await signalBus.OnSignalReceivedAsync(CreateRequest(fixture.LocalPeerId), CancellationToken.None);

        var ack = Assert.Single(fixture.SentSignals);
        Assert.Equal("fallback-lifecycle-unavailable", ack.Body["reason"]);
        Assert.False(Assert.IsType<bool>(ack.Body["accepted"]));
        fixture.SecurityPolicyEngineMock.Verify(engine => engine.EvaluateAsync(
            It.Is<SecurityContext>(context =>
                context.PeerId == "peer-alice" &&
                context.ContentId == "content-123" &&
                context.Operation == "bt-fallback"),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.BitTorrentBackendMock.Verify(backend => backend.IsSupported(), Times.Once);
    }

    [Fact]
    public async Task RequestBtFallback_WhenPolicyDenies_DoesNotCheckTorrentBackend()
    {
        using var fixture = new SignalSystemTestFixture();
        var signalBus = Assert.IsType<SignalBus>(fixture.ServiceProvider.GetRequiredService<ISignalBus>());
        fixture.JobStoreMock
            .Setup(store => store.TryGetJobAsync("job-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SwarmJob(
                "job-123",
                new SwarmFile("content-123", "0123456789abcdef0123456789abcdef01234567", 1),
                Array.Empty<SwarmSource>()));
        fixture.SecurityPolicyEngineMock
            .Setup(engine => engine.EvaluateAsync(It.IsAny<SecurityContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityDecision(false, "policy denied"));

        await signalBus.OnSignalReceivedAsync(CreateRequest(fixture.LocalPeerId), CancellationToken.None);

        var ack = Assert.Single(fixture.SentSignals);
        Assert.Equal("security-denied", ack.Body["reason"]);
        Assert.False(Assert.IsType<bool>(ack.Body["accepted"]));
        fixture.BitTorrentBackendMock.Verify(backend => backend.IsSupported(), Times.Never);
    }

    private static Signal CreateRequest(string toPeerId, IReadOnlyDictionary<string, object>? body = null)
    {
        return new Signal(
            signalId: Guid.NewGuid().ToString("N"),
            fromPeerId: "peer-alice",
            toPeerId: toPeerId,
            sentAt: DateTimeOffset.UtcNow,
            type: "Swarm.RequestBtFallback",
            body: body ?? new Dictionary<string, object>
            {
                ["jobId"] = "job-123",
                ["variantId"] = "variant-abc",
                ["contentIdType"] = "AudioRecording",
                ["contentIdValue"] = "mb:recording:xyz",
                ["reason"] = "mesh-failures"
            },
            ttl: TimeSpan.FromMinutes(5),
            preferredChannels: new[] { SignalChannel.Mesh });
    }
}

/// <summary>
/// Test fixture for signal system integration tests.
/// </summary>
public sealed class SignalSystemTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public string LocalPeerId { get; } = "test-peer-local";
    public ConcurrentQueue<Signal> SentSignals { get; } = new();
    public Mock<ISwarmJobStore> JobStoreMock { get; } = new();
    public Mock<slskd.Security.ISecurityPolicyEngine> SecurityPolicyEngineMock { get; } = new();
    public Mock<IBitTorrentBackend> BitTorrentBackendMock { get; } = new();

    public SignalSystemTestFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSignalSystem();
        services.AddSingleton<ISwarmJobStore>(JobStoreMock.Object);
        services.AddSingleton<slskd.Security.ISecurityPolicyEngine>(SecurityPolicyEngineMock.Object);
        services.AddSingleton<IBitTorrentBackend>(BitTorrentBackendMock.Object);
        services.AddSingleton<SwarmSignalHandlers>(serviceProvider => new SwarmSignalHandlers(
            serviceProvider.GetRequiredService<ILogger<SwarmSignalHandlers>>(),
            serviceProvider.GetRequiredService<ISignalBus>(),
            serviceProvider.GetRequiredService<ISwarmJobStore>(),
            serviceProvider.GetRequiredService<slskd.Security.ISecurityPolicyEngine>(),
            serviceProvider.GetRequiredService<IBitTorrentBackend>(),
            LocalPeerId));

        ServiceProvider = services.BuildServiceProvider();
        SignalServiceExtensions.InitializeSignalSystemAsync(ServiceProvider, LocalPeerId).GetAwaiter().GetResult();

        var channelHandler = new Mock<ISignalChannelHandler>();
        channelHandler.Setup(handler => handler.CanSendTo(It.IsAny<string>())).Returns(true);
        channelHandler.Setup(handler => handler.SendAsync(It.IsAny<Signal>(), It.IsAny<CancellationToken>()))
            .Callback<Signal, CancellationToken>((signal, _) => SentSignals.Enqueue(signal))
            .Returns(Task.CompletedTask);
        channelHandler.Setup(handler => handler.StartReceivingAsync(
                It.IsAny<Func<Signal, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ServiceProvider.GetRequiredService<ISignalBus>().RegisterChannelHandler(SignalChannel.Mesh, channelHandler.Object);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
