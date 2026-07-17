// <copyright file="DefaultTunnelConnectivityTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using slskd.Mesh.ServiceFabric.Services;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace slskd.Tests.Unit.Mesh.ServiceFabric;

public sealed class DefaultTunnelConnectivityTests
{
    [Fact]
    public async Task ConnectAsync_DialsValidatedAddressWithoutResolvingHostAgain()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var acceptTask = listener.AcceptTcpClientAsync();

        var connectivity = new DefaultTunnelConnectivity();
        var (stream, connectedIP) = await connectivity.ConnectAsync(
            "must-not-resolve.invalid",
            port,
            new[] { IPAddress.Loopback.ToString() },
            CancellationToken.None);

        await using (stream)
        using (var accepted = await acceptTask)
        {
            Assert.Equal(IPAddress.Loopback.ToString(), connectedIP);
            Assert.True(accepted.Connected);
        }
    }
}
