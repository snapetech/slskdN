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
    public int ListenPort { get; set; } = 50401;
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
}
