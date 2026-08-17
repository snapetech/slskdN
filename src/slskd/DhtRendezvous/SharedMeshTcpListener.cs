// <copyright file="SharedMeshTcpListener.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using slskd.SoulseekRuntime;

/// <summary>
/// Owns the public Soulseek peer-listen TCP socket whenever DHT rendezvous is enabled (see
/// <see cref="ShouldRun(DhtRendezvousOptions)"/>), and demultiplexes each accepted connection
/// between the Soulseek peer protocol (fed to <see cref="FedTcpListener"/>, consumed by the
/// vendored Soulseek client's own listener pipeline -- including its existing
/// plain-vs-obfuscated sniffing) and the mesh TCP overlay handshake (always TLS-wrapped; handed
/// to <see cref="IMeshOverlayServer"/>), based on the first bytes read from the socket. There is
/// no separate "don't share" mode: whenever the mesh overlay needs a TCP port at all, this is how
/// it gets one, for every installation.
/// </summary>
/// <remarks>
/// This mirrors two things already in this codebase: <see cref="SharedMeshUdpListener"/>, which
/// demultiplexes DHT, mesh overlay control, and QUIC on one public UDP socket, and the vendored
/// Soulseek listener's own per-connection sniff between plain and type-1 obfuscated peer
/// connections on one TCP socket. The technique -- accept on a single socket, peek the first
/// bytes, dispatch -- is the standard way multiple protocols share one TCP port (see e.g. sslh for
/// SSH/TLS/OpenVPN on port 443); the OS has no mechanism to route by content before a connection
/// is accepted.
///
/// Classification uses <see cref="SocketFlags.Peek"/>, which reads without consuming, so the
/// classified connection is handed downstream completely untouched -- the receiving component
/// reads its own protocol's bytes from the very start, exactly as if it had accepted the
/// connection directly.
///
/// This bridges two components with different lifecycles: the vendored Soulseek client's own
/// connect/reconnect/reconfigure lifecycle (which constructs a new listener around the shared
/// <see cref="FedTcpListener"/> on each of those events) and <see cref="IMeshOverlayServer"/>'s
/// beacon-capability-driven start/stop. <see cref="MeshOverlayServer"/> no longer has an
/// independent bind path -- it always waits for connections fed through here.
/// </remarks>
internal sealed class SharedMeshTcpListener : BackgroundService
{
    private const byte TlsHandshakeContentType = 0x16;
    private const byte TlsMajorVersionByte = 0x03;
    private const int MaxClassificationAttempts = 5;
    private static readonly TimeSpan ClassificationRetryDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan ClassificationTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<SharedMeshTcpListener> _logger;
    private readonly OptionsAtStartup _optionsAtStartup;
    private readonly DhtRendezvousOptions _dhtOptions;
    private readonly FedTcpListener _fedTcpListener;
    private readonly IMeshOverlayServer _meshOverlayServer;
    private volatile IPEndPoint? _localEndPoint;

    /// <summary>The bound local endpoint once <see cref="ExecuteAsync"/> has started listening, for diagnostics and tests.</summary>
    internal IPEndPoint? LocalEndPoint => _localEndPoint;

    public SharedMeshTcpListener(
        ILogger<SharedMeshTcpListener> logger,
        OptionsAtStartup optionsAtStartup,
        DhtRendezvousOptions dhtOptions,
        FedTcpListener fedTcpListener,
        IMeshOverlayServer meshOverlayServer)
    {
        _logger = logger;
        _optionsAtStartup = optionsAtStartup;
        _dhtOptions = dhtOptions;
        _fedTcpListener = fedTcpListener;
        _meshOverlayServer = meshOverlayServer;
    }

    /// <summary>
    /// True when this component -- rather than the vendored Soulseek listener binding its own
    /// socket -- should own the public TCP port. Unconditional whenever the mesh TCP overlay
    /// handshake runs at all: there is no separate "don't share" mode, so every installation with
    /// DHT rendezvous enabled gets the same single code path.
    /// </summary>
    internal static bool ShouldRun(DhtRendezvousOptions dhtOptions)
    {
        return dhtOptions.Enabled;
    }

    /// <summary>
    /// Overload additionally gated on the top-level DHT feature flag. Single source of truth for
    /// both the DI registration (<c>CapabilitiesAndRendezvousServiceCollectionExtensions</c>,
    /// which must only register <see cref="SoulseekRuntime.FedTcpListener"/> and this hosted
    /// service when true) and the <c>ISoulseekClient</c> factory (which must only resolve
    /// <see cref="SoulseekRuntime.FedTcpListener"/> -- registered conditionally -- when true).
    /// </summary>
    internal static bool ShouldRun(slskd.Options options)
    {
        return options.Feature.Dht && ShouldRun(options.DhtRendezvous);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "listener.Stop() in the finally block releases the underlying socket; it is not left dangling.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await).
        await Task.Yield();

        if (!ShouldRun(_dhtOptions))
        {
            _logger.LogDebug("[SharedMeshTcpListener] Disabled by configuration");
            return;
        }

        if (!IPAddress.TryParse(_optionsAtStartup.Soulseek.ListenIpAddress, out var listenAddress))
        {
            listenAddress = IPAddress.Any;
        }

        var listenPort = _optionsAtStartup.Soulseek.ListenPort;
        var listener = new TcpListener(listenAddress, listenPort);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        try
        {
            listener.Start();
            _localEndPoint = (IPEndPoint)listener.LocalEndpoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharedMeshTcpListener] Failed to bind shared TCP port {Address}:{Port}; Soulseek peer connections and the mesh TCP overlay will both be unavailable. Set dht.enabled: false to disable DHT rendezvous if this port cannot be used.",
                listenAddress,
                listenPort);
            return;
        }

        _logger.LogInformation(
            "[SharedMeshTcpListener] Sharing {Address}:{Port} between Soulseek peer connections (plain and obfuscated) and the mesh TCP overlay",
            listenAddress,
            _localEndPoint!.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    _logger.LogWarning(ex, "[SharedMeshTcpListener] Error accepting connection");
                    continue;
                }

                _ = RouteConnectionAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
            _localEndPoint = null;
            _fedTcpListener.Complete();
            _logger.LogInformation("[SharedMeshTcpListener] Stopped");
        }
    }

    private async Task RouteConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            var kind = await ClassifyConnectionAsync(client.Client, cancellationToken).ConfigureAwait(false);

            switch (kind)
            {
                case ConnectionKind.MeshOverlay:
                    await _meshOverlayServer.HandleExternallyAcceptedConnectionAsync(client, cancellationToken).ConfigureAwait(false);
                    break;
                case ConnectionKind.Soulseek:
                    _fedTcpListener.Feed(client);
                    break;
                default:
                    client.Dispose();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[SharedMeshTcpListener] Error routing an accepted connection");
            client.Dispose();
        }
    }

    /// <summary>
    /// Peeks (without consuming) the first two bytes of an accepted connection to distinguish a
    /// TLS ClientHello (the mesh overlay handshake always speaks TLS from the first byte) from
    /// Soulseek peer framing (a small length-prefix integer, or -- if obfuscated -- effectively
    /// random bytes; the vendored listener disambiguates those two on its own). Any parsing
    /// ambiguity, timeout, or error returns <see cref="ConnectionKind.Unknown"/> rather than
    /// guessing; both real classifications land in the same trust tier (an arbitrary peer talking
    /// to our public P2P port), so a wrong guess only costs a failed handshake on the misrouted
    /// side, never a boundary crossing.
    /// </summary>
    internal static async Task<ConnectionKind> ClassifyConnectionAsync(Socket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[2];

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ClassificationTimeout);

        try
        {
            for (var attempt = 0; attempt < MaxClassificationAttempts; attempt++)
            {
                // SocketFlags.Peek never advances the socket's read pointer, so every attempt
                // re-reads from the same starting position -- this is not a byte offset to
                // accumulate across attempts, it's a retry of the same read.
                var peeked = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.Peek, cts.Token).ConfigureAwait(false);

                if (peeked >= buffer.Length)
                {
                    return buffer[0] == TlsHandshakeContentType && buffer[1] == TlsMajorVersionByte
                        ? ConnectionKind.MeshOverlay
                        : ConnectionKind.Soulseek;
                }

                if (peeked == 0)
                {
                    // Graceful close before the remote sent anything.
                    return ConnectionKind.Unknown;
                }

                // Only the first byte has arrived so far (unusual TCP segmentation); briefly retry.
                await Task.Delay(ClassificationRetryDelay, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            return ConnectionKind.Unknown;
        }
        catch (SocketException)
        {
            return ConnectionKind.Unknown;
        }
        catch (ObjectDisposedException)
        {
            return ConnectionKind.Unknown;
        }

        return ConnectionKind.Unknown;
    }

    internal enum ConnectionKind
    {
        Unknown,
        Soulseek,
        MeshOverlay,
    }
}
