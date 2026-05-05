// <copyright file="ObfuscatedConnectionMatrixTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team.
// </copyright>

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public sealed class ObfuscatedConnectionMatrixTests
    {
        [Fact]
        public async Task Obfuscated_Peer_Message_Connection_RoundTrips_Message()
        {
            var expected = new UserInfoRequest().ToByteArray();
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetObfuscatedMessageConnection("receiver", pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetObfuscatedMessageConnection("sender", pair.ServerEndPoint, tcpClient: pair.Server))
            {
                var read = WaitForMessageAsync(receiver);

                await sender.WriteAsync(new UserInfoRequest(), CancellationToken.None);

                Assert.Equal(expected, await read);
            }
        }

        [Fact]
        public async Task Regular_Peer_Message_Connection_RoundTrips_Message()
        {
            var expected = new UserInfoRequest().ToByteArray();
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetMessageConnection("receiver", pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetMessageConnection("sender", pair.ServerEndPoint, tcpClient: pair.Server))
            {
                var read = WaitForMessageAsync(receiver);

                await sender.WriteAsync(new UserInfoRequest(), CancellationToken.None);

                Assert.Equal(expected, await read);
            }
        }

        [Fact]
        public async Task Obfuscated_Distributed_Message_Connection_RoundTrips_Message()
        {
            var expected = new DistributedPingResponse(123456).ToByteArray();
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetObfuscatedDistributedConnection("receiver", pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetObfuscatedDistributedConnection("sender", pair.ServerEndPoint, tcpClient: pair.Server))
            {
                var read = WaitForMessageAsync(receiver);

                await sender.WriteAsync(new DistributedPingResponse(123456), CancellationToken.None);

                Assert.Equal(expected, await read);
            }
        }

        [Fact]
        public async Task Regular_Distributed_Message_Connection_RoundTrips_Message()
        {
            var expected = new DistributedPingResponse(123456).ToByteArray();
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetDistributedConnection("receiver", pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetDistributedConnection("sender", pair.ServerEndPoint, tcpClient: pair.Server))
            {
                var read = WaitForMessageAsync(receiver);

                await sender.WriteAsync(new DistributedPingResponse(123456), CancellationToken.None);

                Assert.Equal(expected, await read);
            }
        }

        [Fact]
        public async Task Obfuscated_Transfer_Connection_RoundTrips_Handshake_And_Payload()
        {
            var token = 654321;
            var peerInit = new PeerInit("sender", Constants.ConnectionType.Transfer, token).ToByteArray();
            var payload = new byte[8193];
            new Random(123).NextBytes(payload);
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetObfuscatedTransferConnection(pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetObfuscatedTransferConnection(pair.ServerEndPoint, tcpClient: pair.Server))
            {
                await sender.WriteAsync(peerInit, CancellationToken.None);
                await sender.WriteAsync(BitConverter.GetBytes(token), CancellationToken.None);
                await sender.WriteAsync(payload, CancellationToken.None);

                Assert.Equal(peerInit, await receiver.ReadAsync(peerInit.Length, CancellationToken.None));
                Assert.Equal(BitConverter.GetBytes(token), await receiver.ReadAsync(4, CancellationToken.None));
                Assert.Equal(payload, await receiver.ReadAsync(payload.Length, CancellationToken.None));
            }
        }

        [Fact]
        public async Task Regular_Transfer_Connection_RoundTrips_When_Obfuscation_Is_Not_Used()
        {
            var token = 98765;
            var peerInit = new PeerInit("sender", Constants.ConnectionType.Transfer, token).ToByteArray();
            var payload = new byte[4096];
            new Random(456).NextBytes(payload);
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetTransferConnection(pair.ClientEndPoint, tcpClient: pair.Client))
            using (var receiver = new ConnectionFactory().GetTransferConnection(pair.ServerEndPoint, tcpClient: pair.Server))
            {
                await sender.WriteAsync(peerInit, CancellationToken.None);
                await sender.WriteAsync(BitConverter.GetBytes(token), CancellationToken.None);
                await sender.WriteAsync(payload, CancellationToken.None);

                Assert.Equal(peerInit, await receiver.ReadAsync(peerInit.Length, CancellationToken.None));
                Assert.Equal(BitConverter.GetBytes(token), await receiver.ReadAsync(4, CancellationToken.None));
                Assert.Equal(payload, await receiver.ReadAsync(payload.Length, CancellationToken.None));
            }
        }

        [Fact]
        public async Task Obfuscated_Transfer_Stream_Write_And_Read_RoundTrip()
        {
            var payload = new byte[32 * 1024];
            new Random(789).NextBytes(payload);
            using (var pair = await ConnectedTcpPair.CreateAsync())
            using (var sender = new ConnectionFactory().GetObfuscatedTransferConnection(pair.ClientEndPoint, new ConnectionOptions(writeBufferSize: 2048), pair.Client))
            using (var receiver = new ConnectionFactory().GetObfuscatedTransferConnection(pair.ServerEndPoint, new ConnectionOptions(readBufferSize: 1024), pair.Server))
            using (var input = new MemoryStream(payload))
            using (var output = new MemoryStream())
            {
                await sender.WriteAsync(payload.Length, input, cancellationToken: CancellationToken.None);
                await receiver.ReadAsync(payload.Length, output, null, cancellationToken: CancellationToken.None);

                Assert.Equal(payload, output.ToArray());
            }
        }

        private static Task<byte[]> WaitForMessageAsync(IMessageConnection connection)
        {
            var source = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.MessageRead += (_, args) => source.TrySetResult(args.Message);
            connection.StartReadingContinuously();
            return source.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private sealed class ConnectedTcpPair : IDisposable
        {
            private ConnectedTcpPair(TcpClientAdapter client, TcpClientAdapter server, IPEndPoint clientEndPoint, IPEndPoint serverEndPoint)
            {
                Client = client;
                Server = server;
                ClientEndPoint = clientEndPoint;
                ServerEndPoint = serverEndPoint;
            }

            public TcpClientAdapter Client { get; }

            public IPEndPoint ClientEndPoint { get; }

            public TcpClientAdapter Server { get; }

            public IPEndPoint ServerEndPoint { get; }

            public static async Task<ConnectedTcpPair> CreateAsync()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();

                try
                {
                    var serverEndPoint = (IPEndPoint)listener.LocalEndpoint;
                    var client = new TcpClient();
                    var accept = listener.AcceptTcpClientAsync();
                    await client.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                    var server = await accept;
                    return new ConnectedTcpPair(
                        new TcpClientAdapter(client),
                        new TcpClientAdapter(server),
                        (IPEndPoint)client.Client.RemoteEndPoint,
                        serverEndPoint);
                }
                finally
                {
                    listener.Stop();
                }
            }

            public void Dispose()
            {
                Client.Dispose();
                Server.Dispose();
            }
        }
    }
}
