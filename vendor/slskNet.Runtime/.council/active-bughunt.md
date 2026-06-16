# Active Council Bughunt Candidate Report

This report is not a pass/fail proof. It is a fresh queue of suspicious shapes
that sit outside, or at the edge of, the current closed sweep gates. A green
all-phases council run means registered gates passed; it does not mean these
candidate lines are bugs or that no bugs exist.

Classification rule: any accepted row must be ledgered, fixed with behavior
coverage, sibling-swept, and promoted into a durable gate before closure.

## Event-style async boundaries
src/Messaging/Handlers/ServerMessageHandler.cs:205:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:78:        public async void HandleChildMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:144:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:286:        public async void HandleEmbeddedMessage(byte[] message)
src/Messaging/Handlers/PeerMessageHandler.cs:92:        public async void HandleMessageRead(object sender, byte[] message)
src/Network/PeerConnectionManager.cs:789:        public async void RemoveAndDisposeAll()
src/Network/ListenerHandler.cs:68:        public async void HandleConnection(object sender, IConnection connection)
src/Network/DistributedConnectionManager.cs:700:        public async void RemoveAndDisposeAll()
src/Network/DistributedConnectionManager.cs:1194:        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
src/Network/DistributedConnectionManager.cs:1276:        private async void StatusDebounceTimer_Elapsed(object sender, ElapsedEventArgs e)

## Silent catch or lossy exception boundaries
src/Network/ListenerHandler.cs:261:            catch (Exception)
src/Network/ListenerHandler.cs:262:            {
src/Network/ListenerHandler.cs:263:            }
src/Network/ListenerHandler.cs:270:                catch (Exception)
src/Network/ListenerHandler.cs:271:                {
src/Network/ListenerHandler.cs:272:                }

## Callback/event invocation boundaries
examples/Web/api/SharedFileCache.cs:65:                Refreshed?.Invoke(this, (directoryCount, Files.Count));
src/SearchResponder.cs:51:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/SearchResponder.cs:302:                () => RequestReceived?.Invoke(this, new SearchRequestEventArgs(username, token, query)));
src/SearchResponder.cs:307:                () => ResponseDelivered?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
src/SearchResponder.cs:312:                () => ResponseDeliveryFailed?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
src/Options/TransferOptions.cs:206:                    stateChanged?.Invoke(args);
src/Options/TransferOptions.cs:207:                    StateChanged?.Invoke(args);
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
src/SoulseekClient.cs:3323:                options.ProgressUpdated?.Invoke((e.Username, e.BytesTransferred, e.BytesRemaining, e.PercentComplete, e.Size));
src/SoulseekClient.cs:3688:                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
src/SoulseekClient.cs:3706:                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
src/SoulseekClient.cs:3904:                        options.Reporter?.Invoke(new Transfer(download), attemptedBytes, grantedBytes, actualBytes);
src/SoulseekClient.cs:4709:                options.StateChanged?.Invoke((e.PreviousState, e.Search));
src/SoulseekClient.cs:4746:                        options.ResponseReceived?.Invoke((e.Search, e.Response));
src/SoulseekClient.cs:4904:            => RaiseEventHandler(nameof(PeerCapabilityReceived), () => PeerCapabilityReceived?.Invoke(this, new PeerCapabilityReceivedEventArgs(record)));
src/SoulseekClient.cs:4907:            => RaiseEventHandler(nameof(BrowseProgressUpdated), () => BrowseProgressUpdated?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4910:            => RaiseEventHandler(nameof(Connected), () => Connected?.Invoke(this, EventArgs.Empty));
src/SoulseekClient.cs:4913:            => RaiseEventHandler(nameof(Disconnected), () => Disconnected?.Invoke(this, new SoulseekClientDisconnectedEventArgs(message, exception)));
src/SoulseekClient.cs:4919:                DiagnosticGenerated?.Invoke(sender, eventArgs);
src/SoulseekClient.cs:4940:            => RaiseEventHandler(nameof(LoggedIn), () => LoggedIn?.Invoke(this, EventArgs.Empty));
src/SoulseekClient.cs:4943:            => RaiseEventHandler(nameof(ServerInfoReceived), () => ServerInfoReceived?.Invoke(this, serverInfo));
src/SoulseekClient.cs:4946:            => RaiseEventHandler(nameof(SearchResponseReceived), () => SearchResponseReceived?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4949:            => RaiseEventHandler(nameof(SearchStateChanged), () => SearchStateChanged?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4952:            => RaiseEventHandler(nameof(StateChanged), () => StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, state, message, exception)));
src/SoulseekClient.cs:4955:            => RaiseEventHandler(nameof(TransferProgressUpdated), () => TransferProgressUpdated?.Invoke(this, eventArgs));
src/SoulseekClient.cs:4984:            => RaiseEventHandler(nameof(TransferStateChanged), () => TransferStateChanged?.Invoke(this, eventArgs));
src/SoulseekClient.cs:5172:                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
src/SoulseekClient.cs:5190:                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
src/SoulseekClient.cs:5363:                            options.Reporter?.Invoke(new Transfer(upload), attemptedBytes, grantedBytes, actualBytes);
src/SoulseekClient.cs:5581:                            options.SlotReleased?.Invoke(new Transfer(upload));
src/Network/Tcp/ObfuscatedTransferConnection.cs:202:                reporter?.Invoke(bytesAvailable, bytesGranted, buffer.Length);
src/Network/Tcp/ObfuscatedTransferConnection.cs:267:                reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/WishlistSearchScheduler.cs:225:                    options: options.SearchOptionsFactory?.Invoke(term),
src/WishlistSearchScheduler.cs:230:                SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, null, Array.Empty<SearchResponse>(), ex));
src/WishlistSearchScheduler.cs:234:            SearchCompleted?.Invoke(this, new WishlistSearchCompletedEventArgs(term, result.Search, result.Responses, null));
src/Network/Tcp/Listener.cs:151:                    Accepted?.Invoke(this, eventArgs);
src/PeerCapabilityRegistry.cs:117:                Updated?.Invoke(this, new PeerCapabilityReceivedEventArgs(record));
src/PeerCapabilityRegistry.cs:121:                eventExceptionHandler?.Invoke(nameof(Updated), ex);
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
src/Network/Tcp/Connection.cs:693:                    reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/Network/Tcp/Connection.cs:853:                    reporter?.Invoke(bytesToRead, bytesGranted, bytesRead);
src/Network/Tcp/Connection.cs:914:                .Invoke(this, EventArgs.Empty));
src/Network/Tcp/Connection.cs:923:                        .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:929:                    .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:940:                        .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:946:                    .Invoke(this, new ConnectionDataEventArgs(currentLength, totalLength)));
src/Network/Tcp/Connection.cs:952:                .Invoke(this, new ConnectionDisconnectedEventArgs(message, exception)));
src/Network/Tcp/Connection.cs:956:                .Invoke(this, eventArgs));
src/Network/PeerConnectionManager.cs:63:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
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
src/Messaging/Handlers/DistributedMessageHandler.cs:51:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/Messaging/Handlers/PeerMessageHandler.cs:54:                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
src/Messaging/Handlers/PeerMessageHandler.cs:343:                        DownloadDenied?.Invoke(this, new DownloadDeniedEventArgs(connection.Username, uploadDeniedResponse.Filename, uploadDeniedResponse.Message));
src/Messaging/Handlers/PeerMessageHandler.cs:364:                        DownloadFailed?.Invoke(this, new DownloadFailedEventArgs(connection.Username, uploadFailedResponse.Filename));

## Unisolated server handler event invocations

## Unisolated message connection event invocations

## Unisolated TCP connection event invocations

## Unisolated client lifecycle event invocations

## Unisolated client search event invocations

## Unisolated client transfer/browse event invocations

## Unisolated SoulseekClient bridge event invocations

## Remote/user text in diagnostics or HTTP errors
examples/Web/api/Startup.cs:305:                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [DIAGNOSTIC:{e.GetType().Name}] [{args.Level}] {args.Message}");
examples/Web/api/Startup.cs:365:                Console.WriteLine($"[PUBLIC CHAT] [{args.RoomName}] [{args.Username}]: {args.Message}");
examples/Web/api/Startup.cs:389:                Console.WriteLine($"Disconnected from Soulseek server: {args.Message}");
examples/Web/api/Startup.cs:434:                Console.WriteLine($"[SEARCH RESPONSE DELIVERY] {args.SearchResponse.FileCount + args.SearchResponse.LockedFileCount} files to {args.Username} for query '{args.Query}'");
examples/Web/api/Startup.cs:439:                Console.WriteLine($"[SEARCH RESPONSE DELIVERY FAILED] {args.SearchResponse.FileCount + args.SearchResponse.LockedFileCount} files to {args.Username} for query '{args.Query}'");
examples/Web/api/Startup.cs:630:                Console.WriteLine($"[UPLOAD RE-REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:642:                    Console.WriteLine($"[UPLOAD SLOT REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:657:                    Console.WriteLine($"[UPLOAD SLOT RELEASED] [{username}/{filename}]");
examples/Web/api/Startup.cs:717:                    Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query.SearchText}");
examples/Web/api/SharedFileCache.cs:140:                Console.WriteLine($"[MALFORMED QUERY]: {query} ({ex.Message})");
src/SearchResponder.cs:92:                        Diagnostic.Debug($"Discarded cached search response {responseToken} to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:107:                    Diagnostic.Warning($"Error removing cached search response {responseToken}: {ex.Message}", ex);
src/SearchResponder.cs:139:                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:152:                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");
src/SearchResponder.cs:176:                            Diagnostic.Debug($"Failed to connect to {username} with solicitation token {responseToken} to deliver search results for query '{query}' with token {token}.  Cached response for potential delayed delivery.");
src/SearchResponder.cs:180:                            Diagnostic.Warning($"Error caching undelivered search response {responseToken} for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:189:                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:196:                Diagnostic.Debug($"Failed to send search response to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:237:                    Diagnostic.Warning($"Error retrieving cached search response {responseToken}: {ex.Message}", ex);
src/SearchResponder.cs:250:                        Diagnostic.Debug($"Sent cached response {responseToken} containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:256:                        Diagnostic.Debug($"Failed to send cached search response {responseToken} to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:322:                Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
src/SoulseekClient.cs:189:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} reported as failed by remote client (token: {download.Token})");
src/SoulseekClient.cs:194:                    Diagnostic.Warning($"Failed to mark download(s) failed: {ex.Message}", ex);
src/SoulseekClient.cs:215:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} rejected by remote client (token: {download.Token})");
src/SoulseekClient.cs:220:                    Diagnostic.Warning($"Failed to mark download(s) rejected: {ex.Message}", ex);
src/SoulseekClient.cs:3256:                Diagnostic.Debug($"Acknowledged private message ID {privateMessageId}");
src/SoulseekClient.cs:3620:                    Diagnostic.Debug($"Invalidated message connection cache for {username}");
src/SoulseekClient.cs:3724:                Diagnostic.Debug($"Global download semaphore for file {GetDiagnosticLogValue(download.Filename)} to {username} acquired");
src/SoulseekClient.cs:3728:                Diagnostic.Debug($"Fetched peer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3739:                Diagnostic.Debug($"Wrote transfer request for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3744:                Diagnostic.Debug($"Received transfer request ACK for download of {GetDiagnosticLogValue(download.Filename)} from {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:3768:                    Diagnostic.Debug($"Fetched transfer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
src/SoulseekClient.cs:3800:                    Diagnostic.Debug($"Fetched peer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");
src/SoulseekClient.cs:3812:                        Diagnostic.Debug($"Fetched transfer connection for download of {GetDiagnosticLogValue(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
src/SoulseekClient.cs:3818:                        Diagnostic.Warning($"Attempting to initiate a second-chance transfer connection to {username} for download of {GetDiagnosticLogValue(download.Filename)}");
src/SoulseekClient.cs:3824:                        Diagnostic.Warning($"Successfully established a second-chance transfer connection to {username} for download of {GetDiagnosticLogValue(download.Filename)}");
src/SoulseekClient.cs:3881:                    Diagnostic.Debug($"Seeking output stream for download of {GetDiagnosticLogValue(download.Filename)} from {username} to starting offset of {download.StartOffset} bytes");
src/SoulseekClient.cs:3885:                Diagnostic.Debug($"Seeking download of {GetDiagnosticLogValue(download.Filename)} from {username} to starting offset of {download.StartOffset} bytes");
src/SoulseekClient.cs:3937:                Diagnostic.Info($"Download of {GetDiagnosticLogValue(download.Filename)} from {username} complete ({outputStream.Position} of {download.Size} bytes).");
src/SoulseekClient.cs:4016:                        Diagnostic.Warning($"Failed to cancel wait for key {transferStartRequestedWaitKey}: {ex.Message}");
src/SoulseekClient.cs:4025:                        Diagnostic.Warning($"Failed to dispose transfer connection for file {GetDiagnosticLogValue(remoteFilename)} from user {username}: {ex.Message}");
src/SoulseekClient.cs:4039:                        Diagnostic.Warning($"Failed to determine final position of output stream for file {GetDiagnosticLogValue(download.Filename)} from {username}: {ex.Message}", ex);
src/SoulseekClient.cs:4061:                            Diagnostic.Warning($"Failed to finalize output stream for file {GetDiagnosticLogValue(download.Filename)} from {username}: {ex.Message}", ex);
src/SoulseekClient.cs:4076:                            Diagnostic.Debug($"Global download semaphore for file {GetDiagnosticLogValue(download.Filename)} from {username} released");
src/SoulseekClient.cs:4080:                            Diagnostic.Warning($"Failed to release global download semaphore for file {GetDiagnosticLogValue(download.Filename)} to {username}: {ex.Message}");
src/SoulseekClient.cs:4192:                    Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
src/SoulseekClient.cs:4219:                        Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
src/SoulseekClient.cs:4227:                    Diagnostic.Debug($"EndPoint cache MISS for {username}: {endPoint}");
src/SoulseekClient.cs:4885:                Diagnostic.Warning($"Rejected peer capability message from {username}: descriptor signature is invalid.");
src/SoulseekClient.cs:4988:            Diagnostic.Warning($"Unhandled exception in {eventName} event handler: {ex.Message}", ex);
src/SoulseekClient.cs:5227:                Diagnostic.Debug($"Upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5234:                    Diagnostic.Debug($"Upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5247:                Diagnostic.Debug($"Global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} acquired");
src/SoulseekClient.cs:5254:                Diagnostic.Debug($"Fetched peer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");
src/SoulseekClient.cs:5264:                Diagnostic.Debug($"Wrote transfer request for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");
src/SoulseekClient.cs:5269:                Diagnostic.Debug($"Received transfer request ACK for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:5281:                Diagnostic.Debug($"Fetched transfer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} (id: {upload.Connection.Id}, state: {upload.Connection.State})");
src/SoulseekClient.cs:5313:                    Diagnostic.Debug($"Failed to read start offset for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}");
src/SoulseekClient.cs:5329:                Diagnostic.Debug($"Resolving input stream for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}");
src/SoulseekClient.cs:5339:                    Diagnostic.Debug($"Seeking input stream for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} to starting offset of {upload.StartOffset} bytes");
src/SoulseekClient.cs:5403:                            Diagnostic.Warning($"Transfer connection for upload of {GetDiagnosticLogValue(upload.Filename)} to {username} forcibly closed after exceeding maximum linger time of {options.MaximumLingerTime}ms.");
src/SoulseekClient.cs:5419:                Diagnostic.Info($"Upload of {GetDiagnosticLogValue(upload.Filename)} to {username} complete ({inputStream.Position} of {upload.Size} bytes).");
src/SoulseekClient.cs:5488:                        Diagnostic.Warning($"Failed to dispose transfer connection for file {GetDiagnosticLogValue(remoteFilename)} to user {username}: {ex.Message}");
src/SoulseekClient.cs:5502:                        Diagnostic.Warning($"Failed to determine final position of input stream for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5517:                            Diagnostic.Warning($"Failed to finalize input stream for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5561:                            Diagnostic.Debug($"Upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5566:                            Diagnostic.Warning($"Failed to release upload semaphore for user {username}: {ex.Message}");
src/SoulseekClient.cs:5579:                            Diagnostic.Debug($"Upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5585:                            Diagnostic.Warning($"Encountered Exception releasing upload slot for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}", ex);
src/SoulseekClient.cs:5594:                            Diagnostic.Debug($"Global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username} released");
src/SoulseekClient.cs:5598:                            Diagnostic.Warning($"Failed to release global upload semaphore for file {GetDiagnosticLogValue(upload.Filename)} to {username}: {ex.Message}");
src/Network/ListenerHandler.cs:166:                            Diagnostic.Debug($"Unexpected transfer connection for token {peerInit.Token} from {peerInit.Username} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:189:                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:196:                            Diagnostic.Debug($"Obfuscated distributed PierceFirewall with token {pierceFirewall.Token} accepted from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}); completing solicited distributed wait. (id: {connection.Id})");
src/Network/ListenerHandler.cs:199:                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:208:                        Diagnostic.Debug($"PierceFirewall matching pending search response received from {username} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:226:                        Diagnostic.Debug($"Unknown PierceFirewall with token {pierceFirewall.Token} accepted as provisional peer message connection from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port} (id: {connection.Id})");
src/Network/ListenerHandler.cs:245:                Diagnostic.Debug($"Failed to initialize direct connection from {GetConnectionDescription(connection)}: {ex.Message}");
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
src/Messaging/Handlers/DistributedMessageHandler.cs:345:                Diagnostic.Debug($"Ignored distributed search request with invalid token from {source}");
src/Messaging/Handlers/DistributedMessageHandler.cs:358:                Diagnostic.Debug($"Failed to broadcast distributed message: {ex.Message}", ex);

## Public mutable ownership surfaces
examples/Web/api/Program.cs:15:        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
src/UserInfo.cs:73:        public byte[] Picture => picture == null ? null : (byte[])picture.Clone();
examples/Web/api/Trackers/ConversationTracker.cs:16:        public ConcurrentDictionary<string, IList<PrivateMessage>> Conversations { get; } = new ConcurrentDictionary<string, IList<PrivateMessage>>();
examples/Web/api/Trackers/ConversationTracker.cs:45:        public bool TryGet(string username, out IList<PrivateMessage> messages) => Conversations.TryGetValue(username, out messages);
src/SearchScope.cs:42:        public SearchScope(SearchScopeType type, params string[] subjects)
src/SearchScope.cs:93:        public IEnumerable<string> Subjects { get; }
src/SearchScope.cs:112:        public static SearchScope User(params string[] usernames) => new SearchScope(SearchScopeType.User, usernames);
src/SearchQuery.cs:45:        public SearchQuery(IEnumerable<string> terms, IEnumerable<string> exclusions = null)
src/SearchQuery.cs:69:        public SearchQuery(string query, IEnumerable<string> exclusions)
src/SearchQuery.cs:91:        public IReadOnlyCollection<string> Exclusions { get; }
src/SearchQuery.cs:106:        public IReadOnlyCollection<string> Terms { get; }
examples/Web/api/SharedFileCache.cs:83:        public IEnumerable<Soulseek.File> Search(SearchQuery query)
src/RoomList.cs:110:        public IReadOnlyCollection<RoomInfo> Public { get; }
src/RoomList.cs:115:        public IReadOnlyCollection<RoomInfo> Private { get; }
src/RoomList.cs:120:        public IReadOnlyCollection<RoomInfo> Owned { get; }
src/RoomList.cs:125:        public IReadOnlyCollection<string> ModeratedRoomNames { get; }
src/RoomData.cs:44:        public RoomData(string name, IEnumerable<UserData> userList, bool isPrivate = false, string owner = null, IEnumerable<string> operatorList = null)
src/RoomData.cs:86:        public IReadOnlyCollection<string> Operators { get; }
src/RoomData.cs:101:        public IReadOnlyCollection<UserData> Users { get; }
src/SoulseekClient.cs:359:        public event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;
src/SoulseekClient.cs:415:        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;
src/SoulseekClient.cs:551:        public IReadOnlyCollection<Transfer> Downloads => DownloadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();
src/SoulseekClient.cs:596:        public IReadOnlyCollection<Transfer> Uploads => UploadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();
src/SoulseekClient.cs:1671:        public Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2084:        public Task<IReadOnlyCollection<SimilarUser>> GetMeshRendezvousUsersAsync(CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2160:        public Task<IReadOnlyCollection<SimilarUser>> GetSimilarUsersAsync(CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2446:        public Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2614:        public Task SendPrivateMessageAsync(IEnumerable<string> usernames, string message, CancellationToken? cancellationToken = null)
src/SearchResponse.cs:49:        public SearchResponse(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
src/SearchResponse.cs:99:        public IReadOnlyCollection<File> Files { get; }
src/SearchResponse.cs:115:        public IReadOnlyCollection<File> LockedFiles { get; }
src/Options/SoulseekClientOptionsPatch.cs:213:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/RoomInfo.cs:54:        public RoomInfo(string name, IEnumerable<string> userList)
src/RoomInfo.cs:81:        public IReadOnlyCollection<string> Users { get; }
src/File.cs:45:        public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null)
src/File.cs:95:        public IReadOnlyCollection<FileAttribute> Attributes { get; }
src/Options/SoulseekClientOptions.cs:283:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
examples/Web/api/DTO/RoomResponse.cs:26:        public IList<string> Operators { get; set; }
examples/Web/api/DTO/RoomResponse.cs:36:        public IEnumerable<UserDataResponse> Users { get; set; } = new List<UserDataResponse>();
examples/Web/api/DTO/RoomResponse.cs:41:        public IEnumerable<RoomMessageResponse> Messages { get; set; } = new List<RoomMessageResponse>();
src/MeshRendezvousResult.cs:59:        public IReadOnlyCollection<PeerCapabilityRecord> CapabilityRecords { get; }
src/MeshRendezvousResult.cs:69:        public IReadOnlyCollection<SimilarUser> SimilarUsers { get; }
src/PeerDescriptorSignature.cs:64:        public byte[] PublicKey => publicKey.ToArray();
src/PeerDescriptorSignature.cs:69:        public byte[] Signature => signature.ToArray();
src/BrowseResponse.cs:44:        public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
src/BrowseResponse.cs:70:        public IReadOnlyCollection<Directory> Directories { get; }
src/BrowseResponse.cs:80:        public IReadOnlyCollection<Directory> LockedDirectories { get; }
src/WishlistSearchScheduler.cs:45:        public WishlistSearchScheduler(ISoulseekClient client, IEnumerable<string> terms, WishlistSearchSchedulerOptions options = null)
src/WishlistSearchCompletedEventArgs.cs:34:        public WishlistSearchCompletedEventArgs(string term, Search search, IReadOnlyCollection<SearchResponse> responses, Exception exception)
src/WishlistSearchCompletedEventArgs.cs:57:        public IReadOnlyCollection<SearchResponse> Responses { get; }
src/ItemSimilarUsers.cs:39:        public ItemSimilarUsers(string item, IReadOnlyCollection<string> usernames)
src/ItemSimilarUsers.cs:60:        public IReadOnlyCollection<string> Usernames { get; }
src/UserInterests.cs:40:        public UserInterests(string username, IReadOnlyCollection<string> liked, IReadOnlyCollection<string> hated)
src/UserInterests.cs:63:        public IReadOnlyCollection<string> Hated { get; }
src/UserInterests.cs:68:        public IReadOnlyCollection<string> Liked { get; }
src/ItemRecommendations.cs:39:        public ItemRecommendations(string item, IReadOnlyCollection<Recommendation> recommendations)
src/ItemRecommendations.cs:60:        public IReadOnlyCollection<Recommendation> Recommendations { get; }
src/PeerCapabilityRegistry.cs:54:        public IReadOnlyCollection<PeerCapabilityRecord> Records => records.Values.OrderBy(r => r.Username).ToList().AsReadOnly();
src/RecommendationList.cs:39:        public RecommendationList(IReadOnlyCollection<Recommendation> recommendations, IReadOnlyCollection<Recommendation> unrecommendations)
src/RecommendationList.cs:61:        public IReadOnlyCollection<Recommendation> Recommendations { get; }
src/RecommendationList.cs:66:        public IReadOnlyCollection<Recommendation> Unrecommendations { get; }
src/PeerCapabilityDescriptor.cs:79:        public IReadOnlyCollection<string> Features { get; }
src/Common/WaitKey.cs:42:        public WaitKey(params object[] tokenParts)
src/Common/WaitKey.cs:56:        public object[] TokenParts => tokenParts.ToArray();
examples/Web/api/Room.cs:28:        public IList<string> Operators { get; set; }
examples/Web/api/Room.cs:38:        public IList<UserData> Users { get; set; } = new List<UserData>();
examples/Web/api/Room.cs:43:        public IList<RoomMessage> Messages { get; set; } = new List<RoomMessage>();
src/Network/PeerConnectionManager.cs:74:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> MessageConnections => MessageConnectionDictionary.Values
src/Network/MessageConnectionEventArgs.cs:65:        public byte[] Code => code?.ToArray();
src/Network/MessageConnectionEventArgs.cs:102:        public byte[] Message => message?.ToArray();
src/Network/MessageConnectionEventArgs.cs:126:        public byte[] Code => code?.ToArray();
src/Network/DistributedConnectionManager.cs:160:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => ChildDictionary.Select(c => (c.Key, c.Value.Snapshot())).ToList().AsReadOnly();
src/Network/DistributedConnectionManager.cs:356:        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates)
src/Directory.cs:42:        public Directory(string name, IEnumerable<File> fileList = null)
src/Directory.cs:70:        public IReadOnlyCollection<File> Files { get; }
src/DistributedNetworkInfo.cs:122:        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => children?
src/EventArgs/RoomTickerListReceivedEventArgs.cs:42:        public RoomTickerListReceivedEventArgs(string roomName, IEnumerable<RoomTicker> tickers)
src/EventArgs/RoomTickerListReceivedEventArgs.cs:73:        public IReadOnlyCollection<RoomTicker> Tickers { get; }
src/Messaging/Handlers/ServerMessageHandler.cs:69:        public event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;
src/Messaging/Handlers/ServerMessageHandler.cs:120:        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;
src/Messaging/Messages/Peer/FolderContentsResponse.cs:43:        public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:70:        public IReadOnlyCollection<Directory> Directories { get; }
src/Messaging/Messages/Server/MessageUsersCommand.cs:40:        public MessageUsersCommand(IEnumerable<string> usernames, string message)
src/Messaging/Messages/Server/MessageUsersCommand.cs:61:        public IReadOnlyCollection<string> Usernames { get; }
src/Messaging/Compression/ZStream.cs:78:		public byte[] next_in; // next input byte
src/Messaging/Compression/ZStream.cs:83:		public byte[] next_out; // next output byte should be put there
src/Messaging/Messages/Server/SimilarUsersResponse.cs:38:        public static IReadOnlyCollection<SimilarUser> FromByteArray(byte[] bytes)
src/Messaging/Messages/EmbeddedMessage.cs:56:        public byte[] DistributedMessage => distributedMessage?.ToArray();
src/Messaging/Messages/Server/RoomTickerListNotification.cs:83:        public IReadOnlyCollection<RoomTicker> Tickers { get; }
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:40:        public static IReadOnlyCollection<string> FromByteArray(byte[] bytes)
src/Messaging/Messages/Server/NetInfoNotification.cs:45:        public NetInfoNotification(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
src/Messaging/Messages/Server/NetInfoNotification.cs:92:        public IReadOnlyCollection<(string Username, IPAddress IPAddress, int Port)> Parents
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:40:        public static IReadOnlyCollection<string> FromByteArray(byte[] bytes)

## Red-team abuse lens
scripts/check-local-identity-leaks.sh:17:tmp_tokens="$(mktemp)"
scripts/check-local-identity-leaks.sh:20:trap 'rm -f "$tmp_tokens" "$tmp_commits" "$tmp_files"' EXIT
scripts/check-local-identity-leaks.sh:22:add_token() {
scripts/check-local-identity-leaks.sh:23:  local token="$1"
scripts/check-local-identity-leaks.sh:24:  token="${token//$'\n'/}"
scripts/check-local-identity-leaks.sh:25:  token="${token//$'\r'/}"
scripts/check-local-identity-leaks.sh:26:  [[ ${#token} -ge 3 ]] || return 0
scripts/check-local-identity-leaks.sh:27:  case "$token" in
scripts/check-local-identity-leaks.sh:32:  printf '%s\n' "$token" >>"$tmp_tokens"
scripts/check-local-identity-leaks.sh:35:add_token "${LOCAL_IDENTITY_DENYLIST:-}"
scripts/check-local-identity-leaks.sh:36:add_token "${SLSKDN_LOCAL_IDENTITY_DENYLIST:-}"
scripts/check-local-identity-leaks.sh:37:add_token "${SLSKDN_FORBIDDEN_LOCAL_HOSTNAME:-}"
scripts/check-local-identity-leaks.sh:38:add_token "$(hostname -s 2>/dev/null || true)"
scripts/check-local-identity-leaks.sh:39:add_token "${USER:-}"
scripts/check-local-identity-leaks.sh:40:add_token "$(id -un 2>/dev/null || true)"
scripts/check-local-identity-leaks.sh:41:add_token "$(basename "${HOME:-}" 2>/dev/null || true)"
scripts/check-local-identity-leaks.sh:43:read_csv_tokens() {
scripts/check-local-identity-leaks.sh:46:  IFS=',' read -ra tokens <<<"$value"
scripts/check-local-identity-leaks.sh:47:  for token in "${tokens[@]}"; do
scripts/check-local-identity-leaks.sh:48:    add_token "$token"
scripts/check-local-identity-leaks.sh:52:read_csv_tokens "${LOCAL_IDENTITY_DENYLIST:-}"
scripts/check-local-identity-leaks.sh:53:read_csv_tokens "${SLSKDN_LOCAL_IDENTITY_DENYLIST:-}"
scripts/check-local-identity-leaks.sh:58:  while IFS= read -r token; do
scripts/check-local-identity-leaks.sh:59:    [[ "$token" =~ ^[[:space:]]*# ]] && continue
scripts/check-local-identity-leaks.sh:60:    add_token "$token"
scripts/check-local-identity-leaks.sh:67:sort -u "$tmp_tokens" -o "$tmp_tokens"
scripts/check-local-identity-leaks.sh:68:if [[ ! -s "$tmp_tokens" ]]; then
scripts/check-local-identity-leaks.sh:69:  echo "No local identity tokens configured for scanning."
scripts/check-local-identity-leaks.sh:77:  local path="$2"
scripts/check-local-identity-leaks.sh:78:  local display_path="${3:-$path}"
scripts/check-local-identity-leaks.sh:81:  [[ -f "$path" ]] || return 0
scripts/check-local-identity-leaks.sh:83:    rg --json --fixed-strings --ignore-case --file "$tmp_tokens" "$path" |
scripts/check-local-identity-leaks.sh:84:      jq -r --arg label "$label" --arg display_path "$display_path" 'select(.type == "match") | "\($label): \($display_path):\(.data.line_number)"' |
scripts/check-local-identity-leaks.sh:96:  trap 'rm -f "$tmp_tokens" "$tmp_commits" "$tmp_files" "$tmp_unreleased"' EXIT
scripts/check-local-identity-leaks.sh:117:  -path './.git' -prune -o \
scripts/check-local-identity-leaks.sh:118:  -path './node_modules' -prune -o \
scripts/check-local-identity-leaks.sh:119:  -path './vendor' -prune -o \
scripts/check-local-identity-leaks.sh:120:  -path './target' -prune -o \
scripts/check-local-identity-leaks.sh:121:  -path './dist' -prune -o \
scripts/check-local-identity-leaks.sh:122:  -path './build' -prune -o \
scripts/check-local-identity-leaks.sh:123:  -path './zeek/pkg' -prune -o \
scripts/check-local-identity-leaks.sh:125:    -path './.github/release-notes/*' -o \
scripts/check-local-identity-leaks.sh:126:    -path './docs/dev/release-copy.md' -o \
scripts/check-local-identity-leaks.sh:127:    -path './docs/release*.md' -o \
scripts/check-local-identity-leaks.sh:128:    -path './docs/RELEASE*.md' -o \
scripts/check-local-identity-leaks.sh:129:    -path './packaging/winget/*' \
scripts/check-local-identity-leaks.sh:132:while IFS= read -r path; do
scripts/check-local-identity-leaks.sh:133:  [[ -n "$path" ]] || continue
scripts/check-local-identity-leaks.sh:134:  check_file "$path" "$path"
src/UserStatus.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/UserStatistics.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
docs/dev/bug-council-active-classification-2026-05-06.md:24:| RT-129 | Callback/event invocation boundaries | `DiagnosticFactory` invoked diagnostic event callbacks directly; components that pass `DiagnosticGenerated?.Invoke(...)` could let a throwing diagnostic subscriber interrupt ordinary diagnostic logging and the runtime path that produced it. | Medium | Proven | Fixed |
docs/dev/bug-council-active-classification-2026-05-06.md:25:| RT-130 | Remote/user text in diagnostics or HTTP errors | Transfer diagnostics mostly used basenames but several warning/debug paths logged raw transfer filename values, and `Path.GetFileName` does not strip Soulseek backslashes on every host platform. | Medium | Proven | Fixed |
docs/dev/bug-council-active-classification-2026-05-06.md:28:| RT-133 | Remote/user text in diagnostics or HTTP errors | Runtime search diagnostics and wrapped search failures logged raw search text even though token/count metadata is enough to correlate search lifecycle events. | Medium | Proven | Fixed |
docs/dev/bug-council-active-classification-2026-05-06.md:37:| `src/Network/ListenerHandler.cs:248` | Existing guard | Best-effort disconnect during failed listener initialization; the failure path already emits `Failed to initialize direct connection...` before cleanup. | Covered by listener handler diagnostic-boundary tests. |
docs/dev/bug-council-active-classification-2026-05-06.md:68:Classification marker: `Remote/user text in diagnostics or HTTP errors: accepted transfer filename and search text subgroups`.
docs/dev/bug-council-active-classification-2026-05-06.md:70:The accepted subgroups were transfer filename/path diagnostics and raw search
docs/dev/bug-council-active-classification-2026-05-06.md:71:text diagnostics. Follow-up RT-134 corrected that policy: Runtime diagnostics must preserve operator-visible values such as usernames, filenames, paths, and search text so
docs/dev/bug-council-active-classification-2026-05-06.md:73:keys, API tokens, and equivalent secret material should be withheld from logs;
docs/dev/bug-council-active-classification-2026-05-06.md:77:protocol tokens, exception messages, and example Web API response text that need
src/UserInfo.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
scripts/check-live-vpn-configs.sh:5:"$repo_root/scripts/prepare-live-vpn-secrets.sh" >/dev/null
scripts/check-live-vpn-configs.sh:7:manifest="${SLSKNET_RUNTIME_VPN_SECRET_DIR:-$repo_root/.secrets/vpn}/live-vpn.env"
scripts/check-live-vpn-configs.sh:9:# shellcheck disable=SC1090
scripts/check-live-vpn-configs.sh:27:resolve_path() {
scripts/check-live-vpn-configs.sh:28:    local path="$1"
scripts/check-live-vpn-configs.sh:29:    if [[ "$path" == /* ]]; then
scripts/check-live-vpn-configs.sh:30:        printf '%s' "$path"
scripts/check-live-vpn-configs.sh:32:        printf '%s/%s' "$repo_root" "$path"
scripts/check-live-vpn-configs.sh:56:    config="$(resolve_path "$config")"
src/UserData.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
scripts/prepare-live-vpn-secrets.sh:5:secret_dir="${SLSKNET_RUNTIME_VPN_SECRET_DIR:-$repo_root/.secrets/vpn}"
scripts/prepare-live-vpn-secrets.sh:6:manifest="$secret_dir/live-vpn.env"
scripts/prepare-live-vpn-secrets.sh:8:mkdir -p "$secret_dir"
scripts/prepare-live-vpn-secrets.sh:9:chmod 700 "$repo_root/.secrets" "$secret_dir"
scripts/prepare-live-vpn-secrets.sh:14:    local output="$secret_dir/slsknet-$index.conf"
scripts/prepare-live-vpn-secrets.sh:35:SLSKNET_RUNTIME_VPN_CONFIG_1=.secrets/vpn/slsknet-1.conf
scripts/prepare-live-vpn-secrets.sh:36:SLSKNET_RUNTIME_VPN_CONFIG_2=.secrets/vpn/slsknet-2.conf
scripts/prepare-live-vpn-secrets.sh:37:SLSKNET_RUNTIME_VPN_CONFIG_3=.secrets/vpn/slsknet-3.conf
scripts/prepare-live-vpn-secrets.sh:47:    path="$secret_dir/slsknet-$index.conf"
scripts/prepare-live-vpn-secrets.sh:48:    if [[ ! -f "$path" ]]; then
scripts/prepare-live-vpn-secrets.sh:49:        printf 'Missing live VPN config %s at %s\n' "$index" "$path" >&2
scripts/prepare-live-vpn-secrets.sh:58:printf 'Live VPN secrets ready: %s\n' "$secret_dir"
docs/dev/bug-council-scan-registry.md:31:| Lifecycle cancellation registration | Find cancellation source and token registration ownership points. |
docs/dev/bug-council-scan-registry.md:39:| Transfer stream factory | Find transfer input/output stream factory ownership and lifecycle paths. |
docs/dev/bug-council-scan-registry.md:40:| Example Web API path/request/lifecycle | Find path containment, request validation, and disposable ownership issues in the example app. |
docs/dev/bug-council-scan-registry.md:41:| Example Web API path/shared files | Find shared-file path advertisement, containment, and resolver output issues in the example app. |
docs/dev/bug-council-scan-registry.md:45:| Security-sensitive material | Find high-confidence private keys and token patterns. |
docs/dev/bug-council-scan-registry.md:56:| Red-team abuse lens | Re-check accepted fixes from an attacker viewpoint: spoofed identity, secret disclosure, confused deputy, replay, SSRF/path/process escape, and operational downgrade. |
docs/dev/bug-council-severity-schema.md:12:| Low | Defensive-depth gap: code path is currently unreachable from untrusted input, but the absence of the guard is itself a hazard if a refactor exposes it. |
docs/dev/bug-council-severity-schema.md:15:Pick the **worst plausible** severity given current code paths. If the same code is reachable from two boundaries with different severities, take the higher.
docs/dev/bug-council-sweep-webapi-2026-05-05.md:11:- `Example Web API path, request, and lifecycle candidates`
docs/dev/bug-council-sweep-webapi-2026-05-05.md:12:- `Example Web API path and shared-file candidates`
docs/dev/bug-council-sweep-webapi-2026-05-05.md:19:- Example Web API path, request, and lifecycle candidates: 390/390 classified
docs/dev/bug-council-sweep-webapi-2026-05-05.md:20:- Example Web API path and shared-file candidates: 177/177 classified
docs/dev/bug-council-sweep-webapi-2026-05-05.md:26:This sweep closes the broad example Web API scan by splitting path/shared-file advertisement, controller request validation, transfer lifecycle ownership, and tracker state into stable subgroups. The broad count increased during the sweep because the fixes add shared-path helpers, route-validation helpers, and focused regression tests that are themselves classified by the same sections.
docs/dev/bug-council-sweep-webapi-2026-05-05.md:32:| `examples/Web/api/SharedFileCache.cs:55` | Fixed | RT-081 | Shared search results now advertise paths relative to the configured shared root instead of leaking absolute local filesystem paths. |
docs/dev/bug-council-sweep-webapi-2026-05-05.md:46:- Download output paths use `GetSafeOutputPath`, normalize absolute remote names into relative output paths, and defer file creation until the stream factory is invoked.
docs/dev/bug-council-sweep-webapi-2026-05-05.md:48:- Transfer lifecycle code disposes untracked or replaced cancellation token sources and tracker removals dispose tracked sources.
docs/dev/bug-council-sweep-webapi-2026-05-05.md:49:- Tracker state paths reject invalid room message limits, null room/conversation/browse payloads, and normalize missing room/conversation lists.
docs/dev/bug-council-sweep-webapi-2026-05-05.md:57:- Test fixture temporary path and assertion hits are regression coverage for already-classified path/request/lifecycle behavior.
docs/dev/bug-council-sweep-example-web-api-2026-05-05.md:7:- Example Web API path, request, and lifecycle candidates: 390/390 classified
docs/dev/bug-council-sweep-example-web-api-2026-05-05.md:8:- Example Web API path and shared-file candidates: 177/177 classified
docs/dev/bug-council-active-backlog.md:30:| `Event-style async boundaries` | 10 | Open | Event-handler and timer callbacks remain a broad lifecycle queue. Several known handler paths are already diagnostic-wrapped, but this pile needs a whole-section pass rather than one callback at a time. | Split into event-handler, timer, and disposal subgroups; accept only paths where exceptions can escape without diagnostics or leave state half-updated. |
docs/dev/bug-council-active-backlog.md:31:| `Silent catch or lossy exception boundaries` | 6 | Open | Remaining empty catches are listener initialization cleanup paths that intentionally avoid masking the already-diagnosed initialization failure. The distributed parent reconnect swallow was accepted and fixed as RT-132. | Keep a narrow cleanup-catch gate; accept only future empty catches that hide non-cleanup runtime failures. |
docs/dev/bug-council-active-backlog.md:40:| `Remote/user text in diagnostics or HTTP errors` | 182 | Open | Broad privacy/logging queue. Some runtime diagnostics intentionally include remote usernames or local filenames; example Web API console output has different risk and should be reviewed separately. Runtime transfer filename and search text subgroups have been accepted and fixed. | Split runtime diagnostics from example Web API output; accept only high-confidence sensitive token, full path, raw query, or protocol-secret leaks. |
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:30:| `src/Common/WaitKey.cs:94` | Fixed | RT-086 | Wait-key hash codes now use ordinal string hashing to match token equality. |
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:39:- `src/Common/WaitKey.cs:54` returns a copied token-parts array and the constructor snapshots params input.
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:41:- `src/Common/WaitKey.cs:56`, `src/Common/WaitKey.cs:61`, and `src/Common/WaitKey.cs:81` compare through `object.Equals` or ordinal token equality and handle null operands safely.
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:49:- `scripts/scan-bug-council-candidates.sh:142` is the scanner's own high-confidence secret regex.
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:50:- `scripts/check-remediation-baseline.sh:382` is the remediation baseline's own high-confidence secret regex.
docs/dev/bug-council-sweep-residual-small-2026-05-05.md:60:| `src/Common/WaitKey.cs:94` | Medium | Proven | Fixed | RT-086 | Wait-key hash matched token equality only under invariant culture. Ordinal hashing matches ordinal equality. |
examples/Web/web/.gitignore:1:# See https://help.github.com/articles/ignoring-files/ for more about ignoring files.
docs/dev/bug-council-roslyn-analyzers.md:21:| CSL0004 | TaintToFilePath | High | Network-derived file or directory path without a sanctioned containment validator. Sinks include common `File.*`, `Directory.*`, `FileInfo`, `DirectoryInfo`, and `FileStream` path entry points. Sanctioned validators include explicit contained/safe path resolver methods. |
docs/dev/bug-council-roslyn-analyzers.md:30:| CSL0013 | TaintToDynamicExecution | High | Network-derived reflection, assembly loading, type lookup, or process execution input without allowlist validation. |
scripts/check-bug-council-all-phases.sh:42:  printf 'Council all-phases runner is missing or not executable: %s\n' "${runner#$repo_root/}" >&2
docs/dev/bug-council-sweep-lifecycle-2026-05-05.md:34:| `src/Network/Tcp/Connection.cs:481` | Fixed | RT-076 | `WaitForDisconnect` now scopes cancellation registrations to the wait task and disposes them after completion instead of retaining callbacks for the lifetime of the token source. |
docs/dev/bug-council-sweep-lifecycle-2026-05-05.md:46:- Transfer enqueue and disconnect races use `Task.WhenAny` with linked cancellation, and upload/download stream paths observe the linked race token.
docs/dev/bug-council-sweep-lifecycle-2026-05-05.md:48:- `TokenBucket` races reset waits against cancellation with scoped token registrations and releases reset waiters on disposal.
docs/dev/bug-council-sweep-lifecycle-2026-05-05.md:49:- Scheduler, connection manager, and client timers/semaphores are disposed by their owning lifecycle objects; prior sweeps cover post-dispose wait creation and token-bucket wait release.
docs/dev/bug-council-sweep-lifecycle-2026-05-05.md:50:- Remaining `async void` hits are event handler entry points that wrap exceptions into diagnostics or manager cleanup paths; they remain tracked by this sweep and existing focused tests.
scripts/check-council-negative-space.sh:130:# invalid; the validator is the negative path of resolver dispatch).
scripts/check-council-negative-space.sh:160:# Mythos-level analyzer (CSL0004) - file/directory path sink lens.
scripts/check-council-negative-space.sh:162:  "csl0004-taint-to-file-path" \
scripts/check-council-negative-space.sh:165:assert_baseline_anchor "csl0004-taint-to-file-path" "CSL0004"
scripts/check-council-negative-space.sh:218:  "csl0013-taint-to-dynamic-execution" \
scripts/check-council-negative-space.sh:221:assert_baseline_anchor "csl0013-taint-to-dynamic-execution" "CSL0013"
examples/Web/Dockerfile:12:ENV ASPNETCORE_URLS=http://+:5000
scripts/check-remediation-baseline.sh:19:  local path="$1"
scripts/check-remediation-baseline.sh:22:  if [[ -f "$path" ]]; then
scripts/check-remediation-baseline.sh:25:    fail "$label: missing $path"
scripts/check-remediation-baseline.sh:31:  local path="$2"
scripts/check-remediation-baseline.sh:34:  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/dev/null; then
scripts/check-remediation-baseline.sh:43:  local path="$2"
scripts/check-remediation-baseline.sh:46:  if rg -n -U --pcre2 --hidden --glob '!.git/**' "$pattern" "$path" >/tmp/slsknet-runtime-remediation-hit.$$ 2>/dev/null; then
scripts/check-remediation-baseline.sh:108:require_pattern "tokenBucket\.GetAsync\\(Math\.Min\\(requestedBytes, bytesGrantedByCaller\\), cancelToken\\)" "src/SoulseekClient.cs" "upload token bucket waits observe linked race cancellation"
scripts/check-remediation-baseline.sh:111:require_pattern "WaitForResetAsync\\(cancellationToken\\)" "src/Common/TokenBucket.cs" "token bucket reset waits observe cancellation"
scripts/check-remediation-baseline.sh:112:require_pattern "TrySetException\\(new ObjectDisposedException\\(nameof\\(TokenBucket\\)\\)\\)" "src/Common/TokenBucket.cs" "token bucket disposal releases reset waiters"
scripts/check-remediation-baseline.sh:113:require_pattern "GetAsync_Observes_Cancellation_While_Waiting_For_Reset" "tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs" "token bucket cancellation regression test is registered"
scripts/check-remediation-baseline.sh:114:require_pattern "Dispose_Releases_Waiters_Waiting_For_Reset" "tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs" "token bucket disposal regression test is registered"
scripts/check-remediation-baseline.sh:122:require_pattern "discovery hints rather than authorization decisions" "README.md" "peer capabilities are documented as non-authorization hints"
scripts/check-remediation-baseline.sh:126:require_pattern "GetSharedRemotePath" "examples/Web/api/Extensions.cs" "example Web API has relative shared path helper"
scripts/check-remediation-baseline.sh:128:require_pattern "Path.GetRelativePath" "examples/Web/api/Extensions.cs" "example Web API shared paths are root-relative"
scripts/check-remediation-baseline.sh:134:require_pattern "Extensions.GetSharedRemotePath\\(Directory, f\\)" "examples/Web/api/SharedFileCache.cs" "example shared search cache advertises relative paths"
scripts/check-remediation-baseline.sh:136:require_pattern "Extensions.GetSharedRemotePath\\(SharedDirectory, dir\\)" "examples/Web/api/Startup.cs" "example browse resolver advertises relative paths"
scripts/check-remediation-baseline.sh:137:require_pattern "WebApiPathSecurityTests" "tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs" "example path security tests are registered"
scripts/check-remediation-baseline.sh:138:require_pattern "WebApi_Shared_Remote_Path_Is_Relative_To_Root" "tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs" "example shared path relative test is registered"
scripts/check-remediation-baseline.sh:139:require_pattern "Shared_File_Cache_Advertises_Relative_Filenames" "tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs" "example shared cache relative path test is registered"
scripts/check-remediation-baseline.sh:141:require_pattern "Browse_Response_Resolver_Advertises_Relative_Directory_Names" "tests/Soulseek.Tests.Unit/WebApiRequestTests.cs" "example browse resolver relative path test is registered"
scripts/check-remediation-baseline.sh:142:require_pattern "Directory_Contents_Resolver_Advertises_Relative_Directory_Names" "tests/Soulseek.Tests.Unit/WebApiRequestTests.cs" "example directory resolver relative path test is registered"
scripts/check-remediation-baseline.sh:174:require_pattern "start < 0" "src/Common/TokenFactory.cs" "token factory rejects negative starting tokens"
scripts/check-remediation-baseline.sh:175:require_pattern "StartingToken < 0" "src/Options/SoulseekClientOptions.cs" "client options reject negative starting tokens"
scripts/check-remediation-baseline.sh:176:require_pattern "Throws_If_Start_Is_Negative" "tests/Soulseek.Tests.Unit/Common/TokenFactoryTests.cs" "token factory negative start regression test is registered"
scripts/check-remediation-baseline.sh:177:require_pattern "Throws_If_Starting_Token_Is_Negative" "tests/Soulseek.Tests.Unit/Options/SoulseekClientOptionsTests.cs" "client options negative starting token regression test is registered"
scripts/check-remediation-baseline.sh:229:require_pattern "TokenParts => tokenParts\\.ToArray\\(\\)" "src/Common/WaitKey.cs" "wait key token parts are defensive copies"
scripts/check-remediation-baseline.sh:230:require_pattern "TokenParts_Snapshots_Parts" "tests/Soulseek.Tests.Unit/Common/WaitKeyTests.cs" "wait key token part snapshot test is registered"
scripts/check-remediation-baseline.sh:248:require_pattern "token < 0" "src/EventArgs/SearchRequestEventArgs.cs" "search request events reject negative tokens"
scripts/check-remediation-baseline.sh:249:require_pattern "token < 0" "src/EventArgs/UserCannotConnectEventArgs.cs" "cannot-connect events reject negative tokens"
scripts/check-remediation-baseline.sh:252:require_pattern "SearchRequestEventArgs_Rejects_Negative_Token" "tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs" "search request token event regression test is registered"
scripts/check-remediation-baseline.sh:253:require_pattern "UserCannotConnectEventArgs_Rejects_Negative_Token" "tests/Soulseek.Tests.Unit/DomainModelValidationTests.cs" "cannot-connect token event regression test is registered"
scripts/check-remediation-baseline.sh:279:require_pattern "ThrowIfNegativeToken\\(token\\.Value, nameof\\(token\\)\\)" "src/SoulseekClient.cs" "client public token entry points reject negative tokens"
scripts/check-remediation-baseline.sh:280:require_pattern "SearchAsync_Throws_ArgumentOutOfRangeException_Given_Negative_Token" "tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs" "search negative token regression test is registered"
scripts/check-remediation-baseline.sh:285:require_pattern "TransferDictionarySyncRoot" "src/SoulseekClient.cs" "transfer registration synchronizes token and unique-key dictionaries"
scripts/check-remediation-baseline.sh:286:require_pattern "UploadDictionary\\.ContainsKey\\(download\\.Token\\)" "src/SoulseekClient.cs" "download registration rejects upload token collisions"
scripts/check-remediation-baseline.sh:287:require_pattern "DownloadDictionary\\.ContainsKey\\(upload\\.Token\\)" "src/SoulseekClient.cs" "upload registration rejects download token collisions"
scripts/check-remediation-baseline.sh:288:require_pattern "DownloadToStreamAsync_Throws_DuplicateTokenException_When_Token_Is_Registered_To_Upload" "tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs" "download cross-direction token collision regression test is registered"
scripts/check-remediation-baseline.sh:289:require_pattern "UploadFromStreamAsync_Throws_DuplicateTokenException_When_Token_Is_Registered_To_Download" "tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs" "upload cross-direction token collision regression test is registered"
scripts/check-remediation-baseline.sh:290:require_pattern "RT-115" "docs/dev/bug-burndown-ledger.md" "ledger records transfer token registration hardening"
scripts/check-remediation-baseline.sh:351:require_pattern "DownloadAsync_Throws_ArgumentOutOfRangeException_Given_Negative_Token" "tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs" "download negative token regression test is registered"
scripts/check-remediation-baseline.sh:352:require_pattern "GetDirectoryContentsAsync_Throws_ArgumentOutOfRangeException_Given_Negative_Token" "tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs" "directory contents negative token regression test is registered"
scripts/check-remediation-baseline.sh:353:require_pattern "UploadAsync_Stream_Throws_ArgumentOutOfRangeException_Given_Negative_Token" "tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs" "upload negative token regression test is registered"
scripts/check-remediation-baseline.sh:436:require_pattern "RequireNonNegative\\(token" "src/Messaging/Messages" "protocol message token constructors reject negative values"
scripts/check-remediation-baseline.sh:437:require_pattern "RequireNonNegative\\(token, nameof\\(token\\), \"peer search token\"" "src/Messaging/Messages/Peer/PeerSearchRequest.cs" "peer search requests reject negative tokens"
scripts/check-remediation-baseline.sh:438:require_pattern "RequireNonNegative\\(token, nameof\\(token\\), \"server search token\"" "src/Messaging/Messages/Server/ServerSearchRequest.cs" "server search requests reject negative tokens"
scripts/check-remediation-baseline.sh:441:require_pattern "Peer_Search_Request_Rejects_Negative_Token" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs" "peer search negative token parser test is registered"
scripts/check-remediation-baseline.sh:442:require_pattern "Server_Search_Request_Rejects_Negative_Token" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs" "server search negative token parser test is registered"
scripts/check-remediation-baseline.sh:444:require_pattern "Protocol_Message_Constructors_Reject_Negative_Tokens" "tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs" "protocol token constructor regression test is registered"
scripts/check-remediation-baseline.sh:446:require_pattern "RT-091" "docs/dev/bug-burndown-ledger.md" "ledger records protocol token guard sync"
scripts/check-remediation-baseline.sh:450:require_pattern "with-filename" "scripts/scan-bug-council-candidates.sh" "scanner preserves filenames for single-file sections"
scripts/check-remediation-baseline.sh:534:require_pattern "ipAddress\\.Snapshot\\(\\)" "src/Options/ProxyOptions.cs" "proxy options snapshot IP addresses"
scripts/check-remediation-baseline.sh:543:require_pattern "Snapshots_IPAddress" "tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs" "proxy options IP snapshot test is registered"
scripts/check-remediation-baseline.sh:576:require_pattern "Example Web API path and shared-file candidates" "scripts/scan-bug-council-candidates.sh" "scanner emits example Web API path/shared-file subgroup"
scripts/check-remediation-baseline.sh:580:require_pattern "Example Web API path, request, and lifecycle candidates: 390/390 classified" "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API broad sweep is closed"
scripts/check-remediation-baseline.sh:581:require_pattern "Example Web API path and shared-file candidates: 177/177 classified" "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API path/shared-file subgroup sweep is closed"
scripts/check-remediation-baseline.sh:588:require_pattern "RT-081" "docs/dev/bug-burndown-ledger.md" "ledger records example shared path hardening"
scripts/check-remediation-baseline.sh:594:require_pattern "Security-sensitive material candidates: 2/2 classified" "docs/dev/bug-council-sweep-residual-small-2026-05-05.md" "residual secret-pattern sweep is closed"
scripts/check-remediation-baseline.sh:603:secret_pattern='-----BEGIN (RSA |DSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{36,}|xox[baprs]-[A-Za-z0-9-]{20,}|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)["'\'']?\s*[:=]\s*["'\''][A-Za-z0-9_./+=-]{24,}["'\'']'
scripts/check-remediation-baseline.sh:604:require_absent_pattern "$secret_pattern" "." "tracked text files do not contain high-confidence secret patterns"
scripts/check-remediation-baseline.sh:631:require_pattern "RT-130" "docs/dev/bug-burndown-ledger.md" "ledger records transfer diagnostic filename privacy hardening"
scripts/check-remediation-baseline.sh:678:require_file "analyzers/Soulseek.CouncilAnalyzers/TaintToFilePathAnalyzer.cs" "CSL0004 taint-to-file-path analyzer source exists"
scripts/check-remediation-baseline.sh:687:require_file "analyzers/Soulseek.CouncilAnalyzers/TaintToDynamicExecutionAnalyzer.cs" "CSL0013 taint-to-dynamic-execution analyzer source exists"
scripts/check-remediation-baseline.sh:720:require_pattern "ResolveContainedPath" "analyzers/Soulseek.CouncilAnalyzers/ProtocolTaintAnalysis.cs" "CSL0004 validators include contained path resolution"
scripts/check-council-sweep-counts.sh:91:require_closed_count "Example Web API path, request, and lifecycle candidates" 390 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API broad sweep count matches scanner"
scripts/check-council-sweep-counts.sh:92:require_closed_count "Example Web API path and shared-file candidates" 177 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API path/shared-file sweep count matches scanner"
scripts/check-council-sweep-counts.sh:98:require_closed_count "Security-sensitive material candidates" 2 "docs/dev/bug-council-sweep-residual-small-2026-05-05.md" "residual secret-pattern sweep count matches scanner"
docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md:43:- Raw response streams are app-owned inputs with positive length and non-null stream constructor guards; write failures dispose streams and are reported through existing connection/diagnostic paths.
docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md:51:- XML documentation hits for stream factory parameters are command-reference context, not separate runtime paths.
scripts/run-council-active-bughunt.sh:18:    rg -n -U --with-filename --pcre2 --hidden --glob '!.git/**' --glob '!.council/**' "$pattern" "$@" || true
scripts/run-council-active-bughunt.sh:72:  rg -n --with-filename --pcre2 \
scripts/run-council-active-bughunt.sh:79:  rg -n --with-filename --pcre2 \
scripts/run-council-active-bughunt.sh:86:  rg -n --with-filename --pcre2 \
scripts/run-council-active-bughunt.sh:93:  rg -n --with-filename --pcre2 \
scripts/run-council-active-bughunt.sh:100:  '(Diagnostic\.(Debug|Info|Warning|Error)|StatusCode\(|BadRequest\(|Console\.WriteLine)\([^;\n]*(username|query|filename|directory|token|Message)' \
scripts/run-council-active-bughunt.sh:110:  '(token|secret|password|authorization|cookie|api[-_]?key|session|redirect|proxy|forwarded|path|filename|exec|spawn|shell|http://|https://)' \
examples/Web/web/README.md:1:This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).
examples/Web/web/README.md:10:Open [http://localhost:3000](http://localhost:3000) to view it in the browser.
examples/Web/web/README.md:18:See the section about [running tests](https://facebook.github.io/create-react-app/docs/running-tests) for more information.
examples/Web/web/README.md:25:The build is minified and the filenames include the hashes.<br>
examples/Web/web/README.md:28:See the section about [deployment](https://facebook.github.io/create-react-app/docs/deployment) for more information.
examples/Web/web/README.md:42:You can learn more in the [Create React App documentation](https://facebook.github.io/create-react-app/docs/getting-started).
examples/Web/web/README.md:44:To learn React, check out the [React documentation](https://reactjs.org/).
examples/Web/web/README.md:48:This section has moved here: https://facebook.github.io/create-react-app/docs/code-splitting
examples/Web/web/README.md:52:This section has moved here: https://facebook.github.io/create-react-app/docs/analyzing-the-bundle-size
examples/Web/web/README.md:56:This section has moved here: https://facebook.github.io/create-react-app/docs/making-a-progressive-web-app
examples/Web/web/README.md:60:This section has moved here: https://facebook.github.io/create-react-app/docs/advanced-configuration
examples/Web/web/README.md:64:This section has moved here: https://facebook.github.io/create-react-app/docs/deployment
examples/Web/web/README.md:68:This section has moved here: https://facebook.github.io/create-react-app/docs/troubleshooting#npm-run-build-fails-to-minify
examples/Web/web/src/config.js:1:const baseUrl = process.env.NODE_ENV === 'production' ? 'api/v1' : 'http://localhost:5000/api/v1';
examples/Web/web/src/config.js:2:const tokenKey = 'soulseek-example-token';
examples/Web/web/src/config.js:3:const tokenPassthroughValue = 'n/a';
examples/Web/web/src/config.js:9:    tokenKey,
examples/Web/web/src/config.js:10:    tokenPassthroughValue,
scripts/scan-bug-council-candidates.sh:13:  rg -n --with-filename --pcre2 --hidden --glob '!.git/**' --glob '!.council/**' "$pattern" "$@" || true
scripts/scan-bug-council-candidates.sh:22:  rg -n -U --with-filename --pcre2 --hidden --glob '!.git/**' --glob '!.council/**' "$pattern" "$@" || true
scripts/scan-bug-council-candidates.sh:137:scan "Example Web API path, request, and lifecycle candidates" \
scripts/scan-bug-council-candidates.sh:141:scan "Example Web API path and shared-file candidates" \
scripts/scan-bug-council-candidates.sh:158:  'PRIVATE KEY|gh[pousr]_|xox[baprs]-|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)' \
docs/dev/bug-burndown-ledger.md:3:This ledger tracks runtime-specific bug council findings for `slskNet.Runtime`. The scope is the .NET runtime library, its tests, package/release metadata, local scripts, and the example Web API only where it exercises runtime-facing path handling.
docs/dev/bug-burndown-ledger.md:20:The script verifies protocol parser count guards, frame and buffered-read limits, idempotent task completion, transfer cancellation/disconnect races, token bucket cancellation/disposal, peer descriptor fail-closed behavior, Web API path containment, sensitive token/key pattern absence, fork branding metadata, and this command reference.
docs/dev/bug-burndown-ledger.md:32:| RT-003 | Transfer streams | Buffered connection reads could allocate caller-declared lengths that are inappropriate for message handshakes. | `src/Network/Tcp/Connection.cs` and transfer paths using stream overloads for large payloads. | Fixed | Added `MaximumBufferedReadLength` guard and retained stream read/write overloads for transfers. |
docs/dev/bug-burndown-ledger.md:33:| RT-004 | Network lifecycle/concurrency | Duplicate disconnect, denied, failed, and cancellation callbacks could complete task sources more than once. | `src/SoulseekClient.cs`, `src/Network/Tcp/Connection.cs`, `src/Common/Waiter.cs`, and `src/SearchInternal.cs`. | Fixed | Runtime-owned task completion uses `TrySetResult`, `TrySetException`, or `TrySetCanceled` for race-prone paths. |
docs/dev/bug-burndown-ledger.md:34:| RT-005 | Transfer streams | Download/upload races between transfer IO, disconnect, remote failure, and caller cancellation needed deterministic loser cancellation. | `src/SoulseekClient.cs` download/upload transfer loops. | Fixed | Transfer methods race IO against disconnect/remote-failure tasks and cancel the losing path with linked tokens. |
docs/dev/bug-burndown-ledger.md:35:| RT-006 | Peer capability/signature trust | Capability descriptors must not be treated as authorization and signature verification must fail closed on malformed metadata. | `src/Ed25519PeerDescriptorSigner.cs`, `src/PeerCapabilityRegistry.cs`, and README compatibility notes. | Fixed | Verifier returns false for malformed signatures and catches verifier exceptions; registry stores descriptors as discovery hints only. |
docs/dev/bug-burndown-ledger.md:37:| RT-008 | Example Web API path safety | Shared-directory and download-output paths could be vulnerable to prefix sibling escapes or absolute remote names if path containment was string-prefix only. | `examples/Web/api/Extensions.cs`, `examples/Web/api/Controllers/TransfersController.cs`, and `tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs`. | Fixed | Added normalized root containment and safe relative output path helpers with regression tests. |
docs/dev/bug-burndown-ledger.md:39:| RT-010 | Tests/tooling | Runtime hardening checks needed a durable command and registry so future audits do not depend on ad hoc shell history. | `scripts/check-remediation-baseline.sh` and this ledger. | Fixed | Added a Bash-only remediation baseline; no root `package.json` is required. |
docs/dev/bug-burndown-ledger.md:40:| RT-011 | Sensitive material | Static audit should catch accidental embedded API tokens, private keys, or access tokens in runtime files. | `scripts/check-remediation-baseline.sh` secret scan over tracked text files. | Fixed | Baseline scans source, tests, examples, docs, scripts, bin, and `.circleci` for high-confidence key/token patterns. |
docs/dev/bug-burndown-ledger.md:49:| RT-020 | Example Web API path safety | The example download endpoint opened the local output file immediately when enqueueing, before the runtime requested the stream. | `examples/Web/api/Controllers/TransfersController.cs` and `WebApiTransferTests`. | Fixed | Output file creation is deferred into the download stream factory and malformed request bodies return `400`. |
docs/dev/bug-burndown-ledger.md:53:| RT-024 | Example Web API lifecycle | Removed example Web API transfer records dropped their `CancellationTokenSource` without disposing it. | `examples/Web/api/Trackers/TransferTracker.cs` and `WebApiTransferTests`. | Fixed | Transfer tracker removal now disposes token sources for removed transfer records and removed user buckets. |
docs/dev/bug-burndown-ledger.md:54:| RT-025 | Example Web API lifecycle | Updating an existing tracked transfer with a different `CancellationTokenSource` could orphan the old source. | `examples/Web/api/Trackers/TransferTracker.cs` and `WebApiTransferTests`. | Fixed | Transfer tracker replacement now disposes the previous token source when ownership moves to a new source. |
docs/dev/bug-burndown-ledger.md:55:| RT-026 | Example Web API lifecycle | Failed download enqueue setup could leave a controller-created `CancellationTokenSource` undisposed before any tracker record owned it. | `examples/Web/api/Controllers/TransfersController.cs` and `WebApiTransferTests`. | Fixed | The controller now disposes untracked token sources on pre-tracking failure while leaving tracked sources owned by the tracker. |
docs/dev/bug-burndown-ledger.md:56:| RT-027 | Example Web API lifecycle | Upload task failures in the example Web API skipped disposal of the upload cancellation source. | `examples/Web/api/Startup.cs`. | Fixed | Upload task execution now disposes its token source in a `finally` block on both success and failure. |
docs/dev/bug-burndown-ledger.md:59:| RT-030 | Example Web API token DTO | Token response properties dereferenced missing JWT claims and reparsed `nbf` manually. | `TokenResponse` and `WebApiRequestTests`. | Fixed | Token response now reads optional name claims safely and derives not-before from the token validity window. |
docs/dev/bug-burndown-ledger.md:64:| RT-035 | Runtime options | Search, browse, and connection option constructors accepted impossible timeout, limit, buffer, and queue values that could fail later in network/search paths. | `SearchOptions`, `BrowseOptions`, `ConnectionOptions`, and option regression tests. | Fixed | Runtime option constructors now reject invalid scalar values at construction while preserving the documented `-1` inactivity-timeout sentinel. |
docs/dev/bug-burndown-ledger.md:69:| RT-040 | Network lifecycle/concurrency | Client maximum upload/download speed options accepted zero or negative values that later produced invalid token bucket capacities. | `SoulseekClientOptions`, `SoulseekClientOptionsPatch`, and option tests. | Fixed | Client speed capacities now fail at option construction for both initial options and runtime patches. |
docs/dev/bug-burndown-ledger.md:74:| RT-045 | Example Web API lifecycle | Web API trackers accepted invalid room message limits and null tracker payloads, allowing normal update paths to fail later with incidental collection/null exceptions. | `RoomTracker`, `ConversationTracker`, `BrowseTracker`, and tracker tests. | Fixed | Trackers now reject invalid limits/null payloads and normalize missing room message/user lists before updates. |
docs/dev/bug-burndown-ledger.md:75:| RT-046 | Network lifecycle/concurrency | Empty token bucket waits ignored caller cancellation until the next timer reset and disposal could leave reset waiters unresolved. | `TokenBucket` and token bucket tests. | Fixed | Reset waits now race cancellation and disposal completes the pending reset signal with `ObjectDisposedException`. |
docs/dev/bug-burndown-ledger.md:82:| RT-053 | Network lifecycle/concurrency | Enqueue upload wrappers could wait indefinitely when the transfer task completed before the local queued callback, such as cancellation before the upload semaphore path reached its queue state. | `SoulseekClient.EnqueueUploadAsync`, shared enqueue wait helper, and `EnqueueUploadAsyncTests`. | Fixed | Enqueue wrappers now race the queue signal against the transfer task and upload enqueue callbacks complete on failed terminal states. |
docs/dev/bug-burndown-ledger.md:83:| RT-054 | Network lifecycle/concurrency | Upload transfer races created a linked cancellation token but passed the original caller token into long-running write and token-bucket waits, so a disconnect winner could leave the losing write path alive until unrelated cancellation/disposal. | `SoulseekClient.UploadFromStreamAsync` and upload disconnect-race tests. | Fixed | Upload stream writes and token-bucket waits now observe the linked race token, with regression coverage for disconnect-winning cancellation. |
docs/dev/bug-burndown-ledger.md:84:| RT-055 | Runtime domain models | Public browse/search/file domain models accepted null collection elements, deferring failures into serializer/filter paths as incidental `NullReferenceException`s. | `File`, `Directory`, `BrowseResponse`, `SearchResponse`, and domain model validation tests. | Fixed | Collection-bearing response models now reject null entries at construction before they can be serialized or filtered. |
docs/dev/bug-burndown-ledger.md:86:| RT-057 | Runtime domain models | Additional public collection-bearing DTOs accepted null entries or retained mutable caller collections, allowing later event, sort, serializer, or consumer paths to fail with incidental null dereferences. | `RoomList`, `RoomData`, `RoomTickerListReceivedEventArgs`, `ItemSimilarUsers`, `RecommendationList`, `MeshRendezvousResult`, and domain model validation tests. | Fixed | Runtime DTO constructors now copy collection inputs, preserve existing null-list defaults, and reject null elements before publishing snapshots. |
docs/dev/bug-burndown-ledger.md:90:| RT-061 | Search query validation | Collection-based `SearchQuery` construction accepted null terms and exclusions, allowing malformed search text and `SearchAsync` null dereferences during single-character term filtering. | `SearchQuery`, `SearchQueryTests`, and client search validation path review. | Fixed | Search query construction now rejects null term/exclusion entries while preserving existing null-list-as-empty behavior. |
docs/dev/bug-burndown-ledger.md:91:| RT-062 | Search scope validation | `SearchScope` validated the caller-provided params array and then retained that mutable array, allowing subjects to be changed to null or empty after construction before search messages are emitted. | `SearchScope`, `SearchScopeTests`, and client search message construction paths. | Fixed | Search scopes now snapshot validated subjects into an immutable collection before publishing them. |
docs/dev/bug-burndown-ledger.md:92:| RT-063 | Runtime value snapshots / waiter keys | `WaitKey` retained caller-owned token parts and its equality operators dereferenced null operands; `UserInfo` exposed caller-owned picture bytes. | `WaitKey`, `UserInfo`, wait-key tests, and user-info response tests. | Fixed | Wait keys now snapshot token parts and compare null safely; user info picture bytes are cloned on construction and access. |
docs/dev/bug-burndown-ledger.md:101:| RT-072 | Protocol scalar emission | Protocol scalar emitters could still fail late or emit invalid values through internal-only paths: `WriteString(null)` failed through encoding internals, private/privilege acknowledgement commands accepted negative IDs, and privilege grant commands accepted non-positive day counts. | `MessageBuilder`, acknowledgement/give-privilege commands, outgoing message tests, message-builder tests, and scalar sweep. | Fixed | String emission now rejects null values at the builder boundary, and internal scalar command constructors reject negative acknowledgement IDs and non-positive privilege grant durations before serialization. |
docs/dev/bug-burndown-ledger.md:107:| RT-078 | Network lifecycle/concurrency | Distributed connection status and branch-broadcast paths used fire-and-forget timer/watchdog/status callbacks without a diagnostic boundary, so background failures could be dropped or surfaced only through unobserved tasks. | `DistributedConnectionManager`, distributed connection manager tests, and lifecycle sweep. | Fixed | Distributed status and branch broadcast callbacks now use safe queue helpers that catch failures and emit targeted diagnostics, including debounce timer and background status update failure regressions. |
docs/dev/bug-burndown-ledger.md:109:| RT-080 | Tests/tooling | The example Web API scan mixed path containment, shared-file advertisement, controller request validation, transfer cancellation/stream ownership, tracker state, and test fixture hits, so the council could close individual web example bugs without classifying the whole section. | `scripts/scan-bug-council-candidates.sh`, `docs/dev/bug-council-sweep-webapi-2026-05-05.md`, and remediation-baseline checks. | Fixed | The scanner now emits path/shared-file, controller request-validation, transfer lifecycle, and tracker state subgroups; the sweep records `390/390`, `177/177`, `268/268`, `158/158`, and `212/212` classified with zero unclassified candidates. |
docs/dev/bug-burndown-ledger.md:110:| RT-081 | Example Web API path safety | The example shared-file cache and browse/directory resolvers advertised absolute local filesystem paths to peers, leaking host path layout and coupling remote file names to local roots. | `Extensions.GetSharedRemotePath`, `SharedFileCache`, `Startup` resolver methods, and Web API path/request tests. | Fixed | Shared file search, browse, and directory-contents responses now advertise root-relative paths while retaining root containment for incoming directory/download requests. |
docs/dev/bug-burndown-ledger.md:111:| RT-082 | Example Web API lifecycle | The example shared-file cache created a new in-memory SQLite connection on every refresh without disposing the previous connection. | `SharedFileCache` and Web API path tests. | Fixed | Cache refresh now disposes the prior SQLite connection before replacing it, with regression coverage that the previous connection is closed after a refresh. |
docs/dev/bug-burndown-ledger.md:114:| RT-085 | Tests/tooling | Residual small scan queues for mutable arrays, equality, and secret patterns were left as "mostly fixed" instead of being closed with countable whole-section classification. | `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`, active sweep registers, and remediation-baseline checks. | Fixed | The residual sweep records `12/12`, `4/4`, and `2/2` classified with zero unclassified candidates, so future passes cannot keep rediscovering the same ambiguous small queues. |
docs/dev/bug-burndown-ledger.md:115:| RT-086 | Network identity/equality | `ConnectionKey` compared usernames and computed hashes with culture-sensitive string comparison, allowing protocol identity keys such as `user` and `u\0ser` to compare equal under culture rules. | `ConnectionKey`, `WaitKey`, connection-key tests, and residual equality sweep. | Fixed | Connection identity now uses ordinal string comparison and ordinal hash codes, with regression coverage for embedded-null usernames; `WaitKey` hash codes now match its ordinal token equality. |
docs/dev/bug-burndown-ledger.md:120:| RT-091 | Protocol scalar emission | slskdN's vendored runtime carried negative-token and login-minor-version guards that were missing from the standalone runtime, so the two release sources could diverge and standalone protocol constructors could still emit impossible scalar identifiers. | `ProtocolArgumentValidator`, token-bearing protocol message constructors, `ProtocolScalarHardeningTests`, and the slskdN release sync pass. | Fixed | The standalone runtime now owns the token/minor-version guards, regression coverage checks every affected constructor, and the vendored runtime is synced from the standalone source of truth. |
docs/dev/bug-burndown-ledger.md:124:| RT-095 | Wishlist scheduler lifecycle | `WishlistSearchScheduler.RunSearchAsync` caught exceptions from `SearchCompleted` event handlers as though the underlying wishlist search had failed, causing a subscriber failure after a successful search to emit a second failure completion with the subscriber exception. | `WishlistSearchScheduler` and scheduler lifecycle tests. | Fixed | Wishlist search execution is now caught separately from completion notification, so handler exceptions are no longer reclassified as search failures or emitted through duplicate failure callbacks. |
docs/dev/bug-burndown-ledger.md:127:| RT-098 | Transfer streams | Raw browse/search response constructors accepted non-readable streams, deferring an impossible response to connection-write failure after resolver ownership had already crossed into delivery paths. | `RawSearchResponse`, `RawBrowseResponse`, raw response tests, and responder/peer handler focused tests. | Fixed | Raw response construction now rejects non-readable streams at the public response boundary for both search and browse responses. |
docs/dev/bug-burndown-ledger.md:128:| RT-099 | Peer capability/signature trust | Peer capability envelopes and registry records could carry undefined capability message types, and records could expose a null endpoint through registry snapshots/events. | `PeerCapabilityEnvelope`, `PeerCapabilityRecord`, registry update paths, and peer capability tests. | Fixed | Capability envelopes and records now reject undefined message types, and records require a non-null endpoint before registry publication. |
docs/dev/bug-burndown-ledger.md:131:| RT-102 | Search scope validation | `SearchScope` accepted whitespace-only room and user subjects even though scoped search emission writes those values directly into room/user search protocol messages and other username/room APIs reject whitespace-only identifiers. | `SearchScope`, search scope tests, and scoped search emission paths. | Fixed | Room and user search scopes now reject null, empty, and whitespace-only subjects before scoped search requests can be emitted. |
docs/dev/bug-burndown-ledger.md:134:| RT-105 | Network metadata ownership | Runtime address-bearing options, messages, and endpoint snapshots still retained mutable IPv6 `IPAddress` references, allowing `ScopeId` mutation after construction or after property access to alter published metadata. | `IPEndPointExtensions`, server IP messages, `NetInfoNotification`, client/proxy options, listener metadata, and IP address snapshot tests. | Fixed | IP addresses are now cloned on input and on read access, endpoint snapshots clone their contained addresses, and regression tests cover IPv6 scope-id mutation across public and internal metadata surfaces. |
docs/dev/bug-burndown-ledger.md:137:| RT-108 | Runtime event models | Public event snapshots for search requests, connection failures, private messages, and privilege notifications accepted negative protocol identifiers that parser and acknowledgement paths reject elsewhere. | Search/user/notification event args and event/domain model tests. | Fixed | Event args now reject negative request tokens and notification/message ids before publishing impossible identifier snapshots to subscribers. |
docs/dev/bug-burndown-ledger.md:138:| RT-109 | Protocol parsing/emission | Peer and server search request messages still accepted negative search tokens and server search requests accepted null identity/query values, unlike distributed search requests and outbound search commands. | `PeerSearchRequest`, `ServerSearchRequest`, and protocol scalar tests. | Fixed | Peer and server search request constructors now reject negative tokens and null protocol strings before malformed requests can reach responder event publication. |
docs/dev/bug-burndown-ledger.md:139:| RT-110 | Protocol emission | Client-generated protocol tokens could start negative when `SoulseekClientOptions.StartingToken` or `TokenFactory` was seeded with a negative value, conflicting with hardened outbound token constructors. | `TokenFactory`, `SoulseekClientOptions`, token factory/options tests, and remediation-baseline checks. | Fixed | Token generation now rejects negative starting values at both the public option boundary and internal factory boundary before generated protocol messages can fail or emit invalid identifiers. |
docs/dev/bug-burndown-ledger.md:141:| RT-112 | Runtime public API validation | Public search, directory contents, download, and upload entry points accepted explicit negative tokens even though generated tokens and outbound protocol messages now reject negative identifiers. | `SoulseekClient` token-bearing public methods and focused client regression tests. | Fixed | Added a shared client token guard so explicit negative tokens fail at the public API boundary before duplicate checks, transfer setup, or protocol message emission. |
docs/dev/bug-burndown-ledger.md:143:| RT-114 | Network lifecycle/concurrency | Search token registration used a public pre-check plus unchecked internal `TryAdd`, so concurrent searches with the same token could both pass validation; the loser could continue without owning the active-search dictionary entry and then remove the winner during cleanup. | `SoulseekClient.SearchToCallbackAsync`, active search registration, and search regression tests. | Fixed | Search registration now treats `TryAdd` as authoritative, throws `DuplicateTokenException` on internal registration failure, and removes active-search entries only when the current operation registered them. |
docs/dev/bug-burndown-ledger.md:144:| RT-115 | Transfer lifecycle/concurrency | Transfer token registration was split across upload and download dictionaries without an atomic cross-direction guard, so concurrent upload and download starts could register the same token even though public validation treats transfer tokens as globally unique. | `SoulseekClient` transfer registration and download/upload regression tests. | Fixed | Download and upload registration now lock the unique-key and token dictionary mutation together, reject cross-direction token collisions with `DuplicateTokenException`, and release unique keys when registration fails. |
docs/dev/bug-burndown-ledger.md:154:| RT-125 | Runtime event models | `SoulseekClient` raised public search state and response events directly inside the search state machine and response callback, so subscriber exceptions could prevent search requests from being sent or turn valid search responses into callback failures. | `SoulseekClient`, active council unisolated client-search probe, search event-boundary tests, and remediation-baseline checks. | Fixed | Public search events now run through diagnostic event helpers while the explicit response-handler delegate remains the caller-owned result path, and regression tests cover throwing `SearchStateChanged` and `SearchResponseReceived` subscribers. |
docs/dev/bug-burndown-ledger.md:159:| RT-130 | Diagnostics/privacy | Transfer diagnostics mixed basename-only messages with raw transfer filename variables, and `Path.GetFileName` only strips the host platform separator, so Soulseek-style backslash paths could leak full local or remote path segments in logs. | `SoulseekClient` transfer diagnostics, diagnostic filename tests, active classification register, and remediation-baseline checks. | Fixed | Transfer diagnostic file labels now use a cross-platform basename helper for slash and backslash paths, including previously raw second-chance and dispose warnings. |
docs/dev/bug-burndown-ledger.md:162:| RT-133 | Diagnostics/privacy | Runtime search diagnostics and wrapped search failures logged raw search text, exposing user search terms to diagnostic sinks when token/count metadata is sufficient for correlation. | `SoulseekClient.SearchToCallbackAsync`, diagnostic search-description tests, active classification register, and remediation-baseline checks. | Fixed | Search diagnostics now log token, term count, and exclusion count without raw search terms. |
docs/dev/bug-burndown-ledger.md:163:| RT-134 | Diagnostics/logging | RT-130 and RT-133 overcorrected normal runtime diagnostics by hiding transfer path context and search text that operators need for troubleshooting. | `SoulseekClient` diagnostic helpers, diagnostic value/search-description tests, active classification register, and remediation-baseline checks. | Fixed | Runtime diagnostics preserve operator-visible filenames, paths, usernames, and search text; only log-breaking control characters are escaped. Secrets remain covered by the high-confidence secret scanner. |
src/SoulseekClient.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SoulseekClient.cs:96:        /// <param name="tokenFactory">The ITokenFactory instance to use.</param>
src/SoulseekClient.cs:116:            ITokenFactory tokenFactory = null,
src/SoulseekClient.cs:153:            TokenFactory = tokenFactory ?? new TokenFactory(Options.StartingToken);
src/SoulseekClient.cs:179:                // fail any download that matches this filename and user (we shouldn't have >1 but stranger things could happen)
src/SoulseekClient.cs:189:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} reported as failed by remote client (token: {download.Token})");
src/SoulseekClient.cs:215:                        Diagnostic.Debug($"Download of {GetDiagnosticLogValue(download.Filename)} from {download.Username} rejected by remote client (token: {download.Token})");
src/SoulseekClient.cs:670:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:696:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:725:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:759:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:798:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:826:        ///     Asynchronously changes the password for the currently logged in user.
src/SoulseekClient.cs:828:        /// <param name="password">The new password.</param>
src/SoulseekClient.cs:829:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:832:        ///     Thrown when the <paramref name="password"/> is null, empty, or consists only of whitespace.
src/SoulseekClient.cs:838:        public Task ChangePasswordAsync(string password, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:840:            if (string.IsNullOrWhiteSpace(password))
src/SoulseekClient.cs:842:                throw new ArgumentException("The password must not be a null or empty string, or one consisting only of whitespace", nameof(password));
src/SoulseekClient.cs:847:                throw new InvalidOperationException($"The server connection must be connected and logged in change a password (currently: {State})");
src/SoulseekClient.cs:850:            return ChangePasswordInternalAsync(password, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:855:        ///     <paramref name="username"/> and <paramref name="password"/>.
src/SoulseekClient.cs:858:        /// <param name="password">The password with which to log in.</param>
src/SoulseekClient.cs:859:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:862:        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
src/SoulseekClient.cs:871:        public Task ConnectAsync(string username, string password, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:873:            return ConnectAsync(DefaultAddress, DefaultPort, username, password, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:878:        ///     and logs in using the specified <paramref name="username"/> and <paramref name="password"/>.
src/SoulseekClient.cs:883:        /// <param name="password">The password with which to log in.</param>
src/SoulseekClient.cs:884:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:893:        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
src/SoulseekClient.cs:903:        public Task ConnectAsync(string address, int port, string username, string password, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:920:            if (string.IsNullOrEmpty(password))
src/SoulseekClient.cs:922:                throw new ArgumentException("Password may not be null or an empty string", nameof(password));
src/SoulseekClient.cs:980:            return ConnectInternalAsync(address, new IPEndPoint(ipAddress, port), username, password, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:991:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1067:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/SoulseekClient.cs:1081:        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
src/SoulseekClient.cs:1084:        /// <param name="token">The unique download token.</param>
src/SoulseekClient.cs:1086:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1102:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1115:        public Task<Transfer> DownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1124:                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
src/SoulseekClient.cs:1129:                throw new ArgumentException("The local filename must not be a null or empty string, or one consisting only of whitespace", nameof(localFilename));
src/SoulseekClient.cs:1157:            token ??= GetNextToken();
src/SoulseekClient.cs:1158:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:1160:            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
src/SoulseekClient.cs:1162:                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
src/SoulseekClient.cs:1172:            return DownloadToFileAsync(username, remoteFilename, localFilename, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1177:        ///     <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/SoulseekClient.cs:1189:        /// <param name="token">The unique download token.</param>
src/SoulseekClient.cs:1191:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1207:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1220:        public Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1229:                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
src/SoulseekClient.cs:1262:            token ??= GetNextToken();
src/SoulseekClient.cs:1263:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:1265:            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
src/SoulseekClient.cs:1267:                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
src/SoulseekClient.cs:1277:            return DownloadToStreamAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1284:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1312:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1339:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/SoulseekClient.cs:1364:        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
src/SoulseekClient.cs:1367:        /// <param name="token">The unique download token.</param>
src/SoulseekClient.cs:1369:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1382:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1395:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1415:            var downloadTask = DownloadAsync(username, remoteFilename, localFilename, size, startOffset, token, options, cancellationToken);
src/SoulseekClient.cs:1430:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/SoulseekClient.cs:1454:        /// <param name="token">The unique download token.</param>
src/SoulseekClient.cs:1456:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1469:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1482:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1502:            var downloadTask = DownloadAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);
src/SoulseekClient.cs:1518:        ///         <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:1527:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/SoulseekClient.cs:1528:        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
src/SoulseekClient.cs:1529:        /// <param name="token">The unique upload token.</param>
src/SoulseekClient.cs:1531:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1541:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1551:        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1571:            var uploadTask = UploadAsync(username, remoteFilename, localFilename, token, options, cancellationToken);
src/SoulseekClient.cs:1587:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:1596:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/SoulseekClient.cs:1599:        /// <param name="token">The unique upload token.</param>
src/SoulseekClient.cs:1601:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1612:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:1622:        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1642:            var uploadTask = UploadAsync(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);
src/SoulseekClient.cs:1659:        /// <param name="token">The unique token for the operation.</param>
src/SoulseekClient.cs:1660:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1671:        public Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1688:            token ??= GetNextToken();
src/SoulseekClient.cs:1689:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:1691:            return GetDirectoryContentsInternalAsync(username, directoryName, token.Value, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1695:        ///     Asynchronously fetches the current place of the specified <paramref name="filename"/> in the queue of the
src/SoulseekClient.cs:1699:        /// <param name="filename">The file to check.</param>
src/SoulseekClient.cs:1700:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1703:        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
src/SoulseekClient.cs:1711:        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1718:            if (string.IsNullOrWhiteSpace(filename))
src/SoulseekClient.cs:1720:                throw new ArgumentException("The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
src/SoulseekClient.cs:1728:            if (!DownloadDictionary.Any(d => d.Value.Username == username && d.Value.Filename == filename))
src/SoulseekClient.cs:1730:                throw new TransferNotFoundException($"A download of {filename} from user {username} is not active");
src/SoulseekClient.cs:1733:            return GetDownloadPlaceInQueueInternalAsync(username, filename, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1737:        ///     Gets the next token for use in client operations.
src/SoulseekClient.cs:1740:        ///     <para>Tokens are returned sequentially and the token value rolls over to 1 when it has reached <see cref="int.MaxValue"/>.</para>
src/SoulseekClient.cs:1743:        /// <returns>The next token.</returns>
src/SoulseekClient.cs:1750:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1782:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1814:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1843:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1872:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1905:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1933:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:1987:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2023:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2037:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2050:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2064:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2078:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2093:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2107:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2120:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2132:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2145:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2158:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2171:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2185:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2201:        /// <param name="cancellationToken">The token to minotor for cancellation requests.</param>
src/SoulseekClient.cs:2232:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2261:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2317:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2363:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2398:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2429:        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:2433:        /// <param name="token">The unique search token.</param>
src/SoulseekClient.cs:2435:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2441:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:2446:        public Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2468:            token ??= TokenFactory.NextToken();
src/SoulseekClient.cs:2469:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:2471:            if (Searches.ContainsKey(token.Value))
src/SoulseekClient.cs:2473:                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
src/SoulseekClient.cs:2489:            return SearchToCollectionAsync(query, scope, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:2494:        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:2499:        /// <param name="token">The unique search token.</param>
src/SoulseekClient.cs:2501:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2510:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:2515:        public Task<Search> SearchAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:2542:            token ??= TokenFactory.NextToken();
src/SoulseekClient.cs:2543:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:2545:            if (Searches.ContainsKey(token.Value))
src/SoulseekClient.cs:2547:                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
src/SoulseekClient.cs:2563:            return SearchToCallbackAsync(query, responseHandler, scope, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:2571:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2605:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2662:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2686:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2757:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2790:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2826:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2867:        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
src/SoulseekClient.cs:2907:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2933:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2959:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:2983:        ///     Asynchronously removes the specified <paramref name="username"/> from the server watch list for the current session.
src/SoulseekClient.cs:2990:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:3017:        ///     <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:3020:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/SoulseekClient.cs:3021:        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
src/SoulseekClient.cs:3022:        /// <param name="token">The unique upload token.</param>
src/SoulseekClient.cs:3024:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:3034:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:3044:        public Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:3053:                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
src/SoulseekClient.cs:3058:                throw new ArgumentException("The local filename must not be a null or empty string, or one consisting only of whitespace", nameof(localFilename));
src/SoulseekClient.cs:3080:            token ??= GetNextToken();
src/SoulseekClient.cs:3081:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:3083:            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
src/SoulseekClient.cs:3085:                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
src/SoulseekClient.cs:3095:            return UploadFromFileAsync(username, remoteFilename, localFilename, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:3101:        ///     specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/SoulseekClient.cs:3104:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/SoulseekClient.cs:3107:        /// <param name="token">The unique upload token.</param>
src/SoulseekClient.cs:3109:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:3120:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/SoulseekClient.cs:3130:        public Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:3139:                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
src/SoulseekClient.cs:3157:            token ??= GetNextToken();
src/SoulseekClient.cs:3158:            ThrowIfNegativeToken(token.Value, nameof(token));
src/SoulseekClient.cs:3160:            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
src/SoulseekClient.cs:3162:                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
src/SoulseekClient.cs:3172:            return UploadFromStreamAsync(username, remoteFilename, size, inputStreamFactory, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:3176:        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list for the current session.
src/SoulseekClient.cs:3183:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/SoulseekClient.cs:3397:        private async Task ChangePasswordInternalAsync(string password, CancellationToken cancellationToken)
src/SoulseekClient.cs:3406:                await ServerConnection.WriteAsync(new NewPassword(password), cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:3412:                throw new SoulseekClientException($"Failed to change password: {ex.Message}", ex);
src/SoulseekClient.cs:3415:            if (!response.Equals(password, StringComparison.Ordinal))
src/SoulseekClient.cs:3417:                throw new SoulseekClientException("Probably failed to change password; the response from the server doesn't match the specified password");
src/SoulseekClient.cs:3489:        private async Task ConnectInternalAsync(string address, IPEndPoint ipEndPoint, string username, string password, CancellationToken cancellationToken)
src/SoulseekClient.cs:3560:                    var loginBytes = new LoginRequest(MinorVersion, username, password).ToByteArray()
src/SoulseekClient.cs:3631:        private async Task<Transfer> DownloadToFileAsync(string username, string remoteFilename, string localFilename, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:3642:            return await DownloadToStreamAsync(username, remoteFilename, () => Task.FromResult((Stream)IOAdapter.GetFileStream(localFilename, fileMode, FileAccess.Write, FileShare.None)), size, startOffset, token, options, cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:3645:        private async Task<Transfer> DownloadToStreamAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:3649:            var download = new TransferInternal(TransferDirection.Download, username, remoteFilename, token, options)
src/SoulseekClient.cs:3668:                // we also can't allow the same token to be used across different transfers. we're checking for this in the public-scoped
src/SoulseekClient.cs:3675:                    throw new DuplicateTokenException($"The specified or generated token {download.Token} is already in progress");
src/SoulseekClient.cs:3738:                await peerConnection.WriteAsync(new TransferRequest(TransferDirection.Download, token, remoteFilename), cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:3744:                Diagnostic.Debug($"Received transfer request ACK for download of {GetDiagnosticLogValue(download.Filename)} from {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:3833:                // we'll do that with a cancellation token that we bind to the one that was passed into the method.
src/SoulseekClient.cs:3892:                var tokenBucket = DownloadTokenBucket;
src/SoulseekClient.cs:3900:                        return await tokenBucket.GetAsync(Math.Min(requestedBytes, bytesGrantedByCaller), cancelToken).ConfigureAwait(false);
src/SoulseekClient.cs:3905:                        tokenBucket.Return(grantedBytes - actualBytes);
src/SoulseekClient.cs:3926:                    // the logic in the Disconnected handler above was executed, and the transfer connection is dead
src/SoulseekClient.cs:4124:        private async Task<IReadOnlyCollection<Directory>> GetDirectoryContentsInternalAsync(string username, string directoryName, int token, CancellationToken cancellationToken)
src/SoulseekClient.cs:4128:                var waitKey = new WaitKey(MessageCode.Peer.FolderContentsResponse, username, token);
src/SoulseekClient.cs:4134:                await connection.WriteAsync(new FolderContentsRequest(token, directoryName), cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:4146:        private async Task<int> GetDownloadPlaceInQueueInternalAsync(string username, string filename, CancellationToken cancellationToken)
src/SoulseekClient.cs:4150:                var waitKey = new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename);
src/SoulseekClient.cs:4155:                await connection.WriteAsync(new PlaceInQueueRequest(filename), cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:4163:                throw new SoulseekClientException($"Failed to fetch place in queue for download of {filename} from {username}: {ex.Message}", ex);
src/SoulseekClient.cs:4697:        private async Task<Search> SearchToCallbackAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope, int token, SearchOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:4699:            var search = new SearchInternal(query, scope, token, options);
src/SoulseekClient.cs:4700:            var searchDescription = GetDiagnosticSearchDescription(query, token);
src/SoulseekClient.cs:4717:                    throw new DuplicateTokenException($"An active search with token {search.Token} is already in progress");
src/SoulseekClient.cs:4805:        private async Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchToCollectionAsync(SearchQuery query, SearchScope scope, int token, SearchOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:4814:            var search = await SearchToCallbackAsync(query, ResponseReceived, scope, token, options, cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:5005:        private static string GetDiagnosticSearchDescription(SearchQuery query, int token)
src/SoulseekClient.cs:5007:            return $"token {token}, query \"{GetDiagnosticLogValue(query?.SearchText)}\"";
src/SoulseekClient.cs:5018:        private static void ThrowIfNegativeToken(int token, string paramName)
src/SoulseekClient.cs:5020:            if (token < 0)
src/SoulseekClient.cs:5022:                throw new ArgumentOutOfRangeException(paramName, "The token must be greater than or equal to zero");
src/SoulseekClient.cs:5120:        private async Task<Transfer> UploadFromFileAsync(string username, string remoteFilename, string localFilename, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:5127:            return await UploadFromStreamAsync(username, remoteFilename, length, (_) => Task.FromResult((Stream)ioAdapter.GetFileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read)), token, options, cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:5130:        private async Task<Transfer> UploadFromStreamAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:5134:            var upload = new TransferInternal(TransferDirection.Upload, username, remoteFilename, token, options)
src/SoulseekClient.cs:5152:                // we also can't allow the same token to be used across different transfers. we're checking for this in the public-scoped
src/SoulseekClient.cs:5159:                    throw new DuplicateTokenException($"The specified or generated token {upload.Token} is already in progress");
src/SoulseekClient.cs:5269:                Diagnostic.Debug($"Received transfer request ACK for upload of {GetDiagnosticLogValue(upload.Filename)} to {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");
src/SoulseekClient.cs:5288:                // we'll do that with a cancellation token that we bind to the one that was passed into the method.
src/SoulseekClient.cs:5351:                    var tokenBucket = UploadTokenBucket;
src/SoulseekClient.cs:5359:                            return await tokenBucket.GetAsync(Math.Min(requestedBytes, bytesGrantedByCaller), cancelToken).ConfigureAwait(false);
src/SoulseekClient.cs:5364:                            tokenBucket.Return(grantedBytes - actualBytes);
src/SoulseekClient.cs:5382:                    // the logic in the Disconnected handler above was executed, and the transfer connection is dead
examples/Console/MusicBrainz.cs:39:        private static readonly Uri API_ROOT = new Uri("https://musicbrainz.org/ws/2");
src/SearchResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchResponse.cs:43:        /// <param name="token">The unique search token.</param>
src/SearchResponse.cs:49:        public SearchResponse(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
src/SearchResponse.cs:52:            Token = token;
src/SearchResponse.cs:123:        ///     Gets the unique search token.
docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md:30:| protocol token and minor-version constructors | Fixed | RT-091 | Outbound/internal protocol messages now reject negative tokens and login minor versions through `ProtocolArgumentValidator` before serializing impossible scalar identifiers. |
docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md:43:- Protocol token emissions and login minor-version emission are now bounded by `ProtocolArgumentValidator.RequireNonNegative` across server, peer, distributed, and initialization message constructors.
docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md:54:| Example Web API path, request, and lifecycle candidates | 390 | Fixed | Closed by `docs/dev/bug-council-sweep-webapi-2026-05-05.md`. |
docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md:57:| Security-sensitive material candidates | 2 | Fixed | Closed by `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`; both hits are scanner/baseline regex self-hits and the high-confidence secret gate remains active. |
src/RoomInfo.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/FileAttribute.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
docs/dev/bug-council-phases.md:16:| 4 | Generic `council_of_experts` repo | Done | (agent) | New repo at `/home/keith/Documents/code/council_of_experts` containing language-agnostic scanners, ledger/registry templates, schema docs, Roslyn analyzer template, README. Public push confirmed: https://github.com/snapetech/council_of_experts. |
docs/dev/bug-council-phases.md:26:| 15 | Add CSL0004 file-path analyzer | Done | (agent) | `TaintToFilePathAnalyzer` flags protocol-tainted `File`, `Directory`, `FileInfo`, `DirectoryInfo`, and `FileStream` path sinks without sanctioned containment validation; analyzer unit tests and the calibration corpus prove the lens fires on known-bad code and stays silent on contained paths. |
docs/dev/bug-council-phases.md:72:- [ ] Analyzer fires on a known unprotected `new byte[ReadInteger()]` snippet in a test fixture, and stays silent when `ProtocolCountReader.ReadValidatedCount` is in the path.
docs/mesh-migration-plan.md:3:This document plans a migration of slskdN mesh features from the slskdn application repository (`../slskdn`) into the slskNet.Runtime fork. The goal is to re-express the mesh's protocol/codec surface as runtime extensions so that slskdN-aware peers can rendezvous and exchange overlay metadata over the existing Soulseek peer-message channel, while leaving the BitTorrent-DHT path in slskdn as a server-independent fallback.
docs/mesh-migration-plan.md:19:Three compat-safe rendezvous paths exist in the current runtime and are sufficient for slskdN peers to discover each other without DHT:
docs/mesh-migration-plan.md:23:3. **Search-based rendezvous.** A magic search query yields announcements from slskdN clients via the existing distributed-network search path. Useful as a third channel and during cohort growth phases.
docs/mesh-migration-plan.md:25:The migration plan below assumes paths 1 and 2 as primary, with path 3 reserved for sparse-cohort cases.
docs/mesh-migration-plan.md:34:| `SendPeerMessageAsync(username, code, payload, ...)` | Send a raw slskdN-only peer message to a specific user, reusing existing peer-connection management and indirect-fallback paths. | Additive. |
docs/mesh-migration-plan.md:79:- `Mesh/ProofOfPossessionService.cs`
docs/mesh-migration-plan.md:99:Move `mesh_search_req` and `mesh_search_resp` into the runtime as slskdN-only peer messages. Legacy peers do not see the codes. slskdN peers gain an alternative search path that does not depend on the distributed-network parent topology.
docs/mesh-migration-plan.md:142:The intended end state runs two parallel rendezvous paths with clear roles:
docs/mesh-migration-plan.md:153:- A slskdN-only peer-message code that a misbehaving peer sends to a legacy client costs nothing on the legacy side because the code is unknown and dropped. Verify that the runtime's peer-message read path treats unknown codes as drop-and-continue rather than fatal.
docs/mesh-migration-plan.md:154:- Magic interest tags are publicly visible to anyone who queries the same interest. Treat them as cohort markers, not as secrets.
src/File.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/File.cs:41:        /// <param name="filename">The file name.</param>
src/File.cs:45:        public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null)
src/File.cs:48:            Filename = filename;
examples/Web/web/src/lib/util.js:26:    let path = fullPath;
examples/Web/web/src/lib/util.js:28:    if (path.lastIndexOf('\\') > 0)
examples/Web/web/src/lib/util.js:29:        path = path.substring(0, path.lastIndexOf('\\'));
examples/Web/web/src/lib/util.js:31:    if (path.lastIndexOf('/') > 0)
examples/Web/web/src/lib/util.js:32:        path = path.substring(0, path.lastIndexOf('/'));
examples/Web/web/src/lib/util.js:34:    return path;
examples/Web/web/src/lib/util.js:37:/* https://www.npmjs.com/package/js-file-download
examples/Web/web/src/lib/util.js:47:export const downloadFile = (data, filename, mime) => {
examples/Web/web/src/lib/util.js:54:        window.navigator.msSaveBlob(blob, filename);
examples/Web/web/src/lib/util.js:61:        tempLink.setAttribute('download', filename);
examples/Console/Utility.cs:99:        public static string ToLocalOSPath(this string path)
examples/Console/Utility.cs:101:            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
examples/Web/api/Program.cs:29:                .UseUrls("http://localhost:5000")
docs/dev/bug-council-sweep-2026-05-05.md:28:| `src/PeerCapabilityDescriptor.cs:35` | Existing guard | Existing | Feature inputs are normalized through trimming, blank/null filtering, distinct sorting, and immutable snapshot publication; capability hints are not authorization decisions. |
docs/dev/bug-council-sweep-2026-05-05.md:43:| `src/Common/WaitKey.cs:40` | Fixed | RT-063 | Params token parts are copied and equality handles null operands safely. |
docs/dev/bug-council-sweep-2026-05-05.md:57:| Example Web API path, request, and lifecycle candidates | 390 | Fixed | Closed by `docs/dev/bug-council-sweep-webapi-2026-05-05.md`; accepted shared-path, route-validation, cache-lifecycle, and upload lookup gaps were fixed. |
docs/dev/bug-council-sweep-2026-05-05.md:60:| Security-sensitive material candidates | 2 | Fixed | Closed by `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`; both hits are scanner/baseline regex self-hits and the high-confidence secret gate remains active. |
src/Soulseek.csproj:45:    <PackageProjectUrl>https://github.com/snapetech/slskNet.Runtime</PackageProjectUrl>
src/Soulseek.csproj:46:    <PackageLicense>https://github.com/snapetech/slskNet.Runtime/blob/slsknet-runtime-main/LICENSE</PackageLicense>
src/Soulseek.csproj:50:    <RepositoryUrl>https://github.com/snapetech/slskNet.Runtime</RepositoryUrl>
examples/Web/web/src/lib/transfers.js:7:export const download = ({ username, filename, size }) => {
examples/Web/web/src/lib/transfers.js:8:  return api.post(`/transfers/downloads/${username}`, { filename, size });
examples/Web/web/src/lib/session.js:4:  return (await api.get('/session/enabled')).data;
examples/Web/web/src/lib/session.js:8:  return api.get('/session');
examples/Web/web/src/lib/session.js:11:export const login = ({ username, password }) => {
examples/Web/web/src/lib/session.js:12:  return api.post('/session', { username, password });
examples/Web/api/Extensions.cs:15:        ///     Converts the given path to the local format (normalizes path separators).
examples/Web/api/Extensions.cs:17:        /// <param name="path"></param>
examples/Web/api/Extensions.cs:19:        public static string ToLocalOSPath(this string path)
examples/Web/api/Extensions.cs:21:            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:24:        public static string GetFullPathInsideRoot(string root, string path)
examples/Web/api/Extensions.cs:28:                throw new ArgumentException("Root path is missing or invalid", nameof(root));
examples/Web/api/Extensions.cs:31:            if (string.IsNullOrWhiteSpace(path))
examples/Web/api/Extensions.cs:33:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:37:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:44:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured root");
examples/Web/api/Extensions.cs:50:        public static string GetSafeOutputPath(string root, string path)
examples/Web/api/Extensions.cs:52:            if (string.IsNullOrWhiteSpace(path))
examples/Web/api/Extensions.cs:54:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:58:            var relativePath = ToSafeRelativePath(path);
examples/Web/api/Extensions.cs:63:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured output directory");
examples/Web/api/Extensions.cs:69:        public static string GetSharedRemotePath(string root, string path)
examples/Web/api/Extensions.cs:72:            var fullPath = GetFullPathInsideRoot(rootPath, path);
examples/Web/api/Extensions.cs:77:                throw new ArgumentException("Path does not contain a usable shared name", nameof(path));
examples/Web/api/Extensions.cs:84:        ///     Returns the directory from the given path, regardless of separator format.
examples/Web/api/Extensions.cs:86:        /// <param name="path"></param>
examples/Web/api/Extensions.cs:88:        public static string DirectoryName(this string path)
examples/Web/api/Extensions.cs:90:            var separator = path.Contains('\\') ? '\\' : '/';
examples/Web/api/Extensions.cs:91:            var parts = path.Split(separator);
examples/Web/api/Extensions.cs:112:                throw new ArgumentException("Root path is missing or invalid", nameof(root));
examples/Web/api/Extensions.cs:119:        private static string ToSafeRelativePath(string path)
examples/Web/api/Extensions.cs:121:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:131:                throw new ArgumentException("Path does not contain a usable file name", nameof(path));
src/MeshRendezvousService.cs:45:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/MeshRendezvousService.cs:55:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/MeshRendezvousService.cs:65:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/MeshRendezvousService.cs:69:            var token = cancellationToken ?? CancellationToken.None;
src/MeshRendezvousService.cs:70:            var users = await client.GetSimilarUsersAsync(token).ConfigureAwait(false);
src/MeshRendezvousService.cs:76:                    token.ThrowIfCancellationRequested();
src/MeshRendezvousService.cs:80:                        await client.SendPeerCapabilityAsync(username, cancellationToken: token).ConfigureAwait(false);
examples/Console/build/build.sh:7:warp-packer --arch windows-x64 --input_dir bin/Release/netcoreapp2.1/win-x64/publish --exec slsk-ex.exe --output "$dir"/slsk-ex.win-x64.exe
examples/Console/build/build.sh:10:warp-packer --arch linux-x64 --input_dir bin/Release/netcoreapp2.1/linux-x64/publish --exec slsk-ex --output "$dir"/slsk-ex.linux-x64
examples/Console/build/build.sh:14:warp-packer --arch macos-x64 --input_dir bin/Release/netcoreapp2.1/osx-x64/publish --exec slsk-ex --output "$dir"/slsk-ex.osx-x64
examples/Web/api/Trackers/SearchTracker.cs:25:            Searches.AddOrUpdate(id, search, (token, search) => search);
examples/Web/web/src/components/Transfers/TransferList.js:72:                                <Table.HeaderCell className='transferlist-filename'>File</Table.HeaderCell>
examples/Web/web/src/components/Transfers/TransferList.js:78:                            {files.sort((a, b) => getFileName(a.filename).localeCompare(getFileName(b.filename))).map((f, i) =>
examples/Web/web/src/components/Transfers/TransferList.js:87:                                    <Table.Cell className='transferlist-filename'>{getFileName(f.filename)}</Table.Cell>
examples/Web/web/src/lib/api.js:2:import { baseUrl, tokenKey, tokenPassthroughValue } from '../config';
examples/Web/web/src/lib/api.js:5:  return JSON.parse(sessionStorage.getItem(tokenKey) || localStorage.getItem(tokenKey));
examples/Web/web/src/lib/api.js:12:    const token = getToken();
examples/Web/web/src/lib/api.js:16:    if (token && token !== tokenPassthroughValue) {
examples/Web/web/src/lib/api.js:17:        config.headers.Authorization = 'Bearer ' + token;
examples/Web/web/src/lib/api.js:26:  if (error.response.status === 401 && error.response.config.url !== '/session') {
examples/Web/web/src/lib/api.js:27:    sessionStorage.removeItem(tokenKey);
examples/Web/web/src/lib/api.js:28:    localStorage.removeItem(tokenKey);
src/WishlistSearchScheduler.cs:84:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
examples/Web/web/src/components/Transfers/TransferGroup.js:16:        const obj = JSON.stringify({ directory: directoryName, filename: file.filename });
examples/Web/web/src/components/Transfers/TransferGroup.js:23:        this.state.selections.has(JSON.stringify({ directory: directoryName, filename: file.filename }));
examples/Web/web/src/components/Transfers/TransferGroup.js:32:                .files.find(f => f.filename === s.filename)
examples/Web/web/src/components/Transfers/TransferGroup.js:41:            .find(s => s.filename === file.filename);
examples/Web/web/src/components/Transfers/TransferGroup.js:68:        const { username, filename, size } = file;
examples/Web/web/src/components/Transfers/TransferGroup.js:71:            await transfers.download({username, filename, size });
examples/Web/api/Startup.cs:202:            // by a reverse proxy or having the base path removed
examples/Web/api/Startup.cs:205:                var path = context.Request.Path.ToString();
examples/Web/api/Startup.cs:207:                if (path.StartsWith("//"))
examples/Web/api/Startup.cs:209:                    context.Request.Path = new string(path.Skip(1).ToArray());
examples/Web/api/Startup.cs:279:                enqueueDownload: (username, endpoint, filename) => EnqueueDownloadAction(username, endpoint, filename, tracker),
examples/Web/api/Startup.cs:505:        /// <param name="token">The unique token for the request, supplied by the requesting user.</param>
examples/Web/api/Startup.cs:508:        private Task<IEnumerable<Soulseek.Directory>> DirectoryContentsResponseResolver(string username, IPEndPoint endpoint, int token, string directory)
examples/Web/api/Startup.cs:607:        /// <param name="filename">The filename of the requested file.</param>
examples/Web/api/Startup.cs:612:        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The upload task owns and disposes the cancellation token source in a finally block.")]
examples/Web/api/Startup.cs:613:        private Task EnqueueDownloadAction(string username, IPEndPoint endpoint, string filename, ITransferTracker tracker)
examples/Web/api/Startup.cs:616:            var localFilename = Extensions.GetFullPathInsideRoot(SharedDirectory, filename);
examples/Web/api/Startup.cs:626:            if (tracker.TryGet(TransferDirection.Upload, username, filename, out _))
examples/Web/api/Startup.cs:630:                Console.WriteLine($"[UPLOAD RE-REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:634:            // create a new cancellation token source so that we can cancel the upload from the UI.
examples/Web/api/Startup.cs:642:                    Console.WriteLine($"[UPLOAD SLOT REQUESTED] [{username}/{filename}]");
examples/Web/api/Startup.cs:647:                        addValueFactory: (_) => (filename, ReadyTimestamp: DateTime.UtcNow, enqueuedTimestamp, tcs),
examples/Web/api/Startup.cs:648:                        updateValueFactory: (_, _) => (filename, ReadyTimestamp: DateTime.UtcNow, enqueuedTimestamp, tcs));
examples/Web/api/Startup.cs:657:                    Console.WriteLine($"[UPLOAD SLOT RELEASED] [{username}/{filename}]");
examples/Web/api/Startup.cs:672:                    await Client.UploadAsync(username, filename, fileInfo.Length, (_) => Task.FromResult((Stream)stream), options: topts, cancellationToken: cts.Token);
examples/Web/api/Startup.cs:691:        /// <param name="token">The search token.</param>
examples/Web/api/Startup.cs:694:        private Task<SearchResponse> SearchResponseResolver(string username, int token, SearchQuery query)
examples/Web/api/Startup.cs:721:                        token,
examples/Console/Program.cs:29:        [Argument('p', "password")]
examples/Console/Program.cs:169:                    var path = $"{OutputDirectory}{Path.DirectorySeparatorChar}{Path.GetDirectoryName(file).Replace(Path.GetDirectoryName(Path.GetDirectoryName(file)), "")}";
examples/Console/Program.cs:170:                    var filename = Path.Combine(path, Path.GetFileName(file));
examples/Console/Program.cs:172:                    var transfer = await client.DownloadAsync(username, file, filename, startOffset: 0, token: index++, options: new TransferOptions(stateChanged: (e) =>
examples/Console/Program.cs:211:                    // GetDirectoryName() and GetFileName() only work when the path separator is the same as the current OS' DirectorySeparatorChar.
examples/Console/Program.cs:216:                    if (!System.IO.Directory.Exists(path))
examples/Console/Program.cs:218:                        System.IO.Directory.CreateDirectory(path);
examples/Console/Program.cs:259:                    var filename = file.Filename.ToLocalOSPath();
examples/Console/Program.cs:260:                    o($"    {Path.GetFileName(filename).PadRight(longest)}  {file.Size.ToMB(),7}  {$"{file.BitRate}kbps",9}  {TimeSpan.FromSeconds(file.Length ?? 0),7:m\\:ss}");
examples/Web/api/SharedFileCache.cs:59:                // potentially optimize with multi-valued insert https://stackoverflow.com/questions/16055566/insert-multiple-rows-in-sqlite
examples/Web/api/SharedFileCache.cs:99:            using var cmd = new SqliteCommand("CREATE VIRTUAL TABLE cache USING fts5(filename)", SQLite);
examples/Web/api/SharedFileCache.cs:103:        private void InsertFilename(string filename)
examples/Web/api/SharedFileCache.cs:105:            using var cmd = new SqliteCommand("INSERT INTO cache(filename) VALUES($filename)", SQLite);
examples/Web/api/SharedFileCache.cs:106:            cmd.Parameters.AddWithValue("$filename", filename);
examples/Web/api/DTO/Transfer.cs:43:        ///     Gets the filename of the file to be transferred.
examples/Web/api/DTO/Transfer.cs:73:        ///     Gets the remote unique token for the transfer.
examples/Web/api/DTO/Transfer.cs:98:        ///     Gets the unique token for the transfer.
examples/Web/web/src/components/App.js:3:import { tokenKey, tokenPassthroughValue } from '../config';
examples/Web/web/src/components/App.js:4:import * as session from '../lib/session';
examples/Web/web/src/components/App.js:24:    token: undefined,
examples/Web/web/src/components/App.js:37:        const securityEnabled = await session.getSecurityEnabled();
examples/Web/web/src/components/App.js:40:            this.setToken(sessionStorage, tokenPassthroughValue)
examples/Web/web/src/components/App.js:44:            token: this.getToken(),
examples/Web/web/src/components/App.js:56:            await session.check();
examples/Web/web/src/components/App.js:62:    getToken = () => JSON.parse(sessionStorage.getItem(tokenKey) || localStorage.getItem(tokenKey));
examples/Web/web/src/components/App.js:63:    setToken = (storage, token) => storage.setItem(tokenKey, JSON.stringify(token));
examples/Web/web/src/components/App.js:65:    login = (username, password, rememberMe) => {
examples/Web/web/src/components/App.js:68:                const response = await session.login({ username, password });
examples/Web/web/src/components/App.js:69:                this.setToken(rememberMe ? localStorage : sessionStorage, response.data.token);
examples/Web/web/src/components/App.js:78:        localStorage.removeItem(tokenKey);
examples/Web/web/src/components/App.js:79:        sessionStorage.removeItem(tokenKey);
examples/Web/web/src/components/App.js:89:        const { token, login } = this.state;
examples/Web/web/src/components/App.js:93:                {!token ?
examples/Web/web/src/components/App.js:140:                            {token !== tokenPassthroughValue && <Modal
examples/Web/web/src/components/App.js:155:                                <Route path='*/chat' render={(props) => this.withTokenCheck(<Chat {...props}/>)}/>
examples/Web/web/src/components/App.js:156:                                <Route path='*/rooms' render={(props) => this.withTokenCheck(<Rooms {...props}/>)}/>
examples/Web/web/src/components/App.js:157:                                <Route path='*/browse' render={(props) => this.withTokenCheck(<Browse {...props}/>)}/>
examples/Web/web/src/components/App.js:158:                                <Route path='*/uploads' render={(props) => this.withTokenCheck(<Transfers {...props} direction='upload'/>)}/>
examples/Web/web/src/components/App.js:159:                                <Route path='*/downloads' render={(props) => this.withTokenCheck(<Transfers {...props} direction='download'/>)}/>
examples/Web/web/src/components/App.js:160:                                <Route path='*/' render={(props) => this.withTokenCheck(<Search {...props}/>)}/>
examples/Web/web/src/components/App.css:55:.filelist-filename {
examples/Web/web/src/components/App.css:98:.transferlist-filename {
examples/Web/web/src/components/LoginForm.js:6:    password: '',
examples/Web/web/src/components/LoginForm.js:18:            if (this.state.username !== '' && this.state.password !== '') {
examples/Web/web/src/components/LoginForm.js:28:        const { username, password, rememberMe, ready } = this.state;
examples/Web/web/src/components/LoginForm.js:50:                                    type='password'
examples/Web/web/src/components/LoginForm.js:51:                                    onChange={(event) => this.handleChange('password', event.target.value)}
examples/Web/web/src/components/LoginForm.js:66:                                onClick={() => onLoginAttempt(username, password, rememberMe)}
examples/Web/api/DTO/SearchRequest.cs:58:        ///     Gets or sets the search token.
examples/Web/web/src/components/Shared/FileList.js:45:              <Table.HeaderCell className='filelist-filename'>File</Table.HeaderCell>
examples/Web/web/src/components/Shared/FileList.js:52:            {files.sort((a, b) => a.filename > b.filename ? 1 : -1).map((f, i) =>
examples/Web/web/src/components/Shared/FileList.js:62:                <Table.Cell className='filelist-filename'>{locked ? <Icon name='lock'/> : ''}{getFileName(f.filename)}</Table.Cell>
docs/fork-runtime-changes.md:15:- Keep regular direct and indirect peer/distributed/transfer connection attempts available as fallback paths.
docs/fork-runtime-changes.md:20:Type-1 obfuscation support covers peer-message (`P`), distributed-message (`D`), and file-transfer (`F`) streams. It is not encryption and keeps regular fallback paths available.
docs/fork-runtime-changes.md:30:- `PeerConnectionManager` can prefer a cached compatible obfuscated endpoint while racing regular direct and indirect paths.
docs/fork-runtime-changes.md:31:- `DistributedConnectionManager` can accept obfuscated distributed children, complete obfuscated solicited distributed `PierceFirewall` handoffs, and prefer compatible obfuscated distributed parent candidates while retaining regular direct/indirect fallback paths.
docs/fork-runtime-changes.md:60:- `ObfuscatedConnectionMatrixTests` uses loopback TCP sockets to prove obfuscated peer-message (`P`), distributed-message (`D`), and file-transfer (`F`) runtime paths.
docs/fork-runtime-changes.md:61:- The same matrix covers regular peer-message, distributed-message, and transfer fallback so compatibility paths stay under test alongside obfuscated paths.
docs/fork-runtime-changes.md:124:- The single-user `SendPrivateMessageAsync(string, string, CancellationToken?)` path is unchanged.
docs/fork-runtime-changes.md:177:Descriptors are signed over a canonical binary form that excludes the signature itself. `Ed25519PeerDescriptorSigner` uses BouncyCastle's netstandard-compatible Ed25519 implementation and derives a stable peer id from the public key. Verification is a primitive, not an authorization decision; callers can decide whether unsigned descriptors are acceptable for their deployment.
docs/fork-runtime-changes.md:193:The implementation has focused unit coverage for capability envelope round trips, registry updates, Ed25519 signing and verification, rendezvous probing, wishlist scheduling, and parser hardening. Existing distributed-network tests remain the compatibility guard for the upstream message path.
docs/fork-runtime-changes.md:199:The new Ed25519 implementation adds `BouncyCastle.Cryptography` `2.6.2`. NuGet metadata lists it under the MIT license, which is compatible with this GPL-3.0-only distribution model; it does not require changing the fork license. No credential material or generated secrets are committed by these changes.
examples/Web/api/DTO/QueueDownloadRequest.cs:6:        ///     Gets or sets the filename to download.
examples/Web/api/DTO/QueueDownloadRequest.cs:16:        ///     Gets or sets the optional transfer token.
examples/Web/web/src/components/Shared/DeprecationWarning.js:11:      <span>This application has been superseded by <a href="https://github.com/slskd/slskd">slskd</a>.</span>
examples/Web/api/Security/PBKDF2.cs:12:        ///     Gets a 256 bit (32 byte) key derived from the specified <paramref name="password"/> using PBKDF2/RFC 2898
examples/Web/api/Security/PBKDF2.cs:14:        /// <param name="password"></param>
examples/Web/api/Security/PBKDF2.cs:16:        public static byte[] GetKey(string password)
examples/Web/api/Security/PBKDF2.cs:24:            return KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, 32);
examples/Web/web/src/components/Browse/Directory.js:46:    const { filename, size } = file;
examples/Web/web/src/components/Browse/Directory.js:47:    return transfers.download({ username, filename, size });
tests/Soulseek.Tests.Unit/ServerInfoTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/FileAttributeTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/web/src/components/Search/Response.js:20:        let dir = getDirectoryName(file.filename);
examples/Web/web/src/components/Search/Response.js:55:        const { filename, size } = file;
examples/Web/web/src/components/Search/Response.js:56:        return transfers.download({ username, filename, size });
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:29:        public void Instantiates_With_Given_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, File file)
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:42:            var r = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, list, locked);
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:45:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:63:        public void Instantiates_With_Given_Response_And_List(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/SearchResponseTests.cs:65:            var r1 = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, null);
examples/Web/web/src/components/Browse/Browse.js:167:    const files = (selectedDirectory.files || []).map(f => ({ ...f, filename: `${name}${this.sep(name)}${f.filename}`}));
tests/Soulseek.Tests.Unit/RoomTickerTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/web/src/components/Chat/Chat.js:29:            active: sessionStorage.getItem(activeChatKey) || ''
examples/Web/web/src/components/Chat/Chat.js:122:            sessionStorage.setItem(activeChatKey, active);
src/SearchInternal.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchInternal.cs:48:        /// <param name="token">The unique search token.</param>
src/SearchInternal.cs:50:        public SearchInternal(SearchQuery query, SearchScope scope, int token, SearchOptions options = null)
src/SearchInternal.cs:54:            Token = token;
src/SearchInternal.cs:293:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
tests/Soulseek.Tests.Unit/RoomListTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ItemSimilarUsers.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/UserInterests.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/api/Controllers/TransfersController.cs:115:        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The cancellation token source is owned by the tracker after the first state/progress callback; untracked setup failures are disposed before returning.")]
examples/Web/api/Controllers/TransfersController.cs:229:        ///     Gets the downlaod for the specified username matching the specified filename, and requests
examples/Web/api/Controllers/TransfersController.cs:302:        ///     Gets the upload for the specified username matching the specified filename.
examples/Web/api/Controllers/TransfersController.cs:337:        [SuppressMessage("Security", "CA3003:Review code for file path injection vulnerabilities", Justification = "The remote file name is normalized to a relative path and confined to the configured output directory by Extensions.GetSafeOutputPath.")]
examples/Web/api/Controllers/TransfersController.cs:341:            var path = Path.GetDirectoryName(localFilename);
examples/Web/api/Controllers/TransfersController.cs:343:            if (!System.IO.Directory.Exists(path))
examples/Web/api/Controllers/TransfersController.cs:345:                System.IO.Directory.CreateDirectory(path);
src/SimilarUser.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/web/src/components/Rooms/Rooms.js:37:      active: sessionStorage.getItem(activeRoomKey) || ''
examples/Web/web/src/components/Rooms/Rooms.js:92:      sessionStorage.setItem(activeRoomKey, active);
src/ItemRecommendations.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/api/Controllers/SessionController.cs:44:        ///     This is a no-op provided so that the application can test for an expired token on load.
examples/Web/api/Controllers/SessionController.cs:110:            var token = new JwtSecurityToken(
examples/Web/api/Controllers/SessionController.cs:117:            return token;
src/RecommendationList.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/api/Controllers/ServerController.cs:76:            return BadRequest("Provide one of the following: address and port, username and password, or address, port, username and password");
src/Recommendation.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/RoomDataTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/Settings.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/Settings.cs:33:        ///     Gets the password to use when logging in.
tests/Soulseek.Tests.Integration/Settings.cs:43:        ///     Gets the peer password to use when running multi-client tests.
src/UserPresence.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/RawSearchResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/TransferStates.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/CharacterEncodingTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/RawBrowseResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/DomainModelValidationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/DomainModelValidationTests.cs:372:        [Fact(DisplayName = "UserCannotConnectEventArgs rejects negative token")]
tests/.CodeAnalysis/stylecop.json:2:  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
tests/.CodeAnalysis/stylecop.json:20:      "copyrightText": "    Copyright (c) {companyName}. All rights reserved.\n\n    This program is free software: you can redistribute it and/or modify\n    it under the terms of the GNU General Public License as published by\n    the Free Software Foundation, either version 3 of the License, or\n    (at your option) any later version.\n\n    This program is distributed in the hope that it will be useful,\n    but WITHOUT ANY WARRANTY; without even the implied warranty of\n    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\n    GNU General Public License for more details.\n\n    You should have received a copy of the GNU General Public License\n    along with this program.  If not, see https://www.gnu.org/licenses/.",
tests/Soulseek.Tests.Unit/BrowseResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/Properties/AssemblyInfo.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/TransferInternal.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/TransferInternal.cs:51:        /// <param name="filename">The filename of the file to be transferred.</param>
src/TransferInternal.cs:52:        /// <param name="token">The unique token for the transfer.</param>
src/TransferInternal.cs:54:        public TransferInternal(TransferDirection direction, string username, string filename, int token, TransferOptions options = null)
src/TransferInternal.cs:58:            Filename = filename;
src/TransferInternal.cs:59:            Token = token;
src/TransferInternal.cs:111:        ///     Gets the filename of the file to be transferred.
src/TransferInternal.cs:136:        ///     Gets or sets the remote unique token for the transfer.
src/TransferInternal.cs:217:        ///     Gets the unique token for the transfer.
src/TransferDirection.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/RoomTickerEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/BrowseEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Transfer.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Transfer.cs:43:        /// <param name="filename">The filename of the file to be transferred.</param>
src/Transfer.cs:44:        /// <param name="token">The unique token for the transfer.</param>
src/Transfer.cs:56:        /// <param name="remoteToken">The remote unique token for the transfer.</param>
src/Transfer.cs:62:            string filename,
src/Transfer.cs:63:            int token,
src/Transfer.cs:77:                filename,
src/Transfer.cs:78:                token,
src/Transfer.cs:120:            string filename,
src/Transfer.cs:121:            int token,
src/Transfer.cs:196:            Filename = filename;
src/Transfer.cs:197:            Token = token;
src/Transfer.cs:251:        ///     Gets the filename of the file to be transferred.
src/Transfer.cs:271:        ///     Gets the remote unique token for the transfer.
src/Transfer.cs:296:        ///     Gets the unique token for the transfer.
src/SoulseekClientState.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/DistributedEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ServerInfo.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchStates.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchScopeType.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/DirectoryTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchScope.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/SoulseekClientTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/SoulseekClientTests.cs:105:        [Fact(DisplayName = "GetNextToken returns sequential tokens")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:26:        [InlineData(@"C:\Users\alice\Music\secret.mp3", @"C:\\Users\\alice\\Music\\secret.mp3")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:27:        [InlineData("/home/alice/Music/secret.mp3", "/home/alice/Music/secret.mp3")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:28:        [InlineData(@"@@alias\folder\secret.mp3", @"@@alias\\folder\\secret.mp3")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:29:        [InlineData("folder/secret.mp3", "folder/secret.mp3")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:30:        [InlineData("secret.mp3", "secret.mp3")]
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:33:        public void GetDiagnosticLogValue_Preserves_Operator_Visible_Text(string filename, string expected)
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:37:                var actual = s.InvokeMethod<string>("GetDiagnosticLogValue", filename);
tests/Soulseek.Tests.Unit/Client/DiagnosticFileNameTests.cs:64:                Assert.Equal("token 42, query \"private phrase -excluded\"", actual);
src/SearchResponder.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchResponder.cs:80:        /// <param name="responseToken">The token matching the cached response to discard.</param>
src/SearchResponder.cs:90:                        var (username, token, query, searchResponse) = response;
src/SearchResponder.cs:92:                        Diagnostic.Debug($"Discarded cached search response {responseToken} to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:95:                            RaiseResponseDeliveryFailed(username, token, query, searchResponse);
src/SearchResponder.cs:119:        /// <param name="token">The token for the search request.</param>
src/SearchResponder.cs:122:        public async Task<bool> TryRespondAsync(string username, int token, string query)
src/SearchResponder.cs:124:            RaiseRequestReceived(username, token, query);
src/SearchResponder.cs:135:                searchResponse = await SoulseekClient.Options.SearchResponseResolver(username, token, SearchQuery.FromText(query)).ConfigureAwait(false);
src/SearchResponder.cs:139:                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:152:                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");
src/SearchResponder.cs:168:                    // but may respond later. cache the result along with the solicitation token that was sent so we can attempt a
src/SearchResponder.cs:174:                            SoulseekClient.Options.SearchResponseCache.AddOrUpdate(responseToken, (username, token, query, searchResponse));
src/SearchResponder.cs:176:                            Diagnostic.Debug($"Failed to connect to {username} with solicitation token {responseToken} to deliver search results for query '{query}' with token {token}.  Cached response for potential delayed delivery.");
src/SearchResponder.cs:180:                            Diagnostic.Warning($"Error caching undelivered search response {responseToken} for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:189:                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:190:                RaiseResponseDelivered(username, token, query, searchResponse);
src/SearchResponder.cs:196:                Diagnostic.Debug($"Failed to send search response to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:200:                    RaiseResponseDeliveryFailed(username, token, query, searchResponse);
src/SearchResponder.cs:218:        ///     This overload is called by the listener when an incoming connection is established with a pierce firewall token,
src/SearchResponder.cs:219:        ///     and if that token doesn't match a pending solicitation, and if the token matches a cached search response.  In this case,
src/SearchResponder.cs:222:        /// <param name="responseToken">The token matching the pending response to send.</param>
src/SearchResponder.cs:243:                    var (username, token, query, searchResponse) = record;
src/SearchResponder.cs:250:                        Diagnostic.Debug($"Sent cached response {responseToken} containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
src/SearchResponder.cs:251:                        RaiseResponseDelivered(username, token, query, searchResponse);
src/SearchResponder.cs:256:                        Diagnostic.Debug($"Failed to send cached search response {responseToken} to {username} for query '{query}' with token {token}: {ex.Message}", ex);
src/SearchResponder.cs:257:                        RaiseResponseDeliveryFailed(username, token, query, searchResponse);
src/SearchResponder.cs:299:        private void RaiseRequestReceived(string username, int token, string query)
src/SearchResponder.cs:302:                () => RequestReceived?.Invoke(this, new SearchRequestEventArgs(username, token, query)));
src/SearchResponder.cs:304:        private void RaiseResponseDelivered(string username, int token, string query, SearchResponse searchResponse)
src/SearchResponder.cs:307:                () => ResponseDelivered?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
src/SearchResponder.cs:309:        private void RaiseResponseDeliveryFailed(string username, int token, string query, SearchResponse searchResponse)
src/SearchResponder.cs:312:                () => ResponseDeliveryFailed?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse)));
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:34:            var token = new Random().Next();
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:36:            using (var search = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, new SearchOptions()))
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:54:            var token = new Random().Next();
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:56:            using (var search = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, new SearchOptions()))
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:85:        public void SearchRequestEventArgs_Instantiates_With_Context(string username, int token, string query)
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:87:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:88:            var e = new SearchRequestEventArgs(username, token, query);
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:91:            Assert.Equal(token, e.Token);
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:98:        public void SearchRequestResponseEventArgs_Instantiates_SearchResponse_And_Context(string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:100:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:101:            var e = new SearchRequestResponseEventArgs(username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:104:            Assert.Equal(token, e.Token);
tests/Soulseek.Tests.Unit/EventArgs/SearchEventArgsTests.cs:111:        [Fact(DisplayName = "Rejects negative token")]
tests/Soulseek.Tests.Unit/FileTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/FileTests.cs:30:        public void Instantiates_With_The_Given_Data(int code, string filename, long size, string extension, List<FileAttribute> attributeList)
tests/Soulseek.Tests.Unit/FileTests.cs:34:            var ex = Record.Exception(() => f = new File(code, filename, size, extension, attributeList));
tests/Soulseek.Tests.Unit/FileTests.cs:39:            Assert.Equal(filename, f.Filename);
tests/Soulseek.Tests.Unit/FileTests.cs:48:        public void Instantiates_With_Empty_Attributes_Given_No_AttributeList(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:52:            var ex = Record.Exception(() => f = new File(code, filename, size, extension));
tests/Soulseek.Tests.Unit/FileTests.cs:62:        public void BitDepth_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
tests/Soulseek.Tests.Unit/FileTests.cs:68:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:76:        public void BitDepth_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:78:            var f = new File(code, filename, size, extension);
tests/Soulseek.Tests.Unit/FileTests.cs:86:        public void BitRate_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
tests/Soulseek.Tests.Unit/FileTests.cs:92:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:100:        public void BitRate_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:102:            var f = new File(code, filename, size, extension);
tests/Soulseek.Tests.Unit/FileTests.cs:110:        public void SampleRate_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
tests/Soulseek.Tests.Unit/FileTests.cs:116:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:124:        public void SampleRate_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:126:            var f = new File(code, filename, size, extension);
tests/Soulseek.Tests.Unit/FileTests.cs:134:        public void Length_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
tests/Soulseek.Tests.Unit/FileTests.cs:140:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:148:        public void Length_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:150:            var f = new File(code, filename, size, extension);
tests/Soulseek.Tests.Unit/FileTests.cs:158:        public void IsVariableBitRate_Returns_True_When_Attribute_Is_1(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:162:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:169:        public void IsVariableBitRate_Returns_False_When_Attribute_Is_0(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:173:            var f = new File(code, filename, size, extension, list);
tests/Soulseek.Tests.Unit/FileTests.cs:180:        public void IsVariableBitRate_Returns_Null_When_Attribute_Is_Not_Present(int code, string filename, long size, string extension)
tests/Soulseek.Tests.Unit/FileTests.cs:184:            var f = new File(code, filename, size, extension, list);
src/SearchQuery.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/SearchQuery.cs:80:            IEnumerable<string> tokens = searchText?.Split(' ') ?? Enumerable.Empty<string>();
src/SearchQuery.cs:82:            var excludedTokens = tokens.Where(t => t.StartsWith("-", IgnoreCase) && t.Length > 1);
src/SearchQuery.cs:85:            Terms = tokens.Where(token => !excludedTokens.Contains(token)).ToList().AsReadOnly();
src/Search.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Search.cs:40:        /// <param name="token">The unique search token.</param>
src/Search.cs:45:        public Search(SearchQuery query, SearchScope scope, int token, SearchStates state, int responseCount, int fileCount, int lockedFileCount)
src/Search.cs:80:            Token = token;
tests/Soulseek.Tests.Unit/Diagnostics/DiagnosticEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/SoulseekClientEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/RoomTicker.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/RoomInfoTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/BrowseOptionsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Diagnostics/GlobalDiagnosticTestsCollection.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/RoomList.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:29:        internal void TransferEventArgs_Instantiates_With_The_Given_Data(TransferDirection direction, string username, string filename, int token, TransferOptions options)
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:31:            var dl = new TransferInternal(direction, username, filename, token, options);
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:40:        internal void TransferProgressUpdatedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, int size, int bytesDownloaded)
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:42:            var dl = new TransferInternal(TransferDirection.Download, username, filename, token)
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:54:        internal void TransferStateChangedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, TransferStates transferStates)
tests/Soulseek.Tests.Unit/EventArgs/TransferEventArgsTests.cs:58:            var dl = new TransferInternal(TransferDirection.Download, username, filename, token);
src/RoomData.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/ConnectionOptionsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Diagnostics/GlobalDiagnosticTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/EventBridgeTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/RawSearchResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
docs/Soulseek Protocol Documentation.html:20:2003-01-26: Added peer messages and login sequence, had revelation about reusing existing peer socket from search result for file request, <a href="http://sourceforge.net/users/brienigma/">BriEnigma</a><br>
docs/Soulseek Protocol Documentation.html:21:2003-01-20: Initial revision, <a href="http://sourceforge.net/users/brienigma/">BriEnigma</a><br>
docs/Soulseek Protocol Documentation.html:24:latest version to the <a href="http://sourceforge.net/docman/?group_id=69129">SourceForge
docs/Soulseek Protocol Documentation.html:30:of effort by members of the <a href="http://sourceforge.net/projects/soleseek/">SoleSeek</a>
docs/Soulseek Protocol Documentation.html:32:<a href="http://www.sensi.org/~ak/pyslsk/">Python SoulSeek</a> source code, as well as
docs/Soulseek Protocol Documentation.html:35:<a href="http://www.eff.org/IP/DMCA/">DMCA</a> for reverse engineering the
docs/Soulseek Protocol Documentation.html:109:string is of length 6 and contains the letters "secret".
docs/Soulseek Protocol Documentation.html:406:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:421:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:460:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:469:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:534:<tr><td class=standout>integer</td><td class=standout>token (token of original file request?)</td></tr>
docs/Soulseek Protocol Documentation.html:544:<tr><td class=standout>integer</td><td class=standout>token (token of original file request?)</td></tr>
docs/Soulseek Protocol Documentation.html:605:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:606:<tr><td class=standout>string</td><td class=standout>filename</td></tr>
docs/Soulseek Protocol Documentation.html:645:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:693:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:703:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:766:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:799:<tr><td class=standout>string</td><td class=standout>Dir#1, File#1 filename</td></tr>
docs/Soulseek Protocol Documentation.html:826:<tr><td class=standout>integer</td><td class=standout>token</td></tr>
docs/Soulseek Protocol Documentation.html:829:<tr><td class=standout>string</td><td class=standout>File #1, filename</td></tr>
docs/Soulseek Protocol Documentation.html:875:document.  Token is [new, unique token? taken from another message?].<br>
docs/Soulseek Protocol Documentation.html:880:<tr><td class=standout>string</td><td class=standout>filename</a></td></tr>
docs/Soulseek Protocol Documentation.html:927:<tr><td class=standout>string</td><td class=standout>filename</td></tr>
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:28:        public void UserCannotConnectEventArgs_Instantiates_With_The_Given_Data(int token, string username)
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:30:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:31:            var e = new UserCannotConnectEventArgs(new CannotConnect(token, username));
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:34:            Assert.Equal(token, e.Token);
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:39:        public void DownloadDeniedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, string message)
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:41:            var e = new DownloadDeniedEventArgs(username, filename, message);
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:44:            Assert.Equal(filename, e.Filename);
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:50:        public void DownloadFailedEventArgs_Instantiates_With_The_Given_Data(string username, string filename)
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:52:            var e = new DownloadFailedEventArgs(username, filename);
tests/Soulseek.Tests.Unit/EventArgs/UserEventArgsTests.cs:55:            Assert.Equal(filename, e.Filename);
src/RawBrowseResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:184:        private static async Task ConnectWithRetryAsync(SoulseekClient client, string username, string password)
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:194:                        await client.ConnectAsync(username, password, cancellationTokenSource.Token);
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:272:                        searchResponseResolver: (username, token, query) => Task.FromResult<SearchResponse>(CreateSearchResponse(token, remoteFilename, payload.Length)),
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:274:                        enqueueDownload: (username, endpoint, filename) => EnqueuePeerUploadAsync(peer, username, filename, remoteFilename, payload),
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:275:                        placeInQueueResolver: (username, endpoint, filename) => Task.FromResult<int?>(0)));
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:309:            private static SearchResponse CreateSearchResponse(int token, string remoteFilename, int size)
tests/Soulseek.Tests.Integration/LiveSoulseekNetworkTests.cs:312:                return new SearchResponse(Settings.PeerUsername, token, hasFreeUploadSlot: true, uploadSpeed: 1, queueLength: 0, new[] { file });
src/Properties/AssemblyInfo.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs:143:                int tokens = 0;
tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs:144:                var ex = await Record.ExceptionAsync(async() => tokens = await t.GetAsync(11));
tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs:147:                Assert.Equal(10, tokens);
tests/Soulseek.Tests.Unit/Common/TokenBucketTests.cs:152:        [Fact(DisplayName = "GetAsync returns available tokens if request exceeds available count")]
tests/Soulseek.Tests.Unit/Client/RemovePrivateRoomModeratorAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchQueryTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchQueryTests.cs:127:        [Fact(DisplayName = "Parses single character tokens and punctuation from search text")]
src/Options/PeerObfuscationOptions.cs:17://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/SearchOptionsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Common/WaitKeyTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/TransferOptions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/TransferOptions.cs:38:            (tx, s, token) => Task.FromResult(int.MaxValue);
src/Options/TransferOptions.cs:41:            (tx, token) => Task.CompletedTask;
src/Options/TransferOptions.cs:198:        /// <param name="stateChanged">A new delegate to execute prior to the existing delegate.</param>
tests/Soulseek.Tests.Unit/Client/RemovePrivateRoomMemberAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Diagnostics/DiagnosticFactoryTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/SoulseekClientOptionsPatch.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:53:        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws ArgumentException on bad filename")]
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:58:        public async Task GetDownloadPlaceInQueueAsync_Throws_ArgumentException_On_Bad_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:64:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync("a", filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:92:        public async Task GetDownloadPlaceInQueueAsync_Throws_TransferNotFoundException_When_Downloads_From_Username_Not_Found(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:98:                var transfer = new TransferInternal(TransferDirection.Download, "different", filename, 1);
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:105:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:114:        public async Task GetDownloadPlaceInQueueAsync_Throws_TransferNotFoundException_When_Download_Not_Found(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:127:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:136:        public async Task GetDownloadPlaceInQueueAsync_Returns_Expected_Info(string username, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:138:            var result = new PlaceInQueueResponse(filename, placeInQueue);
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:163:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:167:                var place = await s.GetDownloadPlaceInQueueAsync(username, filename);
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:175:        public async Task GetDownloadPlaceInQueueAsync_Uses_Given_CancellationToken(string username, string filename, int placeInQueue, CancellationToken cancellationToken)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:177:            var result = new PlaceInQueueResponse(filename, placeInQueue);
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:202:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:206:                var place = await s.GetDownloadPlaceInQueueAsync(username, filename, cancellationToken: cancellationToken);
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:216:        public async Task GetDownloadPlaceInQueueAsync_Throws_UserOfflineException_On_User_Offline(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:230:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:234:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:243:        public async Task GetDownloadPlaceInQueueAsync_Throws_SoulseekClientException_On_Exception(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:268:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:272:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:281:        public async Task GetDownloadPlaceInQueueAsync_Throws_TimeoutException_On_Timeout(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:306:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:310:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:319:        public async Task GetDownloadPlaceInQueueAsync_Throws_OperationCanceledException_On_Cancellation(string username, string filename)
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:344:                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/GetDownloadPlaceInQueueAsyncTests.cs:348:                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));
tests/Soulseek.Tests.Unit/Network/ConnectionFactoryTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:33:        [Theory(DisplayName = "ChangePasswordAsync throws ArgumentException on bad password")]
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:38:        public async Task ChangePasswordAsync_Throws_ArgumentException_On_Null_Username(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:44:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:72:        public async Task ChangePasswordAsync_Succeeds_On_Matching_Confirmation(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:76:                .Returns(Task.FromResult(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:86:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:94:        public async Task ChangePasswordAsync_Uses_Given_CancellationToken(string password, CancellationToken cancellationToken)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:98:                .Returns(Task.FromResult(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:108:                await s.ChangePasswordAsync(password, cancellationToken);
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:116:        public async Task ChangePasswordAsync_Throws_On_Mismatching_Confirmation(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:120:                .Returns(Task.FromResult(password + "!"));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:130:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:134:                Assert.True(ex.Message.ContainsInsensitive("doesn't match the specified password"));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:139:        [Fact(DisplayName = "ChangePasswordAsync uses ordinal password confirmation")]
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:158:                Assert.True(ex.Message.ContainsInsensitive("doesn't match the specified password"));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:164:        public async Task ChangePasswordAsync_Throws_SoulseekClientException_On_Throw(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:168:                .Returns(Task.FromResult(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:178:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:188:        public async Task ChangePasswordAsync_Throws_TimeoutException_On_Timeout(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:192:                .Returns(Task.FromResult(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:202:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:211:        public async Task ChangePasswordAsync_Throws_OperationCanceledException_On_Cancel(string password)
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:215:                .Returns(Task.FromResult(password));
tests/Soulseek.Tests.Unit/Client/ChangePasswordAsyncTests.cs:225:                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:275:        [InlineData(MessageCode.Server.Login, "token", null)]
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:277:        [InlineData(MessageCode.Server.Login, "token", 10000)]
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:278:        internal void Wait_Invocation_Creates_Valid_Wait(MessageCode.Server code, string token, int? timeout)
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:280:            var key = new WaitKey(code, token);
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:317:        [InlineData(MessageCode.Server.Login, "token", null)]
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:319:        [InlineData(MessageCode.Server.Login, "token", 10000)]
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:320:        internal void Non_Generic_Wait_Invocation_Creates_Valid_Wait(MessageCode.Server code, string token, int? timeout)
tests/Soulseek.Tests.Unit/Common/WaiterTests.cs:322:            var key = new WaitKey(code, token);
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:33:        public void Instantiates_Properly(string username, string password)
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:40:            var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:48:            Assert.Equal(password, o.Password);
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:123:        public void Throws_ArgumentException_On_Bad_Input(string address, int port, string username, string password)
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:129:                var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:137:        [Fact(DisplayName = "Does not throw if username and password are null")]
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:144:                var ex = Record.Exception(() => o = new ProxyOptions("127.0.0.1", 1, username: null, password: null));
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:170:        public void Throws_ArgumentOutOfRangeException_On_Bad_Input(string address, int port, string username, string password)
tests/Soulseek.Tests.Unit/Options/ProxyOptionsTests.cs:176:                var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));
tests/Soulseek.Tests.Unit/Common/TokenFactoryTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Common/TokenFactoryTests.cs:41:        [Theory(DisplayName = "First token is start"), AutoData]
tests/Soulseek.Tests.Unit/Common/TokenFactoryTests.cs:52:        [Theory(DisplayName = "Returns sequential tokens"), AutoData]
src/Options/SoulseekClientOptions.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/SoulseekClientOptions.cs:84:        /// <param name="startingToken">The starting value for download and search tokens.</param>
src/Options/SoulseekClientOptions.cs:404:        ///     Gets the starting value for download and search tokens. (Default = 0).
tests/Soulseek.Tests.Unit/Common/ExtensionsTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/PingServerAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/SoulseekClientOptionsPatchTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/SearchOptions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/TransferOptionsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/TransferOptionsTests.cs:185:        [Fact(DisplayName = "WithAdditionalStateChanged returns copy that executes both StateChanged")]
src/Options/ProxyOptions.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/ProxyOptions.cs:41:        /// <param name="address">The address of the proxy server to which to connect.</param>
src/Options/ProxyOptions.cs:43:        /// <param name="username">The username for the proxy, if applicable.</param>
src/Options/ProxyOptions.cs:44:        /// <param name="password">The password for the proxy, if applicable.</param>
src/Options/ProxyOptions.cs:45:        public ProxyOptions(string address, int port, string username = null, string password = null)
src/Options/ProxyOptions.cs:57:            if (username == default != (password == default))
src/Options/ProxyOptions.cs:59:                throw new ArgumentException("Username and password must both be specified");
src/Options/ProxyOptions.cs:69:                if (password.Length < 1 || password.Length > 255)
src/Options/ProxyOptions.cs:71:                    throw new ArgumentOutOfRangeException(nameof(password), "The password must be between 1 and 255 characters");
src/Options/ProxyOptions.cs:92:            Password = password;
src/Options/ProxyOptions.cs:96:        ///     Gets the address of the proxy server to which to connect.
src/Options/ProxyOptions.cs:101:        ///     Gets the resolved proxy server address.
src/Options/ProxyOptions.cs:106:        ///     Gets the resolved proxy server endpoint.
src/Options/ProxyOptions.cs:111:        ///     Gets the password for the proxy, if applicable.
src/Options/ProxyOptions.cs:121:        ///     Gets the username for the proxy, if applicable.
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:86:        [Theory(DisplayName = "TryDiscard removes token from cache"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:87:        public void TryDiscard_Removes_Token_From_Cache(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:92:            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:106:        public void TryDiscard_Raises_ResponseDeliveryFailed_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:111:            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:124:            Assert.Equal(token, args.Token);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:131:        public void TryDiscard_Does_Not_Throw_Raising_Unbound_ResponseDeliveryFailed_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:136:            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:148:        public void TryDiscard_Returns_True_If_ResponseDeliveryFailed_Handler_Throws(int responseToken, string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:155:            var record = (username, token, query, (SearchResponse)searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:172:        public void TryDiscard_Produces_Debug_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:177:            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:191:        public void TryDiscard_Disposes_Raw_Search_Response_Stream(int responseToken, string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:198:            var record = (username, token, query, (SearchResponse)searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:270:        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Is_Null(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:274:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:281:        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Throws(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:285:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:292:        public async Task TryRespondAsync_Generates_Warning_If_ResponseResolver_Throws(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:298:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:307:        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Returns_Null(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:311:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:318:        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Returns_Zero_Files(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:320:            var response = new SearchResponse(username, token, false, 0, 0, new List<File>());
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:323:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:330:        public async Task TryRespondAsync_Raises_RequestReceived(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:337:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:342:            Assert.Equal(token, args.Token);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:348:        public async Task TryRespondAsync_Continues_If_RequestReceived_Handler_Throws(string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:360:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:369:        public async Task TryRespondAsync_Sends_Response_And_Returns_True(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:383:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:392:        public async Task TryRespondAsync_Raises_ResponseDelivered_When_Sending_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:409:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:415:            Assert.Equal(token, args.Token);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:422:        public async Task TryRespondAsync_Returns_True_If_ResponseDelivered_Handler_Throws(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:441:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:451:        public async Task TryRespondAsync_Generates_Debug_When_Resolving_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:465:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:474:        public async Task TryRespondAsync_Generates_Debug_When_Sending_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:488:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:497:        public async Task TryRespondAsync_Returns_False_On_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:511:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:518:        public async Task TryRespondAsync_Caches_Response_On_Connect_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:536:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:540:            var value = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:546:        public async Task TryRespondAsync_Sends_Raw_Search_Response(string username, int token, string query, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:562:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:572:        public async Task TryRespondAsync_Raises_ResponseDeliveryFailed_When_Raw_Response_Write_Fails(string username, int token, string query, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:599:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:610:        public async Task TryRespondAsync_Generates_Warning_On_Cache_Add_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:612:            var value = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:633:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:642:        public async Task TryRespondAsync_Generates_Debug_On_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:656:            var responded = await responder.TryRespondAsync(username, token, query);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:664:        [Theory(DisplayName = "TryRespondAsync token returns false if cache is null"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:675:        [Theory(DisplayName = "TryRespondAsync token returns false if not cached"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:692:        [Theory(DisplayName = "TryRespondAsync token returns false if cache throws"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:709:        [Theory(DisplayName = "TryRespondAsync token produces warning if cache throws"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:730:        [Theory(DisplayName = "TryRespondAsync token returns true if delivered"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:731:        public async Task TryRespondAsync_Token_Returns_True_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:733:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:753:        [Theory(DisplayName = "TryRespondAsync token sends raw search response"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:754:        public async Task TryRespondAsync_Token_Sends_Raw_Search_Response(int responseToken, string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:758:            var record = (username, token, query, (SearchResponse)searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:779:        [Theory(DisplayName = "TryRespondAsync token raises ResponseDeliveryFailed before disposing raw response stream"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:780:        public async Task TryRespondAsync_Token_Raises_ResponseDeliveryFailed_Before_Disposing_Raw_Response_Stream(int responseToken, string username, int token, string query)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:784:            var record = (username, token, query, (SearchResponse)searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:810:        [Theory(DisplayName = "TryRespondAsync token produces debug if delivered"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:811:        public async Task TryRespondAsync_Token_Produces_Debug_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:813:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:833:        [Theory(DisplayName = "TryRespondAsync token raises ResponseDelivered if delivered"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:834:        public async Task TryRespondAsync_Token_Raises_ResponseDelivered_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:836:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:856:            Assert.Equal(token, args.Token);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:862:        [Theory(DisplayName = "TryRespondAsync token does not throw raising unbound ResponseDelivered if delivered"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:863:        public async Task TryRespondAsync_Token_Does_Not_Throw_Raising_Unbound_ResponseDelivered_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:865:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:883:        [Theory(DisplayName = "TryRespondAsync token returns true if ResponseDelivered handler throws"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:884:        public async Task TryRespondAsync_Token_Returns_True_If_ResponseDelivered_Handler_Throws(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:886:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:912:        [Theory(DisplayName = "TryRespondAsync token returns false if delivery fails"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:913:        public async Task TryRespondAsync_Token_Returns_False_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:915:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:932:        [Theory(DisplayName = "TryRespondAsync token produces debug if delivery fails"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:933:        public async Task TryRespondAsync_Token_Produces_Debug_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:935:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:956:        [Theory(DisplayName = "TryRespondAsync token raises ResponseDeliveryFailed if delivery fails"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:957:        public async Task TryRespondAsync_Token_Raises_ResponseDeliveryFailed_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:959:            var record = (username, token, query, searchResponse);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:978:            Assert.Equal(token, args.Token);
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:984:        [Theory(DisplayName = "TryRespondAsync token does not throw raising unbound ResponseDeliveryFailed if delivery fails"), AutoData]
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:985:        public async Task TryRespondAsync_Token_Does_Not_Throw_Raising_Unbound_ResponseDeliveryFailed_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
tests/Soulseek.Tests.Unit/SearchResponderTests.cs:987:            var record = (username, token, query, searchResponse);
src/Options/ConnectionOptions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Options/ConnectionOptions.cs:47:        /// <param name="proxyOptions">Optional SOCKS 5 proxy configuration options.</param>
src/Options/ConnectionOptions.cs:55:            ProxyOptions proxyOptions = null,
src/Options/ConnectionOptions.cs:90:            ProxyOptions = proxyOptions;
src/Options/ConnectionOptions.cs:115:        ///     Gets the optional SOCKS 5 proxy configuration options.
src/Options/ConnectionOptions.cs:146:                proxyOptions: ProxyOptions,
src/Options/BrowseOptions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/MessageFrameValidator.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:22:        [Fact(DisplayName = "Web API path guard accepts paths inside the configured root")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:40:        [Fact(DisplayName = "Web API path guard rejects sibling prefix escapes")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:48:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:61:        [Fact(DisplayName = "Web API output path keeps absolute remote names under the output root")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:79:        [Fact(DisplayName = "Web API shared remote path is relative to the configured root")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:98:        [Fact(DisplayName = "Web API shared remote path rejects paths outside the configured root")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:106:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:119:        [Fact(DisplayName = "Shared file cache advertises relative filenames")]
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:169:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:170:            Directory.CreateDirectory(path);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:171:            return path;
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:91:        [Fact(DisplayName = "GetDirectoryContentsAsync throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:98:                var ex = await Record.ExceptionAsync(() => s.GetDirectoryContentsAsync("username", "directory", token: -1));
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:102:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:271:        [Theory(DisplayName = "GetDirectoryContentsAsync uses given token"), AutoData]
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:272:        public async Task GetDirectoryContentsAsync_Uses_Given_Token(string username, string directory, int token)
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:296:                var dir = await s.GetDirectoryContentsAsync(username, directory, token);
tests/Soulseek.Tests.Unit/Client/GetDirectoryContentsAsyncTests.cs:304:                    It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new FolderContentsRequest(token, directory).ToByteArray())),
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:46:        public async Task Throws_ArgumentException_On_Bad_Credentials(string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:50:                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:123:        public async Task Address_Throws_ArgumentException_On_Bad_Input(string address, int port, string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:127:                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, port, username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:136:        public async Task Throws_InvalidOperationException_When_Already_Connected(string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:142:                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:151:        public async Task Address_Throws_InvalidOperationException_If_Connected(IPEndPoint endpoint, string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:159:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:168:        public async Task Throws_InvalidOperationException_If_Connecting(string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:176:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:185:        public async Task Address_Throws_InvalidOperationException_If_Connecting(IPEndPoint endpoint, string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:193:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:202:        public async Task Throws_InvalidOperationException_If_Logging_In(string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:210:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:219:        public async Task Address_Throws_InvalidOperationException_If_Logging_In(IPEndPoint endpoint, string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:227:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:312:        public async Task Connects_And_Logs_In(string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:318:                await client.ConnectAsync(username, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:323:            var expectedBytes = new LoginRequest(minorVersion: 9999, username, password).ToByteArray()
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:419:        public async Task Exits_Gracefully_If_Already_Connected_And_Logged_In(IPEndPoint endpoint, string username, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:428:                var task = s.InvokeMethod<Task>("ConnectInternalAsync", endpoint.Address.ToString(), endpoint, username, password, null);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:438:        public async Task Uses_Given_CancellationToken(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:446:                await client.ConnectAsync(user, password, cancellationToken);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:457:        public async Task Address_uses_Given_CancellationToken(IPEndPoint endpoint, string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:465:                await client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, user, password, cancellationToken);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:476:        public async Task Starts_Listener_On_Success(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:483:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:493:        public async Task Sets_Listen_Port_On_Success(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:500:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:511:        public async Task LoginAsync_Configures_Distributed_Network_With_Parent_Info_On_Success(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:517:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:525:        public async Task Sets_PrivateRoomToggle_On_Success(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:531:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:539:        public async Task Raises_ServerInfoReceived_On_Login(string user, string password, bool isSupporter)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:552:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:564:        public async Task Sets_ServerInfo_On_Login(string user, string password, bool isSupporter)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:573:                await client.ConnectAsync(user, password);
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:581:        public async Task Disconnects_And_Throws_LoginException_On_Login_Rejection(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:590:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(user, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:601:        public async Task Throws_Promptly_When_Server_Disconnects_During_Login(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:621:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(user, password));
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:634:        public async Task LoginAsync_Throws_SoulseekClientException_On_Message_Write_Exception(string user, string password)
tests/Soulseek.Tests.Unit/Client/ConnectAsyncTests.cs:643:                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(user, password));
tests/Soulseek.Tests.Unit/TestIsolationExtensions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/TestExtensions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/JoinLeaveRoomAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/ObfuscatedTransferConnection.cs:17://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/ObfuscatedTransferConnection.cs:174:            var token = cancellationToken ?? CancellationToken.None;
src/Network/Tcp/ObfuscatedTransferConnection.cs:181:                    await ReadNextFrameAsync(token).ConfigureAwait(false);
src/Network/Tcp/ObfuscatedTransferConnection.cs:186:                var bytesGranted = Math.Min(bytesAvailable, await governor(bytesAvailable, token).ConfigureAwait(false));
src/Network/Tcp/ObfuscatedTransferConnection.cs:200:                await outputStream.WriteAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
src/Network/Tcp/ObfuscatedTransferConnection.cs:205:            await outputStream.FlushAsync(token).ConfigureAwait(false);
src/Network/Tcp/ObfuscatedTransferConnection.cs:239:            var token = cancellationToken ?? CancellationToken.None;
src/Network/Tcp/ObfuscatedTransferConnection.cs:248:                var bytesGranted = Math.Min(bytesToRead, await governor(bytesToRead, token).ConfigureAwait(false));
src/Network/Tcp/ObfuscatedTransferConnection.cs:255:                var bytesRead = await inputStream.ReadAsync(buffer, 0, bytesGranted, token).ConfigureAwait(false);
src/Network/Tcp/ObfuscatedTransferConnection.cs:264:                await innerConnection.WriteAsync(EncodeFrame(payload), token).ConfigureAwait(false);
src/Network/Tcp/RotatedObfuscation.cs:17://     along with this program.  If not, see https://www.gnu.org/licenses/.
examples/Web/web/yarn.lock:7:  resolved "https://registry.yarnpkg.com/@apideck/better-ajv-errors/-/better-ajv-errors-0.3.2.tgz#cd6d3814eda8aee38ee2e3fa6457be43af4f8361"
examples/Web/web/yarn.lock:16:  resolved "https://registry.yarnpkg.com/@babel/code-frame/-/code-frame-7.16.7.tgz#44416b6bd7624b998f5b1af5d470856c40138789"
examples/Web/web/yarn.lock:23:  resolved "https://registry.yarnpkg.com/@babel/code-frame/-/code-frame-7.22.13.tgz#e3c1c099402598483b7a8c46a721d1038803755e"
examples/Web/web/yarn.lock:31:  resolved "https://registry.yarnpkg.com/@babel/code-frame/-/code-frame-7.26.2.tgz#4b5fab97d33338eff916235055f0ebc21e573a85"
examples/Web/web/yarn.lock:35:    js-tokens "^4.0.0"
examples/Web/web/yarn.lock:40:  resolved "https://registry.yarnpkg.com/@babel/compat-data/-/compat-data-7.16.8.tgz#31560f9f29fdf1868de8cb55049538a1b9732a60"
examples/Web/web/yarn.lock:45:  resolved "https://registry.yarnpkg.com/@babel/core/-/core-7.16.12.tgz#5edc53c1b71e54881315923ae2aedea2522bb784"
examples/Web/web/yarn.lock:66:  resolved "https://registry.yarnpkg.com/@babel/eslint-parser/-/eslint-parser-7.16.5.tgz#48d3485091d6e36915358e4c0d0b2ebe6da90462"
examples/Web/web/yarn.lock:75:  resolved "https://registry.yarnpkg.com/@babel/generator/-/generator-7.16.8.tgz#359d44d966b8cd059d543250ce79596f792f2ebe"
examples/Web/web/yarn.lock:84:  resolved "https://registry.yarnpkg.com/@babel/generator/-/generator-7.23.0.tgz#df5c386e2218be505b34837acbcb874d7a983420"
examples/Web/web/yarn.lock:94:  resolved "https://registry.yarnpkg.com/@babel/helper-annotate-as-pure/-/helper-annotate-as-pure-7.16.7.tgz#bb2339a7534a9c128e3102024c60760a3a7f3862"
examples/Web/web/yarn.lock:101:  resolved "https://registry.yarnpkg.com/@babel/helper-builder-binary-assignment-operator-visitor/-/helper-builder-binary-assignment-operator-visitor-7.16.7.tgz#38d138561ea207f0f69eb1626a418e4f7e6a580b"
examples/Web/web/yarn.lock:109:  resolved "https://registry.yarnpkg.com/@babel/helper-compilation-targets/-/helper-compilation-targets-7.16.7.tgz#06e66c5f299601e6c7da350049315e83209d551b"
examples/Web/web/yarn.lock:119:  resolved "https://registry.yarnpkg.com/@babel/helper-create-class-features-plugin/-/helper-create-class-features-plugin-7.16.10.tgz#8a6959b9cc818a88815ba3c5474619e9c0f2c21c"
examples/Web/web/yarn.lock:132:  resolved "https://registry.yarnpkg.com/@babel/helper-create-regexp-features-plugin/-/helper-create-regexp-features-plugin-7.16.7.tgz#0cb82b9bac358eb73bfbd73985a776bfa6b14d48"
examples/Web/web/yarn.lock:140:  resolved "https://registry.yarnpkg.com/@babel/helper-define-polyfill-provider/-/helper-define-polyfill-provider-0.3.1.tgz#52411b445bdb2e676869e5a74960d2d3826d2665"
examples/Web/web/yarn.lock:154:  resolved "https://registry.yarnpkg.com/@babel/helper-environment-visitor/-/helper-environment-visitor-7.16.7.tgz#ff484094a839bde9d89cd63cba017d7aae80ecd7"
examples/Web/web/yarn.lock:161:  resolved "https://registry.yarnpkg.com/@babel/helper-environment-visitor/-/helper-environment-visitor-7.22.20.tgz#96159db61d34a29dba454c959f5ae4a649ba9167"
examples/Web/web/yarn.lock:166:  resolved "https://registry.yarnpkg.com/@babel/helper-explode-assignable-expression/-/helper-explode-assignable-expression-7.16.7.tgz#12a6d8522fdd834f194e868af6354e8650242b7a"
examples/Web/web/yarn.lock:173:  resolved "https://registry.yarnpkg.com/@babel/helper-function-name/-/helper-function-name-7.16.7.tgz#f1ec51551fb1c8956bc8dd95f38523b6cf375f8f"
examples/Web/web/yarn.lock:182:  resolved "https://registry.yarnpkg.com/@babel/helper-function-name/-/helper-function-name-7.23.0.tgz#1f9a3cdbd5b2698a670c30d2735f9af95ed52759"
examples/Web/web/yarn.lock:190:  resolved "https://registry.yarnpkg.com/@babel/helper-get-function-arity/-/helper-get-function-arity-7.16.7.tgz#ea08ac753117a669f1508ba06ebcc49156387419"
examples/Web/web/yarn.lock:197:  resolved "https://registry.yarnpkg.com/@babel/helper-hoist-variables/-/helper-hoist-variables-7.16.7.tgz#86bcb19a77a509c7b77d0e22323ef588fa58c246"
examples/Web/web/yarn.lock:204:  resolved "https://registry.yarnpkg.com/@babel/helper-hoist-variables/-/helper-hoist-variables-7.22.5.tgz#c01a007dac05c085914e8fb652b339db50d823bb"
examples/Web/web/yarn.lock:211:  resolved "https://registry.yarnpkg.com/@babel/helper-member-expression-to-functions/-/helper-member-expression-to-functions-7.16.7.tgz#42b9ca4b2b200123c3b7e726b0ae5153924905b0"
examples/Web/web/yarn.lock:218:  resolved "https://registry.yarnpkg.com/@babel/helper-module-imports/-/helper-module-imports-7.16.7.tgz#25612a8091a999704461c8a222d0efec5d091437"
examples/Web/web/yarn.lock:225:  resolved "https://registry.yarnpkg.com/@babel/helper-module-transforms/-/helper-module-transforms-7.16.7.tgz#7665faeb721a01ca5327ddc6bba15a5cb34b6a41"
examples/Web/web/yarn.lock:239:  resolved "https://registry.yarnpkg.com/@babel/helper-optimise-call-expression/-/helper-optimise-call-expression-7.16.7.tgz#a34e3560605abbd31a18546bd2aad3e6d9a174f2"
examples/Web/web/yarn.lock:246:  resolved "https://registry.yarnpkg.com/@babel/helper-plugin-utils/-/helper-plugin-utils-7.16.7.tgz#aa3a8ab4c3cceff8e65eb9e73d87dc4ff320b2f5"
examples/Web/web/yarn.lock:251:  resolved "https://registry.yarnpkg.com/@babel/helper-remap-async-to-generator/-/helper-remap-async-to-generator-7.16.8.tgz#29ffaade68a367e2ed09c90901986918d25e57e3"
examples/Web/web/yarn.lock:260:  resolved "https://registry.yarnpkg.com/@babel/helper-replace-supers/-/helper-replace-supers-7.16.7.tgz#e9f5f5f32ac90429c1a4bdec0f231ef0c2838ab1"
examples/Web/web/yarn.lock:271:  resolved "https://registry.yarnpkg.com/@babel/helper-simple-access/-/helper-simple-access-7.16.7.tgz#d656654b9ea08dbb9659b69d61063ccd343ff0f7"
examples/Web/web/yarn.lock:278:  resolved "https://registry.yarnpkg.com/@babel/helper-skip-transparent-expression-wrappers/-/helper-skip-transparent-expression-wrappers-7.16.0.tgz#0ee3388070147c3ae051e487eca3ebb0e2e8bb09"
examples/Web/web/yarn.lock:285:  resolved "https://registry.yarnpkg.com/@babel/helper-split-export-declaration/-/helper-split-export-declaration-7.16.7.tgz#0b648c0c42da9d3920d85ad585f2778620b8726b"
examples/Web/web/yarn.lock:292:  resolved "https://registry.yarnpkg.com/@babel/helper-split-export-declaration/-/helper-split-export-declaration-7.22.6.tgz#322c61b7310c0997fe4c323955667f18fcefb91c"
examples/Web/web/yarn.lock:299:  resolved "https://registry.yarnpkg.com/@babel/helper-string-parser/-/helper-string-parser-7.22.5.tgz#533f36457a25814cf1df6488523ad547d784a99f"
examples/Web/web/yarn.lock:304:  resolved "https://registry.yarnpkg.com/@babel/helper-string-parser/-/helper-string-parser-7.25.9.tgz#1aabb72ee72ed35789b4bbcad3ca2862ce614e8c"
examples/Web/web/yarn.lock:309:  resolved "https://registry.yarnpkg.com/@babel/helper-validator-identifier/-/helper-validator-identifier-7.16.7.tgz#e8c602438c4a8195751243da9031d1607d247cad"
examples/Web/web/yarn.lock:314:  resolved "https://registry.yarnpkg.com/@babel/helper-validator-identifier/-/helper-validator-identifier-7.22.20.tgz#c4ae002c61d2879e724581d96665583dbc1dc0e0"
examples/Web/web/yarn.lock:319:  resolved "https://registry.yarnpkg.com/@babel/helper-validator-identifier/-/helper-validator-identifier-7.25.9.tgz#24b64e2c3ec7cd3b3c547729b8d16871f22cbdc7"
examples/Web/web/yarn.lock:324:  resolved "https://registry.yarnpkg.com/@babel/helper-validator-option/-/helper-validator-option-7.16.7.tgz#b203ce62ce5fe153899b617c08957de860de4d23"
examples/Web/web/yarn.lock:329:  resolved "https://registry.yarnpkg.com/@babel/helper-wrap-function/-/helper-wrap-function-7.16.8.tgz#58afda087c4cd235de92f7ceedebca2c41274200"
examples/Web/web/yarn.lock:339:  resolved "https://registry.yarnpkg.com/@babel/helpers/-/helpers-7.26.10.tgz#6baea3cd62ec2d0c1068778d63cb1314f6637384"
examples/Web/web/yarn.lock:347:  resolved "https://registry.yarnpkg.com/@babel/highlight/-/highlight-7.16.10.tgz#744f2eb81579d6eea753c227b0f570ad785aba88"
examples/Web/web/yarn.lock:352:    js-tokens "^4.0.0"
examples/Web/web/yarn.lock:356:  resolved "https://registry.yarnpkg.com/@babel/highlight/-/highlight-7.22.20.tgz#4ca92b71d80554b01427815e06f2df965b9c1f54"
examples/Web/web/yarn.lock:361:    js-tokens "^4.0.0"
examples/Web/web/yarn.lock:365:  resolved "https://registry.yarnpkg.com/@babel/parser/-/parser-7.16.12.tgz#9474794f9a650cf5e2f892444227f98e28cdf8b6"
examples/Web/web/yarn.lock:370:  resolved "https://registry.yarnpkg.com/@babel/parser/-/parser-7.23.0.tgz#da950e622420bf96ca0d0f2909cdddac3acd8719"
examples/Web/web/yarn.lock:375:  resolved "https://registry.yarnpkg.com/@babel/parser/-/parser-7.26.10.tgz#e9bdb82f14b97df6569b0b038edd436839c57749"
examples/Web/web/yarn.lock:382:  resolved "https://registry.yarnpkg.com/@babel/plugin-bugfix-safari-id-destructuring-collision-in-function-expression/-/plugin-bugfix-safari-id-destructuring-collision-in-function-expression-7.16.7.tgz#4eda6d6c2a0aa79c70fa7b6da67763dfe2141050"
examples/Web/web/yarn.lock:389:  resolved "https://registry.yarnpkg.com/@babel/plugin-bugfix-v8-spread-parameters-in-optional-chaining/-/plugin-bugfix-v8-spread-parameters-in-optional-chaining-7.16.7.tgz#cc001234dfc139ac45f6bcf801866198c8c72ff9"
examples/Web/web/yarn.lock:398:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-async-generator-functions/-/plugin-proposal-async-generator-functions-7.16.8.tgz#3bdd1ebbe620804ea9416706cd67d60787504bc8"
examples/Web/web/yarn.lock:407:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-class-properties/-/plugin-proposal-class-properties-7.16.7.tgz#925cad7b3b1a2fcea7e59ecc8eb5954f961f91b0"
examples/Web/web/yarn.lock:415:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-class-static-block/-/plugin-proposal-class-static-block-7.16.7.tgz#712357570b612106ef5426d13dc433ce0f200c2a"
examples/Web/web/yarn.lock:424:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-decorators/-/plugin-proposal-decorators-7.16.7.tgz#922907d2e3e327f5b07d2246bcfc0bd438f360d2"
examples/Web/web/yarn.lock:433:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-dynamic-import/-/plugin-proposal-dynamic-import-7.16.7.tgz#c19c897eaa46b27634a00fee9fb7d829158704b2"
examples/Web/web/yarn.lock:441:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-export-namespace-from/-/plugin-proposal-export-namespace-from-7.16.7.tgz#09de09df18445a5786a305681423ae63507a6163"
examples/Web/web/yarn.lock:449:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-json-strings/-/plugin-proposal-json-strings-7.16.7.tgz#9732cb1d17d9a2626a08c5be25186c195b6fa6e8"
examples/Web/web/yarn.lock:457:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-logical-assignment-operators/-/plugin-proposal-logical-assignment-operators-7.16.7.tgz#be23c0ba74deec1922e639832904be0bea73cdea"
examples/Web/web/yarn.lock:465:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-nullish-coalescing-operator/-/plugin-proposal-nullish-coalescing-operator-7.16.7.tgz#141fc20b6857e59459d430c850a0011e36561d99"
examples/Web/web/yarn.lock:473:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-numeric-separator/-/plugin-proposal-numeric-separator-7.16.7.tgz#d6b69f4af63fb38b6ca2558442a7fb191236eba9"
examples/Web/web/yarn.lock:481:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-object-rest-spread/-/plugin-proposal-object-rest-spread-7.16.7.tgz#94593ef1ddf37021a25bdcb5754c4a8d534b01d8"
examples/Web/web/yarn.lock:492:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-optional-catch-binding/-/plugin-proposal-optional-catch-binding-7.16.7.tgz#c623a430674ffc4ab732fd0a0ae7722b67cb74cf"
examples/Web/web/yarn.lock:500:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-optional-chaining/-/plugin-proposal-optional-chaining-7.16.7.tgz#7cd629564724816c0e8a969535551f943c64c39a"
examples/Web/web/yarn.lock:509:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-private-methods/-/plugin-proposal-private-methods-7.16.11.tgz#e8df108288555ff259f4527dbe84813aac3a1c50"
examples/Web/web/yarn.lock:517:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-private-property-in-object/-/plugin-proposal-private-property-in-object-7.16.7.tgz#b0b8cef543c2c3d57e59e2c611994861d46a3fce"
examples/Web/web/yarn.lock:527:  resolved "https://registry.yarnpkg.com/@babel/plugin-proposal-unicode-property-regex/-/plugin-proposal-unicode-property-regex-7.16.7.tgz#635d18eb10c6214210ffc5ff4932552de08188a2"
examples/Web/web/yarn.lock:535:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-async-generators/-/plugin-syntax-async-generators-7.8.4.tgz#a983fb1aeb2ec3f6ed042a210f640e90e786fe0d"
examples/Web/web/yarn.lock:542:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-bigint/-/plugin-syntax-bigint-7.8.3.tgz#4c9a6f669f5d0cdf1b90a1671e9a146be5300cea"
examples/Web/web/yarn.lock:549:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-class-properties/-/plugin-syntax-class-properties-7.12.13.tgz#b5c987274c4a3a82b89714796931a6b53544ae10"
examples/Web/web/yarn.lock:556:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-class-static-block/-/plugin-syntax-class-static-block-7.14.5.tgz#195df89b146b4b78b3bf897fd7a257c84659d406"
examples/Web/web/yarn.lock:563:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-decorators/-/plugin-syntax-decorators-7.16.7.tgz#f66a0199f16de7c1ef5192160ccf5d069739e3d3"
examples/Web/web/yarn.lock:570:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-dynamic-import/-/plugin-syntax-dynamic-import-7.8.3.tgz#62bf98b2da3cd21d626154fc96ee5b3cb68eacb3"
examples/Web/web/yarn.lock:577:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-export-namespace-from/-/plugin-syntax-export-namespace-from-7.8.3.tgz#028964a9ba80dbc094c915c487ad7c4e7a66465a"
examples/Web/web/yarn.lock:584:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-flow/-/plugin-syntax-flow-7.16.7.tgz#202b147e5892b8452bbb0bb269c7ed2539ab8832"
examples/Web/web/yarn.lock:591:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-import-meta/-/plugin-syntax-import-meta-7.10.4.tgz#ee601348c370fa334d2207be158777496521fd51"
examples/Web/web/yarn.lock:598:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-json-strings/-/plugin-syntax-json-strings-7.8.3.tgz#01ca21b668cd8218c9e640cb6dd88c5412b2c96a"
examples/Web/web/yarn.lock:605:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-jsx/-/plugin-syntax-jsx-7.16.7.tgz#50b6571d13f764266a113d77c82b4a6508bbe665"
examples/Web/web/yarn.lock:612:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-logical-assignment-operators/-/plugin-syntax-logical-assignment-operators-7.10.4.tgz#ca91ef46303530448b906652bac2e9fe9941f699"
examples/Web/web/yarn.lock:619:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-nullish-coalescing-operator/-/plugin-syntax-nullish-coalescing-operator-7.8.3.tgz#167ed70368886081f74b5c36c65a88c03b66d1a9"
examples/Web/web/yarn.lock:626:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-numeric-separator/-/plugin-syntax-numeric-separator-7.10.4.tgz#b9b070b3e33570cd9fd07ba7fa91c0dd37b9af97"
examples/Web/web/yarn.lock:633:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-object-rest-spread/-/plugin-syntax-object-rest-spread-7.8.3.tgz#60e225edcbd98a640332a2e72dd3e66f1af55871"
examples/Web/web/yarn.lock:640:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-optional-catch-binding/-/plugin-syntax-optional-catch-binding-7.8.3.tgz#6111a265bcfb020eb9efd0fdfd7d26402b9ed6c1"
examples/Web/web/yarn.lock:647:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-optional-chaining/-/plugin-syntax-optional-chaining-7.8.3.tgz#4f69c2ab95167e0180cd5336613f8c5788f7d48a"
examples/Web/web/yarn.lock:654:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-private-property-in-object/-/plugin-syntax-private-property-in-object-7.14.5.tgz#0dc6671ec0ea22b6e94a1114f857970cd39de1ad"
examples/Web/web/yarn.lock:661:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-top-level-await/-/plugin-syntax-top-level-await-7.14.5.tgz#c1cfdadc35a646240001f06138247b741c34d94c"
examples/Web/web/yarn.lock:668:  resolved "https://registry.yarnpkg.com/@babel/plugin-syntax-typescript/-/plugin-syntax-typescript-7.16.7.tgz#39c9b55ee153151990fb038651d58d3fd03f98f8"
examples/Web/web/yarn.lock:675:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-arrow-functions/-/plugin-transform-arrow-functions-7.16.7.tgz#44125e653d94b98db76369de9c396dc14bef4154"
examples/Web/web/yarn.lock:682:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-async-to-generator/-/plugin-transform-async-to-generator-7.16.8.tgz#b83dff4b970cf41f1b819f8b49cc0cfbaa53a808"
examples/Web/web/yarn.lock:691:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-block-scoped-functions/-/plugin-transform-block-scoped-functions-7.16.7.tgz#4d0d57d9632ef6062cdf354bb717102ee042a620"
examples/Web/web/yarn.lock:698:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-block-scoping/-/plugin-transform-block-scoping-7.16.7.tgz#f50664ab99ddeaee5bc681b8f3a6ea9d72ab4f87"
examples/Web/web/yarn.lock:705:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-classes/-/plugin-transform-classes-7.16.7.tgz#8f4b9562850cd973de3b498f1218796eb181ce00"
examples/Web/web/yarn.lock:719:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-computed-properties/-/plugin-transform-computed-properties-7.16.7.tgz#66dee12e46f61d2aae7a73710f591eb3df616470"
examples/Web/web/yarn.lock:726:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-destructuring/-/plugin-transform-destructuring-7.16.7.tgz#ca9588ae2d63978a4c29d3f33282d8603f618e23"
examples/Web/web/yarn.lock:733:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-dotall-regex/-/plugin-transform-dotall-regex-7.16.7.tgz#6b2d67686fab15fb6a7fd4bd895d5982cfc81241"
examples/Web/web/yarn.lock:741:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-duplicate-keys/-/plugin-transform-duplicate-keys-7.16.7.tgz#2207e9ca8f82a0d36a5a67b6536e7ef8b08823c9"
examples/Web/web/yarn.lock:748:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-exponentiation-operator/-/plugin-transform-exponentiation-operator-7.16.7.tgz#efa9862ef97e9e9e5f653f6ddc7b665e8536fe9b"
examples/Web/web/yarn.lock:756:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-flow-strip-types/-/plugin-transform-flow-strip-types-7.16.7.tgz#291fb140c78dabbf87f2427e7c7c332b126964b8"
examples/Web/web/yarn.lock:764:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-for-of/-/plugin-transform-for-of-7.16.7.tgz#649d639d4617dff502a9a158c479b3b556728d8c"
examples/Web/web/yarn.lock:771:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-function-name/-/plugin-transform-function-name-7.16.7.tgz#5ab34375c64d61d083d7d2f05c38d90b97ec65cf"
examples/Web/web/yarn.lock:780:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-literals/-/plugin-transform-literals-7.16.7.tgz#254c9618c5ff749e87cb0c0cef1a0a050c0bdab1"
examples/Web/web/yarn.lock:787:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-member-expression-literals/-/plugin-transform-member-expression-literals-7.16.7.tgz#6e5dcf906ef8a098e630149d14c867dd28f92384"
examples/Web/web/yarn.lock:794:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-modules-amd/-/plugin-transform-modules-amd-7.16.7.tgz#b28d323016a7daaae8609781d1f8c9da42b13186"
examples/Web/web/yarn.lock:803:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-modules-commonjs/-/plugin-transform-modules-commonjs-7.16.8.tgz#cdee19aae887b16b9d331009aa9a219af7c86afe"
examples/Web/web/yarn.lock:813:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-modules-systemjs/-/plugin-transform-modules-systemjs-7.16.7.tgz#887cefaef88e684d29558c2b13ee0563e287c2d7"
examples/Web/web/yarn.lock:824:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-modules-umd/-/plugin-transform-modules-umd-7.16.7.tgz#23dad479fa585283dbd22215bff12719171e7618"
examples/Web/web/yarn.lock:832:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-named-capturing-groups-regex/-/plugin-transform-named-capturing-groups-regex-7.16.8.tgz#7f860e0e40d844a02c9dcf9d84965e7dfd666252"
examples/Web/web/yarn.lock:839:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-new-target/-/plugin-transform-new-target-7.16.7.tgz#9967d89a5c243818e0800fdad89db22c5f514244"
examples/Web/web/yarn.lock:846:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-object-super/-/plugin-transform-object-super-7.16.7.tgz#ac359cf8d32cf4354d27a46867999490b6c32a94"
examples/Web/web/yarn.lock:854:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-parameters/-/plugin-transform-parameters-7.16.7.tgz#a1721f55b99b736511cb7e0152f61f17688f331f"
examples/Web/web/yarn.lock:861:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-property-literals/-/plugin-transform-property-literals-7.16.7.tgz#2dadac85155436f22c696c4827730e0fe1057a55"
examples/Web/web/yarn.lock:868:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-react-constant-elements/-/plugin-transform-react-constant-elements-7.16.7.tgz#19e9e4c2df2f6c3e6b3aea11778297d81db8df62"
examples/Web/web/yarn.lock:875:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-react-display-name/-/plugin-transform-react-display-name-7.16.7.tgz#7b6d40d232f4c0f550ea348593db3b21e2404340"
examples/Web/web/yarn.lock:882:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-react-jsx-development/-/plugin-transform-react-jsx-development-7.16.7.tgz#43a00724a3ed2557ed3f276a01a929e6686ac7b8"
examples/Web/web/yarn.lock:889:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-react-jsx/-/plugin-transform-react-jsx-7.16.7.tgz#86a6a220552afd0e4e1f0388a68a372be7add0d4"
examples/Web/web/yarn.lock:900:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-react-pure-annotations/-/plugin-transform-react-pure-annotations-7.16.7.tgz#232bfd2f12eb551d6d7d01d13fe3f86b45eb9c67"
examples/Web/web/yarn.lock:908:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-regenerator/-/plugin-transform-regenerator-7.16.7.tgz#9e7576dc476cb89ccc5096fff7af659243b4adeb"
examples/Web/web/yarn.lock:915:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-reserved-words/-/plugin-transform-reserved-words-7.16.7.tgz#1d798e078f7c5958eec952059c460b220a63f586"
examples/Web/web/yarn.lock:922:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-runtime/-/plugin-transform-runtime-7.16.10.tgz#53d9fd3496daedce1dd99639097fa5d14f4c7c2c"
examples/Web/web/yarn.lock:934:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-shorthand-properties/-/plugin-transform-shorthand-properties-7.16.7.tgz#e8549ae4afcf8382f711794c0c7b6b934c5fbd2a"
examples/Web/web/yarn.lock:941:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-spread/-/plugin-transform-spread-7.16.7.tgz#a303e2122f9f12e0105daeedd0f30fb197d8ff44"
examples/Web/web/yarn.lock:949:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-sticky-regex/-/plugin-transform-sticky-regex-7.16.7.tgz#c84741d4f4a38072b9a1e2e3fd56d359552e8660"
examples/Web/web/yarn.lock:956:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-template-literals/-/plugin-transform-template-literals-7.16.7.tgz#f3d1c45d28967c8e80f53666fc9c3e50618217ab"
examples/Web/web/yarn.lock:963:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-typeof-symbol/-/plugin-transform-typeof-symbol-7.16.7.tgz#9cdbe622582c21368bd482b660ba87d5545d4f7e"
examples/Web/web/yarn.lock:970:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-typescript/-/plugin-transform-typescript-7.16.8.tgz#591ce9b6b83504903fa9dd3652c357c2ba7a1ee0"
examples/Web/web/yarn.lock:979:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-unicode-escapes/-/plugin-transform-unicode-escapes-7.16.7.tgz#da8717de7b3287a2c6d659750c964f302b31ece3"
examples/Web/web/yarn.lock:986:  resolved "https://registry.yarnpkg.com/@babel/plugin-transform-unicode-regex/-/plugin-transform-unicode-regex-7.16.7.tgz#0f7aa4a501198976e25e82702574c34cfebe9ef2"
examples/Web/web/yarn.lock:994:  resolved "https://registry.yarnpkg.com/@babel/preset-env/-/preset-env-7.16.11.tgz#5dd88fd885fae36f88fd7c8342475c9f0abe2982"
examples/Web/web/yarn.lock:1074:  resolved "https://registry.yarnpkg.com/@babel/preset-modules/-/preset-modules-0.1.5.tgz#ef939d6e7f268827e1841638dc6ff95515e115d9"
examples/Web/web/yarn.lock:1085:  resolved "https://registry.yarnpkg.com/@babel/preset-react/-/preset-react-7.16.7.tgz#4c18150491edc69c183ff818f9f2aecbe5d93852"
examples/Web/web/yarn.lock:1097:  resolved "https://registry.yarnpkg.com/@babel/preset-typescript/-/preset-typescript-7.16.7.tgz#ab114d68bb2020afc069cd51b37ff98a046a70b9"
examples/Web/web/yarn.lock:1106:  resolved "https://registry.yarnpkg.com/@babel/runtime-corejs3/-/runtime-corejs3-7.26.10.tgz#5a3185ca2813f8de8ae68622572086edf5cf51f2"
examples/Web/web/yarn.lock:1114:  resolved "https://registry.yarnpkg.com/@babel/runtime/-/runtime-7.26.10.tgz#a07b4d8fa27af131a633d7b3524db803eb4764c2"
examples/Web/web/yarn.lock:1121:  resolved "https://registry.yarnpkg.com/@babel/template/-/template-7.16.7.tgz#8d126c8701fde4d66b264b3eba3d96f07666d155"
examples/Web/web/yarn.lock:1130:  resolved "https://registry.yarnpkg.com/@babel/template/-/template-7.22.15.tgz#09576efc3830f0430f4548ef971dde1350ef2f38"
examples/Web/web/yarn.lock:1139:  resolved "https://registry.yarnpkg.com/@babel/template/-/template-7.26.9.tgz#4577ad3ddf43d194528cff4e1fa6b232fa609bb2"
examples/Web/web/yarn.lock:1148:  resolved "https://registry.yarnpkg.com/@babel/traverse/-/traverse-7.23.2.tgz#329c7a06735e144a506bdb2cad0268b7f46f4ad8"
examples/Web/web/yarn.lock:1164:  resolved "https://registry.yarnpkg.com/@babel/types/-/types-7.16.8.tgz#0ba5da91dd71e0a4e7781a30f22770831062e3c1"
examples/Web/web/yarn.lock:1172:  resolved "https://registry.yarnpkg.com/@babel/types/-/types-7.23.0.tgz#8c1f020c9df0e737e4e247c0619f58c68458aaeb"
examples/Web/web/yarn.lock:1181:  resolved "https://registry.yarnpkg.com/@babel/types/-/types-7.26.10.tgz#396382f6335bd4feb65741eacfc808218f859259"
examples/Web/web/yarn.lock:1189:  resolved "https://registry.yarnpkg.com/@bcoe/v8-coverage/-/v8-coverage-0.2.3.tgz#75a2e8b51cb758a7553d6804a5932d7aace75c39"
examples/Web/web/yarn.lock:1194:  resolved "https://registry.yarnpkg.com/@csstools/normalize.css/-/normalize.css-12.0.0.tgz#a9583a75c3f150667771f30b60d9f059473e62c4"
examples/Web/web/yarn.lock:1199:  resolved "https://registry.yarnpkg.com/@csstools/postcss-font-format-keywords/-/postcss-font-format-keywords-1.0.0.tgz#7e7df948a83a0dfb7eb150a96e2390ac642356a1"
examples/Web/web/yarn.lock:1206:  resolved "https://registry.yarnpkg.com/@csstools/postcss-hwb-function/-/postcss-hwb-function-1.0.0.tgz#d6785c1c5ba8152d1d392c66f3a6a446c6034f6d"
examples/Web/web/yarn.lock:1213:  resolved "https://registry.yarnpkg.com/@csstools/postcss-is-pseudo-class/-/postcss-is-pseudo-class-2.0.0.tgz#219a1c1d84de7d9e9b7e662a57fdc194eac38ea7"
examples/Web/web/yarn.lock:1220:  resolved "https://registry.yarnpkg.com/@csstools/postcss-normalize-display-values/-/postcss-normalize-display-values-1.0.0.tgz#ce698f688c28517447aedf15a9037987e3d2dc97"
examples/Web/web/yarn.lock:1227:  resolved "https://registry.yarnpkg.com/@eslint/eslintrc/-/eslintrc-1.0.5.tgz#33f1b838dbf1f923bfa517e008362b78ddbbf318"
examples/Web/web/yarn.lock:1242:  resolved "https://registry.yarnpkg.com/@fluentui/react-component-event-listener/-/react-component-event-listener-0.51.7.tgz#158adb970d8bc982c91c57fd1322a0036042d86e"
examples/Web/web/yarn.lock:1249:  resolved "https://registry.yarnpkg.com/@fluentui/react-component-ref/-/react-component-ref-0.51.7.tgz#bfb0312e926c213bed35e53ee5105a68732eea99"
examples/Web/web/yarn.lock:1257:  resolved "https://registry.yarnpkg.com/@humanwhocodes/config-array/-/config-array-0.9.3.tgz#f2564c744b387775b436418491f15fce6601f63e"
examples/Web/web/yarn.lock:1266:  resolved "https://registry.yarnpkg.com/@humanwhocodes/object-schema/-/object-schema-1.2.1.tgz#b520529ec21d8e5945a1851dfd1c32e94e39ff45"
examples/Web/web/yarn.lock:1271:  resolved "https://registry.yarnpkg.com/@istanbuljs/load-nyc-config/-/load-nyc-config-1.1.0.tgz#fd3db1d59ecf7cf121e80650bb86712f9b55eced"
examples/Web/web/yarn.lock:1282:  resolved "https://registry.yarnpkg.com/@istanbuljs/schema/-/schema-0.1.3.tgz#e45e384e4b8ec16bce2fd903af78450f6bf7ec98"
examples/Web/web/yarn.lock:1287:  resolved "https://registry.yarnpkg.com/@jest/console/-/console-27.4.6.tgz#0742e6787f682b22bdad56f9db2a8a77f6a86107"
examples/Web/web/yarn.lock:1299:  resolved "https://registry.yarnpkg.com/@jest/core/-/core-27.4.7.tgz#84eabdf42a25f1fa138272ed229bcf0a1b5e6913"
examples/Web/web/yarn.lock:1333:  resolved "https://registry.yarnpkg.com/@jest/environment/-/environment-27.4.6.tgz#1e92885d64f48c8454df35ed9779fbcf31c56d8b"
examples/Web/web/yarn.lock:1343:  resolved "https://registry.yarnpkg.com/@jest/fake-timers/-/fake-timers-27.4.6.tgz#e026ae1671316dbd04a56945be2fa251204324e8"
examples/Web/web/yarn.lock:1355:  resolved "https://registry.yarnpkg.com/@jest/globals/-/globals-27.4.6.tgz#3f09bed64b0fd7f5f996920258bd4be8f52f060a"
examples/Web/web/yarn.lock:1364:  resolved "https://registry.yarnpkg.com/@jest/reporters/-/reporters-27.4.6.tgz#b53dec3a93baf9b00826abf95b932de919d6d8dd"
examples/Web/web/yarn.lock:1395:  resolved "https://registry.yarnpkg.com/@jest/source-map/-/source-map-27.4.0.tgz#2f0385d0d884fb3e2554e8f71f8fa957af9a74b6"
examples/Web/web/yarn.lock:1404:  resolved "https://registry.yarnpkg.com/@jest/test-result/-/test-result-27.4.6.tgz#b3df94c3d899c040f602cea296979844f61bdf69"
examples/Web/web/yarn.lock:1414:  resolved "https://registry.yarnpkg.com/@jest/test-sequencer/-/test-sequencer-27.4.6.tgz#447339b8a3d7b5436f50934df30854e442a9d904"
examples/Web/web/yarn.lock:1424:  resolved "https://registry.yarnpkg.com/@jest/transform/-/transform-27.4.6.tgz#153621940b1ed500305eacdb31105d415dc30231"
examples/Web/web/yarn.lock:1445:  resolved "https://registry.yarnpkg.com/@jest/types/-/types-27.4.2.tgz#96536ebd34da6392c2b7c7737d693885b5dd44a5"
examples/Web/web/yarn.lock:1456:  resolved "https://registry.yarnpkg.com/@jridgewell/gen-mapping/-/gen-mapping-0.3.2.tgz#c1aedc61e853f2bb9f5dfe6d4442d3b565b253b9"
examples/Web/web/yarn.lock:1465:  resolved "https://registry.yarnpkg.com/@jridgewell/gen-mapping/-/gen-mapping-0.3.3.tgz#7e02e6eb5df901aaedb08514203b096614024098"
examples/Web/web/yarn.lock:1474:  resolved "https://registry.yarnpkg.com/@jridgewell/gen-mapping/-/gen-mapping-0.3.5.tgz#dcce6aff74bdf6dad1a95802b69b04a2fcb1fb36"
examples/Web/web/yarn.lock:1483:  resolved "https://registry.yarnpkg.com/@jridgewell/resolve-uri/-/resolve-uri-3.1.0.tgz#2203b118c157721addfe69d47b70465463066d78"
examples/Web/web/yarn.lock:1488:  resolved "https://registry.yarnpkg.com/@jridgewell/resolve-uri/-/resolve-uri-3.1.1.tgz#c08679063f279615a3326583ba3a90d1d82cc721"
examples/Web/web/yarn.lock:1493:  resolved "https://registry.yarnpkg.com/@jridgewell/set-array/-/set-array-1.1.2.tgz#7c6cf998d6d20b914c0a55a91ae928ff25965e72"
examples/Web/web/yarn.lock:1498:  resolved "https://registry.yarnpkg.com/@jridgewell/set-array/-/set-array-1.2.1.tgz#558fb6472ed16a4c850b889530e6b36438c49280"
examples/Web/web/yarn.lock:1503:  resolved "https://registry.yarnpkg.com/@jridgewell/source-map/-/source-map-0.3.2.tgz#f45351aaed4527a298512ec72f81040c998580fb"
examples/Web/web/yarn.lock:1511:  resolved "https://registry.yarnpkg.com/@jridgewell/source-map/-/source-map-0.3.6.tgz#9d71ca886e32502eb9362c9a74a46787c36df81a"
examples/Web/web/yarn.lock:1519:  resolved "https://registry.yarnpkg.com/@jridgewell/sourcemap-codec/-/sourcemap-codec-1.4.14.tgz#add4c98d341472a289190b424efbdb096991bb24"
examples/Web/web/yarn.lock:1524:  resolved "https://registry.yarnpkg.com/@jridgewell/sourcemap-codec/-/sourcemap-codec-1.4.15.tgz#d7c6e6755c78567a951e04ab52ef0fd26de59f32"
examples/Web/web/yarn.lock:1529:  resolved "https://registry.yarnpkg.com/@jridgewell/trace-mapping/-/trace-mapping-0.3.19.tgz#f8a3249862f91be48d3127c3cfe992f79b4b8811"
examples/Web/web/yarn.lock:1537:  resolved "https://registry.yarnpkg.com/@jridgewell/trace-mapping/-/trace-mapping-0.3.25.tgz#15f190e98895f3fc23276ee14bc76b675c2e50f0"
examples/Web/web/yarn.lock:1545:  resolved "https://registry.yarnpkg.com/@jridgewell/trace-mapping/-/trace-mapping-0.3.14.tgz#b231a081d8f66796e475ad588a1ef473112701ed"
examples/Web/web/yarn.lock:1553:  resolved "https://registry.yarnpkg.com/@nodelib/fs.scandir/-/fs.scandir-2.1.5.tgz#7619c2eb21b25483f6d167548b4cfd5a7488c3d5"
examples/Web/web/yarn.lock:1561:  resolved "https://registry.yarnpkg.com/@nodelib/fs.stat/-/fs.stat-2.0.5.tgz#5bd262af94e9d25bd1e71b05deed44876a222e8b"
examples/Web/web/yarn.lock:1566:  resolved "https://registry.yarnpkg.com/@nodelib/fs.walk/-/fs.walk-1.2.8.tgz#e95737e8bb6746ddedf69c556953494f196fe69a"
examples/Web/web/yarn.lock:1574:  resolved "https://registry.yarnpkg.com/@pmmmwh/react-refresh-webpack-plugin/-/react-refresh-webpack-plugin-0.5.4.tgz#df0d0d855fc527db48aac93c218a0bf4ada41f99"
examples/Web/web/yarn.lock:1578:    common-path-prefix "^3.0.0"
examples/Web/web/yarn.lock:1589:  resolved "https://registry.yarnpkg.com/@popperjs/core/-/core-2.11.2.tgz#830beaec4b4091a9e9398ac50f865ddea52186b9"
examples/Web/web/yarn.lock:1594:  resolved "https://registry.yarnpkg.com/@rollup/plugin-babel/-/plugin-babel-5.3.0.tgz#9cb1c5146ddd6a4968ad96f209c50c62f92f9879"
examples/Web/web/yarn.lock:1602:  resolved "https://registry.yarnpkg.com/@rollup/plugin-node-resolve/-/plugin-node-resolve-11.2.1.tgz#82aa59397a29cd4e13248b106e6a4a1880362a60"
examples/Web/web/yarn.lock:1614:  resolved "https://registry.yarnpkg.com/@rollup/plugin-replace/-/plugin-replace-2.4.2.tgz#a2d539314fbc77c244858faa523012825068510a"
examples/Web/web/yarn.lock:1622:  resolved "https://registry.yarnpkg.com/@rollup/pluginutils/-/pluginutils-3.1.0.tgz#706b4524ee6dc8b103b3c995533e5ad680c02b9b"
examples/Web/web/yarn.lock:1631:  resolved "https://registry.yarnpkg.com/@rushstack/eslint-patch/-/eslint-patch-1.1.0.tgz#7f698254aadf921e48dda8c0a6b304026b8a9323"
examples/Web/web/yarn.lock:1636:  resolved "https://registry.yarnpkg.com/@semantic-ui-react/css-patch/-/css-patch-1.0.0.tgz#903637479eca4010749e81f15b91dfd5f019b08d"
examples/Web/web/yarn.lock:1644:  resolved "https://registry.yarnpkg.com/@semantic-ui-react/event-stack/-/event-stack-3.1.2.tgz#14fac9796695aa3967962d94ea9733a85325f9c4"
examples/Web/web/yarn.lock:1652:  resolved "https://registry.yarnpkg.com/@sinonjs/commons/-/commons-1.8.3.tgz#3802ddd21a50a949b6721ddd72da36e67e7f1b2d"
examples/Web/web/yarn.lock:1659:  resolved "https://registry.yarnpkg.com/@sinonjs/fake-timers/-/fake-timers-8.1.0.tgz#3fdc2b6cb58935b21bfb8d1625eb1300484316e7"
examples/Web/web/yarn.lock:1666:  resolved "https://registry.yarnpkg.com/@surma/rollup-plugin-off-main-thread/-/rollup-plugin-off-main-thread-2.2.3.tgz#ee34985952ca21558ab0d952f00298ad2190c053"
examples/Web/web/yarn.lock:1676:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-add-jsx-attribute/-/babel-plugin-add-jsx-attribute-5.4.0.tgz#81ef61947bb268eb9d50523446f9c638fb355906"
examples/Web/web/yarn.lock:1681:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-remove-jsx-attribute/-/babel-plugin-remove-jsx-attribute-5.4.0.tgz#6b2c770c95c874654fd5e1d5ef475b78a0a962ef"
examples/Web/web/yarn.lock:1686:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-remove-jsx-empty-expression/-/babel-plugin-remove-jsx-empty-expression-5.0.1.tgz#25621a8915ed7ad70da6cea3d0a6dbc2ea933efd"
examples/Web/web/yarn.lock:1691:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-replace-jsx-attribute-value/-/babel-plugin-replace-jsx-attribute-value-5.0.1.tgz#0b221fc57f9fcd10e91fe219e2cd0dd03145a897"
examples/Web/web/yarn.lock:1696:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-svg-dynamic-title/-/babel-plugin-svg-dynamic-title-5.4.0.tgz#139b546dd0c3186b6e5db4fefc26cb0baea729d7"
examples/Web/web/yarn.lock:1701:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-svg-em-dimensions/-/babel-plugin-svg-em-dimensions-5.4.0.tgz#6543f69526632a133ce5cabab965deeaea2234a0"
examples/Web/web/yarn.lock:1706:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-transform-react-native-svg/-/babel-plugin-transform-react-native-svg-5.4.0.tgz#00bf9a7a73f1cad3948cdab1f8dfb774750f8c80"
examples/Web/web/yarn.lock:1711:  resolved "https://registry.yarnpkg.com/@svgr/babel-plugin-transform-svg-component/-/babel-plugin-transform-svg-component-5.5.0.tgz#583a5e2a193e214da2f3afeb0b9e8d3250126b4a"
examples/Web/web/yarn.lock:1716:  resolved "https://registry.yarnpkg.com/@svgr/babel-preset/-/babel-preset-5.5.0.tgz#8af54f3e0a8add7b1e2b0fcd5a882c55393df327"
examples/Web/web/yarn.lock:1730:  resolved "https://registry.yarnpkg.com/@svgr/core/-/core-5.5.0.tgz#82e826b8715d71083120fe8f2492ec7d7874a579"
examples/Web/web/yarn.lock:1739:  resolved "https://registry.yarnpkg.com/@svgr/hast-util-to-babel-ast/-/hast-util-to-babel-ast-5.5.0.tgz#5ee52a9c2533f73e63f8f22b779f93cd432a5461"
examples/Web/web/yarn.lock:1746:  resolved "https://registry.yarnpkg.com/@svgr/plugin-jsx/-/plugin-jsx-5.5.0.tgz#1aa8cd798a1db7173ac043466d7b52236b369000"
examples/Web/web/yarn.lock:1756:  resolved "https://registry.yarnpkg.com/@svgr/plugin-svgo/-/plugin-svgo-5.5.0.tgz#02da55d85320549324e201c7b2e53bf431fcc246"
examples/Web/web/yarn.lock:1765:  resolved "https://registry.yarnpkg.com/@svgr/webpack/-/webpack-5.5.0.tgz#aae858ee579f5fa8ce6c3166ef56c6a1b381b640"
examples/Web/web/yarn.lock:1779:  resolved "https://registry.yarnpkg.com/@tootallnate/once/-/once-1.1.2.tgz#ccb91445360179a04e7fe6aff78c00ffc1eeaf82"
examples/Web/web/yarn.lock:1784:  resolved "https://registry.yarnpkg.com/@trysound/sax/-/sax-0.2.0.tgz#cccaab758af56761eb7bf37af6f03f326dd798ad"
examples/Web/web/yarn.lock:1789:  resolved "https://registry.yarnpkg.com/@types/babel__core/-/babel__core-7.1.18.tgz#1a29abcc411a9c05e2094c98f9a1b7da6cdf49f8"
examples/Web/web/yarn.lock:1800:  resolved "https://registry.yarnpkg.com/@types/babel__generator/-/babel__generator-7.6.4.tgz#1f20ce4c5b1990b37900b63f050182d28c2439b7"
examples/Web/web/yarn.lock:1807:  resolved "https://registry.yarnpkg.com/@types/babel__template/-/babel__template-7.4.1.tgz#3d1a48fd9d6c0edfd56f2ff578daed48f36c8969"
examples/Web/web/yarn.lock:1815:  resolved "https://registry.yarnpkg.com/@types/babel__traverse/-/babel__traverse-7.14.2.tgz#ffcd470bbb3f8bf30481678fb5502278ca833a43"
examples/Web/web/yarn.lock:1822:  resolved "https://registry.yarnpkg.com/@types/body-parser/-/body-parser-1.19.2.tgz#aea2059e28b7658639081347ac4fab3de166e6f0"
examples/Web/web/yarn.lock:1830:  resolved "https://registry.yarnpkg.com/@types/bonjour/-/bonjour-3.5.10.tgz#0f6aadfe00ea414edc86f5d106357cda9701e275"
examples/Web/web/yarn.lock:1837:  resolved "https://registry.yarnpkg.com/@types/connect-history-api-fallback/-/connect-history-api-fallback-1.3.5.tgz#d1f7a8a09d0ed5a57aee5ae9c18ab9b803205dae"
examples/Web/web/yarn.lock:1845:  resolved "https://registry.yarnpkg.com/@types/connect/-/connect-3.4.35.tgz#5fcf6ae445e4021d1fc2219a4873cc73a3bb2ad1"
examples/Web/web/yarn.lock:1852:  resolved "https://registry.yarnpkg.com/@types/eslint-scope/-/eslint-scope-3.7.7.tgz#3108bd5f18b0cdb277c867b3dd449c9ed7079ac5"
examples/Web/web/yarn.lock:1860:  resolved "https://registry.yarnpkg.com/@types/eslint/-/eslint-9.6.1.tgz#d5795ad732ce81715f27f75da913004a56751584"
examples/Web/web/yarn.lock:1868:  resolved "https://registry.yarnpkg.com/@types/eslint/-/eslint-7.29.0.tgz#e56ddc8e542815272720bb0b4ccc2aff9c3e1c78"
examples/Web/web/yarn.lock:1876:  resolved "https://registry.yarnpkg.com/@types/estree/-/estree-0.0.50.tgz#1e0caa9364d3fccd2931c3ed96fdbeaa5d4cca83"
examples/Web/web/yarn.lock:1881:  resolved "https://registry.yarnpkg.com/@types/estree/-/estree-0.0.39.tgz#e177e699ee1b8c22d23174caaa7422644389509f"
examples/Web/web/yarn.lock:1886:  resolved "https://registry.yarnpkg.com/@types/estree/-/estree-1.0.8.tgz#958b91c991b1867ced318bedea0e215ee050726e"
examples/Web/web/yarn.lock:1891:  resolved "https://registry.yarnpkg.com/@types/express-serve-static-core/-/express-serve-static-core-4.17.28.tgz#c47def9f34ec81dc6328d0b1b5303d1ec98d86b8"
examples/Web/web/yarn.lock:1900:  resolved "https://registry.yarnpkg.com/@types/express/-/express-4.17.13.tgz#a76e2995728999bab51a33fabce1d705a3709034"
examples/Web/web/yarn.lock:1910:  resolved "https://registry.yarnpkg.com/@types/graceful-fs/-/graceful-fs-4.1.5.tgz#21ffba0d98da4350db64891f92a9e5db3cdb4e15"
examples/Web/web/yarn.lock:1917:  resolved "https://registry.yarnpkg.com/@types/html-minifier-terser/-/html-minifier-terser-6.1.0.tgz#4fc33a00c1d0c16987b1a20cf92d20614c55ac35"
examples/Web/web/yarn.lock:1920:"@types/http-proxy@^1.17.8":
examples/Web/web/yarn.lock:1922:  resolved "https://registry.yarnpkg.com/@types/http-proxy/-/http-proxy-1.17.8.tgz#968c66903e7e42b483608030ee85800f22d03f55"
examples/Web/web/yarn.lock:1929:  resolved "https://registry.yarnpkg.com/@types/istanbul-lib-coverage/-/istanbul-lib-coverage-2.0.4.tgz#8467d4b3c087805d63580480890791277ce35c44"
examples/Web/web/yarn.lock:1934:  resolved "https://registry.yarnpkg.com/@types/istanbul-lib-report/-/istanbul-lib-report-3.0.0.tgz#c14c24f18ea8190c118ee7562b7ff99a36552686"
examples/Web/web/yarn.lock:1941:  resolved "https://registry.yarnpkg.com/@types/istanbul-reports/-/istanbul-reports-3.0.1.tgz#9153fe98bba2bd565a63add9436d6f0d7f8468ff"
examples/Web/web/yarn.lock:1948:  resolved "https://registry.yarnpkg.com/@types/json-schema/-/json-schema-7.0.9.tgz#97edc9037ea0c38585320b28964dde3b39e4660d"
examples/Web/web/yarn.lock:1953:  resolved "https://registry.yarnpkg.com/@types/json-schema/-/json-schema-7.0.15.tgz#596a1747233694d50f6ad8a7869fcb6f56cf5841"
examples/Web/web/yarn.lock:1958:  resolved "https://registry.yarnpkg.com/@types/json5/-/json5-0.0.29.tgz#ee28707ae94e11d2b827bcbe5270bcea7f3e71ee"
examples/Web/web/yarn.lock:1963:  resolved "https://registry.yarnpkg.com/@types/mime/-/mime-1.3.2.tgz#93e25bf9ee75fe0fd80b594bc4feb0e862111b5a"
examples/Web/web/yarn.lock:1968:  resolved "https://registry.yarnpkg.com/@types/node/-/node-17.0.14.tgz#33b9b94f789a8fedd30a68efdbca4dbb06b61f20"
examples/Web/web/yarn.lock:1973:  resolved "https://registry.yarnpkg.com/@types/parse-json/-/parse-json-4.0.0.tgz#2f8bb441434d163b35fb8ffdccd7138927ffb8c0"
examples/Web/web/yarn.lock:1978:  resolved "https://registry.yarnpkg.com/@types/prettier/-/prettier-2.4.3.tgz#a3c65525b91fca7da00ab1a3ac2b5a2a4afbffbf"
examples/Web/web/yarn.lock:1983:  resolved "https://registry.yarnpkg.com/@types/q/-/q-1.5.5.tgz#75a2a8e7d8ab4b230414505d92335d1dcb53a6df"
examples/Web/web/yarn.lock:1988:  resolved "https://registry.yarnpkg.com/@types/qs/-/qs-6.9.7.tgz#63bb7d067db107cc1e457c303bc25d511febf6cb"
examples/Web/web/yarn.lock:1993:  resolved "https://registry.yarnpkg.com/@types/range-parser/-/range-parser-1.2.4.tgz#cd667bcfdd025213aafb7ca5915a932590acdcdc"
examples/Web/web/yarn.lock:1998:  resolved "https://registry.yarnpkg.com/@types/resolve/-/resolve-1.17.1.tgz#3afd6ad8967c77e4376c598a82ddd58f46ec45d6"
examples/Web/web/yarn.lock:2005:  resolved "https://registry.yarnpkg.com/@types/retry/-/retry-0.12.1.tgz#d8f1c0d0dc23afad6dc16a9e993a0865774b4065"
examples/Web/web/yarn.lock:2010:  resolved "https://registry.yarnpkg.com/@types/serve-index/-/serve-index-1.9.1.tgz#1b5e85370a192c01ec6cec4735cf2917337a6278"
examples/Web/web/yarn.lock:2017:  resolved "https://registry.yarnpkg.com/@types/serve-static/-/serve-static-1.13.10.tgz#f5e0ce8797d2d7cc5ebeda48a52c96c4fa47a8d9"
examples/Web/web/yarn.lock:2025:  resolved "https://registry.yarnpkg.com/@types/sockjs/-/sockjs-0.3.33.tgz#570d3a0b99ac995360e3136fd6045113b1bd236f"
examples/Web/web/yarn.lock:2032:  resolved "https://registry.yarnpkg.com/@types/stack-utils/-/stack-utils-2.0.1.tgz#20f18294f797f2209b5f65c8e3b5c8e8261d127c"
examples/Web/web/yarn.lock:2037:  resolved "https://registry.yarnpkg.com/@types/trusted-types/-/trusted-types-2.0.2.tgz#fc25ad9943bcac11cceb8168db4f275e0e72e756"
examples/Web/web/yarn.lock:2042:  resolved "https://registry.yarnpkg.com/@types/ws/-/ws-8.2.2.tgz#7c5be4decb19500ae6b3d563043cd407bf366c21"
examples/Web/web/yarn.lock:2049:  resolved "https://registry.yarnpkg.com/@types/yargs-parser/-/yargs-parser-20.2.1.tgz#3b9ce2489919d9e4fea439b76916abc34b2df129"
examples/Web/web/yarn.lock:2054:  resolved "https://registry.yarnpkg.com/@types/yargs/-/yargs-16.0.4.tgz#26aad98dd2c2a38e421086ea9ad42b9e51642977"
examples/Web/web/yarn.lock:2061:  resolved "https://registry.yarnpkg.com/@typescript-eslint/eslint-plugin/-/eslint-plugin-5.10.2.tgz#f8c1d59fc37bd6d9d11c97267fdfe722c4777152"
examples/Web/web/yarn.lock:2076:  resolved "https://registry.yarnpkg.com/@typescript-eslint/experimental-utils/-/experimental-utils-5.10.2.tgz#dbb541e2070c7bd6e63d3e3a55b58be73a8fbb34"
examples/Web/web/yarn.lock:2083:  resolved "https://registry.yarnpkg.com/@typescript-eslint/parser/-/parser-5.10.2.tgz#b6076d27cc5499ce3f2c625f5ccde946ecb7db9a"
examples/Web/web/yarn.lock:2093:  resolved "https://registry.yarnpkg.com/@typescript-eslint/scope-manager/-/scope-manager-5.10.2.tgz#92c0bc935ec00f3d8638cdffb3d0e70c9b879639"
examples/Web/web/yarn.lock:2101:  resolved "https://registry.yarnpkg.com/@typescript-eslint/type-utils/-/type-utils-5.10.2.tgz#ad5acdf98a7d2ab030bea81f17da457519101ceb"
examples/Web/web/yarn.lock:2110:  resolved "https://registry.yarnpkg.com/@typescript-eslint/types/-/types-5.10.2.tgz#604d15d795c4601fffba6ecb4587ff9fdec68ce8"
examples/Web/web/yarn.lock:2115:  resolved "https://registry.yarnpkg.com/@typescript-eslint/typescript-estree/-/typescript-estree-5.10.2.tgz#810906056cd3ddcb35aa333fdbbef3713b0fe4a7"
examples/Web/web/yarn.lock:2128:  resolved "https://registry.yarnpkg.com/@typescript-eslint/utils/-/utils-5.10.2.tgz#1fcd37547c32c648ab11aea7173ec30060ee87a8"
examples/Web/web/yarn.lock:2140:  resolved "https://registry.yarnpkg.com/@typescript-eslint/visitor-keys/-/visitor-keys-5.10.2.tgz#fdbf272d8e61c045d865bd6c8b41bea73d222f3d"
examples/Web/web/yarn.lock:2148:  resolved "https://registry.yarnpkg.com/@webassemblyjs/ast/-/ast-1.14.1.tgz#a9f6a07f2b03c95c8d38c4536a1fdfb521ff55b6"
examples/Web/web/yarn.lock:2156:  resolved "https://registry.yarnpkg.com/@webassemblyjs/floating-point-hex-parser/-/floating-point-hex-parser-1.13.2.tgz#fcca1eeddb1cc4e7b6eed4fc7956d6813b21b9fb"
examples/Web/web/yarn.lock:2161:  resolved "https://registry.yarnpkg.com/@webassemblyjs/helper-api-error/-/helper-api-error-1.13.2.tgz#e0a16152248bc38daee76dd7e21f15c5ef3ab1e7"
examples/Web/web/yarn.lock:2166:  resolved "https://registry.yarnpkg.com/@webassemblyjs/helper-buffer/-/helper-buffer-1.14.1.tgz#822a9bc603166531f7d5df84e67b5bf99b72b96b"
examples/Web/web/yarn.lock:2171:  resolved "https://registry.yarnpkg.com/@webassemblyjs/helper-numbers/-/helper-numbers-1.13.2.tgz#dbd932548e7119f4b8a7877fd5a8d20e63490b2d"
examples/Web/web/yarn.lock:2180:  resolved "https://registry.yarnpkg.com/@webassemblyjs/helper-wasm-bytecode/-/helper-wasm-bytecode-1.13.2.tgz#e556108758f448aae84c850e593ce18a0eb31e0b"
examples/Web/web/yarn.lock:2185:  resolved "https://registry.yarnpkg.com/@webassemblyjs/helper-wasm-section/-/helper-wasm-section-1.14.1.tgz#9629dda9c4430eab54b591053d6dc6f3ba050348"
examples/Web/web/yarn.lock:2195:  resolved "https://registry.yarnpkg.com/@webassemblyjs/ieee754/-/ieee754-1.13.2.tgz#1c5eaace1d606ada2c7fd7045ea9356c59ee0dba"
examples/Web/web/yarn.lock:2202:  resolved "https://registry.yarnpkg.com/@webassemblyjs/leb128/-/leb128-1.13.2.tgz#57c5c3deb0105d02ce25fa3fd74f4ebc9fd0bbb0"
examples/Web/web/yarn.lock:2209:  resolved "https://registry.yarnpkg.com/@webassemblyjs/utf8/-/utf8-1.13.2.tgz#917a20e93f71ad5602966c2d685ae0c6c21f60f1"
examples/Web/web/yarn.lock:2214:  resolved "https://registry.yarnpkg.com/@webassemblyjs/wasm-edit/-/wasm-edit-1.14.1.tgz#ac6689f502219b59198ddec42dcd496b1004d597"
examples/Web/web/yarn.lock:2228:  resolved "https://registry.yarnpkg.com/@webassemblyjs/wasm-gen/-/wasm-gen-1.14.1.tgz#991e7f0c090cb0bb62bbac882076e3d219da9570"
examples/Web/web/yarn.lock:2239:  resolved "https://registry.yarnpkg.com/@webassemblyjs/wasm-opt/-/wasm-opt-1.14.1.tgz#e6f71ed7ccae46781c206017d3c14c50efa8106b"
examples/Web/web/yarn.lock:2249:  resolved "https://registry.yarnpkg.com/@webassemblyjs/wasm-parser/-/wasm-parser-1.14.1.tgz#b3e13f1893605ca78b52c68e54cf6a865f90b9fb"
examples/Web/web/yarn.lock:2261:  resolved "https://registry.yarnpkg.com/@webassemblyjs/wast-printer/-/wast-printer-1.14.1.tgz#3bb3e9638a8ae5fdaf9610e7a06b4d9f9aa6fe07"
examples/Web/web/yarn.lock:2269:  resolved "https://registry.yarnpkg.com/@xtuc/ieee754/-/ieee754-1.2.0.tgz#eef014a3145ae477a1cbc00cd1e552336dceb790"
examples/Web/web/yarn.lock:2274:  resolved "https://registry.yarnpkg.com/@xtuc/long/-/long-4.2.2.tgz#d291c6a4e97989b5c61d9acf396ae4fe133a718d"
examples/Web/web/yarn.lock:2279:  resolved "https://registry.yarnpkg.com/abab/-/abab-2.0.5.tgz#c0b678fb32d60fc1219c784d6a826fe385aeb79a"
examples/Web/web/yarn.lock:2284:  resolved "https://registry.yarnpkg.com/accepts/-/accepts-1.3.8.tgz#0bf0be125b67014adcb0b0921e62db7bffe16b2e"
examples/Web/web/yarn.lock:2292:  resolved "https://registry.yarnpkg.com/acorn-globals/-/acorn-globals-6.0.0.tgz#46cdd39f0f8ff08a876619b55f5ac8a6dc770b45"
examples/Web/web/yarn.lock:2300:  resolved "https://registry.yarnpkg.com/acorn-import-phases/-/acorn-import-phases-1.0.4.tgz#16eb850ba99a056cb7cbfe872ffb8972e18c8bd7"
examples/Web/web/yarn.lock:2305:  resolved "https://registry.yarnpkg.com/acorn-jsx/-/acorn-jsx-5.3.2.tgz#7ed5bb55908b3b2f1bc55c6af1653bada7f07937"
examples/Web/web/yarn.lock:2310:  resolved "https://registry.yarnpkg.com/acorn-node/-/acorn-node-1.8.2.tgz#114c95d64539e53dede23de8b9d96df7c7ae2af8"
examples/Web/web/yarn.lock:2319:  resolved "https://registry.yarnpkg.com/acorn-walk/-/acorn-walk-7.2.0.tgz#0de889a601203909b0fbe07b8938dc21d2e967bc"
examples/Web/web/yarn.lock:2324:  resolved "https://registry.yarnpkg.com/acorn/-/acorn-7.4.1.tgz#feaed255973d2e77555b83dbc08851a6c63520fa"
examples/Web/web/yarn.lock:2329:  resolved "https://registry.yarnpkg.com/acorn/-/acorn-8.15.0.tgz#a360898bc415edaac46c8241f6383975b930b816"
examples/Web/web/yarn.lock:2334:  resolved "https://registry.yarnpkg.com/acorn/-/acorn-8.7.1.tgz#0197122c843d1bf6d0a5e83220a788f278f63c30"
examples/Web/web/yarn.lock:2339:  resolved "https://registry.yarnpkg.com/address/-/address-1.1.2.tgz#bf1116c9c758c51b7a933d296b72c221ed9428b6"
examples/Web/web/yarn.lock:2344:  resolved "https://registry.yarnpkg.com/adjust-sourcemap-loader/-/adjust-sourcemap-loader-4.0.0.tgz#fc4a0fd080f7d10471f30a7320f25560ade28c99"
examples/Web/web/yarn.lock:2352:  resolved "https://registry.yarnpkg.com/agent-base/-/agent-base-6.0.2.tgz#49fff58577cfee3f37176feab4c22e00f86d7f77"
examples/Web/web/yarn.lock:2359:  resolved "https://registry.yarnpkg.com/aggregate-error/-/aggregate-error-3.1.0.tgz#92670ff50f5359bdb7a3e0d40d0ec30c5737687a"
examples/Web/web/yarn.lock:2367:  resolved "https://registry.yarnpkg.com/ajv-formats/-/ajv-formats-2.1.1.tgz#6e669400659eb74973bbf2e33327180a0996b520"
examples/Web/web/yarn.lock:2374:  resolved "https://registry.yarnpkg.com/ajv-keywords/-/ajv-keywords-3.5.2.tgz#31f29da5ab6e00d1c2d329acf7b5929614d5014d"
examples/Web/web/yarn.lock:2379:  resolved "https://registry.yarnpkg.com/ajv-keywords/-/ajv-keywords-5.1.0.tgz#69d4d385a4733cdbeab44964a1170a88f87f0e16"
examples/Web/web/yarn.lock:2386:  resolved "https://registry.yarnpkg.com/ajv/-/ajv-6.12.6.tgz#baf5a62e802b07d977034586f8c3baf5adf26df4"
examples/Web/web/yarn.lock:2396:  resolved "https://registry.yarnpkg.com/ajv/-/ajv-8.9.0.tgz#738019146638824dea25edcf299dcba1b0e7eb18"
examples/Web/web/yarn.lock:2406:  resolved "https://registry.yarnpkg.com/ajv/-/ajv-8.17.1.tgz#37d9a5c776af6bc92d7f4f9510eba4c0a60d11a6"
examples/Web/web/yarn.lock:2416:  resolved "https://registry.yarnpkg.com/ansi-escapes/-/ansi-escapes-4.3.2.tgz#6b2291d1db7d98b6521d5f1efa42d0f3a9feb65e"
examples/Web/web/yarn.lock:2423:  resolved "https://registry.yarnpkg.com/ansi-html-community/-/ansi-html-community-0.0.8.tgz#69fbc4d6ccbe383f9736934ae34c3f8290f1bf41"
examples/Web/web/yarn.lock:2428:  resolved "https://registry.yarnpkg.com/ansi-regex/-/ansi-regex-5.0.1.tgz#082cb2c89c9fe8659a311a53bd6a4dc5301db304"
examples/Web/web/yarn.lock:2433:  resolved "https://registry.yarnpkg.com/ansi-regex/-/ansi-regex-6.0.1.tgz#3183e38fae9a65d7cb5e53945cd5897d0260a06a"
examples/Web/web/yarn.lock:2438:  resolved "https://registry.yarnpkg.com/ansi-styles/-/ansi-styles-3.2.1.tgz#41fbb20243e50b12be0f04b8dedbf07520ce841d"
examples/Web/web/yarn.lock:2445:  resolved "https://registry.yarnpkg.com/ansi-styles/-/ansi-styles-4.3.0.tgz#edd803628ae71c04c85ae7a0906edad34b648937"
examples/Web/web/yarn.lock:2452:  resolved "https://registry.yarnpkg.com/ansi-styles/-/ansi-styles-5.2.0.tgz#07449690ad45777d1924ac2abb2fc8895dba836b"
examples/Web/web/yarn.lock:2457:  resolved "https://registry.yarnpkg.com/anymatch/-/anymatch-3.1.2.tgz#c0557c096af32f106198f4f4e2a383537e378716"
examples/Web/web/yarn.lock:2460:    normalize-path "^3.0.0"
examples/Web/web/yarn.lock:2465:  resolved "https://registry.yarnpkg.com/arg/-/arg-5.0.1.tgz#eb0c9a8f77786cad2af8ff2b862899842d7b6adb"
examples/Web/web/yarn.lock:2470:  resolved "https://registry.yarnpkg.com/argparse/-/argparse-1.0.10.tgz#bcd6791ea5ae09725e17e5ad988134cd40b3d911"
examples/Web/web/yarn.lock:2477:  resolved "https://registry.yarnpkg.com/argparse/-/argparse-2.0.1.tgz#246f50f3ca78a3240f6c997e8a9bd1eac49e4b38"
examples/Web/web/yarn.lock:2482:  resolved "https://registry.yarnpkg.com/aria-query/-/aria-query-4.2.2.tgz#0d2ca6c9aceb56b8977e9fed6aed7e15bbd2f83b"
examples/Web/web/yarn.lock:2490:  resolved "https://registry.yarnpkg.com/array-flatten/-/array-flatten-1.1.1.tgz#9a5f699051b1e7073328f2a008968b64ea2955d2"
examples/Web/web/yarn.lock:2495:  resolved "https://registry.yarnpkg.com/array-flatten/-/array-flatten-2.1.2.tgz#24ef80a28c1a893617e2149b0c6d0d788293b099"
examples/Web/web/yarn.lock:2500:  resolved "https://registry.yarnpkg.com/array-includes/-/array-includes-3.1.4.tgz#f5b493162c760f3539631f005ba2bb46acb45ba9"
examples/Web/web/yarn.lock:2511:  resolved "https://registry.yarnpkg.com/array-union/-/array-union-2.1.0.tgz#b798420adbeb1de828d84acd8a2e23d3efe85e8d"
examples/Web/web/yarn.lock:2516:  resolved "https://registry.yarnpkg.com/array.prototype.flat/-/array.prototype.flat-1.2.5.tgz#07e0975d84bbc7c48cd1879d609e682598d33e13"
examples/Web/web/yarn.lock:2525:  resolved "https://registry.yarnpkg.com/array.prototype.flatmap/-/array.prototype.flatmap-1.2.5.tgz#908dc82d8a406930fdf38598d51e7411d18d4446"
examples/Web/web/yarn.lock:2534:  resolved "https://registry.yarnpkg.com/asap/-/asap-2.0.6.tgz#e50347611d7e690943208bbdafebcbc2fb866d46"
examples/Web/web/yarn.lock:2539:  resolved "https://registry.yarnpkg.com/ast-types-flow/-/ast-types-flow-0.0.7.tgz#f70b735c6bca1a5c9c22d982c3e39e7feba3bdad"
examples/Web/web/yarn.lock:2544:  resolved "https://registry.yarnpkg.com/async/-/async-2.6.4.tgz#706b7ff6084664cd7eae713f6f965433b5504221"
examples/Web/web/yarn.lock:2551:  resolved "https://registry.yarnpkg.com/async/-/async-3.2.3.tgz#ac53dafd3f4720ee9e8a160628f18ea91df196c9"
examples/Web/web/yarn.lock:2556:  resolved "https://registry.yarnpkg.com/asynckit/-/asynckit-0.4.0.tgz#c79ed97f7f34cb8f2ba1bc9790bcc366474b4b79"
examples/Web/web/yarn.lock:2561:  resolved "https://registry.yarnpkg.com/at-least-node/-/at-least-node-1.0.0.tgz#602cd4b46e844ad4effc92a8011a3c46e0238dc2"
examples/Web/web/yarn.lock:2566:  resolved "https://registry.yarnpkg.com/autoprefixer/-/autoprefixer-10.4.2.tgz#25e1df09a31a9fba5c40b578936b90d35c9d4d3b"
examples/Web/web/yarn.lock:2578:  resolved "https://registry.yarnpkg.com/axe-core/-/axe-core-4.4.0.tgz#f93be7f81017eb8bedeb1859cc8092cc918d2dc8"
examples/Web/web/yarn.lock:2583:  resolved "https://registry.yarnpkg.com/axios/-/axios-1.15.0.tgz#0fcee91ef03d386514474904b27863b2c683bf4f"
examples/Web/web/yarn.lock:2586:    follow-redirects "^1.15.11"
examples/Web/web/yarn.lock:2588:    proxy-from-env "^2.1.0"
examples/Web/web/yarn.lock:2592:  resolved "https://registry.yarnpkg.com/axobject-query/-/axobject-query-2.2.0.tgz#943d47e10c0b704aa42275e20edf3722648989be"
examples/Web/web/yarn.lock:2597:  resolved "https://registry.yarnpkg.com/babel-jest/-/babel-jest-27.4.6.tgz#4d024e69e241cdf4f396e453a07100f44f7ce314"
examples/Web/web/yarn.lock:2611:  resolved "https://registry.yarnpkg.com/babel-loader/-/babel-loader-8.2.3.tgz#8986b40f1a64cacfcb4b8429320085ef68b1342d"
examples/Web/web/yarn.lock:2621:  resolved "https://registry.yarnpkg.com/babel-plugin-dynamic-import-node/-/babel-plugin-dynamic-import-node-2.3.3.tgz#84fda19c976ec5c6defef57f9427b3def66e17a3"
examples/Web/web/yarn.lock:2628:  resolved "https://registry.yarnpkg.com/babel-plugin-istanbul/-/babel-plugin-istanbul-6.1.1.tgz#fa88ec59232fd9b4e36dbbc540a8ec9a9b47da73"
examples/Web/web/yarn.lock:2639:  resolved "https://registry.yarnpkg.com/babel-plugin-jest-hoist/-/babel-plugin-jest-hoist-27.4.0.tgz#d7831fc0f93573788d80dee7e682482da4c730d6"
examples/Web/web/yarn.lock:2649:  resolved "https://registry.yarnpkg.com/babel-plugin-macros/-/babel-plugin-macros-3.1.0.tgz#9ef6dc74deb934b4db344dc973ee851d148c50c1"
examples/Web/web/yarn.lock:2658:  resolved "https://registry.yarnpkg.com/babel-plugin-named-asset-import/-/babel-plugin-named-asset-import-0.3.8.tgz#6b7fa43c59229685368683c28bc9734f24524cc2"
examples/Web/web/yarn.lock:2663:  resolved "https://registry.yarnpkg.com/babel-plugin-polyfill-corejs2/-/babel-plugin-polyfill-corejs2-0.3.1.tgz#440f1b70ccfaabc6b676d196239b138f8a2cfba5"
examples/Web/web/yarn.lock:2672:  resolved "https://registry.yarnpkg.com/babel-plugin-polyfill-corejs3/-/babel-plugin-polyfill-corejs3-0.5.1.tgz#d66183bf10976ea677f4149a7fcc4d8df43d4060"
examples/Web/web/yarn.lock:2680:  resolved "https://registry.yarnpkg.com/babel-plugin-polyfill-regenerator/-/babel-plugin-polyfill-regenerator-0.3.1.tgz#2c0678ea47c75c8cc2fbb1852278d8fb68233990"
examples/Web/web/yarn.lock:2687:  resolved "https://registry.yarnpkg.com/babel-plugin-transform-react-remove-prop-types/-/babel-plugin-transform-react-remove-prop-types-0.4.24.tgz#f2edaf9b4c6a5fbe5c1d678bfb531078c1555f3a"
examples/Web/web/yarn.lock:2692:  resolved "https://registry.yarnpkg.com/babel-preset-current-node-syntax/-/babel-preset-current-node-syntax-1.0.1.tgz#b4399239b89b2a011f9ddbe3e4f401fc40cff73b"
examples/Web/web/yarn.lock:2710:  resolved "https://registry.yarnpkg.com/babel-preset-jest/-/babel-preset-jest-27.4.0.tgz#70d0e676a282ccb200fbabd7f415db5fdf393bca"
examples/Web/web/yarn.lock:2718:  resolved "https://registry.yarnpkg.com/babel-preset-react-app/-/babel-preset-react-app-10.0.1.tgz#ed6005a20a24f2c88521809fa9aea99903751584"
examples/Web/web/yarn.lock:2740:  resolved "https://registry.yarnpkg.com/balanced-match/-/balanced-match-1.0.2.tgz#e83e3a7e3f300b34cb9d87f615fa0cbf357690ee"
examples/Web/web/yarn.lock:2745:  resolved "https://registry.yarnpkg.com/baseline-browser-mapping/-/baseline-browser-mapping-2.9.19.tgz#3e508c43c46d961eb4d7d2e5b8d1dd0f9ee4f488"
examples/Web/web/yarn.lock:2750:  resolved "https://registry.yarnpkg.com/batch/-/batch-0.6.1.tgz#dc34314f4e679318093fc760272525f94bf25c16"
examples/Web/web/yarn.lock:2755:  resolved "https://registry.yarnpkg.com/bfj/-/bfj-7.0.2.tgz#1988ce76f3add9ac2913fd8ba47aad9e651bfbb2"
examples/Web/web/yarn.lock:2765:  resolved "https://registry.yarnpkg.com/big.js/-/big.js-5.2.2.tgz#65f0af382f578bcdc742bd9c281e9cb2d7768328"
examples/Web/web/yarn.lock:2770:  resolved "https://registry.yarnpkg.com/binary-extensions/-/binary-extensions-2.2.0.tgz#75f502eeaf9ffde42fc98829645be4ea76bd9e2d"
examples/Web/web/yarn.lock:2775:  resolved "https://registry.yarnpkg.com/bluebird/-/bluebird-3.7.2.tgz#9f229c15be272454ffa973ace0dbee79a1b0c36f"
examples/Web/web/yarn.lock:2780:  resolved "https://registry.yarnpkg.com/body-parser/-/body-parser-1.20.3.tgz#1953431221c6fb5cd63c4b36d53fab0928e548c6"
examples/Web/web/yarn.lock:2798:  resolved "https://registry.yarnpkg.com/bonjour/-/bonjour-3.5.0.tgz#8e890a183d8ee9a2393b3844c691a42bcf7bc9f5"
examples/Web/web/yarn.lock:2810:  resolved "https://registry.yarnpkg.com/boolbase/-/boolbase-1.0.0.tgz#68dff5fbe60c51eb37725ea9e3ed310dcc1e776e"
examples/Web/web/yarn.lock:2815:  resolved "https://registry.yarnpkg.com/brace-expansion/-/brace-expansion-1.1.11.tgz#3c7fcbf529d87226f3d2f52b966ff5271eb441dd"
examples/Web/web/yarn.lock:2823:  resolved "https://registry.yarnpkg.com/brace-expansion/-/brace-expansion-2.0.1.tgz#1edc459e0f0c548486ecf9fc99f2221364b9a0ae"
examples/Web/web/yarn.lock:2830:  resolved "https://registry.yarnpkg.com/braces/-/braces-3.0.3.tgz#490332f40919452272d55a8480adc0c441358789"
examples/Web/web/yarn.lock:2837:  resolved "https://registry.yarnpkg.com/browser-process-hrtime/-/browser-process-hrtime-1.0.0.tgz#3c9b4b7d782c8121e56f10106d84c0d0ffc94626"
examples/Web/web/yarn.lock:2842:  resolved "https://registry.yarnpkg.com/browserslist/-/browserslist-4.19.1.tgz#4ac0435b35ab655896c31d53018b6dd5e9e4c9a3"
examples/Web/web/yarn.lock:2853:  resolved "https://registry.yarnpkg.com/browserslist/-/browserslist-4.28.1.tgz#7f534594628c53c63101079e27e40de490456a95"
examples/Web/web/yarn.lock:2864:  resolved "https://registry.yarnpkg.com/bser/-/bser-2.1.1.tgz#e6787da20ece9d07998533cfd9de6f5c38f4bc05"
examples/Web/web/yarn.lock:2871:  resolved "https://registry.yarnpkg.com/buffer-from/-/buffer-from-1.1.2.tgz#2b146a6fd72e80b4f55d255f35ed59a3a9a41bd5"
examples/Web/web/yarn.lock:2876:  resolved "https://registry.yarnpkg.com/buffer-indexof/-/buffer-indexof-1.1.1.tgz#52fabcc6a606d1a00302802648ef68f639da268c"
examples/Web/web/yarn.lock:2881:  resolved "https://registry.yarnpkg.com/builtin-modules/-/builtin-modules-3.2.0.tgz#45d5db99e7ee5e6bc4f362e008bf917ab5049887"
examples/Web/web/yarn.lock:2886:  resolved "https://registry.yarnpkg.com/bytes/-/bytes-3.0.0.tgz#d32815404d689699f85a4ea4fa8755dd13a96048"
examples/Web/web/yarn.lock:2891:  resolved "https://registry.yarnpkg.com/bytes/-/bytes-3.1.2.tgz#8b0beeb98605adf1b128fa4386403c009e0221a5"
examples/Web/web/yarn.lock:2896:  resolved "https://registry.yarnpkg.com/call-bind-apply-helpers/-/call-bind-apply-helpers-1.0.2.tgz#4b5428c222be985d79c3d82657479dbe0b59b2d6"
examples/Web/web/yarn.lock:2904:  resolved "https://registry.yarnpkg.com/call-bind/-/call-bind-1.0.2.tgz#b1d4e89e688119c3c9a903ad30abb2f6a919be3c"
examples/Web/web/yarn.lock:2912:  resolved "https://registry.yarnpkg.com/call-bind/-/call-bind-1.0.7.tgz#06016599c40c56498c18769d2730be242b6fa3b9"
examples/Web/web/yarn.lock:2923:  resolved "https://registry.yarnpkg.com/callsites/-/callsites-3.1.0.tgz#b3630abd8943432f54b3f0519238e33cd7df2f73"
examples/Web/web/yarn.lock:2928:  resolved "https://registry.yarnpkg.com/camel-case/-/camel-case-4.1.2.tgz#9728072a954f805228225a6deea6b38461e1bd5a"
examples/Web/web/yarn.lock:2936:  resolved "https://registry.yarnpkg.com/camelcase-css/-/camelcase-css-2.0.1.tgz#ee978f6947914cc30c6b44741b6ed1df7f043fd5"
examples/Web/web/yarn.lock:2941:  resolved "https://registry.yarnpkg.com/camelcase/-/camelcase-5.3.1.tgz#e3c9b31569e106811df242f715725a1f4c494320"
examples/Web/web/yarn.lock:2946:  resolved "https://registry.yarnpkg.com/camelcase/-/camelcase-6.3.0.tgz#5685b95eb209ac9c0c177467778c9c84df58ba9a"
examples/Web/web/yarn.lock:2951:  resolved "https://registry.yarnpkg.com/caniuse-api/-/caniuse-api-3.0.0.tgz#5e4d90e2274961d46291997df599e3ed008ee4c0"
examples/Web/web/yarn.lock:2961:  resolved "https://registry.yarnpkg.com/caniuse-lite/-/caniuse-lite-1.0.30001304.tgz#38af55ed3fc8220cb13e35e6e7309c8c65a05559"
examples/Web/web/yarn.lock:2966:  resolved "https://registry.yarnpkg.com/caniuse-lite/-/caniuse-lite-1.0.30001769.tgz#1ad91594fad7dc233777c2781879ab5409f7d9c2"
examples/Web/web/yarn.lock:2969:case-sensitive-paths-webpack-plugin@^2.4.0:
examples/Web/web/yarn.lock:2971:  resolved "https://registry.yarnpkg.com/case-sensitive-paths-webpack-plugin/-/case-sensitive-paths-webpack-plugin-2.4.0.tgz#db64066c6422eed2e08cc14b986ca43796dbc6d4"
examples/Web/web/yarn.lock:2976:  resolved "https://registry.yarnpkg.com/chalk/-/chalk-2.4.2.tgz#cd42541677a54333cf541a49108c1432b44c9424"
examples/Web/web/yarn.lock:2985:  resolved "https://registry.yarnpkg.com/chalk/-/chalk-3.0.0.tgz#3f73c2bf526591f574cc492c51e2456349f844e4"
examples/Web/web/yarn.lock:2993:  resolved "https://registry.yarnpkg.com/chalk/-/chalk-4.1.2.tgz#aac4e2b7734a740867aeb16bf02aad556a1e7a01"
examples/Web/web/yarn.lock:3001:  resolved "https://registry.yarnpkg.com/char-regex/-/char-regex-1.0.2.tgz#d744358226217f981ed58f479b1d6bcc29545dcf"
examples/Web/web/yarn.lock:3006:  resolved "https://registry.yarnpkg.com/char-regex/-/char-regex-2.0.0.tgz#16f98f3f874edceddd300fda5d58df380a7641a6"
examples/Web/web/yarn.lock:3011:  resolved "https://registry.yarnpkg.com/check-types/-/check-types-11.1.2.tgz#86a7c12bf5539f6324eb0e70ca8896c0e38f3e2f"
examples/Web/web/yarn.lock:3016:  resolved "https://registry.yarnpkg.com/chokidar/-/chokidar-3.5.3.tgz#1cf37c8707b932bd1af1ae22c0432e2acd1903bd"
examples/Web/web/yarn.lock:3022:    is-binary-path "~2.1.0"
examples/Web/web/yarn.lock:3024:    normalize-path "~3.0.0"
examples/Web/web/yarn.lock:3031:  resolved "https://registry.yarnpkg.com/chrome-trace-event/-/chrome-trace-event-1.0.3.tgz#1015eced4741e15d06664a957dbbf50d041e26ac"
examples/Web/web/yarn.lock:3036:  resolved "https://registry.yarnpkg.com/ci-info/-/ci-info-3.3.0.tgz#b4ed1fb6818dea4803a55c623041f9165d2066b2"
examples/Web/web/yarn.lock:3041:  resolved "https://registry.yarnpkg.com/cjs-module-lexer/-/cjs-module-lexer-1.2.2.tgz#9f84ba3244a512f3a54e5277e8eef4c489864e40"
examples/Web/web/yarn.lock:3046:  resolved "https://registry.yarnpkg.com/clean-css/-/clean-css-5.2.4.tgz#982b058f8581adb2ae062520808fb2429bd487a4"
examples/Web/web/yarn.lock:3053:  resolved "https://registry.yarnpkg.com/clean-stack/-/clean-stack-2.2.0.tgz#ee8472dbb129e727b31e8a10a427dee9dfe4008b"
examples/Web/web/yarn.lock:3058:  resolved "https://registry.yarnpkg.com/cliui/-/cliui-7.0.4.tgz#a0265ee655476fc807aea9df3df8df7783808b4f"
examples/Web/web/yarn.lock:3067:  resolved "https://registry.yarnpkg.com/clsx/-/clsx-1.1.1.tgz#98b3134f9abbdf23b2663491ace13c5c03a73188"
examples/Web/web/yarn.lock:3072:  resolved "https://registry.yarnpkg.com/co/-/co-4.6.0.tgz#6ea6bdf3d853ae54ccb8e47bfa0bf3f9031fb184"
examples/Web/web/yarn.lock:3077:  resolved "https://registry.yarnpkg.com/coa/-/coa-2.0.2.tgz#43f6c21151b4ef2bf57187db0d73de229e3e7ec3"
examples/Web/web/yarn.lock:3086:  resolved "https://registry.yarnpkg.com/collect-v8-coverage/-/collect-v8-coverage-1.0.1.tgz#cc2c8e94fc18bbdffe64d6534570c8a673b27f59"
examples/Web/web/yarn.lock:3091:  resolved "https://registry.yarnpkg.com/color-convert/-/color-convert-1.9.3.tgz#bb71850690e1f136567de629d2d5471deda4c1e8"
examples/Web/web/yarn.lock:3098:  resolved "https://registry.yarnpkg.com/color-convert/-/color-convert-2.0.1.tgz#72d3a68d598c9bdb3af2ad1e84f21d896abd4de3"
examples/Web/web/yarn.lock:3105:  resolved "https://registry.yarnpkg.com/color-name/-/color-name-1.1.3.tgz#a7d0558bd89c42f795dd42328f740831ca53bc25"
examples/Web/web/yarn.lock:3110:  resolved "https://registry.yarnpkg.com/color-name/-/color-name-1.1.4.tgz#c2a09a87acbde69543de6f63fa3995c826c536a2"
examples/Web/web/yarn.lock:3115:  resolved "https://registry.yarnpkg.com/colord/-/colord-2.9.2.tgz#25e2bacbbaa65991422c07ea209e2089428effb1"
examples/Web/web/yarn.lock:3120:  resolved "https://registry.yarnpkg.com/colorette/-/colorette-2.0.16.tgz#713b9af84fdb000139f04546bd4a93f62a5085da"
examples/Web/web/yarn.lock:3125:  resolved "https://registry.yarnpkg.com/combined-stream/-/combined-stream-1.0.8.tgz#c3d45a8b34fd730631a110a8a2520682b31d5a7f"
examples/Web/web/yarn.lock:3132:  resolved "https://registry.yarnpkg.com/commander/-/commander-2.20.3.tgz#fd485e84c03eb4881c20722ba48035e8531aeb33"
examples/Web/web/yarn.lock:3137:  resolved "https://registry.yarnpkg.com/commander/-/commander-7.2.0.tgz#a36cb57d0b501ce108e4d20559a150a391d97ab7"
examples/Web/web/yarn.lock:3142:  resolved "https://registry.yarnpkg.com/commander/-/commander-8.3.0.tgz#4837ea1b2da67b9c616a67afbb0fafee567bca66"
examples/Web/web/yarn.lock:3145:common-path-prefix@^3.0.0:
examples/Web/web/yarn.lock:3147:  resolved "https://registry.yarnpkg.com/common-path-prefix/-/common-path-prefix-3.0.0.tgz#7d007a7e07c58c4b4d5f433131a19141b29f11e0"
examples/Web/web/yarn.lock:3152:  resolved "https://registry.yarnpkg.com/common-tags/-/common-tags-1.8.2.tgz#94ebb3c076d26032745fd54face7f688ef5ac9c6"
examples/Web/web/yarn.lock:3157:  resolved "https://registry.yarnpkg.com/commondir/-/commondir-1.0.1.tgz#ddd800da0c66127393cca5950ea968a3aaf1253b"
examples/Web/web/yarn.lock:3162:  resolved "https://registry.yarnpkg.com/compressible/-/compressible-2.0.18.tgz#af53cca6b070d4c3c0750fbd77286a6d7cc46fba"
examples/Web/web/yarn.lock:3169:  resolved "https://registry.yarnpkg.com/compression/-/compression-1.7.4.tgz#95523eff170ca57c29a0ca41e6fe131f41e5bb8f"
examples/Web/web/yarn.lock:3182:  resolved "https://registry.yarnpkg.com/concat-map/-/concat-map-0.0.1.tgz#d8a96bd77fd68df7793a73036a3ba0d5405d477b"
examples/Web/web/yarn.lock:3187:  resolved "https://registry.yarnpkg.com/confusing-browser-globals/-/confusing-browser-globals-1.0.11.tgz#ae40e9b57cdd3915408a2805ebd3a5585608dc81"
examples/Web/web/yarn.lock:3192:  resolved "https://registry.yarnpkg.com/connect-history-api-fallback/-/connect-history-api-fallback-1.6.0.tgz#8b32089359308d111115d81cad3fceab888f97bc"
examples/Web/web/yarn.lock:3197:  resolved "https://registry.yarnpkg.com/content-disposition/-/content-disposition-0.5.4.tgz#8b82b4efac82512a02bb0b1dcec9d2c5e8eb5bfe"
examples/Web/web/yarn.lock:3204:  resolved "https://registry.yarnpkg.com/content-type/-/content-type-1.0.4.tgz#e138cc75e040c727b1966fe5e5f8c9aee256fe3b"
examples/Web/web/yarn.lock:3209:  resolved "https://registry.yarnpkg.com/content-type/-/content-type-1.0.5.tgz#8b773162656d1d1086784c8f23a54ce6d73d7918"
examples/Web/web/yarn.lock:3214:  resolved "https://registry.yarnpkg.com/convert-source-map/-/convert-source-map-1.8.0.tgz#f3373c32d21b4d780dd8004514684fb791ca4369"
examples/Web/web/yarn.lock:3219:cookie-signature@1.0.6:
examples/Web/web/yarn.lock:3221:  resolved "https://registry.yarnpkg.com/cookie-signature/-/cookie-signature-1.0.6.tgz#e303a882b342cc3ee8ca513a79999734dab3ae2c"
examples/Web/web/yarn.lock:3224:cookie@0.6.0:
examples/Web/web/yarn.lock:3226:  resolved "https://registry.yarnpkg.com/cookie/-/cookie-0.6.0.tgz#2798b04b071b0ecbff0dbb62a505a8efa4e19051"
examples/Web/web/yarn.lock:3231:  resolved "https://registry.yarnpkg.com/core-js-compat/-/core-js-compat-3.20.3.tgz#d71f85f94eb5e4bea3407412e549daa083d23bd6"
examples/Web/web/yarn.lock:3239:  resolved "https://registry.yarnpkg.com/core-js-pure/-/core-js-pure-3.41.0.tgz#349fecad168d60807a31e83c99d73d786fe80811"
examples/Web/web/yarn.lock:3244:  resolved "https://registry.yarnpkg.com/core-js-pure/-/core-js-pure-3.20.3.tgz#6cc4f36da06c61d95254efc54024fe4797fd5d02"
examples/Web/web/yarn.lock:3249:  resolved "https://registry.yarnpkg.com/core-js/-/core-js-3.20.3.tgz#c710d0a676e684522f3db4ee84e5e18a9d11d69a"
examples/Web/web/yarn.lock:3254:  resolved "https://registry.yarnpkg.com/core-util-is/-/core-util-is-1.0.3.tgz#a6042d3634c2b27e9328f837b965fac83808db85"
examples/Web/web/yarn.lock:3259:  resolved "https://registry.yarnpkg.com/cosmiconfig/-/cosmiconfig-6.0.0.tgz#da4fee853c52f6b1e6935f41c1a2fc50bd4a9982"
examples/Web/web/yarn.lock:3265:    path-type "^4.0.0"
examples/Web/web/yarn.lock:3270:  resolved "https://registry.yarnpkg.com/cosmiconfig/-/cosmiconfig-7.0.1.tgz#714d756522cace867867ccb4474c5d01bbae5d6d"
examples/Web/web/yarn.lock:3276:    path-type "^4.0.0"
examples/Web/web/yarn.lock:3279:cross-spawn@^7.0.2, cross-spawn@^7.0.3:
examples/Web/web/yarn.lock:3281:  resolved "https://registry.yarnpkg.com/cross-spawn/-/cross-spawn-7.0.6.tgz#8a58fe78f00dcd70c370451759dfbfaf03e8ee9f"
examples/Web/web/yarn.lock:3284:    path-key "^3.1.0"
examples/Web/web/yarn.lock:3290:  resolved "https://registry.yarnpkg.com/crypto-random-string/-/crypto-random-string-2.0.0.tgz#ef2a7a966ec11083388369baa02ebead229b30d5"
examples/Web/web/yarn.lock:3295:  resolved "https://registry.yarnpkg.com/css-blank-pseudo/-/css-blank-pseudo-3.0.2.tgz#f8660f6a48b17888a9277e53f25cc5abec1f0169"
examples/Web/web/yarn.lock:3302:  resolved "https://registry.yarnpkg.com/css-declaration-sorter/-/css-declaration-sorter-6.1.4.tgz#b9bfb4ed9a41f8dcca9bf7184d849ea94a8294b4"
examples/Web/web/yarn.lock:3309:  resolved "https://registry.yarnpkg.com/css-has-pseudo/-/css-has-pseudo-3.0.3.tgz#4824a34cb92dae7e09ea1d3fd19691b653412098"
examples/Web/web/yarn.lock:3316:  resolved "https://registry.yarnpkg.com/css-loader/-/css-loader-6.5.1.tgz#0c43d4fbe0d97f699c91e9818cb585759091d1b1"
examples/Web/web/yarn.lock:3330:  resolved "https://registry.yarnpkg.com/css-minimizer-webpack-plugin/-/css-minimizer-webpack-plugin-3.4.1.tgz#ab78f781ced9181992fe7b6e4f3422e76429878f"
examples/Web/web/yarn.lock:3342:  resolved "https://registry.yarnpkg.com/css-prefers-color-scheme/-/css-prefers-color-scheme-6.0.3.tgz#ca8a22e5992c10a5b9d315155e7caee625903349"
examples/Web/web/yarn.lock:3347:  resolved "https://registry.yarnpkg.com/css-select-base-adapter/-/css-select-base-adapter-0.1.1.tgz#3b2ff4972cc362ab88561507a95408a1432135d7"
examples/Web/web/yarn.lock:3352:  resolved "https://registry.yarnpkg.com/css-select/-/css-select-2.1.0.tgz#6a34653356635934a81baca68d0255432105dbef"
examples/Web/web/yarn.lock:3362:  resolved "https://registry.yarnpkg.com/css-select/-/css-select-4.2.1.tgz#9e665d6ae4c7f9d65dbe69d0316e3221fb274cdd"
examples/Web/web/yarn.lock:3373:  resolved "https://registry.yarnpkg.com/css-tree/-/css-tree-1.0.0-alpha.37.tgz#98bebd62c4c1d9f960ec340cf9f7522e30709a22"
examples/Web/web/yarn.lock:3381:  resolved "https://registry.yarnpkg.com/css-tree/-/css-tree-1.1.3.tgz#eb4870fb6fd7707327ec95c2ff2ab09b5e8db91d"
examples/Web/web/yarn.lock:3389:  resolved "https://registry.yarnpkg.com/css-what/-/css-what-3.4.2.tgz#ea7026fcb01777edbde52124e21f327e7ae950e4"
examples/Web/web/yarn.lock:3394:  resolved "https://registry.yarnpkg.com/css-what/-/css-what-5.1.0.tgz#3f7b707aadf633baf62c2ceb8579b545bb40f7fe"
examples/Web/web/yarn.lock:3399:  resolved "https://registry.yarnpkg.com/cssdb/-/cssdb-6.1.0.tgz#75d63b1257e33af72ffdfec65f0f342189e4ab37"
examples/Web/web/yarn.lock:3404:  resolved "https://registry.yarnpkg.com/cssesc/-/cssesc-3.0.0.tgz#37741919903b868565e1c09ea747445cd18983ee"
examples/Web/web/yarn.lock:3409:  resolved "https://registry.yarnpkg.com/cssnano-preset-default/-/cssnano-preset-default-5.1.11.tgz#db10fb1ecee310e8285c5aca45bd8237be206828"
examples/Web/web/yarn.lock:3444:  resolved "https://registry.yarnpkg.com/cssnano-utils/-/cssnano-utils-3.0.1.tgz#d3cc0a142d3d217f8736837ec0a2ccff6a89c6ea"
examples/Web/web/yarn.lock:3449:  resolved "https://registry.yarnpkg.com/cssnano/-/cssnano-5.0.16.tgz#4ee97d30411693f3de24cef70b36f7ae2a843e04"
examples/Web/web/yarn.lock:3458:  resolved "https://registry.yarnpkg.com/csso/-/csso-4.2.0.tgz#ea3a561346e8dc9f546d6febedd50187cf389529"
examples/Web/web/yarn.lock:3465:  resolved "https://registry.yarnpkg.com/cssom/-/cssom-0.4.4.tgz#5a66cf93d2d0b661d80bf6a44fb65f5c2e4e0a10"
examples/Web/web/yarn.lock:3470:  resolved "https://registry.yarnpkg.com/cssom/-/cssom-0.3.8.tgz#9f1276f5b2b463f2114d3f2c75250af8c1a36f4a"
examples/Web/web/yarn.lock:3475:  resolved "https://registry.yarnpkg.com/cssstyle/-/cssstyle-2.3.0.tgz#ff665a0ddbdc31864b09647f34163443d90b0852"
examples/Web/web/yarn.lock:3482:  resolved "https://registry.yarnpkg.com/damerau-levenshtein/-/damerau-levenshtein-1.0.8.tgz#b43d286ccbd36bc5b2f7ed41caf2d0aba1f8a6e7"
examples/Web/web/yarn.lock:3487:  resolved "https://registry.yarnpkg.com/data-urls/-/data-urls-2.0.0.tgz#156485a72963a970f5d5821aaf642bef2bf2db9b"
examples/Web/web/yarn.lock:3496:  resolved "https://registry.yarnpkg.com/debug/-/debug-2.6.9.tgz#5d128515df134ff327e90a4c93f4e077a536341f"
examples/Web/web/yarn.lock:3503:  resolved "https://registry.yarnpkg.com/debug/-/debug-4.3.3.tgz#04266e0b70a98d4462e6e288e38259213332b664"
examples/Web/web/yarn.lock:3510:  resolved "https://registry.yarnpkg.com/debug/-/debug-3.2.7.tgz#72580b7e9145fb39b6676f9c5e5fb100b934179a"
examples/Web/web/yarn.lock:3517:  resolved "https://registry.yarnpkg.com/decimal.js/-/decimal.js-10.3.1.tgz#d8c3a444a9c6774ba60ca6ad7261c3a94fd5e783"
examples/Web/web/yarn.lock:3522:  resolved "https://registry.yarnpkg.com/dedent/-/dedent-0.7.0.tgz#2495ddbaf6eb874abb0e1be9df22d2e5a544326c"
examples/Web/web/yarn.lock:3527:  resolved "https://registry.yarnpkg.com/deep-equal/-/deep-equal-1.1.1.tgz#b5c98c942ceffaf7cb051e24e1434a25a2e6076a"
examples/Web/web/yarn.lock:3539:  resolved "https://registry.yarnpkg.com/deep-is/-/deep-is-0.1.4.tgz#a6f2dce612fadd2ef1f519b73551f17e85199831"
examples/Web/web/yarn.lock:3544:  resolved "https://registry.yarnpkg.com/deepmerge/-/deepmerge-4.2.2.tgz#44d2ea3679b8f4d4ffba33f03d865fc1e7bf4955"
examples/Web/web/yarn.lock:3549:  resolved "https://registry.yarnpkg.com/default-gateway/-/default-gateway-6.0.3.tgz#819494c888053bdb743edbf343d6cdf7f2943a71"
examples/Web/web/yarn.lock:3552:    execa "^5.0.0"
examples/Web/web/yarn.lock:3556:  resolved "https://registry.yarnpkg.com/define-data-property/-/define-data-property-1.1.4.tgz#894dc141bb7d3060ae4366f6a0107e68fbe48c5e"
examples/Web/web/yarn.lock:3565:  resolved "https://registry.yarnpkg.com/define-lazy-prop/-/define-lazy-prop-2.0.0.tgz#3f7ae421129bcaaac9bc74905c98a0009ec9ee7f"
examples/Web/web/yarn.lock:3570:  resolved "https://registry.yarnpkg.com/define-properties/-/define-properties-1.1.3.tgz#cf88da6cbee26fe6db7094f61d870cbd84cee9f1"
examples/Web/web/yarn.lock:3577:  resolved "https://registry.yarnpkg.com/defined/-/defined-1.0.0.tgz#c98d9bcef75674188e110969151199e39b1fa693"
examples/Web/web/yarn.lock:3582:  resolved "https://registry.yarnpkg.com/del/-/del-6.0.0.tgz#0b40d0332cea743f1614f818be4feb717714c952"
examples/Web/web/yarn.lock:3588:    is-path-cwd "^2.2.0"
examples/Web/web/yarn.lock:3589:    is-path-inside "^3.0.2"
examples/Web/web/yarn.lock:3596:  resolved "https://registry.yarnpkg.com/delayed-stream/-/delayed-stream-1.0.0.tgz#df3ae199acadfb7d440aaae0b29e2272b24ec619"
examples/Web/web/yarn.lock:3601:  resolved "https://registry.yarnpkg.com/depd/-/depd-2.0.0.tgz#b696163cc757560d09cf22cc8fad1571b79e76df"
examples/Web/web/yarn.lock:3606:  resolved "https://registry.yarnpkg.com/depd/-/depd-1.1.2.tgz#9bcd52e14c097763e749b274c4346ed2e560b5a9"
examples/Web/web/yarn.lock:3611:  resolved "https://registry.yarnpkg.com/destroy/-/destroy-1.2.0.tgz#4803735509ad8be552934c67df614f94e66fa015"
examples/Web/web/yarn.lock:3616:  resolved "https://registry.yarnpkg.com/detect-newline/-/detect-newline-3.1.0.tgz#576f5dfc63ae1a192ff192d8ad3af6308991b651"
examples/Web/web/yarn.lock:3621:  resolved "https://registry.yarnpkg.com/detect-node/-/detect-node-2.1.0.tgz#c9c70775a49c3d03bc2c06d9a73be550f978f8b1"
examples/Web/web/yarn.lock:3626:  resolved "https://registry.yarnpkg.com/detect-port-alt/-/detect-port-alt-1.1.6.tgz#24707deabe932d4a3cf621302027c2b266568275"
examples/Web/web/yarn.lock:3634:  resolved "https://registry.yarnpkg.com/detective/-/detective-5.2.0.tgz#feb2a77e85b904ecdea459ad897cc90a99bd2a7b"
examples/Web/web/yarn.lock:3643:  resolved "https://registry.yarnpkg.com/didyoumean/-/didyoumean-1.2.2.tgz#989346ffe9e839b4555ecf5666edea0d3e8ad037"
examples/Web/web/yarn.lock:3648:  resolved "https://registry.yarnpkg.com/diff-sequences/-/diff-sequences-27.4.0.tgz#d783920ad8d06ec718a060d00196dfef25b132a5"
examples/Web/web/yarn.lock:3653:  resolved "https://registry.yarnpkg.com/dir-glob/-/dir-glob-3.0.1.tgz#56dbf73d992a4a93ba1584f4534063fd2e41717f"
examples/Web/web/yarn.lock:3656:    path-type "^4.0.0"
examples/Web/web/yarn.lock:3660:  resolved "https://registry.yarnpkg.com/dlv/-/dlv-1.1.3.tgz#5c198a8a11453596e751494d49874bc7732f2e79"
examples/Web/web/yarn.lock:3665:  resolved "https://registry.yarnpkg.com/dns-equal/-/dns-equal-1.0.0.tgz#b39e7f1da6eb0a75ba9c17324b34753c47e0654d"
examples/Web/web/yarn.lock:3670:  resolved "https://registry.yarnpkg.com/dns-packet/-/dns-packet-1.3.4.tgz#e3455065824a2507ba886c55a89963bb107dec6f"
examples/Web/web/yarn.lock:3678:  resolved "https://registry.yarnpkg.com/dns-txt/-/dns-txt-2.0.2.tgz#b91d806f5d27188e4ab3e7d107d881a1cc4642b6"
examples/Web/web/yarn.lock:3685:  resolved "https://registry.yarnpkg.com/doctrine/-/doctrine-2.1.0.tgz#5cd01fc101621b42c4cd7f5d1a66243716d3f39d"
examples/Web/web/yarn.lock:3692:  resolved "https://registry.yarnpkg.com/doctrine/-/doctrine-3.0.0.tgz#addebead72a6574db783639dc87a121773973961"
examples/Web/web/yarn.lock:3699:  resolved "https://registry.yarnpkg.com/dom-converter/-/dom-converter-0.2.0.tgz#6721a9daee2e293682955b6afe416771627bb768"
examples/Web/web/yarn.lock:3706:  resolved "https://registry.yarnpkg.com/dom-serializer/-/dom-serializer-0.2.2.tgz#1afb81f533717175d478655debc5e332d9f9bb51"
examples/Web/web/yarn.lock:3714:  resolved "https://registry.yarnpkg.com/dom-serializer/-/dom-serializer-1.3.2.tgz#6206437d32ceefaec7161803230c7a20bc1b4d91"
examples/Web/web/yarn.lock:3723:  resolved "https://registry.yarnpkg.com/domelementtype/-/domelementtype-1.3.1.tgz#d048c44b37b0d10a7f2a3d5fee3f4333d790481f"
examples/Web/web/yarn.lock:3728:  resolved "https://registry.yarnpkg.com/domelementtype/-/domelementtype-2.2.0.tgz#9a0b6c2782ed6a1c7323d42267183df9bd8b1d57"
examples/Web/web/yarn.lock:3733:  resolved "https://registry.yarnpkg.com/domexception/-/domexception-2.0.1.tgz#fb44aefba793e1574b0af6aed2801d057529f304"
examples/Web/web/yarn.lock:3740:  resolved "https://registry.yarnpkg.com/domhandler/-/domhandler-4.3.0.tgz#16c658c626cf966967e306f966b431f77d4a5626"
examples/Web/web/yarn.lock:3747:  resolved "https://registry.yarnpkg.com/domutils/-/domutils-1.7.0.tgz#56ea341e834e06e6748af7a1cb25da67ea9f8c2a"
examples/Web/web/yarn.lock:3755:  resolved "https://registry.yarnpkg.com/domutils/-/domutils-2.8.0.tgz#4437def5db6e2d1f5d6ee859bd95ca7d02048135"
examples/Web/web/yarn.lock:3764:  resolved "https://registry.yarnpkg.com/dot-case/-/dot-case-3.0.4.tgz#9b2b670d00a431667a8a75ba29cd1b98809ce751"
examples/Web/web/yarn.lock:3772:  resolved "https://registry.yarnpkg.com/dotenv-expand/-/dotenv-expand-5.1.0.tgz#3fbaf020bfd794884072ea26b1e9791d45a629f0"
examples/Web/web/yarn.lock:3777:  resolved "https://registry.yarnpkg.com/dotenv/-/dotenv-10.0.0.tgz#3d4227b8fb95f81096cdd2b66653fb2c7085ba81"
examples/Web/web/yarn.lock:3782:  resolved "https://registry.yarnpkg.com/dunder-proto/-/dunder-proto-1.0.1.tgz#d7ae667e1dc83482f8b70fd0f6eefc50da30f58a"
examples/Web/web/yarn.lock:3791:  resolved "https://registry.yarnpkg.com/duplexer/-/duplexer-0.1.2.tgz#3abe43aef3835f8ae077d136ddce0f276b0400e6"
examples/Web/web/yarn.lock:3796:  resolved "https://registry.yarnpkg.com/ee-first/-/ee-first-1.1.1.tgz#590c61156b0ae2f4f0255732a158b266bc56b21d"
examples/Web/web/yarn.lock:3801:  resolved "https://registry.yarnpkg.com/ejs/-/ejs-3.1.10.tgz#69ab8358b14e896f80cc39e62087b88500c3ac3b"
examples/Web/web/yarn.lock:3808:  resolved "https://registry.yarnpkg.com/electron-to-chromium/-/electron-to-chromium-1.4.59.tgz#657f2588c048fb95975779f8fea101fad854de89"
examples/Web/web/yarn.lock:3813:  resolved "https://registry.yarnpkg.com/electron-to-chromium/-/electron-to-chromium-1.5.286.tgz#142be1ab5e1cd5044954db0e5898f60a4960384e"
examples/Web/web/yarn.lock:3818:  resolved "https://registry.yarnpkg.com/emittery/-/emittery-0.8.1.tgz#bb23cc86d03b30aa75a7f734819dee2e1ba70860"
examples/Web/web/yarn.lock:3823:  resolved "https://registry.yarnpkg.com/emoji-regex/-/emoji-regex-8.0.0.tgz#e818fd69ce5ccfcb404594f842963bf53164cc37"
examples/Web/web/yarn.lock:3828:  resolved "https://registry.yarnpkg.com/emoji-regex/-/emoji-regex-9.2.2.tgz#840c8803b0d8047f4ff0cf963176b32d4ef3ed72"
examples/Web/web/yarn.lock:3833:  resolved "https://registry.yarnpkg.com/emojis-list/-/emojis-list-3.0.0.tgz#5570662046ad29e2e916e71aae260abdff4f6a78"
examples/Web/web/yarn.lock:3838:  resolved "https://registry.yarnpkg.com/encodeurl/-/encodeurl-1.0.2.tgz#ad3ff4c86ec2d029322f5a02c3a9a606c95b3f59"
examples/Web/web/yarn.lock:3843:  resolved "https://registry.yarnpkg.com/encodeurl/-/encodeurl-2.0.0.tgz#7b8ea898077d7e409d3ac45474ea38eaf0857a58"
examples/Web/web/yarn.lock:3848:  resolved "https://registry.yarnpkg.com/enhanced-resolve/-/enhanced-resolve-5.19.0.tgz#6687446a15e969eaa63c2fa2694510e17ae6d97c"
examples/Web/web/yarn.lock:3856:  resolved "https://registry.yarnpkg.com/entities/-/entities-2.2.0.tgz#098dc90ebb83d8dffa089d55256b351d34c4da55"
examples/Web/web/yarn.lock:3861:  resolved "https://registry.yarnpkg.com/error-ex/-/error-ex-1.3.2.tgz#b4ac40648107fdcdcfae242f428bea8a14d4f1bf"
examples/Web/web/yarn.lock:3868:  resolved "https://registry.yarnpkg.com/error-stack-parser/-/error-stack-parser-2.0.6.tgz#5a99a707bd7a4c58a797902d48d82803ede6aad8"
examples/Web/web/yarn.lock:3875:  resolved "https://registry.yarnpkg.com/es-abstract/-/es-abstract-1.19.1.tgz#d4885796876916959de78edaa0df456627115ec3"
examples/Web/web/yarn.lock:3901:  resolved "https://registry.yarnpkg.com/es-define-property/-/es-define-property-1.0.0.tgz#c7faefbdff8b2696cf5f46921edfb77cc4ba3845"
examples/Web/web/yarn.lock:3908:  resolved "https://registry.yarnpkg.com/es-define-property/-/es-define-property-1.0.1.tgz#983eb2f9a6724e9303f61addf011c72e09e0b0fa"
examples/Web/web/yarn.lock:3913:  resolved "https://registry.yarnpkg.com/es-errors/-/es-errors-1.3.0.tgz#05f75a25dab98e4fb1dcd5e1472c0546d5057c8f"
examples/Web/web/yarn.lock:3918:  resolved "https://registry.yarnpkg.com/es-module-lexer/-/es-module-lexer-2.0.0.tgz#f657cd7a9448dcdda9c070a3cb75e5dc1e85f5b1"
examples/Web/web/yarn.lock:3923:  resolved "https://registry.yarnpkg.com/es-object-atoms/-/es-object-atoms-1.1.1.tgz#1c4f2c4837327597ce69d2ca190a7fdd172338c1"
examples/Web/web/yarn.lock:3930:  resolved "https://registry.yarnpkg.com/es-set-tostringtag/-/es-set-tostringtag-2.1.0.tgz#f31dbbe0c183b00a6d26eb6325c810c0fd18bd4d"
examples/Web/web/yarn.lock:3940:  resolved "https://registry.yarnpkg.com/es-to-primitive/-/es-to-primitive-1.2.1.tgz#e55cd4c9cdc188bcefb03b366c736323fc5c898a"
examples/Web/web/yarn.lock:3949:  resolved "https://registry.yarnpkg.com/escalade/-/escalade-3.1.1.tgz#d8cfdc7000965c5a0174b4a82eaa5c0552742e40"
examples/Web/web/yarn.lock:3954:  resolved "https://registry.yarnpkg.com/escalade/-/escalade-3.2.0.tgz#011a3f69856ba189dffa7dc8fcce99d2a87903e5"
examples/Web/web/yarn.lock:3959:  resolved "https://registry.yarnpkg.com/escape-html/-/escape-html-1.0.3.tgz#0258eae4d3d0c0974de1c169188ef0051d1d1988"
examples/Web/web/yarn.lock:3964:  resolved "https://registry.yarnpkg.com/escape-string-regexp/-/escape-string-regexp-1.0.5.tgz#1b61c0562190a8dff6ae3bb2cf0200ca130b86d4"
examples/Web/web/yarn.lock:3969:  resolved "https://registry.yarnpkg.com/escape-string-regexp/-/escape-string-regexp-2.0.0.tgz#a30304e99daa32e23b2fd20f51babd07cffca344"
examples/Web/web/yarn.lock:3974:  resolved "https://registry.yarnpkg.com/escape-string-regexp/-/escape-string-regexp-4.0.0.tgz#14ba83a5d373e3d311e5afca29cf5bfad965bf34"
examples/Web/web/yarn.lock:3979:  resolved "https://registry.yarnpkg.com/escodegen/-/escodegen-2.0.0.tgz#5e32b12833e8aa8fa35e1bf0befa89380484c7dd"
examples/Web/web/yarn.lock:3991:  resolved "https://registry.yarnpkg.com/eslint-config-react-app/-/eslint-config-react-app-7.0.0.tgz#0fa96d5ec1dfb99c029b1554362ab3fa1c3757df"
examples/Web/web/yarn.lock:4011:  resolved "https://registry.yarnpkg.com/eslint-import-resolver-node/-/eslint-import-resolver-node-0.3.6.tgz#4048b958395da89668252001dbd9eca6b83bacbd"
examples/Web/web/yarn.lock:4019:  resolved "https://registry.yarnpkg.com/eslint-module-utils/-/eslint-module-utils-2.7.3.tgz#ad7e3a10552fdd0642e1e55292781bd6e34876ee"
examples/Web/web/yarn.lock:4027:  resolved "https://registry.yarnpkg.com/eslint-plugin-flowtype/-/eslint-plugin-flowtype-8.0.3.tgz#e1557e37118f24734aa3122e7536a038d34a4912"
examples/Web/web/yarn.lock:4035:  resolved "https://registry.yarnpkg.com/eslint-plugin-import/-/eslint-plugin-import-2.25.4.tgz#322f3f916a4e9e991ac7af32032c25ce313209f1"
examples/Web/web/yarn.lock:4050:    tsconfig-paths "^3.12.0"
examples/Web/web/yarn.lock:4054:  resolved "https://registry.yarnpkg.com/eslint-plugin-jest/-/eslint-plugin-jest-25.7.0.tgz#ff4ac97520b53a96187bad9c9814e7d00de09a6a"
examples/Web/web/yarn.lock:4061:  resolved "https://registry.yarnpkg.com/eslint-plugin-jsx-a11y/-/eslint-plugin-jsx-a11y-6.5.1.tgz#cdbf2df901040ca140b6ec14715c988889c2a6d8"
examples/Web/web/yarn.lock:4079:  resolved "https://registry.yarnpkg.com/eslint-plugin-react-hooks/-/eslint-plugin-react-hooks-4.3.0.tgz#318dbf312e06fab1c835a4abef00121751ac1172"
examples/Web/web/yarn.lock:4084:  resolved "https://registry.yarnpkg.com/eslint-plugin-react/-/eslint-plugin-react-7.28.0.tgz#8f3ff450677571a659ce76efc6d80b6a525adbdf"
examples/Web/web/yarn.lock:4104:  resolved "https://registry.yarnpkg.com/eslint-plugin-testing-library/-/eslint-plugin-testing-library-5.0.5.tgz#5757961ec20a6ca8b0992d2c5487db1b51612d8d"
examples/Web/web/yarn.lock:4111:  resolved "https://registry.yarnpkg.com/eslint-scope/-/eslint-scope-5.1.1.tgz#e786e59a66cb92b3f6c1fb0d508aab174848f48c"
examples/Web/web/yarn.lock:4119:  resolved "https://registry.yarnpkg.com/eslint-scope/-/eslint-scope-7.1.0.tgz#c1f6ea30ac583031f203d65c73e723b01298f153"
examples/Web/web/yarn.lock:4127:  resolved "https://registry.yarnpkg.com/eslint-utils/-/eslint-utils-3.0.0.tgz#8aebaface7345bb33559db0a1f13a1d2d48c3672"
examples/Web/web/yarn.lock:4134:  resolved "https://registry.yarnpkg.com/eslint-visitor-keys/-/eslint-visitor-keys-2.1.0.tgz#f65328259305927392c938ed44eb0a5c9b2bd303"
examples/Web/web/yarn.lock:4139:  resolved "https://registry.yarnpkg.com/eslint-visitor-keys/-/eslint-visitor-keys-3.2.0.tgz#6fbb166a6798ee5991358bc2daa1ba76cc1254a1"
examples/Web/web/yarn.lock:4144:  resolved "https://registry.yarnpkg.com/eslint-webpack-plugin/-/eslint-webpack-plugin-3.1.1.tgz#83dad2395e5f572d6f4d919eedaa9cf902890fcb"
examples/Web/web/yarn.lock:4150:    normalize-path "^3.0.0"
examples/Web/web/yarn.lock:4155:  resolved "https://registry.yarnpkg.com/eslint/-/eslint-8.8.0.tgz#9762b49abad0cb4952539ffdb0a046392e571a2d"
examples/Web/web/yarn.lock:4162:    cross-spawn "^7.0.2"
examples/Web/web/yarn.lock:4196:  resolved "https://registry.yarnpkg.com/espree/-/espree-9.3.0.tgz#c1240d79183b72aaee6ccfa5a90bc9111df085a8"
examples/Web/web/yarn.lock:4205:  resolved "https://registry.yarnpkg.com/esprima/-/esprima-4.0.1.tgz#13b04cdb3e6c5d19df91ab6987a8695619b0aa71"
examples/Web/web/yarn.lock:4210:  resolved "https://registry.yarnpkg.com/esquery/-/esquery-1.4.0.tgz#2148ffc38b82e8c7057dfed48425b3e61f0f24a5"
examples/Web/web/yarn.lock:4217:  resolved "https://registry.yarnpkg.com/esrecurse/-/esrecurse-4.3.0.tgz#7ad7964d679abb28bee72cec63758b1c5d2c9921"
examples/Web/web/yarn.lock:4224:  resolved "https://registry.yarnpkg.com/estraverse/-/estraverse-4.3.0.tgz#398ad3f3c5a24948be7725e83d11a7de28cdbd1d"
examples/Web/web/yarn.lock:4229:  resolved "https://registry.yarnpkg.com/estraverse/-/estraverse-5.3.0.tgz#2eea5290702f26ab8fe5370370ff86c965d21123"
examples/Web/web/yarn.lock:4234:  resolved "https://registry.yarnpkg.com/estree-walker/-/estree-walker-1.0.1.tgz#31bc5d612c96b704106b477e6dd5d8aa138cb700"
examples/Web/web/yarn.lock:4239:  resolved "https://registry.yarnpkg.com/esutils/-/esutils-2.0.3.tgz#74d2eb4de0b8da1293711910d50775b9b710ef64"
examples/Web/web/yarn.lock:4244:  resolved "https://registry.yarnpkg.com/etag/-/etag-1.8.1.tgz#41ae2eeb65efa62268aebfea83ac7d79299b0887"
examples/Web/web/yarn.lock:4249:  resolved "https://registry.yarnpkg.com/eventemitter3/-/eventemitter3-4.0.7.tgz#2de9b68f6528d5644ef5c59526a1b4a07306169f"
examples/Web/web/yarn.lock:4254:  resolved "https://registry.yarnpkg.com/events/-/events-3.3.0.tgz#31a95ad0a924e2d2c419a813aeb2c4e878ea7400"
examples/Web/web/yarn.lock:4257:execa@^5.0.0:
examples/Web/web/yarn.lock:4259:  resolved "https://registry.yarnpkg.com/execa/-/execa-5.1.1.tgz#f80ad9cbf4298f7bd1d4c9555c21e93741c411dd"
examples/Web/web/yarn.lock:4262:    cross-spawn "^7.0.3"
examples/Web/web/yarn.lock:4267:    npm-run-path "^4.0.1"
examples/Web/web/yarn.lock:4274:  resolved "https://registry.yarnpkg.com/exenv/-/exenv-1.2.2.tgz#2ae78e85d9894158670b03d47bec1f03bd91bb9d"
examples/Web/web/yarn.lock:4279:  resolved "https://registry.yarnpkg.com/exit/-/exit-0.1.2.tgz#0632638f8d877cc82107d30a0fff1a17cba1cd0c"
examples/Web/web/yarn.lock:4284:  resolved "https://registry.yarnpkg.com/expect/-/expect-27.4.6.tgz#f335e128b0335b6ceb4fcab67ece7cbd14c942e6"
examples/Web/web/yarn.lock:4294:  resolved "https://registry.yarnpkg.com/express/-/express-4.21.0.tgz#d57cb706d49623d4ac27833f1cbc466b668eb915"
examples/Web/web/yarn.lock:4302:    cookie "0.6.0"
examples/Web/web/yarn.lock:4303:    cookie-signature "1.0.6"
examples/Web/web/yarn.lock:4316:    path-to-regexp "0.1.10"
examples/Web/web/yarn.lock:4317:    proxy-addr "~2.0.7"
examples/Web/web/yarn.lock:4331:  resolved "https://registry.yarnpkg.com/fast-deep-equal/-/fast-deep-equal-3.1.3.tgz#3a7d56b559d6cbc3eb512325244e619a65c6c525"
examples/Web/web/yarn.lock:4336:  resolved "https://registry.yarnpkg.com/fast-glob/-/fast-glob-3.2.11.tgz#a1172ad95ceb8a16e20caa5c5e56480e5129c1d9"
examples/Web/web/yarn.lock:4347:  resolved "https://registry.yarnpkg.com/fast-json-stable-stringify/-/fast-json-stable-stringify-2.1.0.tgz#874bf69c6f404c2b5d99c481341399fd55892633"
examples/Web/web/yarn.lock:4352:  resolved "https://registry.yarnpkg.com/fast-levenshtein/-/fast-levenshtein-2.0.6.tgz#3d8a5c66883a16a30ca8643e851f19baa7797917"
examples/Web/web/yarn.lock:4357:  resolved "https://registry.yarnpkg.com/fast-uri/-/fast-uri-3.1.0.tgz#66eecff6c764c0df9b762e62ca7edcfb53b4edfa"
examples/Web/web/yarn.lock:4362:  resolved "https://registry.yarnpkg.com/fastq/-/fastq-1.13.0.tgz#616760f88a7526bdfc596b7cab8c18938c36b98c"
examples/Web/web/yarn.lock:4369:  resolved "https://registry.yarnpkg.com/faye-websocket/-/faye-websocket-0.11.4.tgz#7f0d9275cfdd86a1c963dc8b65fcc451edcbb1da"
examples/Web/web/yarn.lock:4376:  resolved "https://registry.yarnpkg.com/fb-watchman/-/fb-watchman-2.0.1.tgz#fc84fb39d2709cf3ff6d743706157bb5708a8a85"
examples/Web/web/yarn.lock:4383:  resolved "https://registry.yarnpkg.com/file-entry-cache/-/file-entry-cache-6.0.1.tgz#211b2dd9659cb0394b073e7323ac3c933d522027"
examples/Web/web/yarn.lock:4390:  resolved "https://registry.yarnpkg.com/file-loader/-/file-loader-6.2.0.tgz#baef7cf8e1840df325e4390b4484879480eebe4d"
examples/Web/web/yarn.lock:4398:  resolved "https://registry.yarnpkg.com/filelist/-/filelist-1.0.4.tgz#f78978a1e944775ff9e62e744424f215e58352b5"
examples/Web/web/yarn.lock:4405:  resolved "https://registry.yarnpkg.com/filesize/-/filesize-8.0.7.tgz#695e70d80f4e47012c132d57a059e80c6b580bd8"
examples/Web/web/yarn.lock:4410:  resolved "https://registry.yarnpkg.com/fill-range/-/fill-range-7.1.1.tgz#44265d3cac07e3ea7dc247516380643754a05292"
examples/Web/web/yarn.lock:4417:  resolved "https://registry.yarnpkg.com/finalhandler/-/finalhandler-1.3.1.tgz#0c575f1d1d324ddd1da35ad7ece3df7d19088019"
examples/Web/web/yarn.lock:4430:  resolved "https://registry.yarnpkg.com/find-cache-dir/-/find-cache-dir-3.3.2.tgz#b30c5b6eff0730731aea9bbd9dbecbd80256d64b"
examples/Web/web/yarn.lock:4439:  resolved "https://registry.yarnpkg.com/find-up/-/find-up-2.1.0.tgz#45d1b7e506c717ddd482775a2b77920a3c0c57a7"
examples/Web/web/yarn.lock:4442:    locate-path "^2.0.0"
examples/Web/web/yarn.lock:4446:  resolved "https://registry.yarnpkg.com/find-up/-/find-up-3.0.0.tgz#49169f1d7993430646da61ecc5ae355c21c97b73"
examples/Web/web/yarn.lock:4449:    locate-path "^3.0.0"
examples/Web/web/yarn.lock:4453:  resolved "https://registry.yarnpkg.com/find-up/-/find-up-4.1.0.tgz#97afe7d6cdc0bc5928584b7c8d7b16e8a9aa5d19"
examples/Web/web/yarn.lock:4456:    locate-path "^5.0.0"
examples/Web/web/yarn.lock:4457:    path-exists "^4.0.0"
examples/Web/web/yarn.lock:4461:  resolved "https://registry.yarnpkg.com/find-up/-/find-up-5.0.0.tgz#4c92819ecb7083561e4f4a240a86be5198f536fc"
examples/Web/web/yarn.lock:4464:    locate-path "^6.0.0"
examples/Web/web/yarn.lock:4465:    path-exists "^4.0.0"
examples/Web/web/yarn.lock:4469:  resolved "https://registry.yarnpkg.com/flat-cache/-/flat-cache-3.0.4.tgz#61b0338302b2fe9f957dcc32fc2a87f1c3048b11"
examples/Web/web/yarn.lock:4477:  resolved "https://registry.yarnpkg.com/flatted/-/flatted-3.4.2.tgz#f5c23c107f0f37de8dbdf24f13722b3b98d52726"
examples/Web/web/yarn.lock:4480:follow-redirects@^1.0.0, follow-redirects@^1.15.11:
examples/Web/web/yarn.lock:4482:  resolved "https://registry.yarnpkg.com/follow-redirects/-/follow-redirects-1.16.0.tgz#28474a159d3b9d11ef62050a14ed60e4df6d61bc"
examples/Web/web/yarn.lock:4487:  resolved "https://registry.yarnpkg.com/fork-ts-checker-webpack-plugin/-/fork-ts-checker-webpack-plugin-6.5.0.tgz#0282b335fa495a97e167f69018f566ea7d2a2b5e"
examples/Web/web/yarn.lock:4506:  resolved "https://registry.yarnpkg.com/form-data/-/form-data-3.0.1.tgz#ebd53791b78356a99af9a300d4282c4d5eb9755f"
examples/Web/web/yarn.lock:4515:  resolved "https://registry.yarnpkg.com/form-data/-/form-data-4.0.5.tgz#b49e48858045ff4cbf6b03e1805cebcad3679053"
examples/Web/web/yarn.lock:4524:forwarded@0.2.0:
examples/Web/web/yarn.lock:4526:  resolved "https://registry.yarnpkg.com/forwarded/-/forwarded-0.2.0.tgz#2269936428aad4c15c7ebe9779a84bf0b2a81811"
examples/Web/web/yarn.lock:4531:  resolved "https://registry.yarnpkg.com/fraction.js/-/fraction.js-4.1.2.tgz#13e420a92422b6cf244dff8690ed89401029fbe8"
examples/Web/web/yarn.lock:4536:  resolved "https://registry.yarnpkg.com/fresh/-/fresh-0.5.2.tgz#3d8cadd90d976569fa835ab1f8e4b23a105605a7"
examples/Web/web/yarn.lock:4541:  resolved "https://registry.yarnpkg.com/fs-extra/-/fs-extra-10.0.0.tgz#9ff61b655dde53fb34a82df84bb214ce802e17c1"
examples/Web/web/yarn.lock:4550:  resolved "https://registry.yarnpkg.com/fs-extra/-/fs-extra-9.1.0.tgz#5954460c764a8da2094ba3554bf839e6b9a7c86d"
examples/Web/web/yarn.lock:4560:  resolved "https://registry.yarnpkg.com/fs-monkey/-/fs-monkey-1.0.3.tgz#ae3ac92d53bb328efe0e9a1d9541f6ad8d48e2d3"
examples/Web/web/yarn.lock:4565:  resolved "https://registry.yarnpkg.com/fs-monkey/-/fs-monkey-1.0.5.tgz#fe450175f0db0d7ea758102e1d84096acb925788"
examples/Web/web/yarn.lock:4568:fs.realpath@^1.0.0:
examples/Web/web/yarn.lock:4570:  resolved "https://registry.yarnpkg.com/fs.realpath/-/fs.realpath-1.0.0.tgz#1504ad2523158caa40db4a2787cb01411994ea4f"
examples/Web/web/yarn.lock:4575:  resolved "https://registry.yarnpkg.com/fsevents/-/fsevents-2.3.2.tgz#8a526f78b8fdf4623b709e0b975c52c24c02fd1a"
examples/Web/web/yarn.lock:4580:  resolved "https://registry.yarnpkg.com/function-bind/-/function-bind-1.1.1.tgz#a56899d3ea3c9bab874bb9773b7c5ede92f4895d"
examples/Web/web/yarn.lock:4585:  resolved "https://registry.yarnpkg.com/function-bind/-/function-bind-1.1.2.tgz#2c02d864d97f3ea6c8830c464cbd11ab6eab7a1c"
examples/Web/web/yarn.lock:4590:  resolved "https://registry.yarnpkg.com/functional-red-black-tree/-/functional-red-black-tree-1.0.1.tgz#1b0ab3bd553b2a0d6399d29c0e3ea0b252078327"
examples/Web/web/yarn.lock:4595:  resolved "https://registry.yarnpkg.com/gensync/-/gensync-1.0.0-beta.2.tgz#32a6ee76c3d7f52d46b2b1ae5d93fea8580a25e0"
examples/Web/web/yarn.lock:4600:  resolved "https://registry.yarnpkg.com/get-caller-file/-/get-caller-file-2.0.5.tgz#4f94412a82db32f36e3b0b9741f8a97feb031f7e"
examples/Web/web/yarn.lock:4605:  resolved "https://registry.yarnpkg.com/get-intrinsic/-/get-intrinsic-1.1.3.tgz#063c84329ad93e83893c7f4f243ef63ffa351385"
examples/Web/web/yarn.lock:4614:  resolved "https://registry.yarnpkg.com/get-intrinsic/-/get-intrinsic-1.1.1.tgz#15f59f376f855c446963948f0d24cd3637b4abc6"
examples/Web/web/yarn.lock:4623:  resolved "https://registry.yarnpkg.com/get-intrinsic/-/get-intrinsic-1.2.4.tgz#e385f5a4b5227d449c3eabbad05494ef0abbeadd"
examples/Web/web/yarn.lock:4634:  resolved "https://registry.yarnpkg.com/get-intrinsic/-/get-intrinsic-1.3.0.tgz#743f0e3b6964a93a5491ed1bffaae054d7f98d01"
examples/Web/web/yarn.lock:4650:  resolved "https://registry.yarnpkg.com/get-own-enumerable-property-symbols/-/get-own-enumerable-property-symbols-3.0.2.tgz#b5fde77f22cbe35f390b4e089922c50bce6ef664"
examples/Web/web/yarn.lock:4655:  resolved "https://registry.yarnpkg.com/get-package-type/-/get-package-type-0.1.0.tgz#8de2d803cff44df3bc6c456e6668b36c3926e11a"
examples/Web/web/yarn.lock:4660:  resolved "https://registry.yarnpkg.com/get-proto/-/get-proto-1.0.1.tgz#150b3f2743869ef3e851ec0c49d15b1d14d00ee1"
examples/Web/web/yarn.lock:4668:  resolved "https://registry.yarnpkg.com/get-stream/-/get-stream-6.0.1.tgz#a262d8eef67aced57c2852ad6167526a43cbf7b7"
examples/Web/web/yarn.lock:4673:  resolved "https://registry.yarnpkg.com/get-symbol-description/-/get-symbol-description-1.0.0.tgz#7fdb81c900101fbd564dd5f1a30af5aadc1e58d6"
examples/Web/web/yarn.lock:4681:  resolved "https://registry.yarnpkg.com/glob-parent/-/glob-parent-5.1.2.tgz#869832c58034fe68a4093c17dc15e8340d8401c4"
examples/Web/web/yarn.lock:4688:  resolved "https://registry.yarnpkg.com/glob-parent/-/glob-parent-6.0.2.tgz#6d237d99083950c79290f24c7642a3de9a28f9e3"
examples/Web/web/yarn.lock:4695:  resolved "https://registry.yarnpkg.com/glob-to-regexp/-/glob-to-regexp-0.4.1.tgz#c75297087c851b9a578bd217dd59a92f59fe546e"
examples/Web/web/yarn.lock:4700:  resolved "https://registry.yarnpkg.com/glob/-/glob-7.2.0.tgz#d15535af7732e02e948f4c41628bd910293f6023"
examples/Web/web/yarn.lock:4703:    fs.realpath "^1.0.0"
examples/Web/web/yarn.lock:4708:    path-is-absolute "^1.0.0"
examples/Web/web/yarn.lock:4712:  resolved "https://registry.yarnpkg.com/global-modules/-/global-modules-2.0.0.tgz#997605ad2345f27f51539bea26574421215c7780"
examples/Web/web/yarn.lock:4719:  resolved "https://registry.yarnpkg.com/global-prefix/-/global-prefix-3.0.0.tgz#fc85f73064df69f50421f47f883fe5b913ba9b97"
examples/Web/web/yarn.lock:4728:  resolved "https://registry.yarnpkg.com/globals/-/globals-11.12.0.tgz#ab8795338868a0babd8525758018c2a7eb95c42e"
examples/Web/web/yarn.lock:4733:  resolved "https://registry.yarnpkg.com/globals/-/globals-13.12.0.tgz#4d733760304230a0082ed96e21e5c565f898089e"
examples/Web/web/yarn.lock:4740:  resolved "https://registry.yarnpkg.com/globby/-/globby-11.1.0.tgz#bd4be98bb042f83d796f7e3811991fbe82a0d34b"
examples/Web/web/yarn.lock:4752:  resolved "https://registry.yarnpkg.com/gopd/-/gopd-1.0.1.tgz#29ff76de69dac7489b7c0918a5788e56477c332c"
examples/Web/web/yarn.lock:4759:  resolved "https://registry.yarnpkg.com/gopd/-/gopd-1.2.0.tgz#89f56b8217bdbc8802bd299df6d7f1081d7e51a1"
examples/Web/web/yarn.lock:4764:  resolved "https://registry.yarnpkg.com/graceful-fs/-/graceful-fs-4.2.9.tgz#041b05df45755e587a24942279b9d113146e1c96"
examples/Web/web/yarn.lock:4769:  resolved "https://registry.yarnpkg.com/graceful-fs/-/graceful-fs-4.2.11.tgz#4183e4e8bf08bb6e05bbb2f7d2e0c8f712ca40e3"
examples/Web/web/yarn.lock:4774:  resolved "https://registry.yarnpkg.com/gzip-size/-/gzip-size-6.0.0.tgz#065367fd50c239c0671cbcbad5be3e2eeb10e462"
examples/Web/web/yarn.lock:4781:  resolved "https://registry.yarnpkg.com/handle-thing/-/handle-thing-2.0.1.tgz#857f79ce359580c340d43081cc648970d0bb234e"
examples/Web/web/yarn.lock:4786:  resolved "https://registry.yarnpkg.com/harmony-reflect/-/harmony-reflect-1.6.2.tgz#31ecbd32e648a34d030d86adb67d4d47547fe710"
examples/Web/web/yarn.lock:4791:  resolved "https://registry.yarnpkg.com/has-bigints/-/has-bigints-1.0.1.tgz#64fe6acb020673e3b78db035a5af69aa9d07b113"
examples/Web/web/yarn.lock:4796:  resolved "https://registry.yarnpkg.com/has-flag/-/has-flag-3.0.0.tgz#b5d454dc2199ae225699f3467e5a07f3b955bafd"
examples/Web/web/yarn.lock:4801:  resolved "https://registry.yarnpkg.com/has-flag/-/has-flag-4.0.0.tgz#944771fd9c81c81265c4d6941860da06bb59479b"
examples/Web/web/yarn.lock:4806:  resolved "https://registry.yarnpkg.com/has-property-descriptors/-/has-property-descriptors-1.0.2.tgz#963ed7d071dc7bf5f084c5bfbe0d1b6222586854"
examples/Web/web/yarn.lock:4813:  resolved "https://registry.yarnpkg.com/has-proto/-/has-proto-1.0.3.tgz#b31ddfe9b0e6e9914536a6ab286426d0214f77fd"
examples/Web/web/yarn.lock:4818:  resolved "https://registry.yarnpkg.com/has-symbols/-/has-symbols-1.0.2.tgz#165d3070c00309752a1236a479331e3ac56f1423"
examples/Web/web/yarn.lock:4823:  resolved "https://registry.yarnpkg.com/has-symbols/-/has-symbols-1.0.3.tgz#bb7b2c4349251dce87b125f7bdf874aa7c8b39f8"
examples/Web/web/yarn.lock:4828:  resolved "https://registry.yarnpkg.com/has-symbols/-/has-symbols-1.1.0.tgz#fc9c6a783a084951d0b971fe1018de813707a338"
examples/Web/web/yarn.lock:4833:  resolved "https://registry.yarnpkg.com/has-tostringtag/-/has-tostringtag-1.0.0.tgz#7e133818a7d394734f941e73c3d3f9291e658b25"
examples/Web/web/yarn.lock:4840:  resolved "https://registry.yarnpkg.com/has-tostringtag/-/has-tostringtag-1.0.2.tgz#2cdc42d40bef2e5b4eeab7c01a73c54ce7ab5abc"
examples/Web/web/yarn.lock:4847:  resolved "https://registry.yarnpkg.com/has/-/has-1.0.3.tgz#722d7cbfc1f6aa8241f16dd814e011e1f41e8796"
examples/Web/web/yarn.lock:4854:  resolved "https://registry.yarnpkg.com/hasown/-/hasown-2.0.2.tgz#003eaf91be7adc372e84ec59dc37252cedb80003"
examples/Web/web/yarn.lock:4861:  resolved "https://registry.yarnpkg.com/he/-/he-1.2.0.tgz#84ae65fa7eafb165fddb61566ae14baf05664f0f"
examples/Web/web/yarn.lock:4866:  resolved "https://registry.yarnpkg.com/history/-/history-4.10.1.tgz#33371a65e3a83b267434e2b3f3b1b4c58aad4cf3"
examples/Web/web/yarn.lock:4871:    resolve-pathname "^3.0.0"
examples/Web/web/yarn.lock:4878:  resolved "https://registry.yarnpkg.com/hoist-non-react-statics/-/hoist-non-react-statics-3.3.2.tgz#ece0acaf71d62c2969c2ec59feff42a4b1a85b45"
examples/Web/web/yarn.lock:4885:  resolved "https://registry.yarnpkg.com/hoopy/-/hoopy-0.1.4.tgz#609207d661100033a9a9402ad3dea677381c1b1d"
examples/Web/web/yarn.lock:4890:  resolved "https://registry.yarnpkg.com/hpack.js/-/hpack.js-2.1.6.tgz#87774c0949e513f42e84575b3c45681fade2a0b2"
examples/Web/web/yarn.lock:4900:  resolved "https://registry.yarnpkg.com/html-encoding-sniffer/-/html-encoding-sniffer-2.0.1.tgz#42a6dc4fd33f00281176e8b23759ca4e4fa185f3"
examples/Web/web/yarn.lock:4907:  resolved "https://registry.yarnpkg.com/html-entities/-/html-entities-2.3.2.tgz#760b404685cb1d794e4f4b744332e3b00dcfe488"
examples/Web/web/yarn.lock:4912:  resolved "https://registry.yarnpkg.com/html-escaper/-/html-escaper-2.0.2.tgz#dfd60027da36a36dfcbe236262c00a5822681453"
examples/Web/web/yarn.lock:4917:  resolved "https://registry.yarnpkg.com/html-minifier-terser/-/html-minifier-terser-6.1.0.tgz#bfc818934cc07918f6b3669f5774ecdfd48f32ab"
examples/Web/web/yarn.lock:4930:  resolved "https://registry.yarnpkg.com/html-webpack-plugin/-/html-webpack-plugin-5.5.0.tgz#c3911936f57681c1f9f4d8b68c158cd9dfe52f50"
examples/Web/web/yarn.lock:4941:  resolved "https://registry.yarnpkg.com/htmlparser2/-/htmlparser2-6.1.0.tgz#c4d762b6c3371a05dbe65e94ae43a9f845fb8fb7"
examples/Web/web/yarn.lock:4951:  resolved "https://registry.yarnpkg.com/http-deceiver/-/http-deceiver-1.2.7.tgz#fa7168944ab9a519d337cb0bec7284dc3e723d87"
examples/Web/web/yarn.lock:4956:  resolved "https://registry.yarnpkg.com/http-errors/-/http-errors-2.0.0.tgz#b7774a1486ef73cf7667ac9ae0858c012c57b9d3"
examples/Web/web/yarn.lock:4967:  resolved "https://registry.yarnpkg.com/http-errors/-/http-errors-1.6.3.tgz#8b55680bb4be283a0b5bf4ea2e38580be1d9320d"
examples/Web/web/yarn.lock:4977:  resolved "https://registry.yarnpkg.com/http-parser-js/-/http-parser-js-0.5.5.tgz#d7c30d5d3c90d865b4a2e870181f9d6f22ac7ac5"
examples/Web/web/yarn.lock:4980:http-proxy-agent@^4.0.1:
examples/Web/web/yarn.lock:4982:  resolved "https://registry.yarnpkg.com/http-proxy-agent/-/http-proxy-agent-4.0.1.tgz#8a8c8ef7f5932ccf953c296ca8291b95aa74aa3a"
examples/Web/web/yarn.lock:4989:http-proxy-middleware@^2.0.0:
examples/Web/web/yarn.lock:4991:  resolved "https://registry.yarnpkg.com/http-proxy-middleware/-/http-proxy-middleware-2.0.9.tgz#e9e63d68afaa4eee3d147f39149ab84c0c2815ef"
examples/Web/web/yarn.lock:4994:    "@types/http-proxy" "^1.17.8"
examples/Web/web/yarn.lock:4995:    http-proxy "^1.18.1"
examples/Web/web/yarn.lock:5000:http-proxy@^1.18.1:
examples/Web/web/yarn.lock:5002:  resolved "https://registry.yarnpkg.com/http-proxy/-/http-proxy-1.18.1.tgz#401541f0534884bbf95260334e72f88ee3976549"
examples/Web/web/yarn.lock:5006:    follow-redirects "^1.0.0"
examples/Web/web/yarn.lock:5009:https-proxy-agent@^5.0.0:
examples/Web/web/yarn.lock:5011:  resolved "https://registry.yarnpkg.com/https-proxy-agent/-/https-proxy-agent-5.0.0.tgz#e2a90542abb68a762e0a0850f6c9edadfd8506b2"
examples/Web/web/yarn.lock:5019:  resolved "https://registry.yarnpkg.com/human-signals/-/human-signals-2.1.0.tgz#dc91fcba42e4d06e4abaed33b3e7a3c02f514ea0"
examples/Web/web/yarn.lock:5024:  resolved "https://registry.yarnpkg.com/iconv-lite/-/iconv-lite-0.4.24.tgz#2022b4b25fbddc21d2f524974a474aafe733908b"
examples/Web/web/yarn.lock:5031:  resolved "https://registry.yarnpkg.com/iconv-lite/-/iconv-lite-0.6.3.tgz#a52f80bf38da1952eb5c681790719871a1a72501"
examples/Web/web/yarn.lock:5038:  resolved "https://registry.yarnpkg.com/icss-utils/-/icss-utils-5.1.0.tgz#c6be6858abd013d768e98366ae47e25d5887b1ae"
examples/Web/web/yarn.lock:5043:  resolved "https://registry.yarnpkg.com/idb/-/idb-6.1.5.tgz#dbc53e7adf1ac7c59f9b2bf56e00b4ea4fce8c7b"
examples/Web/web/yarn.lock:5046:identity-obj-proxy@^3.0.0:
examples/Web/web/yarn.lock:5048:  resolved "https://registry.yarnpkg.com/identity-obj-proxy/-/identity-obj-proxy-3.0.0.tgz#94d2bda96084453ef36fbc5aaec37e0f79f1fc14"
examples/Web/web/yarn.lock:5055:  resolved "https://registry.yarnpkg.com/ignore/-/ignore-4.0.6.tgz#750e3db5862087b4737ebac8207ffd1ef27b25fc"
examples/Web/web/yarn.lock:5060:  resolved "https://registry.yarnpkg.com/ignore/-/ignore-5.2.0.tgz#6d3bac8fa7fe0d45d9f9be7bac2fc279577e345a"
examples/Web/web/yarn.lock:5065:  resolved "https://registry.yarnpkg.com/immer/-/immer-9.0.12.tgz#2d33ddf3ee1d247deab9d707ca472c8c942a0f20"
examples/Web/web/yarn.lock:5070:  resolved "https://registry.yarnpkg.com/import-fresh/-/import-fresh-3.3.0.tgz#37162c25fcb9ebaa2e6e53d5b4d88ce17d9e0c2b"
examples/Web/web/yarn.lock:5078:  resolved "https://registry.yarnpkg.com/import-local/-/import-local-3.1.0.tgz#b4479df8a5fd44f6cdce24070675676063c95cb4"
examples/Web/web/yarn.lock:5086:  resolved "https://registry.yarnpkg.com/imurmurhash/-/imurmurhash-0.1.4.tgz#9218b9b2b928a238b13dc4fb6b6d576f231453ea"
examples/Web/web/yarn.lock:5091:  resolved "https://registry.yarnpkg.com/indent-string/-/indent-string-4.0.0.tgz#624f8f4497d619b2d9768531d58f4122854d7251"
examples/Web/web/yarn.lock:5096:  resolved "https://registry.yarnpkg.com/inflight/-/inflight-1.0.6.tgz#49bd6331d7d02d0c09bc910a1075ba8165b56df9"
examples/Web/web/yarn.lock:5104:  resolved "https://registry.yarnpkg.com/inherits/-/inherits-2.0.4.tgz#0fa2c64f932917c3433a0ded55363aae37416b7c"
examples/Web/web/yarn.lock:5109:  resolved "https://registry.yarnpkg.com/inherits/-/inherits-2.0.3.tgz#633c2c83e3da42a502f52466022480f4208261de"
examples/Web/web/yarn.lock:5114:  resolved "https://registry.yarnpkg.com/ini/-/ini-1.3.8.tgz#a29da425b48806f34767a4efce397269af28432c"
examples/Web/web/yarn.lock:5119:  resolved "https://registry.yarnpkg.com/internal-slot/-/internal-slot-1.0.3.tgz#7347e307deeea2faac2ac6205d4bc7d34967f59c"
examples/Web/web/yarn.lock:5128:  resolved "https://registry.yarnpkg.com/ip/-/ip-1.1.9.tgz#8dfbcc99a754d07f425310b86a99546b1151e396"
examples/Web/web/yarn.lock:5133:  resolved "https://registry.yarnpkg.com/ipaddr.js/-/ipaddr.js-1.9.1.tgz#bff38543eeb8984825079ff3a2a8e6cbd46781b3"
examples/Web/web/yarn.lock:5138:  resolved "https://registry.yarnpkg.com/ipaddr.js/-/ipaddr.js-2.0.1.tgz#eca256a7a877e917aeb368b0a7497ddf42ef81c0"
examples/Web/web/yarn.lock:5143:  resolved "https://registry.yarnpkg.com/is-arguments/-/is-arguments-1.1.1.tgz#15b3f88fda01f2a97fec84ca761a560f123efa9b"
examples/Web/web/yarn.lock:5151:  resolved "https://registry.yarnpkg.com/is-arrayish/-/is-arrayish-0.2.1.tgz#77c99840527aa8ecb1a8ba697b80645a7a926a9d"
examples/Web/web/yarn.lock:5156:  resolved "https://registry.yarnpkg.com/is-bigint/-/is-bigint-1.0.4.tgz#08147a1875bc2b32005d41ccd8291dffc6691df3"
examples/Web/web/yarn.lock:5161:is-binary-path@~2.1.0:
examples/Web/web/yarn.lock:5163:  resolved "https://registry.yarnpkg.com/is-binary-path/-/is-binary-path-2.1.0.tgz#ea1f7f3b80f064236e83470f86c09c254fb45b09"
examples/Web/web/yarn.lock:5170:  resolved "https://registry.yarnpkg.com/is-boolean-object/-/is-boolean-object-1.1.2.tgz#5c6dc200246dd9321ae4b885a114bb1f75f63719"
examples/Web/web/yarn.lock:5178:  resolved "https://registry.yarnpkg.com/is-callable/-/is-callable-1.2.4.tgz#47301d58dd0259407865547853df6d61fe471945"
examples/Web/web/yarn.lock:5183:  resolved "https://registry.yarnpkg.com/is-core-module/-/is-core-module-2.8.1.tgz#f59fdfca701d5879d0a6b100a40aa1560ce27211"
examples/Web/web/yarn.lock:5190:  resolved "https://registry.yarnpkg.com/is-date-object/-/is-date-object-1.0.5.tgz#0841d5536e724c25597bf6ea62e1bd38298df31f"
examples/Web/web/yarn.lock:5197:  resolved "https://registry.yarnpkg.com/is-docker/-/is-docker-2.2.1.tgz#33eeabe23cfe86f14bde4408a02c0cfb853acdaa"
examples/Web/web/yarn.lock:5202:  resolved "https://registry.yarnpkg.com/is-extglob/-/is-extglob-2.1.1.tgz#a88c02535791f02ed37c76a1b9ea9773c833f8c2"
examples/Web/web/yarn.lock:5207:  resolved "https://registry.yarnpkg.com/is-fullwidth-code-point/-/is-fullwidth-code-point-3.0.0.tgz#f116f8064fe90b3f7844a38997c0b75051269f1d"
examples/Web/web/yarn.lock:5212:  resolved "https://registry.yarnpkg.com/is-generator-fn/-/is-generator-fn-2.1.0.tgz#7d140adc389aaf3011a8f2a2a4cfa6faadffb118"
examples/Web/web/yarn.lock:5217:  resolved "https://registry.yarnpkg.com/is-glob/-/is-glob-4.0.3.tgz#64f61e42cbbb2eec2071a9dac0b28ba1e65d5084"
examples/Web/web/yarn.lock:5224:  resolved "https://registry.yarnpkg.com/is-module/-/is-module-1.0.0.tgz#3258fb69f78c14d5b815d664336b4cffb6441591"
examples/Web/web/yarn.lock:5229:  resolved "https://registry.yarnpkg.com/is-negative-zero/-/is-negative-zero-2.0.2.tgz#7bf6f03a28003b8b3965de3ac26f664d765f3150"
examples/Web/web/yarn.lock:5234:  resolved "https://registry.yarnpkg.com/is-number-object/-/is-number-object-1.0.6.tgz#6a7aaf838c7f0686a50b4553f7e54a96494e89f0"
examples/Web/web/yarn.lock:5241:  resolved "https://registry.yarnpkg.com/is-number/-/is-number-7.0.0.tgz#7535345b896734d5f80c4d06c50955527a14f12b"
examples/Web/web/yarn.lock:5246:  resolved "https://registry.yarnpkg.com/is-obj/-/is-obj-1.0.1.tgz#3e4729ac1f5fde025cd7d83a896dab9f4f67db0f"
examples/Web/web/yarn.lock:5249:is-path-cwd@^2.2.0:
examples/Web/web/yarn.lock:5251:  resolved "https://registry.yarnpkg.com/is-path-cwd/-/is-path-cwd-2.2.0.tgz#67d43b82664a7b5191fd9119127eb300048a9fdb"
examples/Web/web/yarn.lock:5254:is-path-inside@^3.0.2:
examples/Web/web/yarn.lock:5256:  resolved "https://registry.yarnpkg.com/is-path-inside/-/is-path-inside-3.0.3.tgz#d231362e53a07ff2b0e0ea7fed049161ffd16283"
examples/Web/web/yarn.lock:5261:  resolved "https://registry.yarnpkg.com/is-plain-obj/-/is-plain-obj-3.0.0.tgz#af6f2ea14ac5a646183a5bbdb5baabbc156ad9d7"
examples/Web/web/yarn.lock:5266:  resolved "https://registry.yarnpkg.com/is-potential-custom-element-name/-/is-potential-custom-element-name-1.0.1.tgz#171ed6f19e3ac554394edf78caa05784a45bebb5"
examples/Web/web/yarn.lock:5271:  resolved "https://registry.yarnpkg.com/is-regex/-/is-regex-1.1.4.tgz#eef5663cd59fa4c0ae339505323df6854bb15958"
examples/Web/web/yarn.lock:5279:  resolved "https://registry.yarnpkg.com/is-regexp/-/is-regexp-1.0.0.tgz#fd2d883545c46bac5a633e7b9a09e87fa2cb5069"
examples/Web/web/yarn.lock:5284:  resolved "https://registry.yarnpkg.com/is-root/-/is-root-2.1.0.tgz#809e18129cf1129644302a4f8544035d51984a9c"
examples/Web/web/yarn.lock:5289:  resolved "https://registry.yarnpkg.com/is-shared-array-buffer/-/is-shared-array-buffer-1.0.1.tgz#97b0c85fbdacb59c9c446fe653b82cf2b5b7cfe6"
examples/Web/web/yarn.lock:5294:  resolved "https://registry.yarnpkg.com/is-stream/-/is-stream-2.0.1.tgz#fac1e3d53b97ad5a9d0ae9cef2389f5810a5c077"
examples/Web/web/yarn.lock:5299:  resolved "https://registry.yarnpkg.com/is-string/-/is-string-1.0.7.tgz#0dd12bf2006f255bb58f695110eff7491eebc0fd"
examples/Web/web/yarn.lock:5306:  resolved "https://registry.yarnpkg.com/is-symbol/-/is-symbol-1.0.4.tgz#a6dac93b635b063ca6872236de88910a57af139c"
examples/Web/web/yarn.lock:5313:  resolved "https://registry.yarnpkg.com/is-typedarray/-/is-typedarray-1.0.0.tgz#e479c80858df0c1b11ddda6940f96011fcda4a9a"
examples/Web/web/yarn.lock:5318:  resolved "https://registry.yarnpkg.com/is-weakref/-/is-weakref-1.0.2.tgz#9529f383a9338205e89765e0392efc2f100f06f2"
examples/Web/web/yarn.lock:5325:  resolved "https://registry.yarnpkg.com/is-wsl/-/is-wsl-2.2.0.tgz#74a4c76e77ca9fd3f932f290c17ea326cd157271"
examples/Web/web/yarn.lock:5332:  resolved "https://registry.yarnpkg.com/isarray/-/isarray-0.0.1.tgz#8a18acfca9a8f4177e09abfc6038939b05d1eedf"
examples/Web/web/yarn.lock:5337:  resolved "https://registry.yarnpkg.com/isarray/-/isarray-1.0.0.tgz#bb935d48582cba168c06834957a54a3e07124f11"
examples/Web/web/yarn.lock:5342:  resolved "https://registry.yarnpkg.com/isexe/-/isexe-2.0.0.tgz#e8fbf374dc556ff8947a10dcb0572d633f2cfa10"
examples/Web/web/yarn.lock:5347:  resolved "https://registry.yarnpkg.com/istanbul-lib-coverage/-/istanbul-lib-coverage-3.2.0.tgz#189e7909d0a39fa5a3dfad5b03f71947770191d3"
examples/Web/web/yarn.lock:5352:  resolved "https://registry.yarnpkg.com/istanbul-lib-instrument/-/istanbul-lib-instrument-5.1.0.tgz#7b49198b657b27a730b8e9cb601f1e1bff24c59a"
examples/Web/web/yarn.lock:5363:  resolved "https://registry.yarnpkg.com/istanbul-lib-report/-/istanbul-lib-report-3.0.0.tgz#7518fe52ea44de372f460a76b5ecda9ffb73d8a6"
examples/Web/web/yarn.lock:5372:  resolved "https://registry.yarnpkg.com/istanbul-lib-source-maps/-/istanbul-lib-source-maps-4.0.1.tgz#895f3a709fcfba34c6de5a42939022f3e4358551"
examples/Web/web/yarn.lock:5381:  resolved "https://registry.yarnpkg.com/istanbul-reports/-/istanbul-reports-3.1.3.tgz#4bcae3103b94518117930d51283690960b50d3c2"
examples/Web/web/yarn.lock:5389:  resolved "https://registry.yarnpkg.com/jake/-/jake-10.8.5.tgz#f2183d2c59382cb274226034543b9c03b8164c46"
examples/Web/web/yarn.lock:5399:  resolved "https://registry.yarnpkg.com/jest-changed-files/-/jest-changed-files-27.4.2.tgz#da2547ea47c6e6a5f6ed336151bd2075736eb4a5"
examples/Web/web/yarn.lock:5403:    execa "^5.0.0"
examples/Web/web/yarn.lock:5408:  resolved "https://registry.yarnpkg.com/jest-circus/-/jest-circus-27.4.6.tgz#d3af34c0eb742a967b1919fbb351430727bcea6c"
examples/Web/web/yarn.lock:5433:  resolved "https://registry.yarnpkg.com/jest-cli/-/jest-cli-27.4.7.tgz#d00e759e55d77b3bcfea0715f527c394ca314e5a"
examples/Web/web/yarn.lock:5451:  resolved "https://registry.yarnpkg.com/jest-config/-/jest-config-27.4.7.tgz#4f084b2acbd172c8b43aa4cdffe75d89378d3972"
examples/Web/web/yarn.lock:5479:  resolved "https://registry.yarnpkg.com/jest-diff/-/jest-diff-27.4.6.tgz#93815774d2012a2cbb6cf23f84d48c7a2618f98d"
examples/Web/web/yarn.lock:5489:  resolved "https://registry.yarnpkg.com/jest-docblock/-/jest-docblock-27.4.0.tgz#06c78035ca93cbbb84faf8fce64deae79a59f69f"
examples/Web/web/yarn.lock:5496:  resolved "https://registry.yarnpkg.com/jest-each/-/jest-each-27.4.6.tgz#e7e8561be61d8cc6dbf04296688747ab186c40ff"
examples/Web/web/yarn.lock:5507:  resolved "https://registry.yarnpkg.com/jest-environment-jsdom/-/jest-environment-jsdom-27.4.6.tgz#c23a394eb445b33621dfae9c09e4c8021dea7b36"
examples/Web/web/yarn.lock:5520:  resolved "https://registry.yarnpkg.com/jest-environment-node/-/jest-environment-node-27.4.6.tgz#ee8cd4ef458a0ef09d087c8cd52ca5856df90242"
examples/Web/web/yarn.lock:5532:  resolved "https://registry.yarnpkg.com/jest-get-type/-/jest-get-type-27.4.0.tgz#7503d2663fffa431638337b3998d39c5e928e9b5"
examples/Web/web/yarn.lock:5537:  resolved "https://registry.yarnpkg.com/jest-haste-map/-/jest-haste-map-27.4.6.tgz#c60b5233a34ca0520f325b7e2cc0a0140ad0862a"
examples/Web/web/yarn.lock:5557:  resolved "https://registry.yarnpkg.com/jest-jasmine2/-/jest-jasmine2-27.4.6.tgz#109e8bc036cb455950ae28a018f983f2abe50127"
examples/Web/web/yarn.lock:5580:  resolved "https://registry.yarnpkg.com/jest-leak-detector/-/jest-leak-detector-27.4.6.tgz#ed9bc3ce514b4c582637088d9faf58a33bd59bf4"
examples/Web/web/yarn.lock:5588:  resolved "https://registry.yarnpkg.com/jest-matcher-utils/-/jest-matcher-utils-27.4.6.tgz#53ca7f7b58170638590e946f5363b988775509b8"
examples/Web/web/yarn.lock:5598:  resolved "https://registry.yarnpkg.com/jest-message-util/-/jest-message-util-27.4.6.tgz#9fdde41a33820ded3127465e1a5896061524da31"
examples/Web/web/yarn.lock:5613:  resolved "https://registry.yarnpkg.com/jest-mock/-/jest-mock-27.4.6.tgz#77d1ba87fbd33ccb8ef1f061697e7341b7635195"
examples/Web/web/yarn.lock:5621:  resolved "https://registry.yarnpkg.com/jest-pnp-resolver/-/jest-pnp-resolver-1.2.2.tgz#b704ac0ae028a89108a4d040b3f919dfddc8e33c"
examples/Web/web/yarn.lock:5626:  resolved "https://registry.yarnpkg.com/jest-regex-util/-/jest-regex-util-27.4.0.tgz#e4c45b52653128843d07ad94aec34393ea14fbca"
examples/Web/web/yarn.lock:5631:  resolved "https://registry.yarnpkg.com/jest-resolve-dependencies/-/jest-resolve-dependencies-27.4.6.tgz#fc50ee56a67d2c2183063f6a500cc4042b5e2327"
examples/Web/web/yarn.lock:5640:  resolved "https://registry.yarnpkg.com/jest-resolve/-/jest-resolve-27.4.6.tgz#2ec3110655e86d5bfcfa992e404e22f96b0b5977"
examples/Web/web/yarn.lock:5656:  resolved "https://registry.yarnpkg.com/jest-runner/-/jest-runner-27.4.6.tgz#1d390d276ec417e9b4d0d081783584cbc3e24773"
examples/Web/web/yarn.lock:5684:  resolved "https://registry.yarnpkg.com/jest-runtime/-/jest-runtime-27.4.6.tgz#83ae923818e3ea04463b22f3597f017bb5a1cffa"
examples/Web/web/yarn.lock:5697:    execa "^5.0.0"
examples/Web/web/yarn.lock:5712:  resolved "https://registry.yarnpkg.com/jest-serializer/-/jest-serializer-27.4.0.tgz#34866586e1cae2388b7d12ffa2c7819edef5958a"
examples/Web/web/yarn.lock:5720:  resolved "https://registry.yarnpkg.com/jest-snapshot/-/jest-snapshot-27.4.6.tgz#e2a3b4fff8bdce3033f2373b2e525d8b6871f616"
examples/Web/web/yarn.lock:5748:  resolved "https://registry.yarnpkg.com/jest-util/-/jest-util-27.4.2.tgz#ed95b05b1adfd761e2cda47e0144c6a58e05a621"
examples/Web/web/yarn.lock:5760:  resolved "https://registry.yarnpkg.com/jest-validate/-/jest-validate-27.4.6.tgz#efc000acc4697b6cf4fa68c7f3f324c92d0c4f1f"
examples/Web/web/yarn.lock:5772:  resolved "https://registry.yarnpkg.com/jest-watch-typeahead/-/jest-watch-typeahead-1.0.0.tgz#4de2ca1eb596acb1889752afbab84b74fcd99173"
examples/Web/web/yarn.lock:5785:  resolved "https://registry.yarnpkg.com/jest-watcher/-/jest-watcher-27.4.6.tgz#673679ebeffdd3f94338c24f399b85efc932272d"
examples/Web/web/yarn.lock:5798:  resolved "https://registry.yarnpkg.com/jest-worker/-/jest-worker-26.6.2.tgz#7f72cbc4d643c365e27b9fd775f9d0eaa9c7a8ed"
examples/Web/web/yarn.lock:5807:  resolved "https://registry.yarnpkg.com/jest-worker/-/jest-worker-27.4.6.tgz#5d2d93db419566cb680752ca0792780e71b3273e"
examples/Web/web/yarn.lock:5816:  resolved "https://registry.yarnpkg.com/jest/-/jest-27.4.7.tgz#87f74b9026a1592f2da05b4d258e57505f28eca4"
examples/Web/web/yarn.lock:5825:  resolved "https://registry.yarnpkg.com/jquery/-/jquery-3.6.0.tgz#c72a09f15c1bdce142f49dbf1170bdf8adac2470"
examples/Web/web/yarn.lock:5828:"js-tokens@^3.0.0 || ^4.0.0", js-tokens@^4.0.0:
examples/Web/web/yarn.lock:5830:  resolved "https://registry.yarnpkg.com/js-tokens/-/js-tokens-4.0.0.tgz#19203fb59991df98e3a287050d4647cdeaf32499"
examples/Web/web/yarn.lock:5835:  resolved "https://registry.yarnpkg.com/js-yaml/-/js-yaml-3.14.1.tgz#dae812fdb3825fa306609a8717383c50c36a0537"
examples/Web/web/yarn.lock:5843:  resolved "https://registry.yarnpkg.com/js-yaml/-/js-yaml-4.1.0.tgz#c1fb65f8f5017901cdd2c951864ba18458a10602"
examples/Web/web/yarn.lock:5850:  resolved "https://registry.yarnpkg.com/jsdom/-/jsdom-16.7.0.tgz#918ae71965424b197c819f8183a754e18977b710"
examples/Web/web/yarn.lock:5864:    http-proxy-agent "^4.0.1"
examples/Web/web/yarn.lock:5865:    https-proxy-agent "^5.0.0"
examples/Web/web/yarn.lock:5871:    tough-cookie "^4.0.0"
examples/Web/web/yarn.lock:5883:  resolved "https://registry.yarnpkg.com/jsesc/-/jsesc-2.5.2.tgz#80564d2e483dacf6e8ef209650a67df3f0c283a4"
examples/Web/web/yarn.lock:5888:  resolved "https://registry.yarnpkg.com/jsesc/-/jsesc-0.5.0.tgz#e7dee66e35d6fc16f710fe91d5cf69f70f08911d"
examples/Web/web/yarn.lock:5893:  resolved "https://registry.yarnpkg.com/json-parse-even-better-errors/-/json-parse-even-better-errors-2.3.1.tgz#7c47805a94319928e05777405dc12e1f7a4ee02d"
examples/Web/web/yarn.lock:5898:  resolved "https://registry.yarnpkg.com/json-schema-traverse/-/json-schema-traverse-0.4.1.tgz#69f6a87d9513ab8bb8fe63bdb0979c448e684660"
examples/Web/web/yarn.lock:5903:  resolved "https://registry.yarnpkg.com/json-schema-traverse/-/json-schema-traverse-1.0.0.tgz#ae7bcb3656ab77a73ba5c49bf654f38e6b6860e2"
examples/Web/web/yarn.lock:5908:  resolved "https://registry.yarnpkg.com/json-schema/-/json-schema-0.4.0.tgz#f7de4cf6efab838ebaeb3236474cbba5a1930ab5"
examples/Web/web/yarn.lock:5913:  resolved "https://registry.yarnpkg.com/json-stable-stringify-without-jsonify/-/json-stable-stringify-without-jsonify-1.0.1.tgz#9db7b59496ad3f3cfef30a75142d2d930ad72651"
examples/Web/web/yarn.lock:5918:  resolved "https://registry.yarnpkg.com/json5/-/json5-1.0.2.tgz#63d98d60f21b313b77c4d6da18bfa69d80e1d593"
examples/Web/web/yarn.lock:5925:  resolved "https://registry.yarnpkg.com/json5/-/json5-2.2.0.tgz#2dfefe720c6ba525d9ebd909950f0515316c89a3"
examples/Web/web/yarn.lock:5932:  resolved "https://registry.yarnpkg.com/jsonfile/-/jsonfile-6.1.0.tgz#bc55b2634793c679ec6403094eb13698a6ec0aae"
examples/Web/web/yarn.lock:5941:  resolved "https://registry.yarnpkg.com/jsonpointer/-/jsonpointer-5.0.0.tgz#f802669a524ec4805fa7389eadbc9921d5dc8072"
examples/Web/web/yarn.lock:5946:  resolved "https://registry.yarnpkg.com/jsx-ast-utils/-/jsx-ast-utils-3.2.1.tgz#720b97bfe7d901b927d87c3773637ae8ea48781b"
examples/Web/web/yarn.lock:5954:  resolved "https://registry.yarnpkg.com/keyboard-key/-/keyboard-key-1.1.0.tgz#6f2e8e37fa11475bb1f1d65d5174f1b35653f5b7"
examples/Web/web/yarn.lock:5959:  resolved "https://registry.yarnpkg.com/kind-of/-/kind-of-6.0.3.tgz#07c05034a6c349fa06e24fa35aa76db4580ce4dd"
examples/Web/web/yarn.lock:5964:  resolved "https://registry.yarnpkg.com/kleur/-/kleur-3.0.3.tgz#a79c9ecc86ee1ce3fa6206d1216c501f147fc07e"
examples/Web/web/yarn.lock:5969:  resolved "https://registry.yarnpkg.com/klona/-/klona-2.0.5.tgz#d166574d90076395d9963aa7a928fabb8d76afbc"
examples/Web/web/yarn.lock:5974:  resolved "https://registry.yarnpkg.com/language-subtag-registry/-/language-subtag-registry-0.3.21.tgz#04ac218bea46f04cb039084602c6da9e788dd45a"
examples/Web/web/yarn.lock:5979:  resolved "https://registry.yarnpkg.com/language-tags/-/language-tags-1.0.5.tgz#d321dbc4da30ba8bf3024e040fa5c14661f9193a"
examples/Web/web/yarn.lock:5986:  resolved "https://registry.yarnpkg.com/leven/-/leven-3.1.0.tgz#77891de834064cccba82ae7842bb6b14a13ed7f2"
examples/Web/web/yarn.lock:5991:  resolved "https://registry.yarnpkg.com/levn/-/levn-0.4.1.tgz#ae4562c007473b932a6200d403268dd2fffc6ade"
examples/Web/web/yarn.lock:5999:  resolved "https://registry.yarnpkg.com/levn/-/levn-0.3.0.tgz#3b09924edf9f083c0490fdd4c0bc4421e04764ee"
examples/Web/web/yarn.lock:6007:  resolved "https://registry.yarnpkg.com/lilconfig/-/lilconfig-2.0.4.tgz#f4507d043d7058b380b6a8f5cb7bcd4b34cee082"
examples/Web/web/yarn.lock:6012:  resolved "https://registry.yarnpkg.com/lines-and-columns/-/lines-and-columns-1.2.4.tgz#eca284f75d2965079309dc0ad9255abb2ebc1632"
examples/Web/web/yarn.lock:6017:  resolved "https://registry.yarnpkg.com/loader-runner/-/loader-runner-4.3.1.tgz#6c76ed29b0ccce9af379208299f07f876de737e3"
examples/Web/web/yarn.lock:6022:  resolved "https://registry.yarnpkg.com/loader-utils/-/loader-utils-1.4.2.tgz#29a957f3a63973883eb684f10ffd3d151fec01a3"
examples/Web/web/yarn.lock:6031:  resolved "https://registry.yarnpkg.com/loader-utils/-/loader-utils-2.0.2.tgz#d6e3b4fb81870721ae4e0868ab11dd638368c129"
examples/Web/web/yarn.lock:6040:  resolved "https://registry.yarnpkg.com/loader-utils/-/loader-utils-3.2.0.tgz#bcecc51a7898bee7473d4bc6b845b23af8304d4f"
examples/Web/web/yarn.lock:6043:locate-path@^2.0.0:
examples/Web/web/yarn.lock:6045:  resolved "https://registry.yarnpkg.com/locate-path/-/locate-path-2.0.0.tgz#2b568b265eec944c6d9c0de9c3dbbbca0354cd8e"
examples/Web/web/yarn.lock:6049:    path-exists "^3.0.0"
examples/Web/web/yarn.lock:6051:locate-path@^3.0.0:
examples/Web/web/yarn.lock:6053:  resolved "https://registry.yarnpkg.com/locate-path/-/locate-path-3.0.0.tgz#dbec3b3ab759758071b58fe59fc41871af21400e"
examples/Web/web/yarn.lock:6057:    path-exists "^3.0.0"
examples/Web/web/yarn.lock:6059:locate-path@^5.0.0:
examples/Web/web/yarn.lock:6061:  resolved "https://registry.yarnpkg.com/locate-path/-/locate-path-5.0.0.tgz#1afba396afd676a6d42504d0a67a3a7eb9f62aa0"
examples/Web/web/yarn.lock:6066:locate-path@^6.0.0:
examples/Web/web/yarn.lock:6068:  resolved "https://registry.yarnpkg.com/locate-path/-/locate-path-6.0.0.tgz#55321eb309febbc59c4801d931a72452a681d286"
examples/Web/web/yarn.lock:6075:  resolved "https://registry.yarnpkg.com/lodash-es/-/lodash-es-4.18.1.tgz#b962eeb80d9d983a900bf342961fb7418ca10b1d"
examples/Web/web/yarn.lock:6080:  resolved "https://registry.yarnpkg.com/lodash.debounce/-/lodash.debounce-4.0.8.tgz#82d79bff30a67c4005ffd5e2515300ad9ca4d7af"
examples/Web/web/yarn.lock:6085:  resolved "https://registry.yarnpkg.com/lodash.memoize/-/lodash.memoize-4.1.2.tgz#bcc6c49a42a2840ed997f323eada5ecd182e0bfe"
examples/Web/web/yarn.lock:6090:  resolved "https://registry.yarnpkg.com/lodash.merge/-/lodash.merge-4.6.2.tgz#558aa53b43b661e1925a0afdfa36a9a1085fe57a"
examples/Web/web/yarn.lock:6095:  resolved "https://registry.yarnpkg.com/lodash.sortby/-/lodash.sortby-4.7.0.tgz#edd14c824e2cc9c1e0b0a1b42bb5210516a42438"
examples/Web/web/yarn.lock:6100:  resolved "https://registry.yarnpkg.com/lodash.uniq/-/lodash.uniq-4.5.0.tgz#d0225373aeb652adc1bc82e4945339a842754773"
examples/Web/web/yarn.lock:6105:  resolved "https://registry.yarnpkg.com/lodash/-/lodash-4.18.1.tgz#ff2b66c1f6326d59513de2407bf881439812771c"
examples/Web/web/yarn.lock:6110:  resolved "https://registry.yarnpkg.com/log-symbols/-/log-symbols-3.0.0.tgz#f3a08516a5dea893336a7dee14d18a1cfdab77c4"
examples/Web/web/yarn.lock:6117:  resolved "https://registry.yarnpkg.com/loose-envify/-/loose-envify-1.4.0.tgz#71ee51fa7be4caec1a63839f7e682d8132d30caf"
examples/Web/web/yarn.lock:6120:    js-tokens "^3.0.0 || ^4.0.0"
examples/Web/web/yarn.lock:6124:  resolved "https://registry.yarnpkg.com/lower-case/-/lower-case-2.0.2.tgz#6fa237c63dbdc4a82ca0fd882e4722dc5e634e28"
examples/Web/web/yarn.lock:6131:  resolved "https://registry.yarnpkg.com/lru-cache/-/lru-cache-6.0.0.tgz#6d6fe6570ebd96aaf90fcad1dafa3b2566db3a94"
examples/Web/web/yarn.lock:6138:  resolved "https://registry.yarnpkg.com/magic-string/-/magic-string-0.25.7.tgz#3f497d6fd34c669c6798dcb821f2ef31f5445051"
examples/Web/web/yarn.lock:6145:  resolved "https://registry.yarnpkg.com/make-dir/-/make-dir-3.1.0.tgz#415e967046b3a7f1d185277d84aa58203726a13f"
examples/Web/web/yarn.lock:6152:  resolved "https://registry.yarnpkg.com/makeerror/-/makeerror-1.0.12.tgz#3e5dd2079a82e812e983cc6610c4a2cb0eaa801a"
examples/Web/web/yarn.lock:6159:  resolved "https://registry.yarnpkg.com/math-intrinsics/-/math-intrinsics-1.1.0.tgz#a0dd74be81e2aa5c2f27e65ce283605ee4e2b7f9"
examples/Web/web/yarn.lock:6164:  resolved "https://registry.yarnpkg.com/mdn-data/-/mdn-data-2.0.14.tgz#7113fc4281917d63ce29b43446f701e68c25ba50"
examples/Web/web/yarn.lock:6169:  resolved "https://registry.yarnpkg.com/mdn-data/-/mdn-data-2.0.4.tgz#699b3c38ac6f1d728091a64650b65d388502fd5b"
examples/Web/web/yarn.lock:6174:  resolved "https://registry.yarnpkg.com/media-typer/-/media-typer-0.3.0.tgz#8710d7af0aa626f8fffa1ce00168545263255748"
examples/Web/web/yarn.lock:6179:  resolved "https://registry.yarnpkg.com/memfs/-/memfs-3.4.1.tgz#b78092f466a0dce054d63d39275b24c71d3f1305"
examples/Web/web/yarn.lock:6186:  resolved "https://registry.yarnpkg.com/memfs/-/memfs-3.6.0.tgz#d7a2110f86f79dd950a8b6df6d57bc984aa185f6"
examples/Web/web/yarn.lock:6193:  resolved "https://registry.yarnpkg.com/merge-descriptors/-/merge-descriptors-1.0.3.tgz#d80319a65f3c7935351e5cfdac8f9318504dbed5"
examples/Web/web/yarn.lock:6198:  resolved "https://registry.yarnpkg.com/merge-stream/-/merge-stream-2.0.0.tgz#52823629a14dd00c9770fb6ad47dc6310f2c1f60"
examples/Web/web/yarn.lock:6203:  resolved "https://registry.yarnpkg.com/merge2/-/merge2-1.4.1.tgz#4368892f885e907455a6fd7dc55c0c9d404990ae"
examples/Web/web/yarn.lock:6208:  resolved "https://registry.yarnpkg.com/methods/-/methods-1.1.2.tgz#5529a4d67654134edcc5266656835b0f851afcee"
examples/Web/web/yarn.lock:6213:  resolved "https://registry.yarnpkg.com/micromatch/-/micromatch-4.0.8.tgz#d66fa18f3a47076789320b9b1af32bd86d9fa202"
examples/Web/web/yarn.lock:6221:  resolved "https://registry.yarnpkg.com/mime-db/-/mime-db-1.51.0.tgz#d9ff62451859b18342d960850dc3cfb77e63fb0c"
examples/Web/web/yarn.lock:6226:  resolved "https://registry.yarnpkg.com/mime-db/-/mime-db-1.52.0.tgz#bbabcdc02859f4987301c856e3387ce5ec43bf70"
examples/Web/web/yarn.lock:6231:  resolved "https://registry.yarnpkg.com/mime-types/-/mime-types-2.1.34.tgz#5a712f9ec1503511a945803640fafe09d3793c24"
examples/Web/web/yarn.lock:6238:  resolved "https://registry.yarnpkg.com/mime-types/-/mime-types-2.1.35.tgz#381a871b62a734450660ae3deee44813f70d959a"
examples/Web/web/yarn.lock:6245:  resolved "https://registry.yarnpkg.com/mime/-/mime-1.6.0.tgz#32cd9e5c64553bd58d19a568af452acff04981b1"
examples/Web/web/yarn.lock:6250:  resolved "https://registry.yarnpkg.com/mimic-fn/-/mimic-fn-2.1.0.tgz#7ed2c2ccccaf84d3ffcb7a69b57711fc2083401b"
examples/Web/web/yarn.lock:6255:  resolved "https://registry.yarnpkg.com/mini-create-react-context/-/mini-create-react-context-0.4.1.tgz#072171561bfdc922da08a60c2197a497cc2d1d5e"
examples/Web/web/yarn.lock:6263:  resolved "https://registry.yarnpkg.com/mini-css-extract-plugin/-/mini-css-extract-plugin-2.5.3.tgz#c5c79f9b22ce9b4f164e9492267358dbe35376d9"
examples/Web/web/yarn.lock:6270:  resolved "https://registry.yarnpkg.com/minimalistic-assert/-/minimalistic-assert-1.0.1.tgz#2e194de044626d4a10e7f7fbc00ce73e83e4d5c7"
examples/Web/web/yarn.lock:6275:  resolved "https://registry.yarnpkg.com/minimatch/-/minimatch-3.0.4.tgz#5166e286457f03306064be5497e8dbb0c3d32083"
examples/Web/web/yarn.lock:6282:  resolved "https://registry.yarnpkg.com/minimatch/-/minimatch-3.1.2.tgz#19cd194bfd3e428f049a70817c038d89ab4be35b"
examples/Web/web/yarn.lock:6289:  resolved "https://registry.yarnpkg.com/minimatch/-/minimatch-5.0.1.tgz#fb9022f7528125187c92bd9e9b6366be1cf3415b"
examples/Web/web/yarn.lock:6296:  resolved "https://registry.yarnpkg.com/minimist/-/minimist-1.2.7.tgz#daa1c4d91f507390437c6a8bc01078e7000c4d18"
examples/Web/web/yarn.lock:6301:  resolved "https://registry.yarnpkg.com/mkdirp/-/mkdirp-0.5.5.tgz#d91cefd62d1436ca0f41620e251288d420099def"
examples/Web/web/yarn.lock:6308:  resolved "https://registry.yarnpkg.com/ms/-/ms-2.0.0.tgz#5608aeadfc00be6c2901df5f9861788de0d597c8"
examples/Web/web/yarn.lock:6313:  resolved "https://registry.yarnpkg.com/ms/-/ms-2.1.2.tgz#d09d1f357b443f493382a8eb3ccd183872ae6009"
examples/Web/web/yarn.lock:6318:  resolved "https://registry.yarnpkg.com/ms/-/ms-2.1.3.tgz#574c8138ce1d2b5861f0b44579dbadd60c6615b2"
examples/Web/web/yarn.lock:6323:  resolved "https://registry.yarnpkg.com/multicast-dns-service-types/-/multicast-dns-service-types-1.1.0.tgz#899f11d9686e5e05cb91b35d5f0e63b773cfc901"
examples/Web/web/yarn.lock:6328:  resolved "https://registry.yarnpkg.com/multicast-dns/-/multicast-dns-6.2.3.tgz#a0ec7bd9055c4282f790c3c82f4e28db3b31b229"
examples/Web/web/yarn.lock:6336:  resolved "https://registry.yarnpkg.com/nanoid/-/nanoid-3.3.8.tgz#b1be3030bee36aaff18bacb375e5cce521684baf"
examples/Web/web/yarn.lock:6341:  resolved "https://registry.yarnpkg.com/natural-compare/-/natural-compare-1.4.0.tgz#4abebfeed7541f2c27acfb29bdbbd15c8d5ba4f7"
examples/Web/web/yarn.lock:6346:  resolved "https://registry.yarnpkg.com/negotiator/-/negotiator-0.6.3.tgz#58e323a72fedc0d6f9cd4d31fe49f51479590ccd"
examples/Web/web/yarn.lock:6351:  resolved "https://registry.yarnpkg.com/neo-async/-/neo-async-2.6.2.tgz#b4aafb93e3aeb2d8174ca53cf163ab7d7308305f"
examples/Web/web/yarn.lock:6356:  resolved "https://registry.yarnpkg.com/no-case/-/no-case-3.0.4.tgz#d361fd5c9800f558551a8369fc0dcd4662b6124d"
examples/Web/web/yarn.lock:6364:  resolved "https://registry.yarnpkg.com/node-forge/-/node-forge-1.4.0.tgz#1c7b7d8bdc2d078739f58287d589d903a11b2fc2"
examples/Web/web/yarn.lock:6369:  resolved "https://registry.yarnpkg.com/node-int64/-/node-int64-0.4.0.tgz#87a9065cdb355d3182d8f94ce11188b825c68a3b"
examples/Web/web/yarn.lock:6374:  resolved "https://registry.yarnpkg.com/node-releases/-/node-releases-2.0.1.tgz#3d1d395f204f1f2f29a54358b9fb678765ad2fc5"
examples/Web/web/yarn.lock:6379:  resolved "https://registry.yarnpkg.com/node-releases/-/node-releases-2.0.27.tgz#eedca519205cf20f650f61d56b070db111231e4e"
examples/Web/web/yarn.lock:6382:normalize-path@^3.0.0, normalize-path@~3.0.0:
examples/Web/web/yarn.lock:6384:  resolved "https://registry.yarnpkg.com/normalize-path/-/normalize-path-3.0.0.tgz#0dcd69ff23a1c9b11fd0978316644a0388216a65"
examples/Web/web/yarn.lock:6389:  resolved "https://registry.yarnpkg.com/normalize-range/-/normalize-range-0.1.2.tgz#2d10c06bdfd312ea9777695a4d28439456b75942"
examples/Web/web/yarn.lock:6394:  resolved "https://registry.yarnpkg.com/normalize-url/-/normalize-url-6.1.0.tgz#40d0885b535deffe3f3147bec877d05fe4c5668a"
examples/Web/web/yarn.lock:6397:npm-run-path@^4.0.1:
examples/Web/web/yarn.lock:6399:  resolved "https://registry.yarnpkg.com/npm-run-path/-/npm-run-path-4.0.1.tgz#b7ecd1e5ed53da8e37a55e1c2269e0b97ed748ea"
examples/Web/web/yarn.lock:6402:    path-key "^3.0.0"
examples/Web/web/yarn.lock:6406:  resolved "https://registry.yarnpkg.com/nth-check/-/nth-check-1.0.2.tgz#b2bd295c37e3dd58a3bf0700376663ba4d9cf05c"
examples/Web/web/yarn.lock:6413:  resolved "https://registry.yarnpkg.com/nth-check/-/nth-check-2.0.1.tgz#2efe162f5c3da06a28959fbd3db75dbeea9f0fc2"
examples/Web/web/yarn.lock:6420:  resolved "https://registry.yarnpkg.com/nwsapi/-/nwsapi-2.2.0.tgz#204879a9e3d068ff2a55139c2c772780681a38b7"
examples/Web/web/yarn.lock:6425:  resolved "https://registry.yarnpkg.com/object-assign/-/object-assign-4.1.1.tgz#2109adc7965887cfc05cbbd442cac8bfbb360863"
examples/Web/web/yarn.lock:6430:  resolved "https://registry.yarnpkg.com/object-hash/-/object-hash-2.2.0.tgz#5ad518581eefc443bd763472b8ff2e9c2c0d54a5"
examples/Web/web/yarn.lock:6435:  resolved "https://registry.yarnpkg.com/object-inspect/-/object-inspect-1.12.0.tgz#6e2c120e868fd1fd18cb4f18c31741d0d6e776f0"
examples/Web/web/yarn.lock:6440:  resolved "https://registry.yarnpkg.com/object-inspect/-/object-inspect-1.13.2.tgz#dea0088467fb991e67af4058147a24824a3043ff"
examples/Web/web/yarn.lock:6445:  resolved "https://registry.yarnpkg.com/object-inspect/-/object-inspect-1.12.2.tgz#c0641f26394532f28ab8d796ab954e43c009a8ea"
examples/Web/web/yarn.lock:6450:  resolved "https://registry.yarnpkg.com/object-is/-/object-is-1.1.5.tgz#b9deeaa5fc7f1846a0faecdceec138e5778f53ac"
examples/Web/web/yarn.lock:6458:  resolved "https://registry.yarnpkg.com/object-keys/-/object-keys-1.1.1.tgz#1c47f272df277f3b1daf061677d9c82e2322c60e"
examples/Web/web/yarn.lock:6463:  resolved "https://registry.yarnpkg.com/object.assign/-/object.assign-4.1.2.tgz#0ed54a342eceb37b38ff76eb831a0e788cb63940"
examples/Web/web/yarn.lock:6473:  resolved "https://registry.yarnpkg.com/object.entries/-/object.entries-1.1.5.tgz#e1acdd17c4de2cd96d5a08487cfb9db84d881861"
examples/Web/web/yarn.lock:6482:  resolved "https://registry.yarnpkg.com/object.fromentries/-/object.fromentries-2.0.5.tgz#7b37b205109c21e741e605727fe8b0ad5fa08251"
examples/Web/web/yarn.lock:6491:  resolved "https://registry.yarnpkg.com/object.getownpropertydescriptors/-/object.getownpropertydescriptors-2.1.3.tgz#b223cf38e17fefb97a63c10c91df72ccb386df9e"
examples/Web/web/yarn.lock:6500:  resolved "https://registry.yarnpkg.com/object.hasown/-/object.hasown-1.1.0.tgz#7232ed266f34d197d15cac5880232f7a4790afe5"
examples/Web/web/yarn.lock:6508:  resolved "https://registry.yarnpkg.com/object.values/-/object.values-1.1.5.tgz#959f63e3ce9ef108720333082131e4a459b716ac"
examples/Web/web/yarn.lock:6517:  resolved "https://registry.yarnpkg.com/obuf/-/obuf-1.1.2.tgz#09bea3343d41859ebd446292d11c9d4db619084e"
examples/Web/web/yarn.lock:6522:  resolved "https://registry.yarnpkg.com/on-finished/-/on-finished-2.4.1.tgz#58c8c44116e54845ad57f14ab10b03533184ac3f"
examples/Web/web/yarn.lock:6529:  resolved "https://registry.yarnpkg.com/on-headers/-/on-headers-1.0.2.tgz#772b0ae6aaa525c399e489adfad90c403eb3c28f"
examples/Web/web/yarn.lock:6534:  resolved "https://registry.yarnpkg.com/once/-/once-1.4.0.tgz#583b1aa775961d4b113ac17d9c50baef9dd76bd1"
examples/Web/web/yarn.lock:6541:  resolved "https://registry.yarnpkg.com/onetime/-/onetime-5.1.2.tgz#d0e96ebb56b07476df1dd9c4806e5237985ca45e"
examples/Web/web/yarn.lock:6548:  resolved "https://registry.yarnpkg.com/open/-/open-8.4.0.tgz#345321ae18f8138f82565a910fdc6b39e8c244f8"
examples/Web/web/yarn.lock:6557:  resolved "https://registry.yarnpkg.com/optionator/-/optionator-0.8.3.tgz#84fa1d036fe9d3c7e21d99884b601167ec8fb495"
examples/Web/web/yarn.lock:6569:  resolved "https://registry.yarnpkg.com/optionator/-/optionator-0.9.1.tgz#4f236a6373dae0566a6d43e1326674f50c291499"
examples/Web/web/yarn.lock:6581:  resolved "https://registry.yarnpkg.com/p-limit/-/p-limit-1.3.0.tgz#b86bd5f0c25690911c7590fcbfc2010d54b3ccb8"
examples/Web/web/yarn.lock:6588:  resolved "https://registry.yarnpkg.com/p-limit/-/p-limit-2.3.0.tgz#3dd33c647a214fdfffd835933eb086da0dc21db1"
examples/Web/web/yarn.lock:6595:  resolved "https://registry.yarnpkg.com/p-limit/-/p-limit-3.1.0.tgz#e1daccbe78d0d1388ca18c64fea38e3e57e3706b"
examples/Web/web/yarn.lock:6602:  resolved "https://registry.yarnpkg.com/p-locate/-/p-locate-2.0.0.tgz#20a0103b222a70c8fd39cc2e580680f3dde5ec43"
examples/Web/web/yarn.lock:6609:  resolved "https://registry.yarnpkg.com/p-locate/-/p-locate-3.0.0.tgz#322d69a05c0264b25997d9f40cd8a891ab0064a4"
examples/Web/web/yarn.lock:6616:  resolved "https://registry.yarnpkg.com/p-locate/-/p-locate-4.1.0.tgz#a3428bb7088b3a60292f66919278b7c297ad4f07"
examples/Web/web/yarn.lock:6623:  resolved "https://registry.yarnpkg.com/p-locate/-/p-locate-5.0.0.tgz#83c8315c6785005e3bd021839411c9e110e6d834"
examples/Web/web/yarn.lock:6630:  resolved "https://registry.yarnpkg.com/p-map/-/p-map-4.0.0.tgz#bb2f95a5eda2ec168ec9274e06a747c3e2904d2b"
examples/Web/web/yarn.lock:6637:  resolved "https://registry.yarnpkg.com/p-retry/-/p-retry-4.6.1.tgz#8fcddd5cdf7a67a0911a9cf2ef0e5df7f602316c"
examples/Web/web/yarn.lock:6645:  resolved "https://registry.yarnpkg.com/p-try/-/p-try-1.0.0.tgz#cbc79cdbaf8fd4228e13f621f2b1a237c1b207b3"
examples/Web/web/yarn.lock:6650:  resolved "https://registry.yarnpkg.com/p-try/-/p-try-2.2.0.tgz#cb2868540e313d61de58fafbe35ce9004d5540e6"
examples/Web/web/yarn.lock:6655:  resolved "https://registry.yarnpkg.com/param-case/-/param-case-3.0.4.tgz#7d17fe4aa12bde34d4a77d91acfb6219caad01c5"
examples/Web/web/yarn.lock:6663:  resolved "https://registry.yarnpkg.com/parent-module/-/parent-module-1.0.1.tgz#691d2709e78c79fae3a156622452d00762caaaa2"
examples/Web/web/yarn.lock:6670:  resolved "https://registry.yarnpkg.com/parse-json/-/parse-json-5.2.0.tgz#c76fc66dee54231c962b22bcc8a72cf2f99753cd"
examples/Web/web/yarn.lock:6680:  resolved "https://registry.yarnpkg.com/parse5/-/parse5-6.0.1.tgz#e1a1c085c569b3dc08321184f19a39cc27f7c30b"
examples/Web/web/yarn.lock:6685:  resolved "https://registry.yarnpkg.com/parseurl/-/parseurl-1.3.3.tgz#9da19e7bee8d12dff0513ed5b76957793bc2e8d4"
examples/Web/web/yarn.lock:6690:  resolved "https://registry.yarnpkg.com/pascal-case/-/pascal-case-3.1.2.tgz#b48e0ef2b98e205e7c1dae747d0b1508237660eb"
examples/Web/web/yarn.lock:6696:path-exists@^3.0.0:
examples/Web/web/yarn.lock:6698:  resolved "https://registry.yarnpkg.com/path-exists/-/path-exists-3.0.0.tgz#ce0ebeaa5f78cb18925ea7d810d7b59b010fd515"
examples/Web/web/yarn.lock:6701:path-exists@^4.0.0:
examples/Web/web/yarn.lock:6703:  resolved "https://registry.yarnpkg.com/path-exists/-/path-exists-4.0.0.tgz#513bdbe2d3b95d7762e8c1137efa195c6c61b5b3"
examples/Web/web/yarn.lock:6706:path-is-absolute@^1.0.0:
examples/Web/web/yarn.lock:6708:  resolved "https://registry.yarnpkg.com/path-is-absolute/-/path-is-absolute-1.0.1.tgz#174b9268735534ffbc7ace6bf53a5a9e1b5c5f5f"
examples/Web/web/yarn.lock:6711:path-key@^3.0.0, path-key@^3.1.0:
examples/Web/web/yarn.lock:6713:  resolved "https://registry.yarnpkg.com/path-key/-/path-key-3.1.1.tgz#581f6ade658cbba65a0d3380de7753295054f375"
examples/Web/web/yarn.lock:6716:path-parse@^1.0.6, path-parse@^1.0.7:
examples/Web/web/yarn.lock:6718:  resolved "https://registry.yarnpkg.com/path-parse/-/path-parse-1.0.7.tgz#fbc114b60ca42b30d9daf5858e4bd68bbedb6735"
examples/Web/web/yarn.lock:6721:path-to-regexp@0.1.10:
examples/Web/web/yarn.lock:6723:  resolved "https://registry.yarnpkg.com/path-to-regexp/-/path-to-regexp-0.1.10.tgz#67e9108c5c0551b9e5326064387de4763c4d5f8b"
examples/Web/web/yarn.lock:6726:path-to-regexp@^1.7.0:
examples/Web/web/yarn.lock:6728:  resolved "https://registry.yarnpkg.com/path-to-regexp/-/path-to-regexp-1.8.0.tgz#887b3ba9d84393e87a0a0b9f4cb756198b53548a"
examples/Web/web/yarn.lock:6733:path-type@^4.0.0:
examples/Web/web/yarn.lock:6735:  resolved "https://registry.yarnpkg.com/path-type/-/path-type-4.0.0.tgz#84ed01c0a7ba380afe09d90a8c180dcd9d03043b"
examples/Web/web/yarn.lock:6740:  resolved "https://registry.yarnpkg.com/performance-now/-/performance-now-2.1.0.tgz#6309f4e0e5fa913ec1c69307ae364b4b377c9e7b"
examples/Web/web/yarn.lock:6745:  resolved "https://registry.yarnpkg.com/picocolors/-/picocolors-0.2.1.tgz#570670f793646851d1ba135996962abad587859f"
examples/Web/web/yarn.lock:6750:  resolved "https://registry.yarnpkg.com/picocolors/-/picocolors-1.0.0.tgz#cb5bdc74ff3f51892236eaf79d68bc44564ab81c"
examples/Web/web/yarn.lock:6755:  resolved "https://registry.yarnpkg.com/picocolors/-/picocolors-1.1.1.tgz#3d321af3eab939b083c8f929a1d12cda81c26b6b"
examples/Web/web/yarn.lock:6760:  resolved "https://registry.yarnpkg.com/picomatch/-/picomatch-2.3.2.tgz#5a942915e26b372dc0f0e6753149a16e6b1c5601"
examples/Web/web/yarn.lock:6765:  resolved "https://registry.yarnpkg.com/pirates/-/pirates-4.0.5.tgz#feec352ea5c3268fb23a37c702ab1699f35a5f3b"
examples/Web/web/yarn.lock:6770:  resolved "https://registry.yarnpkg.com/pkg-dir/-/pkg-dir-4.2.0.tgz#f099133df7ede422e81d1d8448270eeb3e4261f3"
examples/Web/web/yarn.lock:6777:  resolved "https://registry.yarnpkg.com/pkg-up/-/pkg-up-3.1.0.tgz#100ec235cc150e4fd42519412596a28512a0def5"
examples/Web/web/yarn.lock:6784:  resolved "https://registry.yarnpkg.com/portfinder/-/portfinder-1.0.28.tgz#67c4622852bd5374dd1dd900f779f53462fac778"
examples/Web/web/yarn.lock:6793:  resolved "https://registry.yarnpkg.com/postcss-attribute-case-insensitive/-/postcss-attribute-case-insensitive-5.0.0.tgz#39cbf6babf3ded1e4abf37d09d6eda21c644105c"
examples/Web/web/yarn.lock:6800:  resolved "https://registry.yarnpkg.com/postcss-browser-comments/-/postcss-browser-comments-4.0.0.tgz#bcfc86134df5807f5d3c0eefa191d42136b5e72a"
examples/Web/web/yarn.lock:6805:  resolved "https://registry.yarnpkg.com/postcss-calc/-/postcss-calc-8.2.3.tgz#53b95ce93de19213c2a5fdd71277a81690ef41d0"
examples/Web/web/yarn.lock:6813:  resolved "https://registry.yarnpkg.com/postcss-clamp/-/postcss-clamp-3.0.0.tgz#09cb1ad64243b46c9159ded5e8d3e8349150a09e"
examples/Web/web/yarn.lock:6820:  resolved "https://registry.yarnpkg.com/postcss-color-functional-notation/-/postcss-color-functional-notation-4.2.1.tgz#a25e9e1855e14d04319222a689f120b3240d39e0"
examples/Web/web/yarn.lock:6827:  resolved "https://registry.yarnpkg.com/postcss-color-hex-alpha/-/postcss-color-hex-alpha-8.0.2.tgz#7a248b006dd47bd83063f662352d31fd982f74ec"
examples/Web/web/yarn.lock:6834:  resolved "https://registry.yarnpkg.com/postcss-color-rebeccapurple/-/postcss-color-rebeccapurple-7.0.2.tgz#5d397039424a58a9ca628762eb0b88a61a66e079"
examples/Web/web/yarn.lock:6841:  resolved "https://registry.yarnpkg.com/postcss-colormin/-/postcss-colormin-5.2.4.tgz#7726d3f3d24f111d39faff50a6500688225d5324"
examples/Web/web/yarn.lock:6851:  resolved "https://registry.yarnpkg.com/postcss-convert-values/-/postcss-convert-values-5.0.3.tgz#492db08a28af84d57651f10edc8f6c8fb2f6df40"
examples/Web/web/yarn.lock:6858:  resolved "https://registry.yarnpkg.com/postcss-custom-media/-/postcss-custom-media-8.0.0.tgz#1be6aff8be7dc9bf1fe014bde3b71b92bb4552f1"
examples/Web/web/yarn.lock:6863:  resolved "https://registry.yarnpkg.com/postcss-custom-properties/-/postcss-custom-properties-12.1.4.tgz#e3d8a8000f28094453b836dff5132385f2862285"
examples/Web/web/yarn.lock:6870:  resolved "https://registry.yarnpkg.com/postcss-custom-selectors/-/postcss-custom-selectors-6.0.0.tgz#022839e41fbf71c47ae6e316cb0e6213012df5ef"
examples/Web/web/yarn.lock:6877:  resolved "https://registry.yarnpkg.com/postcss-dir-pseudo-class/-/postcss-dir-pseudo-class-6.0.3.tgz#febfe305e75267913a53bf5094c7679f5cfa9b55"
examples/Web/web/yarn.lock:6884:  resolved "https://registry.yarnpkg.com/postcss-discard-comments/-/postcss-discard-comments-5.0.2.tgz#811ed34e2b6c40713daab0beb4d7a04125927dcd"
examples/Web/web/yarn.lock:6889:  resolved "https://registry.yarnpkg.com/postcss-discard-duplicates/-/postcss-discard-duplicates-5.0.2.tgz#61076f3d256351bdaac8e20aade730fef0609f44"
examples/Web/web/yarn.lock:6894:  resolved "https://registry.yarnpkg.com/postcss-discard-empty/-/postcss-discard-empty-5.0.2.tgz#0676a9bcfc44bb00d338352a45ab80845a31d8f0"
examples/Web/web/yarn.lock:6899:  resolved "https://registry.yarnpkg.com/postcss-discard-overridden/-/postcss-discard-overridden-5.0.3.tgz#004b9818cabb407e60616509267567150b327a3f"
examples/Web/web/yarn.lock:6904:  resolved "https://registry.yarnpkg.com/postcss-double-position-gradients/-/postcss-double-position-gradients-3.0.4.tgz#2484b9785ef3ba81b0f03a279c52ec58fc5344c2"
examples/Web/web/yarn.lock:6911:  resolved "https://registry.yarnpkg.com/postcss-env-function/-/postcss-env-function-4.0.4.tgz#4e85359ca4fcdde4ec4b73752a41de818dbe91cc"
examples/Web/web/yarn.lock:6918:  resolved "https://registry.yarnpkg.com/postcss-flexbugs-fixes/-/postcss-flexbugs-fixes-5.0.2.tgz#2028e145313074fc9abe276cb7ca14e5401eb49d"
examples/Web/web/yarn.lock:6923:  resolved "https://registry.yarnpkg.com/postcss-focus-visible/-/postcss-focus-visible-6.0.3.tgz#14635b71a6b9140f488f11f26cbc9965a13f6843"
examples/Web/web/yarn.lock:6930:  resolved "https://registry.yarnpkg.com/postcss-focus-within/-/postcss-focus-within-5.0.3.tgz#0b0bf425f14a646bbfd973b463e2d20d85a3a841"
examples/Web/web/yarn.lock:6937:  resolved "https://registry.yarnpkg.com/postcss-font-variant/-/postcss-font-variant-5.0.0.tgz#efd59b4b7ea8bb06127f2d031bfbb7f24d32fa66"
examples/Web/web/yarn.lock:6942:  resolved "https://registry.yarnpkg.com/postcss-gap-properties/-/postcss-gap-properties-3.0.2.tgz#562fbf43a6a721565b3ca0e01008690991d2f726"
examples/Web/web/yarn.lock:6947:  resolved "https://registry.yarnpkg.com/postcss-image-set-function/-/postcss-image-set-function-4.0.5.tgz#8cb3a971507e2c00d5532658af62529c89f0ecc6"
examples/Web/web/yarn.lock:6954:  resolved "https://registry.yarnpkg.com/postcss-initial/-/postcss-initial-4.0.1.tgz#529f735f72c5724a0fb30527df6fb7ac54d7de42"
examples/Web/web/yarn.lock:6959:  resolved "https://registry.yarnpkg.com/postcss-js/-/postcss-js-4.0.0.tgz#31db79889531b80dc7bc9b0ad283e418dce0ac00"
examples/Web/web/yarn.lock:6966:  resolved "https://registry.yarnpkg.com/postcss-lab-function/-/postcss-lab-function-4.0.3.tgz#633745b324afbcd5881da85fe2cef58b17487536"
examples/Web/web/yarn.lock:6973:  resolved "https://registry.yarnpkg.com/postcss-load-config/-/postcss-load-config-3.1.1.tgz#2f53a17f2f543d9e63864460af42efdac0d41f87"
examples/Web/web/yarn.lock:6981:  resolved "https://registry.yarnpkg.com/postcss-loader/-/postcss-loader-6.2.1.tgz#0895f7346b1702103d30fdc66e4d494a93c008ef"
examples/Web/web/yarn.lock:6990:  resolved "https://registry.yarnpkg.com/postcss-logical/-/postcss-logical-5.0.3.tgz#9934e0fb16af70adbd94217b24d2f315ceb5c2f0"
examples/Web/web/yarn.lock:6995:  resolved "https://registry.yarnpkg.com/postcss-media-minmax/-/postcss-media-minmax-5.0.0.tgz#7140bddec173e2d6d657edbd8554a55794e2a5b5"
examples/Web/web/yarn.lock:7000:  resolved "https://registry.yarnpkg.com/postcss-merge-longhand/-/postcss-merge-longhand-5.0.5.tgz#cbc217ca22fb5a3e6ee22a6a1aa6920ec1f3c628"
examples/Web/web/yarn.lock:7008:  resolved "https://registry.yarnpkg.com/postcss-merge-rules/-/postcss-merge-rules-5.0.5.tgz#2a18669ec214019884a60f0a0d356803a8138366"
examples/Web/web/yarn.lock:7018:  resolved "https://registry.yarnpkg.com/postcss-minify-font-values/-/postcss-minify-font-values-5.0.3.tgz#48c455c4cd980ecd07ac9bf3fc58e9d8a2ae4168"
examples/Web/web/yarn.lock:7025:  resolved "https://registry.yarnpkg.com/postcss-minify-gradients/-/postcss-minify-gradients-5.0.5.tgz#a5572b9c98ed52cbd7414db24b873f8b9e418290"
examples/Web/web/yarn.lock:7034:  resolved "https://registry.yarnpkg.com/postcss-minify-params/-/postcss-minify-params-5.0.4.tgz#230a4d04456609e614db1d48c2eebc21f6490a45"
examples/Web/web/yarn.lock:7043:  resolved "https://registry.yarnpkg.com/postcss-minify-selectors/-/postcss-minify-selectors-5.1.2.tgz#bc9698f713b9dab7f44f1ec30643fcbad9a043c0"
examples/Web/web/yarn.lock:7050:  resolved "https://registry.yarnpkg.com/postcss-modules-extract-imports/-/postcss-modules-extract-imports-3.0.0.tgz#cda1f047c0ae80c97dbe28c3e76a43b88025741d"
examples/Web/web/yarn.lock:7055:  resolved "https://registry.yarnpkg.com/postcss-modules-local-by-default/-/postcss-modules-local-by-default-4.0.0.tgz#ebbb54fae1598eecfdf691a02b3ff3b390a5a51c"
examples/Web/web/yarn.lock:7064:  resolved "https://registry.yarnpkg.com/postcss-modules-scope/-/postcss-modules-scope-3.0.0.tgz#9ef3151456d3bbfa120ca44898dfca6f2fa01f06"
examples/Web/web/yarn.lock:7071:  resolved "https://registry.yarnpkg.com/postcss-modules-values/-/postcss-modules-values-4.0.0.tgz#d7c5e7e68c3bb3c9b27cbf48ca0bb3ffb4602c9c"
examples/Web/web/yarn.lock:7078:  resolved "https://registry.yarnpkg.com/postcss-nested/-/postcss-nested-5.0.6.tgz#466343f7fc8d3d46af3e7dba3fcd47d052a945bc"
examples/Web/web/yarn.lock:7085:  resolved "https://registry.yarnpkg.com/postcss-nesting/-/postcss-nesting-10.1.2.tgz#2e5f811b3d75602ea18a95dd445bde5297145141"
examples/Web/web/yarn.lock:7092:  resolved "https://registry.yarnpkg.com/postcss-normalize-charset/-/postcss-normalize-charset-5.0.2.tgz#eb6130c8a8e950ce25f9ea512de1d9d6a6f81439"
examples/Web/web/yarn.lock:7097:  resolved "https://registry.yarnpkg.com/postcss-normalize-display-values/-/postcss-normalize-display-values-5.0.2.tgz#8b5273c6c7d0a445e6ef226b8a5bb3204a55fb99"
examples/Web/web/yarn.lock:7104:  resolved "https://registry.yarnpkg.com/postcss-normalize-positions/-/postcss-normalize-positions-5.0.3.tgz#b63fcc4ff5fbf65934fafaf83270b2da214711d1"
examples/Web/web/yarn.lock:7111:  resolved "https://registry.yarnpkg.com/postcss-normalize-repeat-style/-/postcss-normalize-repeat-style-5.0.3.tgz#488c0ad8aac0fa4f66ef56cc8d604b3fd9bf705f"
examples/Web/web/yarn.lock:7118:  resolved "https://registry.yarnpkg.com/postcss-normalize-string/-/postcss-normalize-string-5.0.3.tgz#49e0a1d58a119d5435ef21893ad03136a6e8f0e6"
examples/Web/web/yarn.lock:7125:  resolved "https://registry.yarnpkg.com/postcss-normalize-timing-functions/-/postcss-normalize-timing-functions-5.0.2.tgz#db4f4f49721f47667afd1fdc5edb032f8d9cdb2e"
examples/Web/web/yarn.lock:7132:  resolved "https://registry.yarnpkg.com/postcss-normalize-unicode/-/postcss-normalize-unicode-5.0.3.tgz#10f0d30093598a58c48a616491cc7fa53256dd43"
examples/Web/web/yarn.lock:7140:  resolved "https://registry.yarnpkg.com/postcss-normalize-url/-/postcss-normalize-url-5.0.4.tgz#3b0322c425e31dd275174d0d5db0e466f50810fb"
examples/Web/web/yarn.lock:7148:  resolved "https://registry.yarnpkg.com/postcss-normalize-whitespace/-/postcss-normalize-whitespace-5.0.3.tgz#fb6bcc9ff2f834448b802657c7acd0956f4591d1"
examples/Web/web/yarn.lock:7155:  resolved "https://registry.yarnpkg.com/postcss-normalize/-/postcss-normalize-10.0.1.tgz#464692676b52792a06b06880a176279216540dd7"
examples/Web/web/yarn.lock:7164:  resolved "https://registry.yarnpkg.com/postcss-opacity-percentage/-/postcss-opacity-percentage-1.1.2.tgz#bd698bb3670a0a27f6d657cc16744b3ebf3b1145"
examples/Web/web/yarn.lock:7169:  resolved "https://registry.yarnpkg.com/postcss-ordered-values/-/postcss-ordered-values-5.0.4.tgz#f799dca87a7f17526d31a20085e61768d0b00534"
examples/Web/web/yarn.lock:7177:  resolved "https://registry.yarnpkg.com/postcss-overflow-shorthand/-/postcss-overflow-shorthand-3.0.2.tgz#b4e9c89728cd1e4918173dfb95936b75f78d4148"
examples/Web/web/yarn.lock:7182:  resolved "https://registry.yarnpkg.com/postcss-page-break/-/postcss-page-break-3.0.4.tgz#7fbf741c233621622b68d435babfb70dd8c1ee5f"
examples/Web/web/yarn.lock:7187:  resolved "https://registry.yarnpkg.com/postcss-place/-/postcss-place-7.0.3.tgz#ca8040dfd937c7769a233a3bd6e66e139cf89e62"
examples/Web/web/yarn.lock:7194:  resolved "https://registry.yarnpkg.com/postcss-preset-env/-/postcss-preset-env-7.3.0.tgz#c745dcfea659fa5a8424bb740fde4ad28e38518e"
examples/Web/web/yarn.lock:7238:  resolved "https://registry.yarnpkg.com/postcss-pseudo-class-any-link/-/postcss-pseudo-class-any-link-7.1.0.tgz#88eb02b9529c5458ffebc68df3760534b6c9fbbf"
examples/Web/web/yarn.lock:7245:  resolved "https://registry.yarnpkg.com/postcss-reduce-initial/-/postcss-reduce-initial-5.0.2.tgz#fa424ce8aa88a89bc0b6d0f94871b24abe94c048"
examples/Web/web/yarn.lock:7253:  resolved "https://registry.yarnpkg.com/postcss-reduce-transforms/-/postcss-reduce-transforms-5.0.3.tgz#df60fab34698a43073e8b87938c71df7a3b040ac"
examples/Web/web/yarn.lock:7260:  resolved "https://registry.yarnpkg.com/postcss-replace-overflow-wrap/-/postcss-replace-overflow-wrap-4.0.0.tgz#d2df6bed10b477bf9c52fab28c568b4b29ca4319"
examples/Web/web/yarn.lock:7265:  resolved "https://registry.yarnpkg.com/postcss-selector-not/-/postcss-selector-not-5.0.0.tgz#ac5fc506f7565dd872f82f5314c0f81a05630dc7"
examples/Web/web/yarn.lock:7272:  resolved "https://registry.yarnpkg.com/postcss-selector-parser/-/postcss-selector-parser-6.0.9.tgz#ee71c3b9ff63d9cd130838876c13a2ec1a992b2f"
examples/Web/web/yarn.lock:7280:  resolved "https://registry.yarnpkg.com/postcss-svgo/-/postcss-svgo-5.0.3.tgz#d945185756e5dfaae07f9edb0d3cae7ff79f9b30"
examples/Web/web/yarn.lock:7288:  resolved "https://registry.yarnpkg.com/postcss-unique-selectors/-/postcss-unique-selectors-5.0.3.tgz#07fd116a8fbd9202e7030f7c4952e7b52c26c63d"
examples/Web/web/yarn.lock:7295:  resolved "https://registry.yarnpkg.com/postcss-value-parser/-/postcss-value-parser-4.2.0.tgz#723c09920836ba6d3e5af019f92bc0971c02e514"
examples/Web/web/yarn.lock:7300:  resolved "https://registry.yarnpkg.com/postcss/-/postcss-7.0.39.tgz#9624375d965630e2e1f2c02a935c82a59cb48309"
examples/Web/web/yarn.lock:7308:  resolved "https://registry.yarnpkg.com/postcss/-/postcss-8.4.6.tgz#c5ff3c3c457a23864f32cb45ac9b741498a09ae1"
examples/Web/web/yarn.lock:7317:  resolved "https://registry.yarnpkg.com/prelude-ls/-/prelude-ls-1.2.1.tgz#debc6489d7a6e6b0e7611888cec880337d316396"
examples/Web/web/yarn.lock:7322:  resolved "https://registry.yarnpkg.com/prelude-ls/-/prelude-ls-1.1.2.tgz#21932a549f5e52ffd9a827f570e04be62a97da54"
examples/Web/web/yarn.lock:7327:  resolved "https://registry.yarnpkg.com/pretty-bytes/-/pretty-bytes-5.6.0.tgz#356256f643804773c82f64723fe78c92c62beaeb"
examples/Web/web/yarn.lock:7332:  resolved "https://registry.yarnpkg.com/pretty-error/-/pretty-error-4.0.0.tgz#90a703f46dd7234adb46d0f84823e9d1cb8f10d6"
examples/Web/web/yarn.lock:7340:  resolved "https://registry.yarnpkg.com/pretty-format/-/pretty-format-27.4.6.tgz#1b784d2f53c68db31797b2348fa39b49e31846b7"
examples/Web/web/yarn.lock:7349:  resolved "https://registry.yarnpkg.com/process-nextick-args/-/process-nextick-args-2.0.1.tgz#7820d9b16120cc55ca9ae7792680ae7dba6d7fe2"
examples/Web/web/yarn.lock:7354:  resolved "https://registry.yarnpkg.com/promise/-/promise-8.1.0.tgz#697c25c3dfe7435dd79fcd58c38a135888eaf05e"
examples/Web/web/yarn.lock:7361:  resolved "https://registry.yarnpkg.com/prompts/-/prompts-2.4.2.tgz#7b57e73b3a48029ad10ebd44f74b01722a4cb069"
examples/Web/web/yarn.lock:7369:  resolved "https://registry.yarnpkg.com/prop-types/-/prop-types-15.8.1.tgz#67d87bf1a694f48435cf332c24af10214a3140b5"
examples/Web/web/yarn.lock:7376:proxy-addr@~2.0.7:
examples/Web/web/yarn.lock:7378:  resolved "https://registry.yarnpkg.com/proxy-addr/-/proxy-addr-2.0.7.tgz#f19fe69ceab311eeb94b42e70e8c2070f9ba1025"
examples/Web/web/yarn.lock:7381:    forwarded "0.2.0"
examples/Web/web/yarn.lock:7384:proxy-from-env@^2.1.0:
examples/Web/web/yarn.lock:7386:  resolved "https://registry.yarnpkg.com/proxy-from-env/-/proxy-from-env-2.1.0.tgz#a7487568adad577cfaaa7e88c49cab3ab3081aba"
examples/Web/web/yarn.lock:7391:  resolved "https://registry.yarnpkg.com/psl/-/psl-1.8.0.tgz#9326f8bcfb013adcc005fdff056acce020e51c24"
examples/Web/web/yarn.lock:7396:  resolved "https://registry.yarnpkg.com/punycode/-/punycode-2.1.1.tgz#b58b010ac40c22c5657616c8d2c2c02c7bf479ec"
examples/Web/web/yarn.lock:7401:  resolved "https://registry.yarnpkg.com/q/-/q-1.5.1.tgz#7e32f75b41381291d04611f1bf14109ac00651d7"
examples/Web/web/yarn.lock:7406:  resolved "https://registry.yarnpkg.com/qs/-/qs-6.13.0.tgz#6ca3bd58439f7e245655798997787b0d88a51906"
examples/Web/web/yarn.lock:7413:  resolved "https://registry.yarnpkg.com/querystringify/-/querystringify-2.2.0.tgz#3345941b4153cb9d082d8eee4cda2016a9aef7f6"
examples/Web/web/yarn.lock:7418:  resolved "https://registry.yarnpkg.com/queue-microtask/-/queue-microtask-1.2.3.tgz#4929228bbc724dfac43e0efb058caf7b6cfb6243"
examples/Web/web/yarn.lock:7423:  resolved "https://registry.yarnpkg.com/quick-lru/-/quick-lru-5.1.1.tgz#366493e6b3e42a3a6885e2e99d18f80fb7a8c932"
examples/Web/web/yarn.lock:7428:  resolved "https://registry.yarnpkg.com/raf/-/raf-3.4.1.tgz#0742e99a4a6552f445d73e3ee0328af0ff1ede39"
examples/Web/web/yarn.lock:7435:  resolved "https://registry.yarnpkg.com/randombytes/-/randombytes-2.1.0.tgz#df6f84372f0270dc65cdf6291349ab7a473d4f2a"
examples/Web/web/yarn.lock:7442:  resolved "https://registry.yarnpkg.com/range-parser/-/range-parser-1.2.1.tgz#3cf37023d199e1c24d1a55b84800c2f3e6468031"
examples/Web/web/yarn.lock:7447:  resolved "https://registry.yarnpkg.com/raw-body/-/raw-body-2.5.2.tgz#99febd83b90e08975087e8f1f9419a149366b68a"
examples/Web/web/yarn.lock:7457:  resolved "https://registry.yarnpkg.com/react-app-polyfill/-/react-app-polyfill-3.0.0.tgz#95221e0a9bd259e5ca6b177c7bb1cb6768f68fd7"
examples/Web/web/yarn.lock:7469:  resolved "https://registry.yarnpkg.com/react-dev-utils/-/react-dev-utils-12.0.0.tgz#4eab12cdb95692a077616770b5988f0adf806526"
examples/Web/web/yarn.lock:7476:    cross-spawn "^7.0.3"
examples/Web/web/yarn.lock:7493:    shell-quote "^1.7.3"
examples/Web/web/yarn.lock:7499:  resolved "https://registry.yarnpkg.com/react-dom/-/react-dom-16.14.0.tgz#7ad838ec29a777fb3c75c3a190f661cf92ab8b89"
examples/Web/web/yarn.lock:7509:  resolved "https://registry.yarnpkg.com/react-error-overlay/-/react-error-overlay-6.0.10.tgz#0fe26db4fa85d9dbb8624729580e90e7159a59a6"
examples/Web/web/yarn.lock:7514:  resolved "https://registry.yarnpkg.com/react-fast-compare/-/react-fast-compare-3.2.0.tgz#641a9da81b6a6320f270e89724fb45a0b39e43bb"
examples/Web/web/yarn.lock:7519:  resolved "https://registry.yarnpkg.com/react-is/-/react-is-16.13.1.tgz#789729a4dc36de2999dc156dd6c1d9c18cea56a4"
examples/Web/web/yarn.lock:7524:  resolved "https://registry.yarnpkg.com/react-is/-/react-is-17.0.2.tgz#e691d4a8e9c789365655539ab372762b0efb54f0"
examples/Web/web/yarn.lock:7529:  resolved "https://registry.yarnpkg.com/react-popper/-/react-popper-2.2.5.tgz#1214ef3cec86330a171671a4fbcbeeb65ee58e96"
examples/Web/web/yarn.lock:7537:  resolved "https://registry.yarnpkg.com/react-refresh/-/react-refresh-0.11.0.tgz#77198b944733f0f1f1a90e791de4541f9f074046"
examples/Web/web/yarn.lock:7542:  resolved "https://registry.yarnpkg.com/react-router-dom/-/react-router-dom-5.3.0.tgz#da1bfb535a0e89a712a93b97dd76f47ad1f32363"
examples/Web/web/yarn.lock:7555:  resolved "https://registry.yarnpkg.com/react-router/-/react-router-5.2.1.tgz#4d2e4e9d5ae9425091845b8dbc6d9d276239774d"
examples/Web/web/yarn.lock:7563:    path-to-regexp "^1.7.0"
examples/Web/web/yarn.lock:7571:  resolved "https://registry.yarnpkg.com/react-scripts/-/react-scripts-5.0.0.tgz#6547a6d7f8b64364ef95273767466cc577cb4b60"
examples/Web/web/yarn.lock:7584:    case-sensitive-paths-webpack-plugin "^2.4.0"
examples/Web/web/yarn.lock:7595:    identity-obj-proxy "^3.0.0"
examples/Web/web/yarn.lock:7626:  resolved "https://registry.yarnpkg.com/react/-/react-16.14.0.tgz#94d776ddd0aaa37da3eda8fc5b6b18a4c9a3114d"
examples/Web/web/yarn.lock:7635:  resolved "https://registry.yarnpkg.com/readable-stream/-/readable-stream-2.3.7.tgz#1eca1cf711aef814c04f62252a36a62f6cb23b57"
examples/Web/web/yarn.lock:7648:  resolved "https://registry.yarnpkg.com/readable-stream/-/readable-stream-3.6.0.tgz#337bbda3adc0706bd3e024426a286d4b4b2c9198"
examples/Web/web/yarn.lock:7657:  resolved "https://registry.yarnpkg.com/readdirp/-/readdirp-3.6.0.tgz#74a370bd857116e245b29cc97340cd431a02a6c7"
examples/Web/web/yarn.lock:7664:  resolved "https://registry.yarnpkg.com/recursive-readdir/-/recursive-readdir-2.2.2.tgz#9946fb3274e1628de6e36b2f6714953b4845094f"
examples/Web/web/yarn.lock:7671:  resolved "https://registry.yarnpkg.com/regenerate-unicode-properties/-/regenerate-unicode-properties-9.0.0.tgz#54d09c7115e1f53dc2314a974b32c1c344efe326"
examples/Web/web/yarn.lock:7678:  resolved "https://registry.yarnpkg.com/regenerate/-/regenerate-1.4.2.tgz#b9346d8827e8f5a32f7ba29637d398b69014848a"
examples/Web/web/yarn.lock:7683:  resolved "https://registry.yarnpkg.com/regenerator-runtime/-/regenerator-runtime-0.13.9.tgz#8925742a98ffd90814988d7566ad30ca3b263b52"
examples/Web/web/yarn.lock:7688:  resolved "https://registry.yarnpkg.com/regenerator-runtime/-/regenerator-runtime-0.14.1.tgz#356ade10263f685dda125100cd862c1db895327f"
examples/Web/web/yarn.lock:7693:  resolved "https://registry.yarnpkg.com/regenerator-transform/-/regenerator-transform-0.14.5.tgz#c98da154683671c9c4dcb16ece736517e1b7feb4"
examples/Web/web/yarn.lock:7700:  resolved "https://registry.yarnpkg.com/regex-parser/-/regex-parser-2.2.11.tgz#3b37ec9049e19479806e878cabe7c1ca83ccfe58"
examples/Web/web/yarn.lock:7705:  resolved "https://registry.yarnpkg.com/regexp.prototype.flags/-/regexp.prototype.flags-1.4.1.tgz#b3f4c0059af9e47eca9f3f660e51d81307e72307"
examples/Web/web/yarn.lock:7713:  resolved "https://registry.yarnpkg.com/regexpp/-/regexpp-3.2.0.tgz#0425a2768d8f23bad70ca4b90461fa2f1213e1b2"
examples/Web/web/yarn.lock:7718:  resolved "https://registry.yarnpkg.com/regexpu-core/-/regexpu-core-4.8.0.tgz#e5605ba361b67b1718478501327502f4479a98f0"
examples/Web/web/yarn.lock:7730:  resolved "https://registry.yarnpkg.com/regjsgen/-/regjsgen-0.5.2.tgz#92ff295fb1deecbf6ecdab2543d207e91aa33733"
examples/Web/web/yarn.lock:7735:  resolved "https://registry.yarnpkg.com/regjsparser/-/regjsparser-0.7.0.tgz#a6b667b54c885e18b52554cb4960ef71187e9968"
examples/Web/web/yarn.lock:7742:  resolved "https://registry.yarnpkg.com/relateurl/-/relateurl-0.2.7.tgz#54dbf377e51440aca90a4cd274600d3ff2d888a9"
examples/Web/web/yarn.lock:7747:  resolved "https://registry.yarnpkg.com/renderkid/-/renderkid-3.0.0.tgz#5fd823e4d6951d37358ecc9a58b1f06836b6268a"
examples/Web/web/yarn.lock:7758:  resolved "https://registry.yarnpkg.com/require-directory/-/require-directory-2.1.1.tgz#8c64ad5fd30dab1c976e2344ffe7f792a6a6df42"
examples/Web/web/yarn.lock:7763:  resolved "https://registry.yarnpkg.com/require-from-string/-/require-from-string-2.0.2.tgz#89a7fdd938261267318eafe14f9c32e598c36909"
examples/Web/web/yarn.lock:7768:  resolved "https://registry.yarnpkg.com/requires-port/-/requires-port-1.0.0.tgz#925d2601d39ac485e091cf0da5c6e694dc3dcaff"
examples/Web/web/yarn.lock:7773:  resolved "https://registry.yarnpkg.com/resolve-cwd/-/resolve-cwd-3.0.0.tgz#0f0075f1bb2544766cf73ba6a6e2adfebcb13f2d"
examples/Web/web/yarn.lock:7780:  resolved "https://registry.yarnpkg.com/resolve-from/-/resolve-from-4.0.0.tgz#4abcd852ad32dd7baabfe9b40e00a36db5f392e6"
examples/Web/web/yarn.lock:7785:  resolved "https://registry.yarnpkg.com/resolve-from/-/resolve-from-5.0.0.tgz#c35225843df8f776df21c57557bc087e9dfdfc69"
examples/Web/web/yarn.lock:7788:resolve-pathname@^3.0.0:
examples/Web/web/yarn.lock:7790:  resolved "https://registry.yarnpkg.com/resolve-pathname/-/resolve-pathname-3.0.0.tgz#99d02224d3cf263689becbb393bc560313025dcd"
examples/Web/web/yarn.lock:7795:  resolved "https://registry.yarnpkg.com/resolve-url-loader/-/resolve-url-loader-4.0.0.tgz#d50d4ddc746bb10468443167acf800dcd6c3ad57"
examples/Web/web/yarn.lock:7806:  resolved "https://registry.yarnpkg.com/resolve.exports/-/resolve.exports-1.1.0.tgz#5ce842b94b05146c0e03076985d1d0e7e48c90c9"
examples/Web/web/yarn.lock:7811:  resolved "https://registry.yarnpkg.com/resolve/-/resolve-1.22.0.tgz#5e0b8c67c15df57a89bdbabe603a002f21731198"
examples/Web/web/yarn.lock:7815:    path-parse "^1.0.7"
examples/Web/web/yarn.lock:7820:  resolved "https://registry.yarnpkg.com/resolve/-/resolve-2.0.0-next.3.tgz#d41016293d4a8586a39ca5d9b5f15cbea1f55e46"
examples/Web/web/yarn.lock:7824:    path-parse "^1.0.6"
examples/Web/web/yarn.lock:7828:  resolved "https://registry.yarnpkg.com/retry/-/retry-0.13.1.tgz#185b1587acf67919d63b357349e03537b2484658"
examples/Web/web/yarn.lock:7833:  resolved "https://registry.yarnpkg.com/reusify/-/reusify-1.0.4.tgz#90da382b1e126efc02146e90845a88db12925d76"
examples/Web/web/yarn.lock:7838:  resolved "https://registry.yarnpkg.com/rimraf/-/rimraf-3.0.2.tgz#f1a5402ba6220ad52cc1282bac1ae3aa49fd061a"
examples/Web/web/yarn.lock:7845:  resolved "https://registry.yarnpkg.com/rollup-plugin-terser/-/rollup-plugin-terser-7.0.2.tgz#e8fbba4869981b2dc35ae7e8a502d5c6c04d324d"
examples/Web/web/yarn.lock:7855:  resolved "https://registry.yarnpkg.com/rollup/-/rollup-2.80.0.tgz#a82efc15b748e986a7c76f0f771221b1fa108a2c"
examples/Web/web/yarn.lock:7862:  resolved "https://registry.yarnpkg.com/run-parallel/-/run-parallel-1.2.0.tgz#66d1368da7bdf921eb9d95bd1a9229e7f21a43ee"
examples/Web/web/yarn.lock:7869:  resolved "https://registry.yarnpkg.com/safe-buffer/-/safe-buffer-5.1.2.tgz#991ec69d296e0313747d59bdfd2b745c35f8828d"
examples/Web/web/yarn.lock:7874:  resolved "https://registry.yarnpkg.com/safe-buffer/-/safe-buffer-5.2.1.tgz#1eaf9fa9bdb1fdd4ec75f58f9cdb4e6b7827eec6"
examples/Web/web/yarn.lock:7879:  resolved "https://registry.yarnpkg.com/safer-buffer/-/safer-buffer-2.1.2.tgz#44fa161b0187b9549dd84bb91802f9bd8385cd6a"
examples/Web/web/yarn.lock:7884:  resolved "https://registry.yarnpkg.com/sanitize.css/-/sanitize.css-13.0.0.tgz#2675553974b27964c75562ade3bd85d79879f173"
examples/Web/web/yarn.lock:7889:  resolved "https://registry.yarnpkg.com/sass-loader/-/sass-loader-12.4.0.tgz#260b0d51a8a373bb8e88efc11f6ba5583fea0bcf"
examples/Web/web/yarn.lock:7897:  resolved "https://registry.yarnpkg.com/sax/-/sax-1.2.4.tgz#2816234e2378bddc4e5354fab5caa895df7100d9"
examples/Web/web/yarn.lock:7902:  resolved "https://registry.yarnpkg.com/saxes/-/saxes-5.0.1.tgz#eebab953fa3b7608dbe94e5dadb15c888fa6696d"
examples/Web/web/yarn.lock:7909:  resolved "https://registry.yarnpkg.com/scheduler/-/scheduler-0.19.1.tgz#4f3e2ed2c1a7d65681f4c854fa8c5a1ccb40f196"
examples/Web/web/yarn.lock:7917:  resolved "https://registry.yarnpkg.com/schema-utils/-/schema-utils-2.7.0.tgz#17151f76d8eae67fbbf77960c33c676ad9f4efc7"
examples/Web/web/yarn.lock:7926:  resolved "https://registry.yarnpkg.com/schema-utils/-/schema-utils-2.7.1.tgz#1ca4f32d1b24c590c203b8e7a50bf0ea4cd394d7"
examples/Web/web/yarn.lock:7935:  resolved "https://registry.yarnpkg.com/schema-utils/-/schema-utils-3.1.1.tgz#bc74c4b6b6995c1d88f76a8b77bea7219e0c8281"
examples/Web/web/yarn.lock:7944:  resolved "https://registry.yarnpkg.com/schema-utils/-/schema-utils-4.0.0.tgz#60331e9e3ae78ec5d16353c467c34b3a0a1d3df7"
examples/Web/web/yarn.lock:7954:  resolved "https://registry.yarnpkg.com/schema-utils/-/schema-utils-4.3.3.tgz#5b1850912fa31df90716963d45d9121fdfc09f46"
examples/Web/web/yarn.lock:7964:  resolved "https://registry.yarnpkg.com/select-hose/-/select-hose-2.0.0.tgz#625d8658f865af43ec962bfc376a37359a4994ca"
examples/Web/web/yarn.lock:7969:  resolved "https://registry.yarnpkg.com/selfsigned/-/selfsigned-2.0.0.tgz#e927cd5377cbb0a1075302cff8df1042cc2bce5b"
examples/Web/web/yarn.lock:7976:  resolved "https://registry.yarnpkg.com/semantic-ui-css/-/semantic-ui-css-2.4.1.tgz#f5aea39fafb787cbd905ec724272a3f9cba9004a"
examples/Web/web/yarn.lock:7983:  resolved "https://registry.yarnpkg.com/semantic-ui-react/-/semantic-ui-react-2.1.1.tgz#88864ff3286ba03fc6e7e94096493b2405699413"
examples/Web/web/yarn.lock:8002:  resolved "https://registry.yarnpkg.com/semver/-/semver-7.0.0.tgz#5f3ca35761e47e05b206c6daff2cf814f0316b8e"
examples/Web/web/yarn.lock:8007:  resolved "https://registry.yarnpkg.com/semver/-/semver-6.3.1.tgz#556d2ef8689146e46dcea4bfdd095f3434dffcb4"
examples/Web/web/yarn.lock:8012:  resolved "https://registry.yarnpkg.com/semver/-/semver-7.5.4.tgz#483986ec4ed38e1c6c48c34894a9182dbff68a6e"
examples/Web/web/yarn.lock:8019:  resolved "https://registry.yarnpkg.com/send/-/send-0.19.0.tgz#bbc5a388c8ea6c048967049dbeac0e4a3f09d7f8"
examples/Web/web/yarn.lock:8038:  resolved "https://registry.yarnpkg.com/serialize-javascript/-/serialize-javascript-4.0.0.tgz#b525e1238489a5ecfc42afacc3fe99e666f4b1aa"
examples/Web/web/yarn.lock:8045:  resolved "https://registry.yarnpkg.com/serialize-javascript/-/serialize-javascript-6.0.0.tgz#efae5d88f45d7924141da8b5c3a7a7e663fefeb8"
examples/Web/web/yarn.lock:8052:  resolved "https://registry.yarnpkg.com/serialize-javascript/-/serialize-javascript-6.0.2.tgz#defa1e055c83bf6d59ea805d8da862254eb6a6c2"
examples/Web/web/yarn.lock:8059:  resolved "https://registry.yarnpkg.com/serve-index/-/serve-index-1.9.1.tgz#d3768d69b1e7d82e5ce050fff5b453bea12a9239"
examples/Web/web/yarn.lock:8072:  resolved "https://registry.yarnpkg.com/serve-static/-/serve-static-1.16.2.tgz#b6a5343da47f6bdd2673848bf45754941e803296"
examples/Web/web/yarn.lock:8082:  resolved "https://registry.yarnpkg.com/set-function-length/-/set-function-length-1.2.2.tgz#aac72314198eaed975cf77b2c3b6b880695e5449"
examples/Web/web/yarn.lock:8094:  resolved "https://registry.yarnpkg.com/setprototypeof/-/setprototypeof-1.1.0.tgz#d0bd85536887b6fe7c0d818cb962d9d91c54e656"
examples/Web/web/yarn.lock:8099:  resolved "https://registry.yarnpkg.com/setprototypeof/-/setprototypeof-1.2.0.tgz#66c9a24a73f9fc28cbe66b09fed3d33dcaf1b424"
examples/Web/web/yarn.lock:8104:  resolved "https://registry.yarnpkg.com/shallowequal/-/shallowequal-1.1.0.tgz#188d521de95b9087404fd4dcb68b13df0ae4e7f8"
examples/Web/web/yarn.lock:8109:  resolved "https://registry.yarnpkg.com/shebang-command/-/shebang-command-2.0.0.tgz#ccd0af4f8835fbdc265b82461aaf0c36663f34ea"
examples/Web/web/yarn.lock:8116:  resolved "https://registry.yarnpkg.com/shebang-regex/-/shebang-regex-3.0.0.tgz#ae16f1644d873ecad843b0307b143362d4c42172"
examples/Web/web/yarn.lock:8119:shell-quote@^1.7.3:
examples/Web/web/yarn.lock:8121:  resolved "https://registry.yarnpkg.com/shell-quote/-/shell-quote-1.7.3.tgz#aa40edac170445b9a431e17bb62c0b881b9c4123"
examples/Web/web/yarn.lock:8126:  resolved "https://registry.yarnpkg.com/side-channel/-/side-channel-1.0.4.tgz#efce5c8fdc104ee751b25c58d4290011fa5ea2cf"
examples/Web/web/yarn.lock:8135:  resolved "https://registry.yarnpkg.com/side-channel/-/side-channel-1.0.6.tgz#abd25fb7cd24baf45466406b1096b7831c9215f2"
examples/Web/web/yarn.lock:8145:  resolved "https://registry.yarnpkg.com/signal-exit/-/signal-exit-3.0.6.tgz#24e630c4b0f03fea446a2bd299e62b4a6ca8d0af"
examples/Web/web/yarn.lock:8150:  resolved "https://registry.yarnpkg.com/signalr/-/signalr-2.4.3.tgz#c619b94b854b3e35b2453a6e727d9dada506c0fc"
examples/Web/web/yarn.lock:8157:  resolved "https://registry.yarnpkg.com/sisteransi/-/sisteransi-1.0.5.tgz#134d681297756437cc05ca01370d3a7a571075ed"
examples/Web/web/yarn.lock:8162:  resolved "https://registry.yarnpkg.com/slash/-/slash-3.0.0.tgz#6539be870c165adbd5240220dbe361f1bc4d4634"
examples/Web/web/yarn.lock:8167:  resolved "https://registry.yarnpkg.com/slash/-/slash-4.0.0.tgz#2422372176c4c6c5addb5e2ada885af984b396a7"
examples/Web/web/yarn.lock:8172:  resolved "https://registry.yarnpkg.com/sockjs/-/sockjs-0.3.24.tgz#c9bc8995f33a111bea0395ec30aa3206bdb5ccce"
examples/Web/web/yarn.lock:8181:  resolved "https://registry.yarnpkg.com/source-list-map/-/source-list-map-2.0.1.tgz#3993bd873bfc48479cca9ea3a547835c7c154b34"
examples/Web/web/yarn.lock:8186:  resolved "https://registry.yarnpkg.com/source-map-js/-/source-map-js-1.0.2.tgz#adbc361d9c62df380125e7f161f71c826f1e490c"
examples/Web/web/yarn.lock:8191:  resolved "https://registry.yarnpkg.com/source-map-loader/-/source-map-loader-3.0.1.tgz#9ae5edc7c2d42570934be4c95d1ccc6352eba52d"
examples/Web/web/yarn.lock:8200:  resolved "https://registry.yarnpkg.com/source-map-support/-/source-map-support-0.5.21.tgz#04fe7c7f9e1ed2d662233c28cb2b35b9f63f6e4f"
examples/Web/web/yarn.lock:8208:  resolved "https://registry.yarnpkg.com/source-map-url/-/source-map-url-0.4.1.tgz#0af66605a745a5a2f91cf1bbf8a7afbc283dec56"
examples/Web/web/yarn.lock:8213:  resolved "https://registry.yarnpkg.com/source-map/-/source-map-0.6.1.tgz#74722af32e9614e9c287a8d0bbde48b5e2f1a263"
examples/Web/web/yarn.lock:8218:  resolved "https://registry.yarnpkg.com/source-map/-/source-map-0.5.7.tgz#8a039d2d1021d22d1ea14c80d8ea468ba2ef3fcc"
examples/Web/web/yarn.lock:8223:  resolved "https://registry.yarnpkg.com/source-map/-/source-map-0.7.3.tgz#5302f8169031735226544092e64981f751750383"
examples/Web/web/yarn.lock:8228:  resolved "https://registry.yarnpkg.com/source-map/-/source-map-0.8.0-beta.0.tgz#d4c1bb42c3f7ee925f005927ba10709e0d1d1f11"
examples/Web/web/yarn.lock:8235:  resolved "https://registry.yarnpkg.com/sourcemap-codec/-/sourcemap-codec-1.4.8.tgz#ea804bd94857402e6992d05a38ef1ae35a9ab4c4"
examples/Web/web/yarn.lock:8240:  resolved "https://registry.yarnpkg.com/spdy-transport/-/spdy-transport-3.0.0.tgz#00d4863a6400ad75df93361a1608605e5dcdcf31"
examples/Web/web/yarn.lock:8252:  resolved "https://registry.yarnpkg.com/spdy/-/spdy-4.0.2.tgz#b74f466203a3eda452c02492b91fb9e84a27677b"
examples/Web/web/yarn.lock:8263:  resolved "https://registry.yarnpkg.com/sprintf-js/-/sprintf-js-1.0.3.tgz#04e6926f662895354f3dd015203633b857297e2c"
examples/Web/web/yarn.lock:8268:  resolved "https://registry.yarnpkg.com/stable/-/stable-0.1.8.tgz#836eb3c8382fe2936feaf544631017ce7d47a3cf"
examples/Web/web/yarn.lock:8273:  resolved "https://registry.yarnpkg.com/stack-utils/-/stack-utils-2.0.5.tgz#d25265fca995154659dbbfba3b49254778d2fdd5"
examples/Web/web/yarn.lock:8280:  resolved "https://registry.yarnpkg.com/stackframe/-/stackframe-1.2.0.tgz#52429492d63c62eb989804c11552e3d22e779303"
examples/Web/web/yarn.lock:8285:  resolved "https://registry.yarnpkg.com/statuses/-/statuses-2.0.1.tgz#55cb000ccf1d48728bd23c685a063998cf1a1b63"
examples/Web/web/yarn.lock:8290:  resolved "https://registry.yarnpkg.com/statuses/-/statuses-1.5.0.tgz#161c7dac177659fd9811f43771fa99381478628c"
examples/Web/web/yarn.lock:8295:  resolved "https://registry.yarnpkg.com/string-length/-/string-length-4.0.2.tgz#a8a8dc7bd5c1a82b9b3c8b87e125f66871b6e57a"
examples/Web/web/yarn.lock:8303:  resolved "https://registry.yarnpkg.com/string-length/-/string-length-5.0.1.tgz#3d647f497b6e8e8d41e422f7e0b23bc536c8381e"
examples/Web/web/yarn.lock:8311:  resolved "https://registry.yarnpkg.com/string-natural-compare/-/string-natural-compare-3.0.1.tgz#7a42d58474454963759e8e8b7ae63d71c1e7fdf4"
examples/Web/web/yarn.lock:8316:  resolved "https://registry.yarnpkg.com/string-width/-/string-width-4.2.3.tgz#269c7117d27b05ad2e536830a8ec895ef9c6d010"
examples/Web/web/yarn.lock:8325:  resolved "https://registry.yarnpkg.com/string.prototype.matchall/-/string.prototype.matchall-4.0.6.tgz#5abb5dabc94c7b0ea2380f65ba610b3a544b15fa"
examples/Web/web/yarn.lock:8339:  resolved "https://registry.yarnpkg.com/string.prototype.trimend/-/string.prototype.trimend-1.0.4.tgz#e75ae90c2942c63504686c18b287b4a0b1a45f80"
examples/Web/web/yarn.lock:8347:  resolved "https://registry.yarnpkg.com/string.prototype.trimstart/-/string.prototype.trimstart-1.0.4.tgz#b36399af4ab2999b4c9c648bd7a3fb2bb26feeed"
examples/Web/web/yarn.lock:8355:  resolved "https://registry.yarnpkg.com/string_decoder/-/string_decoder-1.3.0.tgz#42f114594a46cf1a8e30b0a84f56c78c3edac21e"
examples/Web/web/yarn.lock:8362:  resolved "https://registry.yarnpkg.com/string_decoder/-/string_decoder-1.1.1.tgz#9cf1611ba62685d7030ae9e4ba34149c3af03fc8"
examples/Web/web/yarn.lock:8369:  resolved "https://registry.yarnpkg.com/stringify-object/-/stringify-object-3.3.0.tgz#703065aefca19300d3ce88af4f5b3956d7556629"
examples/Web/web/yarn.lock:8378:  resolved "https://registry.yarnpkg.com/strip-ansi/-/strip-ansi-6.0.1.tgz#9e26c63d30f53443e9489495b2105d37b67a85d9"
examples/Web/web/yarn.lock:8385:  resolved "https://registry.yarnpkg.com/strip-ansi/-/strip-ansi-7.0.1.tgz#61740a08ce36b61e50e65653f07060d000975fb2"
examples/Web/web/yarn.lock:8392:  resolved "https://registry.yarnpkg.com/strip-bom/-/strip-bom-3.0.0.tgz#2334c18e9c759f7bdd56fdef7e9ae3d588e68ed3"
examples/Web/web/yarn.lock:8397:  resolved "https://registry.yarnpkg.com/strip-bom/-/strip-bom-4.0.0.tgz#9c3505c1db45bcedca3d9cf7a16f5c5aa3901878"
examples/Web/web/yarn.lock:8402:  resolved "https://registry.yarnpkg.com/strip-comments/-/strip-comments-2.0.1.tgz#4ad11c3fbcac177a67a40ac224ca339ca1c1ba9b"
examples/Web/web/yarn.lock:8407:  resolved "https://registry.yarnpkg.com/strip-final-newline/-/strip-final-newline-2.0.0.tgz#89b852fb2fcbe936f6f4b3187afb0a12c1ab58ad"
examples/Web/web/yarn.lock:8412:  resolved "https://registry.yarnpkg.com/strip-json-comments/-/strip-json-comments-3.1.1.tgz#31f1281b3832630434831c310c01cccda8cbe006"
examples/Web/web/yarn.lock:8417:  resolved "https://registry.yarnpkg.com/style-loader/-/style-loader-3.3.1.tgz#057dfa6b3d4d7c7064462830f9113ed417d38575"
examples/Web/web/yarn.lock:8422:  resolved "https://registry.yarnpkg.com/stylehacks/-/stylehacks-5.0.2.tgz#fa10e5181c6e8dc0bddb4a3fb372e9ac42bba2ad"
examples/Web/web/yarn.lock:8430:  resolved "https://registry.yarnpkg.com/supports-color/-/supports-color-5.5.0.tgz#e2e69a44ac8772f78a1ec0b35b689df6530efc8f"
examples/Web/web/yarn.lock:8437:  resolved "https://registry.yarnpkg.com/supports-color/-/supports-color-7.2.0.tgz#1b7dcdcb32b8138801b3e478ba6a51caa89648da"
examples/Web/web/yarn.lock:8444:  resolved "https://registry.yarnpkg.com/supports-color/-/supports-color-8.1.1.tgz#cd6fc17e28500cff56c1b86c0a7fd4a54a73005c"
examples/Web/web/yarn.lock:8451:  resolved "https://registry.yarnpkg.com/supports-hyperlinks/-/supports-hyperlinks-2.2.0.tgz#4f77b42488765891774b70c79babd87f9bd594bb"
examples/Web/web/yarn.lock:8459:  resolved "https://registry.yarnpkg.com/supports-preserve-symlinks-flag/-/supports-preserve-symlinks-flag-1.0.0.tgz#6eda4bd344a3c94aea376d4cc31bc77311039e09"
examples/Web/web/yarn.lock:8464:  resolved "https://registry.yarnpkg.com/svg-parser/-/svg-parser-2.0.4.tgz#fdc2e29e13951736140b76cb122c8ee6630eb6b5"
examples/Web/web/yarn.lock:8469:  resolved "https://registry.yarnpkg.com/svgo/-/svgo-1.3.2.tgz#b6dc511c063346c9e415b81e43401145b96d4167"
examples/Web/web/yarn.lock:8488:  resolved "https://registry.yarnpkg.com/svgo/-/svgo-2.8.0.tgz#4ff80cce6710dc2795f0c7c74101e6764cfccd24"
examples/Web/web/yarn.lock:8501:  resolved "https://registry.yarnpkg.com/symbol-tree/-/symbol-tree-3.2.4.tgz#430637d248ba77e078883951fb9aa0eed7c63fa2"
examples/Web/web/yarn.lock:8506:  resolved "https://registry.yarnpkg.com/tailwindcss/-/tailwindcss-3.0.18.tgz#ea4825e6496d77dc21877b6b61c7cc56cda3add5"
examples/Web/web/yarn.lock:8520:    normalize-path "^3.0.0"
examples/Web/web/yarn.lock:8532:  resolved "https://registry.yarnpkg.com/tapable/-/tapable-1.1.3.tgz#a1fccc06b58db61fd7a45da2da44f5f3a3e67ba2"
examples/Web/web/yarn.lock:8537:  resolved "https://registry.yarnpkg.com/tapable/-/tapable-2.2.1.tgz#1967a73ef4060a82f12ab96af86d52fdb76eeca0"
examples/Web/web/yarn.lock:8542:  resolved "https://registry.yarnpkg.com/tapable/-/tapable-2.3.0.tgz#7e3ea6d5ca31ba8e078b560f0d83ce9a14aa8be6"
examples/Web/web/yarn.lock:8547:  resolved "https://registry.yarnpkg.com/temp-dir/-/temp-dir-2.0.0.tgz#bde92b05bdfeb1516e804c9c00ad45177f31321e"
examples/Web/web/yarn.lock:8552:  resolved "https://registry.yarnpkg.com/tempy/-/tempy-0.6.0.tgz#65e2c35abc06f1124a97f387b08303442bde59f3"
examples/Web/web/yarn.lock:8562:  resolved "https://registry.yarnpkg.com/terminal-link/-/terminal-link-2.1.1.tgz#14a64a27ab3c0df933ea546fba55f2d078edc994"
examples/Web/web/yarn.lock:8570:  resolved "https://registry.yarnpkg.com/terser-webpack-plugin/-/terser-webpack-plugin-5.3.1.tgz#0320dcc270ad5372c1e8993fabbd927929773e54"
examples/Web/web/yarn.lock:8581:  resolved "https://registry.yarnpkg.com/terser-webpack-plugin/-/terser-webpack-plugin-5.3.16.tgz#741e448cc3f93d8026ebe4f7ef9e4afacfd56330"
examples/Web/web/yarn.lock:8592:  resolved "https://registry.yarnpkg.com/terser/-/terser-5.14.2.tgz#9ac9f22b06994d736174f4091aa368db896f1c10"
examples/Web/web/yarn.lock:8602:  resolved "https://registry.yarnpkg.com/terser/-/terser-5.46.0.tgz#1b81e560d584bbdd74a8ede87b4d9477b0ff9695"
examples/Web/web/yarn.lock:8612:  resolved "https://registry.yarnpkg.com/test-exclude/-/test-exclude-6.0.0.tgz#04a8698661d805ea6fa293b6cb9e63ac044ef15e"
examples/Web/web/yarn.lock:8621:  resolved "https://registry.yarnpkg.com/text-table/-/text-table-0.2.0.tgz#7f5ee823ae805207c00af2df4a84ec3fcfa570b4"
examples/Web/web/yarn.lock:8626:  resolved "https://registry.yarnpkg.com/throat/-/throat-6.0.1.tgz#d514fedad95740c12c2d7fc70ea863eb51ade375"
examples/Web/web/yarn.lock:8631:  resolved "https://registry.yarnpkg.com/thunky/-/thunky-1.1.0.tgz#5abaf714a9405db0504732bbccd2cedd9ef9537d"
examples/Web/web/yarn.lock:8636:  resolved "https://registry.yarnpkg.com/timsort/-/timsort-0.3.0.tgz#405411a8e7e6339fe64db9a234de11dc31e02bd4"
examples/Web/web/yarn.lock:8641:  resolved "https://registry.yarnpkg.com/tiny-invariant/-/tiny-invariant-1.2.0.tgz#a1141f86b672a9148c72e978a19a73b9b94a15a9"
examples/Web/web/yarn.lock:8646:  resolved "https://registry.yarnpkg.com/tiny-warning/-/tiny-warning-1.0.3.tgz#94a30db453df4c643d0fd566060d60a875d84754"
examples/Web/web/yarn.lock:8651:  resolved "https://registry.yarnpkg.com/tmpl/-/tmpl-1.0.5.tgz#8683e0b902bb9c20c4f726e3c0b69f36518c07cc"
examples/Web/web/yarn.lock:8656:  resolved "https://registry.yarnpkg.com/to-fast-properties/-/to-fast-properties-2.0.0.tgz#dc5e698cbd079265bc73e0377681a4e4e83f616e"
examples/Web/web/yarn.lock:8661:  resolved "https://registry.yarnpkg.com/to-regex-range/-/to-regex-range-5.0.1.tgz#1648c44aae7c8d988a326018ed72f5b4dd0392e4"
examples/Web/web/yarn.lock:8668:  resolved "https://registry.yarnpkg.com/toidentifier/-/toidentifier-1.0.1.tgz#3be34321a88a820ed1bd80dfaa33e479fbb8dd35"
examples/Web/web/yarn.lock:8671:tough-cookie@^4.0.0:
examples/Web/web/yarn.lock:8673:  resolved "https://registry.yarnpkg.com/tough-cookie/-/tough-cookie-4.1.3.tgz#97b9adb0728b42280aa3d814b6b999b2ff0318bf"
examples/Web/web/yarn.lock:8683:  resolved "https://registry.yarnpkg.com/tr46/-/tr46-1.0.1.tgz#a8b13fd6bfd2489519674ccde55ba3693b706d09"
examples/Web/web/yarn.lock:8690:  resolved "https://registry.yarnpkg.com/tr46/-/tr46-2.1.0.tgz#fa87aa81ca5d5941da8cbf1f9b749dc969a4e240"
examples/Web/web/yarn.lock:8697:  resolved "https://registry.yarnpkg.com/tryer/-/tryer-1.0.1.tgz#f2c85406800b9b0f74c9f7465b81eaad241252f8"
examples/Web/web/yarn.lock:8700:tsconfig-paths@^3.12.0:
examples/Web/web/yarn.lock:8702:  resolved "https://registry.yarnpkg.com/tsconfig-paths/-/tsconfig-paths-3.12.0.tgz#19769aca6ee8f6a1a341e38c8fa45dd9fb18899b"
examples/Web/web/yarn.lock:8712:  resolved "https://registry.yarnpkg.com/tslib/-/tslib-1.14.1.tgz#cf2d38bdc34a134bcaf1091c41f6619e2f672d00"
examples/Web/web/yarn.lock:8717:  resolved "https://registry.yarnpkg.com/tslib/-/tslib-2.3.1.tgz#e8a335add5ceae51aa261d32a490158ef042ef01"
examples/Web/web/yarn.lock:8722:  resolved "https://registry.yarnpkg.com/tsutils/-/tsutils-3.21.0.tgz#b48717d394cea6c1e096983eed58e9d61715b623"
examples/Web/web/yarn.lock:8729:  resolved "https://registry.yarnpkg.com/type-check/-/type-check-0.4.0.tgz#07b8203bfa7056c0657050e3ccd2c37730bab8f1"
examples/Web/web/yarn.lock:8736:  resolved "https://registry.yarnpkg.com/type-check/-/type-check-0.3.2.tgz#5884cab512cf1d355e3fb784f30804b2b520db72"
examples/Web/web/yarn.lock:8743:  resolved "https://registry.yarnpkg.com/type-detect/-/type-detect-4.0.8.tgz#7646fb5f18871cfbb7749e69bd39a6388eb7450c"
examples/Web/web/yarn.lock:8748:  resolved "https://registry.yarnpkg.com/type-fest/-/type-fest-0.16.0.tgz#3240b891a78b0deae910dbeb86553e552a148860"
examples/Web/web/yarn.lock:8753:  resolved "https://registry.yarnpkg.com/type-fest/-/type-fest-0.20.2.tgz#1bf207f4b28f91583666cb5fbd327887301cd5f4"
examples/Web/web/yarn.lock:8758:  resolved "https://registry.yarnpkg.com/type-fest/-/type-fest-0.21.3.tgz#d260a24b0198436e133fa26a524a6d65fa3b2e37"
examples/Web/web/yarn.lock:8763:  resolved "https://registry.yarnpkg.com/type-is/-/type-is-1.6.18.tgz#4e552cd05df09467dcbc4ef739de89f2cf37c131"
examples/Web/web/yarn.lock:8771:  resolved "https://registry.yarnpkg.com/typedarray-to-buffer/-/typedarray-to-buffer-3.1.5.tgz#a97ee7a9ff42691b9f783ff1bc5112fe3fca9080"
examples/Web/web/yarn.lock:8778:  resolved "https://registry.yarnpkg.com/unbox-primitive/-/unbox-primitive-1.0.1.tgz#085e215625ec3162574dc8859abee78a59b14471"
examples/Web/web/yarn.lock:8788:  resolved "https://registry.yarnpkg.com/unicode-canonical-property-names-ecmascript/-/unicode-canonical-property-names-ecmascript-2.0.0.tgz#301acdc525631670d39f6146e0e77ff6bbdebddc"
examples/Web/web/yarn.lock:8793:  resolved "https://registry.yarnpkg.com/unicode-match-property-ecmascript/-/unicode-match-property-ecmascript-2.0.0.tgz#54fd16e0ecb167cf04cf1f756bdcc92eba7976c3"
examples/Web/web/yarn.lock:8801:  resolved "https://registry.yarnpkg.com/unicode-match-property-value-ecmascript/-/unicode-match-property-value-ecmascript-2.0.0.tgz#1a01aa57247c14c568b89775a54938788189a714"
examples/Web/web/yarn.lock:8806:  resolved "https://registry.yarnpkg.com/unicode-property-aliases-ecmascript/-/unicode-property-aliases-ecmascript-2.0.0.tgz#0a36cb9a585c4f6abd51ad1deddb285c165297c8"
examples/Web/web/yarn.lock:8811:  resolved "https://registry.yarnpkg.com/unique-string/-/unique-string-2.0.0.tgz#39c6451f81afb2749de2b233e3f7c5e8843bd89d"
examples/Web/web/yarn.lock:8818:  resolved "https://registry.yarnpkg.com/universalify/-/universalify-0.2.0.tgz#6451760566fa857534745ab1dde952d1b1761be0"
examples/Web/web/yarn.lock:8823:  resolved "https://registry.yarnpkg.com/universalify/-/universalify-2.0.0.tgz#75a4984efedc4b08975c5aeb73f530d02df25717"
examples/Web/web/yarn.lock:8828:  resolved "https://registry.yarnpkg.com/unpipe/-/unpipe-1.0.0.tgz#b2bf4ee8514aae6165b4817829d21b2ef49904ec"
examples/Web/web/yarn.lock:8833:  resolved "https://registry.yarnpkg.com/unquote/-/unquote-1.1.1.tgz#8fded7324ec6e88a0ff8b905e7c098cdc086d544"
examples/Web/web/yarn.lock:8836:upath@^1.2.0:
examples/Web/web/yarn.lock:8838:  resolved "https://registry.yarnpkg.com/upath/-/upath-1.2.0.tgz#8f66dbcd55a883acdae4408af8b035a5044c1894"
examples/Web/web/yarn.lock:8843:  resolved "https://registry.yarnpkg.com/update-browserslist-db/-/update-browserslist-db-1.2.3.tgz#64d76db58713136acbeb4c49114366cc6cc2e80d"
examples/Web/web/yarn.lock:8851:  resolved "https://registry.yarnpkg.com/uri-js/-/uri-js-4.4.1.tgz#9b1a52595225859e55f669d928f88c6c57f2a77e"
examples/Web/web/yarn.lock:8858:  resolved "https://registry.yarnpkg.com/url-parse/-/url-parse-1.5.10.tgz#9d3c2f736c1d75dd3bd2be507dcc111f1e2ea9c1"
examples/Web/web/yarn.lock:8866:  resolved "https://registry.yarnpkg.com/util-deprecate/-/util-deprecate-1.0.2.tgz#450d4dc9fa70de732762fbd2d4a28981419a0ccf"
examples/Web/web/yarn.lock:8871:  resolved "https://registry.yarnpkg.com/util.promisify/-/util.promisify-1.0.1.tgz#6baf7774b80eeb0f7520d8b81d07982a59abbaee"
examples/Web/web/yarn.lock:8881:  resolved "https://registry.yarnpkg.com/utila/-/utila-0.4.0.tgz#8a16a05d445657a3aea5eecc5b12a4fa5379772c"
examples/Web/web/yarn.lock:8886:  resolved "https://registry.yarnpkg.com/utils-merge/-/utils-merge-1.0.1.tgz#9f95710f50a267947b2ccc124741c1028427e713"
examples/Web/web/yarn.lock:8891:  resolved "https://registry.yarnpkg.com/uuid/-/uuid-14.0.0.tgz#0af883220163d264ffe0c084f6b8a89b9666966d"
examples/Web/web/yarn.lock:8896:  resolved "https://registry.yarnpkg.com/uuid/-/uuid-8.3.2.tgz#80d5b5ced271bb9af6c445f21a1a04c606cefbe2"
examples/Web/web/yarn.lock:8901:  resolved "https://registry.yarnpkg.com/v8-compile-cache/-/v8-compile-cache-2.3.0.tgz#2de19618c66dc247dcfb6f99338035d8245a2cee"
examples/Web/web/yarn.lock:8906:  resolved "https://registry.yarnpkg.com/v8-to-istanbul/-/v8-to-istanbul-8.1.1.tgz#77b752fd3975e31bbcef938f85e9bd1c7a8d60ed"
examples/Web/web/yarn.lock:8915:  resolved "https://registry.yarnpkg.com/value-equal/-/value-equal-1.0.1.tgz#1e0b794c734c5c0cade179c437d356d931a34d6c"
examples/Web/web/yarn.lock:8920:  resolved "https://registry.yarnpkg.com/vary/-/vary-1.1.2.tgz#2299f02c6ded30d4a5961b0b9f74524a18f634fc"
examples/Web/web/yarn.lock:8925:  resolved "https://registry.yarnpkg.com/w3c-hr-time/-/w3c-hr-time-1.0.2.tgz#0a89cdf5cc15822df9c360543676963e0cc308cd"
examples/Web/web/yarn.lock:8932:  resolved "https://registry.yarnpkg.com/w3c-xmlserializer/-/w3c-xmlserializer-2.0.0.tgz#3e7104a05b75146cc60f564380b7f683acf1020a"
examples/Web/web/yarn.lock:8939:  resolved "https://registry.yarnpkg.com/walker/-/walker-1.0.8.tgz#bd498db477afe573dc04185f011d3ab8a8d7653f"
examples/Web/web/yarn.lock:8946:  resolved "https://registry.yarnpkg.com/warning/-/warning-4.0.3.tgz#16e9e077eb8a86d6af7d64aa1e05fd85b4678ca3"
examples/Web/web/yarn.lock:8953:  resolved "https://registry.yarnpkg.com/watchpack/-/watchpack-2.5.1.tgz#dd38b601f669e0cbf567cb802e75cead82cde102"
examples/Web/web/yarn.lock:8961:  resolved "https://registry.yarnpkg.com/wbuf/-/wbuf-1.7.3.tgz#c1d8d149316d3ea852848895cb6a0bfe887b87df"
examples/Web/web/yarn.lock:8968:  resolved "https://registry.yarnpkg.com/webidl-conversions/-/webidl-conversions-4.0.2.tgz#a855980b1f0b6b359ba1d5d9fb39ae941faa63ad"
examples/Web/web/yarn.lock:8973:  resolved "https://registry.yarnpkg.com/webidl-conversions/-/webidl-conversions-5.0.0.tgz#ae59c8a00b121543a2acc65c0434f57b0fc11aff"
examples/Web/web/yarn.lock:8978:  resolved "https://registry.yarnpkg.com/webidl-conversions/-/webidl-conversions-6.1.0.tgz#9111b4d7ea80acd40f5270d666621afa78b69514"
examples/Web/web/yarn.lock:8983:  resolved "https://registry.yarnpkg.com/webpack-dev-middleware/-/webpack-dev-middleware-5.3.4.tgz#eb7b39281cbce10e104eb2b8bf2b63fce49a3517"
examples/Web/web/yarn.lock:8994:  resolved "https://registry.yarnpkg.com/webpack-dev-server/-/webpack-dev-server-4.7.3.tgz#4e995b141ff51fa499906eebc7906f6925d0beaa"
examples/Web/web/yarn.lock:9013:    http-proxy-middleware "^2.0.0"
examples/Web/web/yarn.lock:9029:  resolved "https://registry.yarnpkg.com/webpack-manifest-plugin/-/webpack-manifest-plugin-4.1.1.tgz#10f8dbf4714ff93a215d5a45bcc416d80506f94f"
examples/Web/web/yarn.lock:9037:  resolved "https://registry.yarnpkg.com/webpack-sources/-/webpack-sources-1.4.3.tgz#eedd8ec0b928fbf1cbfe994e22d2d890f330a933"
examples/Web/web/yarn.lock:9045:  resolved "https://registry.yarnpkg.com/webpack-sources/-/webpack-sources-2.3.1.tgz#570de0af163949fe272233c2cefe1b56f74511fd"
examples/Web/web/yarn.lock:9053:  resolved "https://registry.yarnpkg.com/webpack-sources/-/webpack-sources-3.3.3.tgz#d4bf7f9909675d7a070ff14d0ef2a4f3c982c723"
examples/Web/web/yarn.lock:9058:  resolved "https://registry.yarnpkg.com/webpack/-/webpack-5.105.0.tgz#38b5e6c5db8cbe81debbd16e089335ada05ea23a"
examples/Web/web/yarn.lock:9089:  resolved "https://registry.yarnpkg.com/websocket-driver/-/websocket-driver-0.7.4.tgz#89ad5295bbf64b480abcba31e4953aca706f5760"
examples/Web/web/yarn.lock:9098:  resolved "https://registry.yarnpkg.com/websocket-extensions/-/websocket-extensions-0.1.4.tgz#7f8473bc839dfd87608adb95d7eb075211578a42"
examples/Web/web/yarn.lock:9103:  resolved "https://registry.yarnpkg.com/whatwg-encoding/-/whatwg-encoding-1.0.5.tgz#5abacf777c32166a51d085d6b4f3e7d27113ddb0"
examples/Web/web/yarn.lock:9110:  resolved "https://registry.yarnpkg.com/whatwg-fetch/-/whatwg-fetch-3.6.2.tgz#dced24f37f2624ed0281725d51d0e2e3fe677f8c"
examples/Web/web/yarn.lock:9115:  resolved "https://registry.yarnpkg.com/whatwg-mimetype/-/whatwg-mimetype-2.3.0.tgz#3d4b1e0312d2079879f826aff18dbeeca5960fbf"
examples/Web/web/yarn.lock:9120:  resolved "https://registry.yarnpkg.com/whatwg-url/-/whatwg-url-7.1.0.tgz#c2c492f1eca612988efd3d2266be1b9fc6170d06"
examples/Web/web/yarn.lock:9129:  resolved "https://registry.yarnpkg.com/whatwg-url/-/whatwg-url-8.7.0.tgz#656a78e510ff8f3937bc0bcbe9f5c0ac35941b77"
examples/Web/web/yarn.lock:9138:  resolved "https://registry.yarnpkg.com/which-boxed-primitive/-/which-boxed-primitive-1.0.2.tgz#13757bc89b209b049fe5d86430e21cf40a89a8e6"
examples/Web/web/yarn.lock:9149:  resolved "https://registry.yarnpkg.com/which/-/which-1.3.1.tgz#a45043d54f5805316da8d62f9f50918d3da70b0a"
examples/Web/web/yarn.lock:9156:  resolved "https://registry.yarnpkg.com/which/-/which-2.0.2.tgz#7c6a8dd0a636a0327e10b59c9286eee93f3f51b1"
examples/Web/web/yarn.lock:9163:  resolved "https://registry.yarnpkg.com/word-wrap/-/word-wrap-1.2.4.tgz#cb4b50ec9aca570abd1f52f33cd45b6c61739a9f"
examples/Web/web/yarn.lock:9168:  resolved "https://registry.yarnpkg.com/workbox-background-sync/-/workbox-background-sync-6.4.2.tgz#bb31b95928d376abcb9bde0de3a0cef9bae46cf7"
examples/Web/web/yarn.lock:9176:  resolved "https://registry.yarnpkg.com/workbox-broadcast-update/-/workbox-broadcast-update-6.4.2.tgz#5094c4767dfb590532ac03ee07e9e82b2ac206bc"
examples/Web/web/yarn.lock:9183:  resolved "https://registry.yarnpkg.com/workbox-build/-/workbox-build-6.4.2.tgz#47f9baa946c3491533cd5ccb1f194a7160e8a6e3"
examples/Web/web/yarn.lock:9208:    upath "^1.2.0"
examples/Web/web/yarn.lock:9227:  resolved "https://registry.yarnpkg.com/workbox-cacheable-response/-/workbox-cacheable-response-6.4.2.tgz#ebcabb3667019da232e986a9927af97871e37ccb"
examples/Web/web/yarn.lock:9234:  resolved "https://registry.yarnpkg.com/workbox-core/-/workbox-core-6.4.2.tgz#f99fd36a211cc01dce90aa7d5f2c255e8fe9d6bc"
examples/Web/web/yarn.lock:9239:  resolved "https://registry.yarnpkg.com/workbox-expiration/-/workbox-expiration-6.4.2.tgz#61613459fd6ddd1362730767618d444c6b9c9139"
examples/Web/web/yarn.lock:9247:  resolved "https://registry.yarnpkg.com/workbox-google-analytics/-/workbox-google-analytics-6.4.2.tgz#eea7d511b3078665a726dc2ee9f11c6b7a897530"
examples/Web/web/yarn.lock:9257:  resolved "https://registry.yarnpkg.com/workbox-navigation-preload/-/workbox-navigation-preload-6.4.2.tgz#35cd4ba416a530796af135410ca07db5bee11668"
examples/Web/web/yarn.lock:9264:  resolved "https://registry.yarnpkg.com/workbox-precaching/-/workbox-precaching-6.4.2.tgz#8d87c05d54f32ac140f549faebf3b4d42d63621e"
examples/Web/web/yarn.lock:9273:  resolved "https://registry.yarnpkg.com/workbox-range-requests/-/workbox-range-requests-6.4.2.tgz#050f0dfbb61cd1231e609ed91298b6c2442ae41b"
examples/Web/web/yarn.lock:9280:  resolved "https://registry.yarnpkg.com/workbox-recipes/-/workbox-recipes-6.4.2.tgz#68de41fa3a77b444b0f93c9c01a76ba1d41fd2bf"
examples/Web/web/yarn.lock:9292:  resolved "https://registry.yarnpkg.com/workbox-routing/-/workbox-routing-6.4.2.tgz#65b1c61e8ca79bb9152f93263c26b1f248d09dcc"
examples/Web/web/yarn.lock:9299:  resolved "https://registry.yarnpkg.com/workbox-strategies/-/workbox-strategies-6.4.2.tgz#50c02bf2d116918e1a8052df5f2c1e4103c62d5d"
examples/Web/web/yarn.lock:9306:  resolved "https://registry.yarnpkg.com/workbox-streams/-/workbox-streams-6.4.2.tgz#3bc615cccebfd62dedf28315afb7d9ee177912a5"
examples/Web/web/yarn.lock:9314:  resolved "https://registry.yarnpkg.com/workbox-sw/-/workbox-sw-6.4.2.tgz#9a6db5f74580915dc2f0dbd47d2ffe057c94a795"
examples/Web/web/yarn.lock:9319:  resolved "https://registry.yarnpkg.com/workbox-webpack-plugin/-/workbox-webpack-plugin-6.4.2.tgz#aad9f11b028786d5b781420e68f4e8f570ea9936"
examples/Web/web/yarn.lock:9325:    upath "^1.2.0"
examples/Web/web/yarn.lock:9331:  resolved "https://registry.yarnpkg.com/workbox-window/-/workbox-window-6.4.2.tgz#5319a3e343fa1e4bd15a1f53a07b58999d064c8a"
examples/Web/web/yarn.lock:9339:  resolved "https://registry.yarnpkg.com/wrap-ansi/-/wrap-ansi-7.0.0.tgz#67e145cff510a6a6984bdf1152911d69d2eb9e43"
examples/Web/web/yarn.lock:9348:  resolved "https://registry.yarnpkg.com/wrappy/-/wrappy-1.0.2.tgz#b5243d8f3ec1aa35f1364605bc0d1036e30ab69f"
examples/Web/web/yarn.lock:9353:  resolved "https://registry.yarnpkg.com/write-file-atomic/-/write-file-atomic-3.0.3.tgz#56bd5c5a5c70481cd19c571bd39ab965a5de56e8"
examples/Web/web/yarn.lock:9363:  resolved "https://registry.yarnpkg.com/ws/-/ws-7.5.10.tgz#58b5c20dc281633f6c19113f39b349bd8bd558d9"
examples/Web/web/yarn.lock:9368:  resolved "https://registry.yarnpkg.com/ws/-/ws-8.17.1.tgz#9293da530bb548febc95371d90f9c878727d919b"
examples/Web/web/yarn.lock:9373:  resolved "https://registry.yarnpkg.com/xml-name-validator/-/xml-name-validator-3.0.0.tgz#6ae73e06de4d8c6e47f9fb181f78d648ad457c6a"
examples/Web/web/yarn.lock:9378:  resolved "https://registry.yarnpkg.com/xmlchars/-/xmlchars-2.2.0.tgz#060fe1bcb7f9c76fe2a17db86a9bc3ab894210cb"
examples/Web/web/yarn.lock:9383:  resolved "https://registry.yarnpkg.com/xtend/-/xtend-4.0.2.tgz#bb72779f5fa465186b1f438f674fa347fdb5db54"
examples/Web/web/yarn.lock:9388:  resolved "https://registry.yarnpkg.com/y18n/-/y18n-5.0.8.tgz#7f4934d0f7ca8c56f95314939ddcd2dd91ce1d55"
examples/Web/web/yarn.lock:9393:  resolved "https://registry.yarnpkg.com/yallist/-/yallist-4.0.0.tgz#9bb92790d9c0effec63be73519e11a35019a3a72"
examples/Web/web/yarn.lock:9398:  resolved "https://registry.yarnpkg.com/yaml/-/yaml-1.10.3.tgz#76e407ed95c42684fb8e14641e5de62fe65bbcb3"
examples/Web/web/yarn.lock:9403:  resolved "https://registry.yarnpkg.com/yargs-parser/-/yargs-parser-20.2.9.tgz#2eb7dc3b0289718fc295f362753845c41a0c94ee"
examples/Web/web/yarn.lock:9408:  resolved "https://registry.yarnpkg.com/yargs/-/yargs-16.2.0.tgz#1c82bf0f6b6a66eafce7ef30e376f49a12477f66"
examples/Web/web/yarn.lock:9421:  resolved "https://registry.yarnpkg.com/yocto-queue/-/yocto-queue-0.1.0.tgz#0294eb3dee05028d31ee1a5fa2c556a6aaf10a1b"
tests/Soulseek.Tests.Unit/Support/TestFile.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchScopeTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/TcpListenerAdapter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:46:        [Fact(DisplayName = "Transfer tracker disposes cancellation token source when removing transfer")]
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:59:        [Fact(DisplayName = "Transfer tracker disposes cancellation token sources when removing user")]
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:72:        [Fact(DisplayName = "Transfer tracker disposes replaced cancellation token source")]
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:159:                    .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((callbackUsername, callbackFilename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:188:        [Fact(DisplayName = "Transfer enqueue disposes untracked cancellation token source when download faults")]
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:203:                .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((username, filename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:227:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:228:            Directory.CreateDirectory(path);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:229:            return path;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:232:        private static CancellationTokenSource GetCancellationTokenSource(CancellationToken token)
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:234:            var source = typeof(CancellationToken).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(token) as CancellationTokenSource;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:235:            return source ?? throw new InvalidOperationException("Unable to inspect cancellation token source");
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:46:                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:63:                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, "filename", () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:72:        public async Task EnqueueDownloadAsync_Returns_Download_Task_After_Enqueue(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:76:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:77:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:79:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:115:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:122:                var downloadTask = await s.EnqueueDownloadAsync(username, filename, localFilename, (long?)size, 0, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:132:                Assert.Equal(token, transfer.Token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:133:                Assert.Equal(filename, transfer.Filename);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:139:        public async Task EnqueueDownloadAsync_Stream_Returns_Download_Task_After_Enqueue(string username, string filename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:143:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:144:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:146:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:182:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:190:                var downloadTask = await s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), (long?)size, 0, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:200:                Assert.Equal(token, transfer.Token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:201:                Assert.Equal(filename, transfer.Filename);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:207:        public async Task EnqueueDownloadAsync_Stream_Throws_Download_Exception_On_Error(string username, string filename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:211:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:212:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:214:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:253:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:261:                var downloadTask = await s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), (long?)size, 0, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:277:        public async Task EnqueueDownloadAsync_Throws_Download_Exception_On_Error(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:281:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:282:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:284:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:323:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:330:                var downloadTask = await s.EnqueueDownloadAsync(username, filename, localFilename, (long?)size, 0, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:346:        public async Task EnqueueDownloadAsync_Stream_Throws_Download_Exception_On_Error_Before_Queue(string username, string filename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:350:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:351:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:353:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:392:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:400:                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), 0L, 0, token));
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:411:        public async Task EnqueueDownloadAsync_Throws_Download_Exception_On_Error_Before_Queue(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:415:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:416:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:418:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:457:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/EnqueueDownloadAsyncTests.cs:464:                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, filename, localFilename, 0L, 0, token));
tests/Soulseek.Tests.Unit/Options/SoulseekClientOptionsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Options/SoulseekClientOptionsTests.cs:614:        [Fact(DisplayName = "Throws if starting token is negative")]
tests/Soulseek.Tests.Unit/Client/GrantUserPrivilegesAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/TcpClientAdapter.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/TcpClientAdapter.cs:102:        ///     specified <paramref name="proxyAddress"/> and <paramref name="proxyPort"/>.
src/Network/Tcp/TcpClientAdapter.cs:104:        /// <param name="proxyAddress">The address of the proxy server to which to connect.</param>
src/Network/Tcp/TcpClientAdapter.cs:105:        /// <param name="proxyPort">The port of the proxy server to which to connect.</param>
src/Network/Tcp/TcpClientAdapter.cs:108:        /// <param name="username">The optional username for the proxy.</param>
src/Network/Tcp/TcpClientAdapter.cs:109:        /// <param name="password">The optional password for the proxy.</param>
src/Network/Tcp/TcpClientAdapter.cs:110:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/TcpClientAdapter.cs:112:        ///     The Task representing the asynchronous operation, including the address and port reported by the proxy server
src/Network/Tcp/TcpClientAdapter.cs:115:        /// <exception cref="ArgumentNullException">Thrown when the proxy or destination address is null.</exception>
src/Network/Tcp/TcpClientAdapter.cs:117:        ///     Thrown when the proxy or destination port is not within the valid port range 0-65535.
src/Network/Tcp/TcpClientAdapter.cs:119:        /// <exception cref="ArgumentException">Thrown when a username is supplied without a password, or vice versa.</exception>
src/Network/Tcp/TcpClientAdapter.cs:120:        /// <exception cref="ArgumentOutOfRangeException">Thrown when the username or password is longer than 255 characters.</exception>
src/Network/Tcp/TcpClientAdapter.cs:123:            IPAddress proxyAddress,
src/Network/Tcp/TcpClientAdapter.cs:124:            int proxyPort,
src/Network/Tcp/TcpClientAdapter.cs:128:            string password = null,
src/Network/Tcp/TcpClientAdapter.cs:131:            if (proxyAddress == default)
src/Network/Tcp/TcpClientAdapter.cs:133:                throw new ArgumentNullException(nameof(proxyAddress));
src/Network/Tcp/TcpClientAdapter.cs:136:            if (proxyPort < IPEndPoint.MinPort || proxyPort > IPEndPoint.MaxPort)
src/Network/Tcp/TcpClientAdapter.cs:138:                throw new ArgumentOutOfRangeException(nameof(proxyPort), proxyPort, $"Proxy port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
src/Network/Tcp/TcpClientAdapter.cs:151:            if (username == default != (password == default))
src/Network/Tcp/TcpClientAdapter.cs:153:                throw new ArgumentException("Username and password must both be supplied");
src/Network/Tcp/TcpClientAdapter.cs:161:            if (password != default && password.Length > 255)
src/Network/Tcp/TcpClientAdapter.cs:163:                throw new ArgumentOutOfRangeException(nameof(password), "The password length must be less than or equal to 255 characters");
src/Network/Tcp/TcpClientAdapter.cs:166:            return ConnectThroughProxyInternalAsync(proxyAddress, proxyPort, destinationAddress, destinationPort, cancellationToken ?? CancellationToken.None, username, password);
src/Network/Tcp/TcpClientAdapter.cs:190:            IPAddress proxyAddress,
src/Network/Tcp/TcpClientAdapter.cs:191:            int proxyPort,
src/Network/Tcp/TcpClientAdapter.cs:196:            string password = null)
src/Network/Tcp/TcpClientAdapter.cs:213:            var usingCredentials = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
src/Network/Tcp/TcpClientAdapter.cs:240:                await ConnectAsync(proxyAddress, proxyPort).ConfigureAwait(false);
src/Network/Tcp/TcpClientAdapter.cs:271:                            throw new ProxyException("Server requests authorization but none was provided");
src/Network/Tcp/TcpClientAdapter.cs:282:                        creds.Add((byte)password.Length);
src/Network/Tcp/TcpClientAdapter.cs:283:                        creds.AddRange(Encoding.ASCII.GetBytes(password));
src/Network/Tcp/TcpClientAdapter.cs:397:                throw new ProxyException($"Failed to connect to proxy: {ex.Message}", ex);
tests/Soulseek.Tests.Unit/Client/DropPrivateRoomOwnershipAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetUserStatusAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/NetworkStreamAdapter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/NetworkStreamAdapter.cs:56:        ///     Uses SetSocketOption under the hood: https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/NetworkStream.cs.
src/Network/Tcp/NetworkStreamAdapter.cs:74:        ///     Uses SetSocketOption under the hood: https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/NetworkStream.cs.
src/Network/Tcp/NetworkStreamAdapter.cs:113:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/NetworkStreamAdapter.cs:134:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/NetworkStreamAdapter.cs:152:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/NetworkStreamAdapter.cs:174:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:204:            var token = new JwtSecurityToken(notBefore: notBefore, expires: notBefore.AddHours(1));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:205:            var response = new TokenResponse(token);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:208:            Assert.Equal(((DateTimeOffset)token.ValidFrom).ToUnixTimeSeconds(), response.NotBefore);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:296:                    new KeyValuePair<string, string>("PASSWORD", "password"),
src/Network/Tcp/Listener.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/ITcpListener.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:34:        internal void Instantiates_With_The_Given_Data(string username, string filename, int token)
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:36:            var d = new TransferInternal(TransferDirection.Download, username, filename, token);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:39:            Assert.Equal(filename, d.Filename);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:40:            Assert.Equal(token, d.Token);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:45:        internal void Properties_Default_To_Expected_Values(string username, string filename, int token, TransferOptions options)
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:47:            var d = new TransferInternal(TransferDirection.Download, username, filename, token, options);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:71:        internal void IPAddress_And_Port_Props_Return_Connection_Props(string username, string filename, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:73:            var d = new TransferInternal(TransferDirection.Download, username, filename, token);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:86:        internal void Wait_Key_Is_Expected_Value(string username, string filename, int token, TransferDirection direction)
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:88:            var d = new TransferInternal(direction, username, filename, token);
tests/Soulseek.Tests.Unit/TransferInternalTests.cs:90:            Assert.Equal(new WaitKey(Constants.WaitKey.Transfer, direction, username, filename, token), d.WaitKey);
src/Network/Tcp/ITcpClient.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/ITcpClient.cs:74:        ///     specified <paramref name="proxyAddress"/> and <paramref name="proxyPort"/>.
src/Network/Tcp/ITcpClient.cs:76:        /// <param name="proxyAddress">The address of the proxy server to which to connect.</param>
src/Network/Tcp/ITcpClient.cs:77:        /// <param name="proxyPort">The port of the proxy server to which to connect.</param>
src/Network/Tcp/ITcpClient.cs:80:        /// <param name="username">The optional username for the proxy.</param>
src/Network/Tcp/ITcpClient.cs:81:        /// <param name="password">The optional password for the proxy.</param>
src/Network/Tcp/ITcpClient.cs:82:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/ITcpClient.cs:84:        ///     The Task representing the asynchronous operation, including the address and port reported by the proxy server
src/Network/Tcp/ITcpClient.cs:87:        /// <exception cref="ArgumentNullException">Thrown when the proxy or destination address is null.</exception>
src/Network/Tcp/ITcpClient.cs:89:        ///     Thrown when the proxy or destination port is not within the valid port range 0-65535.
src/Network/Tcp/ITcpClient.cs:91:        /// <exception cref="ArgumentException">Thrown when a username is supplied without a password, or vice versa.</exception>
src/Network/Tcp/ITcpClient.cs:92:        /// <exception cref="ArgumentOutOfRangeException">Thrown when the username or password is longer than 255 characters.</exception>
src/Network/Tcp/ITcpClient.cs:95:            IPAddress proxyAddress,
src/Network/Tcp/ITcpClient.cs:96:            int proxyPort,
src/Network/Tcp/ITcpClient.cs:100:            string password = null,
src/Network/Tcp/INetworkStream.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/INetworkStream.cs:58:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/INetworkStream.cs:76:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/INetworkStream.cs:91:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/INetworkStream.cs:110:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IListener.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetUserStatisticsAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/DropPrivateRoomMembershipAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/IConnection.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/IConnection.cs:117:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IConnection.cs:150:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IConnection.cs:171:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IConnection.cs:188:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IConnection.cs:198:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/IConnection.cs:216:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/ConnectionTypes.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.CouncilAnalyzers.Calibration/CouncilAnalyzerCalibrationTests.cs:147:            var path = reader.ReadString();
tests/Soulseek.CouncilAnalyzers.Calibration/CouncilAnalyzerCalibrationTests.cs:148:            return File.ReadAllText(path);
tests/Soulseek.CouncilAnalyzers.Calibration/CouncilAnalyzerCalibrationTests.cs:285:            var path = PathSafety.ResolveContainedPath(root, reader.ReadString());
tests/Soulseek.CouncilAnalyzers.Calibration/CouncilAnalyzerCalibrationTests.cs:286:            return System.IO.File.ReadAllText(path);
src/Network/Tcp/ConnectionState.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/TransferTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/TransferTests.cs:33:            string filename,
tests/Soulseek.Tests.Unit/TransferTests.cs:34:            int token,
tests/Soulseek.Tests.Unit/TransferTests.cs:56:                filename,
tests/Soulseek.Tests.Unit/TransferTests.cs:57:                token,
tests/Soulseek.Tests.Unit/TransferTests.cs:71:            Assert.Equal(filename, t.Filename);
tests/Soulseek.Tests.Unit/TransferTests.cs:72:            Assert.Equal(token, t.Token);
tests/Soulseek.Tests.Unit/TransferTests.cs:92:        internal void Instantiates_With_Expected_Data_Given_TransferInternal(string username, string filename, int token)
tests/Soulseek.Tests.Unit/TransferTests.cs:96:            var i = new TransferInternal(TransferDirection.Download, username, filename, token);
src/Network/Tcp/ConnectionKey.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/ConnectionEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetUserPrivilegedAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/ConnectToUserAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/MessageReaderExtensions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/MessageReaderExtensions.cs:49:            var filename = reader.ReadString();
src/Messaging/MessageReaderExtensions.cs:84:                filename,
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2903:        internal async Task GetParentCandidateConnectionAsync_Sends_PeerInit_When_Direct_Connects_First(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id, int token)
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2910:                .Returns(token);
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2942:            var peerInit = new PeerInit(localUser, Constants.ConnectionType.Distributed, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2949:        internal async Task GetParentCandidateConnectionAsync_Prefers_Cached_Obfuscated_Endpoint_For_Outbound_Direct_Connection(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id, int token)
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2958:                .Returns(token);
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:2994:            var peerInit = new PeerInit(localUser, Constants.ConnectionType.Distributed, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:3002:        internal async Task GetParentCandidateConnectionAsync_Falls_Back_To_Regular_Direct_When_Obfuscated_Negotiation_Fails(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid regularId, Guid obfuscatedId, int token)
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:3011:                .Returns(token);
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:3056:            regularConn.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(new PeerInit(localUser, Constants.ConnectionType.Distributed, token).ToByteArray())), It.IsAny<CancellationToken>()));
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:4080:        internal void WaitForParentCandidateConnection_MessageRead_Completes_Search_Wait_On_Search_Request(string username, IPEndPoint endpoint, Guid id, int token, string query)
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:4088:            var args = new MessageEventArgs(new DistributedSearchRequest(username, token, query).ToByteArray());
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:4100:        internal void WaitForParentCandidateConnection_MessageRead_Completes_Search_Wait_On_Server_Search_Request(string username, int token, string query, IPEndPoint endpoint, Guid id)
tests/Soulseek.Tests.Unit/Network/DistributedConnectionManagerTests.cs:4114:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:236:        [Theory(DisplayName = "AddTransferConnectionAsync reads token and returns connection"), AutoData]
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:237:        internal async Task AddTransferConnectionAsync_Reads_Token_And_Returns_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:243:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:254:                var (connection, remoteToken) = await manager.GetTransferConnectionAsync(username, token, incomingConn.Object);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:257:                Assert.Equal(token, remoteToken);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:263:        internal async Task AddTransferConnectionAsync_Preserves_Obfuscated_Incoming_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:267:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:282:                var (connection, remoteToken) = await manager.GetTransferConnectionAsync(username, token, incomingConn.Object);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:285:                Assert.Equal(token, remoteToken);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:294:        internal async Task AddTransferConnectionAsync_Disposes_Connection_On_Exception(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:313:                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:325:        internal async Task AddTransferConnectionAsync_Produces_Diagnostic_On_Disconnect(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:345:                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:358:        internal async Task AddTransferConnectionAsync_Sets_Connection_Type_To_Inbound_Direct(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:364:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:375:                await manager.GetTransferConnectionAsync(username, token, incomingConn.Object);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:383:        internal async Task AddTransferConnectionAsync_Produces_Expected_Diagnostic_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:398:                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:408:        internal async Task AddTransferConnectionAsync_Throws_Expected_Exception_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:423:                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:435:        internal async Task AddMessageConnectionAsync_Starts_Reading(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:441:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:460:        internal async Task AddMessageConnectionAsync_Disposes_Connection_And_Throws_If_Start_Reading_Throws(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:468:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:495:        internal async Task AddMessageConnectionAsync_Adds_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:501:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:521:        internal async Task AddMessageConnectionAsync_Replaces_Duplicate_Connection_And_Does_Not_Dispose_Old(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:527:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:533:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:568:        internal async Task AddMessageConnectionAsync_Does_Not_Throw_If_Fetch_Of_Cached_Throws(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:574:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:599:        internal async Task AddMessageConnectionAsync_Cancels_Pending_Indirect_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:605:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:715:        internal async Task AddMessageConnectionAsync_Sets_Connection_Type_To_Inbound_Direct(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:721:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:740:        internal async Task GetTransferConnectionAsync_CTPR_Connects_And_Pierces_Firewall(string username, IPEndPoint endpoint, int token, bool isPrivileged)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:742:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, isPrivileged);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:743:            var expectedBytes = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:753:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:769:            Assert.Equal(token, newConn.RemoteToken);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:779:        internal async Task GetTransferConnectionAsync_CTPR_Falls_Back_To_Regular_Endpoint_When_Obfuscated_Attempt_Fails(string username, IPAddress ipAddress, int port, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:785:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false, obfuscationType: 1, obfuscatedPort: obfuscatedPort);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:797:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:811:                Assert.Equal(token, newConn.RemoteToken);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:815:            regularConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(new PierceFirewall(token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:820:        internal async Task GetTransferConnectionAsync_CTPR_Disposes_Connection_If_Connect_Fails(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:822:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:848:        internal async Task GetTransferConnectionAsync_Adds_Diagnostic_On_Disconnect(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:850:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:877:        public async Task GetTransferConnectionAsync_CTPR_Sets_Type_To_Inbound_Indirect(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:879:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:883:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:900:        public async Task GetTransferConnectionAsync_CTPR_Produces_Expected_Diagnostic_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:902:            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:924:        internal async Task GetTransferConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:939:                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:950:        internal async Task GetTransferConnectionOutboundDirectAsync_Returns_Connection_If_Connect_Succeeds(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:962:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:970:        internal async Task GetTransferConnectionOutboundDirectAsync_Adds_Diagnostic_On_Disconnect(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:983:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:993:        internal async Task GetTransferConnectionOutboundDirectAsync_Sets_Connection_Type_To_Outbound_Direct(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1009:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1019:        public async Task GetTransferConnectionOutboundDirectAsyncnc_Produces_Expected_Diagnostic_On_Failure(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1034:                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1042:        internal async Task GetTransferConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1057:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1067:        internal async Task GetTransferConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1085:                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1094:        internal async Task GetTransferConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1109:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1119:        internal async Task GetTransferConnectionOutboundIndirectAsync_Sets_Connection_Type_To_Outbound_Indirect(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1134:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1144:        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1163:                using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1178:        internal async Task GetTransferConnectionOutboundIndirectAsync_Produces_Expected_Diagnostic_On_Failure(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1196:                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1204:        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_Diagnostic_On_Disconnect(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1222:            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1232:        internal async Task GetTransferConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1258:            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1267:        internal async Task GetTransferConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1295:            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1304:        internal async Task GetTransferConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1333:                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1342:        internal async Task GetTransferConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1368:            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1378:        public async Task GetTransferConnectionAsync_Produces_Expected_Diagnostic_On_Failure(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1407:                await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1415:        public async Task GetTransferConnectionAsync_Produces_Expected_Diagnostic_On_Negotiation_Failure(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1446:                await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1454:        internal async Task GetTransferConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1456:            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1482:            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1492:        [Theory(DisplayName = "GetTransferConnectionAsync writes token on connection established"), AutoData]
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1493:        internal async Task GetTransferConnectionAsync_Writes_Token_On_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1519:            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1524:                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(BitConverter.GetBytes(token))), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1530:        internal async Task GetTransferConnectionAsync_Prefers_Cached_Obfuscated_Endpoint_For_Outbound_Direct_Transfer(string localUsername, string username, IPAddress ipAddress, int directPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1555:            using (var newConn = await manager.GetTransferConnectionAsync(username, directEndpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1565:        internal async Task GetTransferConnectionAsync_Falls_Back_To_Regular_Direct_Transfer_When_Obfuscated_Negotiation_Fails(string localUsername, string username, IPAddress ipAddress, int directPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1596:            using (var newConn = await manager.GetTransferConnectionAsync(username, directEndpoint, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1602:            direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:1603:            direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(BitConverter.GetBytes(token))), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2058:        internal async Task GetOrAddMessageConnectionAsyncCTPR_Returns_Existing_Connection_If_Exists(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2060:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2083:        internal async Task GetOrAddMessageConnectionAsyncCTPR_Updates_PendingInboundDirectConnectionDictionary_If_Key_Exists(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2085:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2139:        internal async Task GetOrAddMessageConnectionAsync_Connects_And_Returns_New_If_Not_Existing(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2141:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2165:        internal async Task GetOrAddMessageConnectionAsync_Prefers_Cached_Obfuscated_Endpoint_For_Outbound_Direct_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2189:                var newConn = await manager.GetOrAddMessageConnectionAsync(username, endpoint, token, CancellationToken.None);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2199:        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Connect_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2202:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2227:        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Write_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2230:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2256:        [Theory(DisplayName = "GetOrAddMessageConnectionAsync pierces firewall with correct token"), AutoData]
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2257:        internal async Task GetOrAddMessageConnectionAsync_Pierces_Firewall_With_Correct_Token(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2259:            var expectedMessage = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2260:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2283:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Generates_Expected_Diagnostic_On_Successful_Connection(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2285:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2313:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Purges_Cache_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2315:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2340:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Produces_Warning_And_Replaces_If_Wrong_Connection_Is_Purged(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2342:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2383:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Produces_Expected_Diagnostics_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2385:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2408:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Throws_Expected_Exception_On_Failure(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2410:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2434:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Caches_Connection_If_Uncached(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2436:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2468:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Returns_Cached_Connection_If_Cached(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2470:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2493:        internal async Task GetOrAddMessageConnectionAsync_CTPR_Sets_Connection_Type_To_Inbound_Indirect(string username, IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2495:            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2970:        internal async Task GetOrAddMessageConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2972:            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Peer, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:2989:                .Returns(token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3092:        internal async Task AwaitTransferConnectionAsync_Returns_Indirect_When_Indirect_Connects(string username, string filename, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3098:            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3099:            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3108:            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3116:        internal async Task AwaitTransferConnectionAsync_Returns_Direct_When_Direct_Connects(string username, string filename, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3122:            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3123:            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3132:            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3140:        internal async Task AwaitTransferConnectionAsync_Throws_ConnectionException_When_Both_Fail(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3144:            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3145:            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3155:                var ex = await Record.ExceptionAsync(() => manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3165:        internal async Task AwaitTransferConnectionAsync_Produces_Expected_Diagnostics_On_connection(string username, string filename, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3171:            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3172:            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3181:            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3193:        internal async Task AwaitTransferConnectionAsync_Produces_Expected_Diagnostics_On_Failure(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3197:            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3198:            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/PeerConnectionManagerTests.cs:3208:                await Record.ExceptionAsync(() => manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None));
tests/Soulseek.Tests.Unit/Client/GetUserInfoAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/MessageFrameValidatorTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/ProtocolArgumentValidator.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/Connection.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/Tcp/Connection.cs:234:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:256:            // that ends when the result is set programmatically. create another for cancellation via the externally provided token.
src/Network/Tcp/Connection.cs:271:                        var proxy = Options.ProxyOptions;
src/Network/Tcp/Connection.cs:274:                            proxy.IPEndPoint.Address,
src/Network/Tcp/Connection.cs:275:                            proxy.IPEndPoint.Port,
src/Network/Tcp/Connection.cs:278:                            proxy.Username,
src/Network/Tcp/Connection.cs:279:                            proxy.Password,
src/Network/Tcp/Connection.cs:289:                    // TCS. either the timeout or the external token can now cancel the operation.
src/Network/Tcp/Connection.cs:394:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:435:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:480:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:498:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:534:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/Tcp/Connection.cs:787:                // is in a bad state. when this happens memory usage skyrockets. see https://github.com/slskd/slskd/issues/251 for
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:593:        public void GetNextToken_Invokes_TokenFactory(int token)
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:597:                .Returns(token);
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:599:            using (var s = new SoulseekClient(minorVersion: 9999, tokenFactory: f.Object))
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:603:                Assert.Equal(token, t);
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1504:        public void UserCannotConnect_Fires_When_Handler_Raises(int token, string username)
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1506:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1508:            var expectedArgs = new UserCannotConnectEventArgs(token, username);
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1523:        public void UserCannotConnect_Does_Not_Throw_If_Event_Not_Bound(int token, string username)
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1525:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/SoulseekClientTests.cs:1527:            var expectedArgs = new UserCannotConnectEventArgs(token, username);
tests/Soulseek.Tests.Unit/Client/CleanupSemaphoresAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/GetRoomListAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchTests.cs:30:        public void Instantiates_With_Expected_Data(string searchText, int token, SearchStates state, int responseCount, int fileCount, int lockedFileCount)
tests/Soulseek.Tests.Unit/SearchTests.cs:37:            var s = new Search(new SearchQuery(searchText), SearchScope.Network, token, state, responseCount, fileCount, lockedFileCount);
tests/Soulseek.Tests.Unit/SearchTests.cs:41:            Assert.Equal(token, s.Token);
tests/Soulseek.Tests.Unit/SearchTests.cs:50:        internal void Instantiates_With_Expected_Data_Given_SearchInternal(string searchText, int token)
tests/Soulseek.Tests.Unit/SearchTests.cs:52:            var i = new SearchInternal(SearchQuery.FromText(searchText), SearchScope.Network, token);
tests/Soulseek.Tests.Unit/SearchTests.cs:54:            i.TryAddResponse(new SearchResponse("foo", token, false, 420, 24, new List<File>()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:35:        public void Instantiates_With_Expected_Data(string searchText, int token, SearchOptions options)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:37:            var s = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, options);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:41:            Assert.Equal(token, s.Token);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:198:        [Fact(DisplayName = "TryAddResponse ignores response when token does not match")]
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:291:        public void TryAddResponse_Adds_Response(string username, int token, File file)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:293:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:299:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:314:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }, new List<File>() { file }));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:349:        public void TryAddResponse_Swallows_ObjectDisposedException_Thrown_From_Body(string username, int token, File file)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:351:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:356:            var ex = Record.Exception(() => s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file })));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:365:        public void TryAddResponse_Swallows_Exceptions(string username, int token, File file)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:367:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:373:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:388:            var ex = Record.Exception(() => s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file })));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:398:        public void TryAddResponse_Ignores_Response_When_All_Files_Are_Filtered_And_Response_Filtering_Is_Enabled(string username, int token, byte code, string filename, int size, string extension)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:405:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:411:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:414:                .WriteString(filename) // filename
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:429:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:441:        public void TryAddResponse_Ignores_Response_When_ResponseFilter_Returns_False(string username, int token, byte code, string filename, int size, string extension)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:448:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:454:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:457:                .WriteString(filename) // filename
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:472:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:484:        public async Task TryAddResponse_Completes_Search_And_Invokes_Completed_Event_When_File_Limit_Reached(string username, int token, File file)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:491:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:497:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:511:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:523:        public async Task TryAddResponse_Completes_Search_And_Invokes_Completed_Event_When_Response_Limit_Reached(string username, int token, byte code, string filename, int size, string extension)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:531:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:537:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:540:                .WriteString(filename) // filename
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:557:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:569:        public void TryAddResponse_Invokes_Response_Received_Event_Handler(string username, int token, File file)
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:573:            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:581:                .WriteInteger(token) // token
tests/Soulseek.Tests.Unit/SearchInternalTests.cs:593:            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }));
tests/Soulseek.Tests.Unit/Client/GetPrivilegesAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/WishlistSearchRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/WishlistSearchRequest.cs:37:        /// <param name="token">The unique token for the search.</param>
src/Messaging/Messages/Server/WishlistSearchRequest.cs:38:        public WishlistSearchRequest(string searchText, int token)
src/Messaging/Messages/Server/WishlistSearchRequest.cs:40:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "search token");
src/Messaging/Messages/Server/WishlistSearchRequest.cs:43:            Token = token;
src/Messaging/Messages/Server/WishlistSearchRequest.cs:52:        ///     Gets the unique token for the search.
src/Network/PeerConnectionManager.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/PeerConnectionManager.cs:24://     Modified: Added type-1 obfuscated peer-message connection paths.
src/Network/PeerConnectionManager.cs:183:                        // because we cancelled any pending connection above, the Lazy<> function has completed executing and we
src/Network/PeerConnectionManager.cs:212:        ///     <paramref name="filename"/> and <paramref name="remoteToken"/>.
src/Network/PeerConnectionManager.cs:219:        /// <param name="filename">The filename associated with the expected transfer.</param>
src/Network/PeerConnectionManager.cs:220:        /// <param name="remoteToken">The remote token associated with the expected transfer.</param>
src/Network/PeerConnectionManager.cs:221:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/PeerConnectionManager.cs:223:        public async Task<IConnection> AwaitTransferConnectionAsync(string username, string filename, int remoteToken, CancellationToken cancellationToken)
src/Network/PeerConnectionManager.cs:230:            Diagnostic.Debug($"Waiting for a direct or indirect transfer connection from {username} with remote token {remoteToken} for {filename}");
src/Network/PeerConnectionManager.cs:234:                key: new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, remoteToken),
src/Network/PeerConnectionManager.cs:256:                var msg = $"Failed to establish a direct or indirect transfer connection to {username} with remote token {remoteToken} for {filename}";
src/Network/PeerConnectionManager.cs:264:            Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} transfer connection to {username} ({connection.IPEndPoint}) with remote token {remoteToken} for {filename} established first, attempting to cancel {(isDirect ? "indirect" : "direct")} connection.");
src/Network/PeerConnectionManager.cs:267:            Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) with remote token {remoteToken} for {filename} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:353:                        // had timed out or failed, but before that connection was able to cancel the pending token this should be
src/Network/PeerConnectionManager.cs:377:                Diagnostic.Debug($"Attempting inbound indirect message connection to {r.Username} ({endPoint}) for token {r.Token}");
src/Network/PeerConnectionManager.cs:413:                        // let everyone know this code is done executing and that .Value of the containing cache is safe to await
src/Network/PeerConnectionManager.cs:432:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/PeerConnectionManager.cs:445:        /// <param name="solicitationToken">The optional token for the indirect connection solicitation.</param>
src/Network/PeerConnectionManager.cs:446:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/PeerConnectionManager.cs:578:        /// <param name="token">The token with which the firewall was pierced.</param>
src/Network/PeerConnectionManager.cs:581:        public async Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(string username, int token, IConnection incomingConnection)
src/Network/PeerConnectionManager.cs:583:            Diagnostic.Debug($"Inbound transfer connection to {username} ({incomingConnection.IPEndPoint}) for token {token} accepted. (type: {incomingConnection.Type}, id: {incomingConnection.Id}");
src/Network/PeerConnectionManager.cs:596:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) for token {token} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:598:            Diagnostic.Debug($"Inbound {(incomingConnection.Obfuscated ? "obfuscated " : string.Empty)}transfer connection to {username} ({connection.IPEndPoint}) for token {token} handed off. (old: {incomingConnection.Id}, new: {connection.Id})");
src/Network/PeerConnectionManager.cs:609:                var msg = $"Failed to establish an inbound transfer connection to {username} ({incomingConnection.IPEndPoint}) for token {token}: {ex.Message}";
src/Network/PeerConnectionManager.cs:615:            Diagnostic.Debug($"Transfer connection to {username} ({connection.IPEndPoint}) for token {remoteToken} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:621:        ///     pierces the remote peer's firewall, and retrieves the remote token.
src/Network/PeerConnectionManager.cs:624:        /// <returns>The operation context, including the new connection and the associated remote token.</returns>
src/Network/PeerConnectionManager.cs:630:            Diagnostic.Debug($"Attempting inbound indirect {(useObfuscated ? "obfuscated " : string.Empty)}transfer connection to {connectToPeerResponse.Username} ({endPoint}) for token {connectToPeerResponse.Token}");
src/Network/PeerConnectionManager.cs:641:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for token {connectToPeerResponse.Token} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:663:                    Diagnostic.Debug($"Falling back to regular inbound indirect transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for token {connectToPeerResponse.Token}");
src/Network/PeerConnectionManager.cs:675:            Diagnostic.Debug($"{(useObfuscated ? "Obfuscated t" : "T")}ransfer connection to {connectToPeerResponse.Username} ({endPoint}) for token {connectToPeerResponse.Token} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:680:        ///     Gets a new transfer connection to the specified <paramref name="username"/> using the specified <paramref name="token"/>.
src/Network/PeerConnectionManager.cs:685:        /// <param name="token">The token with which to initialize the connection.</param>
src/Network/PeerConnectionManager.cs:686:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/PeerConnectionManager.cs:688:        public async Task<IConnection> GetTransferConnectionAsync(string username, IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken)
src/Network/PeerConnectionManager.cs:701:            var direct = GetTransferConnectionOutboundDirectAsync(ipEndPoint, token, directLinkedCts.Token);
src/Network/PeerConnectionManager.cs:706:                obfuscated = GetTransferConnectionOutboundObfuscatedDirectAsync(obfuscatedEndPoint, token, obfuscatedLinkedCts.Token);
src/Network/PeerConnectionManager.cs:709:            var indirect = GetTransferConnectionOutboundIndirectAsync(username, token, indirectLinkedCts.Token);
src/Network/PeerConnectionManager.cs:740:                        var request = new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Transfer, token).ToByteArray();
src/Network/PeerConnectionManager.cs:744:                    var tokenBytes = new byte[4];
src/Network/PeerConnectionManager.cs:745:                    BinaryPrimitives.WriteInt32LittleEndian(tokenBytes, token);
src/Network/PeerConnectionManager.cs:746:                    await connection.WriteAsync(tokenBytes, cancellationToken).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:956:            Diagnostic.Debug($"Soliciting indirect message connection to {username} with token {solicitationToken}");
src/Network/PeerConnectionManager.cs:995:                Diagnostic.Debug($"Failed to establish an indirect message connection to {username} with token {solicitationToken}: {ex.Message}");
src/Network/PeerConnectionManager.cs:1004:        private Task<IConnection> GetTransferConnectionOutboundDirectAsync(IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken)
src/Network/PeerConnectionManager.cs:1005:            => GetTransferConnectionOutboundDirectCoreAsync(ipEndPoint, token, cancellationToken, obfuscated: false);
src/Network/PeerConnectionManager.cs:1007:        private Task<IConnection> GetTransferConnectionOutboundObfuscatedDirectAsync(IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken)
src/Network/PeerConnectionManager.cs:1008:            => GetTransferConnectionOutboundDirectCoreAsync(ipEndPoint, token, cancellationToken, obfuscated: true);
src/Network/PeerConnectionManager.cs:1010:        private async Task<IConnection> GetTransferConnectionOutboundDirectCoreAsync(IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken, bool obfuscated)
src/Network/PeerConnectionManager.cs:1017:            Diagnostic.Debug($"Attempting {(obfuscated ? "obfuscated " : string.Empty)}direct transfer connection for token {token} to {ipEndPoint}");
src/Network/PeerConnectionManager.cs:1024:            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection for token {token} to {ipEndPoint} disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1032:                Diagnostic.Debug($"Failed to establish a {(obfuscated ? "obfuscated " : string.Empty)}direct transfer connection for token {token} to ({ipEndPoint}): {ex.Message}");
src/Network/PeerConnectionManager.cs:1037:            Diagnostic.Debug($"{(obfuscated ? "Obfuscated d" : "D")}irect transfer connection for {token} to {connection.IPEndPoint} established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1041:        private async Task<IConnection> GetTransferConnectionOutboundIndirectAsync(string username, int token, CancellationToken cancellationToken)
src/Network/PeerConnectionManager.cs:1043:            Diagnostic.Debug($"Soliciting indirect transfer connection to {username} with token {token}");
src/Network/PeerConnectionManager.cs:1072:                connection.Disconnected += (sender, e) => Diagnostic.Debug($"Transfer connection for token {token} ({incomingConnection.IPEndPoint}) disconnected: {e.Exception?.Message ?? e.Message}. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1074:                Diagnostic.Debug($"Indirect transfer connection for {token} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
src/Network/PeerConnectionManager.cs:1079:                Diagnostic.Debug($"Failed to establish an indirect transfer connection to {username} with token {token}: {ex.Message}");
src/Messaging/Messages/Server/UserSearchRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserSearchRequest.cs:38:        /// <param name="token">The unique token for the search.</param>
src/Messaging/Messages/Server/UserSearchRequest.cs:39:        public UserSearchRequest(string username, string searchText, int token)
src/Messaging/Messages/Server/UserSearchRequest.cs:41:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "search token");
src/Messaging/Messages/Server/UserSearchRequest.cs:45:            Token = token;
src/Messaging/Messages/Server/UserSearchRequest.cs:54:        ///     Gets the unique token for the search.
src/Messaging/Messages/Server/SearchRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SearchRequest.cs:37:        /// <param name="token">The unique token for the search.</param>
src/Messaging/Messages/Server/SearchRequest.cs:38:        public SearchRequest(string searchText, int token)
src/Messaging/Messages/Server/SearchRequest.cs:40:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "search token");
src/Messaging/Messages/Server/SearchRequest.cs:43:            Token = token;
src/Messaging/Messages/Server/SearchRequest.cs:52:        ///     Gets the unique token for the search.
src/Network/MessageConnectionEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomSearchRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomSearchRequest.cs:38:        /// <param name="token">The unique token for the search.</param>
src/Messaging/Messages/Server/RoomSearchRequest.cs:39:        public RoomSearchRequest(string roomName, string searchText, int token)
src/Messaging/Messages/Server/RoomSearchRequest.cs:41:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "search token");
src/Messaging/Messages/Server/RoomSearchRequest.cs:45:            Token = token;
src/Messaging/Messages/Server/RoomSearchRequest.cs:59:        ///     Gets the unique token for the search.
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:206:        public void Creates_Diagnostic_On_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:212:            var message = new PeerInit(username, Constants.ConnectionType.Peer, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:229:        public async Task Creates_Diagnostic_On_PierceFirewall(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:238:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:255:        public async Task Adds_Provisional_Peer_Connection_On_Unknown_PierceFirewall(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:259:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:270:            var expectedUsername = $"pierce-{token}-{endpoint.Address}:{endpoint.Port}";
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:278:        public async Task Adds_Provisional_Obfuscated_Peer_Connection_On_Unknown_Obfuscated_PierceFirewall(IPEndPoint endpoint, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:282:            var message = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:293:            var expectedUsername = $"pierce-{token}-{endpoint.Address}:{endpoint.Port}";
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:303:        public void Creates_Diagnostic_On_Peer_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:307:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:316:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:329:        public void Creates_Diagnostic_On_Distributed_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:333:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:342:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:355:        public void Adds_Peer_Connection_On_Peer_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:359:            var message = new PeerInit(username, Constants.ConnectionType.Peer, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:375:        public void Adds_Transfer_Connection_On_Transfer_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:379:            var message = new PeerInit(username, Constants.ConnectionType.Transfer, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:390:            mocks.PeerConnectionManager.Verify(m => m.GetTransferConnectionAsync(username, token, It.IsAny<IConnection>()));
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:395:        public void Completes_DirectTransfer_Wait_On_Transfer_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:399:            var message = new PeerInit(username, Constants.ConnectionType.Transfer, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:410:            mocks.PeerConnectionManager.Setup(m => m.GetTransferConnectionAsync(username, token, It.IsAny<IConnection>()))
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:411:                .Returns(Task.FromResult((newTransfer.Object, token)));
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:418:            var key = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:424:        public void Disconnects_DirectTransfer_On_Missing_Wait(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:428:            var message = new PeerInit(username, Constants.ConnectionType.Transfer, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:439:            mocks.PeerConnectionManager.Setup(m => m.GetTransferConnectionAsync(username, token, It.IsAny<IConnection>()))
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:440:                .Returns(Task.FromResult((newTransfer.Object, token)));
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:447:            var key = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:450:            newTransfer.Verify(m => m.Disconnect("Transfer connection rejected: unknown token", null), Times.Once);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:455:        public void Adds_Distributed_Connection_On_Distributed_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:459:            var message = new PeerInit(username, Constants.ConnectionType.Distributed, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:475:        public void Adds_Obfuscated_Distributed_Connection_On_Distributed_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:479:            var message = new PeerInit(username, Constants.ConnectionType.Distributed, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:496:        public void Accepts_Obfuscated_Transfer_PeerInit(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:500:            var message = new PeerInit(username, Constants.ConnectionType.Transfer, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:512:            mocks.PeerConnectionManager.Verify(m => m.GetTransferConnectionAsync(username, token, It.IsAny<IConnection>()), Times.Once);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:518:        public void Completes_Solicited_Peer_Connection_On_Peer_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:522:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:531:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:538:            var expectedKey = new WaitKey(Constants.WaitKey.SolicitedPeerConnection, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:544:        public void Completes_Solicited_Obfuscated_Peer_Connection_On_Peer_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:548:            var message = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:557:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:565:            var expectedKey = new WaitKey(Constants.WaitKey.SolicitedPeerConnection, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:593:        public void Completes_Solicited_Distributed_Connection_On_Distributed_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:597:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:606:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:613:            var expectedKey = new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:619:        public void Completes_Solicited_Obfuscated_Distributed_Connection_On_Distributed_PierceFirewall(IPEndPoint endpoint, string username, int token)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:623:            var message = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:632:            dict.TryAdd(token, username);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:640:            var expectedKey = new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, username, token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:647:        public void Adds_Connection_On_SearchResponse_PierceFirewall(IPEndPoint endpoint, string username, int token, string query)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:649:            (string Username, int Token, string Query, SearchResponse SearchResponse) response = (username, token, query, null);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:652:            cache.Setup(m => m.TryGet(token, out response)).Returns(true);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:656:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:671:        public void Adds_Obfuscated_Connection_On_Obfuscated_SearchResponse_PierceFirewall(IPEndPoint endpoint, string username, int token, string query)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:673:            (string Username, int Token, string Query, SearchResponse SearchResponse) response = (username, token, query, null);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:676:            cache.Setup(m => m.TryGet(token, out response)).Returns(true);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:680:            var message = new PierceFirewall(token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:697:        public void Responds_To_Search_On_SearchResponse_PierceFirewall(IPEndPoint endpoint, string username, int token, string query)
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:699:            (string Username, int Token, string Query, SearchResponse SearchResponse) response = (username, token, query, null);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:702:            cache.Setup(m => m.TryGet(token, out response)).Returns(true);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:706:            var message = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Network/ListenerHandlerTests.cs:716:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(token), Times.Once);
src/Messaging/Messages/Server/LoginRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/LoginRequest.cs:38:        /// <param name="password">The password.</param>
src/Messaging/Messages/Server/LoginRequest.cs:39:        public LoginRequest(int minorVersion, string username, string password)
src/Messaging/Messages/Server/LoginRequest.cs:46:            Password = ProtocolArgumentValidator.RequireNotNull(password, nameof(password), "password");
src/Messaging/Messages/Server/LoginRequest.cs:52:        ///     Gets the MD5 hash of the username and password.
src/Messaging/Messages/Server/LoginRequest.cs:62:        ///     Gets the password.
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:36:        /// <param name="token">The unique connection token.</param>
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:39:        public ConnectToPeerRequest(int token, string username, string type)
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:41:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "connection token");
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:43:            Token = token;
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:49:        ///     Gets the unique connection token.
tests/Soulseek.Tests.Unit/Client/BrowseAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/CannotConnect.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/CannotConnect.cs:36:        /// <param name="token">The unique connection token.</param>
src/Messaging/Messages/Server/CannotConnect.cs:38:        public CannotConnect(int token, string username = null)
src/Messaging/Messages/Server/CannotConnect.cs:40:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "connection token");
src/Messaging/Messages/Server/CannotConnect.cs:42:            Token = token;
src/Messaging/Messages/Server/CannotConnect.cs:47:        ///     Gets the unique connection token.
src/Messaging/Messages/Server/CannotConnect.cs:71:            var token = reader.ReadInteger();
src/Messaging/Messages/Server/CannotConnect.cs:80:            return new CannotConnect(token, username);
src/Network/MessageConnection.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/MessageConnection.cs:169:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
tests/Soulseek.Tests.Unit/Client/UnwatchUserAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ProtocolCountReader.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/MessageUsersCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ItemSimilarUsersResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ItemRecommendationsResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionKeyTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/ListenerHandler.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/ListenerHandler.cs:157:                        // check to see if we are expecting this token, and if so complete the wait and start the upload
src/Network/ListenerHandler.cs:166:                            Diagnostic.Debug($"Unexpected transfer connection for token {peerInit.Token} from {peerInit.Username} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:167:                            transferConnection.Disconnect("Transfer connection rejected: unknown token");
src/Network/ListenerHandler.cs:185:                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it
src/Network/ListenerHandler.cs:189:                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:196:                            Diagnostic.Debug($"Obfuscated distributed PierceFirewall with token {pierceFirewall.Token} accepted from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}); completing solicited distributed wait. (id: {connection.Id})");
src/Network/ListenerHandler.cs:199:                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPEndPoint.Address}:{listenerPort}) (id: {connection.Id})");
src/Network/ListenerHandler.cs:222:                        // Search responders behind a firewall can arrive with a bare PierceFirewall token before sending the
src/Network/ListenerHandler.cs:224:                        // username and search token, so keep the socket alive long enough for PeerMessageHandler to process it.
src/Network/ListenerHandler.cs:226:                        Diagnostic.Debug($"Unknown PierceFirewall with token {pierceFirewall.Token} accepted as provisional peer message connection from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port} (id: {connection.Id})");
src/Messaging/Messages/Server/BranchRootCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ItemRecommendationsRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.CouncilAnalyzers.Tests/TaintToFilePathAnalyzerTests.cs:84:            var path = Path.Combine(root, name);
tests/Soulseek.CouncilAnalyzers.Tests/TaintToFilePathAnalyzerTests.cs:85:            return Directory.EnumerateFiles(path).ToArray();
tests/Soulseek.CouncilAnalyzers.Tests/TaintToFilePathAnalyzerTests.cs:132:            var path = PathSafety.ResolveContainedPath(root, reader.ReadString());
tests/Soulseek.CouncilAnalyzers.Tests/TaintToFilePathAnalyzerTests.cs:133:            return File.ReadAllText(path);
tests/Soulseek.CouncilAnalyzers.Tests/TaintToFilePathAnalyzerTests.cs:152:        public string Read(string path) => File.ReadAllText(path);
tests/Soulseek.Tests.Unit/Client/StopPublicChatAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/BranchLevelCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PublicChatMessageNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SimilarUsersResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IPeerConnectionManager.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IPeerConnectionManager.cs:24://     Modified: Added type-1 obfuscated peer-message connection paths.
src/Network/IPeerConnectionManager.cs:76:        ///     <paramref name="filename"/> and <paramref name="remoteToken"/>.
src/Network/IPeerConnectionManager.cs:83:        /// <param name="filename">The filename associated with the expected transfer.</param>
src/Network/IPeerConnectionManager.cs:84:        /// <param name="remoteToken">The remote token associated with the expected transfer.</param>
src/Network/IPeerConnectionManager.cs:85:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/IPeerConnectionManager.cs:87:        Task<IConnection> AwaitTransferConnectionAsync(string username, string filename, int remoteToken, CancellationToken cancellationToken);
src/Network/IPeerConnectionManager.cs:115:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/IPeerConnectionManager.cs:125:        /// <param name="solicitationToken">The optional token for the indirect connection solicitation.</param>
src/Network/IPeerConnectionManager.cs:126:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/IPeerConnectionManager.cs:134:        /// <param name="token">The token with which the firewall was pierced.</param>
src/Network/IPeerConnectionManager.cs:137:        Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(string username, int token, IConnection incomingConnection);
src/Network/IPeerConnectionManager.cs:141:        ///     pierces the remote peer's firewall, and retrieves the remote token.
src/Network/IPeerConnectionManager.cs:144:        /// <returns>The operation context, including the new connection and the associated remote token.</returns>
src/Network/IPeerConnectionManager.cs:148:        ///     Gets a new transfer connection to the specified <paramref name="username"/> using the specified <paramref name="token"/>.
src/Network/IPeerConnectionManager.cs:153:        /// <param name="token">The token with which to initialize the connection.</param>
src/Network/IPeerConnectionManager.cs:154:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/IPeerConnectionManager.cs:156:        Task<IConnection> GetTransferConnectionAsync(string username, IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken);
src/Messaging/Messages/Server/PrivilegedUserNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/AcknowledgePrivilegeNotificationCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/MessageConnectionEventArgsTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SimilarUsersRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/AcknowledgePrivateMessageCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserInterestsResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:48:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", outputStreamFactory: null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:56:        [Fact(DisplayName = "DownloadAsync throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:63:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", "local", token: -1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:67:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:72:        [Fact(DisplayName = "DownloadAsync stream throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:80:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream), token: -1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:84:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:99:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:116:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, "filename", () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:124:        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad remote filename")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:128:        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Remote_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:132:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:140:        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad local filename")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:144:        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Local_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:148:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "remote", filename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:156:        [Theory(DisplayName = "DownloadAsync stream throws ArgumentException given bad filename")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:160:        public async Task DownloadAsync_Stream_Throws_ArgumentException_Given_Bad_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:165:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:295:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:310:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:326:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:343:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:352:        [Theory(DisplayName = "DownloadAsync throws DuplicateTokenException when token used by download"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:364:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:368:                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:373:        [Theory(DisplayName = "DownloadAsync throws DuplicateTokenException when token used by upload"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:385:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:389:                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:394:        [Fact(DisplayName = "DownloadAsync stream throws DuplicateTokenException when token used by download")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:407:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:411:                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:416:        [Fact(DisplayName = "DownloadAsync stream throws DuplicateTokenException when token used by upload")]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:429:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:433:                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:438:        [Theory(DisplayName = "DownloadAsync throws DuplicateTransferException when an existing download matches the username and filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:439:        public async Task DownloadAsync_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_The_Username_And_Filename(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:446:                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:449:                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:454:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:458:                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:464:        public async Task DownloadAsync_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_A_Unique_Key(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:471:                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:475:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:479:                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:485:        public async Task DownloadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Username(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:496:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:504:        [Theory(DisplayName = "DownloadAsync does not throw DuplicateTransferException when an existing download matches only the filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:505:        public async Task DownloadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Filename(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:512:                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, "different", filename, 0));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:516:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:524:        [Theory(DisplayName = "DownloadAsync stream throws DuplicateTransferException when an existing download matches the username and filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:525:        public async Task DownloadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_The_Username_And_Filename(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:533:                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:536:                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:541:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:545:                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:551:        public async Task DownloadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_A_Unique_Key(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:559:                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:563:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:567:                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:573:        public async Task DownloadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Username(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:585:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:593:        [Theory(DisplayName = "DownloadAsync stream does not throw DuplicateTransferException when an existing download matches only the filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:594:        public async Task DownloadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Filename(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:602:                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, "different", filename, 0));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:606:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:615:        public async Task DownloadAsync_Stream_Substitutes_CancellationToken_Given_Null(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:624:                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:632:        public async Task DownloadAsync_Stream_Uses_Given_CancellationToken(string username, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:642:                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), cancellationToken: cancellationToken));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:650:        public async Task DownloadAsync_Substitutes_CancellationToken_Given_Null(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:658:                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:666:        public async Task DownloadAsync_Uses_Given_CancellationToken(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:675:                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, cancellationToken: cancellationToken));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:683:        public async Task DownloadAsync_Throws_UserOfflineException_On_User_Offline(string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:701:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, localFilename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:710:        public async Task DownloadAsync_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:730:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, localFilename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:739:        public async Task DownloadAsync_Stream_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:760:                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, () => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:769:        public async Task DownloadToFileAsync_Throws_TransferException_When_WriteAsync_Throws(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:795:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:805:        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferResponse_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:827:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:836:        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferResponse_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:840:            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:860:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:869:        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferRequest_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:873:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:874:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:894:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:903:        public async Task DownloadToFileAsync_Throws_TransferException_On_Download_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:907:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:908:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:933:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:940:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:949:        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:953:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:955:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:981:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:990:        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Read_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:994:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:995:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:997:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1022:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1029:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1038:        public async Task DownloadToFileAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1042:            var response = new TransferResponse(token, "File not shared."); // not shared
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1043:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1045:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1070:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1077:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1086:        public async Task DownloadToFileAsync_Sets_Exception_Property_When_transfer_Fails(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1090:            var response = new TransferResponse(token, "File not shared."); // not shared
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1091:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1093:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1118:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1130:                    filename,
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1134:                    token,
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1158:        public async Task DownloadToFileAsync_Raises_Expected_Events_On_Success(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1162:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1163:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1165:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1192:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1212:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1258:        public async Task DownloadToFileAsync_Uses_Size_From_TransferResponse_Given_Null_Size_When_Skipping_Queue(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1262:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1263:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1265:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1290:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1304:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, null, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1319:        public async Task DownloadToFileAsync_Throws_On_Size_Mismatch_When_Skipping_Queue(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1323:            var response = new TransferResponse(token, remoteSize); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1324:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1326:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1351:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1365:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1376:        public async Task DownloadToFileAsync_Sets_State_To_Aborted_On_Size_Mismatch(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1380:            var response = new TransferResponse(token, remoteSize); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1381:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1383:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1408:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1422:                _ = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1430:        public async Task DownloadToFileAsync_Writes_Offset_To_Connection(string username, IPEndPoint endpoint, string filename, string localFilename, long offset, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1432:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1433:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1435:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1460:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1467:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, offset, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1475:        public async Task DownloadToStreamAsync_Does_Not_Throw_If_Stream_Position_Getter_Throws_In_Finally_Block(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1479:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1480:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1482:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1507:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1519:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1529:        public async Task DownloadToStreamAsync_Disposes_Output_Stream_Given_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1533:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1534:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1536:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1561:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1570:                await s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1586:        public async Task DownloadToStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Disposal_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1590:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1591:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1593:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1618:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1632:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1647:        public async Task DownloadToStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Flush_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1651:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1652:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1654:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1679:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1693:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1708:        public async Task DownloadToStreamAsync_Completes_Following_Normal_Transfer_Connection_Disconnect(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1712:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1713:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1715:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1740:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1748:                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1756:                Assert.Equal(token, transfer.Token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1757:                Assert.Equal(filename, transfer.Filename);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1763:        public async Task DownloadToStreamAsync_Releases_Unique_Key_On_Success(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1767:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1768:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1770:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1795:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1810:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1820:        public async Task DownloadToStreamAsync_Releases_Unique_Key_On_Failure(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1824:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1825:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1827:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1852:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1866:                queued.TryAdd(token, new TransferInternal(TransferDirection.Download, "foo", "bar", token));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1872:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1883:        public async Task DownloadToStreamAsync_Invokes_Reporter_Delegate_Passed_In_Options(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1887:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1888:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1890:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1920:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1939:                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1952:        [Theory(DisplayName = "DownloadToStreamAsync returns unused tokens to DownloadTokenBucket"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1953:        public async Task DownloadToStreamAsync_Returns_Unused_Tokens_To_DownloadTokenBucket(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1957:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1958:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1960:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:1990:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2011:                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2027:        public async Task DownloadToStreamAsync_Does_Not_Throw_If_Reporter_Delegate_From_Options_Is_Null(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2031:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2032:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2034:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2064:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2074:                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2086:        public async Task DownloadToStreamAsync_Retrieves_Grant_From_Governor_Passed_In_Options_Then_DownloadTokenBucket(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2093:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2094:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2096:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2126:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2139:                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2148:                // be used to take tokens from the bucket.
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2155:        public async Task DownloadToStreamAsync_Throws_DuplicateTransferException_When_Failing_To_Insert_UniqueKeyDictionary(string username, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2168:                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2171:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, null, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2180:        public async Task DownloadToStreamAsync_Throws_DuplicateTokenException_When_Failing_To_Insert_DownloadDictionary(string username, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2196:                queued.TryAdd(token, new TransferInternal(TransferDirection.Download, "foo", "bar", token));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2200:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, null, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2211:        [Theory(DisplayName = "DownloadToStreamAsync throws DuplicateTokenException when token is registered to upload"), AutoData]
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2212:        public async Task DownloadToStreamAsync_Throws_DuplicateTokenException_When_Token_Is_Registered_To_Upload(string username, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2228:                queued.TryAdd(token, new TransferInternal(TransferDirection.Upload, "foo", "bar", token));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2232:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, null, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2238:                Assert.True(queued.ContainsKey(token));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2244:        public async Task DownloadToStreamAsync_Throws_TimeoutException_On_Unexpected_Transfer_Connection_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2248:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2249:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2251:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2256:            // capture the cancellation token passed to read so we can ensure it is cancelled
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2286:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2294:                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2308:        public async Task DownloadToStreamAsync_Throws_OperationCanceledException_On_Unexpected_Transfer_Connection_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2312:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2313:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2315:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2320:            // capture the cancellation token passed to read so we can ensure it is cancelled
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2350:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2358:                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2376:        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Unexpected_Transfer_Connection_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2380:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2381:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2383:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2388:            // capture the cancellation token passed to read so we can ensure it is cancelled
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2418:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2426:                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2450:        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Remote_Client_DownloadFailed_Message(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2454:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2455:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2457:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2462:            // capture the cancellation token passed to read so we can ensure it is cancelled
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2492:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2502:                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2508:                peerMessageHandler.Raise(m => m.DownloadFailed += null, peerMessageHandler.Object, new DownloadFailedEventArgs(username, filename));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2525:        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Remote_Client_DownloadDenied_Message(string username, IPEndPoint endpoint, string filename, int token, int size, string denialMessage)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2529:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2530:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2532:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2537:            // capture the cancellation token passed to read so we can ensure it is cancelled
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2567:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2577:                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2583:                peerMessageHandler.Raise(m => m.DownloadDenied += null, peerMessageHandler.Object, new DownloadDeniedEventArgs(username, filename, denialMessage));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2599:        public async Task DownloadToStreamAsync_Does_Not_Dispose_Output_Stream_Given_No_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2603:            var response = new TransferResponse(token, size); // allowed, will start download immediately
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2604:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2606:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2633:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2642:                await s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2655:        public async Task DownloadToFileAsync_Uses_Size_From_TransferResponse_When_Queued(string username, IPEndPoint endpoint, string remoteFilename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2659:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2660:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2662:            var request = new TransferRequest(TransferDirection.Download, token, remoteFilename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2687:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, remoteFilename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2701:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, remoteFilename, localFilename, null, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2716:        public async Task DownloadToFileAsync_Throws_TransferSizeMismatchException_On_Mismatch_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2720:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2721:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2723:            var request = new TransferRequest(TransferDirection.Download, token, filename, remoteSize);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2748:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2762:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2773:        public async Task DownloadToFileAsync_Sets_Transfer_State_To_On_Mismatch_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2777:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2778:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2780:            var request = new TransferRequest(TransferDirection.Download, token, filename, remoteSize);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2805:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2819:                _ = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2827:        public async Task DownloadToFileAsync_Uses_Given_Size_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2831:            var queueResponse = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2832:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2834:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2859:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2873:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, null, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2888:        public async Task DownloadToFileAsync_Initiates_A_Transfer_If_Remote_Client_Does_Not_Initiate(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2892:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2893:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2895:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2920:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2922:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2931:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(stateChanged: (e) => fired = true), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2936:            connManager.Verify(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()), Times.Once);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2941:        public async Task DownloadToFileAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2945:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2946:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2948:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2973:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2982:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(stateChanged: (e) => fired = true), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2990:        public async Task DownloadToFileAsync_Succeeds_If_TransferStateChanged_Handler_Throws(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2994:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2995:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:2997:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3022:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3030:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3038:        public async Task DownloadToFileAsync_Raises_DownloadProgressUpdated_Event_On_Data_Read(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int progressSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3042:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3043:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3045:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3075:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3086:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3101:        public async Task DownloadToFileAsync_Succeeds_If_TransferProgressUpdated_Handler_Throws(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int progressSize)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3105:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3106:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3108:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3138:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3146:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3154:        public async Task DownloadToFileAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3158:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3159:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3161:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3169:                .Returns(Task.FromResult(BitConverter.GetBytes(token)));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3190:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3199:                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(progressUpdated: (e) => fired = true), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3207:        public async Task DownloadToFileAsync_Opens_Stream_With_FileMode_Create_If_StartOffset_Is_0(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3211:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3212:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3214:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3222:                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3244:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3257:                    await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3266:        public async Task DownloadToFileAsync_Opens_Stream_With_FileMode_Append_If_StartOffset_Is_Greater_Than_0(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3270:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3271:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3273:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3281:                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3303:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3316:                    await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 1, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3325:        public async Task DownloadToFileAsync_Raises_Download_Events_On_Failure(string username, IPEndPoint endpoint, string remoteFilename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3329:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3330:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3332:            var request = new TransferRequest(TransferDirection.Download, token, remoteFilename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3363:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3377:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, remoteFilename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3390:        public async Task DownloadToFileAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3394:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3395:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3397:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3428:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3442:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3454:        public async Task DownloadToFileAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, string localFilename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3475:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3486:        public async Task DownloadToFileAsync_Throws_TransferException_And_ConnectionException_On_Transfer_Exception(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3490:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3491:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3493:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3521:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3528:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3539:        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3543:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3544:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3546:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3574:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3581:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3590:        public async Task DownloadToFileAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3594:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3595:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3597:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3625:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3632:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3641:        public async Task DownloadToFileAsync_Throws_TransferRejectedException_On_Transfer_Rejection(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3645:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3646:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3673:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3680:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3689:        public async Task DownloadToFileAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3693:            var response = new TransferResponse(token, "Queued");
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3694:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3696:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3721:            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3728:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3739:        public async Task DownloadToStreamAsync_Reports_StartOffset_In_Initial_Progress_When_StartOffset_Greater_Than_0(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3746:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3747:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3749:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3778:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3792:                await s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, startOffset, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3802:        public async Task DownloadToStreamAsync_Reports_Full_File_Size_In_Final_Progress_When_StartOffset_Greater_Than_0(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3813:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3814:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3816:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3847:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3861:                await s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, startOffset, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3874:        public async Task DownloadToStreamAsync_Reports_Stream_Position_Not_Position_Plus_StartOffset_In_Final_Progress_On_Error_When_StartOffset_Greater_Than_0(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3884:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3885:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3887:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3918:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3932:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, startOffset, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3946:        public async Task DownloadToStreamAsync_Throws_TransferStreamException_If_StartOffset_NonZero_And_SeekOutputStreamAutomatically_And_Stream_Not_Seekable(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3953:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3954:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3955:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3977:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3989:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult(stream.Object)), (long?)size, startOffset, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:3999:        public async Task DownloadToStreamAsync_Does_Not_Throw_And_Seeks_Stream_If_StartOffset_NonZero_And_SeekOutputStreamAutomatically_And_Stream_Seekable(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4006:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4007:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4008:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4034:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4047:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult(stream.Object)), (long?)size, startOffset, token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4056:        public async Task DownloadToStreamAsync_Does_Not_Check_CanSeek_Or_Seek_Stream_If_SeekOutputStreamAutomatically_Is_False(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4063:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4064:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4065:            var request = new TransferRequest(TransferDirection.Download, token, filename, size);
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4091:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/DownloadAsyncTests.cs:4102:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult(stream.Object)), (long?)size, startOffset, token, txoptions, null));
src/Network/IMessageConnection.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IMessageConnection.cs:104:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Messaging/Messages/Server/AcceptChildrenCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivilegeNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/AddPrivateRoomModeratorAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserInterestsRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IListenerHandler.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/Zlib.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/PrivateRoomUserListNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/StartPublicChatAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomAddUser.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/LoginResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/LoginResponse.cs:44:        /// <param name="hash">The MD5 hash of the username and password.</param>
src/Messaging/Messages/Server/LoginResponse.cs:59:        ///     Gets the MD5 hash of the username and password.
src/Messaging/Messages/Server/RecommendationsResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomToggle.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/ZStreamException.cs:2:// http://www.componentace.com
src/Network/IDistributedConnectionManager.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IDistributedConnectionManager.cs:144:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/IDistributedConnectionManager.cs:191:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Messaging/Messages/Server/LeaveRoomResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomAddOperator.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomRemoveUser.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RecommendationsRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/LeaveRoomRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/InterestCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomRemoveOperator.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateMessageNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/IConnectionFactory.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/JoinRoomResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/ZStream.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/PrivateRoomOwnedListNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/WatchUserResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateMessageCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/TransferResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/TransferResponse.cs:38:        /// <param name="token">The unique token for the transfer.</param>
src/Messaging/Messages/Peer/TransferResponse.cs:40:        public TransferResponse(int token, string message)
src/Messaging/Messages/Peer/TransferResponse.cs:42:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "transfer token");
src/Messaging/Messages/Peer/TransferResponse.cs:44:            Token = token;
src/Messaging/Messages/Peer/TransferResponse.cs:52:        /// <param name="token">The unique token for the transfer.</param>
src/Messaging/Messages/Peer/TransferResponse.cs:54:        public TransferResponse(int token, long fileSize)
src/Messaging/Messages/Peer/TransferResponse.cs:56:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "transfer token");
src/Messaging/Messages/Peer/TransferResponse.cs:58:            Token = token;
src/Messaging/Messages/Peer/TransferResponse.cs:66:        /// <param name="token">The unique token for the transfer.</param>
src/Messaging/Messages/Peer/TransferResponse.cs:67:        public TransferResponse(int token)
src/Messaging/Messages/Peer/TransferResponse.cs:69:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "transfer token");
src/Messaging/Messages/Peer/TransferResponse.cs:71:            Token = token;
src/Messaging/Messages/Peer/TransferResponse.cs:91:        ///     Gets the unique token for the transfer.
src/Messaging/Messages/Peer/TransferResponse.cs:110:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/TransferResponse.cs:119:                return new TransferResponse(token, fileSize);
src/Messaging/Messages/Peer/TransferResponse.cs:124:                return new TransferResponse(token, msg);
src/Messaging/Messages/Peer/TransferResponse.cs:127:            return new TransferResponse(token);
src/Messaging/Messages/Server/JoinRoomRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomDropOwnershipCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/WatchUserRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SetStatusAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/IntegerResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/PrivateRoomDropMembershipCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserStatusResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ParentsIPCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/TransferRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/TransferRequest.cs:39:        /// <param name="token">The unique token for the transfer.</param>
src/Messaging/Messages/Peer/TransferRequest.cs:40:        /// <param name="filename">The name of the file being transferred.</param>
src/Messaging/Messages/Peer/TransferRequest.cs:42:        public TransferRequest(TransferDirection direction, int token, string filename, long fileSize = 0)
src/Messaging/Messages/Peer/TransferRequest.cs:44:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "transfer token");
src/Messaging/Messages/Peer/TransferRequest.cs:47:            Token = token;
src/Messaging/Messages/Peer/TransferRequest.cs:48:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/TransferRequest.cs:68:        ///     Gets the unique token for the transfer.
src/Messaging/Messages/Peer/TransferRequest.cs:89:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/TransferRequest.cs:90:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/TransferRequest.cs:99:            return new TransferRequest(direction, token, filename, fileSize);
src/Messaging/Compression/ZOutputStream.cs:2:// http://www.componentace.com
src/Messaging/Compression/ZOutputStream.cs:98:		// https://zlib.net/zlib_how.html
src/Messaging/Compression/ZOutputStream.cs:127:		//UPGRADE_TODO: The differences in the Expected value  of parameters for method 'WriteByte'  may cause compilation errors.  'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1092_3"'
src/Messaging/Compression/ZOutputStream.cs:228:		//UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:233:		//UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:237:		//UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:242:		//UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:251:		//UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:260:		//UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:269:		//UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/Messaging/Compression/ZOutputStream.cs:278:		//UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
src/ISearchResponseCache.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ISearchResponseCache.cs:34:        /// <param name="responseToken">The token for which the response is to be added or updated.</param>
src/ISearchResponseCache.cs:41:        /// <param name="responseToken">The token for the cached response.</param>
src/ISearchResponseCache.cs:49:        /// <param name="responseToken">The token for the cached response to remove.</param>
src/Messaging/Messages/Server/NewPassword.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/NewPassword.cs:27:    ///     The command and response to a password change.
src/Messaging/Messages/Server/NewPassword.cs:34:        /// <param name="password">The new password.</param>
src/Messaging/Messages/Server/NewPassword.cs:35:        public NewPassword(string password)
src/Messaging/Messages/Server/NewPassword.cs:37:            Password = ProtocolArgumentValidator.RequireNotNull(password, nameof(password), "password");
src/Messaging/Messages/Server/NewPassword.cs:41:        ///     Gets the new password.
src/Messaging/Messages/Server/NewPassword.cs:60:            var password = reader.ReadString();
src/Messaging/Messages/Server/NewPassword.cs:62:            return new NewPassword(password);
src/Messaging/Messages/Server/UserStatusRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:38:        /// <param name="filename">The filename which was checked.</param>
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:40:        public PlaceInQueueResponse(string filename, int placeInQueue)
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:42:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:47:        ///     Gets the filename which failed to be queued.
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:71:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:76:            return new PlaceInQueueResponse(filename, placeInQueue);
tests/Soulseek.Tests.Unit/Client/AddPrivateRoomMemberAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/HaveNoParentsCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:499:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:547:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:602:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:651:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:700:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
tests/Soulseek.Tests.Unit/Network/MessageConnectionTests.cs:748:                .Callback<Memory<byte>, CancellationToken>((bytes, token) =>
src/Messaging/Compression/ZInputStream.cs:2:// http://www.componentace.com
src/ISearchResponder.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ISearchResponder.cs:53:        /// <param name="responseToken">The token matching the cached response to discard.</param>
src/ISearchResponder.cs:61:        /// <param name="token">The token for the search request.</param>
src/ISearchResponder.cs:64:        Task<bool> TryRespondAsync(string username, int token, string query);
src/ISearchResponder.cs:69:        /// <param name="responseToken">The token matching the cached response to send.</param>
src/Messaging/Messages/Server/UserStatisticsResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/NetInfoNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/FileAttributeType.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/FolderContentsResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/FolderContentsResponse.cs:40:        /// <param name="token">The unique token for the request.</param>
src/Messaging/Messages/Peer/FolderContentsResponse.cs:43:        public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:45:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "folder contents token");
src/Messaging/Messages/Peer/FolderContentsResponse.cs:59:            Token = token;
src/Messaging/Messages/Peer/FolderContentsResponse.cs:83:        ///     Gets the token for the response.
src/Messaging/Messages/Peer/FolderContentsResponse.cs:104:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/FolderContentsResponse.cs:114:            return new FolderContentsResponse(token, rootDirectory, directoryList);
src/Messaging/Messages/Server/GlobalMessageNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserStatisticsRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ChildDepthCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/FolderContentsRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/FolderContentsRequest.cs:36:        /// <param name="token">The unique token for the request.</param>
src/Messaging/Messages/Peer/FolderContentsRequest.cs:38:        public FolderContentsRequest(int token, string directoryName)
src/Messaging/Messages/Peer/FolderContentsRequest.cs:40:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "folder contents token");
src/Messaging/Messages/Peer/FolderContentsRequest.cs:43:            Token = token;
src/Messaging/Messages/Peer/FolderContentsRequest.cs:52:        ///     Gets the unique token for the request.
src/Messaging/Messages/Peer/FolderContentsRequest.cs:71:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/FolderContentsRequest.cs:74:            return new FolderContentsRequest(token, directoryName);
tests/Soulseek.Tests.Unit/Client/GetUserEndPointAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserPrivilegesRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SetSharedCountsAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/GivePrivilegesCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/CheckPrivilegesRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/Tree.cs:2:// http://www.componentace.com
tests/Soulseek.Tests.Unit/Client/AcknowledgePrivilegeNotificationAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserPrivilegeResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/CannotJoinRoomNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UserInfoRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Diagnostics/IDiagnosticGenerator.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserLeftRoomNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UploadFailed.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UploadFailed.cs:34:        /// <param name="filename">The filename which failed to be uploaded.</param>
src/Messaging/Messages/Peer/UploadFailed.cs:35:        public UploadFailed(string filename)
src/Messaging/Messages/Peer/UploadFailed.cs:37:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/UploadFailed.cs:41:        ///     Gets the filename which failed to be uploaded.
src/Messaging/Messages/Peer/UploadFailed.cs:60:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/UploadFailed.cs:62:            return new UploadFailed(filename);
src/Diagnostics/IDiagnosticFactory.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Diagnostics/DiagnosticEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UploadDenied.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/UploadDenied.cs:34:        /// <param name="filename">The filename for which the upload was denied.</param>
src/Messaging/Messages/Peer/UploadDenied.cs:36:        public UploadDenied(string filename, string message)
src/Messaging/Messages/Peer/UploadDenied.cs:38:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/UploadDenied.cs:43:        ///     Gets the filename for which the upload was denied.
src/Messaging/Messages/Peer/UploadDenied.cs:67:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/UploadDenied.cs:70:            return new UploadDenied(filename, msg);
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:47:        /// <param name="token">The unique connection token.</param>
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:51:        public ConnectToPeerResponse(string username, string type, IPAddress ipAddress, int port, int token, bool isPrivileged, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:52:            : this(username, type, new IPEndPoint(ipAddress, port), token, isPrivileged, obfuscationType, obfuscatedPort)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:62:        /// <param name="token">The unique connection token.</param>
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:66:        public ConnectToPeerResponse(string username, string type, IPEndPoint endpoint, int token, bool isPrivileged, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:70:            Token = token;
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:121:        ///     Gets the unique connection token.
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:160:            var token = reader.ReadInteger();
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:179:            return new ConnectToPeerResponse(username, type, ipAddress, port, token, isPrivileged, obfuscationType, obfuscatedPort);
src/Diagnostics/GlobalDiagnostic.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/StaticTree.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/UserOfflineException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/BrowseResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Diagnostics/DiagnosticLevel.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/SearchResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/SearchResponseFactory.cs:55:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/SearchResponseFactory.cs:81:            return new SearchResponse(username, token, hasFreeUploadSlot: freeUploadSlots > 0, uploadSpeed, queueLength, fileList, lockedFileList);
tests/Soulseek.Tests.Unit/Client/SetRoomTickerAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/UserNotFoundException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:41:                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:58:                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, "filename", 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:67:        public async Task EnqueueUploadAsync_File_Returns_After_Upload_Enters_Queued_State(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:74:                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:82:        public async Task EnqueueUploadAsync_Stream_Returns_After_Upload_Enters_Queued_State(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:89:                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:97:        public async Task EnqueueUploadAsync_File_Throws_Cancellation_Before_Local_Queue(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:106:                var enqueueTask = s.EnqueueUploadAsync(username, filename, testFile.Path, token, new TransferOptions(), cancellationTokenSource.Token);
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:120:        public async Task EnqueueUploadAsync_Stream_Throws_Cancellation_Before_Local_Queue(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/EnqueueUploadAsyncTests.cs:129:                var enqueueTask = s.EnqueueUploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), token, new TransferOptions(), cancellationTokenSource.Token);
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:34:        /// <param name="filename">The name of the file being enqueued.</param>
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:35:        public QueueDownloadRequest(string filename)
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:37:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:60:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:61:            return new QueueDownloadRequest(filename);
src/Diagnostics/DiagnosticFactory.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/UserEndPointException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UserAddressResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/DistributedConnectionManager.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/DistributedConnectionManager.cs:190:        ///         lazy value is executed when we await the value shortly after.
src/Network/DistributedConnectionManager.cs:300:                        // because we cancelled any pending connection above, the Lazy<> function has completed executing and we
src/Network/DistributedConnectionManager.cs:425:                    // there is a very small chance that a connection will disconnect between the time it was filtered above and before this code executes.
src/Network/DistributedConnectionManager.cs:467:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/DistributedConnectionManager.cs:571:                    Diagnostic.Debug($"Child connection from {r.Username} ({r.IPEndPoint}) for token {r.Token} ignored; connection already exists.");
src/Network/DistributedConnectionManager.cs:594:                        // had timed out or failed, but before that connection was able to cancel the pending token this should be
src/Network/DistributedConnectionManager.cs:632:                Diagnostic.Debug($"Attempting {(useObfuscated ? "obfuscated " : string.Empty)}inbound indirect child connection to {r.Username} ({endPoint}) for token {r.Token}");
src/Network/DistributedConnectionManager.cs:662:                        // let everyone know this code is done executing and that .Value of the containing cache is safe to await
src/Network/DistributedConnectionManager.cs:767:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Network/DistributedConnectionManager.cs:1021:                Diagnostic.Debug($"Adding obfuscated direct parent candidate path to {username} ({obfuscatedEndPoint}) while retaining regular direct and indirect fallback paths");
src/Network/DistributedConnectionManager.cs:1027:                Diagnostic.Debug($"No compatible obfuscated distributed endpoint available for {username} ({ipEndPoint}); using regular direct and indirect parent candidate paths");
src/Network/DistributedConnectionManager.cs:1146:            Diagnostic.Debug($"Soliciting indirect parent candidate connection to {username} with token {solicitationToken}");
src/Network/DistributedConnectionManager.cs:1176:                Diagnostic.Debug($"Failed to establish an indirect parent candidate connection to {username} with token {solicitationToken}: {ex.Message}");
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:34:        /// <param name="filename">The filename to check.</param>
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:35:        public PlaceInQueueRequest(string filename)
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:37:            Filename = ProtocolArgumentValidator.RequireNotNull(filename, nameof(filename), "filename");
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:41:        ///     Gets the filename to check.
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:60:            var filename = reader.ReadString();
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:62:            return new PlaceInQueueRequest(filename);
src/Messaging/Compression/Inflate.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/UserAddressRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/UserEndPointCacheException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/MessageCompressionException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/WaitKeyNormalizer.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/UnwatchUserCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/.CodeAnalysis/stylecop.json:2:  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
src/.CodeAnalysis/stylecop.json:20:      "copyrightText": "    Copyright (c) {companyName}.\n\n    This program is free software: you can redistribute it and/or modify\n    it under the terms of the GNU General Public License as published by\n    the Free Software Foundation, version 3.\n\n    This program is distributed in the hope that it will be useful,\n    but WITHOUT ANY WARRANTY; without even the implied warranty of\n    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\n    GNU General Public License for more details.\n\n    You should have received a copy of the GNU General Public License\n    along with this program.  If not, see https://www.gnu.org/licenses/.\n\n    This program is distributed with Additional Terms pursuant to Section 7\n    of the GPLv3.  See the LICENSE file in the root directory of this\n    project for the complete terms and conditions.\n\n    SPDX-FileCopyrightText: JP Dillingham\n    SPDX-License-Identifier: GPL-3.0-only",
src/Messaging/Messages/Peer/PeerSearchRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/PeerSearchRequest.cs:34:        /// <param name="token">The unique token for the request.</param>
src/Messaging/Messages/Peer/PeerSearchRequest.cs:36:        public PeerSearchRequest(int token, string query)
src/Messaging/Messages/Peer/PeerSearchRequest.cs:38:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "peer search token");
src/Messaging/Messages/Peer/PeerSearchRequest.cs:40:            Token = token;
src/Messaging/Messages/Peer/PeerSearchRequest.cs:50:        ///     Gets the unique token for the search.
src/Messaging/Messages/Peer/PeerSearchRequest.cs:69:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/PeerSearchRequest.cs:72:            return new PeerSearchRequest(token, query);
src/Exceptions/TransferStreamException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/LoginRejectedException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/StringResponse.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Network/ConnectionFactory.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/TransferSizeMismatchException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/StopPublicChatCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/ListenException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SendRoomMessageAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/InfTree.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/StartPublicChatCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/ITokenBucket.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/ITokenBucket.cs:31:    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
src/Common/ITokenBucket.cs:41:        ///     Asynchronously retrieves the specified token <paramref name="count"/> from the bucket.
src/Common/ITokenBucket.cs:48:        ///     <para>If the bucket has tokens available, but fewer than the requested amount, the available tokens are returned.</para>
src/Common/ITokenBucket.cs:50:        ///         If the bucket has no tokens available, execution waits for the bucket to be replenished before servicing the request.
src/Common/ITokenBucket.cs:53:        /// <param name="count">The number of tokens to retrieve.</param>
src/Common/ITokenBucket.cs:54:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Common/ITokenBucket.cs:55:        /// <returns>A Task that completes when tokens have been provided.</returns>
src/Common/ITokenBucket.cs:59:        ///     Returns the specified token <paramref name="count"/> to the bucket.
src/Common/ITokenBucket.cs:62:        ///     <para>This method should only be called if tokens were retrieved from the bucket, but were not used.</para>
src/Common/ITokenBucket.cs:65:        ///         allows the bucket to 'burst' up to 2x capacity to 'catch up' to the desired rate if tokens were wastefully
src/Common/ITokenBucket.cs:70:        /// <param name="count">The number of tokens to return.</param>
tests/Soulseek.Tests.Unit/Client/SendUploadSpeedAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SetSharedCountsCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/TransferReportedFailedException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/KickedFromServerException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IOAdapter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IOAdapter.cs:36:        ///     Returns true if the given path exists, false otherwse.
src/Common/IOAdapter.cs:38:        /// <param name="path">The path to check.</param>
src/Common/IOAdapter.cs:39:        /// <returns>A value indicating whether the given path exists.</returns>
src/Common/IOAdapter.cs:40:        public bool Exists(string path) => System.IO.File.Exists(path);
src/Common/IOAdapter.cs:43:        ///     Creates a new FileStream from the given <paramref name="path"/> using the specified <paramref name="mode"/> and <paramref name="access"/>.
src/Common/IOAdapter.cs:45:        /// <param name="path">The path to open.</param>
src/Common/IOAdapter.cs:50:        public FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share)
src/Common/IOAdapter.cs:51:            => new FileStream(path, mode, access, share);
src/Common/IOAdapter.cs:54:        ///     Returns a new FileInfo object from the given <paramref name="path"/>.
src/Common/IOAdapter.cs:56:        /// <param name="path">The path for which to retrieve info.</param>
src/Common/IOAdapter.cs:58:        public FileInfo GetFileInfo(string path)
src/Common/IOAdapter.cs:59:            => new FileInfo(path);
src/Messaging/Messages/Peer/BrowseRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SetRoomTickerCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/Waiter.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/Waiter.cs:175:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/Waiter.cs:188:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/Waiter.cs:226:            // concern if we are given a timeout of 0, or a cancellation token which is already cancelled
src/Common/Waiter.cs:235:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/Waiter.cs:247:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/Waiter.cs:315:                throw new SoulseekClientException($"Failed to bind Wait Types for key {key}; this is likely a mismatch in the Types specified in the Wait() and the Complete(), which needs investigation. Please file a GitHub issue https://github.com/jpdillingham/Soulseek.NET. Exception message: {ex.Message}", ex);
src/Common/Waiter.cs:331:            /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Messaging/Messages/Server/SetOnlineStatusCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/DuplicateTransferException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IIOAdapter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IIOAdapter.cs:34:        ///     Returns true if the given path exists, false otherwse.
src/Common/IIOAdapter.cs:36:        /// <param name="path">The path to check.</param>
src/Common/IIOAdapter.cs:37:        /// <returns>A value indicating whether the given path exists.</returns>
src/Common/IIOAdapter.cs:38:        bool Exists(string path);
src/Common/IIOAdapter.cs:41:        ///     Creates a new FileStream from the given <paramref name="path"/> using the specified <paramref name="mode"/> and <paramref name="access"/>.
src/Common/IIOAdapter.cs:43:        /// <param name="path">The path to open.</param>
src/Common/IIOAdapter.cs:48:        FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share);
src/Common/IIOAdapter.cs:51:        ///     Returns a new FileInfo object from the given <paramref name="path"/>.
src/Common/IIOAdapter.cs:53:        /// <param name="path">The path for which to retrieve info.</param>
src/Common/IIOAdapter.cs:55:        FileInfo GetFileInfo(string path);
src/Exceptions/TransferRejectedException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:121:            var proxyOptions = new ProxyOptions("192.168.1.1", 1);
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:122:            var options = new ConnectionOptions(1, 1, 1, 1, 1, proxyOptions);
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:436:        [Theory(DisplayName = "Connect throws OperationCanceledException when token is cancelled"), AutoData]
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:499:        [Theory(DisplayName = "Connect connects through proxy if configured"), AutoData]
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:510:                var proxy = new ProxyOptions("127.0.0.1", 1, "username", "password");
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:511:                var options = new ConnectionOptions(proxyOptions: proxy);
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:526:                            proxy.IPEndPoint.Address,
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:527:                            proxy.IPEndPoint.Port,
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:530:                            proxy.Username,
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionTests.cs:531:                            proxy.Password,
src/Messaging/Compression/InfCodes.cs:2:// http://www.componentace.com
src/Messaging/Messages/Server/SetListenPortCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/WaitKey.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/WaitKey.cs:36:        private readonly object[] tokenParts;
src/Common/WaitKey.cs:41:        /// <param name="tokenParts">The parts which make up the key.</param>
src/Common/WaitKey.cs:42:        public WaitKey(params object[] tokenParts)
src/Common/WaitKey.cs:44:            this.tokenParts = tokenParts?.ToArray() ?? Array.Empty<object>();
src/Common/WaitKey.cs:45:            Token = string.Join(":", this.tokenParts);
src/Common/WaitKey.cs:49:        ///     Gets the wait token.
src/Common/WaitKey.cs:56:        public object[] TokenParts => tokenParts.ToArray();
src/Common/Extensions.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/TransferNotFoundException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/TokenFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/TokenFactory.cs:31:    ///     Generates unique tokens for network operations.
src/Common/TokenFactory.cs:34:    ///     Generated tokens skip zero because some Soulseek peers treat zero as a sentinel and do not return search responses
src/Common/TokenFactory.cs:57:        ///     Gets the next token.
src/Common/TokenFactory.cs:60:        ///     <para>Tokens are returned sequentially and the token value rolls over to 1 when it has reached <see cref="int.MaxValue"/>.</para>
src/Common/TokenFactory.cs:63:        /// <returns>The next token.</returns>
src/Exceptions/DuplicateTokenException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/DuplicateTokenException.cs:31:    ///     Represents errors that occur due to token collisions.
src/Messaging/Messages/Server/ServerSearchRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ServerSearchRequest.cs:39:        /// <param name="token">The unique token for the request.</param>
src/Messaging/Messages/Server/ServerSearchRequest.cs:41:        public ServerSearchRequest(string username, int token, string query)
src/Messaging/Messages/Server/ServerSearchRequest.cs:43:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "server search token");
src/Messaging/Messages/Server/ServerSearchRequest.cs:46:            Token = token;
src/Messaging/Messages/Server/ServerSearchRequest.cs:56:        ///     Gets the unique token for the request.
src/Messaging/Messages/Server/ServerSearchRequest.cs:81:            var token = reader.ReadInteger();
src/Messaging/Messages/Server/ServerSearchRequest.cs:84:            return new ServerSearchRequest(username, token, query);
src/Common/Constants.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Initialization/PierceFirewall.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Initialization/PierceFirewall.cs:36:        /// <param name="token">The unique token for the connection.</param>
src/Messaging/Messages/Initialization/PierceFirewall.cs:37:        public PierceFirewall(int token)
src/Messaging/Messages/Initialization/PierceFirewall.cs:39:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "firewall token");
src/Messaging/Messages/Initialization/PierceFirewall.cs:41:            Token = token;
src/Messaging/Messages/Initialization/PierceFirewall.cs:45:        ///     Gets the unique token for the connection.
src/Messaging/Messages/Initialization/PierceFirewall.cs:68:                var token = reader.ReadInteger();
src/Messaging/Messages/Initialization/PierceFirewall.cs:75:                response = new PierceFirewall(token);
src/Exceptions/TransferException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ListenerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/DownloadEnqueueException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/ServerPing.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/CharacterEncoding.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/SoulseekClientException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/SoulseekClientException.cs:31:    ///     Represents errors that occur during execution of <see cref="SoulseekClient"/> operations.
src/Messaging/Messages/Initialization/PeerInit.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Initialization/PeerInit.cs:38:        /// <param name="token">The unique token for the connection.</param>
src/Messaging/Messages/Initialization/PeerInit.cs:39:        public PeerInit(string username, string connectionType, int token)
src/Messaging/Messages/Initialization/PeerInit.cs:41:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "peer initialization token");
src/Messaging/Messages/Initialization/PeerInit.cs:45:            Token = token;
src/Messaging/Messages/Initialization/PeerInit.cs:54:        ///     Gets the unique token for the connection.
src/Messaging/Messages/Initialization/PeerInit.cs:84:                var token = reader.ReadInteger();
src/Messaging/Messages/Initialization/PeerInit.cs:91:                response = new PeerInit(username, transferType, token);
src/Exceptions/ConnectionWriteException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/RoomJoinForbiddenException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/SendUploadSpeedCommand.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/IOutgoingMessage.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/ITokenFactory.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/ITokenFactory.cs:27:    ///     Generates unique tokens for network operations.
src/Common/ITokenFactory.cs:32:        ///     Gets the next token.
src/Common/ITokenFactory.cs:35:        ///     <para>Tokens are returned sequentially and the token value rolls over to 1 when it has reached <see cref="int.MaxValue"/>.</para>
src/Common/ITokenFactory.cs:38:        /// <returns>The next token.</returns>
src/Common/TokenBucket.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/TokenBucket.cs:33:    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
src/Common/TokenBucket.cs:43:        /// <param name="interval">The interval at which tokens are replenished.</param>
src/Common/TokenBucket.cs:89:        ///     Asynchronously retrieves the specified token <paramref name="count"/> from the bucket.
src/Common/TokenBucket.cs:96:        ///     <para>If the bucket has tokens available, but fewer than the requested amount, the available tokens are returned.</para>
src/Common/TokenBucket.cs:98:        ///         If the bucket has no tokens available, execution waits for the bucket to be replenished before servicing the request.
src/Common/TokenBucket.cs:101:        /// <param name="count">The number of tokens to retrieve.</param>
src/Common/TokenBucket.cs:102:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/Common/TokenBucket.cs:103:        /// <returns>A Task that completes when tokens have been provided.</returns>
src/Common/TokenBucket.cs:115:        ///     Returns the specified token <paramref name="count"/> to the bucket.
src/Common/TokenBucket.cs:118:        ///     <para>This method should only be called if tokens were retrieved from the bucket, but were not used.</para>
src/Common/TokenBucket.cs:121:        ///         allows the bucket to 'burst' up to 2x capacity to 'catch up' to the desired rate if tokens were wastefully
src/Common/TokenBucket.cs:126:        /// <param name="count">The number of tokens to return.</param>
src/Common/TokenBucket.cs:171:                // this ensures tokens are distributed in the order in which callers obtain the semaphore,
src/Messaging/Compression/InfBlocks.cs:2:// http://www.componentace.com
src/Messaging/Messages/IInitializationMessage.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/ConnectionWriteDroppedException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/IIncomingMessage.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomTickerRemovedNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/RoomException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IWaiter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Common/IWaiter.cs:91:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/IWaiter.cs:100:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/IWaiter.cs:109:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Common/IWaiter.cs:117:        /// <param name="cancellationToken">The cancellation token for the wait.</param>
src/Exceptions/ConnectionReadException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/ReconfigureOptionsAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/ProxyException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/EmbeddedMessage.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Directory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomTickerListNotification.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/ConnectionException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/NoResponseException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/DistributedChildEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomTickerAddedNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/AddressException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/AddressException.cs:31:    ///     Represents errors that occur while changing the currently logged in user's password.
src/Exceptions/MessageReadException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/BrowseProgressUpdatedEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomMessageNotification.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Exceptions/MessageException.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/IUserEndPointCache.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/BrowseEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomMessageCommand.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/DistributedParentEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/UserEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomListResponseFactory.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/Adler32.cs:2:// http://www.componentace.com
src/DistributedNetworkInfo.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Server/RoomListRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/UserCannotConnectEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/UserCannotConnectEventArgs.cs:38:        /// <param name="token">The unique connection token.</param>
src/EventArgs/UserCannotConnectEventArgs.cs:39:        public UserCannotConnectEventArgs(int token, string username)
src/EventArgs/UserCannotConnectEventArgs.cs:42:            if (token < 0)
src/EventArgs/UserCannotConnectEventArgs.cs:44:                throw new ArgumentOutOfRangeException(nameof(token), "Connection token must be greater than or equal to zero");
src/EventArgs/UserCannotConnectEventArgs.cs:47:            Token = token;
src/EventArgs/UserCannotConnectEventArgs.cs:60:        ///     Gets the unique connection token.
src/EventArgs/RoomTickerListReceivedEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedChildDepth.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/TransferStateChangedEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/RoomTickerEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SendAcknowledgePrivateMessageAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedBranchLevel.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/TransferProgressUpdatedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:42:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), token: 0, cancellationToken: CancellationToken.None));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:56:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), (r) => { }, token: 0, cancellationToken: CancellationToken.None));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:65:        [Fact(DisplayName = "SearchAsync throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:72:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), token: -1));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:76:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:81:        [Fact(DisplayName = "SearchAsync delegate throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:88:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), (r) => { }, token: -1));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:92:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:104:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:120:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText("foo"), (r) => { }, token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:139:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(search), token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:155:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(query: null, token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:171:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(query: new SearchQuery(null), token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:187:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(query: new SearchQuery("-no"), token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:204:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(query: new SearchQuery("a"), token: 0, options: options));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:221:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(query: new SearchQuery("a"), token: 0, options: options));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:304:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(search), (r) => { }, token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:320:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(null, (r) => { }, token: 0));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:345:        [Theory(DisplayName = "SearchAsync throws DuplicateTokenException given a token in use"), AutoData]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:346:        public async Task SearchAsync_Throws_DuplicateTokenException_Given_A_Token_In_Use(string text, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:348:            using (var search = new SearchInternal(new SearchQuery(text), SearchScope.Network, token, new SearchOptions()))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:351:                dict.TryAdd(token, search);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:358:                    var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(text), token: token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:367:        [Theory(DisplayName = "SearchAsync delegate throws DuplicateTokenException given a token in use"), AutoData]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:368:        public async Task SearchAsync_Delegate_Throws_DuplicateTokenException_Given_A_Token_In_Use(string text, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:370:            using (var search = new SearchInternal(new SearchQuery(text), SearchScope.Network, token, new SearchOptions()))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:373:                dict.TryAdd(token, search);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:380:                    var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(text), (r) => { }, token: token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:390:        internal async Task SearchInternal_Duplicate_Registration_Does_Not_Remove_Existing_Active_Search(string text, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:392:            using (var existingSearch = new SearchInternal(new SearchQuery(text), SearchScope.Network, token, new SearchOptions()))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:395:                dict.TryAdd(token, existingSearch);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:406:                        token,
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:412:                    Assert.True(dict.TryGetValue(token, out var active));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:420:        public async Task SearchAsync_Returns_Completed_Search(string searchText, int token, string username)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:427:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:430:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:451:                var task = s.SearchAsync(SearchQuery.FromText(searchText), token: token, options: options);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:461:                Assert.Equal(token, res.Token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:465:                Assert.Equal(token, search.Token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:490:                await s.SearchAsync(SearchQuery.FromText(searchText), token: 0, options: options);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:498:        public async Task SearchAsync_Delegate_Returns_Completed_Search(string searchText, int token, string username)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:505:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:508:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:530:                var task = s.SearchAsync(SearchQuery.FromText(searchText), (r) => { responses.Add(r); }, token: token, options: options);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:540:                Assert.Equal(token, res.Token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:544:                Assert.Equal(token, search.Token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:552:        public async Task SearchInternalAsync_Adds_Search_To_ActiveSearches(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:556:            using (var search = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, options))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:569:                    var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, cts.Token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:578:                    Assert.Contains(active, kvp => kvp.Key == token);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:584:        [Theory(DisplayName = "SearchAsync creates token when not given"), AutoData]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:610:        [Theory(DisplayName = "SearchAsync delegate creates token when not given"), AutoData]
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:637:        public async Task SearchInternalAsync_Throws_OperationCanceledException_On_Cancellation(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:651:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, ct));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:660:        public async Task SearchInternalAsync_Throws_TimeoutException_On_Timeout(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:672:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:681:        public async Task SearchInternalAsync_Throws_SoulseekClientException_On_Error(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:693:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:702:        public async Task SearchAsync_Invokes_StateChanged_Delegate(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:707:            using (var search = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, options))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:719:                    var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:730:        public async Task SearchAsync_Fires_SearchStateChanged_Event(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:735:            using (var search = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, options))
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:748:                    var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:759:        public async Task SearchAsync_Succeeds_If_SearchStateChanged_Handler_Throws(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:772:                var ex = await Record.ExceptionAsync(() => s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:781:        public async Task SearchAsync_Invokes_ResponseReceived_Delegate(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:785:            var response = new SearchResponse("username", token, true, 1, 1, new List<File>() { new File(1, "foo", 1, "bar") });
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:796:                var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:800:                searches.FirstOrDefault(r => r.Key == token).Value.ResponseReceived.Invoke(response);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:811:        public async Task SearchAsync_Fires_SearchResponseReceived_Event(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:815:            var response = new SearchResponse("username", token, true, 1, 1, new List<File>() { new File(1, "foo", 1, "bar") });
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:826:                var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:828:                var search = s.GetProperty<ConcurrentDictionary<int, SearchInternal>>("Searches")[token];
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:839:        public async Task SearchAsync_Continues_If_SearchResponseReceived_Handler_Throws(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:842:            var response = new SearchResponse("username", token, true, 1, 1, new List<File>() { new File(1, "foo", 1, "bar") });
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:853:                var task = s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, options, null);
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:855:                var search = s.GetProperty<ConcurrentDictionary<int, SearchInternal>>("Searches")[token];
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:866:        public async Task SearchAsync_Sends_SearchRequest_Given_Network_Scope(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:868:            var expected = new SearchRequest(searchText, token).ToByteArray();
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:881:                        s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Network, token, cancellationToken: cts.Token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:890:        public async Task SearchAsync_Sends_WishlistSearchRequest_Given_Wishlist_Scope(string searchText, int token)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:892:            var expected = new WishlistSearchRequest(searchText, token).ToByteArray();
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:905:                        s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Wishlist, token, cancellationToken: cts.Token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:914:        public async Task SearchAsync_Sends_RoomSearchRequest_Given_Room_Scope(string searchText, int token, string room)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:916:            var expected = new RoomSearchRequest(room, searchText, token).ToByteArray();
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:929:                        s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.Room(room), token, cancellationToken: cts.Token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:938:        public async Task SearchAsync_Sends_UserSearchRequest_Given_User_Scope(string searchText, int token, string user)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:940:            var expected = new UserSearchRequest(user, searchText, token).ToByteArray();
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:953:                        s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.User(user), token, cancellationToken: cts.Token));
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:962:        public async Task SearchAsync_Sends_Multiple_UserSearchRequest_Given_User_Scope_With_Multiple_Users(string searchText, int token, string[] users)
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:968:                messages.AddRange(new UserSearchRequest(user, searchText, token).ToByteArray());
tests/Soulseek.Tests.Unit/Client/SearchAsyncTests.cs:984:                        s.SearchAsync(SearchQuery.FromText(searchText), SearchScope.User(users), token, cancellationToken: cts.Token));
src/Messaging/MessageReader.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/TransferEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/RoomTickerAddedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:37:        /// <param name="token">The unique token for the request.</param>
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:39:        public DistributedSearchRequest(string username, int token, string query)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:41:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "distributed search token");
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:44:            Token = token;
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:54:        ///     Gets the unique token for the request.
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:82:            var token = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:85:            return new DistributedSearchRequest(username, token, query);
src/EventArgs/SoulseekClientStateChangedEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:36:        /// <param name="token">The unique token for the response.</param>
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:37:        public DistributedPingResponse(int token)
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:39:            ProtocolArgumentValidator.RequireNonNegative(token, nameof(token), "distributed ping token");
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:41:            Token = token;
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:45:        ///     Gets the unique token for the response.
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:64:            int token = 0;
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:68:                token = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:71:            return new DistributedPingResponse(token);
src/EventArgs/RoomMessageReceivedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SoulseekClientEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/WatchUserAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/RoomLeftEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedPingRequest.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/MessageCode.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Messages/Distributed/DistributedBranchRoot.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/RoomJoinedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SoulseekClientDisconnectedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/MessageBuilderExtensions.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/RoomEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SearchStateChangedEventArgs.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/PublicChatMessageReceivedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SearchResponseReceivedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Compression/Deflate.cs:2:// http://www.componentace.com
src/Messaging/Compression/Deflate.cs:1032:					// zlib, so we don't care about this pathological case.)
tests/Soulseek.Tests.Unit/Client/SendPeerMessageAsyncTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/SendPeerMessageAsyncTests.cs:109:                .Callback<byte[], CancellationToken?>((message, token) => sentMessage = message)
tests/Soulseek.Tests.Unit/Client/SendPeerMessageAsyncTests.cs:168:        [Fact(DisplayName = "SendPeerMessageAsync uses given cancellation token")]
tests/Soulseek.Tests.Unit/Client/SendPeerMessageAsyncTests.cs:186:                .Callback<byte[], CancellationToken?>((message, token) => writeToken = token)
src/EventArgs/PrivilegeNotificationReceivedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:87:            var token = 654321;
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:88:            var peerInit = new PeerInit("sender", Constants.ConnectionType.Transfer, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:96:                await sender.WriteAsync(BitConverter.GetBytes(token), CancellationToken.None);
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:100:                Assert.Equal(BitConverter.GetBytes(token), await receiver.ReadAsync(4, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:108:            var token = 98765;
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:109:            var peerInit = new PeerInit("sender", Constants.ConnectionType.Transfer, token).ToByteArray();
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:117:                await sender.WriteAsync(BitConverter.GetBytes(token), CancellationToken.None);
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:121:                Assert.Equal(BitConverter.GetBytes(token), await receiver.ReadAsync(4, CancellationToken.None));
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:181:                    (requested, token) => Task.FromResult(0),
tests/Soulseek.Tests.Unit/Network/Tcp/ObfuscatedConnectionMatrixTests.cs:197:                    (requested, token) => Task.FromResult(0),
src/EventArgs/SearchRequestResponseEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SearchRequestResponseEventArgs.cs:35:        /// <param name="token">The unique token for the request.</param>
src/EventArgs/SearchRequestResponseEventArgs.cs:38:        public SearchRequestResponseEventArgs(string username, int token, string query, SearchResponse searchResponse)
src/EventArgs/SearchRequestResponseEventArgs.cs:39:            : base(username, token, query)
src/Messaging/MessageBuilder.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/PrivateMessageReceivedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/DownloadDeniedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/DownloadDeniedEventArgs.cs:35:        /// <param name="filename">The filename associated with the event.</param>
src/EventArgs/DownloadDeniedEventArgs.cs:37:        public DownloadDeniedEventArgs(string username, string filename, string message)
src/EventArgs/DownloadDeniedEventArgs.cs:40:            Filename = filename;
src/EventArgs/DownloadDeniedEventArgs.cs:45:        ///     Gets the filename associated with the event.
src/EventArgs/DownloadFailedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/DownloadFailedEventArgs.cs:35:        /// <param name="filename">The filename associated with the event.</param>
src/EventArgs/DownloadFailedEventArgs.cs:36:        public DownloadFailedEventArgs(string username, string filename)
src/EventArgs/DownloadFailedEventArgs.cs:39:            Filename = filename;
src/EventArgs/DownloadFailedEventArgs.cs:43:        ///     Gets the filename associated with the event.
src/EventArgs/RoomTickerRemovedEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SearchRequestEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/EventArgs/SearchRequestEventArgs.cs:37:        /// <param name="token">The unique token for the request.</param>
src/EventArgs/SearchRequestEventArgs.cs:39:        public SearchRequestEventArgs(string username, int token, string query)
src/EventArgs/SearchRequestEventArgs.cs:41:            if (token < 0)
src/EventArgs/SearchRequestEventArgs.cs:43:                throw new ArgumentOutOfRangeException(nameof(token), "Search request token must be greater than or equal to zero");
src/EventArgs/SearchRequestEventArgs.cs:47:            Token = token;
src/EventArgs/SearchRequestEventArgs.cs:57:        ///     Gets the unique token for the request.
src/EventArgs/SearchEventArgs.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/IMessageHandler.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/IDistributedMessageHandler.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/RawMessageWriter.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/MessageBuilderExtensionsTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/IServerMessageHandler.cs:14://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/IPeerMessageHandler.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/DistributedMessageHandler.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/DistributedMessageHandler.cs:337:            catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "token")
src/Messaging/Handlers/DistributedMessageHandler.cs:345:                Diagnostic.Debug($"Ignored distributed search request with invalid token from {source}");
tests/Soulseek.Tests.Unit/Messaging/Messages/EmbeddedMessageTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/ServerMessageHandler.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/ServerMessageHandler.cs:385:                        Diagnostic.Debug($"Received CannotConnect message for token {cannotConnect.Token}{(!string.IsNullOrEmpty(cannotConnect.Username) ? $" from user {cannotConnect.Username}" : string.Empty)}");
src/Messaging/Handlers/ServerMessageHandler.cs:427:                                Diagnostic.Debug($"Received transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token}");
src/Messaging/Handlers/ServerMessageHandler.cs:438:                                        Diagnostic.Debug($"Solicited inbound transfer connection to {download.Username} ({connection.IPEndPoint}) for token {download.Token} (remote: {download.RemoteToken}) established. (id: {connection.Id})");
src/Messaging/Handlers/ServerMessageHandler.cs:443:                                        Diagnostic.Debug($"Transfer ConnectToPeer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPEndPoint}) for remote token {connectToPeerResponse.Token} does not match any waiting downloads, discarding.");
src/Messaging/Handlers/ServerMessageHandler.cs:592:                            // check the list of searches that are underway to see if there's one that 1) matches this token,
src/Messaging/Handlers/PeerMessageHandler.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/Messaging/Handlers/PeerMessageHandler.cs:201:                            Diagnostic.Warning($"Error resolving search response for query '{searchRequest.Query}' requested by {connection.Username} with token {searchRequest.Token}: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:315:                                Diagnostic.Debug($"Rejecting unknown upload from {connection.Username} for {transferRequest.Filename} with token {transferRequest.Token}");
src/Messaging/Handlers/PeerMessageHandler.cs:504:        private async Task<(bool Rejected, string RejectionMessage)> TryEnqueueDownloadAsync(string username, IPEndPoint ipEndPoint, string filename)
src/Messaging/Handlers/PeerMessageHandler.cs:512:                    .EnqueueDownload(username, ipEndPoint, filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:533:        private async Task TrySendPlaceInQueueAsync(IMessageConnection connection, string filename)
src/Messaging/Handlers/PeerMessageHandler.cs:539:                placeInQueue = await SoulseekClient.Options.PlaceInQueueResolver(connection.Username, connection.IPEndPoint, filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:543:                Diagnostic.Warning($"Failed to resolve place in queue for file {filename} from {connection.Username}: {ex.Message}", ex);
src/Messaging/Handlers/PeerMessageHandler.cs:551:                    await connection.WriteAsync(new PlaceInQueueResponse(filename, placeInQueue.Value)).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:555:                    Diagnostic.Warning($"Failed to send place in queue response for file {filename} from {connection.Username}: {ex.Message}", ex);
tests/Soulseek.Tests.Unit/Messaging/MessageReaderTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ISoulseekClient.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
src/ISoulseekClient.cs:342:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:355:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:370:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:378:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:387:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:403:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:424:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:437:        ///     Asynchronously changes the password for the currently logged in user.
src/ISoulseekClient.cs:439:        /// <param name="password">The new password.</param>
src/ISoulseekClient.cs:440:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:443:        ///     Thrown when the <paramref name="password"/> is null, empty, or consists only of whitespace.
src/ISoulseekClient.cs:449:        Task ChangePasswordAsync(string password, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:453:        ///     <paramref name="username"/> and <paramref name="password"/>.
src/ISoulseekClient.cs:456:        /// <param name="password">The password with which to log in.</param>
src/ISoulseekClient.cs:457:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:460:        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
src/ISoulseekClient.cs:469:        Task ConnectAsync(string username, string password, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:473:        ///     and logs in using the specified <paramref name="username"/> and <paramref name="password"/>.
src/ISoulseekClient.cs:478:        /// <param name="password">The password with which to log in.</param>
src/ISoulseekClient.cs:479:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:488:        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
src/ISoulseekClient.cs:498:        Task ConnectAsync(string address, int port, string username, string password, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:508:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:530:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/ISoulseekClient.cs:544:        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
src/ISoulseekClient.cs:547:        /// <param name="token">The unique download token.</param>
src/ISoulseekClient.cs:549:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:565:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:578:        Task<Transfer> DownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:582:        ///     <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/ISoulseekClient.cs:594:        /// <param name="token">The unique download token.</param>
src/ISoulseekClient.cs:596:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:612:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:625:        Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:631:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:646:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:660:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/ISoulseekClient.cs:685:        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
src/ISoulseekClient.cs:688:        /// <param name="token">The unique download token.</param>
src/ISoulseekClient.cs:690:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:703:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:716:        Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:721:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
src/ISoulseekClient.cs:745:        /// <param name="token">The unique download token.</param>
src/ISoulseekClient.cs:747:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:760:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:773:        Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:779:        ///         <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:788:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/ISoulseekClient.cs:789:        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
src/ISoulseekClient.cs:790:        /// <param name="token">The unique upload token.</param>
src/ISoulseekClient.cs:792:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:802:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:812:        Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:818:        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:827:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/ISoulseekClient.cs:830:        /// <param name="token">The unique upload token.</param>
src/ISoulseekClient.cs:832:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:843:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:853:        Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:860:        /// <param name="token">The unique token for the operation.</param>
src/ISoulseekClient.cs:861:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:872:        Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:875:        ///     Asynchronously fetches the current place of the specified <paramref name="filename"/> in the queue of the
src/ISoulseekClient.cs:879:        /// <param name="filename">The file to check.</param>
src/ISoulseekClient.cs:880:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:883:        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
src/ISoulseekClient.cs:891:        Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:894:        ///     Gets the next token for use in client operations.
src/ISoulseekClient.cs:897:        ///     <para>Tokens are returned sequentially and the token value rolls over to 1 when it has reached <see cref="int.MaxValue"/>.</para>
src/ISoulseekClient.cs:900:        /// <returns>The next token.</returns>
src/ISoulseekClient.cs:907:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:918:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:925:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:932:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:944:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:960:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:976:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:984:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:991:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:999:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1007:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1027:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1042:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1083:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1103:        /// <param name="cancellationToken">The token to minotor for cancellation requests.</param>
src/ISoulseekClient.cs:1121:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1137:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1167:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1182:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1199:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1214:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1222:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1229:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1240:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1251:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1261:        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:1265:        /// <param name="token">The unique search token.</param>
src/ISoulseekClient.cs:1267:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1273:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:1278:        Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:1282:        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:1287:        /// <param name="token">The unique search token.</param>
src/ISoulseekClient.cs:1289:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1298:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:1303:        Task<Search> SearchAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:1310:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1326:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1336:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1353:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1387:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1402:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1418:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1434:        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
src/ISoulseekClient.cs:1449:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1460:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1471:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1480:        ///     Asynchronously removes the specified <paramref name="username"/> from the server watch list for the current session.
src/ISoulseekClient.cs:1487:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1501:        ///     <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:1504:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/ISoulseekClient.cs:1505:        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
src/ISoulseekClient.cs:1506:        /// <param name="token">The unique upload token.</param>
src/ISoulseekClient.cs:1508:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1518:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:1528:        Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:1533:        ///     specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
src/ISoulseekClient.cs:1536:        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
src/ISoulseekClient.cs:1539:        /// <param name="token">The unique upload token.</param>
src/ISoulseekClient.cs:1541:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
src/ISoulseekClient.cs:1552:        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
src/ISoulseekClient.cs:1562:        Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
src/ISoulseekClient.cs:1565:        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list for the current session.
src/ISoulseekClient.cs:1572:        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs:28:        // Outgoing message constructors validate locally-generated tokens. These remain strict.
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs:52:        [Theory(DisplayName = "Outgoing message constructors reject negative tokens")]
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs:60:        [Fact(DisplayName = "Peer search request rejects negative token")]
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarHardeningTests.cs:72:        [Fact(DisplayName = "Server search request rejects negative token")]
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:229:        public void Handles_Ping(string username, int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:239:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:244:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Distributed.Ping, username), It.Is<DistributedPingResponse>(r => r.Token == token)), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:249:        public void Broadcasts_SearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:255:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:264:        public async Task Broadcasts_SearchRequest_Reports_Background_Broadcast_Failures(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:270:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:289:        public void Broadcasts_ServerSearchRequest_As_SearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:300:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:304:            var forwardedMessage = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:309:                .Verify(m => m.BroadcastMessageAsync(forwardedMessage, It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:314:        public void Responds_To_SearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:320:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:324:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query));
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:329:        public void Deduplicates_SearchRequest_When_Deduplicate_Option_Is_Set(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:335:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:340:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Exactly(1));
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:345:        public void Does_Not_Deduplicate_SearchRequest_When_Deduplicate_Option_Is_Unset(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:351:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:356:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Exactly(2));
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:361:        public void Responds_To_ServerSearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:372:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:378:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:382:        [Theory(DisplayName = "Ignores SearchRequest with invalid token"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:399:            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("invalid token"))), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:403:        [Theory(DisplayName = "Ignores embedded SearchRequest with invalid token"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:421:            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("invalid token"))), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:425:        [Theory(DisplayName = "HandleEmbeddedMessage ignores SearchRequest with invalid token"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:443:            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("invalid token"))), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:448:        public void Doesnt_Respond_To_SearchRequest_If_Result_Is_Null(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:464:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:476:        public void Doesnt_Respond_To_SearchRequest_If_Result_Contains_No_Files(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:478:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>());
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:497:            var message = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:527:        public void HandleChildMessageRead_Responds_To_Ping(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:532:                .Returns(token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:540:            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new DistributedPingResponse(token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:545:        public void HandleChildMessageRead_Responds_To_Ping_From_EventArgs(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:550:                .Returns(token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:558:            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new DistributedPingResponse(token).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:563:        public void HandleChildMessageRead_Logs_ChildDepth(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:568:                .Returns(token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:581:        public void HandleChildMessageRead_Produces_Warning_On_Exception(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:586:                .Returns(token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:645:        public void HandleEmbeddedMessage_Promotes_To_Branch_Root_On_Search_Request(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:654:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:665:        public void HandleEmbeddedMessage_Broadcasts_Unwrapped_Search_Request(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:674:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:687:        public void HandleEmbeddedMessage_Responds_To_Search_Request(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:696:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/DistributedMessageHandlerTests.cs:702:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query));
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarEmissionTests.cs:41:            () => new LoginRequest(-1, "user", "password"),
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarEmissionTests.cs:77:            () => new LoginRequest(1, null, "password"),
tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolScalarEmissionTests.cs:118:        [Theory(DisplayName = "Outbound peer and initialization scalar commands reject invalid tokens before emission")]
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedChildDepthTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedBranchLevelTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:29:        public void Instantiates_With_The_Given_Data(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:31:            var r = new DistributedSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:34:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:40:        public void ToByteArray_Constructs_The_Correct_Message(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:42:            var msg = new DistributedSearchRequest(username, token, query).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:54:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:60:        public void FromByteArray_Returns_Expected_Data(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:66:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedSearchRequestTests.cs:73:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:29:        public void Instantiates_With_The_Given_Data(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:31:            var r = new DistributedPingResponse(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:33:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:38:        public void ToByteArray_Constructs_The_Correct_Message(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:40:            var msg = new DistributedPingResponse(token).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:48:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:53:        public void FromByteArray_Returns_Expected_Data(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:57:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:62:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedPingResponseTests.cs:66:        [Fact(DisplayName = "FromByteArray does not throw if message is missing token")]
tests/Soulseek.Tests.Unit/Messaging/MessageBuilderTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Distributed/DistributedBranchRootTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:166:            // length + code + token
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:212:            var password = Guid.NewGuid().ToString();
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:213:            var a = new LoginRequest(minorVersion: 9999, name, password);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:216:            Assert.Equal(password, a.Password);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:228:            var password = Guid.NewGuid().ToString();
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:229:            var a = new LoginRequest(minorVersion: 9999, name, password);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:236:            Assert.Equal(name.Length + password.Length + a.Hash.Length + 28, msg.Length);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:238:            Assert.Equal(password, reader.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:273:        public void SearchRequest_Instantiates_Properly(string text, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:275:            var a = new SearchRequest(text, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:278:            Assert.Equal(token, a.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:284:        public void SearchRequest_Constructs_The_Correct_Message(string text, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:286:            var a = new SearchRequest(text, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:294:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:301:        public void WishlistSearchRequest_Instantiates_Properly(string text, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:303:            var a = new WishlistSearchRequest(text, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:306:            Assert.Equal(token, a.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:312:        public void WishlistSearchRequest_Constructs_The_Correct_Message(string text, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:314:            var a = new WishlistSearchRequest(text, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:322:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:486:        public void ConnectToPeerRequest_Instantiates_Properly(int token, string username, string type)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:488:            var a = new ConnectToPeerRequest(token, username, type);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:490:            Assert.Equal(token, a.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:498:        public void ConnectToPeerRequest_Constructs_The_Correct_Message(int token, string username, string type)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:500:            var a = new ConnectToPeerRequest(token, username, type);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:507:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:748:        public void UserSearchRequest_Constructs_The_Correct_Message(string username, string searchText, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:750:            var a = new UserSearchRequest(username, searchText, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:758:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:765:        public void RoomSearchRequest_Constructs_The_Correct_Message(string roomName, string searchText, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:767:            var a = new RoomSearchRequest(roomName, searchText, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:775:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:782:        public void AcknowledgePrivilegeNotificationCommand_Constructs_The_Correct_Message(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:784:            token = Math.Abs(token % 100000);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:785:            var a = new AcknowledgePrivilegeNotificationCommand(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/OutgoingTests.cs:792:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:93:        public void Raises_DownloadDenied_On_UploadDenied(string username, string filename, string message)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:104:                l.HandleMessageRead(mocks.PeerConnection.Object, new UploadDenied(filename, message).ToByteArray());
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:108:                Assert.Equal(filename, args.Filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:115:        public void Raises_DownloadFailed_On_UploadFailed(string username, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:126:                l.HandleMessageRead(mocks.PeerConnection.Object, new UploadFailed(filename).ToByteArray());
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:130:                Assert.Equal(filename, args.Filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:174:        public void Throws_TransferReRequest_Wait_On_PeerUploadFailed_Message(string username, IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:179:            dict.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:189:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:194:            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<TransferReportedFailedException>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:264:        public void Completes_Wait_For_TransferResponse(string username, IPEndPoint endpoint, int token, int fileSize)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:268:            var msg = new TransferResponse(token, fileSize).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:272:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferResponse, username, token), It.Is<TransferResponse>(r => r.Token == token)), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:299:        public void Completes_Wait_For_FolderContentsResponse(string username, IPEndPoint endpoint, int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:303:            var msg = new FolderContentsResponse(token, dirname, new List<Directory>() { new Directory(dirname) }).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:307:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.FolderContentsResponse, username, token), It.IsAny<IEnumerable<Directory>>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:312:        public void Completes_Wait_For_PeerPlaceInQueueResponse(string username, IPEndPoint endpoint, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:318:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:326:                    new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename),
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:327:                    It.Is<PlaceInQueueResponse>(r => r.Filename == filename && r.PlaceInQueue == placeInQueue)), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:366:        public void Ignores_Inactive_Search_Response(string username, IPEndPoint endpoint, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:373:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:376:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:396:        public void Throws_TransferRequest_Wait_On_PeerUploadDenied(string username, IPEndPoint endpoint, string filename, string message)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:402:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:408:            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<Exception>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:413:        public void Appends_Active_Search_Response(string username, IPEndPoint endpoint, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:420:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:423:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:438:            using (var search = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:444:                mocks.Searches.TryAdd(token, search);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:449:                Assert.Contains(responses, r => r.Username == username && r.Token == token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:528:        public void Sends_Resolved_SearchResponse(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:536:            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:543:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:555:        public void Ignores_PeerSearchRequest_If_Search_Response_Resolver_Is_Null(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:563:            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:570:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:584:        public void Ignores_PeerSearchRequest_If_Search_Response_Is_Empty(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:588:            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:595:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:609:        public void Writes_RawSearchResponse_With_Expected_Length(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:622:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:641:        public void Writes_Obfuscated_RawSearchResponse_Encoded(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:669:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:682:        public void Disposes_RawSearchResponse_Stream_When_Write_Fails(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:703:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:714:        public void Does_Not_Throw_When_Disposing_RawSearchResponse_Stream_Fails(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:728:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:747:        public void Creates_Diagnostic_On_Failed_Search_Response_Resolution(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:751:            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:759:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:822:        public void Writes_RawBrowseResponse_With_Expected_Length(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:835:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:889:        public void Disposes_RawBrowseResponse_Stream_When_Write_Fails(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:910:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:921:        public void Does_Not_Throw_When_Disposing_RawBrowseResponse_Stream_Fails(string username, IPEndPoint endpoint, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:935:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:965:        public void Sends_Resolved_FolderContentsResponse(int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:975:            var response = new FolderContentsResponse(token, dirname, new List<Directory>() { dir });
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:980:            var msg = new FolderContentsRequest(token, dirname).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:990:        public void Creates_Diagnostic_On_Failed_FolderContentsResponse_Resolution(string username, IPEndPoint endpoint, int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1000:            var message = new FolderContentsRequest(token, dirname).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1009:        public void Creates_Diagnostic_On_Invalid_FolderContentsResponse_Resolver_Output(string username, IPEndPoint endpoint, int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1019:            var message = new FolderContentsRequest(token, dirname).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1029:        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_QueueDownload(string username, IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1039:            var message = new QueueDownloadRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1048:        public void Writes_PlaceInQueueResponse_On_Successful_Enqueue_Via_QueueDownload(string username, IPEndPoint endpoint, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1056:            var message = new QueueDownloadRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1061:                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1066:        public void Does_Not_Write_PlaceInQueueResponse_On_Successful_Enqueue_Via_QueueDownload_If_PlaceInQueueResponse_Is_Null(string username, IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1074:            var message = new QueueDownloadRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1084:        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_TransferRequest(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1094:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1103:        public void Writes_TransferResponse_On_Successful_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1108:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1109:            var expected = new TransferResponse(token, "Queued").ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1118:        public void Writes_PlaceInQueueResponse_On_Successful_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1126:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1131:                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1136:        public void Writes_PlaceInQueueResponse_On_PlaceInQueueRequest(string username, IPEndPoint endpoint, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1144:            var message = new PlaceInQueueRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1149:                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1154:        public void Does_Not_Write_PlaceInQueueResponse_On_PlaceInQueueRequest_If_Response_Is_Null(string username, IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1162:            var message = new PlaceInQueueRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1172:        public void Does_Not_Write_PlaceInQueueResponse_On_Successful_QueueDownload_Invocation_If_PlaceInQueueResponse_Is_Null(string username, IPEndPoint endpoint, int token, string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1180:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1185:                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1190:        public void Creates_Diagnostic_When_PlaceInQueueResponseResolver_Throws(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1200:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1209:        public void Writes_PlaceInQueueResponse_With_Negative_Position_When_Resolver_Returns_It(string username, IPEndPoint endpoint, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1219:            var message = new PlaceInQueueRequest(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1223:            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, -1).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1228:        public void Writes_TransferResponse_And_QueueFailedResponse_On_Failed_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1233:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1234:            var expectedTransferResponse = new TransferResponse(token, "Enqueue failed due to internal error").ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1235:            var expectedQueueFailedResponse = new UploadDenied(filename, "Enqueue failed due to internal error").ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1245:        public void Writes_TransferResponse_And_QueueFailedResponse_On_Rejected_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename, string rejectMessage)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1250:            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1251:            var expectedTransferResponse = new TransferResponse(token, rejectMessage).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1252:            var expectedQueueFailedResponse = new UploadDenied(filename, rejectMessage).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1262:        public void Completes_TransferRequest_Wait_On_Upload_Request_If_Transfer_Is_Tracked(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1267:            downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, username, filename, token));
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1272:            var request = new TransferRequest(TransferDirection.Upload, token, filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1277:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1282:        public void Does_Not_Complete_TransferRequest_Wait_On_Upload_Request_If_No_Downloads_Are_Tracked(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1286:            var request = new TransferRequest(TransferDirection.Upload, token, filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1291:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1296:        public void Does_Not_Complete_TransferRequest_Wait_On_Upload_Request_If_Transfer_Is_Not_Tracked(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1301:            downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, "not-username", filename, token));
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1306:            var request = new TransferRequest(TransferDirection.Upload, token, filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1311:            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1316:        public void Rejects_TransferRequest_Upload_Request_If_Transfer_Is_Not_Tracked(string username, IPEndPoint endpoint, int token, string filename)
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1320:            var request = new TransferRequest(TransferDirection.Upload, token, filename);
tests/Soulseek.Tests.Unit/Messaging/Handlers/PeerMessageHandlerTests.cs:1325:            var expected = new TransferResponse(token, "Cancelled").ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadDeniedTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadDeniedTests.cs:94:        public void ToByteArray_Returns_Expected_Data(string filename, string message)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadDeniedTests.cs:96:            var m = new UploadDenied(filename, message).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadDeniedTests.cs:102:            Assert.Equal(4 + 4 + 4 + filename.Length + 4 + message.Length, m.Length);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadDeniedTests.cs:103:            Assert.Equal(filename, reader.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:29:        public void Instantiates_Properly(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:31:            var a = new PlaceInQueueRequest(filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:33:            Assert.Equal(filename, a.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:38:        public void Constructs_The_Correct_Message(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:40:            var a = new PlaceInQueueRequest(filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:47:            Assert.Equal(filename, reader.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:80:        public void Parse_Returns_Expected_Data(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:84:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueRequestTests.cs:89:            Assert.Equal(filename, response.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:32:        public void Instantiates_With_The_Given_Data(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:34:            var r = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:36:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:63:            // omit token
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:72:        public void TryParse_Returns_Expected_Data(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:79:            msg.AddRange(BitConverter.GetBytes(token));
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:81:            // omit token
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:87:            Assert.Equal(token, result.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:92:        public void TryParse_Returns_False_On_Trailing_Data(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:99:            msg.AddRange(BitConverter.GetBytes(token));
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:113:            var token = new Random().Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:114:            var a = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:116:            Assert.Equal(token, a.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:124:            var token = new Random().Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:125:            var a = new PierceFirewall(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PierceFirewallTests.cs:134:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:34:        public void Instantiates_With_The_Given_Data(string username, string transferType, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:36:            var r = new PeerInit(username, transferType, token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:40:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:72:            // omit token
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:81:        public void TryParse_Returns_Expected_Data(string username, char type, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:92:            msg.AddRange(BitConverter.GetBytes(token));
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:94:            // omit token
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:102:            Assert.Equal(token, result.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:107:        public void TryParse_Returns_False_On_Trailing_Data(string username, char type, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:118:            msg.AddRange(BitConverter.GetBytes(token));
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:132:            var token = new Random().Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:133:            var a = new PeerInit(name, "P", token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Initialization/PeerInitTests.cs:144:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:29:        public void Instantiates_With_The_Given_Data(string text, int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:33:            var ex = Record.Exception(() => request = new PeerSearchRequest(token, text));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:38:            Assert.Equal(token, request.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:71:        public void Parse_Returns_Expected_Data(int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:75:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PeerSearchRequestTests.cs:81:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:31:        public void Instantiates_With_The_Proper_Data_When_Disallowed(int token, string msg)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:36:            ex = Record.Exception(() => response = new TransferResponse(token, msg));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:40:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:48:        public void Instantiates_With_The_Proper_Data_When_Allowed(int token, long size)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:55:            ex = Record.Exception(() => response = new TransferResponse(token, size));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:59:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:94:        public void Parse_Returns_Expected_Data_When_Allowed(int token, long size)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:100:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:107:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:158:        public void Parse_Returns_Expected_Data_When_Upload_Allowed(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:162:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:168:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:174:        public void Parse_Returns_Expected_Data_When_Disallowed(int token, string message)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:178:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:185:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:192:        public void ToByteArray_Constructs_The_Correct_Message_When_Allowed(int token, long size)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:196:            var a = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:204:            // length + code + token + allowed + size
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:206:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:213:        public void ToByteArray_Constructs_The_Correct_Message_When_Disallowed(int token, string message)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:215:            var a = new TransferResponse(token, message);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:222:            // length + code + token + allowed + message len + message
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:227:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:234:        public void ToByteArray_Constructs_The_Correct_Message(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:236:            var a = new TransferResponse(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:243:            // length + code + token + allowed + message len + message
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferResponseTests.cs:248:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:184:        public void Raises_UserCannotConnect_Event_On_CannotConnect_If_Username(int token, string username)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:186:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:191:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:201:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:207:        public void Does_Not_Raise_UserCannotConnect_Event_On_CannotConnect_If_No_Username(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:209:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:214:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:227:        public void Discards_SearchResponse_On_CannotConnect(int token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:229:            token = token < 0 ? 0 : token;
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:234:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:239:            mocks.SearchResponder.Verify(m => m.TryDiscard(token), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:244:        public void Does_Not_Throw_On_CannotConnect_If_UserCannotConnect_Event_Is_Unbound(int token, string username)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:250:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:688:        public void Creates_Connection_On_ConnectToPeerResponse_P(string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:707:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:722:        public void Ignores_ConnectToPeerResponse_F_On_Unexpected_Connection(string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:737:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:748:        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_F(string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:766:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:786:        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_X(string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:804:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:824:        public void Attempts_Connection_On_Expected_ConnectToPeerResponse_F(string filename, string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:827:            active.TryAdd(token, new TransferInternal(TransferDirection.Download, username, filename, token));
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:833:            var transfer = new TransferInternal(TransferDirection.Download, username, filename, token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:835:                RemoteToken = token,
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:838:            mocks.Downloads.TryAdd(token, transfer);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:840:            var key = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:851:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:860:                .Returns(Task.FromResult((conn.Object, token)));
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:870:        public void Ignores_Connection_On_Unexpected_ConnectToPeerResponse_F(string filename, string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:873:            active.TryAdd(token, new TransferInternal(TransferDirection.Download, username, filename, token + 1));
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:879:            var transfer = new TransferInternal(TransferDirection.Download, username, filename, token + 1)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:881:                RemoteToken = token + 1,
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:884:            mocks.Downloads.TryAdd(token + 1, transfer); // add a record for this user, but with the wrong token
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:895:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:904:                .Returns(Task.FromResult((conn.Object, token)));
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:916:        public void Adds_Child_Connection_On_ConnectToPeerResponse_D(string username, int token, IPAddress ip, int port)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:934:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:941:            Assert.Equal(token, result.Token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:1670:        public void Handles_NewPassword(string password)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:1676:                .WriteString(password)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:1681:            mocks.Waiter.Verify(m => m.Complete<string>(new WaitKey(MessageCode.Server.NewPassword), password), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2212:        public void Responds_To_SearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2218:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2222:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2227:        public void Doesnt_Respond_To_SearchRequest_If_Result_Is_Null(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2243:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2256:        public void Doesnt_Respond_To_SearchRequest_If_Result_Contains_No_Files(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2258:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>());
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2273:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2286:        public void Doesnt_Respond_To_SearchRequest_If_It_Came_From_The_Local_User(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2288:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>());
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2301:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2310:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2315:        public void Responds_To_SearchRequest_If_It_Came_From_The_Local_User_And_It_Was_Intentionally_Sent(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2317:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2329:            var searchInternal = new SearchInternal(searchQuery, searchScope, token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2333:            searches.TryAdd(token, searchInternal);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2337:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2342:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Once);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2347:        public void Doesnt_Respond_To_SearchRequest_From_Local_User_If_Search_Scope_Is_Not_User(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2349:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2361:            var searchInternal = new SearchInternal(searchQuery, searchScope, token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2365:            searches.TryAdd(token, searchInternal);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2369:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2374:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2379:        public void Doesnt_Respond_To_SearchRequest_From_Local_User_If_Username_Not_In_Search_Subjects(string username, string otherUsername, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2381:            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2393:            var searchInternal = new SearchInternal(searchQuery, searchScope, token);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2397:            searches.TryAdd(token, searchInternal);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2401:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2406:            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2411:        public void Does_Not_Throw_When_Handling_SearchRequest_If_SearchResponseResolver_Is_Null(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2418:            var message = GetServerSearchRequest(username, token, query);
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2579:        private static byte[] GetServerSearchRequest(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Handlers/ServerMessageHandlerTests.cs:2584:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:31:        public void Instantiates_With_The_Given_Data(string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:35:            var a = new PlaceInQueueResponse(filename, placeInQueue);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:37:            Assert.Equal(filename, a.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:81:        public void Parse_Returns_Expected_Data(string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:87:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:93:            Assert.Equal(filename, response.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:99:        public void ToByteArray_Constructs_The_Correct_Message(string filename, int placeInQueue)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:103:            var res = new PlaceInQueueResponse(filename, placeInQueue).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/PlaceInQueueResponseTests.cs:107:            Assert.Equal(filename, reader.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:29:        public void Instantiates_With_The_Given_Data(int token, string directoryName)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:33:            var ex = Record.Exception(() => m = new FolderContentsRequest(token, directoryName));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:37:            Assert.Equal(token, m.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:71:        public void Parse_Returns_Expected_Data(int token, string directoryName)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:75:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:81:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:87:        public void Constructs_The_Correct_Message(int token, string directoryName)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsRequestTests.cs:89:            var a = new FolderContentsRequest(token, directoryName);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:29:        public void Instantiates_With_The_Given_Data(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:33:            var ex = Record.Exception(() => response = new QueueDownloadRequest(filename));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:37:            Assert.Equal(filename, response.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:70:        public void Parse_Returns_Expected_Data(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:74:                .WriteString(filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:79:            Assert.Equal(filename, response.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:84:        public void ToByteArray_Returns_Expected_Data(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:86:            var a = new QueueDownloadRequest(filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:94:            // length + code + filename len + filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:95:            Assert.Equal(4 + 4 + 4 + filename.Length, msg.Length);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/QueueDownloadRequestTests.cs:96:            Assert.Equal(filename, reader.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:36:            var token = Random.Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:42:            var ex = Record.Exception(() => response = new TransferRequest(dir, token, file, size));
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:47:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:85:            var token = Random.Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:92:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:100:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:110:            var token = Random.Next();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:116:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:123:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:181:        public void ToByteArray_Constructs_The_Correct_Message(TransferDirection dir, int token, string file, long size)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:185:            var a = new TransferRequest(dir, token, file, size);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:193:            // length + code + direction + token + file length + filename + size
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/TransferRequestTests.cs:196:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadFailedTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadFailedTests.cs:84:        public void ToByteArray_Returns_Expected_Data(string filename)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadFailedTests.cs:86:            var m = new UploadFailed(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UploadFailedTests.cs:91:            Assert.Equal(filename, r.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:37:        public void Instantiates_With_Given_Data(int token, Directory dir)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:40:            var a = new FolderContentsResponse(token, directoryName: dir.Name, directories: dirList);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:42:            Assert.Equal(token, a.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:91:        public void Parse_Returns_Empty_Response_Given_Empty_Message(int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:95:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:108:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:116:        public void Parse_Throws_MessageReadException_On_Missing_Data(int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:120:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:137:        public void Parse_Handles_Files_With_No_Attributes(int token, string dirname)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:141:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:177:        public void Parse_Handles_A_Complete_Response(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:184:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:256:        public void ToByteArray_Returns_Expected_Data(int token, string dirname1, string dirname2)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:273:            var r = new FolderContentsResponse(token, dirname1, new List<Directory>() { dir1, dir2 });
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:281:            Assert.Equal(token, m.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/FolderContentsResponseTests.cs:385:                filename: Guid.NewGuid().ToString(),
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/IntegerResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/LeaveRoomResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateMessageNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotJoinRoomNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/GlobalMessageNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:40:        [Fact(DisplayName = "UploadAsync stream throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:48:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream), token: -1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:52:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:57:        [Fact(DisplayName = "UploadAsync file throws ArgumentOutOfRangeException given negative token")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:65:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", testFile.Path, token: -1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:69:                Assert.Equal("token", ((ArgumentOutOfRangeException)ex).ParamName);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:83:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:100:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:109:        [Theory(DisplayName = "UploadAsync stream throws ArgumentException given bad filename")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:113:        public async Task UploadAsync_Stream_Throws_ArgumentException_Given_Bad_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:118:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:127:        [Theory(DisplayName = "UploadAsync file throws ArgumentException given bad remote filename")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:131:        public async Task UploadAsync_File_Throws_ArgumentException_Given_Bad_Remote_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:135:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:144:        [Theory(DisplayName = "UploadAsync file throws ArgumentException given bad local filename")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:148:        public async Task UploadAsync_File_Throws_ArgumentException_Given_Bad_Local_Filename(string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:152:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "remote", filename));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:185:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", zeroSizeArgument, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:204:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", size, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:219:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:237:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:251:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, inputStreamFactory: null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:268:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:288:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:310:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:321:        [Fact(DisplayName = "UploadAsync stream throws DuplicateTokenException when token used")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:334:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:338:                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:343:        [Fact(DisplayName = "UploadAsync file throws DuplicateTokenException when token used")]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:363:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString(), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:367:                    Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:373:        [Theory(DisplayName = "UploadAsync stream throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:374:        public async Task UploadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:382:                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:385:                tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:390:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:394:                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:400:        public async Task UploadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_A_Unique_Key(string username, string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:408:                tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:412:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:416:                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:422:        public async Task UploadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Username(string username, string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:430:                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:434:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", 1, (_) => Task.FromResult((Stream)stream), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:442:        [Theory(DisplayName = "UploadAsync stream does not throw DuplicateTransferException when an existing Upload matches only the filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:443:        public async Task UploadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Filename(string username, string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:451:                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:455:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, 1, (_) => Task.FromResult((Stream)stream), 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:463:        [Theory(DisplayName = "UploadAsync file throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:464:        public async Task UploadAsync_File_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:479:                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:482:                    tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:487:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:491:                    Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:498:        public async Task UploadAsync_File_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_A_Unique_Key(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:513:                    tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:517:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:521:                    Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:528:        public async Task UploadAsync_File_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Username(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:543:                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:547:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", localFilename, 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:556:        [Theory(DisplayName = "UploadAsync file does not throw DuplicateTransferException when an existing Upload matches only the filename"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:557:        public async Task UploadAsync_File_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Filename(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:572:                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:576:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, localFilename, 1));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:586:        public async Task UploadAsync_Stream_Uses_Given_CancellationToken(string username, string filename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:597:                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), cancellationToken: cancellationToken));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:606:        public async Task UploadAsync_File_Uses_Given_CancellationToken(string username, string filename, string localFilename)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:629:                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, cancellationToken: cancellationToken));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:660:                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:669:        public async Task UploadFromFileAsync_Throws_UserOfflineException_When_User_Offline(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:688:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:697:        public async Task UploadFromFileAsync_Throws_TimeoutException_On_TransferResponse_Timeout(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:722:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:731:        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_TransferResponse_Cancellation(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:735:            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:756:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:765:        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Request_Write_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:769:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:770:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:789:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:797:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:806:        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:810:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:812:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:839:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:848:        public async Task UploadFromFileAsync_Completes_Following_Normal_Transfer_Connection_Disconnect(string username, IPEndPoint endpoint, byte[] data, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:853:                var response = new TransferResponse(token, "foo".Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:854:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:877:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:884:                    var task = s.InvokeMethod<Task<Transfer>>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:891:                    Assert.Equal(filename, transfer.Filename);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:892:                    Assert.Equal(token, transfer.Token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:900:        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Unexpected_Transfer_Connection_Timeout(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:903:            var response = new TransferResponse(token, data.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:904:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:929:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:939:                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:953:        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Unexpected_Transfer_Connection_Cancellation(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:956:            var response = new TransferResponse(token, data.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:957:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:982:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:992:                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1007:        public async Task UploadFromFileAsync_Throws_Wrapped_Exception_On_Unexpected_Transfer_Connection_Exception(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1010:            var response = new TransferResponse(token, data.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1011:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1036:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1046:                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1065:        public async Task UploadFromFileAsync_Cancels_Write_Task_When_Disconnect_Wins_Race(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1069:            var response = new TransferResponse(token, fileData.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1070:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1099:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1109:                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1128:        public async Task UploadFromFileAsync_Completes_Without_Exception_When_Transfer_Is_Allowed(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1134:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1135:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1154:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1161:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1170:        public async Task UploadFromFileAsync_Completes_Without_Exception_When_Trailing_Read_Throws_ConnectionReadException(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1176:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1177:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1198:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1205:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1214:        public async Task UploadFromFileAsync_Completes_Without_Exception_After_MaximumLingerTime_When_Trailing_Read_Does_Not_Throw_ConnectionReadException(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1220:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1221:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1242:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1251:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, txOptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1260:        public async Task UploadFromFileAsync_Produces_Warning_Diagnostic_When_Disconnected_Due_To_MaximumLingerTime(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1266:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1267:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1288:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1299:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, txOptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1310:        public async Task UploadFromStreamAsync_Throws_DuplicateTransferException_If_Unique_Key_Add_Fails(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1323:                tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1327:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1336:        public async Task UploadFromStreamAsync_Throws_DuplicateTokenException_If_UploadDictionary_Add_Fails(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1352:                queued.TryAdd(token, new TransferInternal(TransferDirection.Upload, "foo", "bar", token));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1356:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1367:        [Theory(DisplayName = "UploadFromStreamAsync throws DuplicateTokenException when token is registered to download"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1368:        public async Task UploadFromStreamAsync_Throws_DuplicateTokenException_When_Token_Is_Registered_To_Download(string username, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1384:                queued.TryAdd(token, new TransferInternal(TransferDirection.Download, "foo", "bar", token));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1388:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1394:                Assert.True(queued.ContainsKey(token));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1400:        public async Task UploadFromStreamAsync_Disposes_Stream_Given_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1404:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1405:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1424:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1434:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1452:        public async Task UploadFromStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Disposal_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1456:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1457:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1476:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1490:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1505:        public async Task UploadFromStreamAsync_Does_Not_Dispose_Stream_Given_False_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1509:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1510:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1529:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1539:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1556:        public async Task UploadFromStreamAsync_Does_Not_Throw_If_Stream_Position_Getter_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1560:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1561:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1582:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1596:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1606:        public async Task UploadFromStreamAsync_Seeks_Stream_To_Offset_Value(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1612:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1613:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1632:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1642:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1651:        public async Task UploadFromStreamAsync_Skips_Write_If_Offset_Equals_Size(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1657:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1658:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1677:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1687:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1696:        public async Task UploadFromStreamAsync_Does_Not_Seek_Stream_If_SeekInputStreamAutomatically_Is_False(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1702:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1703:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1722:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1732:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1741:        public async Task UploadFromStreamAsync_Throws_SoulseekClientException_If_Seek_Is_NonZero_And_Input_Stream_Is_Not_Seekable(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1747:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1748:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1767:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1777:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1789:        public async Task UploadFromStreamAsync_Throws_SoulseekClientException_If_Seek_Is_Longer_Than_File(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1795:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1796:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1815:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1825:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1837:        public async Task UploadFromStreamAsync_Throws_SoulseekClientException_If_Peer_Sends_Negative_StartOffset(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1843:            var response = new TransferResponse(token, Size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1844:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1863:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1873:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, Size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1885:        public async Task UploadFromStreamAsync_Writes_Correct_Length_Given_Offset_Value(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1891:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1892:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1911:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1921:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1932:        public async Task UploadFromStreamAsync_Invokes_Reporter_Delegate_Passed_In_Options(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1936:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1937:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1961:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1980:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1991:        [Theory(DisplayName = "UploadFromStreamAsync returns unused tokens to UploadTokenBucket"), AutoData]
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1992:        public async Task UploadFromStreamAsync_Returns_Unused_Tokens_To_UploadTokenBucket(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1996:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:1997:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2021:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2042:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2056:        public async Task UploadFromStreamAsync_Does_Not_Throw_If_Reporter_Passed_In_Options_Is_Null(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2060:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2061:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2085:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2095:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2103:        public async Task UploadFromStreamAsync_Retrieves_Grant_From_Governor_Passed_In_Options_Then_UploadTokenBucket(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2110:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2111:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2135:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2148:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2153:                // be used to take tokens from the bucket.
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2160:        public async Task UploadFromFileAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2164:            var response = new TransferResponse(token, string.Empty); // reject
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2165:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2182:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2190:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2199:        public async Task UploadFromFileAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2205:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2206:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2208:                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2233:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2242:                    await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(stateChanged: (e) => fired = true), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2251:        public async Task UploadFromFileAsync_Raises_UploadProgressUpdated_Event_On_Data_Read(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size, int progressSize)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2255:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2256:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2258:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2288:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2300:                await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testData.Path, token, new TransferOptions(maximumLingerTime: 0), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2315:        public async Task UploadFromFileAsync_Raises_Expected_Events_On_Success(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2319:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2320:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2322:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2351:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2372:                await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testData.Path, token, new TransferOptions(maximumLingerTime: 0), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2412:        public async Task UploadFromFileAsync_Invokes_ProgressUpdated_Delegate_On_Data_Write(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2418:                var response = new TransferResponse(token, data.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2419:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2421:                var request = new TransferRequest(TransferDirection.Upload, token, filename, data.Length);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2450:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2460:                    await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(progressUpdated: (e) => fired = true), null);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2469:        public async Task UploadFromFileAsync_Raises_Upload_Events_On_Failure(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2473:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2474:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2476:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2507:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2522:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2535:        public async Task UploadFromFileAsync_Raises_Upload_Events_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2541:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2542:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2544:                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2571:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2585:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2599:        public async Task UploadFromFileAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2605:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2606:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2608:                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2639:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2653:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2666:        public async Task UploadFromFileAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, byte[] data, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2688:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2699:        public async Task UploadFromFileAsync_Writes_UploadDenied_On_Cancellation(string username, string filename, int token, IPEndPoint endpoint)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2724:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2730:            var expectedBytes = new UploadDenied(filename, "Cancelled").ToByteArray();
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2736:        public async Task UploadFromFileAsync_Throws_SoulseekClientException_And_ConnectionException_On_Transfer_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2740:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2741:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2743:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2769:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2777:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2788:        public async Task UploadFromFileAsync_Throws_SoulseekClientException_On_Failure_To_Read_Offset_Data(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2792:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2793:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2814:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2822:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2833:        public async Task UploadFromFileAsync_Throws_SoulseekClientException_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2837:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2838:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2857:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2865:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2876:        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2880:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2881:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2883:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2909:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2917:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2926:        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2930:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2931:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2933:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2959:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2967:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2978:        public async Task UploadFromFileAsync_Throws_TransferRejectedException_On_Transfer_Rejection(string username, IPEndPoint endpoint, string filename, int token)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2982:            var response = new TransferResponse(token, string.Empty); // reject
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:2983:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3000:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3008:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3017:        public async Task UploadFromFileAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3021:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3022:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3037:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3045:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3055:        public async Task UploadFromFileAsync_Sets_Exception_Property_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3059:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3060:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3075:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3088:                    filename,
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3090:                    token,
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3111:        public async Task UploadFromFileAsync_Updates_Remote_User_On_Failure(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3115:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3116:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3118:            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3144:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3152:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3157:            var expectedBytes = new UploadFailed(filename).ToByteArray();
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3163:        public async Task UploadFromFileAsync_Swallows_Final_Read_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3167:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3168:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3189:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3197:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: int.MaxValue), null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3205:        public async Task UploadFromStreamAsync_Throws_TransferException_If_AcquireSlot_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3209:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3210:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3227:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3237:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3250:        public async Task UploadFromStreamAsync_Throws_OperationCanceledException_If_AcquireSlot_Task_Is_Cancelled(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3254:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3255:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3272:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3282:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3292:        public async Task UploadFromStreamAsync_Does_Not_Throw_If_SlotReleased_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3296:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3297:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3316:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3328:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3338:        public async Task UploadFromStreamAsync_Releases_Unique_Key_When_Succeeding(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3342:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3343:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3362:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3375:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3385:        public async Task UploadFromStreamAsync_Releases_Unique_Key_When_Failing(string username, IPEndPoint endpoint, string filename, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3389:            var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3390:            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3405:            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3418:                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3428:        public async Task UploadFromFileAsync_Throws_When_Write_Throws(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3434:                var response = new TransferResponse(token, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3435:                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3437:                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3468:                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
tests/Soulseek.Tests.Unit/Client/UploadAsyncTests.cs:3482:                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PublicChatMessageNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:29:        public void Instantiates_With_The_Given_Data(string password)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:33:            var ex = Record.Exception(() => response = new NewPassword(password));
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:37:            Assert.Equal(password, response.Password);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:70:        public void Parse_Returns_Expected_Data(string password)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:74:                .WriteString(password)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:79:            Assert.Equal(password, response.Password);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:84:        public void ToByteArray_Returns_Expected_Data(string password)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:86:            var m = new NewPassword(password).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NewPasswordTests.cs:91:            Assert.Equal(password, r.ReadString());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:86:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:93:                .WriteString("filename2") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:114:        public void Parse_Returns_Expected_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:119:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:122:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:138:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:149:            Assert.Equal("filename", file.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:160:        public void Parse_Handles_Legacy_Responses_With_4_Byte_Queue_Length(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:165:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:168:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:183:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:194:            Assert.Equal("filename", file.Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:205:        public void Parse_Handles_Empty_Responses(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:210:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:222:            Assert.Equal(token, r.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:233:        public void Parse_Handles_Multiple_Files(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:238:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:241:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:248:                .WriteString("filename2") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:268:            Assert.Equal("filename", file[0].Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:277:            Assert.Equal("filename2", file[1].Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:288:        public void Parse_Handles_Locked_Files(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:293:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:296:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:303:                .WriteString("filename2") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:314:                .WriteString("filename3") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:330:            Assert.Equal("filename", file[0].Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:339:            Assert.Equal("filename2", file[1].Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:350:            Assert.Equal("filename3", locked[0].Filename);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:361:        public void Parse_Handles_Empty_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:366:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:369:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:388:        public void Parse_Handles_Multiple_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:393:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:396:                .WriteString("filename") // filename
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:426:        public void ToByteArray_Returns_Expected_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:434:            var s = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, list);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:444:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:470:        public void ToByteArray_Returns_Expected_Data_When_HasFreeUploadSlot_False(string username, int token, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:474:            var s = new SearchResponse(username, token, hasFreeUploadSlot: false, uploadSpeed, queueLength, list);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:484:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:492:        public void ToByteArray_Returns_Expected_Data_When_HasFreeUploadSlot_True(string username, int token, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:496:            var s = new SearchResponse(username, token, hasFreeUploadSlot: true, uploadSpeed, queueLength, list);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:506:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:514:        public void ToByteArray_Handles_Locked_Files(string username, int token, bool freeUploadSlots, int uploadSpeed, int queueLength)
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:526:            var s = new SearchResponse(username, token, hasFreeUploadSlot: freeUploadSlots, uploadSpeed, queueLength, list, locked);
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/SearchResponseFactoryTests.cs:536:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:32:        public void Instantiates_With_The_Given_Data(string username, string type, IPEndPoint endpoint, int token, bool isPrivileged)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:36:            var ex = Record.Exception(() => response = new ConnectToPeerResponse(username, type, endpoint, token, isPrivileged));
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:44:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:50:        public void Snapshots_Endpoint(string username, string type, IPEndPoint endpoint, int token, bool isPrivileged)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:53:            var response = new ConnectToPeerResponse(username, type, endpoint, token, isPrivileged);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:112:        public void Parse_Returns_Expected_Data(string username, string type, IPEndPoint endpoint, int token, bool isPrivileged)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:123:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:133:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:161:        public void Parse_Returns_Obfuscated_Metadata(string username, IPEndPoint endpoint, int token, bool isPrivileged)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ConnectToPeerResponseTests.cs:174:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivilegedUserNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/UserStatisticsResponseFactoryTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/BrowseResponseFactoryTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/BrowseResponseFactoryTests.cs:354:        [InlineData(-1, 4294967295)] // https://onlinetoolz.net/unsigned-signed
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/BrowseResponseFactoryTests.cs:570:                filename: Guid.NewGuid().ToString(),
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomRemoveOperatorTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/LoginResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomAddUserTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ExcludedSearchPhrasesTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivilegeNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Peer/UserInfoResponseFactoryTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/UserPrivilegeResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomToggleTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomOwnedListNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:28:        [Theory(DisplayName = "Instantiates correctly given token and username"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:29:        public void Instantiates_Correctly_Given_Token_And_Username(int token, string username)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:31:            var msg = new CannotConnect(token, username);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:33:            Assert.Equal(token, msg.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:38:        [Theory(DisplayName = "Instantiates correctly given token and username"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:39:        public void Instantiates_Correctly_Given_Token_Only(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:41:            var msg = new CannotConnect(token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:43:            Assert.Equal(token, msg.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:48:        [Theory(DisplayName = "ToByteArray Constructs the correct Message given token and username"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:49:        public void ToByteArray_Constructs_The_Correct_Message_Given_Token_And_Username(int token, string username)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:51:            var msg = new CannotConnect(token, username).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:58:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:63:        [Theory(DisplayName = "ToByteArray Constructs the correct Message given token only"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:64:        public void ToByteArray_Constructs_The_Correct_Message_Given_Token_Only(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:66:            var msg = new CannotConnect(token).ToByteArray();
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:73:            Assert.Equal(token, reader.ReadInteger());
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:79:        public void FromByteArray_Returns_Expected_Data(int token, string username)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:83:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:89:            Assert.Equal(token, m.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:94:        [Theory(DisplayName = "FromByteArray returns the expected data given message with only token"), AutoData]
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:95:        public void FromByteArray_Returns_Expected_Data_Given_Message_With_Only_Token(int token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:99:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/CannotConnectTests.cs:104:            Assert.Equal(token, m.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomRemoveUserTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomAddOperatorTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/StringResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/JoinRoomResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivateRoomUserListNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerPingTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomTickerAddedNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomTickerRemovedNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:29:        public void Instantiates_With_The_Given_Data(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:33:            var ex = Record.Exception(() => m = new ServerSearchRequest(username, token, query));
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:38:            Assert.Equal(token, m.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:72:        public void Parse_Returns_Expected_Data(string username, int token, string query)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:77:                .WriteInteger(token)
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/ServerSearchRequestTests.cs:84:            Assert.Equal(token, response.Token);
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomLeftNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/PrivilegedUserListTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomMessageNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/UserStatusResponseFactoryTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/UserAddressResponseTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/WatchUserResponseTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomJoinedNotificationTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/NetInfoTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomListTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RoomTickerListNotificationTests.cs:16://     along with this program.  If not, see https://www.gnu.org/licenses/.
tests/Soulseek.Tests.Unit/Messaging/Messages/Server/RecommendationsProtocolTests.cs:15://     along with this program.  If not, see https://www.gnu.org/licenses/.
