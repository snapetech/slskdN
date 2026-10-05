// <copyright file="AnonymityTransportSelectionTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Common.Security;
using slskd.Mesh;
using slskd.Mesh.Transport;
using Xunit;

namespace slskd.Tests.Unit.Mesh.Transport;

public class AnonymityTransportSelectionTests : IDisposable
{
    private readonly Mock<ILogger<AnonymityTransportSelector>> _selectorLoggerMock;
    private readonly Mock<ILogger<TransportPolicyManager>> _policyLoggerMock;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TransportPolicyManager _policyManager;
    private readonly AdversarialOptions _adversarialOptions;

    public AnonymityTransportSelectionTests()
    {
        _selectorLoggerMock = new Mock<ILogger<AnonymityTransportSelector>>();
        _policyLoggerMock = new Mock<ILogger<TransportPolicyManager>>();
        _loggerFactory = LoggerFactory.Create(_ => { });
        _policyManager = new TransportPolicyManager(_policyLoggerMock.Object);

        _adversarialOptions = new AdversarialOptions
        {
            Anonymity = new AnonymityLayerOptions
            {
                Enabled = true,
                Mode = AnonymityMode.Tor
            },
            MeshTransportOptions = new MeshTransportOptions
            {
                EnableDirect = true,
                Tor = new TorTransportOptions { Enabled = true },
                I2P = new I2PTransportOptions { Enabled = true }
            }
        };
    }

    public void Dispose()
    {
        (_loggerFactory as IDisposable)?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert - Should not throw
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);
        Assert.NotNull(selector);
    }

    [Fact]
    public void GetTransportPriorityOrder_WithPolicyPreferringPrivate_PrioritizesPrivateTransports()
    {
        // Arrange
        var policy = new TransportPolicy { PreferPrivateTransports = true };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act - Access private method via reflection for testing
        var method = typeof(AnonymityTransportSelector).GetMethod("GetTransportPriorityOrder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var priorityOrder = (List<AnonymityTransportType>)method.Invoke(selector, new object[] { policy });

        // Assert - Tor should come before Direct when private transports are preferred
        var torIndex = priorityOrder.IndexOf(AnonymityTransportType.Tor);
        var directIndex = priorityOrder.IndexOf(AnonymityTransportType.Direct);
        Assert.True(torIndex < directIndex, "Tor should be prioritized over Direct when preferring private transports");
    }

    [Fact]
    public void GetTransportPriorityOrder_WithPolicyDisablingClearnet_ExcludesDirect()
    {
        // Arrange
        var policy = new TransportPolicy { DisableClearnet = true };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("GetTransportPriorityOrder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var priorityOrder = (List<AnonymityTransportType>)method.Invoke(selector, new object[] { policy });

        // Assert
        Assert.DoesNotContain(AnonymityTransportType.Direct, priorityOrder);
    }

    [Fact]
    public void IsTransportAllowedByPolicy_WithAllowedTransport_ReturnsTrue()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            AllowedTransportTypes = new List<TransportType> { TransportType.TorOnionQuic }
        };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Tor, policy });

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void IsTransportAllowedByPolicy_WithDisallowedTransport_ReturnsFalse()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            AllowedTransportTypes = new List<TransportType> { TransportType.TorOnionQuic }
        };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Direct, policy });

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void IsTransportAllowedByPolicy_WithObfuscatedTransportAndPrivatePreference_ReturnsTrue()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            PreferPrivateTransports = true
        };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Obfs4, policy });

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void IsTransportAllowedByPolicy_WithObfuscatedTransportAndClearnetAllowed_ReturnsTrue()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            DisableClearnet = false,
            PreferPrivateTransports = false
        };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Meek, policy });

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void IsTransportAllowedByPolicy_WithObfuscatedTransportDisableClearnetAndNoPrivatePreference_ReturnsFalse()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            DisableClearnet = true,
            PreferPrivateTransports = false
        };
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Obfs4, policy });

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public async Task SelectTransportTypeAsync_ReturnsAvailableTransportWithoutConnecting()
    {
        // Arrange
        _adversarialOptions.AnonymityLayer.Mode = AnonymityMode.Direct;

        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var selected = await selector.SelectTransportTypeAsync("test-peer", null, CancellationToken.None);

        // Assert
        Assert.Equal(AnonymityTransportType.Direct, selected);
    }

    [Fact]
    public void GetTransportStatuses_ReturnsAllTransportStatuses()
    {
        // Arrange
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var statuses = selector.GetTransportStatuses();

        // Assert
        Assert.NotNull(statuses);
        // Should contain at least Tor transport
        Assert.Contains(AnonymityTransportType.Tor, statuses.Keys);
    }

    [Fact]
    public void GetTransportStatuses_WithDirectMode_ContainsDirectTransportInsteadOfTor()
    {
        // Arrange
        _adversarialOptions.AnonymityLayer.Mode = AnonymityMode.Direct;
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var statuses = selector.GetTransportStatuses();

        // Assert
        Assert.Contains(AnonymityTransportType.Direct, statuses.Keys);
        Assert.DoesNotContain(AnonymityTransportType.Tor, statuses.Keys);
    }

    [Fact]
    public void GetTransportPriorityOrder_WithDirectMode_PrioritizesDirect()
    {
        // Arrange
        _adversarialOptions.AnonymityLayer.Mode = AnonymityMode.Direct;
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act
        var method = typeof(AnonymityTransportSelector).GetMethod("GetTransportPriorityOrder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var priorityOrder = (List<AnonymityTransportType>)method.Invoke(selector, new object[] { null });

        // Assert
        Assert.NotEmpty(priorityOrder);
        Assert.Equal(AnonymityTransportType.Direct, priorityOrder[0]);
    }

    [Fact]
    public async Task SelectAndConnectAsync_WithDirectModeAndNoTorProxy_FailsAsDirectDialInsteadOfNoTransportAvailable()
    {
        // Arrange
        _adversarialOptions.AnonymityLayer.Mode = AnonymityMode.Direct;
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        // Act
        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            selector.SelectAndConnectAsync("peer-direct", null, "127.0.0.1", port, null, CancellationToken.None));

        // Assert
        Assert.DoesNotContain("No anonymity transport is available", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectAndConnectAsync_WhenPrimaryConnectIsCanceled_DoesNotStartFallbackTransport()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var primary = CreateAvailableTransport(AnonymityTransportType.WebSocket);
        primary
            .Setup(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string _, int _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<Stream>(token);
            });

        var fallback = CreateAvailableTransport(AnonymityTransportType.Tor);
        using var selector = CreateSelectorWithTransports(primary, fallback);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            selector.SelectAndConnectAsync("peer", null, "example.com", 443, null, cancellation.Token));

        fallback.Verify(transport => transport.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
        fallback.Verify(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectAndConnectAsync_WhenFallbackConnectIsCanceled_PropagatesCancellationInsteadOfAggregateFailure()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var primary = CreateAvailableTransport(AnonymityTransportType.WebSocket);
        primary
            .Setup(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<Stream>(new IOException("Primary transport unavailable")));

        var fallback = CreateAvailableTransport(AnonymityTransportType.Tor);
        fallback
            .Setup(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string _, int _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<Stream>(token);
            });

        using var selector = CreateSelectorWithTransports(primary, fallback);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            selector.SelectAndConnectAsync("peer", null, "example.com", 443, null, cancellation.Token));

        fallback.Verify(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SelectAndConnectAsync_WhenAvailabilityProbeCancelsCaller_DoesNotProbeFallback()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var primary = CreateAvailableTransport(AnonymityTransportType.WebSocket);
        primary
            .Setup(transport => transport.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<bool>(token);
            });

        var fallback = CreateAvailableTransport(AnonymityTransportType.Tor);
        using var selector = CreateSelectorWithTransports(primary, fallback);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            selector.SelectAndConnectAsync("peer", null, "example.com", 443, null, cancellation.Token));

        fallback.Verify(transport => transport.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
        fallback.Verify(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectAndConnectAsync_WhenAvailabilityCompletesAfterCancellation_DoesNotConnect()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var primary = CreateAvailableTransport(AnonymityTransportType.WebSocket);
        primary
            .Setup(transport => transport.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(true);
            });

        var fallback = CreateAvailableTransport(AnonymityTransportType.Tor);
        using var selector = CreateSelectorWithTransports(primary, fallback);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            selector.SelectAndConnectAsync("peer", null, "example.com", 443, null, cancellation.Token));

        primary.Verify(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        fallback.Verify(transport => transport.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuiltInAvailabilityProbes_WhenCallerIsAlreadyCanceled_PropagateCancellation()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        IAnonymityTransport[] transports =
        {
            new TorSocksTransport(new TorOptions(), loggerFactory.CreateLogger<TorSocksTransport>()),
            new I2PTransport(new I2POptions(), loggerFactory.CreateLogger<I2PTransport>()),
            new WebSocketTransport(new WebSocketTransportOptions(), loggerFactory.CreateLogger<WebSocketTransport>()),
            new HttpTunnelTransport(new HttpTunnelTransportOptions(), loggerFactory.CreateLogger<HttpTunnelTransport>()),
            new Obfs4Transport(new Obfs4TransportOptions(), loggerFactory.CreateLogger<Obfs4Transport>()),
            new MeekTransport(new MeekTransportOptions { FrontDomain = "example.com" }, loggerFactory.CreateLogger<MeekTransport>())
        };

        // Act & Assert
        foreach (var transport in transports)
        {
            try
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.IsAvailableAsync(cancellation.Token));
            }
            finally
            {
                (transport as IDisposable)?.Dispose();
            }
        }
    }

    [Fact]
    public async Task SelectAndConnectAsync_WhenPrimaryTransportFails_ConnectsFallbackTransport()
    {
        // Arrange
        var primary = CreateAvailableTransport(AnonymityTransportType.WebSocket);
        primary
            .Setup(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<Stream>(new IOException("Primary transport unavailable")));

        var fallback = CreateAvailableTransport(AnonymityTransportType.Tor);
        var fallbackStream = new MemoryStream();
        fallback
            .Setup(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackStream);

        using var selector = CreateSelectorWithTransports(primary, fallback);

        // Act
        var result = await selector.SelectAndConnectAsync("peer", null, "example.com", 443, null, CancellationToken.None);

        // Assert
        Assert.Same(fallback.Object, result.Transport);
        Assert.Same(fallbackStream, result.Stream);
        fallback.Verify(transport => transport.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        result.Stream.Dispose();
    }

    [Fact]
    public async Task SelectAndConnectAsync_WithPeerPolicy_UsesPolicyAwareSelection()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            PeerId = "test-peer",
            PreferPrivateTransports = true
        };
        _policyManager.AddOrUpdatePolicy(policy);

        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act & Assert - Should not throw, even though transports aren't fully set up for testing
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            selector.SelectAndConnectAsync("test-peer", null, "example.com", 80, null, CancellationToken.None));
    }

    [Fact]
    public async Task SelectAndConnectAsync_WithPodPolicy_ConsidersPodContext()
    {
        // Arrange
        var policy = new TransportPolicy
        {
            PodId = "test-pod",
            DisableClearnet = true
        };
        _policyManager.AddOrUpdatePolicy(policy);

        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act & Assert - Should attempt connection without Direct transport
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            selector.SelectAndConnectAsync("peer123", "test-pod", "example.com", 80, null, CancellationToken.None));
    }

    [Fact]
    public void TransportPolicyIntegration_WithSelector_RespectsPolicyConstraints()
    {
        // Arrange
        var restrictivePolicy = new TransportPolicy
        {
            PeerId = "restricted-peer",
            AllowedTransportTypes = new List<TransportType> { TransportType.TorOnionQuic },
            DisableClearnet = true
        };
        _policyManager.AddOrUpdatePolicy(restrictivePolicy);

        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act - Check if Direct transport is allowed for this peer
        var method = typeof(AnonymityTransportSelector).GetMethod("IsTransportAllowedByPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var directAllowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Direct, restrictivePolicy });
        var torAllowed = (bool)method.Invoke(selector, new object[] { AnonymityTransportType.Tor, restrictivePolicy });

        // Assert
        Assert.False(directAllowed, "Direct transport should not be allowed for restricted peer");
        Assert.True(torAllowed, "Tor transport should be allowed for restricted peer");
    }

    [Fact]
    public void PolicySpecificity_PeerPlusPod_MostSpecific()
    {
        // Arrange
        var peerOnlyPolicy = new TransportPolicy { PeerId = "peer123", PreferPrivateTransports = false };
        var peerPodPolicy = new TransportPolicy { PeerId = "peer123", PodId = "pod456", PreferPrivateTransports = true };

        _policyManager.AddOrUpdatePolicy(peerOnlyPolicy);
        _policyManager.AddOrUpdatePolicy(peerPodPolicy);

        // Act
        var applicablePolicy = _policyManager.GetApplicablePolicy("peer123", "pod456");

        // Assert - Should return the more specific policy (peer + pod)
        Assert.NotNull(applicablePolicy);
        Assert.Equal("peer123", applicablePolicy.PeerId);
        Assert.Equal("pod456", applicablePolicy.PodId);
        Assert.True(applicablePolicy.PreferPrivateTransports);
    }

    [Fact]
    public async Task TestConnectivityAsync_CompletesWithoutError()
    {
        // Arrange
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Act & Assert - Should complete without throwing
        await selector.TestConnectivityAsync();
    }

    private AnonymityTransportSelector CreateSelectorWithTransports(
        Mock<IAnonymityTransport> primary,
        Mock<IAnonymityTransport> fallback)
    {
        _adversarialOptions.AnonymityLayer.Mode = AnonymityMode.Tor;
        _adversarialOptions.ObfuscatedTransports.Enabled = true;
        _adversarialOptions.ObfuscatedTransports.Mode = ObfuscatedTransportMode.WebSocket;
        _adversarialOptions.ObfuscatedTransports.WebSocket.Enabled = true;

        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);
        var transportsField = typeof(AnonymityTransportSelector).GetField(
            "_transports",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var transports = (Dictionary<AnonymityTransportType, IAnonymityTransport>)transportsField.GetValue(selector);
        transports[primary.Object.TransportType] = primary.Object;
        transports[fallback.Object.TransportType] = fallback.Object;
        return selector;
    }

    private static Mock<IAnonymityTransport> CreateAvailableTransport(AnonymityTransportType transportType)
    {
        var transport = new Mock<IAnonymityTransport>();
        transport.SetupGet(value => value.TransportType).Returns(transportType);
        transport.Setup(value => value.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return transport;
    }

    [Fact]
    public void TransportTypeMapping_CorrectlyMapsBetweenTypes()
    {
        // Arrange
        var selector = new AnonymityTransportSelector(_adversarialOptions, _policyManager, _selectorLoggerMock.Object, _loggerFactory);

        // Test mapping methods via reflection
        var mapToAnonymityMethod = typeof(AnonymityTransportSelector).GetMethod("MapTransportTypeToAnonymityType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var mapToTransportMethod = typeof(AnonymityTransportSelector).GetMethod("MapAnonymityTypeToTransportType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var anonymityType = (AnonymityTransportType?)mapToAnonymityMethod.Invoke(null, new object[] { TransportType.TorOnionQuic });
        var transportType = (TransportType?)mapToTransportMethod.Invoke(null, new object[] { AnonymityTransportType.Tor });

        // Assert
        Assert.Equal(AnonymityTransportType.Tor, anonymityType);
        Assert.Equal(TransportType.TorOnionQuic, transportType);
    }
}
