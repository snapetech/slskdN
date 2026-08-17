// <copyright file="DataOverlayOptions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Mesh.Overlay;

using slskd.Common;

/// <summary>
/// Options for QUIC data-plane overlay transfers.
/// </summary>
public class DataOverlayOptions
{
    public bool Enable { get; set; } = false;

    /// <summary>
    /// Public QUIC data-plane port. Matches <see cref="OverlayOptions.QuicListenPort"/> by default
    /// so it can share the public mesh UDP socket (see <see cref="ShareWithDhtPort"/>) instead of
    /// occupying a separate public port; used verbatim only when <see cref="ShareWithDhtPort"/> is
    /// false.
    /// </summary>
    public int ListenPort { get; set; } = 50300;

    /// <summary>
    /// When true (default) and the public mesh UDP socket is shared (see
    /// <see cref="OverlayOptions.ShareQuicWithDhtPort"/>), the data-plane QUIC listener binds to a
    /// loopback-only <see cref="BackendListenPort"/> instead of a public port; <see cref="slskd.DhtRendezvous.SharedMeshUdpListener"/>
    /// proxies inbound QUIC Initial packets to it after inspecting the TLS ClientHello ALPN
    /// (application protocol "slskdn-overlay-data") to distinguish them from control-plane QUIC.
    /// When false, binds <see cref="ListenPort"/> directly and publicly, as a standalone listener.
    /// </summary>
    public bool ShareWithDhtPort { get; set; } = true;

    /// <summary>Loopback-only backend port used when <see cref="ShareWithDhtPort"/> is true.</summary>
    public int BackendListenPort { get; set; } = 55401;

    public int MaxPayloadBytes { get; set; } = 512 * 1024; // 512 KB per message
    public int MaxConcurrentStreams { get; set; } = 8;
    public int ReceiveBufferBytes { get; set; } = 512 * 1024;
    public int SendBufferBytes { get; set; } = 512 * 1024;

    /// <summary>
    /// Gets or sets the shared secret required before processing relay commands.
    /// Relay commands are disabled when this value is empty.
    /// </summary>
    [Secret]
    public string RelayAuthenticationToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets exact host:port destinations that authenticated relay clients may reach.
    /// </summary>
    public List<string> AllowedRelayDestinations { get; set; } = new();

    public int MaxConcurrentRelays { get; set; } = 4;
    public long MaxRelayBytesPerDirection { get; set; } = 64 * 1024 * 1024;
    public int MaxRelayDurationSeconds { get; set; } = 300;

    /// <summary>Trusted SPKI SHA-256 pins keyed by remote endpoint (IP:port).</summary>
    public Dictionary<string, List<string>> TrustedCertificatePins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
