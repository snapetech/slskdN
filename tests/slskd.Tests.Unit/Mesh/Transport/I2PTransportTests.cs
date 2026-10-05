// <copyright file="I2PTransportTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Common.Security;
using Xunit;

namespace slskd.Tests.Unit.Mesh.Transport;

public class I2PTransportTests
{
    [Fact]
    public async Task IsAvailableAsync_SamPeerAcceptsButDoesNotRespond_UsesProbeTimeout()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        var options = new I2POptions { SamAddress = $"127.0.0.1:{endpoint.Port}" };
        var transport = new I2PTransport(options, NullLogger<I2PTransport>.Instance);

        var serverTask = Task.Run(async () =>
        {
            using var acceptedClient = await listener.AcceptTcpClientAsync();
            using var reader = new StreamReader(acceptedClient.GetStream());
            var hello = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal("HELLO VERSION MIN=3.1 MAX=3.1", hello);
            var unexpectedResponse = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(8));
            Assert.Null(unexpectedResponse);
        });

        using var callerDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var result = await transport.IsAvailableAsync(callerDeadline.Token);

        Assert.False(result);
        Assert.Equal("I2P SAM bridge availability check timed out", transport.GetStatus().LastError);
        await serverTask.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
