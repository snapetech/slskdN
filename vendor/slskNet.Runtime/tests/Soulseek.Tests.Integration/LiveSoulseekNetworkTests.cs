// <copyright file="LiveSoulseekNetworkTests.cs" company="slskdN Team">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class LiveSoulseekNetworkTests
    {
        private const int LiveOperationTimeout = 30000;
        private const int LiveConnectRetryCount = 3;

        [Trait("Category", "Connectivity")]
        [Trait("Category", "PeerConnectivity")]
        [Fact(DisplayName = "Live credentials can exercise peer, server, user, interests, and network search operations")]
        public async Task Live_Credentials_Can_Exercise_Peer_Server_User_Interests_And_Network_Search_Operations()
        {
            var remoteFilename = @"test-share\fixture-search.mp3";
            var remoteDirectory = @"test-share";
            var payload = Encoding.UTF8.GetBytes("slskNet.Runtime live peer fixture");
            var query = "slsknet-runtime-live-search";
            var suffix = Guid.NewGuid().ToString("N");
            var liked = "slsknet-runtime-live-like-" + suffix;
            var hated = "slsknet-runtime-live-hate-" + suffix;

            using (var fixture = await LivePeerFixture.ConnectAsync(remoteFilename, payload))
            using (var output = new MemoryStream())
            using (var cancellationTokenSource = new CancellationTokenSource(LiveOperationTimeout))
            {
                var browseResponse = await fixture.Primary.BrowseAsync(fixture.PeerUsername, cancellationToken: cancellationTokenSource.Token);
                var directory = Assert.Single(browseResponse.Directories);
                var browsedFile = Assert.Single(directory.Files);

                Assert.Equal(remoteDirectory, directory.Name);
                Assert.Equal(remoteFilename, browsedFile.Filename);
                Assert.Equal(payload.Length, browsedFile.Size);

                var transfer = await fixture.Primary.DownloadAsync(
                    fixture.PeerUsername,
                    remoteFilename,
                    () => Task.FromResult((Stream)output),
                    size: payload.Length,
                    options: new TransferOptions(maximumLingerTime: 250),
                    cancellationToken: cancellationTokenSource.Token);

                Assert.True(transfer.State.HasFlag(TransferStates.Succeeded));
                Assert.Equal(payload.Length, transfer.BytesTransferred);
                Assert.Equal(payload, output.ToArray());

                var options = new SearchOptions(
                    searchTimeout: 2000,
                    responseLimit: 1,
                    fileLimit: 10,
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    removeSingleCharacterSearchTerms: false);

                var result = await fixture.Primary.SearchAsync(
                    SearchQuery.FromText(query),
                    SearchScope.User(fixture.PeerUsername),
                    options: options,
                    cancellationToken: cancellationTokenSource.Token);

                var searchResponse = Assert.Single(result.Responses);
                var searchFile = Assert.Single(searchResponse.Files);

                Assert.Equal(fixture.PeerUsername, searchResponse.Username);
                Assert.Equal(remoteFilename, searchFile.Filename);
                Assert.Equal(payload.Length, searchFile.Size);

                var privileges = await fixture.Primary.GetPrivilegesAsync(cancellationTokenSource.Token);
                var rooms = await fixture.Primary.GetRoomListAsync(cancellationTokenSource.Token);

                await fixture.Primary.SetStatusAsync(UserPresence.Online, cancellationTokenSource.Token);
                await fixture.Primary.SetSharedCountsAsync(0, 0, cancellationTokenSource.Token);
                await fixture.Primary.SendUploadSpeedAsync(1, cancellationTokenSource.Token);

                Assert.True(privileges >= 0);
                Assert.NotNull(rooms);
                Assert.True(rooms.PublicCount >= 0);
                Assert.Equal(rooms.PublicCount, rooms.Public.Count);
                Assert.Equal(rooms.PrivateCount, rooms.Private.Count);
                Assert.Equal(rooms.OwnedCount, rooms.Owned.Count);
                Assert.Equal(rooms.ModeratedRoomNameCount, rooms.ModeratedRoomNames.Count);

                var watched = await fixture.Primary.WatchUserAsync(Settings.Username, cancellationTokenSource.Token);
                var status = await fixture.Primary.GetUserStatusAsync(Settings.Username, cancellationTokenSource.Token);
                var statistics = await fixture.Primary.GetUserStatisticsAsync(Settings.Username, cancellationTokenSource.Token);
                var privileged = await fixture.Primary.GetUserPrivilegedAsync(Settings.Username, cancellationTokenSource.Token);

                await fixture.Primary.UnwatchUserAsync(Settings.Username, cancellationTokenSource.Token);

                Assert.Equal(Settings.Username, watched.Username);
                Assert.Equal(Settings.Username, status.Username);
                Assert.Equal(Settings.Username, statistics.Username);
                Assert.True(watched.FileCount >= 0);
                Assert.True(statistics.FileCount >= 0);
                Assert.IsType<bool>(privileged);

                try
                {
                    await fixture.Primary.AddInterestAsync(liked, cancellationTokenSource.Token);
                    await fixture.Primary.AddHatedInterestAsync(hated, cancellationTokenSource.Token);

                    var interests = await WaitForInterestsAsync(fixture.Primary, liked, hated, cancellationTokenSource.Token);

                    Assert.Equal(Settings.Username, interests.Username);
                    Assert.Contains(liked, interests.Liked);
                    Assert.Contains(hated, interests.Hated);
                }
                finally
                {
                    if (fixture.Primary.State.HasFlag(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn))
                    {
                        await fixture.Primary.RemoveInterestAsync(liked, cancellationTokenSource.Token);
                        await fixture.Primary.RemoveHatedInterestAsync(hated, cancellationTokenSource.Token);
                    }
                }

                var networkSearchOptions = new SearchOptions(
                    searchTimeout: 3000,
                    responseLimit: 3,
                    fileLimit: 100,
                    filterResponses: false,
                    minimumResponseFileCount: 0);

                var networkResult = await fixture.Primary.SearchAsync(
                    SearchQuery.FromText("mp3"),
                    SearchScope.Network,
                    options: networkSearchOptions,
                    cancellationToken: cancellationTokenSource.Token);

                Assert.NotNull(networkResult.Search);
                Assert.NotNull(networkResult.Responses);
                Assert.True(networkResult.Responses.Count <= 3);
            }
        }

        private static async Task<UserInterests> WaitForInterestsAsync(
            SoulseekClient client,
            string liked,
            string hated,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var interests = await client.GetUserInterestsAsync(Settings.Username, cancellationToken);

                if (interests.Liked.Contains(liked) && interests.Hated.Contains(hated))
                {
                    return interests;
                }

                await Task.Delay(1000, cancellationToken);
            }

            return await client.GetUserInterestsAsync(Settings.Username, cancellationToken);
        }

        private static async Task ConnectWithRetryAsync(SoulseekClient client, string username, string password)
        {
            Exception lastException = null;

            for (var attempt = 1; attempt <= LiveConnectRetryCount; attempt++)
            {
                try
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(LiveOperationTimeout))
                    {
                        await client.ConnectAsync(username, password, cancellationTokenSource.Token);
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

        private sealed class LivePeerFixture : IDisposable
        {
            private LivePeerFixture(SoulseekClient primary, SoulseekClient peer, string peerUsername)
            {
                Primary = primary;
                Peer = peer;
                PeerUsername = peerUsername;
            }

            public SoulseekClient Peer { get; }

            public string PeerUsername { get; }

            public SoulseekClient Primary { get; }

            public static async Task<LivePeerFixture> ConnectAsync(string remoteFilename, byte[] payload)
            {
                if (string.IsNullOrWhiteSpace(Settings.PeerUsername) || string.IsNullOrWhiteSpace(Settings.PeerPassword))
                {
                    throw new InvalidOperationException("Set SLSK_INTEGRATION_PEER_USERNAME and SLSK_INTEGRATION_PEER_PASSWORD to run peer-to-peer live Soulseek integration tests.");
                }

                var primaryPort = GetAvailablePort();
                var peerPort = GetAvailablePort();

                var primaryCache = new StaticUserEndPointCache();
                var peerCache = new StaticUserEndPointCache();
                var connectionOptions = new ConnectionOptions(connectTimeout: LiveOperationTimeout);
                primaryCache.AddOrUpdate(Settings.PeerUsername, new IPEndPoint(IPAddress.Loopback, peerPort));
                peerCache.AddOrUpdate(Settings.Username, new IPEndPoint(IPAddress.Loopback, primaryPort));

                var primary = new SoulseekClient(
                    minorVersion: 9999,
                    options: new SoulseekClientOptions(
                        listenIPAddress: IPAddress.Loopback,
                        listenPort: primaryPort,
                        messageTimeout: LiveOperationTimeout,
                        serverConnectionOptions: connectionOptions,
                        peerConnectionOptions: connectionOptions,
                        transferConnectionOptions: connectionOptions,
                        incomingConnectionOptions: connectionOptions,
                        distributedConnectionOptions: connectionOptions,
                        userEndPointCache: primaryCache));

                SoulseekClient peer = null;
                peer = new SoulseekClient(
                    minorVersion: 9999,
                    options: new SoulseekClientOptions(
                        listenIPAddress: IPAddress.Loopback,
                        listenPort: peerPort,
                        messageTimeout: LiveOperationTimeout,
                        serverConnectionOptions: connectionOptions,
                        peerConnectionOptions: connectionOptions,
                        transferConnectionOptions: connectionOptions,
                        incomingConnectionOptions: connectionOptions,
                        distributedConnectionOptions: connectionOptions,
                        userEndPointCache: peerCache,
                        searchResponseResolver: (username, token, query) => Task.FromResult<SearchResponse>(CreateSearchResponse(token, remoteFilename, payload.Length)),
                        browseResponseResolver: (username, endpoint) => Task.FromResult(CreateBrowseResponse(remoteFilename, payload.Length)),
                        enqueueDownload: (username, endpoint, filename) => EnqueuePeerUploadAsync(peer, username, filename, remoteFilename, payload),
                        placeInQueueResolver: (username, endpoint, filename) => Task.FromResult<int?>(0)));

                try
                {
                    using (var cancellationTokenSource = new CancellationTokenSource(LiveOperationTimeout))
                    {
                        await ConnectWithRetryAsync(primary, Settings.Username, Settings.Password);
                        await ConnectWithRetryAsync(peer, Settings.PeerUsername, Settings.PeerPassword);
                    }

                    return new LivePeerFixture(primary, peer, Settings.PeerUsername);
                }
                catch
                {
                    primary.Dispose();
                    peer.Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                Primary.Dispose();
                Peer.Dispose();
            }

            private static BrowseResponse CreateBrowseResponse(string remoteFilename, int size)
            {
                var separatorIndex = remoteFilename.LastIndexOf('\\');
                var directoryName = separatorIndex >= 0 ? remoteFilename.Substring(0, separatorIndex) : string.Empty;
                var file = new Soulseek.File(1, remoteFilename, size, Path.GetExtension(remoteFilename)?.TrimStart('.') ?? string.Empty);
                return new BrowseResponse(new[] { new Soulseek.Directory(directoryName, new[] { file }) });
            }

            private static SearchResponse CreateSearchResponse(int token, string remoteFilename, int size)
            {
                var file = new Soulseek.File(1, remoteFilename, size, Path.GetExtension(remoteFilename)?.TrimStart('.') ?? string.Empty);
                return new SearchResponse(Settings.PeerUsername, token, hasFreeUploadSlot: true, uploadSpeed: 1, queueLength: 0, new[] { file });
            }

            private static async Task EnqueuePeerUploadAsync(
                SoulseekClient peer,
                string username,
                string requestedFilename,
                string expectedFilename,
                byte[] payload)
            {
                if (!string.Equals(requestedFilename, expectedFilename, StringComparison.Ordinal))
                {
                    throw new DownloadEnqueueException($"Unexpected fixture file requested: {requestedFilename}");
                }

                await peer.EnqueueUploadAsync(
                    username,
                    requestedFilename,
                    payload.Length,
                    _ => Task.FromResult((Stream)new MemoryStream(payload, writable: false)),
                    options: new TransferOptions(maximumLingerTime: 250));
            }

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

        private sealed class StaticUserEndPointCache : IUserEndPointCache
        {
            private readonly ConcurrentDictionary<string, IPEndPoint> endPoints = new ConcurrentDictionary<string, IPEndPoint>();

            public void AddOrUpdate(string username, IPEndPoint endPoint)
            {
                endPoints.AddOrUpdate(username, endPoint, (key, existing) => endPoint);
            }

            public bool TryGet(string username, out IPEndPoint endPoint)
            {
                return endPoints.TryGetValue(username, out endPoint);
            }
        }
    }
}
