// <copyright file="QuicDataServer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
#pragma warning disable CA2252 // Preview features - QUIC APIs require preview features
#pragma warning disable CA1416 // Runtime IsSupported guards already gate QUIC-only code paths

namespace slskd.Mesh.Overlay;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.Transport;

/// <summary>
/// QUIC data-plane server for bulk payload transfers.
/// </summary>
public class QuicDataServer : BackgroundService
{
    private readonly ILogger<QuicDataServer> logger;
    private readonly DataOverlayOptions options;
    private readonly ConnectionThrottler connectionThrottler;
    private readonly int maxPayloadBytes;
    private readonly ConcurrentDictionary<IPEndPoint, QuicConnection> activeConnections = new();
    private readonly ConcurrentDictionary<int, Task> activeConnectionTasks = new();
    private readonly ConcurrentDictionary<int, Task> activeStreamTasks = new();
    private readonly SemaphoreSlim relayGate;
    private int nextConnectionTaskId;
    private int nextStreamTaskId;

    public QuicDataServer(
        ILogger<QuicDataServer> logger,
        IOptions<DataOverlayOptions> options,
        ConnectionThrottler connectionThrottler,
        IOptions<MeshOptions>? meshOptions = null)
    {
        logger.LogDebug("[QuicDataServer] Constructor called");
        this.logger = logger;
        this.options = options.Value;
        this.connectionThrottler = connectionThrottler ?? throw new ArgumentNullException(nameof(connectionThrottler));
        var cap = this.options.MaxPayloadBytes;
        if (meshOptions?.Value?.Security != null)
            cap = Math.Min(cap, meshOptions.Value.Security.GetEffectiveMaxPayloadSize());
        maxPayloadBytes = Math.Max(1, cap);
        relayGate = new SemaphoreSlim(Math.Max(1, this.options.MaxConcurrentRelays));
        logger.LogDebug("[QuicDataServer] Constructor completed");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CloseActiveConnectionsAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await DrainStreamTasksAsync(cancellationToken).ConfigureAwait(false);
        await DrainConnectionTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        // This yields immediately so Kestrel can start binding while QUIC initializes
        await Task.Yield();

        logger.LogDebug("[QuicDataServer] ExecuteAsync called");
        if (!options.Enable)
        {
            logger.LogInformation("[Overlay-QUIC-DATA] Disabled by configuration");
            return;
        }

        if (!QuicListener.IsSupported)
        {
            logger.LogWarning("[Overlay-QUIC-DATA] QUIC is not supported on this platform");
            return;
        }

        try
        {
            // Generate self-signed certificate for QUIC/TLS
            using var certificate = SelfSignedCertificate.Create("CN=mesh-overlay-quic-data");

            var listenerOptions = new QuicListenerOptions
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Any, options.ListenPort),
                ApplicationProtocols = new List<SslApplicationProtocol> { new SslApplicationProtocol("slskdn-overlay-data") },
                ConnectionOptionsCallback = (connection, hello, token) =>
                {
                    return new ValueTask<QuicServerConnectionOptions>(new QuicServerConnectionOptions
                    {
                        DefaultStreamErrorCode = 0x02,
                        DefaultCloseErrorCode = 0x02,
                        MaxInboundBidirectionalStreams = options.MaxConcurrentStreams,
                        MaxInboundUnidirectionalStreams = options.MaxConcurrentStreams,
                        ServerAuthenticationOptions = new SslServerAuthenticationOptions
                        {
                            ApplicationProtocols = new List<SslApplicationProtocol> { new SslApplicationProtocol("slskdn-overlay-data") },
                            ServerCertificate = certificate,
                            ClientCertificateRequired = false,
                        }
                    });
                }
            };

            await using var listener = await QuicListener.ListenAsync(listenerOptions, stoppingToken);
            logger.LogInformation("[Overlay-QUIC-DATA] Listening on port {Port}", options.ListenPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var connection = await listener.AcceptConnectionAsync(stoppingToken);
                    var ep = connection.RemoteEndPoint as IPEndPoint;
                    if (ep != null && !connectionThrottler.ShouldAllowConnection(ep.ToString(), TransportType.DirectQuic))
                    {
                        try
                        {
                            await connection.CloseAsync(0, stoppingToken);
                        }
                        catch
                        {
                            // Ignore close failures for rejected peers.
                        }

                        await connection.DisposeAsync();
                        continue;
                    }

                    TrackConnectionTask(HandleConnectionAsync(connection, stoppingToken));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Overlay-QUIC-DATA] Error accepting connection");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Overlay-QUIC-DATA] Server failed");
        }
    }

    private async Task HandleConnectionAsync(QuicConnection connection, CancellationToken ct)
    {
        var remoteEndPoint = connection.RemoteEndPoint as IPEndPoint;
        if (remoteEndPoint != null)
        {
            activeConnections.TryAdd(remoteEndPoint, connection);
        }

        try
        {
            await using (connection)
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var stream = await connection.AcceptInboundStreamAsync(ct);
                        if (!connectionThrottler.ShouldAllowInboundStream(remoteEndPoint?.ToString() ?? "unknown"))
                        {
                            await stream.DisposeAsync();
                            continue;
                        }

                        TrackStreamTask(HandleStreamAsync(stream, remoteEndPoint, ct));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (QuicOverlayServer.IsQuietAcceptStreamException(ex))
                        {
                            logger.LogDebug(ex, "[Overlay-QUIC-DATA] Peer closed before opening a stream from {Endpoint}", remoteEndPoint);
                        }
                        else
                        {
                            logger.LogWarning(ex, "[Overlay-QUIC-DATA] Error accepting stream from {Endpoint}", remoteEndPoint);
                        }

                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Overlay-QUIC-DATA] Connection error from {Endpoint}", remoteEndPoint);
        }
        finally
        {
            if (remoteEndPoint != null)
            {
                activeConnections.TryRemove(remoteEndPoint, out _);
            }
        }
    }

    private async Task HandleStreamAsync(QuicStream stream, IPEndPoint? remoteEndPoint, CancellationToken ct)
    {
        try
        {
            await using (stream)
            {
                var (line, lineBytes) = await ReadCommandLineAsync(stream, ct);

                if (line.StartsWith("RELAY_TCP ", StringComparison.Ordinal))
                {
                    connectionThrottler.ReportFailedAuth(remoteEndPoint?.Address.ToString() ?? "unknown", "missing relay authentication");
                    await WriteRelayErrorAsync(stream, "authentication required", remoteEndPoint, ct);
                    return;
                }

                if (line.StartsWith("AUTH ", StringComparison.Ordinal))
                {
                    if (!IsRelayAuthenticated(line, options.RelayAuthenticationToken))
                    {
                        connectionThrottler.ReportFailedAuth(remoteEndPoint?.Address.ToString() ?? "unknown", "invalid relay authentication");
                        await WriteRelayErrorAsync(stream, "authentication failed", remoteEndPoint, ct);
                        return;
                    }

                    connectionThrottler.ReportSuccessfulAuth(remoteEndPoint?.Address.ToString() ?? "unknown");

                    (line, _) = await ReadCommandLineAsync(stream, ct);
                    var parts = line.Split(' ');
                    if (parts.Length == 3 &&
                        parts[0] == "RELAY_TCP" &&
                        int.TryParse(parts[2], out var port) &&
                        port is > 0 and <= ushort.MaxValue)
                    {
                        var host = parts[1];
                        var destination = await ResolveAllowedRelayDestinationAsync(
                            host,
                            port,
                            options.AllowedRelayDestinations,
                            ct);
                        if (destination is null)
                        {
                            await WriteRelayErrorAsync(stream, "destination denied", remoteEndPoint, ct);
                            return;
                        }

                        if (!await relayGate.WaitAsync(0, ct))
                        {
                            await WriteRelayErrorAsync(stream, "relay capacity reached", remoteEndPoint, ct);
                            return;
                        }

                        try
                        {
                            using var relayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                            relayCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.MaxRelayDurationSeconds)));
                            using var tcp = new TcpClient(destination.AddressFamily);
                            await tcp.ConnectAsync(destination, relayCts.Token);
                            var tcpStream = tcp.GetStream();
                            await stream.WriteAsync("OK\n"u8.ToArray(), relayCts.Token);

                            var byteLimit = Math.Max(1, options.MaxRelayBytesPerDirection);
                            var toTcp = CopyToAsync(stream, tcpStream, byteLimit, relayCts.Token);
                            var toStream = CopyToAsync(tcpStream, stream, byteLimit, relayCts.Token);
                            await Task.WhenAny(toTcp, toStream);
                            await relayCts.CancelAsync();
                            try
                            {
                                await Task.WhenAll(toTcp, toStream);
                            }
                            catch (OperationCanceledException) when (relayCts.IsCancellationRequested)
                            {
                                // Expected after either relay direction completes or the duration quota expires.
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "[Overlay-QUIC-DATA] Approved RELAY_TCP connection failed");
                            await WriteRelayErrorAsync(stream, "relay failed", remoteEndPoint, ct);
                        }
                        finally
                        {
                            relayGate.Release();
                        }
                    }
                    else
                    {
                        await WriteRelayErrorAsync(stream, "bad command", remoteEndPoint, ct);
                    }

                    return;
                }

                // Non-relay: read payload (existing behavior)
                var buffer = new byte[maxPayloadBytes];
                var totalRead = Math.Min(lineBytes.Length, buffer.Length);
                if (totalRead > 0)
                    Array.Copy(lineBytes, 0, buffer, 0, totalRead);
                while (totalRead < buffer.Length)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(totalRead), ct);
                    if (read == 0) break;
                    totalRead += read;
                }

                if (totalRead > 0)
                {
                    logger.LogDebug("[Overlay-QUIC-DATA] Received {Size} bytes from {Endpoint}", totalRead, remoteEndPoint);

                    // Payload delivery: deferred until IOverlayDataPayloadHandler (or similar) is designed and wired.
                    // See memory-bank/triage-todo-fixme.md. For now we log and retain buffer for future use.
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Overlay-QUIC-DATA] Stream error from {Endpoint}", remoteEndPoint);
        }
    }

    internal static bool IsRelayAuthenticated(string authenticationLine, string configuredToken)
    {
        if (string.IsNullOrWhiteSpace(configuredToken) ||
            !authenticationLine.StartsWith("AUTH ", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var presentedToken = Convert.FromBase64String(authenticationLine[5..]);
            var expectedToken = Encoding.UTF8.GetBytes(configuredToken);
            return CryptographicOperations.FixedTimeEquals(presentedToken, expectedToken);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal static async Task<IPEndPoint?> ResolveAllowedRelayDestinationAsync(
        string host,
        int port,
        IReadOnlyCollection<string> allowedDestinations,
        CancellationToken cancellationToken)
    {
        var requested = host.Contains(':', StringComparison.Ordinal)
            ? $"[{host}]:{port}"
            : $"{host}:{port}";
        if (!allowedDestinations.Contains(requested, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var address))
        {
            addresses = new[] { address };
        }
        else
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }

        var publicAddress = addresses.FirstOrDefault(IsPublicRelayAddress);
        return publicAddress is null ? null : new IPEndPoint(publicAddress, port);
    }

    internal static bool IsPublicRelayAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] != 0 &&
                   bytes[0] != 10 &&
                   bytes[0] != 127 &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] >= 224);
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
               !address.IsIPv6LinkLocal &&
               !address.IsIPv6SiteLocal &&
               !address.IsIPv6Multicast &&
               (bytes[0] & 0xfe) != 0xfc;
    }

    private static async Task<(string Line, byte[] Bytes)> ReadCommandLineAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[256];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count, 1), ct);
            if (read == 0)
            {
                break;
            }

            count += read;
            if (buffer[count - 1] == (byte)'\n')
            {
                break;
            }
        }

        var bytes = buffer[..count];
        return (Encoding.ASCII.GetString(bytes).TrimEnd(), bytes);
    }

    private async Task WriteRelayErrorAsync(Stream stream, string reason, IPEndPoint? remoteEndPoint, CancellationToken ct)
    {
        try
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"ERR {reason}\n"), ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[Overlay-QUIC-DATA] Failed to send relay rejection to {Endpoint}", remoteEndPoint);
        }
    }

    private static async Task CopyToAsync(Stream source, Stream target, long maxBytes, CancellationToken ct)
    {
        var buf = new byte[8192];
        long total = 0;
        int r;
        while (total < maxBytes &&
               (r = await source.ReadAsync(buf.AsMemory(0, (int)Math.Min(buf.Length, maxBytes - total)), ct)) > 0)
        {
            await target.WriteAsync(buf.AsMemory(0, r), ct);
            total += r;
        }
    }

    private void TrackConnectionTask(Task task)
    {
        var taskId = Interlocked.Increment(ref nextConnectionTaskId);
        activeConnectionTasks.TryAdd(taskId, task);
        _ = task.ContinueWith(
            _ =>
            {
                if (activeConnectionTasks.TryRemove(taskId, out var removedTask))
                {
                    _ = removedTask;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void TrackStreamTask(Task task)
    {
        var taskId = Interlocked.Increment(ref nextStreamTaskId);
        activeStreamTasks.TryAdd(taskId, task);
        _ = task.ContinueWith(
            _ =>
            {
                if (activeStreamTasks.TryRemove(taskId, out var removedTask))
                {
                    _ = removedTask;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task CloseActiveConnectionsAsync()
    {
        var connections = activeConnections.Values.Distinct().ToArray();
        foreach (var connection in connections)
        {
            try
            {
                await connection.CloseAsync(0, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[Overlay-QUIC-DATA] Failed to close active connection during stop");
            }
        }
    }

    private async Task DrainConnectionTasksAsync(CancellationToken cancellationToken)
    {
        var tasks = activeConnectionTasks.Values.ToArray();
        if (tasks.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Host shutdown timeout should not surface as a second failure here.
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[Overlay-QUIC-DATA] Error draining active connection tasks during stop");
        }
    }

    private async Task DrainStreamTasksAsync(CancellationToken cancellationToken)
    {
        var tasks = activeStreamTasks.Values.ToArray();
        if (tasks.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Host shutdown timeout should not surface as a second failure here.
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[Overlay-QUIC-DATA] Error draining active stream tasks during stop");
        }
    }
}
