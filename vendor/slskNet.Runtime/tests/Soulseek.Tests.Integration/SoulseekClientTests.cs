// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//     Copyright (c) slskdN Team.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    public class SoulseekClientTests
    {
        private const int LiveConnectTimeout = 30000;
        private const int LiveConnectRetryCount = 3;

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client connects")]
        public async Task Client_Connects()
        {
            using (var client = CreateLiveClient())
            {
                var ex = await Record.ExceptionAsync(() => ConnectWithRetryAsync(client));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);
            }
        }

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client connect raises StateChanged event")]
        public async Task Client_Connect_Raises_StateChanged_Event()
        {
            using (var client = CreateLiveClient())
            {
                var events = new List<SoulseekClientStateChangedEventArgs>();

                client.StateChanged += (sender, e) => events.Add(e);

                var ex = await Record.ExceptionAsync(() => ConnectWithRetryAsync(client));

                Assert.Null(ex);

                Assert.Equal(4, events.Count);
                Assert.Equal(SoulseekClientStates.Connecting, events[0].State);
                Assert.Equal(SoulseekClientStates.Connected, events[1].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, events[2].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, events[3].State);
            }
        }

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client disconnects")]
        public async Task Client_Disconnects()
        {
            using (var client = CreateLiveClient())
            {
                await ConnectWithRetryAsync(client);

                var ex = Record.Exception(() => client.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, client.State);
            }
        }

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client disconnect raises StateChanged event")]
        public async Task Client_Disconnect_Raises_StateChanged_Event()
        {
            SoulseekClientStateChangedEventArgs args = null;

            using (var client = CreateLiveClient())
            {
                await ConnectWithRetryAsync(client);

                client.StateChanged += (sender, e) => args = e;

                var ex = Record.Exception(() => client.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, client.State);
                Assert.Equal(SoulseekClientStates.Disconnected, args.State);
            }
        }

        [Trait("Category", "GetNextToken")]
        [Fact(DisplayName = "GetNextToken returns sequential tokens")]
        public void GetNextToken_Returns_Sequential_Tokens()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var t1 = s.GetNextToken();
                var t2 = s.GetNextToken();

                Assert.Equal(t1 + 1, t2);
            }
        }

        [Trait("Category", "GetNextToken")]
        [Fact(DisplayName = "GetNextToken rolls over at int.MaxValue")]
        public void GetNextToken_Rolls_Over_At_Int_MaxValue()
        {
            using (var s = new SoulseekClient(
                minorVersion: 9999,
                new SoulseekClientOptions(startingToken: int.MaxValue)))
            {
                var t1 = s.GetNextToken();
                var t2 = s.GetNextToken();

                Assert.Equal(int.MaxValue, t1);
                Assert.Equal(1, t2);
            }
        }

        private static SoulseekClient CreateLiveClient()
        {
            var connectionOptions = new ConnectionOptions(connectTimeout: LiveConnectTimeout);

            return new SoulseekClient(
                minorVersion: 9999,
                options: new SoulseekClientOptions(
                    listenIPAddress: IPAddress.Loopback,
                    listenPort: GetAvailablePort(),
                    messageTimeout: LiveConnectTimeout,
                    serverConnectionOptions: connectionOptions,
                    peerConnectionOptions: connectionOptions,
                    transferConnectionOptions: connectionOptions,
                    incomingConnectionOptions: connectionOptions,
                    distributedConnectionOptions: connectionOptions));
        }

        private static async Task ConnectWithRetryAsync(SoulseekClient client)
        {
            Exception lastException = null;

            for (var attempt = 1; attempt <= LiveConnectRetryCount; attempt++)
            {
                try
                {
                    using (var cancellationTokenSource = new System.Threading.CancellationTokenSource(LiveConnectTimeout))
                    {
                        await client.ConnectAsync(Settings.Username, Settings.Password, cancellationTokenSource.Token);
                        return;
                    }
                }
                catch (Exception ex) when (IsTransientConnectFailure(ex) && attempt < LiveConnectRetryCount)
                {
                    lastException = ex;
                    client.Disconnect();
                    await Task.Delay(2000 * attempt);
                }
            }

            throw lastException ?? new TimeoutException("Unable to connect to Soulseek after retries");
        }

        private static bool IsTransientConnectFailure(Exception ex)
            => ex is TimeoutException
                || ex is OperationCanceledException
                || (ex is SoulseekClientException clientException
                    && (clientException.InnerException is ConnectionException || clientException.InnerException is IOException));

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
