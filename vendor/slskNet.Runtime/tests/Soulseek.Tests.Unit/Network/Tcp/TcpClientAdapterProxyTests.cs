// <copyright file="TcpClientAdapterProxyTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class TcpClientAdapterProxyTests
    {
        [Fact(DisplayName = "Proxy connect honors cancellation while reading bound domain response")]
        public async Task Proxy_Connect_Honors_Cancellation_While_Reading_Bound_Domain_Response()
        {
            using (var listener = new TcpListenerFixture(async stream =>
            {
                var buffer = new byte[1024];
                await stream.ReadAsync(buffer, 0, 3).ConfigureAwait(false);
                await stream.WriteAsync(new byte[] { 0x05, 0x00 }, 0, 2).ConfigureAwait(false);
                await stream.ReadAsync(buffer, 0, 10).ConfigureAwait(false);
                await stream.WriteAsync(new byte[] { 0x05, 0x00, 0x00, 0x03 }, 0, 4).ConfigureAwait(false);
                await WaitForClientDisconnectAsync(stream).ConfigureAwait(false);
            }))
            using (var client = new TcpClientAdapter())
            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80,
                    cancellationToken: cancellationTokenSource.Token));
            }
        }

        [Fact(DisplayName = "Proxy connect rejects short authentication response")]
        public async Task Proxy_Connect_Rejects_Short_Authentication_Response()
        {
            using (var listener = new TcpListenerFixture(async stream =>
            {
                var buffer = new byte[3];
                await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                await stream.WriteAsync(new byte[] { 0x05 }, 0, 1).ConfigureAwait(false);
            }))
            using (var client = new TcpClientAdapter())
            {
                await Assert.ThrowsAsync<ProxyException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80));
            }
        }

        [Fact(DisplayName = "Proxy connect rejects short connection response")]
        public async Task Proxy_Connect_Rejects_Short_Connection_Response()
        {
            using (var listener = new TcpListenerFixture(stream => WriteProxyResponseAsync(stream, new byte[] { 0x05, 0x00, 0x00 })))
            using (var client = new TcpClientAdapter())
            {
                await Assert.ThrowsAsync<ProxyException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80));
            }
        }

        [Fact(DisplayName = "Proxy connect rejects short bound domain response")]
        public async Task Proxy_Connect_Rejects_Short_Bound_Domain_Response()
        {
            using (var listener = new TcpListenerFixture(stream => WriteProxyResponseAsync(stream, new byte[] { 0x05, 0x00, 0x00, 0x03, 0x05, 0x61, 0x62 })))
            using (var client = new TcpClientAdapter())
            {
                await Assert.ThrowsAsync<ProxyException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80));
            }
        }

        [Fact(DisplayName = "Proxy connect rejects short bound IPv6 response")]
        public async Task Proxy_Connect_Rejects_Short_Bound_Ipv6_Response()
        {
            using (var listener = new TcpListenerFixture(stream => WriteProxyResponseAsync(stream, new byte[] { 0x05, 0x00, 0x00, 0x04, 0x20, 0x01, 0x0d, 0xb8 })))
            using (var client = new TcpClientAdapter())
            {
                await Assert.ThrowsAsync<ProxyException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80));
            }
        }

        [Fact(DisplayName = "Proxy connect rejects short bound port response")]
        public async Task Proxy_Connect_Rejects_Short_Bound_Port_Response()
        {
            using (var listener = new TcpListenerFixture(stream => WriteProxyResponseAsync(stream, new byte[] { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0x1f })))
            using (var client = new TcpClientAdapter())
            {
                await Assert.ThrowsAsync<ProxyException>(() => client.ConnectThroughProxyAsync(
                    listener.EndPoint.Address,
                    listener.EndPoint.Port,
                    IPAddress.Loopback,
                    80));
            }
        }

        private static async Task WaitForClientDisconnectAsync(NetworkStream stream)
        {
            var buffer = new byte[1];

            try
            {
                while (await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false) > 0)
                {
                    // Drain until the client closes the socket.
                }
            }
            catch
            {
                // The test server only needs to observe disconnect; socket shutdown races are acceptable here.
            }
        }

        private static async Task WriteProxyResponseAsync(NetworkStream stream, byte[] response)
        {
            var buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, 3).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x05, 0x00 }, 0, 2).ConfigureAwait(false);
            await stream.ReadAsync(buffer, 0, 10).ConfigureAwait(false);
            await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
        }

        private sealed class TcpListenerFixture : IDisposable
        {
            private readonly Task serverTask;
            private readonly TcpListener tcpListener;

            public TcpListenerFixture(Func<NetworkStream, Task> handler)
            {
                tcpListener = new TcpListener(IPAddress.Loopback, 0);
                tcpListener.Start();
                EndPoint = (IPEndPoint)tcpListener.LocalEndpoint;
                serverTask = RunServerAsync(handler);
            }

            public IPEndPoint EndPoint { get; }

            public void Dispose()
            {
                tcpListener.Stop();

                try
                {
                    serverTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch
                {
                    // Best-effort test cleanup.
                }
            }

            private async Task RunServerAsync(Func<NetworkStream, Task> handler)
            {
                using (var client = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false))
                using (var stream = client.GetStream())
                {
                    await handler(stream).ConfigureAwait(false);
                }
            }
        }
    }
}
