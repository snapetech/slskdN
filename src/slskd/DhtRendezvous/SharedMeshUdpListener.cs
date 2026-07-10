// <copyright file="SharedMeshUdpListener.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous;

using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MonoTorrent.Connections;
using MonoTorrent.Connections.Dht;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.Overlay;
using slskd.Mesh.Transport;

/// <summary>
/// Shares the public mesh UDP socket between DHT rendezvous, UDP overlay control, and QUIC.
/// </summary>
public sealed class SharedMeshUdpListener : IDhtListener, IDisposable
{
    private static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PendingSessionTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MalformedOverlayDatagramLogInterval = TimeSpan.FromMinutes(1);
    private readonly IPEndPoint _listenEndPoint;
    private readonly IPEndPoint? _quicBackendEndPoint;
    private readonly ILogger<SharedMeshUdpListener> _logger;
    private readonly IControlDispatcher? _overlayDispatcher;
    private readonly ConnectionThrottler? _connectionThrottler;
    private readonly int _maxOverlayDatagramBytes;
    private readonly int _maxRemotePayload;
    private readonly ConcurrentDictionary<IPEndPoint, QuicProxySession> _quicSessions = new();
    private readonly QuicProxyAdmissionGate _quicAdmissionGate = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly List<UdpClient> _publicSockets = new();
    private readonly List<Task> _receiveTasks = new();
    private UdpClient? _defaultPublicUdp;
    private ListenerStatus _status = ListenerStatus.NotListening;
    private long _malformedOverlayDatagramCount;
    private DateTimeOffset _lastMalformedOverlayDatagramWarning = DateTimeOffset.MinValue;

    public SharedMeshUdpListener(
        IPEndPoint listenEndPoint,
        IPEndPoint? quicBackendEndPoint,
        ILogger<SharedMeshUdpListener> logger,
        IControlDispatcher? overlayDispatcher = null,
        ConnectionThrottler? connectionThrottler = null,
        IOptions<OverlayOptions>? overlayOptions = null,
        IOptions<MeshOptions>? meshOptions = null)
    {
        _listenEndPoint = listenEndPoint;
        _quicBackendEndPoint = quicBackendEndPoint;
        _logger = logger;
        _overlayDispatcher = overlayDispatcher;
        _connectionThrottler = connectionThrottler;
        _maxOverlayDatagramBytes = overlayOptions?.Value?.MaxDatagramBytes ?? new OverlayOptions().MaxDatagramBytes;
        _maxRemotePayload = meshOptions?.Value?.Security?.GetEffectiveMaxPayloadSize() ?? SecurityUtils.MaxRemotePayloadSize;
    }

    public event Action<ReadOnlyMemory<byte>, IPEndPoint>? MessageReceived;
    public event EventHandler<EventArgs>? StatusChanged;

    public IPEndPoint LocalEndPoint => (IPEndPoint?)_defaultPublicUdp?.Client.LocalEndPoint ?? _listenEndPoint;
    public ListenerStatus Status => _status;

    public void Start()
    {
        if (_defaultPublicUdp is not null)
        {
            return;
        }

        try
        {
            foreach (var endpoint in GetBindEndPoints(_listenEndPoint))
            {
                var udp = new UdpClient(endpoint);
                _publicSockets.Add(udp);
                _defaultPublicUdp ??= udp;
            }

            SetStatus(ListenerStatus.Listening);
            foreach (var udp in _publicSockets)
            {
                _receiveTasks.Add(ReceiveLoopAsync(udp, _cts.Token));
            }

            _logger.LogInformation(
                "[DHT] Shared UDP listener bound {PublicPort} on {SocketCount} socket(s); UDP overlay enabled={OverlayEnabled}; QUIC backend={Backend}",
                _listenEndPoint.Port,
                _publicSockets.Count,
                _overlayDispatcher is not null,
                _quicBackendEndPoint?.ToString() ?? "disabled");
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            SetStatus(ListenerStatus.PortNotFree);
            throw;
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        foreach (var udp in _publicSockets)
        {
            udp.Dispose();
        }

        _publicSockets.Clear();
        _receiveTasks.Clear();
        _defaultPublicUdp = null;
        foreach (var session in _quicSessions.Values)
        {
            session.Dispose();
        }

        _quicSessions.Clear();
        SetStatus(ListenerStatus.NotListening);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, IPEndPoint endpoint)
    {
        var udp = _defaultPublicUdp ?? throw new InvalidOperationException("Shared UDP listener is not started.");
        await udp.SendAsync(buffer.ToArray(), endpoint).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }

    internal static bool IsQuicInitialPacket(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 1200 || (buffer[0] & 0xC0) != 0xC0)
        {
            return false;
        }

        var version = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(buffer[1..5]);
        var packetType = (buffer[0] & 0x30) >> 4;
        return (version == 0x00000001 && packetType == 0) ||
               (version == 0x6b3343cf && packetType == 1);
    }

    internal static bool IsQuicShortHeaderPacket(ReadOnlySpan<byte> buffer)
    {
        return buffer.Length > 0 && (buffer[0] & 0xC0) == 0x40;
    }

    internal static bool IsDhtPacket(ReadOnlySpan<byte> buffer)
    {
        return buffer.Length > 0 && buffer[0] == (byte)'d';
    }

    private async Task ReceiveLoopAsync(UdpClient udp, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                PruneIdleQuicSessions();

                if (_quicSessions.TryGetValue(result.RemoteEndPoint, out var existingSession))
                {
                    await existingSession.ForwardToBackendAsync(result.Buffer, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (IsDhtPacket(result.Buffer))
                {
                    MessageReceived?.Invoke(result.Buffer, result.RemoteEndPoint);
                    continue;
                }

                if (_quicBackendEndPoint is not null && IsQuicInitialPacket(result.Buffer))
                {
                    if (!_quicSessions.TryGetValue(result.RemoteEndPoint, out var session))
                    {
                        IDisposable? admissionLease = null;
                        try
                        {
                            admissionLease = _quicAdmissionGate.TryAcquire(result.RemoteEndPoint, DateTimeOffset.UtcNow);
                            if (admissionLease is null)
                            {
                                continue;
                            }

                            var candidate = new QuicProxySession(
                                result.RemoteEndPoint,
                                _quicBackendEndPoint,
                                udp,
                                _logger,
                                admissionLease,
                                _cts.Token);
                            admissionLease = null;
                            if (!_quicSessions.TryAdd(result.RemoteEndPoint, candidate))
                            {
                                candidate.Dispose();
                                if (!_quicSessions.TryGetValue(result.RemoteEndPoint, out session))
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                session = candidate;
                            }
                        }
                        finally
                        {
                            admissionLease?.Dispose();
                        }
                    }

                    await session.ForwardToBackendAsync(result.Buffer, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (_overlayDispatcher is not null)
                {
                    await HandleOverlayDatagramAsync(result.Buffer, result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                MessageReceived?.Invoke(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DHT] Shared UDP listener receive loop error");
            }
        }
    }

    private async Task HandleOverlayDatagramAsync(byte[] buffer, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        if (_connectionThrottler is not null && !_connectionThrottler.ShouldAllowInboundDatagram(remoteEndPoint.ToString()))
        {
            return;
        }

        if (buffer.Length > _maxRemotePayload)
        {
            _logger.LogWarning("[Overlay] Dropping datagram exceeding MaxRemotePayloadSize ({Size} bytes)", buffer.Length);
            return;
        }

        if (buffer.Length > _maxOverlayDatagramBytes)
        {
            _logger.LogWarning("[Overlay] Dropping oversized datagram size={Size}", buffer.Length);
            return;
        }

        ControlEnvelope? envelope;
        try
        {
            envelope = PayloadParser.ParseMessagePackSafely<ControlEnvelope>(buffer, _maxRemotePayload);
        }
        catch (Exception ex)
        {
            LogMalformedOverlayDatagram(remoteEndPoint, buffer.Length, ex);
            return;
        }

        if (envelope is null)
        {
            return;
        }

        await _overlayDispatcher!.HandleAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private void LogMalformedOverlayDatagram(IPEndPoint remoteEndPoint, int size, Exception exception)
    {
        var count = Interlocked.Increment(ref _malformedOverlayDatagramCount);
        var now = DateTimeOffset.UtcNow;
        if (now - _lastMalformedOverlayDatagramWarning >= MalformedOverlayDatagramLogInterval)
        {
            _lastMalformedOverlayDatagramWarning = now;
            _logger.LogInformation(
                "[Overlay] Dropped malformed overlay datagram from {Endpoint} size={Size} count={Count}; enable debug logging for decode details",
                OverlayLogSanitizer.Endpoint(remoteEndPoint),
                size,
                count);
        }

        _logger.LogDebug(
            exception,
            "[Overlay] Failed to decode malformed overlay datagram from {Endpoint} size={Size}",
            OverlayLogSanitizer.Endpoint(remoteEndPoint),
            size);
    }

    private void PruneIdleQuicSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (endpoint, session) in _quicSessions)
        {
            var timeout = session.IsAddressValidated ? SessionIdleTimeout : PendingSessionTimeout;
            if (session.LastActivity >= now - timeout || !_quicSessions.TryRemove(endpoint, out var removed))
            {
                continue;
            }

            removed.Dispose();
        }
    }

    private void SetStatus(ListenerStatus status)
    {
        if (_status == status)
        {
            return;
        }

        _status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyList<IPEndPoint> GetBindEndPoints(IPEndPoint listenEndPoint)
    {
        if (!listenEndPoint.Address.Equals(IPAddress.Any))
        {
            return new[] { listenEndPoint };
        }

        // Detect which local IP the OS would choose for internet-bound traffic.
        // This socket is never connected (UDP connect just sets the routing, no packets sent).
        // It becomes _defaultPublicUdp so DHT bootstrap packets go out with a routable source IP
        // and responses come back to the matching per-interface receive socket.
        var defaultRouteSourceIP = GetDefaultRouteSourceIP();

        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Select(address => address.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Distinct()
            .OrderBy(address =>
            {
                if (defaultRouteSourceIP is not null && address.Equals(defaultRouteSourceIP)) return 0; // default route IP first
                if (IPAddress.IsLoopback(address)) return 2; // loopback last
                return 1; // other IPs in the middle
            })
            .ToList();

        var endpoints = addresses.Select(address => new IPEndPoint(address, listenEndPoint.Port)).ToList();

        return endpoints.Count > 0
            ? endpoints
            : new[] { listenEndPoint };
    }

    private static IPAddress? GetDefaultRouteSourceIP()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 53);
            return ((IPEndPoint)socket.LocalEndPoint!).Address;
        }
        catch
        {
            return null;
        }
    }

    private sealed class QuicProxySession : IDisposable
    {
        private readonly IPEndPoint _remoteEndPoint;
        private readonly IPEndPoint _backendEndPoint;
        private readonly UdpClient _backendUdp = new(new IPEndPoint(IPAddress.Loopback, 0));
        private readonly UdpClient _publicUdp;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly Task _backendReceiveTask;
        private readonly IDisposable _admissionLease;
        private int _backendResponded;
        private int _addressValidated;
        private int _disposed;

        public QuicProxySession(
            IPEndPoint remoteEndPoint,
            IPEndPoint backendEndPoint,
            UdpClient publicUdp,
            ILogger logger,
            IDisposable admissionLease,
            CancellationToken parentToken)
        {
            _remoteEndPoint = remoteEndPoint;
            _backendEndPoint = backendEndPoint;
            _publicUdp = publicUdp;
            _logger = logger;
            _admissionLease = admissionLease;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            LastActivity = DateTimeOffset.UtcNow;
            _backendReceiveTask = ReceiveFromBackendAsync(_cts.Token);
        }

        public DateTimeOffset LastActivity { get; private set; }
        public bool IsAddressValidated => Volatile.Read(ref _addressValidated) != 0;

        public async Task ForwardToBackendAsync(byte[] datagram, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _backendResponded) != 0)
            {
                Volatile.Write(ref _addressValidated, 1);
            }

            LastActivity = DateTimeOffset.UtcNow;
            await _backendUdp.SendAsync(datagram, _backendEndPoint, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cts.Cancel();
            _backendUdp.Dispose();
            _cts.Dispose();
            _admissionLease.Dispose();
        }

        private async Task ReceiveFromBackendAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _backendUdp.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    Volatile.Write(ref _backendResponded, 1);
                    LastActivity = DateTimeOffset.UtcNow;
                    await _publicUdp.SendAsync(result.Buffer, _remoteEndPoint, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[DHT] QUIC UDP proxy session for {Endpoint} stopped", _remoteEndPoint);
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Bounds QUIC proxy state before a remote address has completed return-path validation.
/// </summary>
internal sealed class QuicProxyAdmissionGate
{
    internal const int GlobalSessionLimit = 128;
    internal const int PrefixSessionLimit = 8;
    internal const int GlobalAttemptLimit = 64;
    internal const int PrefixAttemptLimit = 4;
    internal static readonly TimeSpan AttemptWindow = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private readonly Dictionary<string, int> _activeByPrefix = new(StringComparer.Ordinal);
    private readonly Queue<(DateTimeOffset Timestamp, string Prefix)> _recentAttempts = new();
    private int _activeSessions;

    public IDisposable? TryAcquire(IPEndPoint remoteEndPoint, DateTimeOffset now)
    {
        var prefix = GetNetworkPrefix(remoteEndPoint.Address);
        lock (_lock)
        {
            while (_recentAttempts.TryPeek(out var attempt) && attempt.Timestamp < now - AttemptWindow)
            {
                _recentAttempts.Dequeue();
            }

            var activeForPrefix = _activeByPrefix.GetValueOrDefault(prefix);
            var attemptsForPrefix = _recentAttempts.Count(attempt => attempt.Prefix == prefix);
            if (_activeSessions >= GlobalSessionLimit ||
                activeForPrefix >= PrefixSessionLimit ||
                _recentAttempts.Count >= GlobalAttemptLimit ||
                attemptsForPrefix >= PrefixAttemptLimit)
            {
                return null;
            }

            _recentAttempts.Enqueue((now, prefix));
            _activeSessions++;
            _activeByPrefix[prefix] = activeForPrefix + 1;
            return new AdmissionLease(this, prefix);
        }
    }

    internal static string GetNetworkPrefix(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? $"{bytes[0]}.{bytes[1]}.{bytes[2]}"
            : Convert.ToHexString(bytes.AsSpan(0, 7));
    }

    private void Release(string prefix)
    {
        lock (_lock)
        {
            _activeSessions--;
            var remaining = _activeByPrefix[prefix] - 1;
            if (remaining == 0)
            {
                _activeByPrefix.Remove(prefix);
            }
            else
            {
                _activeByPrefix[prefix] = remaining;
            }
        }
    }

    private sealed class AdmissionLease : IDisposable
    {
        private QuicProxyAdmissionGate? _owner;
        private readonly string _prefix;

        public AdmissionLease(QuicProxyAdmissionGate owner, string prefix)
        {
            _owner = owner;
            _prefix = prefix;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.Release(_prefix);
        }
    }
}
