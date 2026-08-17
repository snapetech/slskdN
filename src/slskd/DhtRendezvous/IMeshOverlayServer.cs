// <copyright file="IMeshOverlayServer.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.DhtRendezvous;

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// TCP server for accepting inbound overlay connections from mesh peers.
/// </summary>
public interface IMeshOverlayServer
{
    /// <summary>
    /// Gets a value indicating whether the server is currently listening.
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Gets the number of active connections.
    /// </summary>
    int ActiveConnections { get; }

    /// <summary>
    /// Gets the total number of connections accepted.
    /// </summary>
    long TotalConnectionsAccepted { get; }

    /// <summary>
    /// Gets the total number of connections rejected.
    /// </summary>
    long TotalConnectionsRejected { get; }

    /// <summary>
    /// Start listening for incoming overlay connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop listening for incoming overlay connections.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Get current server statistics.
    /// </summary>
    /// <returns>Server statistics.</returns>
    MeshOverlayServerStats GetStats();

    /// <summary>
    /// Handles a connection that was accepted and classified as a mesh overlay handshake by an
    /// external demultiplexer (<see cref="SharedMeshTcpListener"/>) rather than by this server's
    /// own listener. Only meaningful when the server is running in shared-TCP-port mode
    /// (<see cref="DhtRendezvousOptions.ShareOverlayTcpPortWithSoulseek"/>); the connection is
    /// disposed without further handling otherwise.
    /// </summary>
    /// <param name="tcpClient">The already-accepted, unconsumed connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleExternallyAcceptedConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken = default);
}
