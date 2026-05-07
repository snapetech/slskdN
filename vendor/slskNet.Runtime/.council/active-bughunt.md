# Active Council Bughunt Candidate Report

This report is not a pass/fail proof. It is a fresh queue of suspicious shapes
that sit outside, or at the edge of, the current closed sweep gates. A green
all-phases council run means registered gates passed; it does not mean these
candidate lines are bugs or that no bugs exist.

Classification rule: any accepted row must be ledgered, fixed with behavior
coverage, sibling-swept, and promoted into a durable gate before closure.

## Event-style async boundaries
src/Messaging/Handlers/ServerMessageHandler.cs:205:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/PeerMessageHandler.cs:92:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:78:        public async void HandleChildMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:144:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:280:        public async void HandleEmbeddedMessage(byte[] message)
src/Network/ListenerHandler.cs:68:        public async void HandleConnection(object sender, IConnection connection)
src/Network/DistributedConnectionManager.cs:700:        public async void RemoveAndDisposeAll()
src/Network/DistributedConnectionManager.cs:1194:        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
src/Network/DistributedConnectionManager.cs:1276:        private async void StatusDebounceTimer_Elapsed(object sender, ElapsedEventArgs e)
src/Network/PeerConnectionManager.cs:789:        public async void RemoveAndDisposeAll()

## Silent catch or lossy exception boundaries
src/Network/ListenerHandler.cs:248:            catch (Exception)
src/Network/ListenerHandler.cs:249:            {
src/Network/ListenerHandler.cs:250:            }
src/Network/ListenerHandler.cs:257:                catch (Exception)
src/Network/ListenerHandler.cs:258:                {
src/Network/ListenerHandler.cs:259:                }

## Callback/event invocation boundaries
examples/Web/api/SharedFileCache.cs:65:                Refreshed?.Invoke(this, (directoryCount, Files.Count));
src/SearchResponder.cs:50:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/SearchResponder.cs:301:                () => RequestReceived?.Invoke(this, new SearchRequestEventArgs(username, token, query)));
src/SearchResponder.cs:306:                () => ResponseDelivered?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
src/SearchResponder.cs:311:                () => ResponseDeliveryFailed?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
src/Options/TransferOptions.cs:176:                    stateChanged?.Invoke(args);
src/Options/TransferOptions.cs:177:                    StateChanged?.Invoke(args);
src/Network/Tcp/ObfuscatedTransferConnection.cs:202:                reporter?.Invoke(bytesAvailable, bytesGranted, buffer.Length);
src/Network/Tcp/ObfuscatedTransferConnection.cs:267:                reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/Network/Tcp/Listener.cs:151:                    Accepted?.Invoke(this, eventArgs);
src/SoulseekClient.cs:167:            SearchResponder.RequestReceived += (sender, e) => RaiseEventHandler(nameof(SearchRequestReceived), () => SearchRequestReceived?.Invoke(this, e));
src/SoulseekClient.cs:168:            SearchResponder.ResponseDelivered += (sender, e) => RaiseEventHandler(nameof(SearchResponseDelivered), () => SearchResponseDelivered?.Invoke(this, e));
src/SoulseekClient.cs:169:            SearchResponder.ResponseDeliveryFailed += (sender, e) => RaiseEventHandler(nameof(SearchResponseDeliveryFailed), () => SearchResponseDeliveryFailed?.Invoke(this, e));
src/SoulseekClient.cs:198:                    RaiseEventHandler(nameof(DownloadFailed), () => DownloadFailed?.Invoke(this, e));
src/SoulseekClient.cs:224:                    RaiseEventHandler(nameof(DownloadDenied), () => DownloadDenied?.Invoke(this, e));
src/SoulseekClient.cs:238:            DistributedConnectionManager.PromotedToBranchRoot += (sender, e) => RaiseEventHandler(nameof(PromotedToDistributedBranchRoot), () => PromotedToDistributedBranchRoot?.Invoke(this, e));
src/SoulseekClient.cs:239:            DistributedConnectionManager.DemotedFromBranchRoot += (sender, e) => RaiseEventHandler(nameof(DemotedFromDistributedBranchRoot), () => DemotedFromDistributedBranchRoot?.Invoke(this, e));
src/SoulseekClient.cs:240:            DistributedConnectionManager.ParentAdopted += (sender, e) => RaiseEventHandler(nameof(DistributedParentAdopted), () => DistributedParentAdopted?.Invoke(this, e));
src/SoulseekClient.cs:241:            DistributedConnectionManager.ParentDisconnected += (sender, e) => RaiseEventHandler(nameof(DistributedParentDisconnected), () => DistributedParentDisconnected?.Invoke(this, e));
src/SoulseekClient.cs:242:            DistributedConnectionManager.ChildAdded += (sender, e) => RaiseEventHandler(nameof(DistributedChildAdded), () => DistributedChildAdded?.Invoke(this, e));
src/SoulseekClient.cs:243:            DistributedConnectionManager.ChildDisconnected += (sender, e) => RaiseEventHandler(nameof(DistributedChildDisconnected), () => DistributedChildDisconnected?.Invoke(this, e));
src/SoulseekClient.cs:244:            DistributedConnectionManager.StateChanged += (sender, e) => RaiseEventHandler(nameof(DistributedNetworkStateChanged), () => DistributedNetworkStateChanged?.Invoke(this, e));
src/SoulseekClient.cs:247:            ServerMessageHandler.UserCannotConnect += (sender, e) => RaiseEventHandler(nameof(UserCannotConnect), () => UserCannotConnect?.Invoke(this, e));
src/SoulseekClient.cs:248:            ServerMessageHandler.UserStatusChanged += (sender, e) => RaiseEventHandler(nameof(UserStatusChanged), () => UserStatusChanged?.Invoke(this, e));
src/SoulseekClient.cs:249:            ServerMessageHandler.UserStatisticsChanged += (sender, e) => RaiseEventHandler(nameof(UserStatisticsChanged), () => UserStatisticsChanged?.Invoke(this, e));
src/SoulseekClient.cs:250:            ServerMessageHandler.PrivateMessageReceived += (sender, e) => RaiseEventHandler(nameof(PrivateMessageReceived), () => PrivateMessageReceived?.Invoke(this, e));
src/SoulseekClient.cs:251:            ServerMessageHandler.PrivateRoomMembershipAdded += (sender, e) => RaiseEventHandler(nameof(PrivateRoomMembershipAdded), () => PrivateRoomMembershipAdded?.Invoke(this, e));
src/SoulseekClient.cs:252:            ServerMessageHandler.PrivateRoomMembershipRemoved += (sender, e) => RaiseEventHandler(nameof(PrivateRoomMembershipRemoved), () => PrivateRoomMembershipRemoved?.Invoke(this, e));
src/SoulseekClient.cs:253:            ServerMessageHandler.PrivateRoomModeratedUserListReceived += (sender, e) => RaiseEventHandler(nameof(PrivateRoomModeratedUserListReceived), () => PrivateRoomModeratedUserListReceived?.Invoke(this, e));
src/SoulseekClient.cs:254:            ServerMessageHandler.PrivateRoomModerationAdded += (sender, e) => RaiseEventHandler(nameof(PrivateRoomModerationAdded), () => PrivateRoomModerationAdded?.Invoke(this, e));
src/SoulseekClient.cs:255:            ServerMessageHandler.PrivateRoomModerationRemoved += (sender, e) => RaiseEventHandler(nameof(PrivateRoomModerationRemoved), () => PrivateRoomModerationRemoved?.Invoke(this, e));
src/SoulseekClient.cs:256:            ServerMessageHandler.PrivateRoomUserListReceived += (sender, e) => RaiseEventHandler(nameof(PrivateRoomUserListReceived), () => PrivateRoomUserListReceived?.Invoke(this, e));
src/SoulseekClient.cs:257:            ServerMessageHandler.PrivilegedUserListReceived += (sender, e) => RaiseEventHandler(nameof(PrivilegedUserListReceived), () => PrivilegedUserListReceived?.Invoke(this, e));
src/SoulseekClient.cs:258:            ServerMessageHandler.PrivilegeNotificationReceived += (sender, e) => RaiseEventHandler(nameof(PrivilegeNotificationReceived), () => PrivilegeNotificationReceived?.Invoke(this, e));
src/SoulseekClient.cs:259:            ServerMessageHandler.RoomMessageReceived += (sender, e) => RaiseEventHandler(nameof(RoomMessageReceived), () => RoomMessageReceived?.Invoke(this, e));
src/SoulseekClient.cs:260:            ServerMessageHandler.RoomTickerListReceived += (sender, e) => RaiseEventHandler(nameof(RoomTickerListReceived), () => RoomTickerListReceived?.Invoke(this, e));
src/SoulseekClient.cs:261:            ServerMessageHandler.RoomTickerAdded += (sender, e) => RaiseEventHandler(nameof(RoomTickerAdded), () => RoomTickerAdded?.Invoke(this, e));
src/SoulseekClient.cs:262:            ServerMessageHandler.RoomTickerRemoved += (sender, e) => RaiseEventHandler(nameof(RoomTickerRemoved), () => RoomTickerRemoved?.Invoke(this, e));
src/SoulseekClient.cs:263:            ServerMessageHandler.PublicChatMessageReceived += (sender, e) => RaiseEventHandler(nameof(PublicChatMessageReceived), () => PublicChatMessageReceived?.Invoke(this, e));
src/SoulseekClient.cs:264:            ServerMessageHandler.RoomJoined += (sender, e) => RaiseEventHandler(nameof(RoomJoined), () => RoomJoined?.Invoke(this, e));
src/SoulseekClient.cs:265:            ServerMessageHandler.RoomLeft += (sender, e) => RaiseEventHandler(nameof(RoomLeft), () => RoomLeft?.Invoke(this, e));
src/SoulseekClient.cs:266:            ServerMessageHandler.RoomListReceived += (sender, e) => RaiseEventHandler(nameof(RoomListReceived), () => RoomListReceived?.Invoke(this, e));
src/SoulseekClient.cs:268:            ServerMessageHandler.GlobalMessageReceived += (sender, e) => RaiseEventHandler(nameof(GlobalMessageReceived), () => GlobalMessageReceived?.Invoke(this, e));
src/SoulseekClient.cs:269:            ServerMessageHandler.DistributedNetworkReset += (sender, e) => RaiseEventHandler(nameof(DistributedNetworkReset), () => DistributedNetworkReset?.Invoke(this, e));
src/SoulseekClient.cs:270:            ServerMessageHandler.ExcludedSearchPhrasesReceived += (sender, e) => RaiseEventHandler(nameof(ExcludedSearchPhrasesReceived), () => ExcludedSearchPhrasesReceived?.Invoke(this, e));
src/SoulseekClient.cs:287:                RaiseEventHandler(nameof(KickedFromServer), () => KickedFromServer?.Invoke(this, e));
src/SoulseekClient.cs:3343:                options.ProgressUpdated?.Invoke((e.Username, e.BytesTransferred, e.BytesRemaining, e.PercentComplete, e.Size));
src/SoulseekClient.cs:3680:                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
src/SoulseekClient.cs:3689:                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
src/SoulseekClient.cs:3887:                        options.Reporter?.Invoke(new Transfer(download), attemptedBytes, grantedBytes, actualBytes);
src/SoulseekClient.cs:4692:                options.StateChanged?.Invoke((e.PreviousState, e.Search));
src/SoulseekClient.cs:4729:                        options.ResponseReceived?.Invoke((e.Search, e.Response));
src/SoulseekClient.cs:4887:            => RaiseEventHandler(nameof(PeerCapabilityReceived), () => PeerCapabilityReceived?.Invoke(this, new PeerCapabilityReceivedEventArgs(record)));
src/SoulseekClient.cs:4890:            => RaiseEventHandler(nameof(BrowseProgressUpdated), () => BrowseProgressUpdated?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4893:            => RaiseEventHandler(nameof(Connected), () => Connected?.Invoke(this, EventArgs.Empty));
src/SoulseekClient.cs:4896:            => RaiseEventHandler(nameof(Disconnected), () => Disconnected?.Invoke(this, new SoulseekClientDisconnectedEventArgs(message, exception)));
src/SoulseekClient.cs:4902:                DiagnosticGenerated?.Invoke(sender, eventArgs);
src/SoulseekClient.cs:4923:            => RaiseEventHandler(nameof(LoggedIn), () => LoggedIn?.Invoke(this, EventArgs.Empty));
src/SoulseekClient.cs:4926:            => RaiseEventHandler(nameof(ServerInfoReceived), () => ServerInfoReceived?.Invoke(this, serverInfo));
src/SoulseekClient.cs:4929:            => RaiseEventHandler(nameof(SearchResponseReceived), () => SearchResponseReceived?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4932:            => RaiseEventHandler(nameof(SearchStateChanged), () => SearchStateChanged?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4935:            => RaiseEventHandler(nameof(StateChanged), () => StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, state, message, exception)));
src/SoulseekClient.cs:4938:            => RaiseEventHandler(nameof(TransferProgressUpdated), () => TransferProgressUpdated?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4941:            => RaiseEventHandler(nameof(TransferStateChanged), () => TransferStateChanged?.Invoke(this, eventArgs));
src/SoulseekClient.cs:5127:                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
src/SoulseekClient.cs:5136:                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
src/SoulseekClient.cs:5309:                            options.Reporter?.Invoke(new Transfer(upload), attemptedBytes, grantedBytes, actualBytes);
src/SoulseekClient.cs:5527:                            options.SlotReleased?.Invoke(new Transfer(upload));
src/WishlistSearchScheduler.cs:225:                    options: options.SearchOptionsFactory?.Invoke(term),
src/WishlistSearchScheduler.cs:230:                SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, null, Array.Empty<SearchResponse>(), ex));
src/WishlistSearchScheduler.cs:234:            SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, result.Search, result.Responses, null));
src/Network/Tcp/Connection.cs:691:                    reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/Network/Tcp/Connection.cs:830:                    reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/Network/Tcp/Connection.cs:891:                .Invoke(this, EventArgs.Empty));
src/Network/Tcp/Connection.cs:900:                        .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:906:                    .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:917:                        .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:923:                    .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:929:                .Invoke(this, new ConnectionDisconnectedEventArgs(message, exception)));
src/Network/Tcp/Connection.cs:933:                .Invoke(this, eventArgs));
src/PeerCapabilityRegistry.cs:117:                Updated?.Invoke(this, new PeerCapabilityReceivedEventArgs(record));
src/PeerCapabilityRegistry.cs:121:                eventExceptionHandler?.Invoke(nameof(Updated), ex);
src/Network/PeerConnectionManager.cs:63:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/SearchInternal.cs:245:                        if (!(Options.ResponseFilter?.Invoke(response) ?? true))
src/SearchInternal.cs:251:                        var filteredFiles = response.Files.Where(f => Options.FileFilter?.Invoke(f) ?? true);
src/SearchInternal.cs:252:                        var filteredLockedFiles = response.LockedFiles.Where(f => Options.FileFilter?.Invoke(f) ?? true);
src/SearchInternal.cs:267:                    ResponseReceived?.Invoke(response);
src/Network/MessageConnection.cs:296:                        .Invoke(this, new MessageDataEventArgs(codeBytes, currentLength, totalLength)));
src/Network/MessageConnection.cs:302:                    .Invoke(this, new MessageDataEventArgs(codeBytes, currentLength, totalLength)));
src/Network/MessageConnection.cs:313:                        .Invoke(this, new MessageEventArgs(message)));
src/Network/MessageConnection.cs:319:                    .Invoke(this, new MessageEventArgs(message)));
src/Network/MessageConnection.cs:325:                .Invoke(this, new MessageReceivedEventArgs(length, code)));
src/Network/MessageConnection.cs:334:                        .Invoke(this, new MessageEventArgs(message)));
src/Network/MessageConnection.cs:340:                    .Invoke(this, new MessageEventArgs(message)));
src/Network/ListenerHandler.cs:52:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/Network/DistributedConnectionManager.cs:922:            => RaiseEvent(nameof(ChildAdded), () => ChildAdded?.Invoke(this, new DistributedChildEventArgs(connection.Username, connection.IPEndPoint)));
src/Network/DistributedConnectionManager.cs:925:            => RaiseEvent(nameof(ChildDisconnected), () => ChildDisconnected?.Invoke(this, new DistributedChildEventArgs(connection.Username, connection.IPEndPoint)));
src/Network/DistributedConnectionManager.cs:928:            => RaiseEvent(nameof(DemotedFromBranchRoot), () => DemotedFromBranchRoot?.Invoke(this, EventArgs.Empty));
src/Network/DistributedConnectionManager.cs:934:                DiagnosticGenerated?.Invoke(this, e);
src/Network/DistributedConnectionManager.cs:955:            => RaiseEvent(nameof(ParentAdopted), () => ParentAdopted?.Invoke(this, new DistributedParentEventArgs(connection.Username, connection.IPEndPoint, ParentBranchLevel, ParentBranchRoot)));
src/Network/DistributedConnectionManager.cs:958:            => RaiseEvent(nameof(ParentDisconnected), () => ParentDisconnected?.Invoke(this, new DistributedParentEventArgs(connection.Username, connection.IPEndPoint, ParentBranchLevel, ParentBranchRoot)));
src/Network/DistributedConnectionManager.cs:961:            => RaiseEvent(nameof(PromotedToBranchRoot), () => PromotedToBranchRoot?.Invoke(this, EventArgs.Empty));
src/Network/DistributedConnectionManager.cs:964:            => RaiseEvent(nameof(StateChanged), () => StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this)));
src/Messaging/Handlers/ServerMessageHandler.cs:222:                        RaiseEventHandler(nameof(ServerInfoReceived), () => ServerInfoReceived?.Invoke(this, new ServerInfo(parentMinSpeed: parentMinSpeed)));
src/Messaging/Handlers/ServerMessageHandler.cs:227:                        RaiseEventHandler(nameof(ServerInfoReceived), () => ServerInfoReceived?.Invoke(this, new ServerInfo(parentSpeedRatio: parentSpeedRatio)));
src/Messaging/Handlers/ServerMessageHandler.cs:232:                        RaiseEventHandler(nameof(ServerInfoReceived), () => ServerInfoReceived?.Invoke(this, new ServerInfo(wishlistInterval: wishlistInterval)));
src/Messaging/Handlers/ServerMessageHandler.cs:267:                        RaiseEventHandler(nameof(PrivateRoomMembershipAdded), () => PrivateRoomMembershipAdded?.Invoke(this, StringResponse.FromByteArray<MessageCode.Server>(message)));
src/Messaging/Handlers/ServerMessageHandler.cs:273:                        RaiseEventHandler(nameof(PrivateRoomMembershipRemoved), () => PrivateRoomMembershipRemoved?.Invoke(this, privateRoomRemoved));
src/Messaging/Handlers/ServerMessageHandler.cs:277:                        RaiseEventHandler(nameof(PrivateRoomModerationAdded), () => PrivateRoomModerationAdded?.Invoke(this, StringResponse.FromByteArray<MessageCode.Server>(message)));
src/Messaging/Handlers/ServerMessageHandler.cs:283:                        RaiseEventHandler(nameof(PrivateRoomModerationRemoved), () => PrivateRoomModerationRemoved?.Invoke(this, privateRoomOperatorRemoved));
src/Messaging/Handlers/ServerMessageHandler.cs:298:                        RaiseEventHandler(nameof(ExcludedSearchPhrasesReceived), () => ExcludedSearchPhrasesReceived?.Invoke(this, excludedSearchPhraseList));
src/Messaging/Handlers/ServerMessageHandler.cs:303:                        RaiseEventHandler(nameof(GlobalMessageReceived), () => GlobalMessageReceived?.Invoke(this, msg));
src/Messaging/Handlers/ServerMessageHandler.cs:317:                        RaiseEventHandler(nameof(RoomListReceived), () => RoomListReceived?.Invoke(this, roomList));
src/Messaging/Handlers/ServerMessageHandler.cs:322:                        RaiseEventHandler(nameof(PrivateRoomModeratedUserListReceived), () => PrivateRoomModeratedUserListReceived?.Invoke(this, moderatedRoomInfo));
src/Messaging/Handlers/ServerMessageHandler.cs:327:                        RaiseEventHandler(nameof(PrivateRoomUserListReceived), () => PrivateRoomUserListReceived?.Invoke(this, roomInfo));
src/Messaging/Handlers/ServerMessageHandler.cs:332:                        RaiseEventHandler(nameof(PrivilegedUserListReceived), () => PrivilegedUserListReceived?.Invoke(this, privilegedUserList));
src/Messaging/Handlers/ServerMessageHandler.cs:338:                            () => PrivilegeNotificationReceived?.Invoke(this, new PrivilegeNotificationReceivedEventArgs(PrivilegedUserNotification.FromByteArray(message))));
src/Messaging/Handlers/ServerMessageHandler.cs:345:                            () => PrivilegeNotificationReceived?.Invoke(this, new PrivilegeNotificationReceivedEventArgs(pn.Username, pn.Id)));
src/Messaging/Handlers/ServerMessageHandler.cs:376:                        RaiseEventHandler(nameof(DistributedNetworkReset), () => DistributedNetworkReset?.Invoke(this, EventArgs.Empty));
src/Messaging/Handlers/ServerMessageHandler.cs:391:                            RaiseEventHandler(nameof(UserCannotConnect), () => UserCannotConnect?.Invoke(this, new UserCannotConnectEventArgs(cannotConnect)));
src/Messaging/Handlers/ServerMessageHandler.cs:482:                        RaiseEventHandler(nameof(UserStatusChanged), () => UserStatusChanged?.Invoke(this, status));
src/Messaging/Handlers/ServerMessageHandler.cs:488:                        RaiseEventHandler(nameof(UserStatisticsChanged), () => UserStatisticsChanged?.Invoke(this, stats));
src/Messaging/Handlers/ServerMessageHandler.cs:495:                            () => PrivateMessageReceived?.Invoke(this, new PrivateMessageReceivedEventArgs(pm)));
src/Messaging/Handlers/ServerMessageHandler.cs:521:                        RaiseEventHandler(nameof(RoomLeft), () => RoomLeft?.Invoke(this, new RoomLeftEventArgs(leaveRoomResponse.RoomName, SoulseekClient.Username)));
src/Messaging/Handlers/ServerMessageHandler.cs:526:                        RaiseEventHandler(nameof(RoomMessageReceived), () => RoomMessageReceived?.Invoke(this, new RoomMessageReceivedEventArgs(roomMessage)));
src/Messaging/Handlers/ServerMessageHandler.cs:531:                        RaiseEventHandler(nameof(PublicChatMessageReceived), () => PublicChatMessageReceived?.Invoke(this, new PublicChatMessageReceivedEventArgs(publicChatMessage)));
src/Messaging/Handlers/ServerMessageHandler.cs:536:                        RaiseEventHandler(nameof(RoomJoined), () => RoomJoined?.Invoke(this, new RoomJoinedEventArgs(joinNotification)));
src/Messaging/Handlers/ServerMessageHandler.cs:541:                        RaiseEventHandler(nameof(RoomLeft), () => RoomLeft?.Invoke(this, new RoomLeftEventArgs(leftNotification)));
src/Messaging/Handlers/ServerMessageHandler.cs:546:                        RaiseEventHandler(nameof(RoomTickerListReceived), () => RoomTickerListReceived?.Invoke(this, new RoomTickerListReceivedEventArgs(roomTickers)));
src/Messaging/Handlers/ServerMessageHandler.cs:551:                        RaiseEventHandler(nameof(RoomTickerAdded), () => RoomTickerAdded?.Invoke(this, new RoomTickerAddedEventArgs(roomTickerAdded.RoomName, roomTickerAdded.Ticker)));
src/Messaging/Handlers/ServerMessageHandler.cs:556:                        RaiseEventHandler(nameof(RoomTickerRemoved), () => RoomTickerRemoved?.Invoke(this, new RoomTickerRemovedEventArgs(roomTickerRemoved.RoomName, roomTickerRemoved.Username)));
src/Messaging/Handlers/ServerMessageHandler.cs:580:                        RaiseEventHandler(nameof(KickedFromServer), () => KickedFromServer?.Invoke(this, EventArgs.Empty));
src/Messaging/Handlers/ServerMessageHandler.cs:640:                DiagnosticGenerated?.Invoke(this, e);
src/Messaging/Handlers/PeerMessageHandler.cs:54:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/Messaging/Handlers/PeerMessageHandler.cs:343:                        DownloadDenied?.Invoke(this, new DownloadDeniedEventArgs(connection.Username, uploadDeniedResponse.Filename, uploadDeniedResponse.Message));
src/Messaging/Handlers/PeerMessageHandler.cs:364:                        DownloadFailed?.Invoke(this, new DownloadFailedEventArgs(connection.Username, uploadFailedResponse.Filename));
src/Messaging/Handlers/DistributedMessageHandler.cs:51:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

## Unisolated server handler event invocations

## Unisolated message connection event invocations

## Unisolated TCP connection event invocations

## Unisolated client lifecycle event invocations

## Unisolated client search event invocations

## Unisolated client transfer/browse event invocations

## Unisolated SoulseekClient bridge event invocations

## Remote/user text in diagnostics or HTTP errors
examples/Web/api/SharedFileCache.cs:140:                Console.WriteLine($"[MALFORMED QUERY]: {query} ({ex.Message})");
src/SearchResponder.cs:91:                        Diagnostic.Debug($"Discarded cached search response {responseToken} to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:106:                    Diagnostic.Warning($"Error removing cached search response {responseToken}: {ex.Message}", ex);
src/SearchResponder.cs:138:                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:151:                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");
src/SearchResponder.cs:175:                            Diagnostic.Debug($"Failed to connect to {username} with solicitation token {responseToken} to deliver search results for query '{query}' with token {token}.  Cached response for potential delayed delivery.");
src/SearchResponder.cs:179:                            Diagnostic.Warning($"Error caching undelivered search response {responseToken} for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:188:                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:195:                Diagnostic.Debug($"Failed to send search response to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:236:                    Diagnostic.Warning($"Error retrieving cached search response {responseToken}: {ex.Message}", ex);
src/SearchResponder.cs:249:                        Diagnostic.Debug($"Sent cached response {responseToken} containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:255:                        Diagnostic.Debug($"Failed to send cached search response {responseToken} to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:321:                Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
examples/Web/api/Startup.cs:305:                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [DIAGNOSTIC:{e.GetType().Name}] [{args.Level}] {args.Message}");
examples/Web/api/Startup.cs:365:                Console.WriteLine($"[PUBLIC CHAT] [{args.RoomName}] [{args.Username}]: {args.Message}");
examples/Web/api/Startup.cs:389:                Console.WriteLine($"Disconnected from Soulseek server: {args.Message}");
examples/Web/api/Startup.cs:434:                Console.WriteLine($"[SEARCH RESPONSE DELIVERY] {args.SearchResponse.FileCount + args.SearchResponse.LockedFileCount} files to {args.Username} for query '{args.Query}'");
examples/Web/api/Startup.cs:439:                Console.WriteLine($"[SEARCH RESPONSE DELIVERY FAILED] {args.SearchResponse.FileCount + args.SearchResponse.LockedFileCount} files to {args.Username} for query '{args.Query}'");
examples/Web/api/Startup.cs:630:                Console.WriteLine($"[UPLOAD RE-REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:642:                    Console.WriteLine($"[UPLOAD SLOT REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:657:                    Console.WriteLine($"[UPLOAD SLOT RELEASED] [{username}/{filename}]");
examples/Web/api/Startup.cs:717:                    Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query.SearchText}");
src/SoulseekClient.cs:189:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} reported as failed by remote client (token: {download.Token})");
src/SoulseekClient.cs:194:                    Diagnostic.Warning($"Failed to mark download(s) failed: {ex.Message}", ex);
src/SoulseekClient.cs:215:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} rejected by remote client (token: {download.Token})");
src/SoulseekClient.cs:220:                    Diagnostic.Warning($"Failed to mark download(s) rejected: {ex.Message}", ex);
src/SoulseekClient.cs:3276:                Diagnostic.Debug($"Acknowledged private message ID {privateMessageId}");
src/SoulseekClient.cs:3614:                    Diagnostic.Debug($"Invalidated message connection cache for {username}");
src/SoulseekClient.cs:3707:                Diagnostic.Debug($"Global download semaphore for file {GetDiagnosticLogValue(download.Filename)} to {username} acquired");
src/SoulseekClient.cs:3711:                Diagnostic.Debug($"Fetched peer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3722:                Diagnostic.Debug($"Wrote transfer request for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3727:                Diagnostic.Debug($"Received transfer request ACK for download of {GetDiagnosticLogValue(download.Filename)} from {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:3751:                    Diagnostic.Debug($"Fetched transfer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
src/SoulseekClient.cs:3783:                    Diagnostic.Debug($"Fetched peer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3795:                        Diagnostic.Debug($"Fetched transfer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
src/SoulseekClient.cs:3801:                        Diagnostic.Warning($"Attempting to initiate a second-chance transfer connection to {username} for download of {GetDiagnosticLogValue(download.Filename)}");
src/SoulseekClient.cs:3807:                        Diagnostic.Warning($"Successfully established a second-chance transfer connection to {username} for download of {GetDiagnosticLogValue(download.Filename)}");
src/SoulseekClient.cs:3864:                    Diagnostic.Debug($"Seeking output stream for download of {GetDiagnosticLogValue(download.Filename)} from {username} to starting offset of {download.StartOffset} bytes");
src/SoulseekClient.cs:3868:                Diagnostic.Debug($"Seeking download of {GetDiagnosticLogValue(download.Filename)} from {username} to starting offset of {download.StartOffset} bytes");
src/SoulseekClient.cs:3920:                Diagnostic.Info($"Download of {GetDiagnosticLogValue(download.Filename)} from {username} complete ({outputStream.Position} of {download.Size} bytes).");
src/SoulseekClient.cs:3999:                        Diagnostic.Warning($"Failed to cancel wait for key {transferStartRequestedWaitKey}: {ex.Message}");
src/SoulseekClient.cs:4008:                        Diagnostic.Warning($"Failed to dispose transfer connection for file {GetDiagnosticLogValue(remoteFilename)} from user {username}: {ex.Message}");
src/SoulseekClient.cs:4022:                        Diagnostic.Warning($"Failed to determine final position of output stream for file {GetDiagnosticLogValue(download.Filename)} from {username}: {ex.Message}", ex);
src/SoulseekClient.cs:4044:                            Diagnostic.Warning($"Failed to finalize output stream for file {GetDiagnosticLogValue(download.Filename)} from {username}: {ex.Message}", ex);
src/SoulseekClient.cs:4059:                            Diagnostic.Debug($"Global download semaphore for file {GetDiagnosticLogValue(download.Filename)} from {username} released");
src/SoulseekClient.cs:4063:                            Diagnostic.Warning($"Failed to release global download semaphore for file {GetDiagnosticLogValue(download.Filename)} to {username}: {ex.Message}");
src/SoulseekClient.cs:4175:                    Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
src/SoulseekClient.cs:4202:                        Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
src/SoulseekClient.cs:4210:                    Diagnostic.Debug($"EndPoint cache MISS for {username}: {endPoint}");
src/SoulseekClient.cs:4868:                Diagnostic.Warning($"Rejected peer capability message from {username}: descriptor signature is invalid.");
src/SoulseekClient.cs:4945:            Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
src/SoulseekClient.cs:5173:                Diagnostic.Debug($"Upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5180:                    Diagnostic.Debug($"Upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5193:                Diagnostic.Debug($"Global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5200:                Diagnostic.Debug($"Fetched peer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");
src/SoulseekClient.cs:5210:                Diagnostic.Debug($"Wrote transfer request for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");
src/SoulseekClient.cs:5215:                Diagnostic.Debug($"Received transfer request ACK for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:5227:                Diagnostic.Debug($"Fetched transfer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {upload.Connection.Id}, state: {upload.Connection.State})");
src/SoulseekClient.cs:5259:                    Diagnostic.Debug($"Failed to read start offset for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}");
src/SoulseekClient.cs:5275:                Diagnostic.Debug($"Resolving input stream for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}");
src/SoulseekClient.cs:5285:                    Diagnostic.Debug($"Seeking input stream for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} to starting offset of {upload.StartOffset} bytes");
src/SoulseekClient.cs:5349:                            Diagnostic.Warning($"Transfer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} forcibly closed after exceeding maximum linger time of {options.MaximumLingerTime}ms.");
src/SoulseekClient.cs:5365:                Diagnostic.Info($"Upload of {GetDiagnosticLogValue(upload.Filename)} to {username} complete ({inputStream.Position} of {upload.Size} bytes).");
src/SoulseekClient.cs:5434:                        Diagnostic.Warning($"Failed to dispose transfer connection for file {GetDiagnosticLogValue(remoteFilename)} to user {username}: {ex.Message}");
src/SoulseekClient.cs:5448:                        Diagnostic.Warning($"Failed to determine final position of input stream for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5463:                            Diagnostic.Warning($"Failed to finalize input stream for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5507:                            Diagnostic.Debug($"Upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5512:                            Diagnostic.Warning($"Failed to release upload semaphore for user {username}: {ex.Message}");
src/SoulseekClient.cs:5525:                            Diagnostic.Debug($"Upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5531:                            Diagnostic.Warning($"Encountered Exception releasing upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5540:                            Diagnostic.Debug($"Global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5544:                            Diagnostic.Warning($"Failed to release global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}");
src/Network/PeerConnectionManager.cs:141:                Diagnostic.Debug($"Purging message connection cache of failed connection to {username} ({c.IPEndPoint}).");
src/Network/PeerConnectionManager.cs:148:                Diagnostic.Debug($"Inbound message connection to {username} ({c.IPEndPoint}) accepted. (type: {c.Type}, id: {c.Id})");
src/Network/PeerConnectionManager.cs:162:                Diagnostic.Debug($"Inbound message connection to {username} ({connection.IPEndPoint}) handed off. (old: {c.Id}, new: {connection.Id})");
src/Network/PeerConnectionManager.cs:177:                        Diagnostic.Debug($"Cancelling pending inbound indirect message connection to {username}");
src/Network/PeerConnectionManager.cs:187:                        Diagnostic.Debug($"Superseding cached message connection to {username} ({cachedConnection.IPEndPoint}) (old: {cachedConnection.Id}, new: {connection.Id}");
src/Network/PeerConnectionManager.cs:205:                Diagnostic.Debug($"Message connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:230:            Diagnostic.Debug($"Waiting for a direct or indirect transfer connection from {username} with remote token {remoteToken} for {filename}");
src/Network/PeerConnectionManager.cs:264:            Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} transfer connection to {username} ({connection.IPEndPoint}) with remote token {remoteToken} for {filename} established first, attempting to cancel {(isDirect ? "indirect" : "direct")} connection.");
src/Network/PeerConnectionManager.cs:267:            Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) with remote token {remoteToken} for {filename} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:298:                Diagnostic.Debug($"Failed to retrieve cached message connection to {username}: {ex.Message}");
src/Network/PeerConnectionManager.cs:377:                Diagnostic.Debug($"Attempting inbound indirect message connection to {r.Username} ({endPoint}) for token {r.Token}");
src/Network/PeerConnectionManager.cs:421:                Diagnostic.Debug($"Message connection to {r.Username} ({r.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:460:                    Diagnostic.Debug($"Retrieved cached message connection to {username} ({ipEndPoint}) (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:467:                Diagnostic.Debug($"Purging message connection cache of failed connection to {username} ({ipEndPoint}).");
src/Network/PeerConnectionManager.cs:481:                Diagnostic.Debug($"Attempting simultaneous direct and indirect message connections to {username} ({ipEndPoint})");
src/Network/PeerConnectionManager.cs:520:                Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} message connection to {username} ({ipEndPoint}) established first, attempting to cancel {(isDirect ? "indirect" : "direct")} connection.");
src/Network/PeerConnectionManager.cs:552:                Diagnostic.Debug($"Message connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:583:            Diagnostic.Debug($"Inbound transfer connection to {username} ({incomingConnection.IPEndPoint}) for token {token} accepted. (type: {incomingConnection.Type}, id: {incomingConnection.Id}");
src/Network/PeerConnectionManager.cs:596:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) for token {token} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:598:            Diagnostic.Debug($"Inbound {(incomingConnection.Obfuscated ? "obfuscated " : string.Empty)}transfer connection to {username} ({connection.IPEndPoint}) for token {token} handed off. (old: {incomingConnection.Id}, new: {connection.Id})");
src/Network/PeerConnectionManager.cs:615:            Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) for token {remoteToken} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:630:            Diagnostic.Debug($"Attempting inbound indirect {(useObfuscated ? "obfuscated " : string.Empty)}transfer connection to {connectToPeerResponse.Username} ({endPoint}) for token {connectToPeerResponse.Token}");
src/Network/PeerConnectionManager.cs:641:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for token {connectToPeerResponse.Token} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:663:                    Diagnostic.Debug($"Falling back to regular inbound indirect transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for token {connectToPeerResponse.Token}");
src/Network/PeerConnectionManager.cs:675:            Diagnostic.Debug($"{(useObfuscated ? "Obfuscated t" : "T")}ransfer connection to {connectToPeerResponse.Username} ({endPoint}) for token {connectToPeerResponse.Token} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:699:            Diagnostic.Debug($"Attempting simultaneous direct{(obfuscatedEndPoint != null ? ", obfuscated direct," : string.Empty)} and indirect transfer connections to {username} ({ipEndPoint})");
src/Network/PeerConnectionManager.cs:705:                Diagnostic.Debug($"Compatible obfuscated transfer endpoint found for {username} ({obfuscatedEndPoint}); adding obfuscated direct transfer candidate");
src/Network/PeerConnectionManager.cs:734:                Diagnostic.Debug($"{(isObfuscated ? "Obfuscated direct" : isDirect ? "Direct" : "Indirect")} transfer connection to {username} ({connection.IPEndPoint}) established first, negotiating transfer setup before cancelling remaining candidates.");
src/Network/PeerConnectionManager.cs:750:                    Diagnostic.Debug($"Failed to negotiate obfuscated transfer connection to {username} ({connection.IPEndPoint}); preserving regular fallback candidates: {ex.Message}");
src/Network/PeerConnectionManager.cs:781:                Diagnostic.Debug($"{(connection.Obfuscated ? "Obfuscated t" : "T")}ransfer connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:891:            Diagnostic.Debug($"Attempting direct message connection to {username} ({ipEndPoint})");
src/Network/PeerConnectionManager.cs:910:                Diagnostic.Debug($"Failed to establish a direct message connection to {username} ({ipEndPoint}): {ex.Message}");
src/Network/PeerConnectionManager.cs:915:            Diagnostic.Debug($"Direct message connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:926:            Diagnostic.Debug($"Attempting obfuscated direct message connection to {username} ({ipEndPoint})");
src/Network/PeerConnectionManager.cs:945:                Diagnostic.Debug($"Failed to establish an obfuscated direct message connection to {username} ({ipEndPoint}): {ex.Message}");
src/Network/PeerConnectionManager.cs:950:            Diagnostic.Debug($"Obfuscated direct message connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:956:            Diagnostic.Debug($"Soliciting indirect message connection to {username} with token {solicitationToken}");
src/Network/PeerConnectionManager.cs:982:                Diagnostic.Debug($"Indirect message connection to {username} ({incomingConnection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");
src/Network/PeerConnectionManager.cs:990:                Diagnostic.Debug($"Indirect message connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:995:                Diagnostic.Debug($"Failed to establish an indirect message connection to {username} with token {solicitationToken}: {ex.Message}");
src/Network/PeerConnectionManager.cs:1017:            Diagnostic.Debug($"Attempting {(obfuscated ? "obfuscated " : string.Empty)}direct transfer connection for token {token} to {ipEndPoint}");
src/Network/PeerConnectionManager.cs:1024:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection for token {token} to {ipEndPoint} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1032:                Diagnostic.Debug($"Failed to establish a {(obfuscated ? "obfuscated " : string.Empty)}direct transfer connection for token {token} to ({ipEndPoint}): {ex.Message}");
src/Network/PeerConnectionManager.cs:1037:            Diagnostic.Debug($"{(obfuscated ? "Obfuscated d" : "D")}irect transfer connection for {token} to {connection.IPEndPoint} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1043:            Diagnostic.Debug($"Soliciting indirect transfer connection to {username} with token {token}");
src/Network/PeerConnectionManager.cs:1069:                Diagnostic.Debug($"Indirect {(incomingConnection.Obfuscated ? "obfuscated " : string.Empty)}transfer connection to {username} ({incomingConnection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");
src/Network/PeerConnectionManager.cs:1072:                connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection for token {token} ({incomingConnection.IPEndPoint}) disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1074:                Diagnostic.Debug($"Indirect transfer connection for {token} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1079:                Diagnostic.Debug($"Failed to establish an indirect transfer connection to {username} with token {token}: {ex.Message}");
src/Network/PeerConnectionManager.cs:1092:            Diagnostic.Debug($"Message connection to {connection.Username} ({connection.IPEndPoint}) disconnected. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1113:            Diagnostic.Debug($"Message connection cache now contains {MessageConnectionDictionary.Count} connections.");
src/Network/ListenerHandler.cs:166:                            Diagnostic.Debug($"Unexpected transfer connection for token {peerInit.Token} from {peerInit.Username} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:189:                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:196:                            Diagnostic.Debug($"Obfuscated distributed PierceFirewall with token {pierceFirewall.Token} accepted from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}); completing solicited distributed wait. (id: {connection.Id})");
src/Network/ListenerHandler.cs:199:                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:208:                        Diagnostic.Debug($"PierceFirewall matching pending search response received from {username} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:232:                Diagnostic.Debug($"Failed to initialize direct connection from {GetConnectionDescription(connection)}: {ex.Message}");
src/Network/DistributedConnectionManager.cs:246:                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) rejected: enabled {Enabled}; has parent: {HasParent}; is branch root: {IsBranchRoot}; children: {ChildDictionary.Count}/{ChildLimit}");
src/Network/DistributedConnectionManager.cs:263:                Diagnostic.Debug($"Purging child connection cache of failed connection to {username} ({c.IPEndPoint})");
src/Network/DistributedConnectionManager.cs:270:                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) accepted. (type: {c.Type}, id: {c.Id}");
src/Network/DistributedConnectionManager.cs:280:                Diagnostic.Debug($"Inbound {(c.Obfuscated ? "obfuscated " : string.Empty)}child connection to {username} ({connection.IPEndPoint}) handed off. (old: {c.Id}, new: {connection.Id})");
src/Network/DistributedConnectionManager.cs:294:                        Diagnostic.Debug($"Cancelling pending indirect child connection to {username}");
src/Network/DistributedConnectionManager.cs:304:                        Diagnostic.Debug($"Superseding existing child connection to {username} ({cachedConnection.IPEndPoint}) (old: {c.Id}, new: {connection.Id}");
src/Network/DistributedConnectionManager.cs:571:                    Diagnostic.Debug($"Child connection from {r.Username} ({r.IPEndPoint}) for token {r.Token} ignored; connection already exists.");
src/Network/DistributedConnectionManager.cs:623:                    Diagnostic.Debug($"Falling back to regular inbound indirect child connection to {r.Username} ({r.IPEndPoint}) after obfuscated attempt failed: {ex.Message}");
src/Network/DistributedConnectionManager.cs:632:                Diagnostic.Debug($"Attempting {(useObfuscated ? "obfuscated " : string.Empty)}inbound indirect child connection to {r.Username} ({endPoint}) for token {r.Token}");
src/Network/DistributedConnectionManager.cs:882:            Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:883:            Diagnostic.Info($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}");
src/Network/DistributedConnectionManager.cs:950:                Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1009:            Diagnostic.Debug($"Attempting simultaneous direct and indirect parent candidate connections to {username} ({ipEndPoint})");
src/Network/DistributedConnectionManager.cs:1021:                Diagnostic.Debug($"Adding obfuscated direct parent candidate path to {username} ({obfuscatedEndPoint}) while retaining regular direct and indirect fallback paths");
src/Network/DistributedConnectionManager.cs:1027:                Diagnostic.Debug($"No compatible obfuscated distributed endpoint available for {username} ({ipEndPoint}); using regular direct and indirect parent candidate paths");
src/Network/DistributedConnectionManager.cs:1052:                Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) established first, negotiating parent setup before cancelling remaining candidates.");
src/Network/DistributedConnectionManager.cs:1072:                    Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) initialized.  Waiting for branch information and first search request. (id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1077:                    Diagnostic.Debug($"Failed to negotiate obfuscated parent candidate connection to {username} ({connection.IPEndPoint}); preserving regular fallback candidates: {ex.Message}");
src/Network/DistributedConnectionManager.cs:1107:                Diagnostic.Debug($"Parent candidate connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1120:            Diagnostic.Debug($"Attempting {(obfuscated ? "obfuscated " : string.Empty)}direct parent candidate connection to {username} ({ipEndPoint})");
src/Network/DistributedConnectionManager.cs:1133:                Diagnostic.Debug($"Failed to establish a{(obfuscated ? "n obfuscated" : string.Empty)} direct parent candidate connection to {username} ({ipEndPoint}): {ex.Message}");
src/Network/DistributedConnectionManager.cs:1138:            Diagnostic.Debug($"{(obfuscated ? "Obfuscated d" : "D")}irect parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1146:            Diagnostic.Debug($"Soliciting indirect parent candidate connection to {username} with token {solicitationToken}");
src/Network/DistributedConnectionManager.cs:1166:                Diagnostic.Debug($"Indirect {(incomingConnection.Obfuscated ? "obfuscated " : string.Empty)}parent candidate connection to {username} ({incomingConnection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1171:                Diagnostic.Debug($"Indirect parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1176:                Diagnostic.Debug($"Failed to establish an indirect parent candidate connection to {username} with token {solicitationToken}: {ex.Message}");
src/Network/DistributedConnectionManager.cs:1189:            Diagnostic.Debug($"Parent candidate connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1198:            Diagnostic.Debug($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
src/Network/DistributedConnectionManager.cs:1199:            Diagnostic.Info($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}.");
src/Network/DistributedConnectionManager.cs:1216:                Diagnostic.Debug($"Failed to reconnect to a distributed parent after parent disconnect: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1242:                Diagnostic.Debug($"Failed to broadcast distributed status message: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1260:                Diagnostic.Debug($"Failed to update distributed status from background callback: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1272:                Diagnostic.Debug($"Failed to queue distributed status update: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1284:                Diagnostic.Debug($"Failed to update distributed status from debounce timer: {ex.Message}", ex);
src/Network/DistributedConnectionManager.cs:1327:                Diagnostic.Debug($"Failed to handle message from parent candidate: {ex.Message}", ex);
src/Messaging/Handlers/ServerMessageHandler.cs:369:                            Diagnostic.Debug($"Error handling NetInfo message: {ex.Message}");
src/Messaging/Handlers/ServerMessageHandler.cs:385:                        Diagnostic.Debug($"Received CannotConnect message for token {cannotConnect.Token}{(!string.IsNullOrEmpty(cannotConnect.Username) ? $" from user {cannotConnect.Username}" : string.Empty)}");
src/Messaging/Handlers/ServerMessageHandler.cs:427:                                Diagnostic.Debug($"Received transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token}");
src/Messaging/Handlers/ServerMessageHandler.cs:438:                                        Diagnostic.Debug($"Solicited inbound transfer connection to {download.Username} ({connection.IPEndPoint}) for token {download.Token} (remote: {download.RemoteToken}) established. (id: {connection.Id})");
src/Messaging/Handlers/ServerMessageHandler.cs:443:                                        Diagnostic.Debug($"Transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token} does not match any waiting downloads, discarding.");
src/Messaging/Handlers/ServerMessageHandler.cs:469:                            Diagnostic.Debug($"Error handling ConnectToPeer response from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}): {ex.Message}");
src/Messaging/Handlers/ServerMessageHandler.cs:656:                Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:163:                            Diagnostic.Warning($"Failed to resolve user info response: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:201:                            Diagnostic.Warning($"Error resolving search response for query '{searchRequest.Query}' requested by {connection.Username} with token {searchRequest.Token}: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:218:                            Diagnostic.Warning($"Failed to resolve browse response: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:251:                            Diagnostic.Warning($"Failed to resolve directory contents response: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:265:                                Diagnostic.Warning($"Failed to send directory contents response: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:315:                                Diagnostic.Debug($"Rejecting unknown upload from {connection.Username} for {transferRequest.Filename} with token {transferRequest.Token}");
src/Messaging/Handlers/PeerMessageHandler.cs:340:                        Diagnostic.Debug($"Download of {uploadDeniedResponse.Filename} from {connection.Username} was denied: {uploadDeniedResponse.Message}");
src/Messaging/Handlers/PeerMessageHandler.cs:522:                Diagnostic.Warning($"Failed to invoke QueueDownload action: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:543:                Diagnostic.Warning($"Failed to resolve place in queue for file {filename} from {connection.Username}: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:555:                    Diagnostic.Warning($"Failed to send place in queue response for file {filename} from {connection.Username}: {ex.Message}", ex);
src/Messaging/Handlers/DistributedMessageHandler.cs:329:                Diagnostic.Debug($"Failed to broadcast distributed message: {ex.Message}", ex);

## Public mutable ownership surfaces
examples/Web/api/Room.cs:28:        public IList<string> Operators { get; set; }
examples/Web/api/Room.cs:38:        public IList<UserData> Users { get; set; } = new List<UserData>();
examples/Web/api/Room.cs:43:        public IList<RoomMessage> Messages { get; set; } = new List<RoomMessage>();
examples/Web/api/Program.cs:15:        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
examples/Web/api/SharedFileCache.cs:83:        public IEnumerable<Soulseek.File> Search(SearchQuery query)
examples/Web/api/Trackers/ConversationTracker.cs:16:        public ConcurrentDictionary<string, IList<PrivateMessage>> Conversations { get; } = new ConcurrentDictionary<string, IList<PrivateMessage>>();
examples/Web/api/Trackers/ConversationTracker.cs:45:        public bool TryGet(string username, out IList<PrivateMessage> messages) => Conversations.TryGetValue(username, out messages);
examples/Web/api/DTO/RoomResponse.cs:26:        public IList<string> Operators { get; set; }
examples/Web/api/DTO/RoomResponse.cs:36:        public IEnumerable<UserDataResponse> Users { get; set; } = new List<UserDataResponse>();
examples/Web/api/DTO/RoomResponse.cs:41:        public IEnumerable<RoomMessageResponse> Messages { get; set; } = new List<RoomMessageResponse>();
src/Messaging/Messages/Peer/FolderContentsResponse.cs:43:        public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:70:        public IReadOnlyCollection<Directory> Directories { get; }
src/SoulseekClient.cs:359:        public event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;
src/SoulseekClient.cs:415:        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;
src/SoulseekClient.cs:551:        public IReadOnlyCollection<Transfer> Downloads => DownloadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();
src/SoulseekClient.cs:596:        public IReadOnlyCollection<Transfer> Uploads => UploadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();
src/SoulseekClient.cs:1681:        public Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2094:        public Task<IReadOnlyCollection<SimilarUser>> GetMeshRendezvousUsersAsync(CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2170:        public Task<IReadOnlyCollection<SimilarUser>> GetSimilarUsersAsync(CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2456:        public Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2624:        public Task SendPrivateMessageAsync(IEnumerable<string> usernames, string message, CancellationToken? cancellationToken = null)
src/EventArgs/RoomTickerListReceivedEventArgs.cs:42:        public RoomTickerListReceivedEventArgs(string roomName, IEnumerable<RoomTicker> tickers)
src/EventArgs/RoomTickerListReceivedEventArgs.cs:73:        public IReadOnlyCollection<RoomTicker> Tickers { get; }
src/Messaging/Messages/Server/MessageUsersCommand.cs:40:        public MessageUsersCommand(IEnumerable<string> usernames, string message)
src/Messaging/Messages/Server/MessageUsersCommand.cs:61:        public IReadOnlyCollection<string> Usernames { get; }
src/MeshRendezvousResult.cs:59:        public IReadOnlyCollection<PeerCapabilityRecord> CapabilityRecords { get; }
src/MeshRendezvousResult.cs:69:        public IReadOnlyCollection<SimilarUser> SimilarUsers { get; }
src/DistributedNetworkInfo.cs:122:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => children?
src/Messaging/Messages/EmbeddedMessage.cs:56:        public byte[] DistributedMessage => distributedMessage?.ToArray();
src/Messaging/Messages/Server/SimilarUsersResponse.cs:38:        public static IReadOnlyCollection<SimilarUser> FromByteArray(byte[] bytes)
src/WishlistSearchScheduler.cs:45:        public WishlistSearchScheduler(ISoulseekClient client, IEnumerable<string> terms, WishlistSearchSchedulerOptions options = null)
src/Directory.cs:42:        public Directory(string name, IEnumerable<File> fileList = null)
src/Directory.cs:70:        public IReadOnlyCollection<File> Files { get; }
src/WishlistSearchCompletedEventArgs.cs:34:        public WishlistSearchCompletedEventArgs(string term, Search search, IReadOnlyCollection<SearchResponse> responses, Exception exception)
src/WishlistSearchCompletedEventArgs.cs:57:        public IReadOnlyCollection<SearchResponse> Responses { get; }
src/PeerCapabilityRegistry.cs:54:        public IReadOnlyCollection<PeerCapabilityRecord> Records => records.Values.OrderBy(r => r.Username).ToList().AsReadOnly();
src/PeerCapabilityDescriptor.cs:79:        public IReadOnlyCollection<string> Features { get; }
src/PeerDescriptorSignature.cs:64:        public byte[] PublicKey => publicKey.ToArray();
src/PeerDescriptorSignature.cs:69:        public byte[] Signature => signature.ToArray();
src/File.cs:45:        public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null)
src/File.cs:100:        public IReadOnlyCollection<FileAttribute> Attributes { get; }
src/ItemSimilarUsers.cs:39:        public ItemSimilarUsers(string item, IReadOnlyCollection<string> usernames)
src/ItemSimilarUsers.cs:60:        public IReadOnlyCollection<string> Usernames { get; }
src/UserInterests.cs:40:        public UserInterests(string username, IReadOnlyCollection<string> liked, IReadOnlyCollection<string> hated)
src/UserInterests.cs:63:        public IReadOnlyCollection<string> Hated { get; }
src/UserInterests.cs:68:        public IReadOnlyCollection<string> Liked { get; }
src/ItemRecommendations.cs:39:        public ItemRecommendations(string item, IReadOnlyCollection<Recommendation> recommendations)
src/ItemRecommendations.cs:60:        public IReadOnlyCollection<Recommendation> Recommendations { get; }
src/Messaging/Messages/Server/RoomTickerListNotification.cs:83:        public IReadOnlyCollection<RoomTicker> Tickers { get; }
src/RecommendationList.cs:39:        public RecommendationList(IReadOnlyCollection<Recommendation> recommendations, IReadOnlyCollection<Recommendation> unrecommendations)
src/RecommendationList.cs:61:        public IReadOnlyCollection<Recommendation> Recommendations { get; }
src/RecommendationList.cs:66:        public IReadOnlyCollection<Recommendation> Unrecommendations { get; }
src/Common/WaitKey.cs:42:        public WaitKey(params object[] tokenParts)
src/Common/WaitKey.cs:56:        public object[] TokenParts => tokenParts.ToArray();
src/Messaging/Handlers/ServerMessageHandler.cs:69:        public event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;
src/Messaging/Handlers/ServerMessageHandler.cs:120:        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:40:        public static IReadOnlyCollection<string> FromByteArray(byte[] bytes)
src/BrowseResponse.cs:44:        public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
src/BrowseResponse.cs:70:        public IReadOnlyCollection<Directory> Directories { get; }
src/BrowseResponse.cs:80:        public IReadOnlyCollection<Directory> LockedDirectories { get; }
src/UserInfo.cs:83:        public byte[] Picture => picture == null ? null : (byte[])picture.Clone();
src/Messaging/Messages/Server/NetInfoNotification.cs:45:        public NetInfoNotification(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
src/Messaging/Messages/Server/NetInfoNotification.cs:92:        public IReadOnlyCollection<(string Username, IPAddress IPAddress, int Port)> Parents
src/SearchScope.cs:42:        public SearchScope(SearchScopeType type, params string[] subjects)
src/SearchScope.cs:93:        public IEnumerable<string> Subjects { get; }
src/SearchScope.cs:112:        public static SearchScope User(params string[] usernames) => new SearchScope(SearchScopeType.User, usernames);
src/SearchResponse.cs:49:        public SearchResponse(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
src/SearchResponse.cs:109:        public IReadOnlyCollection<File> Files { get; }
src/SearchResponse.cs:125:        public IReadOnlyCollection<File> LockedFiles { get; }
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:40:        public static IReadOnlyCollection<string> FromByteArray(byte[] bytes)
src/Messaging/Compression/ZStream.cs:78:		public byte[] next_in; // next input byte
src/Messaging/Compression/ZStream.cs:83:		public byte[] next_out; // next output byte should be put there
src/SearchQuery.cs:45:        public SearchQuery(IEnumerable<string> terms, IEnumerable<string> exclusions = null)
src/SearchQuery.cs:69:        public SearchQuery(string query, IEnumerable<string> exclusions)
src/SearchQuery.cs:91:        public IReadOnlyCollection<string> Exclusions { get; }
src/SearchQuery.cs:106:        public IReadOnlyCollection<string> Terms { get; }
src/RoomList.cs:110:        public IReadOnlyCollection<RoomInfo> Public { get; }
src/RoomList.cs:115:        public IReadOnlyCollection<RoomInfo> Private { get; }
src/RoomList.cs:120:        public IReadOnlyCollection<RoomInfo> Owned { get; }
src/RoomList.cs:125:        public IReadOnlyCollection<string> ModeratedRoomNames { get; }
src/RoomInfo.cs:59:        public RoomInfo(string name, IEnumerable<string> userList)
src/RoomInfo.cs:86:        public IReadOnlyCollection<string> Users { get; }
src/RoomData.cs:44:        public RoomData(string name, IEnumerable<UserData> userList, bool isPrivate = false, string owner = null, IEnumerable<string> operatorList = null)
src/RoomData.cs:86:        public IReadOnlyCollection<string> Operators { get; }
src/RoomData.cs:101:        public IReadOnlyCollection<UserData> Users { get; }
src/Options/SoulseekClientOptionsPatch.cs:213:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Network/DistributedConnectionManager.cs:160:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => ChildDictionary.Select(c => (c.Key, c.Value.Snapshot())).ToList().AsReadOnly();
src/Network/DistributedConnectionManager.cs:356:        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates)
src/Options/SoulseekClientOptions.cs:283:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Network/PeerConnectionManager.cs:74:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> MessageConnections => MessageConnectionDictionary.Values
src/Network/MessageConnectionEventArgs.cs:65:        public byte[] Code => code?.ToArray();
src/Network/MessageConnectionEventArgs.cs:102:        public byte[] Message => message?.ToArray();
src/Network/MessageConnectionEventArgs.cs:126:        public byte[] Code => code?.ToArray();
