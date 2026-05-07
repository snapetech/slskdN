# slskNet.Runtime bug council candidate scan
# Generated: 2026-05-07T02:37:23Z

## Mutable public byte arrays and array properties
src/SearchScope.cs:112:        public static SearchScope User(params string[] usernames) => new SearchScope(SearchScopeType.User, usernames);
src/UserInfo.cs:83:        public byte[] Picture => picture == null ? null : (byte[])picture.Clone();
src/PeerDescriptorSignature.cs:64:        public byte[] PublicKey => publicKey.ToArray();
src/PeerDescriptorSignature.cs:69:        public byte[] Signature => signature.ToArray();
src/Network/MessageConnectionEventArgs.cs:65:        public byte[] Code => code?.ToArray();
src/Network/MessageConnectionEventArgs.cs:102:        public byte[] Message => message?.ToArray();
src/Network/MessageConnectionEventArgs.cs:126:        public byte[] Code => code?.ToArray();
tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionKeyTests.cs:61:        public static IEnumerable<object[]> GetHashCodeData => new List<object[]>
src/Common/WaitKey.cs:56:        public object[] TokenParts => tokenParts.ToArray();
src/Messaging/Messages/EmbeddedMessage.cs:56:        public byte[] DistributedMessage => distributedMessage?.ToArray();
src/Messaging/Compression/ZStream.cs:78:		public byte[] next_in; // next input byte
src/Messaging/Compression/ZStream.cs:83:		public byte[] next_out; // next output byte should be put there

## Constructors accepting mutable collections or params arrays
src/BrowseResponse.cs:44:public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
src/Common/WaitKey.cs:42:public WaitKey(params object[] tokenParts)
src/Directory.cs:42:public Directory(string name, IEnumerable<File> fileList = null)
src/DistributedNetworkInfo.cs:54:public DistributedNetworkInfo( double? averageBroadcastLatency, int branchLevel, string branchRoot, bool isBranchRoot, int childLimit, bool canAcceptChildren, IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)
src/EventArgs/RoomTickerListReceivedEventArgs.cs:42:public RoomTickerListReceivedEventArgs(string roomName, IEnumerable<RoomTicker> tickers)
src/File.cs:45:public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null)
src/ItemRecommendations.cs:39:public ItemRecommendations(string item, IReadOnlyCollection<Recommendation> recommendations)
src/ItemSimilarUsers.cs:39:public ItemSimilarUsers(string item, IReadOnlyCollection<string> usernames)
src/MeshRendezvousResult.cs:33:public MeshRendezvousResult( string interestTag, IReadOnlyCollection<SimilarUser> similarUsers, IReadOnlyCollection<PeerCapabilityRecord> capabilityRecords)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:43:public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
src/Messaging/Messages/Server/MessageUsersCommand.cs:40:public MessageUsersCommand(IEnumerable<string> usernames, string message)
src/Messaging/Messages/Server/NetInfoNotification.cs:45:public NetInfoNotification(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)
src/Messaging/Messages/Server/RoomTickerListNotification.cs:43:public RoomTickerListNotification( string roomName, int tickerCount, IEnumerable<RoomTicker> tickers)
src/Options/SoulseekClientOptions.cs:116:public SoulseekClientOptions( bool enableListener = true, IPAddress listenIPAddress = null, int listenPort = 50000, bool enableDistributedNetwork = true, bool acceptDistributedChildren = true, int distributedChildLimit = 25, int maximumConcurrentSearches = 2, int maximumConcurrentUploads = 10, int maximumUploadSpeed = int.MaxValue, int maximumConcurrentDownloads = int.MaxValue, int maximumDownloadSpeed = int.MaxValue, bool deduplicateSearchRequests = true, int messageTimeout = 5000, bool autoAcknowledgePrivateMessages = true, bool autoAcknowledgePrivilegeNotifications = true, bool acceptPrivateRoomInvitations = false, DiagnosticLevel minimumDiagnosticLevel = DiagnosticLevel.Info, int startingToken = 0, ConnectionOptions serverConnectionOptions = null, ConnectionOptions peerConnectionOptions = null, ConnectionOptions transferConnectionOptions = null, ConnectionOptions incomingConnectionOptions = null, PeerObfuscationOptions peerObfuscationOptions = null, ConnectionOptions distributedConnectionOptions = null, IUserEndPointCache userEndPointCache = null, Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null, ISearchResponseCache searchResponseCache = null, Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null, Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null, Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null, Func<string, IPEndPoint, string, Task> enqueueDownload = null, Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null, bool raiseEventsAsynchronously = false)
src/Options/SoulseekClientOptionsPatch.cs:93:public SoulseekClientOptionsPatch( bool? enableListener = null, IPAddress listenIPAddress = null, int? listenPort = null, bool? enableDistributedNetwork = null, bool? acceptDistributedChildren = null, int? distributedChildLimit = null, int? maximumUploadSpeed = null, int? maximumDownloadSpeed = null, bool? deduplicateSearchRequests = null, bool? autoAcknowledgePrivateMessages = null, bool? autoAcknowledgePrivilegeNotifications = null, bool? acceptPrivateRoomInvitations = null, ConnectionOptions serverConnectionOptions = null, ConnectionOptions peerConnectionOptions = null, ConnectionOptions transferConnectionOptions = null, ConnectionOptions incomingConnectionOptions = null, PeerObfuscationOptions peerObfuscationOptions = null, ConnectionOptions distributedConnectionOptions = null, IUserEndPointCache userEndPointCache = null, Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null, ISearchResponseCache searchResponseCache = null, Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null, Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null, Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null, Func<string, IPEndPoint, string, Task> enqueueDownload = null, Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null)
src/PeerCapabilityDescriptor.cs:36:public PeerCapabilityDescriptor( string peerId = null, IEnumerable<string> features = null, int? overlayPort = null, int maxPayloadLength = PeerCapabilityEnvelope.DefaultMaxPayloadLength, PeerDescriptorSignature signature = null)
src/RecommendationList.cs:39:public RecommendationList(IReadOnlyCollection<Recommendation> recommendations, IReadOnlyCollection<Recommendation> unrecommendations)
src/RoomData.cs:44:public RoomData(string name, IEnumerable<UserData> userList, bool isPrivate = false, string owner = null, IEnumerable<string> operatorList = null)
src/RoomInfo.cs:59:public RoomInfo(string name, IEnumerable<string> userList)
src/RoomList.cs:43:public RoomList( IEnumerable<RoomInfo> publicList, IEnumerable<RoomInfo> privateList, IEnumerable<RoomInfo> ownedList, IEnumerable<string> moderatedRoomNameList)
src/SearchQuery.cs:45:public SearchQuery(IEnumerable<string> terms, IEnumerable<string> exclusions = null)
src/SearchQuery.cs:69:public SearchQuery(string query, IEnumerable<string> exclusions)
src/SearchResponse.cs:49:public SearchResponse(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
src/SearchResponse.cs:95:internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
src/SearchScope.cs:42:public SearchScope(SearchScopeType type, params string[] subjects)
src/UserInterests.cs:40:public UserInterests(string username, IReadOnlyCollection<string> liked, IReadOnlyCollection<string> hated)
src/WishlistSearchCompletedEventArgs.cs:34:public WishlistSearchCompletedEventArgs(string term, Search search, IReadOnlyCollection<SearchResponse> responses, Exception exception)
src/WishlistSearchScheduler.cs:45:public WishlistSearchScheduler(ISoulseekClient client, IEnumerable<string> terms, WishlistSearchSchedulerOptions options = null)

## Value equality and hash-code comparisons
src/Network/Tcp/ConnectionKey.cs:73:        public bool Equals(ConnectionKey other)
src/Common/WaitKey.cs:58:        public static bool operator !=(WaitKey lhs, WaitKey rhs)
src/Common/WaitKey.cs:63:        public static bool operator ==(WaitKey lhs, WaitKey rhs)
src/Common/WaitKey.cs:83:        public bool Equals(WaitKey other)

## Non-idempotent task completion candidates

## Task, cancellation, timer, and semaphore lifecycle candidates
src/SearchInternal.cs:32:    using SystemTimer = System.Timers.Timer;
src/SearchInternal.cs:58:            SearchTimeoutTimer = new SystemTimer()
src/SearchInternal.cs:65:            SearchTimeoutTimer.Elapsed += (sender, e) => { Complete(SearchStates.TimedOut); };
src/SearchInternal.cs:114:        private SystemTimer SearchTimeoutTimer { get; set; }
src/SearchInternal.cs:115:        private TaskCompletionSource<int> TaskCompletionSource { get; } = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SearchInternal.cs:127:                SearchTimeoutTimer.Stop();
src/SearchInternal.cs:129:                TaskCompletionSource.TrySetException(new OperationCanceledException());
src/SearchInternal.cs:147:                SearchTimeoutTimer.Stop();
src/SearchInternal.cs:149:                TaskCompletionSource.TrySetResult(0);
src/SearchInternal.cs:176:                    SearchTimeoutTimer.Dispose();
src/SearchInternal.cs:200:                    SearchTimeoutTimer.Reset();
src/SearchInternal.cs:268:                    SearchTimeoutTimer.Reset();
src/SearchInternal.cs:297:            var cancellationTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SearchInternal.cs:298:            var taskCompletionSource = TaskCompletionSource;
src/SearchInternal.cs:300:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetException(new OperationCanceledException("Operation cancelled"))))
src/SearchInternal.cs:302:                var completedTask = await Task.WhenAny(taskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/WishlistSearchScheduler.cs:35:        private CancellationTokenSource cancellationTokenSource;
src/WishlistSearchScheduler.cs:100:                    var nextSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/WishlistSearchScheduler.cs:103:                    loopTask = previousTask.ContinueWith(
src/WishlistSearchScheduler.cs:133:                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/WishlistSearchScheduler.cs:154:            CancellationTokenSource source;
src/WishlistSearchScheduler.cs:181:                task.ContinueWith(
src/Common/Waiter.cs:75:                wait.TaskCompletionSource.TrySetCanceled());
src/Common/Waiter.cs:104:                ((TaskCompletionSource<T>)wait.TaskCompletionSource).TrySetResult(result));
src/Common/Waiter.cs:157:                wait.TaskCompletionSource.TrySetException(exception));
src/Common/Waiter.cs:167:                wait.TaskCompletionSource.TrySetException(new TimeoutException($"The wait timed out after {wait.Timeout} milliseconds")));
src/Common/Waiter.cs:197:            var taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/Waiter.cs:227:            wait.Register();
src/Common/Waiter.cs:228:            return ((TaskCompletionSource<T>)wait.TaskCompletionSource).Task;
src/Common/Waiter.cs:334:                TaskCompletionSource = taskCompletionSource;
src/Common/Waiter.cs:344:            public dynamic TaskCompletionSource { get; }
src/Common/Waiter.cs:357:            private CancellationTokenSource TimeoutTokenSource { get; set; }
src/Common/Waiter.cs:371:            public void Register()
src/Common/Waiter.cs:373:                CancellationTokenRegistration = CancellationToken.Register(() => CancelAction());
src/Common/Waiter.cs:375:                TimeoutTokenSource = new CancellationTokenSource(Timeout);
src/Common/Waiter.cs:376:                TimeoutTokenRegistration = TimeoutTokenSource.Token.Register(() => TimeoutAction());
src/Common/Waiter.cs:389:                        // this will be null if the wait is disposed before Register() is called,
src/TransferInternal.cs:234:        public TaskCompletionSource<bool> RemoteTaskCompletionSource { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/Extensions.cs:35:    using System.Timers;
src/Common/Extensions.cs:65:            task.ContinueWith(t =>
src/Common/Extensions.cs:79:            task.ContinueWith(t => { throw (T)Activator.CreateInstance(typeof(T), t.Exception.Message, t.Exception); }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
src/Common/Extensions.cs:121:        public static void Reset(this Timer timer)
src/Common/TokenBucket.cs:37:        private TaskCompletionSource<bool> waitForReset = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/TokenBucket.cs:59:            Clock = new System.Timers.Timer(interval);
src/Common/TokenBucket.cs:74:        private System.Timers.Timer Clock { get; set; }
src/Common/TokenBucket.cs:77:        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Common/TokenBucket.cs:155:                    Interlocked.Exchange(ref waitForReset, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
src/Common/TokenBucket.cs:191:            => Interlocked.Exchange(ref waitForReset, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(true);
src/Common/TokenBucket.cs:201:            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/TokenBucket.cs:203:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled()))
src/Common/TokenBucket.cs:205:                var completedTask = await Task.WhenAny(waitForReset.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:134:            SearchSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentSearches, maxCount: Options.MaximumConcurrentSearches);
src/SoulseekClient.cs:136:            GlobalDownloadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentDownloads, maxCount: Options.MaximumConcurrentDownloads);
src/SoulseekClient.cs:137:            GlobalUploadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploads, maxCount: Options.MaximumConcurrentUploads);
src/SoulseekClient.cs:139:            UserEndPointSemaphoreCleanupTimer = new System.Timers.Timer(300000); // 5 minutes
src/SoulseekClient.cs:140:            UserEndPointSemaphoreCleanupTimer.Elapsed += (sender, e) => _ = CleanupUserEndPointSemaphoresAsync();
src/SoulseekClient.cs:141:            UserEndPointSemaphoreCleanupTimer.Start();
src/SoulseekClient.cs:143:            UploadSemaphoreCleanupTimer = new System.Timers.Timer(900000); // 15 minutes
src/SoulseekClient.cs:144:            UploadSemaphoreCleanupTimer.Elapsed += (sender, e) => _ = CleanupUploadSemaphoresAsync();
src/SoulseekClient.cs:145:            UploadSemaphoreCleanupTimer.Start();
src/SoulseekClient.cs:188:                        download.RemoteTaskCompletionSource.TrySetException(new TransferReportedFailedException("Download reported as failed by remote client"));
src/SoulseekClient.cs:214:                        download.RemoteTaskCompletionSource.TrySetException(new TransferRejectedException(e.Message));
src/SoulseekClient.cs:635:        private SemaphoreSlim SearchSemaphore { get; }
src/SoulseekClient.cs:636:        private SemaphoreSlim GlobalDownloadSemaphore { get; }
src/SoulseekClient.cs:637:        private SemaphoreSlim GlobalRecommendationsSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:638:        private SemaphoreSlim GlobalUploadSemaphore { get; }
src/SoulseekClient.cs:641:        private SemaphoreSlim RecommendationsSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:642:        private SemaphoreSlim StateSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:643:        private SemaphoreSlim SimilarUsersSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:646:        private System.Timers.Timer UploadSemaphoreCleanupTimer { get; }
src/SoulseekClient.cs:647:        private ConcurrentDictionary<string, SemaphoreSlim> UploadSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
src/SoulseekClient.cs:648:        private SemaphoreSlim UploadSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:650:        private System.Timers.Timer UserEndPointSemaphoreCleanupTimer { get; }
src/SoulseekClient.cs:651:        private ConcurrentDictionary<string, SemaphoreSlim> UserEndPointSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
src/SoulseekClient.cs:652:        private SemaphoreSlim UserEndPointSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:1407:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1416:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1420:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1427:            var success = await WaitForTransferEnqueueAsync(downloadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1494:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1503:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1507:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1514:            var success = await WaitForTransferEnqueueAsync(downloadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1563:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1572:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1576:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1583:            var success = await WaitForTransferEnqueueAsync(uploadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1634:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1643:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1647:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1654:            var success = await WaitForTransferEnqueueAsync(uploadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:3263:                    UserEndPointSemaphoreCleanupTimer.Dispose();
src/SoulseekClient.cs:3264:                    UploadSemaphoreCleanupTimer.Dispose();
src/SoulseekClient.cs:3556:                    using var loginFailureCts = new CancellationTokenSource();
src/SoulseekClient.cs:3557:                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, loginFailureCts.Token);
src/SoulseekClient.cs:3813:                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);
src/SoulseekClient.cs:3817:                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/SoulseekClient.cs:3818:                var linkedCancellationToken = linkedCancellationTokenSource.Token;
src/SoulseekClient.cs:3892:                var firstTask = await Task.WhenAny(
src/SoulseekClient.cs:3895:                    download.RemoteTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:3898:                linkedCancellationTokenSource.Cancel();
src/SoulseekClient.cs:3900:                if (firstTask == download.RemoteTaskCompletionSource.Task)
src/SoulseekClient.cs:3905:                    await download.RemoteTaskCompletionSource.Task.ConfigureAwait(false);
src/SoulseekClient.cs:4179:                SemaphoreSlim semaphore;
src/SoulseekClient.cs:4186:                    semaphore = UserEndPointSemaphores.GetOrAdd(username, new SemaphoreSlim(1, 1));
src/SoulseekClient.cs:4985:            var completedTask = await Task.WhenAny(enqueuedTask, transferTask).ConfigureAwait(false);
src/SoulseekClient.cs:5147:            SemaphoreSlim semaphore = null;
src/SoulseekClient.cs:5158:                    semaphore = UploadSemaphores.GetOrAdd(username, new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploadsPerUser, maxCount: Options.MaximumConcurrentUploadsPerUser));
src/SoulseekClient.cs:5231:                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);
src/SoulseekClient.cs:5235:                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/SoulseekClient.cs:5236:                var linkedCancellationToken = linkedCancellationTokenSource.Token;
src/SoulseekClient.cs:5319:                var firstTask = await Task.WhenAny(
src/SoulseekClient.cs:5324:                linkedCancellationTokenSource.Cancel();
src/Messaging/Handlers/ServerMessageHandler.cs:205:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/PeerMessageHandler.cs:92:        public async void HandleMessageRead(object sender, byte[] message)
src/Network/ListenerHandler.cs:68:        public async void HandleConnection(object sender, IConnection connection)
src/Messaging/Handlers/DistributedMessageHandler.cs:78:        public async void HandleChildMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:144:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:280:        public async void HandleEmbeddedMessage(byte[] message)
src/Network/Tcp/Connection.cs:34:    using SystemTimer = System.Timers.Timer;
src/Network/Tcp/Connection.cs:73:            WriteQueueSemaphore = new SemaphoreSlim(Options.WriteQueueSize);
src/Network/Tcp/Connection.cs:77:                InactivityTimer = new SystemTimer()
src/Network/Tcp/Connection.cs:84:                InactivityTimer.Elapsed += (sender, e) =>
src/Network/Tcp/Connection.cs:91:            WatchdogTimer = new SystemTimer()
src/Network/Tcp/Connection.cs:98:            WatchdogTimer.Elapsed += (sender, e) =>
src/Network/Tcp/Connection.cs:109:                InactivityTimer?.Start();
src/Network/Tcp/Connection.cs:110:                WatchdogTimer.Start();
src/Network/Tcp/Connection.cs:199:        protected SystemTimer InactivityTimer { get; set; }
src/Network/Tcp/Connection.cs:219:        protected SystemTimer WatchdogTimer { get; set; }
src/Network/Tcp/Connection.cs:221:        private TaskCompletionSource<string> DisconnectTaskCompletionSource { get; } = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:222:        private SemaphoreSlim WriteSemaphore { get; set; } = new SemaphoreSlim(initialCount: 1, maxCount: 1);
src/Network/Tcp/Connection.cs:223:        private SemaphoreSlim WriteQueueSemaphore { get; set; }
src/Network/Tcp/Connection.cs:257:            var timeoutTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:258:            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:265:                using (var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(Options.ConnectTimeout)))
src/Network/Tcp/Connection.cs:291:                    using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:292:                    using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:294:                    await using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:295:                    await using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:298:                        var completedTask = await Task.WhenAny(connectTask, timeoutTaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/Network/Tcp/Connection.cs:300:                        if (completedTask == timeoutTaskCompletionSource.Task)
src/Network/Tcp/Connection.cs:304:                        else if (completedTask == cancellationTaskCompletionSource.Task)
src/Network/Tcp/Connection.cs:316:                InactivityTimer?.Start();
src/Network/Tcp/Connection.cs:317:                WatchdogTimer.Start();
src/Network/Tcp/Connection.cs:354:                InactivityTimer?.Stop();
src/Network/Tcp/Connection.cs:355:                WatchdogTimer.Stop();
src/Network/Tcp/Connection.cs:485:                return DisconnectTaskCompletionSource.Task;
src/Network/Tcp/Connection.cs:599:                    DisconnectTaskCompletionSource.TrySetException(exception);
src/Network/Tcp/Connection.cs:603:                    DisconnectTaskCompletionSource.TrySetResult(message);
src/Network/Tcp/Connection.cs:619:                    InactivityTimer?.Dispose();
src/Network/Tcp/Connection.cs:620:                    WatchdogTimer.Dispose();
src/Network/Tcp/Connection.cs:726:            InactivityTimer?.Reset();
src/Network/Tcp/Connection.cs:732:            using (cancellationToken.Register(() =>
src/Network/Tcp/Connection.cs:735:                return await DisconnectTaskCompletionSource.Task.ConfigureAwait(false);
src/Network/DistributedConnectionManager.cs:38:    using System.Timers;
src/Network/DistributedConnectionManager.cs:43:    using SystemTimer = System.Timers.Timer;
src/Network/DistributedConnectionManager.cs:73:            StatusDebounceTimer = new SystemTimer()
src/Network/DistributedConnectionManager.cs:80:            StatusDebounceTimer.Elapsed += StatusDebounceTimer_Elapsed;
src/Network/DistributedConnectionManager.cs:82:            WatchdogTimer = new SystemTimer()
src/Network/DistributedConnectionManager.cs:89:            WatchdogTimer.Elapsed += WatchdogTimer_Elapsed;
src/Network/DistributedConnectionManager.cs:221:        private SemaphoreSlim ParentSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Network/DistributedConnectionManager.cs:222:        private ConcurrentDictionary<string, CancellationTokenSource> PendingInboundIndirectConnectionDictionary { get; set; } = new ConcurrentDictionary<string, CancellationTokenSource>();
src/Network/DistributedConnectionManager.cs:225:        private SystemTimer StatusDebounceTimer { get; set; }
src/Network/DistributedConnectionManager.cs:226:        private SemaphoreSlim StatusSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Network/DistributedConnectionManager.cs:227:        private SystemTimer WatchdogTimer { get; }
src/Network/DistributedConnectionManager.cs:393:                using var cts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:641:                using (var cts = new CancellationTokenSource())
src/Network/DistributedConnectionManager.cs:700:        public async void RemoveAndDisposeAll()
src/Network/DistributedConnectionManager.cs:860:        private void AddOrUpdatePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/DistributedConnectionManager.cs:898:                    WatchdogTimer.Dispose();
src/Network/DistributedConnectionManager.cs:899:                    StatusDebounceTimer.Dispose();
src/Network/DistributedConnectionManager.cs:966:        private void RemovePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/DistributedConnectionManager.cs:968:            var pending = (ICollection<KeyValuePair<string, CancellationTokenSource>>)PendingInboundIndirectConnectionDictionary;
src/Network/DistributedConnectionManager.cs:969:            pending.Remove(new KeyValuePair<string, CancellationTokenSource>(username, pendingCts));
src/Network/DistributedConnectionManager.cs:1004:            using var directCts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:1005:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/DistributedConnectionManager.cs:1006:            using var indirectCts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:1007:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/DistributedConnectionManager.cs:1034:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/DistributedConnectionManager.cs:1082:                        task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/DistributedConnectionManager.cs:1194:        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
src/Network/DistributedConnectionManager.cs:1222:            if (StatusDebounceTimer.Enabled && LastStatusTimestamp.AddMilliseconds(StatusAgeLimit) <= DateTime.UtcNow)
src/Network/DistributedConnectionManager.cs:1228:            StatusDebounceTimer.Reset();
src/Network/DistributedConnectionManager.cs:1276:        private async void StatusDebounceTimer_Elapsed(object sender, ElapsedEventArgs e)
src/Network/DistributedConnectionManager.cs:1380:        private void WatchdogTimer_Elapsed(object sender, ElapsedEventArgs e)
src/Network/PeerConnectionManager.cs:94:        private ConcurrentDictionary<string, CancellationTokenSource> PendingInboundIndirectConnectionDictionary { get; set; } =
src/Network/PeerConnectionManager.cs:95:            new ConcurrentDictionary<string, CancellationTokenSource>();
src/Network/PeerConnectionManager.cs:225:            using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:226:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:227:            using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:228:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:249:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:394:                using (var cts = new CancellationTokenSource())
src/Network/PeerConnectionManager.cs:476:                using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:477:                using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:478:                using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:479:                using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:501:                    task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:690:            using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:691:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:692:            using var obfuscatedCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:693:            using var obfuscatedLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, obfuscatedCts.Token);
src/Network/PeerConnectionManager.cs:694:            using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:695:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:716:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:755:                        task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:789:        public async void RemoveAndDisposeAll()
src/Network/PeerConnectionManager.cs:860:        private void AddOrUpdatePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/PeerConnectionManager.cs:1100:        private void RemovePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/PeerConnectionManager.cs:1102:            var pending = (ICollection<KeyValuePair<string, CancellationTokenSource>>)PendingInboundIndirectConnectionDictionary;
src/Network/PeerConnectionManager.cs:1103:            pending.Remove(new KeyValuePair<string, CancellationTokenSource>(username, pendingCts));

## Lifecycle task completion and race candidates
src/SearchInternal.cs:115:        private TaskCompletionSource<int> TaskCompletionSource { get; } = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SearchInternal.cs:129:                TaskCompletionSource.TrySetException(new OperationCanceledException());
src/SearchInternal.cs:149:                TaskCompletionSource.TrySetResult(0);
src/SearchInternal.cs:297:            var cancellationTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SearchInternal.cs:298:            var taskCompletionSource = TaskCompletionSource;
src/SearchInternal.cs:300:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetException(new OperationCanceledException("Operation cancelled"))))
src/SearchInternal.cs:302:                var completedTask = await Task.WhenAny(taskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/WishlistSearchScheduler.cs:103:                    loopTask = previousTask.ContinueWith(
src/WishlistSearchScheduler.cs:181:                task.ContinueWith(
src/TransferInternal.cs:234:        public TaskCompletionSource<bool> RemoteTaskCompletionSource { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:221:        private TaskCompletionSource<string> DisconnectTaskCompletionSource { get; } = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:257:            var timeoutTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:258:            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Network/Tcp/Connection.cs:291:                    using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:292:                    using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:294:                    await using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:295:                    await using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:298:                        var completedTask = await Task.WhenAny(connectTask, timeoutTaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/Network/Tcp/Connection.cs:300:                        if (completedTask == timeoutTaskCompletionSource.Task)
src/Network/Tcp/Connection.cs:304:                        else if (completedTask == cancellationTaskCompletionSource.Task)
src/Network/Tcp/Connection.cs:485:                return DisconnectTaskCompletionSource.Task;
src/Network/Tcp/Connection.cs:599:                    DisconnectTaskCompletionSource.TrySetException(exception);
src/Network/Tcp/Connection.cs:603:                    DisconnectTaskCompletionSource.TrySetResult(message);
src/Network/Tcp/Connection.cs:735:                return await DisconnectTaskCompletionSource.Task.ConfigureAwait(false);
src/SoulseekClient.cs:188:                        download.RemoteTaskCompletionSource.TrySetException(new TransferReportedFailedException("Download reported as failed by remote client"));
src/SoulseekClient.cs:214:                        download.RemoteTaskCompletionSource.TrySetException(new TransferRejectedException(e.Message));
src/SoulseekClient.cs:1407:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1416:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1420:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1427:            var success = await WaitForTransferEnqueueAsync(downloadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1494:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1503:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1507:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1514:            var success = await WaitForTransferEnqueueAsync(downloadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1563:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1572:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1576:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1583:            var success = await WaitForTransferEnqueueAsync(uploadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:1634:            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/SoulseekClient.cs:1643:                    enqueuedTaskCompletionSource.TrySetResult(true);
src/SoulseekClient.cs:1647:                    enqueuedTaskCompletionSource.TrySetResult(false);
src/SoulseekClient.cs:1654:            var success = await WaitForTransferEnqueueAsync(uploadTask, enqueuedTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:3813:                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);
src/SoulseekClient.cs:3892:                var firstTask = await Task.WhenAny(
src/SoulseekClient.cs:3895:                    download.RemoteTaskCompletionSource.Task).ConfigureAwait(false);
src/SoulseekClient.cs:3900:                if (firstTask == download.RemoteTaskCompletionSource.Task)
src/SoulseekClient.cs:3905:                    await download.RemoteTaskCompletionSource.Task.ConfigureAwait(false);
src/SoulseekClient.cs:4985:            var completedTask = await Task.WhenAny(enqueuedTask, transferTask).ConfigureAwait(false);
src/SoulseekClient.cs:5231:                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);
src/SoulseekClient.cs:5319:                var firstTask = await Task.WhenAny(
src/Network/PeerConnectionManager.cs:249:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:501:                    task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:716:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:755:                        task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/PeerConnectionManager.cs:789:        public async void RemoveAndDisposeAll()
src/Common/Waiter.cs:75:                wait.TaskCompletionSource.TrySetCanceled());
src/Common/Waiter.cs:104:                ((TaskCompletionSource<T>)wait.TaskCompletionSource).TrySetResult(result));
src/Common/Waiter.cs:157:                wait.TaskCompletionSource.TrySetException(exception));
src/Common/Waiter.cs:167:                wait.TaskCompletionSource.TrySetException(new TimeoutException($"The wait timed out after {wait.Timeout} milliseconds")));
src/Common/Waiter.cs:197:            var taskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/Waiter.cs:228:            return ((TaskCompletionSource<T>)wait.TaskCompletionSource).Task;
src/Common/Waiter.cs:334:                TaskCompletionSource = taskCompletionSource;
src/Common/Waiter.cs:344:            public dynamic TaskCompletionSource { get; }
src/Network/ListenerHandler.cs:68:        public async void HandleConnection(object sender, IConnection connection)
src/Network/DistributedConnectionManager.cs:700:        public async void RemoveAndDisposeAll()
src/Network/DistributedConnectionManager.cs:1034:                task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/DistributedConnectionManager.cs:1082:                        task = await Task.WhenAny(tasks).ConfigureAwait(false);
src/Network/DistributedConnectionManager.cs:1194:        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
src/Network/DistributedConnectionManager.cs:1276:        private async void StatusDebounceTimer_Elapsed(object sender, ElapsedEventArgs e)
src/Common/TokenBucket.cs:37:        private TaskCompletionSource<bool> waitForReset = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/TokenBucket.cs:155:                    Interlocked.Exchange(ref waitForReset, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
src/Common/TokenBucket.cs:191:            => Interlocked.Exchange(ref waitForReset, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(true);
src/Common/TokenBucket.cs:201:            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
src/Common/TokenBucket.cs:203:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled()))
src/Common/TokenBucket.cs:205:                var completedTask = await Task.WhenAny(waitForReset.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
src/Common/Extensions.cs:65:            task.ContinueWith(t =>
src/Common/Extensions.cs:79:            task.ContinueWith(t => { throw (T)Activator.CreateInstance(typeof(T), t.Exception.Message, t.Exception); }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
src/Messaging/Handlers/ServerMessageHandler.cs:205:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:78:        public async void HandleChildMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:144:        public async void HandleMessageRead(object sender, byte[] message)
src/Messaging/Handlers/DistributedMessageHandler.cs:280:        public async void HandleEmbeddedMessage(byte[] message)
src/Messaging/Handlers/PeerMessageHandler.cs:92:        public async void HandleMessageRead(object sender, byte[] message)

## Lifecycle cancellation registration candidates
src/Common/Waiter.cs:227:            wait.Register();
src/Common/Waiter.cs:357:            private CancellationTokenSource TimeoutTokenSource { get; set; }
src/Common/Waiter.cs:371:            public void Register()
src/Common/Waiter.cs:373:                CancellationTokenRegistration = CancellationToken.Register(() => CancelAction());
src/Common/Waiter.cs:375:                TimeoutTokenSource = new CancellationTokenSource(Timeout);
src/Common/Waiter.cs:376:                TimeoutTokenRegistration = TimeoutTokenSource.Token.Register(() => TimeoutAction());
src/Common/Waiter.cs:389:                        // this will be null if the wait is disposed before Register() is called,
src/Common/TokenBucket.cs:203:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled()))
src/Network/DistributedConnectionManager.cs:222:        private ConcurrentDictionary<string, CancellationTokenSource> PendingInboundIndirectConnectionDictionary { get; set; } = new ConcurrentDictionary<string, CancellationTokenSource>();
src/Network/DistributedConnectionManager.cs:393:                using var cts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:641:                using (var cts = new CancellationTokenSource())
src/Network/DistributedConnectionManager.cs:860:        private void AddOrUpdatePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/DistributedConnectionManager.cs:966:        private void RemovePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/DistributedConnectionManager.cs:968:            var pending = (ICollection<KeyValuePair<string, CancellationTokenSource>>)PendingInboundIndirectConnectionDictionary;
src/Network/DistributedConnectionManager.cs:969:            pending.Remove(new KeyValuePair<string, CancellationTokenSource>(username, pendingCts));
src/Network/DistributedConnectionManager.cs:1004:            using var directCts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:1005:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/DistributedConnectionManager.cs:1006:            using var indirectCts = new CancellationTokenSource();
src/Network/DistributedConnectionManager.cs:1007:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/SearchInternal.cs:300:            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetException(new OperationCanceledException("Operation cancelled"))))
src/Network/Tcp/Connection.cs:265:                using (var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(Options.ConnectTimeout)))
src/Network/Tcp/Connection.cs:291:                    using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:292:                    using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:294:                    await using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:295:                    await using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
src/Network/Tcp/Connection.cs:732:            using (cancellationToken.Register(() =>
src/Network/PeerConnectionManager.cs:94:        private ConcurrentDictionary<string, CancellationTokenSource> PendingInboundIndirectConnectionDictionary { get; set; } =
src/Network/PeerConnectionManager.cs:95:            new ConcurrentDictionary<string, CancellationTokenSource>();
src/Network/PeerConnectionManager.cs:225:            using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:226:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:227:            using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:228:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:394:                using (var cts = new CancellationTokenSource())
src/Network/PeerConnectionManager.cs:476:                using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:477:                using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:478:                using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:479:                using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:690:            using var directCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:691:            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
src/Network/PeerConnectionManager.cs:692:            using var obfuscatedCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:693:            using var obfuscatedLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, obfuscatedCts.Token);
src/Network/PeerConnectionManager.cs:694:            using var indirectCts = new CancellationTokenSource();
src/Network/PeerConnectionManager.cs:695:            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);
src/Network/PeerConnectionManager.cs:860:        private void AddOrUpdatePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/PeerConnectionManager.cs:1100:        private void RemovePendingInboundIndirectConnection(string username, CancellationTokenSource pendingCts)
src/Network/PeerConnectionManager.cs:1102:            var pending = (ICollection<KeyValuePair<string, CancellationTokenSource>>)PendingInboundIndirectConnectionDictionary;
src/Network/PeerConnectionManager.cs:1103:            pending.Remove(new KeyValuePair<string, CancellationTokenSource>(username, pendingCts));
src/SoulseekClient.cs:3556:                    using var loginFailureCts = new CancellationTokenSource();
src/SoulseekClient.cs:3557:                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, loginFailureCts.Token);
src/SoulseekClient.cs:3817:                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/SoulseekClient.cs:3818:                var linkedCancellationToken = linkedCancellationTokenSource.Token;
src/SoulseekClient.cs:3898:                linkedCancellationTokenSource.Cancel();
src/SoulseekClient.cs:5235:                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/SoulseekClient.cs:5236:                var linkedCancellationToken = linkedCancellationTokenSource.Token;
src/SoulseekClient.cs:5324:                linkedCancellationTokenSource.Cancel();
src/WishlistSearchScheduler.cs:35:        private CancellationTokenSource cancellationTokenSource;
src/WishlistSearchScheduler.cs:100:                    var nextSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/WishlistSearchScheduler.cs:133:                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
src/WishlistSearchScheduler.cs:154:            CancellationTokenSource source;

## Lifecycle timer and semaphore candidates
src/Common/TokenBucket.cs:59:            Clock = new System.Timers.Timer(interval);
src/Common/TokenBucket.cs:66:            Clock.Start();
src/Common/TokenBucket.cs:74:        private System.Timers.Timer Clock { get; set; }
src/Common/TokenBucket.cs:77:        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Common/Extensions.cs:125:                timer.Stop();
src/Common/Extensions.cs:126:                timer.Start();
src/Network/Tcp/TcpListenerAdapter.cs:84:            TcpListener.Start();
src/Network/Tcp/TcpListenerAdapter.cs:93:            TcpListener.Stop();
src/Network/Tcp/Listener.cs:106:            TcpListener.Start();
src/Network/Tcp/Listener.cs:116:            TcpListener.Stop();
src/Network/DistributedConnectionManager.cs:43:    using SystemTimer = System.Timers.Timer;
src/Network/DistributedConnectionManager.cs:73:            StatusDebounceTimer = new SystemTimer()
src/Network/DistributedConnectionManager.cs:82:            WatchdogTimer = new SystemTimer()
src/Network/DistributedConnectionManager.cs:221:        private SemaphoreSlim ParentSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Network/DistributedConnectionManager.cs:225:        private SystemTimer StatusDebounceTimer { get; set; }
src/Network/DistributedConnectionManager.cs:226:        private SemaphoreSlim StatusSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/Network/DistributedConnectionManager.cs:227:        private SystemTimer WatchdogTimer { get; }
src/Network/DistributedConnectionManager.cs:493:            sw.Start();
src/Network/DistributedConnectionManager.cs:504:            sw.Stop();
src/Network/DistributedConnectionManager.cs:1228:            StatusDebounceTimer.Reset();
src/SoulseekClient.cs:134:            SearchSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentSearches, maxCount: Options.MaximumConcurrentSearches);
src/SoulseekClient.cs:136:            GlobalDownloadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentDownloads, maxCount: Options.MaximumConcurrentDownloads);
src/SoulseekClient.cs:137:            GlobalUploadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploads, maxCount: Options.MaximumConcurrentUploads);
src/SoulseekClient.cs:139:            UserEndPointSemaphoreCleanupTimer = new System.Timers.Timer(300000); // 5 minutes
src/SoulseekClient.cs:141:            UserEndPointSemaphoreCleanupTimer.Start();
src/SoulseekClient.cs:143:            UploadSemaphoreCleanupTimer = new System.Timers.Timer(900000); // 15 minutes
src/SoulseekClient.cs:145:            UploadSemaphoreCleanupTimer.Start();
src/SoulseekClient.cs:635:        private SemaphoreSlim SearchSemaphore { get; }
src/SoulseekClient.cs:636:        private SemaphoreSlim GlobalDownloadSemaphore { get; }
src/SoulseekClient.cs:637:        private SemaphoreSlim GlobalRecommendationsSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:638:        private SemaphoreSlim GlobalUploadSemaphore { get; }
src/SoulseekClient.cs:641:        private SemaphoreSlim RecommendationsSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:642:        private SemaphoreSlim StateSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:643:        private SemaphoreSlim SimilarUsersSemaphore { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:646:        private System.Timers.Timer UploadSemaphoreCleanupTimer { get; }
src/SoulseekClient.cs:647:        private ConcurrentDictionary<string, SemaphoreSlim> UploadSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
src/SoulseekClient.cs:648:        private SemaphoreSlim UploadSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:650:        private System.Timers.Timer UserEndPointSemaphoreCleanupTimer { get; }
src/SoulseekClient.cs:651:        private ConcurrentDictionary<string, SemaphoreSlim> UserEndPointSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
src/SoulseekClient.cs:652:        private SemaphoreSlim UserEndPointSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);
src/SoulseekClient.cs:957:                    listener.Start();
src/SoulseekClient.cs:966:                        obfuscatedListener.Start();
src/SoulseekClient.cs:975:                    listener?.Stop();
src/SoulseekClient.cs:976:                    obfuscatedListener?.Stop();
src/SoulseekClient.cs:1029:                Listener?.Stop();
src/SoulseekClient.cs:1030:                ObfuscatedListener?.Stop();
src/SoulseekClient.cs:2290:                sw.Start();
src/SoulseekClient.cs:2296:                sw.Stop();
src/SoulseekClient.cs:2353:                    listener.Start();
src/SoulseekClient.cs:2361:                    listener?.Stop();
src/SoulseekClient.cs:3239:                    Listener?.Stop();
src/SoulseekClient.cs:3240:                    ObfuscatedListener?.Stop();
src/SoulseekClient.cs:3527:                        Listener.Start();
src/SoulseekClient.cs:3537:                            ObfuscatedListener.Start();
src/SoulseekClient.cs:4179:                SemaphoreSlim semaphore;
src/SoulseekClient.cs:4186:                    semaphore = UserEndPointSemaphores.GetOrAdd(username, new SemaphoreSlim(1, 1));
src/SoulseekClient.cs:4552:                    Listener?.Stop();
src/SoulseekClient.cs:4554:                    ObfuscatedListener?.Stop();
src/SoulseekClient.cs:4567:                        Listener.Start();
src/SoulseekClient.cs:4577:                            ObfuscatedListener.Start();
src/SoulseekClient.cs:5147:            SemaphoreSlim semaphore = null;
src/SoulseekClient.cs:5158:                    semaphore = UploadSemaphores.GetOrAdd(username, new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploadsPerUser, maxCount: Options.MaximumConcurrentUploadsPerUser));
src/Network/Tcp/Connection.cs:34:    using SystemTimer = System.Timers.Timer;
src/Network/Tcp/Connection.cs:73:            WriteQueueSemaphore = new SemaphoreSlim(Options.WriteQueueSize);
src/Network/Tcp/Connection.cs:77:                InactivityTimer = new SystemTimer()
src/Network/Tcp/Connection.cs:91:            WatchdogTimer = new SystemTimer()
src/Network/Tcp/Connection.cs:109:                InactivityTimer?.Start();
src/Network/Tcp/Connection.cs:110:                WatchdogTimer.Start();
src/Network/Tcp/Connection.cs:199:        protected SystemTimer InactivityTimer { get; set; }
src/Network/Tcp/Connection.cs:219:        protected SystemTimer WatchdogTimer { get; set; }
src/Network/Tcp/Connection.cs:222:        private SemaphoreSlim WriteSemaphore { get; set; } = new SemaphoreSlim(initialCount: 1, maxCount: 1);
src/Network/Tcp/Connection.cs:223:        private SemaphoreSlim WriteQueueSemaphore { get; set; }
src/Network/Tcp/Connection.cs:316:                InactivityTimer?.Start();
src/Network/Tcp/Connection.cs:317:                WatchdogTimer.Start();
src/Network/Tcp/Connection.cs:354:                InactivityTimer?.Stop();
src/Network/Tcp/Connection.cs:355:                WatchdogTimer.Stop();
src/Network/Tcp/Connection.cs:726:            InactivityTimer?.Reset();
src/SearchInternal.cs:32:    using SystemTimer = System.Timers.Timer;
src/SearchInternal.cs:58:            SearchTimeoutTimer = new SystemTimer()
src/SearchInternal.cs:114:        private SystemTimer SearchTimeoutTimer { get; set; }
src/SearchInternal.cs:127:                SearchTimeoutTimer.Stop();
src/SearchInternal.cs:147:                SearchTimeoutTimer.Stop();
src/SearchInternal.cs:200:                    SearchTimeoutTimer.Reset();
src/SearchInternal.cs:268:                    SearchTimeoutTimer.Reset();

## Lifecycle fire-and-forget async misuse candidates

## Protocol count and length allocation candidates
src/Network/Tcp/ObfuscatedTransferConnection.cs:137:            var output = new byte[checked((int)length)];
src/Network/Tcp/ObfuscatedTransferConnection.cs:140:            while (offset < output.Length)
src/Network/Tcp/ObfuscatedTransferConnection.cs:147:                while (decodedBuffer.Count > 0 && offset < output.Length)
src/Network/Tcp/ObfuscatedTransferConnection.cs:177:            while (totalBytesRead < length)
src/Network/Tcp/ObfuscatedTransferConnection.cs:193:                var buffer = new byte[bytesGranted];
src/Network/Tcp/ObfuscatedTransferConnection.cs:241:            var buffer = new byte[maxPayloadLength];
src/Network/Tcp/ObfuscatedTransferConnection.cs:244:            while (totalBytesWritten < length)
src/Network/Tcp/ObfuscatedTransferConnection.cs:262:                var payload = new byte[bytesRead];
src/Network/Tcp/ObfuscatedTransferConnection.cs:278:            var frame = new byte[FrameLengthBytes + payload.Length];
src/Network/Tcp/ObfuscatedTransferConnection.cs:295:            var encoded = new byte[8 + length];
src/Network/Tcp/RotatedObfuscation.cs:49:            var keyBytes = new byte[4];
src/Network/Tcp/RotatedObfuscation.cs:65:            var output = new byte[4 + input.Length];
src/Network/Tcp/RotatedObfuscation.cs:85:            var output = new byte[input.Length - 4];
src/Network/Tcp/TcpClientAdapter.cs:214:            var buffer = new byte[1024];
src/Network/Tcp/TcpClientAdapter.cs:220:                while (totalBytesRead < length)
src/Network/Tcp/Listener.cs:122:            while (Listening)
src/Messaging/Messages/Server/CannotConnect.cs:71:            var token = reader.ReadInteger();
src/Messaging/Messages/Server/ProtocolCountReader.cs:40:            return ReadValidatedCount(reader.ReadInteger(), reader.Remaining, collectionName, minimumBytesPerItem);
src/Messaging/Messages/Server/ProtocolCountReader.cs:52:            return ReadValidatedCount(reader.ReadInteger(), reader.Remaining, collectionName, minimumBytesPerItem);
src/Messaging/Messages/Server/ItemSimilarUsersResponse.cs:52:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/ItemRecommendationsResponse.cs:52:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/ItemRecommendationsResponse.cs:54:                recommendations.Add(new Recommendation(reader.ReadString(), reader.ReadInteger()));
src/Messaging/Messages/Server/SimilarUsersResponse.cs:51:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/SimilarUsersResponse.cs:53:                users.Add(new SimilarUser(reader.ReadString(), reader.ReadInteger()));
src/Messaging/Messages/Server/UserInterestsResponse.cs:60:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/RecommendationsResponse.cs:59:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/RecommendationsResponse.cs:61:                recommendations.Add(new Recommendation(reader.ReadString(), reader.ReadInteger()));
src/Network/Tcp/Connection.cs:651:            var buffer = new byte[Options.ReadBufferSize];
src/Network/Tcp/Connection.cs:660:                while (!Disposed && totalBytesRead < length)
src/Network/Tcp/Connection.cs:787:                buffer = new byte[Options.WriteBufferSize];
src/Network/Tcp/Connection.cs:794:                while (totalBytesWritten < length)
src/Messaging/Messages/Server/WatchUserResponse.cs:86:                var status = ProtocolValueValidator.ToDefinedEnum<UserPresence>(reader.ReadInteger(), "user presence");
src/Messaging/Messages/Server/WatchUserResponse.cs:88:                var averageSpeed = reader.ReadInteger();
src/Messaging/Messages/Server/WatchUserResponse.cs:89:                var downloadCount = reader.ReadLong();
src/Messaging/Messages/Server/WatchUserResponse.cs:90:                var fileCount = reader.ReadInteger();
src/Messaging/Messages/Server/WatchUserResponse.cs:91:                var directoryCount = reader.ReadInteger();
src/Messaging/Messages/Server/UserStatusResponseFactory.cs:49:            var presence = ProtocolValueValidator.ToDefinedEnum<UserPresence>(reader.ReadInteger(), "user presence");
src/Messaging/Messages/Server/UserStatisticsResponseFactory.cs:49:            var averageSpeed = reader.ReadInteger();
src/Messaging/Messages/Server/UserStatisticsResponseFactory.cs:50:            var uploadCount = reader.ReadLong();
src/Messaging/Messages/Server/UserStatisticsResponseFactory.cs:51:            var fileCount = reader.ReadInteger();
src/Messaging/Messages/Server/UserStatisticsResponseFactory.cs:52:            var directoryCount = reader.ReadInteger();
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:79:            var status = ProtocolValueValidator.ToDefinedEnum<UserPresence>(reader.ReadInteger(), "user presence");
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:81:            var averageSpeed = reader.ReadInteger();
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:82:            var downloadCount = reader.ReadLong();
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:83:            var fileCount = reader.ReadInteger();
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:84:            var directoryCount = reader.ReadInteger();
src/Messaging/Messages/Server/UserJoinedRoomNotification.cs:85:            var slotsFree = reader.ReadInteger();
src/Messaging/Messages/Server/UserAddressResponse.cs:129:            var ipBytes = reader.ReadBytes(4);
src/Messaging/Messages/Server/UserAddressResponse.cs:133:            var port = reader.ReadInteger();
src/Messaging/Messages/Server/UserAddressResponse.cs:141:                obfuscationType = reader.ReadInteger();
src/Messaging/Messages/Server/UserAddressResponse.cs:144:                obfuscatedPort = BinaryPrimitives.ReadUInt16LittleEndian(reader.ReadBytes(2));
src/Network/PeerConnectionManager.cs:252:            while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Network/PeerConnectionManager.cs:504:                while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Network/PeerConnectionManager.cs:719:            while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Network/PeerConnectionManager.cs:728:            while (true)
src/Network/PeerConnectionManager.cs:744:                    var tokenBytes = new byte[4];
src/Network/PeerConnectionManager.cs:758:                    while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Network/PeerConnectionManager.cs:794:            while (!MessageConnectionDictionary.IsEmpty)
src/Messaging/Messages/Server/ServerSearchRequest.cs:81:            var token = reader.ReadInteger();
src/Network/MessageConnection.cs:216:                while (!Disposed)
src/Network/MessageConnection.cs:351:            var encoded = new byte[8 + length];
src/Messaging/Messages/Server/RoomTickerListNotification.cs:104:            for (int i = 0; i < tickerCount; i++)
src/Messaging/Messages/Server/RoomListResponseFactory.cs:70:            for (int i = 0; i < userCountCount; i++)
src/Messaging/Messages/Server/RoomListResponseFactory.cs:72:                var count = reader.ReadInteger();
src/Messaging/Messages/Server/RoomListResponseFactory.cs:85:            for (int i = 0; i < roomCount; i++)
src/Network/ListenerHandler.cs:100:                    var obfuscatedMessage = new byte[8 + length];
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:53:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/PrivilegeNotification.cs:72:            var id = reader.ReadInteger();
src/Messaging/Messages/Server/PrivateRoomUserListNotification.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/PrivateRoomOwnedListNotification.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/PrivateMessageNotification.cs:92:            var id = reader.ReadInteger();
src/Messaging/Messages/Server/PrivateMessageNotification.cs:94:            var timestampSeconds = reader.ReadInteger();
src/Messaging/Messages/Server/NetInfoNotification.cs:116:            for (int i = 0; i < parentCount; i++)
src/Messaging/Messages/Server/NetInfoNotification.cs:120:                var ipBytes = reader.ReadBytes(4);
src/Messaging/Messages/Server/NetInfoNotification.cs:124:                var port = reader.ReadInteger();
src/Messaging/Messages/Server/LoginResponse.cs:111:                var ipBytes = reader.ReadBytes(4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:64:            for (int i = 0; i < statusCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:66:                statuses.Add(ProtocolValueValidator.ToDefinedEnum<UserPresence>(reader.ReadInteger(), "user presence"));
src/Messaging/Messages/Server/JoinRoomResponse.cs:73:            for (int i = 0; i < dataCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:75:                var averageSpeed = reader.ReadInteger();
src/Messaging/Messages/Server/JoinRoomResponse.cs:76:                var downloadCount = reader.ReadLong();
src/Messaging/Messages/Server/JoinRoomResponse.cs:77:                var fileCount = reader.ReadInteger();
src/Messaging/Messages/Server/JoinRoomResponse.cs:78:                var directoryCount = reader.ReadInteger();
src/Messaging/Messages/Server/JoinRoomResponse.cs:92:            for (int i = 0; i < slotsFreeCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:94:                var slotCount = reader.ReadInteger();
src/Messaging/Messages/Server/JoinRoomResponse.cs:103:            for (int i = 0; i < countryCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:110:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:131:                for (int i = 0; i < operatorCount; i++)
src/Messaging/Messages/Server/IntegerResponse.cs:44:            return reader.ReadInteger();
src/Network/DistributedConnectionManager.cs:706:            while (!ChildConnectionDictionary.IsEmpty)
src/Network/DistributedConnectionManager.cs:1037:            while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Network/DistributedConnectionManager.cs:1046:            while (true)
src/Network/DistributedConnectionManager.cs:1085:                    while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:53:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:153:            var ipBytes = reader.ReadBytes(4);
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:157:            var port = reader.ReadInteger();
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:160:            var token = reader.ReadInteger();
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:170:                obfuscationType = reader.ReadInteger();
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:171:                obfuscatedPort = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:79:            reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:82:            var token = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:68:                token = reader.ReadInteger();
src/Messaging/Messages/Peer/PeerSearchRequest.cs:69:            var token = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedBranchLevel.cs:69:            var level = reader.ReadInteger();
src/Messaging/Messages/Distributed/DistributedChildDepth.cs:69:            var depth = reader.ReadInteger();
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:57:            for (int i = 0; i < directoryCount; i++)
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:64:                _ = reader.ReadInteger();
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:70:                    for (int i = 0; i < lockedDirectoryCount; i++)
src/Messaging/Messages/Peer/TransferResponse.cs:115:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/TransferResponse.cs:123:                var fileSize = reader.ReadLong();
src/Messaging/MessageBuilder.cs:251:                byte[] buffer = new byte[2000];
src/Messaging/MessageBuilder.cs:254:                while ((len = input.Read(buffer, 0, 2000)) > 0)
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:77:            var placeInQueue = reader.ReadInteger();
src/Messaging/Messages/Peer/TransferRequest.cs:97:            var direction = ProtocolValueValidator.ToDefinedEnum<TransferDirection>(reader.ReadInteger(), "transfer direction");
src/Messaging/Messages/Peer/TransferRequest.cs:99:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/TransferRequest.cs:106:                fileSize = reader.ReadLong();
src/Messaging/MessageReaderExtensions.cs:50:            var size = reader.ReadLong();
src/Messaging/MessageReaderExtensions.cs:73:            for (int i = 0; i < attributeCount; i++)
src/Messaging/MessageReaderExtensions.cs:75:                var type = ProtocolValueValidator.ToDefinedEnum<FileAttributeType>(reader.ReadInteger(), "file attribute type");
src/Messaging/MessageReaderExtensions.cs:76:                var value = reader.ReadInteger();
src/Messaging/MessageReaderExtensions.cs:105:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Peer/FolderContentsRequest.cs:71:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/FolderContentsResponse.cs:104:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/FolderContentsResponse.cs:109:            for (int i = 0; i < directoryCount; i++)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:55:            var token = reader.ReadInteger();
src/Messaging/Messages/Peer/SearchResponseFactory.cs:61:            var uploadSpeed = reader.ReadInteger();
src/Messaging/Messages/Peer/SearchResponseFactory.cs:62:            var queueLength = reader.ReadInteger();
src/Messaging/Messages/Peer/SearchResponseFactory.cs:70:                _ = reader.ReadInteger();
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:62:                var pictureLen = reader.ReadInteger();
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:76:                picture = reader.ReadBytes(pictureLen);
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:79:            var uploadSlots = reader.ReadInteger();
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:80:            var queueLength = reader.ReadInteger();
src/Messaging/MessageReader.cs:148:        public byte[] ReadBytes(int count)
src/Messaging/MessageReader.cs:195:        public int ReadInteger()
src/Messaging/MessageReader.cs:215:        public long ReadLong()
src/Messaging/MessageReader.cs:256:            var length = ValidateSliceBounds(ReadInteger(), Payload.Length - Position);
src/Messaging/MessageReader.cs:316:                byte[] buffer = new byte[2000];
src/Messaging/MessageReader.cs:319:                while ((len = input.Read(buffer, 0, 2000)) > 0)
src/Messaging/Messages/Initialization/PierceFirewall.cs:68:                var token = reader.ReadInteger();
src/Messaging/Messages/Initialization/PeerInit.cs:84:                var token = reader.ReadInteger();
src/Messaging/Compression/Inflate.cs:170:			while (true)
src/Messaging/Compression/Inflate.cs:407:			while (n != 0 && m < 4)
src/Messaging/Compression/InfTree.cs:126:			while (i != 0);
src/Messaging/Compression/InfTree.cs:175:			while (--i != 0)
src/Messaging/Compression/InfTree.cs:193:			while (++i < n);
src/Messaging/Compression/InfTree.cs:209:				while (a-- != 0)
src/Messaging/Compression/InfTree.cs:213:					while (k > w + l)
src/Messaging/Compression/InfTree.cs:228:								while (++j < z)
src/Messaging/Compression/InfTree.cs:295:					while ((i & mask) != x[h])
src/Messaging/Compression/ZOutputStream.cs:59:			buf = new byte[bufsize];
src/Messaging/Compression/ZOutputStream.cs:101:		protected internal byte[] buf, buf1 = new byte[1];
src/Messaging/Compression/ZOutputStream.cs:138:			byte[] b = new byte[b1.Length];
src/Messaging/Compression/ZOutputStream.cs:156:			while (z.avail_in > 0 || z.avail_out == 0);
src/Messaging/Compression/ZOutputStream.cs:182:			while (z.avail_in > 0 || z.avail_out == 0);
src/Messaging/Compression/ZInputStream.cs:58:			buf = new byte[bufsize];
src/Messaging/Compression/ZInputStream.cs:95:		protected byte[] buf, buf1 = new byte[1];
src/Messaging/Compression/ZInputStream.cs:167:			while (z.avail_out == len && err == zlibConst.Z_OK);
src/Messaging/Compression/ZInputStream.cs:177:			byte[] tmp = new byte[len];
src/Messaging/Compression/InfCodes.cs:146:			while (true)
src/Messaging/Compression/InfCodes.cs:180:						while (k < (j))
src/Messaging/Compression/InfCodes.cs:245:						while (k < (j))
src/Messaging/Compression/InfCodes.cs:275:						while (k < (j))
src/Messaging/Compression/InfCodes.cs:325:						while (k < (j))
src/Messaging/Compression/InfCodes.cs:351:						while (f < 0)
src/Messaging/Compression/InfCodes.cs:356:						while (len != 0)
src/Messaging/Compression/InfCodes.cs:519:				while (k < (20))
src/Messaging/Compression/InfCodes.cs:550:						while (k < (15))
src/Messaging/Compression/InfCodes.cs:571:								while (k < (e))
src/Messaging/Compression/InfCodes.cs:608:									while (r < 0); // covers invalid distances
src/Messaging/Compression/InfCodes.cs:620:											while (--e != 0);
src/Messaging/Compression/InfCodes.cs:638:									while (--c != 0);
src/Messaging/Compression/InfCodes.cs:666:						while (true);
src/Messaging/Compression/InfCodes.cs:708:				while (true);
src/Messaging/Compression/InfCodes.cs:710:			while (m >= 258 && n >= 10);
src/Messaging/Compression/Tree.cs:180:				while (s.bl_count[bits] == 0)
src/Messaging/Compression/Tree.cs:189:			while (overflow > 0);
src/Messaging/Compression/Tree.cs:194:				while (n != 0)
src/Messaging/Compression/Tree.cs:247:			while (s.heap_len < 2)
src/Messaging/Compression/Tree.cs:289:			while (s.heap_len >= 2);
src/Messaging/Compression/Tree.cs:350:			while (--len > 0);
src/Messaging/Compression/SupportClass.cs:115:			byte[] receiver = new byte[target.Length];
src/Messaging/Compression/Adler32.cs:74:			while (len > 0)
src/Messaging/Compression/Adler32.cs:78:				while (k >= 16)
src/Messaging/Compression/Adler32.cs:104:					while (--k != 0);
src/Messaging/Compression/InfBlocks.cs:113:			window = new byte[w];
src/Messaging/Compression/InfBlocks.cs:160:			while (true)
src/Messaging/Compression/InfBlocks.cs:167:						while (k < (3))
src/Messaging/Compression/InfBlocks.cs:249:						while (k < (32))
src/Messaging/Compression/InfBlocks.cs:335:						while (k < (14))
src/Messaging/Compression/InfBlocks.cs:379:						while (index < 4 + (SupportClass.URShift(table, 10)))
src/Messaging/Compression/InfBlocks.cs:381:							while (k < (3))
src/Messaging/Compression/InfBlocks.cs:408:						while (index < 19)
src/Messaging/Compression/InfBlocks.cs:435:						while (true)
src/Messaging/Compression/InfBlocks.cs:448:							while (k < (t))
src/Messaging/Compression/InfBlocks.cs:487:								while (k < (t + i))
src/Messaging/Compression/InfBlocks.cs:533:								while (--j != 0);
src/Messaging/Compression/Deflate.cs:268:		internal byte[] depth = new byte[2 * L_CODES + 1];
src/Messaging/Compression/Deflate.cs:324:			for (int i = 0; i < hash_size - 1; i++)
src/Messaging/Compression/Deflate.cs:367:			for (int i = 0; i < L_CODES; i++)
src/Messaging/Compression/Deflate.cs:369:			for (int i = 0; i < D_CODES; i++)
src/Messaging/Compression/Deflate.cs:371:			for (int i = 0; i < BL_CODES; i++)
src/Messaging/Compression/Deflate.cs:387:			while (j <= heap_len)
src/Messaging/Compression/Deflate.cs:548:					while (--count != 0);
src/Messaging/Compression/Deflate.cs:754:				while (lx < last_lit);
src/Messaging/Compression/Deflate.cs:770:			while (n < 7)
src/Messaging/Compression/Deflate.cs:774:			while (n < 128)
src/Messaging/Compression/Deflate.cs:778:			while (n < LITERALS)
src/Messaging/Compression/Deflate.cs:866:			while (true)
src/Messaging/Compression/Deflate.cs:1042:					while (--n != 0);
src/Messaging/Compression/Deflate.cs:1054:					while (--n != 0);
src/Messaging/Compression/Deflate.cs:1084:			while (lookahead < MIN_LOOKAHEAD && strm.avail_in != 0);
src/Messaging/Compression/Deflate.cs:1098:			while (true)
src/Messaging/Compression/Deflate.cs:1167:						while (--match_length != 0);
src/Messaging/Compression/Deflate.cs:1219:			while (true)
src/Messaging/Compression/Deflate.cs:1302:					while (--prev_length != 0);
src/Messaging/Compression/Deflate.cs:1415:				while (window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && window[++scan] == window[++match] && scan < strend);
src/Messaging/Compression/Deflate.cs:1430:			while ((cur_match = (prev[cur_match & wmask] & 0xffff)) > limit && --chain_length != 0);
src/Messaging/Compression/Deflate.cs:1485:			window = new byte[w_size * 2];
src/Messaging/Compression/Deflate.cs:1493:			pending_buf = new byte[lit_bufsize * 4];
src/Messaging/Compression/Deflate.cs:1757:							for (int i = 0; i < hash_size; i++)

## Protocol counted collection loops
src/Messaging/Messages/Server/JoinRoomResponse.cs:52:            var userCount = ProtocolCountReader.ReadCount(reader, "room user", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:60:            var statusCount = ProtocolCountReader.ReadCount(reader, "room user status", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:64:            for (int i = 0; i < statusCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:69:            var dataCount = ProtocolCountReader.ReadCount(reader, "room user data", minimumBytesPerItem: 20);
src/Messaging/Messages/Server/JoinRoomResponse.cs:73:            for (int i = 0; i < dataCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:88:            var slotsFreeCount = ProtocolCountReader.ReadCount(reader, "room user slot", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:92:            for (int i = 0; i < slotsFreeCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:99:            var countryCount = ProtocolCountReader.ReadCount(reader, "room user country", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:103:            for (int i = 0; i < countryCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:110:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/JoinRoomResponse.cs:128:                operatorCount = ProtocolCountReader.ReadCount(reader, "room operator", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/JoinRoomResponse.cs:131:                for (int i = 0; i < operatorCount; i++)
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:50:            var count = ProtocolCountReader.ReadCount(reader, "excluded search phrase", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs:53:            for (int i = 0; i < count; i++)
src/Messaging/MessageReaderExtensions.cs:70:            var attributeCount = ProtocolCountReader.ReadCount(reader, "file attribute", minimumBytesPerItem: 8);
src/Messaging/MessageReaderExtensions.cs:73:            for (int i = 0; i < attributeCount; i++)
src/Messaging/MessageReaderExtensions.cs:101:        internal static IReadOnlyCollection<File> ReadFiles(this MessageReader<MessageCode.Peer> reader, int count)
src/Messaging/MessageReaderExtensions.cs:105:            for (int i = 0; i < count; i++)
src/Messaging/MessageReaderExtensions.cs:121:            var fileCount = ProtocolCountReader.ReadCount(reader, "directory file", minimumBytesPerItem: 4);
src/Messaging/MessageReaderExtensions.cs:125:            for (int j = 0; j < fileCount; j++)
src/Messaging/Messages/Server/ItemSimilarUsersResponse.cs:49:            var count = ProtocolCountReader.ReadCount(reader, "item similar user", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/ItemSimilarUsersResponse.cs:52:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/ItemRecommendationsResponse.cs:49:            var count = ProtocolCountReader.ReadCount(reader, "item recommendation", minimumBytesPerItem: 8);
src/Messaging/Messages/Server/ItemRecommendationsResponse.cs:52:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/SimilarUsersResponse.cs:48:            var count = ProtocolCountReader.ReadCount(reader, "similar user", minimumBytesPerItem: 8);
src/Messaging/Messages/Server/SimilarUsersResponse.cs:51:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:106:            var directoryCount = ProtocolCountReader.ReadCount(reader, "directory", minimumBytesPerItem: 4); // directory count, should always be 1
src/Messaging/Messages/Peer/FolderContentsResponse.cs:109:            for (int i = 0; i < directoryCount; i++)
src/Messaging/Messages/Server/UserInterestsResponse.cs:57:            var count = ProtocolCountReader.ReadCount(reader, "interest", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/UserInterestsResponse.cs:60:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/RecommendationsResponse.cs:56:            var count = ProtocolCountReader.ReadCount(reader, "recommendation", minimumBytesPerItem: 8);
src/Messaging/Messages/Server/RecommendationsResponse.cs:59:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:56:            var fileCount = ProtocolCountReader.ReadCount(reader, "file", minimumBytesPerItem: 4);
src/Messaging/Messages/Peer/SearchResponseFactory.cs:77:                var count = ProtocolCountReader.ReadCount(reader, "locked file", minimumBytesPerItem: 4);
src/Messaging/Messages/Peer/SearchResponseFactory.cs:78:                lockedFileList = reader.ReadFiles(count);
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:53:            var directoryCount = ProtocolCountReader.ReadCount(reader, "directory", minimumBytesPerItem: 4);
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:57:            for (int i = 0; i < directoryCount; i++)
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:68:                    var lockedDirectoryCount = ProtocolCountReader.ReadCount(reader, "locked directory", minimumBytesPerItem: 4);
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:70:                    for (int i = 0; i < lockedDirectoryCount; i++)
src/Messaging/Messages/Server/RoomListResponseFactory.cs:66:            var userCountCount = ProtocolCountReader.ReadCount(reader, "room user count", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/RoomListResponseFactory.cs:70:            for (int i = 0; i < userCountCount; i++)
src/Messaging/Messages/Server/RoomListResponseFactory.cs:82:            var roomCount = ProtocolCountReader.ReadCount(reader, "room name", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/RoomListResponseFactory.cs:85:            for (int i = 0; i < roomCount; i++)
src/Messaging/Messages/Server/NetInfoNotification.cs:113:            var parentCount = ProtocolCountReader.ReadCount(reader, "distributed parent", minimumBytesPerItem: 12);
src/Messaging/Messages/Server/NetInfoNotification.cs:116:            for (int i = 0; i < parentCount; i++)
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:50:            var count = ProtocolCountReader.ReadCount(reader, "privileged user", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/PrivilegedUserListNotification.cs:53:            for (int i = 0; i < count; i++)
src/Messaging/Messages/Server/PrivateRoomUserListNotification.cs:51:            var userCount = ProtocolCountReader.ReadCount(reader, "private room user", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/PrivateRoomUserListNotification.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/PrivateRoomOwnedListNotification.cs:51:            var userCount = ProtocolCountReader.ReadCount(reader, "owned private room user", minimumBytesPerItem: 4);
src/Messaging/Messages/Server/PrivateRoomOwnedListNotification.cs:55:            for (int i = 0; i < userCount; i++)
src/Messaging/Messages/Server/RoomTickerListNotification.cs:101:            var tickerCount = ProtocolCountReader.ReadCount(reader, "room ticker", minimumBytesPerItem: 8);
src/Messaging/Messages/Server/RoomTickerListNotification.cs:104:            for (int i = 0; i < tickerCount; i++)

## Protocol length-prefixed reads and payload allocations
src/Network/Tcp/ObfuscatedTransferConnection.cs:137:            var output = new byte[checked((int)length)];
src/Network/Tcp/ObfuscatedTransferConnection.cs:278:            var frame = new byte[FrameLengthBytes + payload.Length];
src/Network/Tcp/ObfuscatedTransferConnection.cs:295:            var encoded = new byte[8 + length];
src/Network/Tcp/RotatedObfuscation.cs:65:            var output = new byte[4 + input.Length];
src/Network/Tcp/RotatedObfuscation.cs:85:            var output = new byte[input.Length - 4];
src/Network/MessageConnection.cs:351:            var encoded = new byte[8 + length];
src/Network/ListenerHandler.cs:100:                    var obfuscatedMessage = new byte[8 + length];
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:62:                var pictureLen = reader.ReadInteger();
src/Messaging/MessageReader.cs:148:        public byte[] ReadBytes(int count)
src/Messaging/MessageReader.cs:242:            return ReadStringAndEncoding(encoding).Value;
src/Messaging/MessageReader.cs:254:        public (string Value, CharacterEncoding Encoding) ReadStringAndEncoding(CharacterEncoding encoding = null)

## Protocol compression boundary candidates
src/Messaging/MessageReader.cs:46:        internal const int MaximumDecompressedPayloadLength = 64 * 1024 * 1024;
src/Messaging/MessageReader.cs:103:        public MessageReader<T> Decompress()
src/Messaging/MessageReader.cs:115:            Decompress(Payload.ToArray(), out byte[] decompressedPayload);
src/Messaging/MessageReader.cs:312:        private void Decompress(byte[] inData, out byte[] outData)
src/Messaging/MessageReader.cs:329:                using var outMemoryStream = new BoundedMemoryStream(MaximumDecompressedPayloadLength);
src/Messaging/MessageReader.cs:354:        private sealed class BoundedMemoryStream : MemoryStream
src/Messaging/MessageReader.cs:356:            public BoundedMemoryStream(int maximumLength)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:102:            reader.Decompress();
src/Messaging/Messages/Peer/SearchResponseFactory.cs:52:            reader.Decompress();
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:51:            reader.Decompress();
src/Messaging/Compression/ZOutputStream.cs:59:			buf = new byte[bufsize];
src/Messaging/Compression/ZInputStream.cs:58:			buf = new byte[bufsize];
src/Messaging/Compression/ZInputStream.cs:177:			byte[] tmp = new byte[len];
src/Messaging/Compression/InfBlocks.cs:113:			window = new byte[w];
src/Messaging/Compression/Deflate.cs:1485:			window = new byte[w_size * 2];
src/Messaging/Compression/Deflate.cs:1493:			pending_buf = new byte[lit_bufsize * 4];

## Protocol scalar emission candidates
src/Messaging/MessageBuilderExtensions.cs:47:                .WriteByte((byte)file.Code)
src/Messaging/MessageBuilderExtensions.cs:48:                .WriteString(file.Filename)
src/Messaging/MessageBuilderExtensions.cs:49:                .WriteLong(file.Size)
src/Messaging/MessageBuilderExtensions.cs:50:                .WriteString(file.Extension)
src/Messaging/MessageBuilderExtensions.cs:51:                .WriteInteger(file.AttributeCount);
src/Messaging/MessageBuilderExtensions.cs:56:                    .WriteInteger((int)attribute.Type)
src/Messaging/MessageBuilderExtensions.cs:57:                    .WriteInteger(attribute.Value);
src/Messaging/MessageBuilderExtensions.cs:74:                .WriteString(directory.Name)
src/Messaging/MessageBuilderExtensions.cs:75:                .WriteInteger(directory.FileCount);
src/Messaging/MessageBuilder.cs:99:        public MessageBuilder WriteByte(byte value)
src/Messaging/MessageBuilder.cs:101:            return WriteBytes(new[] { value });
src/Messaging/MessageBuilder.cs:112:        public MessageBuilder WriteBytes(byte[] bytes)
src/Messaging/MessageBuilder.cs:188:        public MessageBuilder WriteInteger(int value)
src/Messaging/MessageBuilder.cs:190:            return WriteBytes(BitConverter.GetBytes(value));
src/Messaging/MessageBuilder.cs:201:        public MessageBuilder WriteLong(long value)
src/Messaging/MessageBuilder.cs:203:            return WriteBytes(BitConverter.GetBytes(value));
src/Messaging/MessageBuilder.cs:219:        public MessageBuilder WriteString(string value, CharacterEncoding encoding = null)
src/Messaging/MessageBuilder.cs:243:            return WriteBytes(BitConverter.GetBytes(bytes.Length))
src/Messaging/MessageBuilder.cs:244:                .WriteBytes(bytes);
src/Messaging/Messages/EmbeddedMessage.cs:77:                .WriteBytes(bytes.Skip(9).ToArray())
src/Messaging/Messages/Server/BranchRootCommand.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/BranchLevelCommand.cs:62:                .WriteInteger(Level)
src/Messaging/Messages/Server/AcknowledgePrivilegeNotificationCommand.cs:62:                .WriteInteger(Id)
src/Messaging/Messages/Server/ParentsIPCommand.cs:66:                .WriteBytes(ipBytes)
src/Messaging/Messages/Server/AcknowledgePrivateMessageCommand.cs:62:                .WriteInteger(Id)
src/Messaging/Messages/Server/NewPassword.cs:73:                .WriteString(Password)
src/Messaging/Messages/Server/AcceptChildrenCommand.cs:53:                .WriteByte((byte)(Accepted ? 1 : 0))
src/Messaging/Messages/Server/WishlistSearchRequest.cs:64:                .WriteInteger(Token)
src/Messaging/Messages/Server/WishlistSearchRequest.cs:65:                .WriteString(SearchText)
src/Messaging/Messages/Server/UserSearchRequest.cs:71:                .WriteString(Username)
src/Messaging/Messages/Server/UserSearchRequest.cs:72:                .WriteInteger(Token)
src/Messaging/Messages/Server/UserSearchRequest.cs:73:                .WriteString(SearchText)
src/Messaging/Messages/Server/SearchRequest.cs:64:                .WriteInteger(Token)
src/Messaging/Messages/Server/SearchRequest.cs:65:                .WriteString(SearchText)
src/Messaging/Messages/Server/LeaveRoomRequest.cs:53:                .WriteString(RoomName)
src/Messaging/Messages/Server/RoomSearchRequest.cs:71:                .WriteString(RoomName)
src/Messaging/Messages/Server/RoomSearchRequest.cs:72:                .WriteInteger(Token)
src/Messaging/Messages/Server/RoomSearchRequest.cs:73:                .WriteString(SearchText)
src/Messaging/Messages/Server/LoginRequest.cs:84:                .WriteString(Username)
src/Messaging/Messages/Server/LoginRequest.cs:85:                .WriteString(Password)
src/Messaging/Messages/Server/LoginRequest.cs:86:                .WriteInteger(Version)
src/Messaging/Messages/Server/LoginRequest.cs:87:                .WriteString(Hash)
src/Messaging/Messages/Server/LoginRequest.cs:88:                .WriteInteger(MinorVersion)
src/Messaging/Messages/Server/JoinRoomRequest.cs:60:                .WriteString(RoomName)
src/Messaging/Messages/Server/JoinRoomRequest.cs:61:                .WriteInteger(IsPrivate ? 1 : 0)
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:71:                .WriteInteger(Token)
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:72:                .WriteString(Username)
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:73:                .WriteString(Type)
src/Messaging/Messages/Server/HaveNoParentsCommand.cs:53:                .WriteByte((byte)(HaveNoParents ? 1 : 0))
src/Messaging/Messages/Server/CannotConnect.cs:93:                .WriteInteger(Token);
src/Messaging/Messages/Server/CannotConnect.cs:97:                builder.WriteString(Username);
src/Messaging/Messages/Server/GivePrivilegesCommand.cs:69:                .WriteString(Username)
src/Messaging/Messages/Server/GivePrivilegesCommand.cs:70:                .WriteInteger(Days)
src/Messaging/Messages/Server/MessageUsersCommand.cs:71:                .WriteInteger(Usernames.Count);
src/Messaging/Messages/Server/MessageUsersCommand.cs:75:                builder.WriteString(username);
src/Messaging/Messages/Server/MessageUsersCommand.cs:79:                .WriteString(Message)
src/Messaging/Messages/Server/ChildDepthCommand.cs:62:                .WriteInteger(Depth)
src/Messaging/Messages/Server/ItemRecommendationsRequest.cs:65:                .WriteString(Item)
src/Messaging/Messages/Server/SetListenPortCommand.cs:93:                .WriteInteger(Port);
src/Messaging/Messages/Server/SetListenPortCommand.cs:98:                    .WriteInteger(ObfuscationType.Value)
src/Messaging/Messages/Server/SetListenPortCommand.cs:99:                    .WriteInteger(ObfuscatedPort.Value);
src/Messaging/Messages/Server/UserInterestsRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/SendUploadSpeedCommand.cs:62:                .WriteInteger(Speed)
src/Messaging/Messages/Server/InterestCommand.cs:60:                .WriteString(Item)
src/Messaging/Messages/Server/WatchUserRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:96:                .WriteInteger(0)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:97:                .WriteString(Username)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:98:                .WriteInteger(Token)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:99:                .WriteString(Query)
src/Messaging/Messages/Server/UserStatusRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/RoomMessageCommand.cs:60:                .WriteString(RoomName)
src/Messaging/Messages/Server/RoomMessageCommand.cs:61:                .WriteString(Message)
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:82:                .WriteInteger(Token)
src/Messaging/Messages/Server/UserStatisticsRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/UserPrivilegesRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Distributed/DistributedChildDepth.cs:83:                .WriteInteger(Depth)
src/Messaging/Messages/Distributed/DistributedBranchRoot.cs:73:                .WriteString(Username)
src/Messaging/Messages/Peer/TransferResponse.cs:149:                .WriteInteger(Token)
src/Messaging/Messages/Peer/TransferResponse.cs:150:                .WriteByte((byte)(IsAllowed ? 1 : 0));
src/Messaging/Messages/Peer/TransferResponse.cs:154:                builder.WriteLong(FileSize);
src/Messaging/Messages/Peer/TransferResponse.cs:158:                builder.WriteString(Message);
src/Messaging/Messages/Server/PrivateRoomToggle.cs:78:                .WriteByte((byte)(AcceptInvitations ? 1 : 0))
src/Messaging/Messages/Distributed/DistributedBranchLevel.cs:83:                .WriteInteger(Level)
src/Messaging/Messages/Server/UserAddressRequest.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/PrivateRoomRemoveUser.cs:81:                .WriteString(RoomName)
src/Messaging/Messages/Server/PrivateRoomRemoveUser.cs:82:                .WriteString(Username)
src/Messaging/Messages/Peer/TransferRequest.cs:125:                .WriteInteger((int)Direction)
src/Messaging/Messages/Peer/TransferRequest.cs:126:                .WriteInteger(Token)
src/Messaging/Messages/Peer/TransferRequest.cs:127:                .WriteString(Filename)
src/Messaging/Messages/Peer/TransferRequest.cs:128:                .WriteLong(FileSize)
src/Messaging/Messages/Server/UnwatchUserCommand.cs:53:                .WriteString(Username)
src/Messaging/Messages/Server/PrivateRoomRemoveOperator.cs:81:                .WriteString(RoomName)
src/Messaging/Messages/Server/PrivateRoomRemoveOperator.cs:82:                .WriteString(Username)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:125:                .WriteInteger(Token)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:126:                .WriteString(DirectoryName)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:127:                .WriteInteger(DirectoryCount);
src/Messaging/Messages/Server/PrivateRoomDropOwnershipCommand.cs:53:                .WriteString(RoomName)
src/Messaging/Messages/Peer/FolderContentsRequest.cs:85:                .WriteInteger(Token)
src/Messaging/Messages/Peer/FolderContentsRequest.cs:86:                .WriteString(DirectoryName)
src/Messaging/Messages/Server/SetSharedCountsCommand.cs:74:                .WriteInteger(DirectoryCount)
src/Messaging/Messages/Server/SetSharedCountsCommand.cs:75:                .WriteInteger(FileCount)
src/Messaging/Messages/Server/PrivateRoomDropMembershipCommand.cs:53:                .WriteString(RoomName)
src/Messaging/Messages/Server/SetRoomTickerCommand.cs:60:                .WriteString(RoomName)
src/Messaging/Messages/Server/SetRoomTickerCommand.cs:61:                .WriteString(Message)
src/Messaging/Messages/Server/PrivateRoomAddUser.cs:81:                .WriteString(RoomName)
src/Messaging/Messages/Server/PrivateRoomAddUser.cs:82:                .WriteString(Username)
src/Messaging/MessageReader.cs:369:            public override void WriteByte(byte value)
src/Messaging/MessageReader.cs:372:                base.WriteByte(value);
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:101:                .WriteString(userInfo.Description)
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:102:                .WriteByte((byte)(userInfo.HasPicture ? 1 : 0));
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:107:                    .WriteInteger(userInfo.Picture.Length)
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:108:                    .WriteBytes(userInfo.Picture);
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:112:                .WriteInteger(userInfo.UploadSlots)
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:113:                .WriteInteger(userInfo.QueueLength)
src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:114:                .WriteByte((byte)(userInfo.HasFreeUploadSlot ? 1 : 0));
src/Messaging/Messages/Server/SetOnlineStatusCommand.cs:62:                .WriteInteger((int)Status)
src/Messaging/Messages/Server/PrivateRoomAddOperator.cs:81:                .WriteString(RoomName)
src/Messaging/Messages/Server/PrivateRoomAddOperator.cs:82:                .WriteString(Username)
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:73:                .WriteString(Filename)
src/Messaging/Messages/Peer/UploadFailed.cs:73:                .WriteString(Filename)
src/Messaging/Messages/Server/PrivateMessageCommand.cs:60:                .WriteString(Username)
src/Messaging/Messages/Server/PrivateMessageCommand.cs:61:                .WriteString(Message)
src/Messaging/Compression/ZOutputStream.cs:122:		public  void  WriteByte(int b)
src/Messaging/Compression/ZOutputStream.cs:128:		public override  void  WriteByte(byte b)
src/Messaging/Compression/ZOutputStream.cs:130:			WriteByte((int) b);
src/Messaging/Messages/Peer/UploadDenied.cs:81:                .WriteString(Filename)
src/Messaging/Messages/Peer/UploadDenied.cs:82:                .WriteString(Message)
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:89:                .WriteInteger(browseResponse.DirectoryCount);
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:96:            builder.WriteInteger(0);
src/Messaging/Messages/Peer/BrowseResponseFactory.cs:97:            builder.WriteInteger(browseResponse.LockedDirectoryCount);
src/Messaging/Messages/Peer/SearchResponseFactory.cs:93:                .WriteString(searchResponse.Username)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:94:                .WriteInteger(searchResponse.Token)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:95:                .WriteInteger(searchResponse.FileCount);
src/Messaging/Messages/Peer/SearchResponseFactory.cs:103:                .WriteByte((byte)(searchResponse.HasFreeUploadSlot ? 1 : 0))
src/Messaging/Messages/Peer/SearchResponseFactory.cs:104:                .WriteInteger(searchResponse.UploadSpeed)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:105:                .WriteInteger(searchResponse.QueueLength)
src/Messaging/Messages/Peer/SearchResponseFactory.cs:106:                .WriteInteger(0); // unknown value included for compatibility
src/Messaging/Messages/Peer/SearchResponseFactory.cs:108:            builder.WriteInteger(searchResponse.LockedFileCount);
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:72:                .WriteString(Filename)
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:92:                .WriteString(Filename)
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:93:                .WriteInteger(PlaceInQueue)
src/Messaging/Messages/Initialization/PeerInit.cs:108:                .WriteString(Username)
src/Messaging/Messages/Initialization/PeerInit.cs:109:                .WriteString(ConnectionType)
src/Messaging/Messages/Initialization/PeerInit.cs:110:                .WriteInteger(Token)
src/Messaging/Messages/Initialization/PierceFirewall.cs:92:                .WriteInteger(Token)

## Protocol scalar constructor guard candidates
src/Messaging/Compression/ZInputStream.cs:111:public ZInputStream(System.IO.Stream in_Renamed, int level)
src/Messaging/Compression/ZInputStream.cs:135:public int read(byte[] b, int off, int len)
src/Messaging/Compression/ZInputStream.cs:172:public long skip(long n)
src/Messaging/Compression/ZOutputStream.cs:114:public ZOutputStream(System.IO.Stream out_Renamed, int level)
src/Messaging/Compression/ZOutputStream.cs:122:public void WriteByte(int b)
src/Messaging/Compression/ZStream.cs:102:public int inflateInit(int w)
src/Messaging/Compression/ZStream.cs:108:public int inflate(int f)
src/Messaging/Compression/ZStream.cs:128:public int inflateSetDictionary(byte[] dictionary, int dictLength)
src/Messaging/Compression/ZStream.cs:135:public int deflateInit(int level)
src/Messaging/Compression/ZStream.cs:139:public int deflateInit(int level, int bits)
src/Messaging/Compression/ZStream.cs:144:public int deflate(int flush)
src/Messaging/Compression/ZStream.cs:160:public int deflateParams(int level, int strategy)
src/Messaging/Compression/ZStream.cs:166:public int deflateSetDictionary(byte[] dictionary, int dictLength)
src/Messaging/Handlers/PeerMessageHandler.cs:385:public void RegisterPeerMessageHandler(int messageCode, Func<string, IPEndPoint, byte[], Task> handler)
src/Messaging/Handlers/PeerMessageHandler.cs:411:public bool UnregisterPeerMessageHandler(int messageCode)
src/Messaging/MessageBuilder.cs:99:public MessageBuilder WriteByte(byte value)
src/Messaging/MessageBuilder.cs:112:public MessageBuilder WriteBytes(byte[] bytes)
src/Messaging/MessageBuilder.cs:155:public MessageBuilder WriteCode(MessageCode.Server code)
src/Messaging/MessageBuilder.cs:165:public MessageBuilder WriteCode(int code)
src/Messaging/MessageBuilder.cs:188:public MessageBuilder WriteInteger(int value)
src/Messaging/MessageBuilder.cs:201:public MessageBuilder WriteLong(long value)
src/Messaging/MessageBuilder.cs:219:public MessageBuilder WriteString(string value, CharacterEncoding encoding = null)
src/Messaging/MessageReader.cs:181:public bool HasRemainingBytes(int count)
src/Messaging/MessageReader.cs:282:public void Seek(int position)
src/Messaging/MessageReader.cs:356:public BoundedMemoryStream(int maximumLength)
src/Messaging/Messages/Distributed/DistributedBranchLevel.cs:39:public DistributedBranchLevel(int level)
src/Messaging/Messages/Distributed/DistributedChildDepth.cs:39:public DistributedChildDepth(int depth)
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:37:public DistributedPingResponse(int token)
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:39:public DistributedSearchRequest(string username, int token, string query)
src/Messaging/Messages/Initialization/PeerInit.cs:39:public PeerInit(string username, string connectionType, int token)
src/Messaging/Messages/Initialization/PierceFirewall.cs:37:public PierceFirewall(int token)
src/Messaging/Messages/Peer/FolderContentsRequest.cs:38:public FolderContentsRequest(int token, string directoryName)
src/Messaging/Messages/Peer/FolderContentsResponse.cs:43:public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
src/Messaging/Messages/Peer/PeerSearchRequest.cs:36:public PeerSearchRequest(int token, string query)
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:40:public PlaceInQueueResponse(string filename, int placeInQueue)
src/Messaging/Messages/Peer/TransferRequest.cs:42:public TransferRequest(TransferDirection direction, int token, string filename, long fileSize = 0)
src/Messaging/Messages/Peer/TransferResponse.cs:40:public TransferResponse(int token, string message)
src/Messaging/Messages/Peer/TransferResponse.cs:54:public TransferResponse(int token, long fileSize)
src/Messaging/Messages/Peer/TransferResponse.cs:72:public TransferResponse(int token)
src/Messaging/Messages/Server/AcknowledgePrivateMessageCommand.cs:39:public AcknowledgePrivateMessageCommand(int id)
src/Messaging/Messages/Server/AcknowledgePrivilegeNotificationCommand.cs:39:public AcknowledgePrivilegeNotificationCommand(int id)
src/Messaging/Messages/Server/BranchLevelCommand.cs:39:public BranchLevelCommand(int level)
src/Messaging/Messages/Server/CannotConnect.cs:38:public CannotConnect(int token, string username = null)
src/Messaging/Messages/Server/ChildDepthCommand.cs:39:public ChildDepthCommand(int depth)
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:39:public ConnectToPeerRequest(int token, string username, string type)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:51:public ConnectToPeerResponse(string username, string type, IPAddress ipAddress, int port, int token, bool isPrivileged, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/ConnectToPeerResponse.cs:66:public ConnectToPeerResponse(string username, string type, IPEndPoint endpoint, int token, bool isPrivileged, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/GivePrivilegesCommand.cs:40:public GivePrivilegesCommand(string username, int days)
src/Messaging/Messages/Server/InterestCommand.cs:36:public InterestCommand(MessageCode.Server code, string item)
src/Messaging/Messages/Server/ItemRecommendationsRequest.cs:36:public ItemRecommendationsRequest(MessageCode.Server code, string item)
src/Messaging/Messages/Server/LoginRequest.cs:39:public LoginRequest(int minorVersion, string username, string password)
src/Messaging/Messages/Server/NetInfoNotification.cs:45:public NetInfoNotification(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)
src/Messaging/Messages/Server/PrivateMessageNotification.cs:43:public PrivateMessageNotification(int id, DateTime timestamp, string username, string message, bool replayed)
src/Messaging/Messages/Server/PrivilegeNotification.cs:41:public PrivilegeNotification(int id, string username)
src/Messaging/Messages/Server/RoomSearchRequest.cs:39:public RoomSearchRequest(string roomName, string searchText, int token)
src/Messaging/Messages/Server/RoomTickerListNotification.cs:43:public RoomTickerListNotification( string roomName, int tickerCount, IEnumerable<RoomTicker> tickers)
src/Messaging/Messages/Server/SearchRequest.cs:38:public SearchRequest(string searchText, int token)
src/Messaging/Messages/Server/SendUploadSpeedCommand.cs:39:public SendUploadSpeedCommand(int speed)
src/Messaging/Messages/Server/ServerSearchRequest.cs:41:public ServerSearchRequest(string username, int token, string query)
src/Messaging/Messages/Server/SetListenPortCommand.cs:43:public SetListenPortCommand(int port, int? obfuscationType = null, int? obfuscatedPort = null)
src/Messaging/Messages/Server/SetOnlineStatusCommand.cs:39:public SetOnlineStatusCommand(UserPresence status)
src/Messaging/Messages/Server/SetSharedCountsCommand.cs:40:public SetSharedCountsCommand(int directoryCount, int fileCount)
src/Messaging/Messages/Server/UserAddressResponse.cs:49:public UserAddressResponse(string username, IPAddress ipAddress, int port, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/UserAddressResponse.cs:61:public UserAddressResponse(string username, IPEndPoint endpoint, int obfuscationType = 0, int obfuscatedPort = 0)
src/Messaging/Messages/Server/UserSearchRequest.cs:39:public UserSearchRequest(string username, string searchText, int token)
src/Messaging/Messages/Server/WishlistSearchRequest.cs:38:public WishlistSearchRequest(string searchText, int token)

## Resolver output and raw stream candidates
src/SoulseekClient.cs:1183:        ///     <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
src/SoulseekClient.cs:1191:        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
src/SoulseekClient.cs:1209:        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
src/SoulseekClient.cs:1225:        public Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1257:            if (outputStreamFactory == null)
src/SoulseekClient.cs:1259:                throw new ArgumentNullException(nameof(outputStreamFactory), "The specified output stream factory is null");
src/SoulseekClient.cs:1287:            return DownloadToStreamAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1441:        ///         <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
src/SoulseekClient.cs:1445:        ///         <see cref="DownloadAsync(string, string, Func{Task{Stream}}, long?, long, int?, TransferOptions, CancellationToken?)"/>,
src/SoulseekClient.cs:1461:        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
src/SoulseekClient.cs:1476:        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
src/SoulseekClient.cs:1492:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1512:            var downloadTask = DownloadAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);
src/SoulseekClient.cs:1596:        ///         <see cref="Stream"/> created by the specified <paramref name="inputStreamFactory"/> to the the specified
src/SoulseekClient.cs:1601:        ///         <see cref="UploadAsync(string, string, long, Func{long, Task{Stream}}, int?, TransferOptions, CancellationToken?)"/>,
src/SoulseekClient.cs:1608:        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
src/SoulseekClient.cs:1619:        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
src/SoulseekClient.cs:1632:        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1652:            var uploadTask = UploadAsync(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);
src/SoulseekClient.cs:2711:            return SendPeerMessageInternalAsync(username, PeerCapabilityEnvelope.MessageCode, envelope.ToByteArray(), cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:3083:                using var stream = IOAdapter.GetFileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
src/SoulseekClient.cs:3114:        ///     Asynchronously uploads the specified <paramref name="remoteFilename"/> from the <see cref="Stream"/> created by
src/SoulseekClient.cs:3115:        ///     the specified <paramref name="inputStreamFactory"/> to the the specified <paramref name="username"/> using the
src/SoulseekClient.cs:3121:        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
src/SoulseekClient.cs:3132:        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
src/SoulseekClient.cs:3145:        public Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:3162:            if (inputStreamFactory == null)
src/SoulseekClient.cs:3164:                throw new ArgumentNullException(nameof(inputStreamFactory), "The specified input stream factory is null");
src/SoulseekClient.cs:3192:            return UploadFromStreamAsync(username, remoteFilename, size, inputStreamFactory, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:3565:                    var loginBytes = new LoginRequest(MinorVersion, username, password).ToByteArray()
src/SoulseekClient.cs:3566:                        .Concat(CreateSetListenPortCommand().ToByteArray())
src/SoulseekClient.cs:3627:            options = options.WithDisposalOptions(disposeOutputStreamOnCompletion: true);
src/SoulseekClient.cs:3636:            return await DownloadToStreamAsync(username, remoteFilename, () => Task.FromResult((Stream)IOAdapter.GetFileStream(localFilename, fileMode, FileAccess.Write, FileShare.None)), size, startOffset, token, options, cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:3639:        private async Task<Transfer> DownloadToStreamAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:3696:            Stream outputStream = null;
src/SoulseekClient.cs:3844:                outputStream = await outputStreamFactory().ConfigureAwait(false);
src/SoulseekClient.cs:3853:                    anyone that sets SeekOutputStreamAutomatically to false and passes a stream positioned at anything
src/SoulseekClient.cs:3857:                if (download.StartOffset > 0 && options.SeekOutputStreamAutomatically)
src/SoulseekClient.cs:3859:                    if (!outputStream.CanSeek)
src/SoulseekClient.cs:3861:                        throw new TransferStreamException($"Requested non-zero start offset but output stream does not support seeking");
src/SoulseekClient.cs:3865:                    outputStream.Seek(download.StartOffset, SeekOrigin.Begin);
src/SoulseekClient.cs:3879:                    outputStream: outputStream,
src/SoulseekClient.cs:3917:                UpdateProgress(outputStream.Position);
src/SoulseekClient.cs:3920:                Diagnostic.Info($"Download of {GetDiagnosticLogValue(download.Filename)} from {username} complete ({outputStream.Position} of {download.Size} bytes).");
src/SoulseekClient.cs:3945:                UpdateProgress(outputStream?.Position ?? 0);
src/SoulseekClient.cs:3959:                UpdateProgress(outputStream?.Position ?? 0);
src/SoulseekClient.cs:3971:                UpdateProgress(outputStream?.Position ?? 0);
src/SoulseekClient.cs:4011:                    long finalStreamPosition = 0;
src/SoulseekClient.cs:4014:                    // which can happen depending on the stream type (e.g. FileStream.Position can throw if the file is closed),
src/SoulseekClient.cs:4018:                        finalStreamPosition = outputStream?.Position ?? 0;
src/SoulseekClient.cs:4025:                    if (options.DisposeOutputStreamOnCompletion && outputStream != null)
src/SoulseekClient.cs:4031:                                await outputStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
src/SoulseekClient.cs:4036:                                outputStream.Dispose();
src/SoulseekClient.cs:4038:                                await outputStream.DisposeAsync().ConfigureAwait(false);
src/SoulseekClient.cs:4601:                    searchResponseResolver: patch.SearchResponseResolver,
src/SoulseekClient.cs:4603:                    browseResponseResolver: patch.BrowseResponseResolver,
src/SoulseekClient.cs:4604:                    directoryContentsResolver: patch.DirectoryContentsResolver,
src/SoulseekClient.cs:4605:                    userInfoResolver: patch.UserInfoResolver,
src/SoulseekClient.cs:4607:                    placeInQueueResolver: patch.PlaceInQueueResolver);
src/SoulseekClient.cs:4718:                        SearchScopeType.Room => new RoomSearchRequest(scope.Subjects.First(), search.Query.SearchText, search.Token).ToByteArray(),
src/SoulseekClient.cs:4719:                        SearchScopeType.User => scope.Subjects.SelectMany(u => new UserSearchRequest(u, search.Query.SearchText, search.Token).ToByteArray()).ToArray(),
src/SoulseekClient.cs:4720:                        SearchScopeType.Wishlist => new WishlistSearchRequest(search.Query.SearchText, search.Token).ToByteArray(),
src/SoulseekClient.cs:4721:                        _ => new SearchRequest(search.Query.SearchText, search.Token).ToByteArray()
src/SoulseekClient.cs:4882:                await SendPeerMessageInternalAsync(username, PeerCapabilityEnvelope.MessageCode, response.ToByteArray(), CancellationToken.None).ConfigureAwait(false);
src/SoulseekClient.cs:5079:            options = options.WithDisposalOptions(disposeInputStreamOnCompletion: true);
src/SoulseekClient.cs:5084:            return await UploadFromStreamAsync(username, remoteFilename, length, (_) => Task.FromResult((Stream)ioAdapter.GetFileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read)), token, options, cancellationToken).ConfigureAwait(false);
src/SoulseekClient.cs:5087:        private async Task<Transfer> UploadFromStreamAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:5145:            Stream inputStream = null;
src/SoulseekClient.cs:5276:                inputStream = await inputStreamFactory(upload.StartOffset).ConfigureAwait(false);
src/SoulseekClient.cs:5278:                if (upload.StartOffset > 0 && options.SeekInputStreamAutomatically)
src/SoulseekClient.cs:5280:                    if (!inputStream.CanSeek)
src/SoulseekClient.cs:5282:                        throw new TransferStreamException($"Requested non-zero start offset but input stream does not support seeking");
src/SoulseekClient.cs:5286:                    inputStream.Seek(upload.StartOffset, SeekOrigin.Begin);
src/SoulseekClient.cs:5301:                        inputStream: inputStream,
src/SoulseekClient.cs:5362:                UpdateProgress(inputStream.Position);
src/SoulseekClient.cs:5365:                Diagnostic.Info($"Upload of {GetDiagnosticLogValue(upload.Filename)} to {username} complete ({inputStream.Position} of {upload.Size} bytes).");
src/SoulseekClient.cs:5381:                UpdateProgress(inputStream?.Position ?? 0);
src/SoulseekClient.cs:5395:                UpdateProgress(inputStream?.Position ?? 0);
src/SoulseekClient.cs:5407:                UpdateProgress(inputStream?.Position ?? 0);
src/SoulseekClient.cs:5437:                    long finalStreamPosition = 0;
src/SoulseekClient.cs:5440:                    // which can happen depending on the stream type (e.g. FileStream.Position can throw if the file is closed),
src/SoulseekClient.cs:5444:                        finalStreamPosition = inputStream?.Position ?? 0;
src/SoulseekClient.cs:5451:                    if (options.DisposeInputStreamOnCompletion && inputStream != null)
src/SoulseekClient.cs:5456:                            inputStream.Dispose();
src/SoulseekClient.cs:5458:                            await inputStream.DisposeAsync().ConfigureAwait(false);
src/Options/TransferOptions.cs:61:        /// <param name="seekInputStreamAutomatically">
src/Options/TransferOptions.cs:64:        /// <param name="seekOutputStreamAutomatically">
src/Options/TransferOptions.cs:67:        /// <param name="disposeInputStreamOnCompletion">
src/Options/TransferOptions.cs:70:        /// <param name="disposeOutputStreamOnCompletion">
src/Options/TransferOptions.cs:81:            bool seekInputStreamAutomatically = true,
src/Options/TransferOptions.cs:82:            bool seekOutputStreamAutomatically = true,
src/Options/TransferOptions.cs:83:            bool disposeInputStreamOnCompletion = true,
src/Options/TransferOptions.cs:84:            bool disposeOutputStreamOnCompletion = true)
src/Options/TransferOptions.cs:91:            SeekInputStreamAutomatically = seekInputStreamAutomatically;
src/Options/TransferOptions.cs:92:            SeekOutputStreamAutomatically = seekOutputStreamAutomatically;
src/Options/TransferOptions.cs:93:            DisposeInputStreamOnCompletion = disposeInputStreamOnCompletion;
src/Options/TransferOptions.cs:94:            DisposeOutputStreamOnCompletion = disposeOutputStreamOnCompletion;
src/Options/TransferOptions.cs:108:        public bool DisposeInputStreamOnCompletion { get; }
src/Options/TransferOptions.cs:113:        public bool DisposeOutputStreamOnCompletion { get; }
src/Options/TransferOptions.cs:142:        public bool SeekInputStreamAutomatically { get; }
src/Options/TransferOptions.cs:148:        public bool SeekOutputStreamAutomatically { get; }
src/Options/TransferOptions.cs:184:                seekInputStreamAutomatically: SeekInputStreamAutomatically,
src/Options/TransferOptions.cs:185:                seekOutputStreamAutomatically: SeekOutputStreamAutomatically,
src/Options/TransferOptions.cs:186:                disposeInputStreamOnCompletion: DisposeInputStreamOnCompletion,
src/Options/TransferOptions.cs:187:                disposeOutputStreamOnCompletion: DisposeOutputStreamOnCompletion);
src/Options/TransferOptions.cs:193:        /// <param name="disposeInputStreamOnCompletion">
src/Options/TransferOptions.cs:196:        /// <param name="disposeOutputStreamOnCompletion">
src/Options/TransferOptions.cs:201:            bool? disposeInputStreamOnCompletion = null,
src/Options/TransferOptions.cs:202:            bool? disposeOutputStreamOnCompletion = null)
src/Options/TransferOptions.cs:212:                seekInputStreamAutomatically: SeekInputStreamAutomatically,
src/Options/TransferOptions.cs:213:                seekOutputStreamAutomatically: SeekOutputStreamAutomatically,
src/Options/TransferOptions.cs:214:                disposeInputStreamOnCompletion: disposeInputStreamOnCompletion ?? DisposeInputStreamOnCompletion,
src/Options/TransferOptions.cs:215:                disposeOutputStreamOnCompletion: disposeOutputStreamOnCompletion ?? DisposeOutputStreamOnCompletion);
src/Options/SoulseekClientOptionsPatch.cs:70:        /// <param name="searchResponseResolver">
src/Options/SoulseekClientOptionsPatch.cs:76:        /// <param name="browseResponseResolver">
src/Options/SoulseekClientOptionsPatch.cs:79:        /// <param name="directoryContentsResolver">
src/Options/SoulseekClientOptionsPatch.cs:82:        /// <param name="userInfoResolver">The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.</param>
src/Options/SoulseekClientOptionsPatch.cs:84:        /// <param name="placeInQueueResolver">
src/Options/SoulseekClientOptionsPatch.cs:113:            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
src/Options/SoulseekClientOptionsPatch.cs:115:            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
src/Options/SoulseekClientOptionsPatch.cs:116:            Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null,
src/Options/SoulseekClientOptionsPatch.cs:117:            Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null,
src/Options/SoulseekClientOptionsPatch.cs:119:            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null)
src/Options/SoulseekClientOptionsPatch.cs:169:            SearchResponseResolver = searchResponseResolver;
src/Options/SoulseekClientOptionsPatch.cs:172:            BrowseResponseResolver = browseResponseResolver;
src/Options/SoulseekClientOptionsPatch.cs:173:            DirectoryContentsResolver = directoryContentsResolver;
src/Options/SoulseekClientOptionsPatch.cs:175:            UserInfoResolver = userInfoResolver;
src/Options/SoulseekClientOptionsPatch.cs:177:            PlaceInQueueResolver = placeInQueueResolver;
src/Options/SoulseekClientOptionsPatch.cs:203:        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:213:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:282:        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:292:        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:312:        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResolver { get; }
src/Options/SoulseekClientOptions.cs:44:        private readonly Func<string, IPEndPoint, Task<BrowseResponse>> defaultBrowseResponseResolver =
src/Options/SoulseekClientOptions.cs:50:        private readonly Func<string, IPEndPoint, string, Task<int?>> defaultPlaceInQueueResolver =
src/Options/SoulseekClientOptions.cs:53:        private readonly Func<string, IPEndPoint, Task<UserInfo>> defaultUserInfoResolver =
src/Options/SoulseekClientOptions.cs:92:        /// <param name="searchResponseResolver">
src/Options/SoulseekClientOptions.cs:98:        /// <param name="browseResponseResolver">
src/Options/SoulseekClientOptions.cs:101:        /// <param name="directoryContentsResolver">
src/Options/SoulseekClientOptions.cs:104:        /// <param name="userInfoResolver">The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.</param>
src/Options/SoulseekClientOptions.cs:106:        /// <param name="placeInQueueResolver">
src/Options/SoulseekClientOptions.cs:142:            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
src/Options/SoulseekClientOptions.cs:144:            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
src/Options/SoulseekClientOptions.cs:145:            Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null,
src/Options/SoulseekClientOptions.cs:146:            Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null,
src/Options/SoulseekClientOptions.cs:148:            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null,
src/Options/SoulseekClientOptions.cs:234:            SearchResponseResolver = searchResponseResolver;
src/Options/SoulseekClientOptions.cs:237:            BrowseResponseResolver = browseResponseResolver ?? defaultBrowseResponseResolver;
src/Options/SoulseekClientOptions.cs:238:            DirectoryContentsResolver = directoryContentsResolver;
src/Options/SoulseekClientOptions.cs:240:            UserInfoResolver = userInfoResolver ?? defaultUserInfoResolver;
src/Options/SoulseekClientOptions.cs:242:            PlaceInQueueResolver = placeInQueueResolver ?? defaultPlaceInQueueResolver;
src/Options/SoulseekClientOptions.cs:272:        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }
src/Options/SoulseekClientOptions.cs:283:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Options/SoulseekClientOptions.cs:386:        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResolver { get; }
src/Options/SoulseekClientOptions.cs:396:        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }
src/Options/SoulseekClientOptions.cs:421:        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResolver { get; }
src/Options/SoulseekClientOptions.cs:461:                searchResponseResolver: patch.SearchResponseResolver,
src/Options/SoulseekClientOptions.cs:463:                browseResponseResolver: patch.BrowseResponseResolver,
src/Options/SoulseekClientOptions.cs:464:                directoryContentsResolver: patch.DirectoryContentsResolver,
src/Options/SoulseekClientOptions.cs:465:                userInfoResolver: patch.UserInfoResolver,
src/Options/SoulseekClientOptions.cs:467:                placeInQueueResolver: patch.PlaceInQueueResolver);
src/Options/SoulseekClientOptions.cs:498:        /// <param name="searchResponseResolver">
src/Options/SoulseekClientOptions.cs:504:        /// <param name="browseResponseResolver">
src/Options/SoulseekClientOptions.cs:507:        /// <param name="directoryContentsResolver">
src/Options/SoulseekClientOptions.cs:510:        /// <param name="userInfoResolver">The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.</param>
src/Options/SoulseekClientOptions.cs:512:        /// <param name="placeInQueueResolver">
src/Options/SoulseekClientOptions.cs:536:            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
src/Options/SoulseekClientOptions.cs:538:            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
src/Options/SoulseekClientOptions.cs:539:            Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null,
src/Options/SoulseekClientOptions.cs:540:            Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null,
src/Options/SoulseekClientOptions.cs:542:            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null)
src/Options/SoulseekClientOptions.cs:569:                searchResponseResolver: searchResponseResolver ?? SearchResponseResolver,
src/Options/SoulseekClientOptions.cs:571:                browseResponseResolver: browseResponseResolver ?? BrowseResponseResolver,
src/Options/SoulseekClientOptions.cs:572:                directoryContentsResolver: directoryContentsResolver ?? DirectoryContentsResolver,
src/Options/SoulseekClientOptions.cs:573:                userInfoResolver: userInfoResolver ?? UserInfoResolver,
src/Options/SoulseekClientOptions.cs:575:                placeInQueueResolver: placeInQueueResolver ?? PlaceInQueueResolver);
src/Messaging/Messages/Server/BranchLevelCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Server/AcknowledgePrivilegeNotificationCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Server/AcknowledgePrivateMessageCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Server/AcceptChildrenCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/TransferResponse.cs:145:        public byte[] ToByteArray()
src/Messaging/Messages/Server/WishlistSearchRequest.cs:60:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/TransferRequest.cs:121:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UserSearchRequest.cs:67:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/FolderContentsResponse.cs:121:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SearchRequest.cs:60:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/FolderContentsRequest.cs:81:        public byte[] ToByteArray()
src/Messaging/Messages/Server/RoomSearchRequest.cs:67:        public byte[] ToByteArray()
src/Messaging/Messages/Server/LoginRequest.cs:80:        public byte[] ToByteArray()
src/Messaging/Messages/Server/ConnectToPeerRequest.cs:67:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/UserInfoRequest.cs:35:        public byte[] ToByteArray()
src/Messaging/Messages/Server/CannotConnect.cs:87:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/UploadFailed.cs:69:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/UploadDenied.cs:77:        public byte[] ToByteArray()
src/Messaging/Messages/Server/MessageUsersCommand.cs:67:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/QueueDownloadRequest.cs:68:        public byte[] ToByteArray()
src/Messaging/Messages/Server/ItemRecommendationsRequest.cs:61:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/PlaceInQueueResponse.cs:88:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SimilarUsersRequest.cs:35:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/PlaceInQueueRequest.cs:69:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UserInterestsRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/RecommendationsRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/InterestCommand.cs:56:        public byte[] ToByteArray()
src/Messaging/Messages/Peer/BrowseRequest.cs:35:        public byte[] ToByteArray()
src/Messaging/Messages/Server/WatchUserRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UserStatusRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Initialization/PierceFirewall.cs:88:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UserStatisticsRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UserPrivilegesRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Initialization/PeerInit.cs:104:        public byte[] ToByteArray()
src/Messaging/Messages/IOutgoingMessage.cs:35:        byte[] ToByteArray();
src/Messaging/Messages/Server/UserAddressRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/UnwatchUserCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/StopPublicChatCommand.cs:42:        public byte[] ToByteArray()
src/Messaging/Messages/Server/StartPublicChatCommand.cs:42:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedSearchRequest.cs:92:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SetSharedCountsCommand.cs:70:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedPingResponse.cs:78:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SetRoomTickerCommand.cs:56:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SetOnlineStatusCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedPingRequest.cs:60:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SetListenPortCommand.cs:89:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedChildDepth.cs:79:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedBranchRoot.cs:69:        public byte[] ToByteArray()
src/Messaging/Messages/Server/ServerPing.cs:60:        public byte[] ToByteArray()
src/Messaging/Messages/Server/SendUploadSpeedCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Distributed/DistributedBranchLevel.cs:79:        public byte[] ToByteArray()
src/Messaging/Messages/Server/RoomMessageCommand.cs:56:        public byte[] ToByteArray()
src/Messaging/Messages/Server/RoomListRequest.cs:42:        public byte[] ToByteArray()
src/Messaging/MessageReader.cs:314:            static void CopyStream(Stream input, Stream output)
src/Messaging/MessageReader.cs:329:                using var outMemoryStream = new BoundedMemoryStream(MaximumDecompressedPayloadLength);
src/Messaging/MessageReader.cs:330:                using var outZStream = new ZOutputStream(outMemoryStream);
src/Messaging/MessageReader.cs:331:                using var inMemoryStream = new MemoryStream(inData);
src/Messaging/MessageReader.cs:332:                CopyStream(inMemoryStream, outZStream);
src/Messaging/MessageReader.cs:333:                outZStream.finish();
src/Messaging/MessageReader.cs:334:                outData = outMemoryStream.ToArray();
src/Messaging/MessageReader.cs:354:        private sealed class BoundedMemoryStream : MemoryStream
src/Messaging/MessageReader.cs:356:            public BoundedMemoryStream(int maximumLength)
src/Messaging/Messages/Server/PrivateRoomToggle.cs:74:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateRoomRemoveUser.cs:77:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateRoomRemoveOperator.cs:77:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateRoomDropOwnershipCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/MessageBuilder.cs:249:            static void CopyStream(Stream input, Stream output)
src/Messaging/MessageBuilder.cs:264:                using MemoryStream outMemoryStream = new MemoryStream();
src/Messaging/MessageBuilder.cs:265:                using ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION);
src/Messaging/MessageBuilder.cs:266:                using Stream inMemoryStream = new MemoryStream(inData);
src/Messaging/MessageBuilder.cs:268:                CopyStream(inMemoryStream, outZStream);
src/Messaging/MessageBuilder.cs:269:                outZStream.finish();
src/Messaging/MessageBuilder.cs:270:                outData = outMemoryStream.ToArray();
src/Messaging/Messages/Server/PrivateRoomDropMembershipCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/JoinRoomRequest.cs:56:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateRoomAddUser.cs:77:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateRoomAddOperator.cs:77:        public byte[] ToByteArray()
src/Messaging/Messages/Server/HaveNoParentsCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/PrivateMessageCommand.cs:56:        public byte[] ToByteArray()
src/Messaging/Messages/Server/GivePrivilegesCommand.cs:65:        public byte[] ToByteArray()
src/Messaging/Messages/Server/ParentsIPCommand.cs:54:        public byte[] ToByteArray()
src/Messaging/Messages/Server/LeaveRoomRequest.cs:49:        public byte[] ToByteArray()
src/Messaging/Messages/Server/ChildDepthCommand.cs:58:        public byte[] ToByteArray()
src/Messaging/Messages/Server/NewPassword.cs:69:        public byte[] ToByteArray()
src/Messaging/Messages/Server/CheckPrivilegesRequest.cs:42:        public byte[] ToByteArray()
src/Messaging/Messages/Server/BranchRootCommand.cs:49:        public byte[] ToByteArray()
src/Messaging/Handlers/PeerMessageHandler.cs:151:                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:161:                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:166:                        await connection.WriteAsync(outgoingInfo.ToByteArray()).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:174:                        if (SoulseekClient.Options.SearchResponseResolver == default)
src/Messaging/Handlers/PeerMessageHandler.cs:181:                            var peerSearchResponse = await SoulseekClient.Options.SearchResponseResolver(connection.Username, searchRequest.Token, SearchQuery.FromText(searchRequest.Query)).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:183:                            if (peerSearchResponse is RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:187:                                    await WriteRawSearchResponseAsync(connection, rawSearchResponse).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:191:                                    DisposeRawSearchResponseStream(rawSearchResponse);
src/Messaging/Handlers/PeerMessageHandler.cs:196:                                await connection.WriteAsync(peerSearchResponse.ToByteArray()).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:211:                            browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:216:                                .BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:221:                        if (browseResponse is RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:223:                            await WriteRawBrowseResponseAsync(connection, rawBrowseResponse).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:227:                            await connection.WriteAsync(browseResponse.ToByteArray()).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:243:                            outgoingFolderContents = await SoulseekClient.Options.DirectoryContentsResolver(
src/Messaging/Handlers/PeerMessageHandler.cs:462:        private static void DisposeRawBrowseResponseStream(RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:466:                rawBrowseResponse.Stream?.Dispose();
src/Messaging/Handlers/PeerMessageHandler.cs:474:        private static void DisposeRawSearchResponseStream(RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:478:                rawSearchResponse.Stream?.Dispose();
src/Messaging/Handlers/PeerMessageHandler.cs:486:        private static async Task WriteRawBrowseResponseAsync(IMessageConnection connection, RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:490:                await connection.WriteAsync(rawBrowseResponse.Length, rawBrowseResponse.Stream).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:494:                DisposeRawBrowseResponseStream(rawBrowseResponse);
src/Messaging/Handlers/PeerMessageHandler.cs:498:        private static async Task WriteRawSearchResponseAsync(IMessageConnection connection, RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:501:            await connection.WriteAsync(rawSearchResponse.Length, rawSearchResponse.Stream).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:539:                placeInQueue = await SoulseekClient.Options.PlaceInQueueResolver(connection.Username, connection.IPEndPoint, filename).ConfigureAwait(false);
src/Messaging/Compression/InfTree.cs:307:		internal static int inflate_trees_bits(int[] c, int[] bb, int[] tb, int[] hp, ZStream z)
src/Messaging/Compression/InfTree.cs:327:		internal static int inflate_trees_dynamic(int nl, int nd, int[] c, int[] bl, int[] bd, int[] tl, int[] td, int[] hp, ZStream z)
src/Messaging/Compression/InfTree.cs:374:		internal static int inflate_trees_fixed(int[] bl, int[] bd, int[][] tl, int[][] td, ZStream z)
src/Messaging/Compression/ZStreamException.cs:55:    internal class ZStreamException:System.IO.IOException
src/Messaging/Compression/ZStreamException.cs:57:		public ZStreamException():base()
src/Messaging/Compression/ZStreamException.cs:60:		public ZStreamException(System.String s):base(s)
src/Messaging/Compression/ZStreamException.cs:63:        public ZStreamException(System.String s, Exception e):base(s, e)
src/Messaging/Compression/InfBlocks.cs:110:		internal InfBlocks(ZStream z, System.Object checkfn, int w)
src/Messaging/Compression/InfBlocks.cs:120:		internal void  reset(ZStream z, long[] c)
src/Messaging/Compression/InfBlocks.cs:141:		internal int proc(ZStream z, int r)
src/Messaging/Compression/InfBlocks.cs:637:		internal void  free(ZStream z)
src/Messaging/Compression/InfBlocks.cs:659:		internal int inflate_flush(ZStream z, int r)
src/Messaging/Compression/ZStream.cs:54:    internal sealed class ZStream
src/Messaging/Compression/Deflate.cs:164:		internal ZStream strm; // pointer back to this zlib stream
src/Messaging/Compression/Deflate.cs:1437:		internal int deflateInit(ZStream strm, int level, int bits)
src/Messaging/Compression/Deflate.cs:1441:		internal int deflateInit(ZStream strm, int level)
src/Messaging/Compression/Deflate.cs:1445:		internal int deflateInit2(ZStream strm, int level, int method, int windowBits, int memLevel, int strategy)
src/Messaging/Compression/Deflate.cs:1509:		internal int deflateReset(ZStream strm)
src/Messaging/Compression/Deflate.cs:1548:		internal int deflateParams(ZStream strm, int _level, int _strategy)
src/Messaging/Compression/Deflate.cs:1579:		internal int deflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
src/Messaging/Compression/Deflate.cs:1616:		internal int deflate(ZStream strm, int flush)
src/Messaging/Compression/SupportClass.cs:103:		/// <summary>Reads a number of characters from the current source Stream and writes the data to the target array at the specified index.</summary>
src/Messaging/Compression/SupportClass.cs:104:		/// <param name="sourceStream">The source Stream to read from.</param>
src/Messaging/Compression/SupportClass.cs:105:		/// <param name="target">Contains the array of characteres read from the source Stream.</param>
src/Messaging/Compression/SupportClass.cs:107:		/// <param name="count">The maximum number of characters to read from the source Stream.</param>
src/Messaging/Compression/SupportClass.cs:108:		/// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source Stream. Returns -1 if the end of the stream is reached.</returns>
src/Messaging/Compression/SupportClass.cs:109:		public static System.Int32 ReadInput(System.IO.Stream sourceStream, byte[] target, int start, int count)
src/Messaging/Compression/SupportClass.cs:116:			int bytesRead   = sourceStream.Read(receiver, start, count);
src/Messaging/Compression/ZOutputStream.cs:54:    internal class ZOutputStream:System.IO.Stream
src/Messaging/Compression/ZOutputStream.cs:93:		protected internal ZStream z = new ZStream();
src/Messaging/Compression/ZOutputStream.cs:104:		private System.IO.Stream out_Renamed;
src/Messaging/Compression/ZOutputStream.cs:106:		public ZOutputStream(System.IO.Stream out_Renamed):base()
src/Messaging/Compression/ZOutputStream.cs:114:		public ZOutputStream(System.IO.Stream out_Renamed, int level):base()
src/Messaging/Compression/ZOutputStream.cs:153:					throw new ZStreamException((compress?"de":"in") + "flating: " + z.msg);
src/Messaging/Compression/ZOutputStream.cs:176:					throw new ZStreamException((compress?"de":"in") + "flating: " + z.msg);
src/Messaging/Compression/ZInputStream.cs:53:    internal class ZInputStream:System.IO.BinaryReader
src/Messaging/Compression/ZInputStream.cs:92:		protected ZStream z = new ZStream();
src/Messaging/Compression/ZInputStream.cs:98:		internal System.IO.Stream in_Renamed = null;
src/Messaging/Compression/ZInputStream.cs:100:		public ZInputStream(System.IO.Stream in_Renamed):base(in_Renamed)
src/Messaging/Compression/ZInputStream.cs:111:		public ZInputStream(System.IO.Stream in_Renamed, int level):base(in_Renamed)
src/Messaging/Compression/ZInputStream.cs:163:					throw new ZStreamException((compress?"de":"in") + "flating: " + z.msg);
src/Messaging/Compression/ZInputStream.cs:178:			return ((long) SupportClass.ReadInput(BaseStream, tmp, 0, tmp.Length));
src/Messaging/Compression/InfCodes.cs:105:		internal InfCodes(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, ZStream z)
src/Messaging/Compression/InfCodes.cs:116:		internal InfCodes(int bl, int bd, int[] tl, int[] td, ZStream z)
src/Messaging/Compression/InfCodes.cs:127:		internal int proc(InfBlocks s, ZStream z, int r)
src/Messaging/Compression/InfCodes.cs:478:		internal void  free(ZStream z)
src/Messaging/Compression/InfCodes.cs:488:		internal int inflate_fast(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, InfBlocks s, ZStream z)
src/Messaging/Compression/Inflate.cs:112:		internal int inflateReset(ZStream z)
src/Messaging/Compression/Inflate.cs:124:		internal int inflateEnd(ZStream z)
src/Messaging/Compression/Inflate.cs:133:		internal int inflateInit(ZStream z, int w)
src/Messaging/Compression/Inflate.cs:161:		internal int inflate(ZStream z, int f)
src/Messaging/Compression/Inflate.cs:360:		internal int inflateSetDictionary(ZStream z, byte[] dictionary, int dictLength)
src/Messaging/Compression/Inflate.cs:386:		internal int inflateSync(ZStream z)
src/Messaging/Compression/Inflate.cs:448:		internal int inflateSyncPoint(ZStream z)

## Resolver delegate surface candidates
src/Messaging/Handlers/PeerMessageHandler.cs:151:                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:161:                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:174:                        if (SoulseekClient.Options.SearchResponseResolver == default)
src/Messaging/Handlers/PeerMessageHandler.cs:181:                            var peerSearchResponse = await SoulseekClient.Options.SearchResponseResolver(connection.Username, searchRequest.Token, SearchQuery.FromText(searchRequest.Query)).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:211:                            browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:216:                                .BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:243:                            outgoingFolderContents = await SoulseekClient.Options.DirectoryContentsResolver(
src/Messaging/Handlers/PeerMessageHandler.cs:290:                            await TryEnqueueDownloadAsync(connection.Username, connection.IPEndPoint, queueDownloadRequest.Filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:321:                            var (transferRejected, transferRejectionMessage) = await TryEnqueueDownloadAsync(connection.Username, connection.IPEndPoint, transferRequest.Filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:504:        private async Task<(bool Rejected, string RejectionMessage)> TryEnqueueDownloadAsync(string username, IPEndPoint ipEndPoint, string filename)
src/Messaging/Handlers/PeerMessageHandler.cs:512:                    .EnqueueDownload(username, ipEndPoint, filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:516:                // pass the exception message through to the remote user only if EnqueueDownloadException is thrown
src/Messaging/Handlers/PeerMessageHandler.cs:539:                placeInQueue = await SoulseekClient.Options.PlaceInQueueResolver(connection.Username, connection.IPEndPoint, filename).ConfigureAwait(false);
src/SoulseekClient.cs:1405:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1492:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:4601:                    searchResponseResolver: patch.SearchResponseResolver,
src/SoulseekClient.cs:4603:                    browseResponseResolver: patch.BrowseResponseResolver,
src/SoulseekClient.cs:4604:                    directoryContentsResolver: patch.DirectoryContentsResolver,
src/SoulseekClient.cs:4605:                    userInfoResolver: patch.UserInfoResolver,
src/SoulseekClient.cs:4606:                    enqueueDownload: patch.EnqueueDownload,
src/SoulseekClient.cs:4607:                    placeInQueueResolver: patch.PlaceInQueueResolver);
src/Options/SoulseekClientOptionsPatch.cs:169:            SearchResponseResolver = searchResponseResolver;
src/Options/SoulseekClientOptionsPatch.cs:172:            BrowseResponseResolver = browseResponseResolver;
src/Options/SoulseekClientOptionsPatch.cs:173:            DirectoryContentsResolver = directoryContentsResolver;
src/Options/SoulseekClientOptionsPatch.cs:175:            UserInfoResolver = userInfoResolver;
src/Options/SoulseekClientOptionsPatch.cs:176:            EnqueueDownload = enqueueDownload;
src/Options/SoulseekClientOptionsPatch.cs:177:            PlaceInQueueResolver = placeInQueueResolver;
src/Options/SoulseekClientOptionsPatch.cs:203:        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:213:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:242:        public Func<string, IPEndPoint, string, Task> EnqueueDownload { get; }
src/Options/SoulseekClientOptionsPatch.cs:282:        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:292:        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }
src/Options/SoulseekClientOptionsPatch.cs:312:        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResolver { get; }
src/Options/SoulseekClientOptions.cs:44:        private readonly Func<string, IPEndPoint, Task<BrowseResponse>> defaultBrowseResponseResolver =
src/Options/SoulseekClientOptions.cs:47:        private readonly Func<string, IPEndPoint, string, Task> defaultEnqueueDownload =
src/Options/SoulseekClientOptions.cs:50:        private readonly Func<string, IPEndPoint, string, Task<int?>> defaultPlaceInQueueResolver =
src/Options/SoulseekClientOptions.cs:53:        private readonly Func<string, IPEndPoint, Task<UserInfo>> defaultUserInfoResolver =
src/Options/SoulseekClientOptions.cs:234:            SearchResponseResolver = searchResponseResolver;
src/Options/SoulseekClientOptions.cs:237:            BrowseResponseResolver = browseResponseResolver ?? defaultBrowseResponseResolver;
src/Options/SoulseekClientOptions.cs:238:            DirectoryContentsResolver = directoryContentsResolver;
src/Options/SoulseekClientOptions.cs:240:            UserInfoResolver = userInfoResolver ?? defaultUserInfoResolver;
src/Options/SoulseekClientOptions.cs:241:            EnqueueDownload = enqueueDownload ?? defaultEnqueueDownload;
src/Options/SoulseekClientOptions.cs:242:            PlaceInQueueResolver = placeInQueueResolver ?? defaultPlaceInQueueResolver;
src/Options/SoulseekClientOptions.cs:272:        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }
src/Options/SoulseekClientOptions.cs:283:        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }
src/Options/SoulseekClientOptions.cs:312:        public Func<string, IPEndPoint, string, Task> EnqueueDownload { get; }
src/Options/SoulseekClientOptions.cs:386:        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResolver { get; }
src/Options/SoulseekClientOptions.cs:396:        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }
src/Options/SoulseekClientOptions.cs:421:        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResolver { get; }
src/Options/SoulseekClientOptions.cs:461:                searchResponseResolver: patch.SearchResponseResolver,
src/Options/SoulseekClientOptions.cs:463:                browseResponseResolver: patch.BrowseResponseResolver,
src/Options/SoulseekClientOptions.cs:464:                directoryContentsResolver: patch.DirectoryContentsResolver,
src/Options/SoulseekClientOptions.cs:465:                userInfoResolver: patch.UserInfoResolver,
src/Options/SoulseekClientOptions.cs:466:                enqueueDownload: patch.EnqueueDownload,
src/Options/SoulseekClientOptions.cs:467:                placeInQueueResolver: patch.PlaceInQueueResolver);
src/Options/SoulseekClientOptions.cs:569:                searchResponseResolver: searchResponseResolver ?? SearchResponseResolver,
src/Options/SoulseekClientOptions.cs:571:                browseResponseResolver: browseResponseResolver ?? BrowseResponseResolver,
src/Options/SoulseekClientOptions.cs:572:                directoryContentsResolver: directoryContentsResolver ?? DirectoryContentsResolver,
src/Options/SoulseekClientOptions.cs:573:                userInfoResolver: userInfoResolver ?? UserInfoResolver,
src/Options/SoulseekClientOptions.cs:574:                enqueueDownload: enqueueDownload ?? EnqueueDownload,
src/Options/SoulseekClientOptions.cs:575:                placeInQueueResolver: placeInQueueResolver ?? PlaceInQueueResolver);

## Peer resolver dispatch candidates
src/Messaging/Handlers/PeerMessageHandler.cs:174:                        if (SoulseekClient.Options.SearchResponseResolver == default)
src/Messaging/Handlers/PeerMessageHandler.cs:181:                            var peerSearchResponse = await SoulseekClient.Options.SearchResponseResolver(connection.Username, searchRequest.Token, SearchQuery.FromText(searchRequest.Query)).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:183:                            if (peerSearchResponse is RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:187:                                    await WriteRawSearchResponseAsync(connection, rawSearchResponse).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:191:                                    DisposeRawSearchResponseStream(rawSearchResponse);
src/Messaging/Handlers/PeerMessageHandler.cs:211:                            browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:221:                        if (browseResponse is RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:223:                            await WriteRawBrowseResponseAsync(connection, rawBrowseResponse).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:243:                            outgoingFolderContents = await SoulseekClient.Options.DirectoryContentsResolver(
src/Messaging/Handlers/PeerMessageHandler.cs:298:                            await TrySendPlaceInQueueAsync(connection, queueDownloadRequest.Filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:331:                                await TrySendPlaceInQueueAsync(connection, transferRequest.Filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:353:                        await TrySendPlaceInQueueAsync(connection, placeInQueueRequest.Filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:462:        private static void DisposeRawBrowseResponseStream(RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:474:        private static void DisposeRawSearchResponseStream(RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:486:        private static async Task WriteRawBrowseResponseAsync(IMessageConnection connection, RawBrowseResponse rawBrowseResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:494:                DisposeRawBrowseResponseStream(rawBrowseResponse);
src/Messaging/Handlers/PeerMessageHandler.cs:498:        private static async Task WriteRawSearchResponseAsync(IMessageConnection connection, RawSearchResponse rawSearchResponse)
src/Messaging/Handlers/PeerMessageHandler.cs:533:        private async Task TrySendPlaceInQueueAsync(IMessageConnection connection, string filename)
src/Messaging/Handlers/PeerMessageHandler.cs:539:                placeInQueue = await SoulseekClient.Options.PlaceInQueueResolver(connection.Username, connection.IPEndPoint, filename).ConfigureAwait(false);
src/Messaging/Handlers/PeerMessageHandler.cs:551:                    await connection.WriteAsync(new PlaceInQueueResponse(filename, placeInQueue.Value)).ConfigureAwait(false);

## Transfer stream factory candidates
src/Options/TransferOptions.cs:91:            SeekInputStreamAutomatically = seekInputStreamAutomatically;
src/Options/TransferOptions.cs:92:            SeekOutputStreamAutomatically = seekOutputStreamAutomatically;
src/Options/TransferOptions.cs:93:            DisposeInputStreamOnCompletion = disposeInputStreamOnCompletion;
src/Options/TransferOptions.cs:94:            DisposeOutputStreamOnCompletion = disposeOutputStreamOnCompletion;
src/Options/TransferOptions.cs:108:        public bool DisposeInputStreamOnCompletion { get; }
src/Options/TransferOptions.cs:113:        public bool DisposeOutputStreamOnCompletion { get; }
src/Options/TransferOptions.cs:142:        public bool SeekInputStreamAutomatically { get; }
src/Options/TransferOptions.cs:148:        public bool SeekOutputStreamAutomatically { get; }
src/Options/TransferOptions.cs:184:                seekInputStreamAutomatically: SeekInputStreamAutomatically,
src/Options/TransferOptions.cs:185:                seekOutputStreamAutomatically: SeekOutputStreamAutomatically,
src/Options/TransferOptions.cs:186:                disposeInputStreamOnCompletion: DisposeInputStreamOnCompletion,
src/Options/TransferOptions.cs:187:                disposeOutputStreamOnCompletion: DisposeOutputStreamOnCompletion);
src/Options/TransferOptions.cs:212:                seekInputStreamAutomatically: SeekInputStreamAutomatically,
src/Options/TransferOptions.cs:213:                seekOutputStreamAutomatically: SeekOutputStreamAutomatically,
src/Options/TransferOptions.cs:214:                disposeInputStreamOnCompletion: disposeInputStreamOnCompletion ?? DisposeInputStreamOnCompletion,
src/Options/TransferOptions.cs:215:                disposeOutputStreamOnCompletion: disposeOutputStreamOnCompletion ?? DisposeOutputStreamOnCompletion);
src/SoulseekClient.cs:1183:        ///     <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
src/SoulseekClient.cs:1191:        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
src/SoulseekClient.cs:1209:        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
src/SoulseekClient.cs:1225:        public Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1257:            if (outputStreamFactory == null)
src/SoulseekClient.cs:1259:                throw new ArgumentNullException(nameof(outputStreamFactory), "The specified output stream factory is null");
src/SoulseekClient.cs:1287:            return DownloadToStreamAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:1441:        ///         <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
src/SoulseekClient.cs:1461:        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
src/SoulseekClient.cs:1476:        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
src/SoulseekClient.cs:1492:        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1512:            var downloadTask = DownloadAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);
src/SoulseekClient.cs:1596:        ///         <see cref="Stream"/> created by the specified <paramref name="inputStreamFactory"/> to the the specified
src/SoulseekClient.cs:1608:        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
src/SoulseekClient.cs:1619:        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
src/SoulseekClient.cs:1632:        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:1652:            var uploadTask = UploadAsync(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);
src/SoulseekClient.cs:3115:        ///     the specified <paramref name="inputStreamFactory"/> to the the specified <paramref name="username"/> using the
src/SoulseekClient.cs:3121:        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
src/SoulseekClient.cs:3132:        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
src/SoulseekClient.cs:3145:        public Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
src/SoulseekClient.cs:3162:            if (inputStreamFactory == null)
src/SoulseekClient.cs:3164:                throw new ArgumentNullException(nameof(inputStreamFactory), "The specified input stream factory is null");
src/SoulseekClient.cs:3192:            return UploadFromStreamAsync(username, remoteFilename, size, inputStreamFactory, token.Value, options, cancellationToken ?? CancellationToken.None);
src/SoulseekClient.cs:3639:        private async Task<Transfer> DownloadToStreamAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:3844:                outputStream = await outputStreamFactory().ConfigureAwait(false);
src/SoulseekClient.cs:3853:                    anyone that sets SeekOutputStreamAutomatically to false and passes a stream positioned at anything
src/SoulseekClient.cs:3857:                if (download.StartOffset > 0 && options.SeekOutputStreamAutomatically)
src/SoulseekClient.cs:4025:                    if (options.DisposeOutputStreamOnCompletion && outputStream != null)
src/SoulseekClient.cs:5087:        private async Task<Transfer> UploadFromStreamAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int token, TransferOptions options, CancellationToken cancellationToken)
src/SoulseekClient.cs:5276:                inputStream = await inputStreamFactory(upload.StartOffset).ConfigureAwait(false);
src/SoulseekClient.cs:5278:                if (upload.StartOffset > 0 && options.SeekInputStreamAutomatically)
src/SoulseekClient.cs:5451:                    if (options.DisposeInputStreamOnCompletion && inputStream != null)

## Example Web API path, request, and lifecycle candidates
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:50:            var cancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:63:            var cancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:76:            var oldCancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:77:            var newCancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:80:            tracker.AddOrUpdate(transfer, oldCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:81:            tracker.AddOrUpdate(transfer, newCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:83:            Assert.Throws<ObjectDisposedException>(() => _ = oldCancellationTokenSource.Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:85:            Assert.Same(newCancellationTokenSource, record.CancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:87:            newCancellationTokenSource.Dispose();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:91:        public async Task Transfer_Enqueue_Rejects_Null_Request()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:93:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:97:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:103:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:105:            var response = await controller.Enqueue("user", new QueueDownloadRequest { Filename = "file.mp3", Size = -1 });
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:107:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:113:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:115:            Assert.IsType<BadRequestObjectResult>(controller.CancelDownload(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:116:            Assert.IsType<BadRequestObjectResult>(controller.CancelDownload("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:117:            Assert.IsType<BadRequestObjectResult>(controller.CancelUpload(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:118:            Assert.IsType<BadRequestObjectResult>(controller.CancelUpload("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:119:            Assert.IsType<BadRequestObjectResult>(await controller.Enqueue(" ", new QueueDownloadRequest { Filename = "file.mp3", Size = 1 }));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:120:            Assert.IsType<BadRequestObjectResult>(controller.GetDownloads(" "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:121:            Assert.IsType<BadRequestObjectResult>(await controller.GetPlaceInQueue(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:122:            Assert.IsType<BadRequestObjectResult>(await controller.GetPlaceInQueue("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:123:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads(" "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:124:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:125:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:131:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:139:        public async Task Transfer_Enqueue_Defers_Output_File_Creation_Until_Stream_Factory_Is_Invoked()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:145:                Func<Task<Stream>> capturedStreamFactory = null;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:153:                        It.IsAny<Func<Task<Stream>>>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:159:                    .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((callbackUsername, callbackFilename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:161:                        capturedStreamFactory = streamFactory;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:169:                var response = await controller.Enqueue("user", new QueueDownloadRequest { Filename = remoteFilename, Size = 1 });
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:170:                var localFilename = Path.Combine(root, "album", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:174:                Assert.NotNull(capturedStreamFactory);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:175:                Assert.False(File.Exists(localFilename));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:177:                var stream = await capturedStreamFactory();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:180:                Assert.True(File.Exists(localFilename));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:184:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:197:                    It.IsAny<Func<Task<Stream>>>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:203:                .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((username, filename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:209:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), client.Object, new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:211:            var response = await controller.Enqueue("user", new QueueDownloadRequest { Filename = "file.mp3", Size = 1 });
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:215:            Assert.Throws<ObjectDisposedException>(() => _ = GetCancellationTokenSource(capturedCancellationToken).Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:227:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:228:            Directory.CreateDirectory(path);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:232:        private static CancellationTokenSource GetCancellationTokenSource(CancellationToken token)
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:234:            var source = typeof(CancellationToken).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(token) as CancellationTokenSource;
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:1:// <copyright file="WebApiPathSecurityTests.cs" company="slskdN Team">
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:20:    public class WebApiPathSecurityTests
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:23:        public void WebApi_Path_Guard_Accepts_Paths_Inside_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:29:                var file = Path.Combine(root, "music", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:30:                var resolved = Extensions.GetFullPathInsideRoot(root, file);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:32:                Assert.Equal(Path.GetFullPath(file), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:36:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:41:        public void WebApi_Path_Guard_Rejects_Sibling_Prefix_Escapes()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:47:                var root = Path.Combine(parent, "share");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:48:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:50:                Directory.CreateDirectory(root);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:51:                Directory.CreateDirectory(Path.GetDirectoryName(sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:53:                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetFullPathInsideRoot(root, sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:57:                Directory.Delete(parent, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:62:        public void WebApi_Output_Path_Keeps_Absolute_Remote_Names_Under_Output_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:68:                var resolved = Extensions.GetSafeOutputPath(root, Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "passwd"));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:70:                Assert.StartsWith(Path.GetFullPath(root), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:71:                Assert.EndsWith(Path.Combine("etc", "passwd"), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:75:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:80:        public void WebApi_Shared_Remote_Path_Is_Relative_To_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:86:                var file = Path.Combine(root, "music", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:87:                var resolved = Extensions.GetSharedRemotePath(root, file);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:89:                Assert.Equal(Path.Combine("music", "track.mp3"), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:90:                Assert.DoesNotContain(Path.GetFullPath(root), resolved, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:94:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:99:        public void WebApi_Shared_Remote_Path_Rejects_Paths_Outside_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:105:                var root = Path.Combine(parent, "share");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:106:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:108:                Directory.CreateDirectory(root);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:109:                Directory.CreateDirectory(Path.GetDirectoryName(sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:111:                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetSharedRemotePath(root, sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:115:                Directory.Delete(parent, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:126:                var album = Path.Combine(root, "album");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:127:                Directory.CreateDirectory(album);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:128:                File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:134:                Assert.Equal(Path.Combine("album", "track.mp3"), file.Filename);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:135:                Assert.DoesNotContain(Path.GetFullPath(root), file.Filename, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:139:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:150:                File.WriteAllText(Path.Combine(root, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:163:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:169:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:170:            Directory.CreateDirectory(path);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:1:// <copyright file="WebApiRequestTests.cs" company="slskdN Team">
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:37:    public class WebApiRequestTests
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:40:        public async Task Search_Endpoint_Rejects_Null_Request()
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:46:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:58:            var response = await controller.Post(new SearchRequest { SearchText = searchText });
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:60:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:68:            var response = await controller.PostUsers(new SearchRequest { SearchText = "music" }, " ");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:70:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:74:        public async Task Connect_Endpoint_Rejects_Null_Request()
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:80:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:90:            var response = await controller.Connect(new ConnectRequest
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:98:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:118:            var response = await controller.Post(new SearchRequest
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:129:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:144:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:159:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:174:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:187:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:197:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:214:            var originalDirectory = Directory.GetCurrentDirectory();
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:215:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:216:            Directory.CreateDirectory(temp);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:220:                Directory.SetCurrentDirectory(temp);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:232:                Directory.SetCurrentDirectory(originalDirectory);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:233:                Directory.Delete(temp, recursive: true);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:240:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:241:            var album = Path.Combine(temp, "album");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:242:            Directory.CreateDirectory(album);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:243:            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:255:                Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:259:                Directory.Delete(temp, recursive: true);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:266:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:267:            var album = Path.Combine(temp, "album");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:268:            var disc = Path.Combine(album, "disc1");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:269:            Directory.CreateDirectory(disc);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:270:            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:281:                Assert.Contains(response, directory => directory.Name == Path.Combine("album", "disc1"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:282:                Assert.All(response, directory => Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:286:                Directory.Delete(temp, recursive: true);
examples/Web/api/WebAPI.csproj:16:    <OutputPath></OutputPath>
examples/Web/api/Trackers/TransferTracker.cs:19:        public static ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> WithDirection(
examples/Web/api/Trackers/TransferTracker.cs:20:            this ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> allTransfers,
examples/Web/api/Trackers/TransferTracker.cs:24:            return transfers ?? new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>();
examples/Web/api/Trackers/TransferTracker.cs:33:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers)
examples/Web/api/Trackers/TransferTracker.cs:50:        public static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> FromUser(
examples/Web/api/Trackers/TransferTracker.cs:51:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers,
examples/Web/api/Trackers/TransferTracker.cs:55:            return transfers ?? new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:63:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers)
examples/Web/api/Trackers/TransferTracker.cs:76:        public static (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) WithId(
examples/Web/api/Trackers/TransferTracker.cs:77:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers,
examples/Web/api/Trackers/TransferTracker.cs:93:        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; private set; } =
examples/Web/api/Trackers/TransferTracker.cs:94:            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer, CancellationTokenSource)>>>();
examples/Web/api/Trackers/TransferTracker.cs:101:            Transfers.TryAdd(TransferDirection.Download, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:102:            Transfers.TryAdd(TransferDirection.Upload, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:110:        public void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:119:                    if (!ReferenceEquals(record.CancellationTokenSource, cancellationTokenSource))
examples/Web/api/Trackers/TransferTracker.cs:121:                        record.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:148:                        transfer.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:161:                    removedTransfer.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:179:        public bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer)
examples/Web/api/Trackers/TransferTracker.cs:197:        private static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> GetNewDictionaryForUser(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:199:            var r = new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:201:            r.AddOrUpdate(tx.Id, (tx, cancellationTokenSource), (id, record) => (tx, record.CancellationTokenSource));
examples/Web/api/Trackers/ITransferTracker.cs:15:        ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; }
examples/Web/api/Trackers/ITransferTracker.cs:22:        void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource);
examples/Web/api/Trackers/ITransferTracker.cs:38:        bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer);
examples/Web/api/Startup.cs:37:        internal static string BasePath { get; set; }
examples/Web/api/Startup.cs:69:            BasePath = Configuration.GetValue<string>("BASE_PATH");
examples/Web/api/Startup.cs:169:                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml"));
examples/Web/api/Startup.cs:196:            BasePath ??= "/";
examples/Web/api/Startup.cs:197:            BasePath = BasePath.StartsWith("/") ? BasePath : $"/{BasePath}";
examples/Web/api/Startup.cs:199:            app.UsePathBase(BasePath);
examples/Web/api/Startup.cs:205:                var path = context.Request.Path.ToString();
examples/Web/api/Startup.cs:209:                    context.Request.Path = new string(path.Skip(1).ToArray());
examples/Web/api/Startup.cs:215:            WebRoot ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wwwroot");
examples/Web/api/Startup.cs:221:                RequestPath = "",
examples/Web/api/Startup.cs:240:                if (!context.Request.Path.StartsWithSegments("/api"))
examples/Web/api/Startup.cs:242:                    context.Request.Path = "/";
examples/Web/api/Startup.cs:320:                var file = Path.GetFileName(args.Transfer.Filename);
examples/Web/api/Startup.cs:337:                //Console.WriteLine($"[{args.Transfer.Direction.ToString().ToUpper()}] [{args.Transfer.Username}/{Path.GetFileName(args.Transfer.Filename)}] {args.Transfer.BytesTransferred}/{args.Transfer.Size} {args.Transfer.PercentComplete}% {args.Transfer.AverageSpeed}kb/s");
examples/Web/api/Startup.cs:421:            Client.SearchRequestReceived += (e, args) =>
examples/Web/api/Startup.cs:465:            const string PicturePath = "slsk_bird.jpg";
examples/Web/api/Startup.cs:467:            if (System.IO.File.Exists(PicturePath))
examples/Web/api/Startup.cs:469:                picture = System.IO.File.ReadAllBytes(PicturePath);
examples/Web/api/Startup.cs:487:        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>
examples/Web/api/Startup.cs:493:                    Extensions.GetSharedRemotePath(SharedDirectory, dir),
examples/Web/api/Startup.cs:494:                    System.IO.Directory.GetFiles(dir)
examples/Web/api/Startup.cs:495:                        .Select(f => new Soulseek.File(1, Path.GetFileName(f), new FileInfo(f).Length, Path.GetExtension(f)))));
examples/Web/api/Startup.cs:511:                name: Extensions.GetSharedRemotePath(root, dir),
examples/Web/api/Startup.cs:512:                fileList: System.IO.Directory.GetFiles(dir)
examples/Web/api/Startup.cs:513:                    .Select(f => new Soulseek.File(1, Path.GetFileName(f), new FileInfo(f).Length, Path.GetExtension(f))));
examples/Web/api/Startup.cs:515:            directory = Extensions.GetFullPathInsideRoot(SharedDirectory, directory);
examples/Web/api/Startup.cs:522:            foreach (var subDirectory in System.IO.Directory.GetDirectories(directory))
examples/Web/api/Startup.cs:563:                        Console.WriteLine($"[QUEUE] Only one upload waiting, selecting {selected.Key} with {Path.GetFileName(selected.Value.Filename)}");
examples/Web/api/Startup.cs:572:                            Console.WriteLine($"\t[QUEUE] Candidate: {kvp.Key} with {Path.GetFileName(kvp.Value.Filename)}; Ready at: {kvp.Value.ReadyTimestamp}");
examples/Web/api/Startup.cs:575:                        Console.WriteLine($"[QUEUE] Selected {selected.Key} with {Path.GetFileName(selected.Value.Filename)} as the earliest ready");
examples/Web/api/Startup.cs:584:                            Console.WriteLine($"\t[QUEUE] Candidate: {kvp.Key} with {Path.GetFileName(kvp.Value.Filename)}; Enqueued at: {kvp.Value.EnqueuedTimestamp}");
examples/Web/api/Startup.cs:587:                        Console.WriteLine($"[QUEUE] Selected {selected.Key} with {Path.GetFileName(selected.Value.Filename)} as the earliest enqueued");
examples/Web/api/Startup.cs:616:            var localFilename = Extensions.GetFullPathInsideRoot(SharedDirectory, filename);
examples/Web/api/Startup.cs:635:            var cts = new CancellationTokenSource();
examples/Web/api/Startup.cs:671:                    using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
examples/Web/api/Startup.cs:672:                    await Client.UploadAsync(username, filename, fileInfo.Length, (_) => Task.FromResult((Stream)stream), options: topts, cancellationToken: cts.Token);
examples/Web/api/SharedFileCache.cs:53:                var directoryCount = System.IO.Directory.GetDirectories(Directory, "*", SearchOption.AllDirectories).Length;
examples/Web/api/SharedFileCache.cs:55:                Files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
examples/Web/api/SharedFileCache.cs:56:                    .Select(f => new Soulseek.File(1, Extensions.GetSharedRemotePath(Directory, f), new FileInfo(f).Length, Path.GetExtension(f)))
examples/Web/api/Extensions.cs:19:        public static string ToLocalOSPath(this string path)
examples/Web/api/Extensions.cs:21:            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:24:        public static string GetFullPathInsideRoot(string root, string path)
examples/Web/api/Extensions.cs:33:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:36:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:37:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:38:            var fullPath = Path.IsPathRooted(localPath)
examples/Web/api/Extensions.cs:39:                ? Path.GetFullPath(localPath)
examples/Web/api/Extensions.cs:40:                : Path.GetFullPath(Path.Combine(rootPath, localPath));
examples/Web/api/Extensions.cs:42:            if (!IsPathInsideRoot(rootPath, fullPath))
examples/Web/api/Extensions.cs:44:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured root");
examples/Web/api/Extensions.cs:47:            return fullPath;
examples/Web/api/Extensions.cs:50:        public static string GetSafeOutputPath(string root, string path)
examples/Web/api/Extensions.cs:54:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:57:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:58:            var relativePath = ToSafeRelativePath(path);
examples/Web/api/Extensions.cs:59:            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
examples/Web/api/Extensions.cs:61:            if (!IsPathInsideRoot(rootPath, fullPath))
examples/Web/api/Extensions.cs:63:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured output directory");
examples/Web/api/Extensions.cs:66:            return fullPath;
examples/Web/api/Extensions.cs:69:        public static string GetSharedRemotePath(string root, string path)
examples/Web/api/Extensions.cs:71:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:72:            var fullPath = GetFullPathInsideRoot(rootPath, path);
examples/Web/api/Extensions.cs:73:            var relativePath = Path.GetRelativePath(rootPath, fullPath);
examples/Web/api/Extensions.cs:75:            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
examples/Web/api/Extensions.cs:77:                throw new ArgumentException("Path does not contain a usable shared name", nameof(path));
examples/Web/api/Extensions.cs:80:            return relativePath.ToLocalOSPath().TrimStart(Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:95:        private static bool IsPathInsideRoot(string normalizedRoot, string fullPath)
examples/Web/api/Extensions.cs:97:            var comparison = Path.DirectorySeparatorChar == '\\'
examples/Web/api/Extensions.cs:101:            var normalizedPath = Path.GetFullPath(fullPath);
examples/Web/api/Extensions.cs:102:            var rootWithoutSeparator = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:104:            return string.Equals(normalizedPath, rootWithoutSeparator, comparison) ||
examples/Web/api/Extensions.cs:105:                normalizedPath.StartsWith(normalizedRoot, comparison);
examples/Web/api/Extensions.cs:108:        private static string NormalizeRootPath(string root)
examples/Web/api/Extensions.cs:115:            var fullPath = Path.GetFullPath(root.ToLocalOSPath());
examples/Web/api/Extensions.cs:116:            return fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
examples/Web/api/Extensions.cs:119:        private static string ToSafeRelativePath(string path)
examples/Web/api/Extensions.cs:121:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:122:            var parts = localPath
examples/Web/api/Extensions.cs:123:                .Split(Path.DirectorySeparatorChar)
examples/Web/api/Extensions.cs:125:                .Select(SanitizePathPart)
examples/Web/api/Extensions.cs:131:                throw new ArgumentException("Path does not contain a usable file name", nameof(path));
examples/Web/api/Extensions.cs:134:            return Path.Combine(parts);
examples/Web/api/Extensions.cs:137:        private static string SanitizePathPart(string part)
examples/Web/api/Extensions.cs:139:            foreach (var c in Path.GetInvalidFileNameChars())
examples/Web/api/DTO/SearchRequest.cs:9:    public class SearchRequest
examples/Web/api/DTO/QueueDownloadRequest.cs:3:    public class QueueDownloadRequest
examples/Web/api/DTO/LoginRequest.cs:3:    public class LoginRequest
examples/Web/api/DTO/ConnectRequest.cs:3:    public class ConnectRequest
examples/Web/api/Controllers/UserController.cs:46:        public async Task<IActionResult> Address([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:50:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:56:                return Ok(new UserAddress() { IPAddress = endpoint.Address.ToString(), Port = endpoint.Port });
examples/Web/api/Controllers/UserController.cs:73:        public async Task<IActionResult> Browse([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:77:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:90:                return Ok(result);
examples/Web/api/Controllers/UserController.cs:108:        public async Task<IActionResult> FolderContents([FromRoute, Required] string username, [FromRoute, Required] string folderName)
examples/Web/api/Controllers/UserController.cs:112:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:117:                return BadRequest("Folder name is required");
examples/Web/api/Controllers/UserController.cs:123:                return Ok(result);
examples/Web/api/Controllers/UserController.cs:145:        public IActionResult BrowseStatus([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:149:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:154:                return Ok(progress);
examples/Web/api/Controllers/UserController.cs:169:        public async Task<IActionResult> Info([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:173:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:179:                return Ok(response);
examples/Web/api/Controllers/UserController.cs:196:        public async Task<IActionResult> Status([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:200:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:206:                return Ok(response);
examples/Web/api/Controllers/UserController.cs:223:        public async Task<IActionResult> Statistics([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:227:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:233:                return Ok(response);
examples/Web/api/Controllers/TransfersController.cs:57:        public IActionResult CancelDownload([FromRoute, Required] string username, [FromRoute, Required] string id, [FromQuery] bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:61:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:66:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:85:        public IActionResult CancelUpload([FromRoute, Required] string username, [FromRoute, Required] string id, [FromQuery] bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:89:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:94:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:116:        public async Task<IActionResult> Enqueue([FromRoute, Required] string username, [FromBody] QueueDownloadRequest request)
examples/Web/api/Controllers/TransfersController.cs:120:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:123:            CancellationTokenSource cts = null;
examples/Web/api/Controllers/TransfersController.cs:130:                    return BadRequest("Request body is required");
examples/Web/api/Controllers/TransfersController.cs:135:                    return BadRequest("Filename is required");
examples/Web/api/Controllers/TransfersController.cs:140:                    return BadRequest("Size must be greater than or equal to zero");
examples/Web/api/Controllers/TransfersController.cs:145:                cts = new CancellationTokenSource();
examples/Web/api/Controllers/TransfersController.cs:147:                var downloadTask = Client.DownloadAsync(username, request.Filename, () => Task.FromResult((Stream)GetLocalFileStream(request.Filename, OutputDirectory)), request.Size, 0, request.Token, new TransferOptions(disposeOutputStreamOnCompletion: true, stateChanged: (e) =>
examples/Web/api/Controllers/TransfersController.cs:168:                    DisposeUntrackedCancellationTokenSource(cts, isTracked);
examples/Web/api/Controllers/TransfersController.cs:185:                DisposeUntrackedCancellationTokenSource(cts, isTracked);
examples/Web/api/Controllers/TransfersController.cs:199:        public IActionResult GetDownloads()
examples/Web/api/Controllers/TransfersController.cs:201:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:215:        public IActionResult GetDownloads([FromRoute, Required] string username)
examples/Web/api/Controllers/TransfersController.cs:219:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:222:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:241:        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required] string username, [FromRoute, Required] string id)
examples/Web/api/Controllers/TransfersController.cs:245:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:250:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:261:            return Ok(record.Transfer);
examples/Web/api/Controllers/TransfersController.cs:272:        public IActionResult GetUploads()
examples/Web/api/Controllers/TransfersController.cs:274:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:288:        public IActionResult GetUploads([FromRoute, Required] string username)
examples/Web/api/Controllers/TransfersController.cs:292:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:295:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:312:        public IActionResult GetUploads([FromRoute, Required] string username, [FromRoute, Required] string id)
examples/Web/api/Controllers/TransfersController.cs:316:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:321:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:334:            return Ok(record.Transfer);
examples/Web/api/Controllers/TransfersController.cs:337:        [SuppressMessage("Security", "CA3003:Review code for file path injection vulnerabilities", Justification = "The remote file name is normalized to a relative path and confined to the configured output directory by Extensions.GetSafeOutputPath.")]
examples/Web/api/Controllers/TransfersController.cs:338:        private static FileStream GetLocalFileStream(string remoteFilename, string saveDirectory)
examples/Web/api/Controllers/TransfersController.cs:340:            var localFilename = Extensions.GetSafeOutputPath(saveDirectory, remoteFilename);
examples/Web/api/Controllers/TransfersController.cs:341:            var path = Path.GetDirectoryName(localFilename);
examples/Web/api/Controllers/TransfersController.cs:343:            if (!System.IO.Directory.Exists(path))
examples/Web/api/Controllers/TransfersController.cs:345:                System.IO.Directory.CreateDirectory(path);
examples/Web/api/Controllers/TransfersController.cs:348:            return new FileStream(localFilename, FileMode.Create);
examples/Web/api/Controllers/TransfersController.cs:351:        private static void DisposeUntrackedCancellationTokenSource(CancellationTokenSource cts, int isTracked)
examples/Web/api/Controllers/TransfersController.cs:359:        private IActionResult CancelTransfer(TransferDirection direction, string username, string id, bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:363:                transfer.CancellationTokenSource.Cancel();
examples/Web/api/Controllers/SessionController.cs:35:        public IActionResult Enabled()
examples/Web/api/Controllers/SessionController.cs:37:            return Ok(Startup.EnableSecurity);
examples/Web/api/Controllers/SessionController.cs:54:        public IActionResult Check()
examples/Web/api/Controllers/SessionController.cs:56:            return Ok();
examples/Web/api/Controllers/SessionController.cs:74:        public IActionResult Login([FromBody]LoginRequest login)
examples/Web/api/Controllers/SessionController.cs:78:                return BadRequest();
examples/Web/api/Controllers/SessionController.cs:83:                return BadRequest("Username and/or Password missing or invalid");
examples/Web/api/Controllers/SessionController.cs:88:                return Ok(new TokenResponse(GetJwtSecurityToken()));
examples/Web/api/Controllers/ServerController.cs:34:        public IActionResult Disconnect([FromBody]string message)
examples/Web/api/Controllers/ServerController.cs:47:        public async Task<IActionResult> Connect([FromBody]ConnectRequest req)
examples/Web/api/Controllers/ServerController.cs:51:                return BadRequest("Request body is required");
examples/Web/api/Controllers/ServerController.cs:63:                    return BadRequest($"Port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
examples/Web/api/Controllers/ServerController.cs:67:                return Ok();
examples/Web/api/Controllers/ServerController.cs:73:                return Ok();
examples/Web/api/Controllers/ServerController.cs:76:            return BadRequest("Provide one of the following: address and port, username and password, or address, port, username and password");
examples/Web/api/Controllers/SearchesController.cs:51:        public async Task<IActionResult> Post([FromBody] SearchRequest request)
examples/Web/api/Controllers/SearchesController.cs:53:            if (!TryNormalizeSearchRequest(request, out var searchText, out var badRequest))
examples/Web/api/Controllers/SearchesController.cs:55:                return badRequest;
examples/Web/api/Controllers/SearchesController.cs:69:                return Ok(results);
examples/Web/api/Controllers/SearchesController.cs:96:        public async Task<IActionResult> PostUsers([FromBody] SearchRequest request, [FromRoute] string username)
examples/Web/api/Controllers/SearchesController.cs:100:                return BadRequest("Username is required");
examples/Web/api/Controllers/SearchesController.cs:103:            if (!TryNormalizeSearchRequest(request, out var searchText, out var badRequest))
examples/Web/api/Controllers/SearchesController.cs:105:                return badRequest;
examples/Web/api/Controllers/SearchesController.cs:119:                return Ok(results);
examples/Web/api/Controllers/SearchesController.cs:143:        public IActionResult GetById([FromRoute] Guid id)
examples/Web/api/Controllers/SearchesController.cs:152:            return Ok(search);
examples/Web/api/Controllers/SearchesController.cs:155:        private bool TryNormalizeSearchRequest(SearchRequest request, out string searchText, out IActionResult badRequest)
examples/Web/api/Controllers/SearchesController.cs:158:            badRequest = null;
examples/Web/api/Controllers/SearchesController.cs:162:                badRequest = BadRequest("Request body is required");
examples/Web/api/Controllers/SearchesController.cs:168:                badRequest = BadRequest("Search text is required");
examples/Web/api/Controllers/SearchesController.cs:176:                badRequest = BadRequest("Search text must contain at least one term longer than one character");
examples/Web/api/Controllers/SearchesController.cs:182:                badRequest = BadRequest("Search timeout must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:188:                badRequest = BadRequest("Response limit must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:194:                badRequest = BadRequest("File limit must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:200:                badRequest = BadRequest("Minimum response file count must be greater than or equal to zero");
examples/Web/api/Controllers/SearchesController.cs:206:                badRequest = BadRequest("Maximum peer queue length must be greater than or equal to zero");
examples/Web/api/Controllers/SearchesController.cs:212:                badRequest = BadRequest("Minimum peer upload speed must be greater than or equal to zero");
examples/Web/api/Controllers/RoomsController.cs:42:        public IActionResult GetAll()
examples/Web/api/Controllers/RoomsController.cs:44:            return Ok(Tracker.Rooms.Keys);
examples/Web/api/Controllers/RoomsController.cs:58:        public IActionResult GetByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:62:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:67:                return Ok(MapRoomToRoomResponse(room));
examples/Web/api/Controllers/RoomsController.cs:86:        public async Task<IActionResult> SendMessage([FromRoute]string roomName, [FromBody]string message)
examples/Web/api/Controllers/RoomsController.cs:90:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:95:                return BadRequest("Message is required");
examples/Web/api/Controllers/RoomsController.cs:120:        public async Task<IActionResult> SetTicker([FromRoute] string roomName, [FromBody] string message)
examples/Web/api/Controllers/RoomsController.cs:124:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:129:                return BadRequest("Message is required");
examples/Web/api/Controllers/RoomsController.cs:154:        public async Task<IActionResult> AddRoomMember([FromRoute]string roomName, [FromBody]string username)
examples/Web/api/Controllers/RoomsController.cs:158:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:163:                return BadRequest("Username is required");
examples/Web/api/Controllers/RoomsController.cs:186:        public IActionResult GetUsersByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:190:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:198:                return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:215:        public IActionResult GetMessagesByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:219:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:227:                return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:240:        public async Task<IActionResult> GetRooms()
examples/Web/api/Controllers/RoomsController.cs:255:            return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:269:        public async Task<IActionResult> JoinRoom([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:273:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:311:        public async Task<IActionResult> LeaveRoom([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:315:                return BadRequest("Room name is required");
examples/Web/api/Controllers/PublicChatController.cs:32:        public async Task<IActionResult> Start()
examples/Web/api/Controllers/PublicChatController.cs:44:        public async Task<IActionResult> Stop()
examples/Web/api/Controllers/ConversationsController.cs:52:        public async Task<IActionResult> Acknowledge([FromRoute]string username, [FromRoute]int id)
examples/Web/api/Controllers/ConversationsController.cs:56:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:81:        public async Task<IActionResult> AcknowledgeAll([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:85:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:120:        public IActionResult Delete([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:124:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:145:        public IActionResult GetAll()
examples/Web/api/Controllers/ConversationsController.cs:153:            return Ok(response);
examples/Web/api/Controllers/ConversationsController.cs:167:        public IActionResult GetByUsername([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:171:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:180:                return Ok(response);
examples/Web/api/Controllers/ConversationsController.cs:198:        public async Task<IActionResult> Send([FromRoute]string username, [FromBody]string message)
examples/Web/api/Controllers/ConversationsController.cs:202:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:207:                return BadRequest("Message is required");

## Example Web API path and shared-file candidates
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:93:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:103:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:113:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:131:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:170:                var localFilename = Path.Combine(root, "album", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:175:                Assert.False(File.Exists(localFilename));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:180:                Assert.True(File.Exists(localFilename));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:184:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:209:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), client.Object, new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:227:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:228:            Directory.CreateDirectory(path);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:214:            var originalDirectory = Directory.GetCurrentDirectory();
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:215:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:216:            Directory.CreateDirectory(temp);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:220:                Directory.SetCurrentDirectory(temp);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:232:                Directory.SetCurrentDirectory(originalDirectory);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:233:                Directory.Delete(temp, recursive: true);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:240:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:241:            var album = Path.Combine(temp, "album");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:242:            Directory.CreateDirectory(album);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:243:            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:248:                var method = typeof(Startup).GetMethod("BrowseResponseResolver", BindingFlags.Instance | BindingFlags.NonPublic);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:255:                Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:259:                Directory.Delete(temp, recursive: true);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:266:            var temp = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:267:            var album = Path.Combine(temp, "album");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:268:            var disc = Path.Combine(album, "disc1");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:269:            Directory.CreateDirectory(disc);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:270:            File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:275:                var method = typeof(Startup).GetMethod("DirectoryContentsResponseResolver", BindingFlags.Instance | BindingFlags.NonPublic);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:281:                Assert.Contains(response, directory => directory.Name == Path.Combine("album", "disc1"));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:282:                Assert.All(response, directory => Assert.DoesNotContain(Path.GetFullPath(temp), directory.Name, StringComparison.Ordinal));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:286:                Directory.Delete(temp, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:1:// <copyright file="WebApiPathSecurityTests.cs" company="slskdN Team">
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:20:    public class WebApiPathSecurityTests
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:23:        public void WebApi_Path_Guard_Accepts_Paths_Inside_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:29:                var file = Path.Combine(root, "music", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:30:                var resolved = Extensions.GetFullPathInsideRoot(root, file);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:32:                Assert.Equal(Path.GetFullPath(file), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:36:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:41:        public void WebApi_Path_Guard_Rejects_Sibling_Prefix_Escapes()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:47:                var root = Path.Combine(parent, "share");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:48:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:50:                Directory.CreateDirectory(root);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:51:                Directory.CreateDirectory(Path.GetDirectoryName(sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:53:                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetFullPathInsideRoot(root, sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:57:                Directory.Delete(parent, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:62:        public void WebApi_Output_Path_Keeps_Absolute_Remote_Names_Under_Output_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:68:                var resolved = Extensions.GetSafeOutputPath(root, Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "passwd"));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:70:                Assert.StartsWith(Path.GetFullPath(root), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:71:                Assert.EndsWith(Path.Combine("etc", "passwd"), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:75:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:80:        public void WebApi_Shared_Remote_Path_Is_Relative_To_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:86:                var file = Path.Combine(root, "music", "track.mp3");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:87:                var resolved = Extensions.GetSharedRemotePath(root, file);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:89:                Assert.Equal(Path.Combine("music", "track.mp3"), resolved);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:90:                Assert.DoesNotContain(Path.GetFullPath(root), resolved, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:94:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:99:        public void WebApi_Shared_Remote_Path_Rejects_Paths_Outside_Root()
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:105:                var root = Path.Combine(parent, "share");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:106:                var sibling = Path.Combine(parent, "share-other", "secret.txt");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:108:                Directory.CreateDirectory(root);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:109:                Directory.CreateDirectory(Path.GetDirectoryName(sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:111:                Assert.Throws<UnauthorizedAccessException>(() => Extensions.GetSharedRemotePath(root, sibling));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:115:                Directory.Delete(parent, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:126:                var album = Path.Combine(root, "album");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:127:                Directory.CreateDirectory(album);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:128:                File.WriteAllText(Path.Combine(album, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:130:                var cache = new SharedFileCache(root, ttl: 3600000);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:134:                Assert.Equal(Path.Combine("album", "track.mp3"), file.Filename);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:135:                Assert.DoesNotContain(Path.GetFullPath(root), file.Filename, StringComparison.Ordinal);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:139:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:150:                File.WriteAllText(Path.Combine(root, "track.mp3"), "test");
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:152:                var cache = new SharedFileCache(root, ttl: 3600000);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:163:                Directory.Delete(root, recursive: true);
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:169:            var path = Path.Combine(Path.GetTempPath(), "slsknet-runtime-tests", Guid.NewGuid().ToString("N"));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:170:            Directory.CreateDirectory(path);
examples/Web/api/WebAPI.csproj:16:    <OutputPath></OutputPath>
examples/Web/api/Startup.cs:37:        internal static string BasePath { get; set; }
examples/Web/api/Startup.cs:58:        private ISharedFileCache SharedFileCache { get; set; }
examples/Web/api/Startup.cs:69:            BasePath = Configuration.GetValue<string>("BASE_PATH");
examples/Web/api/Startup.cs:90:            SharedFileCache = new SharedFileCache(SharedDirectory, SharedCacheTTL);
examples/Web/api/Startup.cs:169:                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml"));
examples/Web/api/Startup.cs:196:            BasePath ??= "/";
examples/Web/api/Startup.cs:197:            BasePath = BasePath.StartsWith("/") ? BasePath : $"/{BasePath}";
examples/Web/api/Startup.cs:199:            app.UsePathBase(BasePath);
examples/Web/api/Startup.cs:205:                var path = context.Request.Path.ToString();
examples/Web/api/Startup.cs:209:                    context.Request.Path = new string(path.Skip(1).ToArray());
examples/Web/api/Startup.cs:215:            WebRoot ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wwwroot");
examples/Web/api/Startup.cs:221:                RequestPath = "",
examples/Web/api/Startup.cs:240:                if (!context.Request.Path.StartsWithSegments("/api"))
examples/Web/api/Startup.cs:242:                    context.Request.Path = "/";
examples/Web/api/Startup.cs:277:                browseResponseResolver: BrowseResponseResolver,
examples/Web/api/Startup.cs:278:                directoryContentsResolver: DirectoryContentsResponseResolver,
examples/Web/api/Startup.cs:280:                searchResponseResolver: SearchResponseResolver,
examples/Web/api/Startup.cs:287:            SharedFileCache.Refreshed += (e, args) =>
examples/Web/api/Startup.cs:320:                var file = Path.GetFileName(args.Transfer.Filename);
examples/Web/api/Startup.cs:337:                //Console.WriteLine($"[{args.Transfer.Direction.ToString().ToUpper()}] [{args.Transfer.Username}/{Path.GetFileName(args.Transfer.Filename)}] {args.Transfer.BytesTransferred}/{args.Transfer.Size} {args.Transfer.PercentComplete}% {args.Transfer.AverageSpeed}kb/s");
examples/Web/api/Startup.cs:465:            const string PicturePath = "slsk_bird.jpg";
examples/Web/api/Startup.cs:467:            if (System.IO.File.Exists(PicturePath))
examples/Web/api/Startup.cs:469:                picture = System.IO.File.ReadAllBytes(PicturePath);
examples/Web/api/Startup.cs:487:        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>
examples/Web/api/Startup.cs:488:        private Task<BrowseResponse> BrowseResponseResolver(string username, IPEndPoint endpoint)
examples/Web/api/Startup.cs:493:                    Extensions.GetSharedRemotePath(SharedDirectory, dir),
examples/Web/api/Startup.cs:494:                    System.IO.Directory.GetFiles(dir)
examples/Web/api/Startup.cs:495:                        .Select(f => new Soulseek.File(1, Path.GetFileName(f), new FileInfo(f).Length, Path.GetExtension(f)))));
examples/Web/api/Startup.cs:508:        private Task<IEnumerable<Soulseek.Directory>> DirectoryContentsResponseResolver(string username, IPEndPoint endpoint, int token, string directory)
examples/Web/api/Startup.cs:511:                name: Extensions.GetSharedRemotePath(root, dir),
examples/Web/api/Startup.cs:512:                fileList: System.IO.Directory.GetFiles(dir)
examples/Web/api/Startup.cs:513:                    .Select(f => new Soulseek.File(1, Path.GetFileName(f), new FileInfo(f).Length, Path.GetExtension(f))));
examples/Web/api/Startup.cs:515:            directory = Extensions.GetFullPathInsideRoot(SharedDirectory, directory);
examples/Web/api/Startup.cs:522:            foreach (var subDirectory in System.IO.Directory.GetDirectories(directory))
examples/Web/api/Startup.cs:563:                        Console.WriteLine($"[QUEUE] Only one upload waiting, selecting {selected.Key} with {Path.GetFileName(selected.Value.Filename)}");
examples/Web/api/Startup.cs:572:                            Console.WriteLine($"\t[QUEUE] Candidate: {kvp.Key} with {Path.GetFileName(kvp.Value.Filename)}; Ready at: {kvp.Value.ReadyTimestamp}");
examples/Web/api/Startup.cs:575:                        Console.WriteLine($"[QUEUE] Selected {selected.Key} with {Path.GetFileName(selected.Value.Filename)} as the earliest ready");
examples/Web/api/Startup.cs:584:                            Console.WriteLine($"\t[QUEUE] Candidate: {kvp.Key} with {Path.GetFileName(kvp.Value.Filename)}; Enqueued at: {kvp.Value.EnqueuedTimestamp}");
examples/Web/api/Startup.cs:587:                        Console.WriteLine($"[QUEUE] Selected {selected.Key} with {Path.GetFileName(selected.Value.Filename)} as the earliest enqueued");
examples/Web/api/Startup.cs:616:            var localFilename = Extensions.GetFullPathInsideRoot(SharedDirectory, filename);
examples/Web/api/Startup.cs:694:        private Task<SearchResponse> SearchResponseResolver(string username, int token, SearchQuery query)
examples/Web/api/Startup.cs:713:                var results = SharedFileCache.Search(query);
examples/Web/api/SharedFileCache.cs:15:    public class SharedFileCache : ISharedFileCache
examples/Web/api/SharedFileCache.cs:18:        ///     Initializes a new instance of the <see cref="SharedFileCache"/> class.
examples/Web/api/SharedFileCache.cs:22:        public SharedFileCache(string directory, long ttl)
examples/Web/api/SharedFileCache.cs:53:                var directoryCount = System.IO.Directory.GetDirectories(Directory, "*", SearchOption.AllDirectories).Length;
examples/Web/api/SharedFileCache.cs:55:                Files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
examples/Web/api/SharedFileCache.cs:56:                    .Select(f => new Soulseek.File(1, Extensions.GetSharedRemotePath(Directory, f), new FileInfo(f).Length, Path.GetExtension(f)))
examples/Web/api/ISharedFileCache.cs:7:    internal interface ISharedFileCache
examples/Web/api/Extensions.cs:19:        public static string ToLocalOSPath(this string path)
examples/Web/api/Extensions.cs:21:            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:24:        public static string GetFullPathInsideRoot(string root, string path)
examples/Web/api/Extensions.cs:33:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:36:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:37:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:38:            var fullPath = Path.IsPathRooted(localPath)
examples/Web/api/Extensions.cs:39:                ? Path.GetFullPath(localPath)
examples/Web/api/Extensions.cs:40:                : Path.GetFullPath(Path.Combine(rootPath, localPath));
examples/Web/api/Extensions.cs:42:            if (!IsPathInsideRoot(rootPath, fullPath))
examples/Web/api/Extensions.cs:44:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured root");
examples/Web/api/Extensions.cs:47:            return fullPath;
examples/Web/api/Extensions.cs:50:        public static string GetSafeOutputPath(string root, string path)
examples/Web/api/Extensions.cs:54:                throw new ArgumentException("Path is missing or invalid", nameof(path));
examples/Web/api/Extensions.cs:57:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:58:            var relativePath = ToSafeRelativePath(path);
examples/Web/api/Extensions.cs:59:            var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
examples/Web/api/Extensions.cs:61:            if (!IsPathInsideRoot(rootPath, fullPath))
examples/Web/api/Extensions.cs:63:                throw new UnauthorizedAccessException($"Path '{path}' is outside the configured output directory");
examples/Web/api/Extensions.cs:66:            return fullPath;
examples/Web/api/Extensions.cs:69:        public static string GetSharedRemotePath(string root, string path)
examples/Web/api/Extensions.cs:71:            var rootPath = NormalizeRootPath(root);
examples/Web/api/Extensions.cs:72:            var fullPath = GetFullPathInsideRoot(rootPath, path);
examples/Web/api/Extensions.cs:73:            var relativePath = Path.GetRelativePath(rootPath, fullPath);
examples/Web/api/Extensions.cs:75:            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
examples/Web/api/Extensions.cs:77:                throw new ArgumentException("Path does not contain a usable shared name", nameof(path));
examples/Web/api/Extensions.cs:80:            return relativePath.ToLocalOSPath().TrimStart(Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:95:        private static bool IsPathInsideRoot(string normalizedRoot, string fullPath)
examples/Web/api/Extensions.cs:97:            var comparison = Path.DirectorySeparatorChar == '\\'
examples/Web/api/Extensions.cs:101:            var normalizedPath = Path.GetFullPath(fullPath);
examples/Web/api/Extensions.cs:102:            var rootWithoutSeparator = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar);
examples/Web/api/Extensions.cs:104:            return string.Equals(normalizedPath, rootWithoutSeparator, comparison) ||
examples/Web/api/Extensions.cs:105:                normalizedPath.StartsWith(normalizedRoot, comparison);
examples/Web/api/Extensions.cs:108:        private static string NormalizeRootPath(string root)
examples/Web/api/Extensions.cs:115:            var fullPath = Path.GetFullPath(root.ToLocalOSPath());
examples/Web/api/Extensions.cs:116:            return fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
examples/Web/api/Extensions.cs:119:        private static string ToSafeRelativePath(string path)
examples/Web/api/Extensions.cs:121:            var localPath = path.ToLocalOSPath();
examples/Web/api/Extensions.cs:122:            var parts = localPath
examples/Web/api/Extensions.cs:123:                .Split(Path.DirectorySeparatorChar)
examples/Web/api/Extensions.cs:125:                .Select(SanitizePathPart)
examples/Web/api/Extensions.cs:131:                throw new ArgumentException("Path does not contain a usable file name", nameof(path));
examples/Web/api/Extensions.cs:134:            return Path.Combine(parts);
examples/Web/api/Extensions.cs:137:        private static string SanitizePathPart(string part)
examples/Web/api/Extensions.cs:139:            foreach (var c in Path.GetInvalidFileNameChars())
examples/Web/api/Controllers/TransfersController.cs:337:        [SuppressMessage("Security", "CA3003:Review code for file path injection vulnerabilities", Justification = "The remote file name is normalized to a relative path and confined to the configured output directory by Extensions.GetSafeOutputPath.")]
examples/Web/api/Controllers/TransfersController.cs:340:            var localFilename = Extensions.GetSafeOutputPath(saveDirectory, remoteFilename);
examples/Web/api/Controllers/TransfersController.cs:341:            var path = Path.GetDirectoryName(localFilename);
examples/Web/api/Controllers/TransfersController.cs:343:            if (!System.IO.Directory.Exists(path))
examples/Web/api/Controllers/TransfersController.cs:345:                System.IO.Directory.CreateDirectory(path);

## Example Web API controller request-validation candidates
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:97:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:107:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:115:            Assert.IsType<BadRequestObjectResult>(controller.CancelDownload(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:116:            Assert.IsType<BadRequestObjectResult>(controller.CancelDownload("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:117:            Assert.IsType<BadRequestObjectResult>(controller.CancelUpload(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:118:            Assert.IsType<BadRequestObjectResult>(controller.CancelUpload("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:119:            Assert.IsType<BadRequestObjectResult>(await controller.Enqueue(" ", new QueueDownloadRequest { Filename = "file.mp3", Size = 1 }));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:120:            Assert.IsType<BadRequestObjectResult>(controller.GetDownloads(" "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:121:            Assert.IsType<BadRequestObjectResult>(await controller.GetPlaceInQueue(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:122:            Assert.IsType<BadRequestObjectResult>(await controller.GetPlaceInQueue("user", " "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:123:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads(" "));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:124:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads(" ", "id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:125:            Assert.IsType<BadRequestObjectResult>(controller.GetUploads("user", " "));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:46:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:60:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:70:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:80:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:98:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:129:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:144:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:159:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:174:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:187:            Assert.IsType<BadRequestObjectResult>(response);
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:197:            Assert.IsType<BadRequestObjectResult>(response);
examples/Web/api/Controllers/ConversationsController.cs:50:        [ProducesResponseType(200)]
examples/Web/api/Controllers/ConversationsController.cs:51:        [ProducesResponseType(404)]
examples/Web/api/Controllers/ConversationsController.cs:52:        public async Task<IActionResult> Acknowledge([FromRoute]string username, [FromRoute]int id)
examples/Web/api/Controllers/ConversationsController.cs:56:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:63:                return NotFound();
examples/Web/api/Controllers/ConversationsController.cs:67:            return StatusCode(200);
examples/Web/api/Controllers/ConversationsController.cs:79:        [ProducesResponseType(200)]
examples/Web/api/Controllers/ConversationsController.cs:80:        [ProducesResponseType(404)]
examples/Web/api/Controllers/ConversationsController.cs:81:        public async Task<IActionResult> AcknowledgeAll([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:85:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:92:                return NotFound();
examples/Web/api/Controllers/ConversationsController.cs:107:            return StatusCode(200);
examples/Web/api/Controllers/ConversationsController.cs:118:        [ProducesResponseType(404)]
examples/Web/api/Controllers/ConversationsController.cs:119:        [ProducesResponseType(204)]
examples/Web/api/Controllers/ConversationsController.cs:120:        public IActionResult Delete([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:124:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:131:                return StatusCode(204);
examples/Web/api/Controllers/ConversationsController.cs:134:            return StatusCode(404);
examples/Web/api/Controllers/ConversationsController.cs:144:        [ProducesResponseType(typeof(Dictionary<string, List<PrivateMessageResponse>>), 200)]
examples/Web/api/Controllers/ConversationsController.cs:145:        public IActionResult GetAll()
examples/Web/api/Controllers/ConversationsController.cs:153:            return Ok(response);
examples/Web/api/Controllers/ConversationsController.cs:165:        [ProducesResponseType(typeof(List<PrivateMessageResponse>), 200)]
examples/Web/api/Controllers/ConversationsController.cs:166:        [ProducesResponseType(404)]
examples/Web/api/Controllers/ConversationsController.cs:167:        public IActionResult GetByUsername([FromRoute]string username)
examples/Web/api/Controllers/ConversationsController.cs:171:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:180:                return Ok(response);
examples/Web/api/Controllers/ConversationsController.cs:183:            return NotFound();
examples/Web/api/Controllers/ConversationsController.cs:196:        [ProducesResponseType(201)]
examples/Web/api/Controllers/ConversationsController.cs:197:        [ProducesResponseType(400)]
examples/Web/api/Controllers/ConversationsController.cs:198:        public async Task<IActionResult> Send([FromRoute]string username, [FromBody]string message)
examples/Web/api/Controllers/ConversationsController.cs:202:                return BadRequest("Username is required");
examples/Web/api/Controllers/ConversationsController.cs:207:                return BadRequest("Message is required");
examples/Web/api/Controllers/ConversationsController.cs:220:            return StatusCode(201);
examples/Web/api/Controllers/UserController.cs:44:        [ProducesResponseType(typeof(UserAddress), 200)]
examples/Web/api/Controllers/UserController.cs:45:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:46:        public async Task<IActionResult> Address([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:50:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:56:                return Ok(new UserAddress() { IPAddress = endpoint.Address.ToString(), Port = endpoint.Port });
examples/Web/api/Controllers/UserController.cs:60:                return NotFound(ex.Message);
examples/Web/api/Controllers/UserController.cs:71:        [ProducesResponseType(typeof(IEnumerable<Directory>), 200)]
examples/Web/api/Controllers/UserController.cs:72:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:73:        public async Task<IActionResult> Browse([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:77:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:90:                return Ok(result);
examples/Web/api/Controllers/UserController.cs:94:                return NotFound(ex.Message);
examples/Web/api/Controllers/UserController.cs:106:        [ProducesResponseType(typeof(IEnumerable<Directory>), 200)]
examples/Web/api/Controllers/UserController.cs:107:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:108:        public async Task<IActionResult> FolderContents([FromRoute, Required] string username, [FromRoute, Required] string folderName)
examples/Web/api/Controllers/UserController.cs:112:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:117:                return BadRequest("Folder name is required");
examples/Web/api/Controllers/UserController.cs:123:                return Ok(result);
examples/Web/api/Controllers/UserController.cs:127:                return NotFound(ex.Message);
examples/Web/api/Controllers/UserController.cs:143:        [ProducesResponseType(typeof(decimal), 200)]
examples/Web/api/Controllers/UserController.cs:144:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:145:        public IActionResult BrowseStatus([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:149:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:154:                return Ok(progress);
examples/Web/api/Controllers/UserController.cs:157:            return NotFound();
examples/Web/api/Controllers/UserController.cs:167:        [ProducesResponseType(typeof(UserInfo), 200)]
examples/Web/api/Controllers/UserController.cs:168:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:169:        public async Task<IActionResult> Info([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:173:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:179:                return Ok(response);
examples/Web/api/Controllers/UserController.cs:183:                return NotFound(ex.Message);
examples/Web/api/Controllers/UserController.cs:194:        [ProducesResponseType(typeof(UserStatus), 200)]
examples/Web/api/Controllers/UserController.cs:195:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:196:        public async Task<IActionResult> Status([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:200:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:206:                return Ok(response);
examples/Web/api/Controllers/UserController.cs:210:                return NotFound(ex.Message);
examples/Web/api/Controllers/UserController.cs:221:        [ProducesResponseType(typeof(UserStatus), 200)]
examples/Web/api/Controllers/UserController.cs:222:        [ProducesResponseType(404)]
examples/Web/api/Controllers/UserController.cs:223:        public async Task<IActionResult> Statistics([FromRoute, Required] string username)
examples/Web/api/Controllers/UserController.cs:227:                return BadRequest("Username is required");
examples/Web/api/Controllers/UserController.cs:233:                return Ok(response);
examples/Web/api/Controllers/UserController.cs:237:                return NotFound(ex.Message);
examples/Web/api/Controllers/SessionController.cs:34:        [ProducesResponseType(typeof(bool), 200)]
examples/Web/api/Controllers/SessionController.cs:35:        public IActionResult Enabled()
examples/Web/api/Controllers/SessionController.cs:37:            return Ok(Startup.EnableSecurity);
examples/Web/api/Controllers/SessionController.cs:52:        [ProducesResponseType(200)]
examples/Web/api/Controllers/SessionController.cs:53:        [ProducesResponseType(401)]
examples/Web/api/Controllers/SessionController.cs:54:        public IActionResult Check()
examples/Web/api/Controllers/SessionController.cs:56:            return Ok();
examples/Web/api/Controllers/SessionController.cs:70:        [ProducesResponseType(typeof(TokenResponse), 200)]
examples/Web/api/Controllers/SessionController.cs:71:        [ProducesResponseType(400)]
examples/Web/api/Controllers/SessionController.cs:72:        [ProducesResponseType(401)]
examples/Web/api/Controllers/SessionController.cs:73:        [ProducesResponseType(typeof(string), 500)]
examples/Web/api/Controllers/SessionController.cs:74:        public IActionResult Login([FromBody]LoginRequest login)
examples/Web/api/Controllers/SessionController.cs:78:                return BadRequest();
examples/Web/api/Controllers/SessionController.cs:83:                return BadRequest("Username and/or Password missing or invalid");
examples/Web/api/Controllers/SessionController.cs:88:                return Ok(new TokenResponse(GetJwtSecurityToken()));
examples/Web/api/Controllers/ServerController.cs:34:        public IActionResult Disconnect([FromBody]string message)
examples/Web/api/Controllers/ServerController.cs:47:        public async Task<IActionResult> Connect([FromBody]ConnectRequest req)
examples/Web/api/Controllers/ServerController.cs:51:                return BadRequest("Request body is required");
examples/Web/api/Controllers/ServerController.cs:63:                    return BadRequest($"Port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}");
examples/Web/api/Controllers/ServerController.cs:67:                return Ok();
examples/Web/api/Controllers/ServerController.cs:73:                return Ok();
examples/Web/api/Controllers/ServerController.cs:76:            return BadRequest("Provide one of the following: address and port, username and password, or address, port, username and password");
examples/Web/api/Controllers/PublicChatController.cs:32:        public async Task<IActionResult> Start()
examples/Web/api/Controllers/PublicChatController.cs:35:            return StatusCode(StatusCodes.Status201Created);
examples/Web/api/Controllers/PublicChatController.cs:44:        public async Task<IActionResult> Stop()
examples/Web/api/Controllers/PublicChatController.cs:47:            return StatusCode(StatusCodes.Status204NoContent);
examples/Web/api/Controllers/RoomsController.cs:41:        [ProducesResponseType(typeof(Dictionary<string, Dictionary<string, Room>>), 200)]
examples/Web/api/Controllers/RoomsController.cs:42:        public IActionResult GetAll()
examples/Web/api/Controllers/RoomsController.cs:44:            return Ok(Tracker.Rooms.Keys);
examples/Web/api/Controllers/RoomsController.cs:56:        [ProducesResponseType(typeof(Room), 200)]
examples/Web/api/Controllers/RoomsController.cs:57:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:58:        public IActionResult GetByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:62:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:67:                return Ok(MapRoomToRoomResponse(room));
examples/Web/api/Controllers/RoomsController.cs:70:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:83:        [ProducesResponseType(201)]
examples/Web/api/Controllers/RoomsController.cs:84:        [ProducesResponseType(400)]
examples/Web/api/Controllers/RoomsController.cs:85:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:86:        public async Task<IActionResult> SendMessage([FromRoute]string roomName, [FromBody]string message)
examples/Web/api/Controllers/RoomsController.cs:90:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:95:                return BadRequest("Message is required");
examples/Web/api/Controllers/RoomsController.cs:101:                return StatusCode(StatusCodes.Status201Created);
examples/Web/api/Controllers/RoomsController.cs:104:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:117:        [ProducesResponseType(201)]
examples/Web/api/Controllers/RoomsController.cs:118:        [ProducesResponseType(400)]
examples/Web/api/Controllers/RoomsController.cs:119:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:120:        public async Task<IActionResult> SetTicker([FromRoute] string roomName, [FromBody] string message)
examples/Web/api/Controllers/RoomsController.cs:124:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:129:                return BadRequest("Message is required");
examples/Web/api/Controllers/RoomsController.cs:135:                return StatusCode(StatusCodes.Status201Created);
examples/Web/api/Controllers/RoomsController.cs:138:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:151:        [ProducesResponseType(201)]
examples/Web/api/Controllers/RoomsController.cs:152:        [ProducesResponseType(400)]
examples/Web/api/Controllers/RoomsController.cs:153:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:154:        public async Task<IActionResult> AddRoomMember([FromRoute]string roomName, [FromBody]string username)
examples/Web/api/Controllers/RoomsController.cs:158:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:163:                return BadRequest("Username is required");
examples/Web/api/Controllers/RoomsController.cs:169:                return StatusCode(StatusCodes.Status201Created);
examples/Web/api/Controllers/RoomsController.cs:172:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:184:        [ProducesResponseType(typeof(IList<UserData>), 200)]
examples/Web/api/Controllers/RoomsController.cs:185:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:186:        public IActionResult GetUsersByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:190:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:198:                return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:201:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:213:        [ProducesResponseType(typeof(IList<RoomMessage>), 200)]
examples/Web/api/Controllers/RoomsController.cs:214:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:215:        public IActionResult GetMessagesByRoomName([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:219:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:227:                return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:230:            return NotFound();
examples/Web/api/Controllers/RoomsController.cs:239:        [ProducesResponseType(typeof(List<RoomInfo>), 200)]
examples/Web/api/Controllers/RoomsController.cs:240:        public async Task<IActionResult> GetRooms()
examples/Web/api/Controllers/RoomsController.cs:255:            return Ok(response);
examples/Web/api/Controllers/RoomsController.cs:267:        [ProducesResponseType(typeof(Room), 201)]
examples/Web/api/Controllers/RoomsController.cs:268:        [ProducesResponseType(304)]
examples/Web/api/Controllers/RoomsController.cs:269:        public async Task<IActionResult> JoinRoom([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:273:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:278:                return StatusCode(StatusCodes.Status304NotModified);
examples/Web/api/Controllers/RoomsController.cs:287:                return StatusCode(StatusCodes.Status201Created, MapRoomToRoomResponse(room));
examples/Web/api/Controllers/RoomsController.cs:293:                    return StatusCode(StatusCodes.Status403Forbidden, $"The server rejected your request to join {roomName}");
examples/Web/api/Controllers/RoomsController.cs:309:        [ProducesResponseType(204)]
examples/Web/api/Controllers/RoomsController.cs:310:        [ProducesResponseType(404)]
examples/Web/api/Controllers/RoomsController.cs:311:        public async Task<IActionResult> LeaveRoom([FromRoute]string roomName)
examples/Web/api/Controllers/RoomsController.cs:315:                return BadRequest("Room name is required");
examples/Web/api/Controllers/RoomsController.cs:320:                return StatusCode(StatusCodes.Status404NotFound);
examples/Web/api/Controllers/RoomsController.cs:326:            return StatusCode(StatusCodes.Status204NoContent);
examples/Web/api/Controllers/TransfersController.cs:55:        [ProducesResponseType(204)]
examples/Web/api/Controllers/TransfersController.cs:56:        [ProducesResponseType(404)]
examples/Web/api/Controllers/TransfersController.cs:57:        public IActionResult CancelDownload([FromRoute, Required] string username, [FromRoute, Required] string id, [FromQuery] bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:61:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:66:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:83:        [ProducesResponseType(204)]
examples/Web/api/Controllers/TransfersController.cs:84:        [ProducesResponseType(404)]
examples/Web/api/Controllers/TransfersController.cs:85:        public IActionResult CancelUpload([FromRoute, Required] string username, [FromRoute, Required] string id, [FromQuery] bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:89:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:94:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:111:        [ProducesResponseType(201)]
examples/Web/api/Controllers/TransfersController.cs:112:        [ProducesResponseType(typeof(string), 400)]
examples/Web/api/Controllers/TransfersController.cs:113:        [ProducesResponseType(typeof(string), 403)]
examples/Web/api/Controllers/TransfersController.cs:114:        [ProducesResponseType(typeof(string), 500)]
examples/Web/api/Controllers/TransfersController.cs:116:        public async Task<IActionResult> Enqueue([FromRoute, Required] string username, [FromBody] QueueDownloadRequest request)
examples/Web/api/Controllers/TransfersController.cs:120:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:130:                    return BadRequest("Request body is required");
examples/Web/api/Controllers/TransfersController.cs:135:                    return BadRequest("Filename is required");
examples/Web/api/Controllers/TransfersController.cs:140:                    return BadRequest("Size must be greater than or equal to zero");
examples/Web/api/Controllers/TransfersController.cs:174:                        return StatusCode(403, rejected.First().Message);
examples/Web/api/Controllers/TransfersController.cs:177:                    return StatusCode(500, downloadTask.Exception.Message);
examples/Web/api/Controllers/TransfersController.cs:181:                return StatusCode(201);
examples/Web/api/Controllers/TransfersController.cs:187:                return StatusCode(500, ex.Message);
examples/Web/api/Controllers/TransfersController.cs:198:        [ProducesResponseType(200)]
examples/Web/api/Controllers/TransfersController.cs:199:        public IActionResult GetDownloads()
examples/Web/api/Controllers/TransfersController.cs:201:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:214:        [ProducesResponseType(200)]
examples/Web/api/Controllers/TransfersController.cs:215:        public IActionResult GetDownloads([FromRoute, Required] string username)
examples/Web/api/Controllers/TransfersController.cs:219:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:222:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:239:        [ProducesResponseType(typeof(DTO.Transfer), 200)]
examples/Web/api/Controllers/TransfersController.cs:240:        [ProducesResponseType(404)]
examples/Web/api/Controllers/TransfersController.cs:241:        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required] string username, [FromRoute, Required] string id)
examples/Web/api/Controllers/TransfersController.cs:245:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:250:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:257:                return NotFound();
examples/Web/api/Controllers/TransfersController.cs:261:            return Ok(record.Transfer);
examples/Web/api/Controllers/TransfersController.cs:271:        [ProducesResponseType(200)]
examples/Web/api/Controllers/TransfersController.cs:272:        public IActionResult GetUploads()
examples/Web/api/Controllers/TransfersController.cs:274:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:287:        [ProducesResponseType(200)]
examples/Web/api/Controllers/TransfersController.cs:288:        public IActionResult GetUploads([FromRoute, Required] string username)
examples/Web/api/Controllers/TransfersController.cs:292:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:295:            return Ok(Tracker.Transfers
examples/Web/api/Controllers/TransfersController.cs:310:        [ProducesResponseType(200)]
examples/Web/api/Controllers/TransfersController.cs:311:        [ProducesResponseType(404)]
examples/Web/api/Controllers/TransfersController.cs:312:        public IActionResult GetUploads([FromRoute, Required] string username, [FromRoute, Required] string id)
examples/Web/api/Controllers/TransfersController.cs:316:                return BadRequest("Username is required");
examples/Web/api/Controllers/TransfersController.cs:321:                return BadRequest("Transfer id is required");
examples/Web/api/Controllers/TransfersController.cs:331:                return NotFound();
examples/Web/api/Controllers/TransfersController.cs:334:            return Ok(record.Transfer);
examples/Web/api/Controllers/TransfersController.cs:359:        private IActionResult CancelTransfer(TransferDirection direction, string username, string id, bool remove = false)
examples/Web/api/Controllers/TransfersController.cs:373:            return NotFound();
examples/Web/api/Controllers/SearchesController.cs:48:        [ProducesResponseType(typeof(IEnumerable<SearchResponse>), 200)]
examples/Web/api/Controllers/SearchesController.cs:49:        [ProducesResponseType(400)]
examples/Web/api/Controllers/SearchesController.cs:50:        [ProducesResponseType(typeof(string), 500)]
examples/Web/api/Controllers/SearchesController.cs:51:        public async Task<IActionResult> Post([FromBody] SearchRequest request)
examples/Web/api/Controllers/SearchesController.cs:69:                return Ok(results);
examples/Web/api/Controllers/SearchesController.cs:73:                return StatusCode(500, $"Search terminated abnormally: {ex.Message}");
examples/Web/api/Controllers/SearchesController.cs:93:        [ProducesResponseType(typeof(IEnumerable<SearchResponse>), 200)]
examples/Web/api/Controllers/SearchesController.cs:94:        [ProducesResponseType(400)]
examples/Web/api/Controllers/SearchesController.cs:95:        [ProducesResponseType(typeof(string), 500)]
examples/Web/api/Controllers/SearchesController.cs:96:        public async Task<IActionResult> PostUsers([FromBody] SearchRequest request, [FromRoute] string username)
examples/Web/api/Controllers/SearchesController.cs:100:                return BadRequest("Username is required");
examples/Web/api/Controllers/SearchesController.cs:119:                return Ok(results);
examples/Web/api/Controllers/SearchesController.cs:123:                return StatusCode(500, $"Search terminated abnormally: {ex.Message}");
examples/Web/api/Controllers/SearchesController.cs:141:        [ProducesResponseType(typeof(Search), 200)]
examples/Web/api/Controllers/SearchesController.cs:142:        [ProducesResponseType(404)]
examples/Web/api/Controllers/SearchesController.cs:143:        public IActionResult GetById([FromRoute] Guid id)
examples/Web/api/Controllers/SearchesController.cs:149:                return NotFound();
examples/Web/api/Controllers/SearchesController.cs:152:            return Ok(search);
examples/Web/api/Controllers/SearchesController.cs:155:        private bool TryNormalizeSearchRequest(SearchRequest request, out string searchText, out IActionResult badRequest)
examples/Web/api/Controllers/SearchesController.cs:162:                badRequest = BadRequest("Request body is required");
examples/Web/api/Controllers/SearchesController.cs:168:                badRequest = BadRequest("Search text is required");
examples/Web/api/Controllers/SearchesController.cs:176:                badRequest = BadRequest("Search text must contain at least one term longer than one character");
examples/Web/api/Controllers/SearchesController.cs:182:                badRequest = BadRequest("Search timeout must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:188:                badRequest = BadRequest("Response limit must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:194:                badRequest = BadRequest("File limit must be greater than or equal to one");
examples/Web/api/Controllers/SearchesController.cs:200:                badRequest = BadRequest("Minimum response file count must be greater than or equal to zero");
examples/Web/api/Controllers/SearchesController.cs:206:                badRequest = BadRequest("Maximum peer queue length must be greater than or equal to zero");
examples/Web/api/Controllers/SearchesController.cs:212:                badRequest = BadRequest("Minimum peer upload speed must be greater than or equal to zero");

## Example Web API transfer lifecycle candidates
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:41:            var ex = Record.Exception(() => tracker.TryRemove(TransferDirection.Download, "missing", "missing-id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:47:        public void Transfer_Tracker_Disposes_Cancellation_Token_Source_When_Removing_Transfer()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:50:            var cancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:53:            tracker.AddOrUpdate(transfer, cancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:54:            tracker.TryRemove(TransferDirection.Download, "user", WebAPI.DTO.Transfer.FromSoulseekTransfer(transfer).Id);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:56:            Assert.Throws<ObjectDisposedException>(() => _ = cancellationTokenSource.Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:60:        public void Transfer_Tracker_Disposes_Cancellation_Token_Sources_When_Removing_User()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:63:            var cancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:66:            tracker.AddOrUpdate(transfer, cancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:67:            tracker.TryRemove(TransferDirection.Download, "user");
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:69:            Assert.Throws<ObjectDisposedException>(() => _ = cancellationTokenSource.Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:73:        public void Transfer_Tracker_Disposes_Replaced_Cancellation_Token_Source()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:76:            var oldCancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:77:            var newCancellationTokenSource = new CancellationTokenSource();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:80:            tracker.AddOrUpdate(transfer, oldCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:81:            tracker.AddOrUpdate(transfer, newCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:83:            Assert.Throws<ObjectDisposedException>(() => _ = oldCancellationTokenSource.Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:85:            Assert.Same(newCancellationTokenSource, record.CancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:87:            newCancellationTokenSource.Dispose();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:139:        public async Task Transfer_Enqueue_Defers_Output_File_Creation_Until_Stream_Factory_Is_Invoked()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:145:                Func<Task<Stream>> capturedStreamFactory = null;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:147:                var downloadCompletion = new TaskCompletionSource<Soulseek.Transfer>(TaskCreationOptions.RunContinuationsAsynchronously);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:153:                        It.IsAny<Func<Task<Stream>>>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:157:                        It.IsAny<TransferOptions>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:159:                    .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((callbackUsername, callbackFilename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:161:                        capturedStreamFactory = streamFactory;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:174:                Assert.NotNull(capturedStreamFactory);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:177:                var stream = await capturedStreamFactory();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:178:                await stream.DisposeAsync();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:189:        public async Task Transfer_Enqueue_Disposes_Untracked_Cancellation_Token_Source_When_Download_Faults()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:197:                    It.IsAny<Func<Task<Stream>>>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:201:                    It.IsAny<TransferOptions>(),
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:203:                .Callback<string, string, Func<Task<Stream>>, long?, long, int?, TransferOptions, CancellationToken?>((username, filename, streamFactory, size, startOffset, token, options, cancellationToken) =>
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:215:            Assert.Throws<ObjectDisposedException>(() => _ = GetCancellationTokenSource(capturedCancellationToken).Token);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:232:        private static CancellationTokenSource GetCancellationTokenSource(CancellationToken token)
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:234:            var source = typeof(CancellationToken).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(token) as CancellationTokenSource;
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:40:            tracker.AddOrUpdateMessage("room", new RoomMessage { RoomName = "room", Username = "user", Message = "message" });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:64:            tracker.TryRemoveUser("room", "user");
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:76:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdateMessage("room", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:85:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:94:            tracker.AddOrUpdate("user", new PrivateMessage { Username = "user", Message = "message" });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:105:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs:144:        public void Shared_File_Cache_Disposes_Previous_SQLite_Connection_On_Refresh()
examples/Web/api/Trackers/TransferTracker.cs:19:        public static ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> WithDirection(
examples/Web/api/Trackers/TransferTracker.cs:20:            this ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> allTransfers,
examples/Web/api/Trackers/TransferTracker.cs:24:            return transfers ?? new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>();
examples/Web/api/Trackers/TransferTracker.cs:33:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers)
examples/Web/api/Trackers/TransferTracker.cs:50:        public static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> FromUser(
examples/Web/api/Trackers/TransferTracker.cs:51:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers,
examples/Web/api/Trackers/TransferTracker.cs:55:            return transfers ?? new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:63:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers)
examples/Web/api/Trackers/TransferTracker.cs:76:        public static (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) WithId(
examples/Web/api/Trackers/TransferTracker.cs:77:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers,
examples/Web/api/Trackers/TransferTracker.cs:93:        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; private set; } =
examples/Web/api/Trackers/TransferTracker.cs:94:            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer, CancellationTokenSource)>>>();
examples/Web/api/Trackers/TransferTracker.cs:101:            Transfers.TryAdd(TransferDirection.Download, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:102:            Transfers.TryAdd(TransferDirection.Upload, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:110:        public void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:114:            direction.AddOrUpdate(transfer.Username, GetNewDictionaryForUser(transfer, cancellationTokenSource), (user, dict) =>
examples/Web/api/Trackers/TransferTracker.cs:117:                dict.AddOrUpdate(tx.Id, (tx, cancellationTokenSource), (id, record) =>
examples/Web/api/Trackers/TransferTracker.cs:119:                    if (!ReferenceEquals(record.CancellationTokenSource, cancellationTokenSource))
examples/Web/api/Trackers/TransferTracker.cs:121:                        record.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:135:        public void TryRemove(TransferDirection direction, string username, string id = null)
examples/Web/api/Trackers/TransferTracker.cs:144:                if (directionDict.TryRemove(username, out var removedTransfers))
examples/Web/api/Trackers/TransferTracker.cs:148:                        transfer.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:159:                if (userDict.TryRemove(id, out var removedTransfer))
examples/Web/api/Trackers/TransferTracker.cs:161:                    removedTransfer.CancellationTokenSource?.Dispose();
examples/Web/api/Trackers/TransferTracker.cs:166:                    directionDict.TryRemove(username, out _);
examples/Web/api/Trackers/TransferTracker.cs:179:        public bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer)
examples/Web/api/Trackers/TransferTracker.cs:197:        private static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> GetNewDictionaryForUser(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:199:            var r = new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:201:            r.AddOrUpdate(tx.Id, (tx, cancellationTokenSource), (id, record) => (tx, record.CancellationTokenSource));
examples/Web/api/Trackers/SearchTracker.cs:23:        public void AddOrUpdate(Guid id, Search search)
examples/Web/api/Trackers/SearchTracker.cs:25:            Searches.AddOrUpdate(id, search, (token, search) => search);
examples/Web/api/Trackers/SearchTracker.cs:40:        public void TryRemove(Guid id)
examples/Web/api/Trackers/SearchTracker.cs:42:            Searches.TryRemove(id, out _);
examples/Web/api/Trackers/RoomTracker.cs:41:        public void AddOrUpdateMessage(string roomName, RoomMessage message)
examples/Web/api/Trackers/RoomTracker.cs:48:            Rooms.AddOrUpdate(roomName, new Room() { Messages = new List<RoomMessage>() { message } }, (_, room) =>
examples/Web/api/Trackers/RoomTracker.cs:108:        public void TryRemove(string roomName) => Rooms.TryRemove(roomName, out _);
examples/Web/api/Trackers/RoomTracker.cs:115:        public void TryRemoveUser(string roomName, string username)
examples/Web/api/Trackers/ITransferTracker.cs:15:        ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; }
examples/Web/api/Trackers/ITransferTracker.cs:22:        void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource);
examples/Web/api/Trackers/ITransferTracker.cs:28:        void TryRemove(TransferDirection direction, string username, string id = null);
examples/Web/api/Trackers/ITransferTracker.cs:38:        bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer);
examples/Web/api/Trackers/ISearchTracker.cs:22:        void AddOrUpdate(Guid id, Search search);
examples/Web/api/Trackers/ISearchTracker.cs:33:        void TryRemove(Guid id);
examples/Web/api/Trackers/IRoomTracker.cs:22:        void AddOrUpdateMessage(string roomName, RoomMessage message);
examples/Web/api/Trackers/IRoomTracker.cs:50:        void TryRemove(string roomName);
examples/Web/api/Trackers/IRoomTracker.cs:57:        void TryRemoveUser(string roomName, string username);
examples/Web/api/Trackers/IConversationTracker.cs:23:        void AddOrUpdate(string username, PrivateMessage message);
examples/Web/api/Trackers/IConversationTracker.cs:37:        void TryRemove(string username);
examples/Web/api/Trackers/IBrowseTracker.cs:21:        void AddOrUpdate(string username, BrowseProgressUpdatedEventArgs progress);
examples/Web/api/Trackers/IBrowseTracker.cs:27:        void TryRemove(string username);
examples/Web/api/Trackers/ConversationTracker.cs:24:        public void AddOrUpdate(string username, PrivateMessage message)
examples/Web/api/Trackers/ConversationTracker.cs:31:            Conversations.AddOrUpdate(username, new List<PrivateMessage>() { message }, (_, messageList) =>
examples/Web/api/Trackers/ConversationTracker.cs:51:        public void TryRemove(string username) => Conversations.TryRemove(username, out _);
examples/Web/api/Trackers/BrowseTracker.cs:22:        public void AddOrUpdate(string username, BrowseProgressUpdatedEventArgs progress)
examples/Web/api/Trackers/BrowseTracker.cs:29:            Browses.AddOrUpdate(username, progress, (user, oldprogress) => progress);
examples/Web/api/Trackers/BrowseTracker.cs:36:        public void TryRemove(string username)
examples/Web/api/Trackers/BrowseTracker.cs:37:            => Browses.TryRemove(username, out _);
examples/Web/api/Startup.cs:344:                browseTracker.AddOrUpdate(args.Username, args);
examples/Web/api/Startup.cs:355:                conversationTracker.AddOrUpdate(args.Username, PrivateMessage.FromEventArgs(args));
examples/Web/api/Startup.cs:371:                roomTracker.AddOrUpdateMessage(args.RoomName, message);
examples/Web/api/Startup.cs:384:                roomTracker.TryRemoveUser(args.RoomName, args.Username);
examples/Web/api/Startup.cs:393:                // if ObjectDisposedException, the client is shutting down.
examples/Web/api/Startup.cs:394:                if (!(args.Exception is KickedFromServerException || args.Exception is ObjectDisposedException))
examples/Web/api/Startup.cs:447:            Task.Run(async () =>
examples/Web/api/Startup.cs:533:        private ConcurrentDictionary<string, (string Filename, DateTime ReadyTimestamp, DateTime EnqueuedTimestamp, TaskCompletionSource TaskCompletionSource)> WaitingUploads = new ConcurrentDictionary<string, (string Filename, DateTime ReadyTimestamp, DateTime EnqueuedTimestamp, TaskCompletionSource TaskCompletionSource)>();
examples/Web/api/Startup.cs:558:                    KeyValuePair<string, (string Filename, DateTime ReadyTimestamp, DateTime EnqueuedTimestamp, TaskCompletionSource TaskCompletionSource)> selected;
examples/Web/api/Startup.cs:591:                    WaitingUploads.TryRemove(key, out _);
examples/Web/api/Startup.cs:593:                    value.TaskCompletionSource.SetResult();
examples/Web/api/Startup.cs:612:        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The upload task owns and disposes the cancellation token source in a finally block.")]
examples/Web/api/Startup.cs:635:            var cts = new CancellationTokenSource();
examples/Web/api/Startup.cs:637:            var topts = new TransferOptions(
examples/Web/api/Startup.cs:638:                stateChanged: (e) => tracker.AddOrUpdate(e.Transfer, cts),
examples/Web/api/Startup.cs:639:                progressUpdated: (e) => tracker.AddOrUpdate(e.Transfer, cts),
examples/Web/api/Startup.cs:643:                    var tcs = new TaskCompletionSource();
examples/Web/api/Startup.cs:645:                    WaitingUploads.AddOrUpdate(
examples/Web/api/Startup.cs:667:            Task.Run(async () =>
examples/Web/api/Startup.cs:671:                    using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
examples/Web/api/Startup.cs:672:                    await Client.UploadAsync(username, filename, fileInfo.Length, (_) => Task.FromResult((Stream)stream), options: topts, cancellationToken: cts.Token);
examples/Web/api/Startup.cs:676:                    cts.Dispose();
examples/Web/api/Startup.cs:678:            }).ContinueWith(t =>
examples/Web/api/Startup.cs:757:            public void AddOrUpdate(string username, IPEndPoint endPoint)
examples/Web/api/Startup.cs:773:            public void AddOrUpdate(int responseToken, (string Username, int Token, string Query, SearchResponse SearchResponse) response)
examples/Web/api/Startup.cs:775:                Cache.AddOrUpdate(responseToken, response, (k, v) => response);
examples/Web/api/Startup.cs:776:                _ = Task.Run(async () =>
examples/Web/api/Startup.cs:779:                    TryRemove(responseToken, out var _);
examples/Web/api/Startup.cs:790:            public bool TryRemove(int responseToken, out (string Username, int Token, string Query, SearchResponse SearchResponse) response)
examples/Web/api/Startup.cs:794:                if (Cache.TryRemove(responseToken, out response))
examples/Web/api/SharedFileCache.cs:95:            SQLite?.Dispose();
examples/Web/api/Controllers/UserController.cs:84:                _ = Task.Run(async () =>
examples/Web/api/Controllers/UserController.cs:87:                    BrowseTracker.TryRemove(username);
examples/Web/api/Controllers/RoomsController.cs:324:            Tracker.TryRemove(roomName);
examples/Web/api/Controllers/TransfersController.cs:115:        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The cancellation token source is owned by the tracker after the first state/progress callback; untracked setup failures are disposed before returning.")]
examples/Web/api/Controllers/TransfersController.cs:123:            CancellationTokenSource cts = null;
examples/Web/api/Controllers/TransfersController.cs:143:                var waitUntilEnqueue = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
examples/Web/api/Controllers/TransfersController.cs:145:                cts = new CancellationTokenSource();
examples/Web/api/Controllers/TransfersController.cs:147:                var downloadTask = Client.DownloadAsync(username, request.Filename, () => Task.FromResult((Stream)GetLocalFileStream(request.Filename, OutputDirectory)), request.Size, 0, request.Token, new TransferOptions(disposeOutputStreamOnCompletion: true, stateChanged: (e) =>
examples/Web/api/Controllers/TransfersController.cs:149:                    Tracker.AddOrUpdate(e.Transfer, cts);
examples/Web/api/Controllers/TransfersController.cs:158:                    Tracker.AddOrUpdate(e.Transfer, cts);
examples/Web/api/Controllers/TransfersController.cs:168:                    DisposeUntrackedCancellationTokenSource(cts, isTracked);
examples/Web/api/Controllers/TransfersController.cs:185:                DisposeUntrackedCancellationTokenSource(cts, isTracked);
examples/Web/api/Controllers/TransfersController.cs:338:        private static FileStream GetLocalFileStream(string remoteFilename, string saveDirectory)
examples/Web/api/Controllers/TransfersController.cs:348:            return new FileStream(localFilename, FileMode.Create);
examples/Web/api/Controllers/TransfersController.cs:351:        private static void DisposeUntrackedCancellationTokenSource(CancellationTokenSource cts, int isTracked)
examples/Web/api/Controllers/TransfersController.cs:355:                cts.Dispose();
examples/Web/api/Controllers/TransfersController.cs:363:                transfer.CancellationTokenSource.Cancel();
examples/Web/api/Controllers/TransfersController.cs:367:                    Tracker.TryRemove(direction, username, id);
examples/Web/api/Controllers/ConversationsController.cs:99:                tasks.Add(Task.Run(async () =>
examples/Web/api/Controllers/ConversationsController.cs:127:            var deleted = Tracker.Conversations.TryRemove(username, out _);
examples/Web/api/Controllers/ConversationsController.cs:212:            Tracker.AddOrUpdate(username, new PrivateMessage()
examples/Web/api/Controllers/SearchesController.cs:61:                responseReceived: (e) => Tracker.AddOrUpdate(id, e.Search),
examples/Web/api/Controllers/SearchesController.cs:62:                stateChanged: (e) => Tracker.AddOrUpdate(id, e.Search));
examples/Web/api/Controllers/SearchesController.cs:78:                Tracker.TryRemove(id);
examples/Web/api/Controllers/SearchesController.cs:111:                responseReceived: (e) => Tracker.AddOrUpdate(id, e.Search),
examples/Web/api/Controllers/SearchesController.cs:112:                stateChanged: (e) => Tracker.AddOrUpdate(id, e.Search));
examples/Web/api/Controllers/SearchesController.cs:128:                Tracker.TryRemove(id);

## Example Web API tracker state candidates
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:31:    using WebAPI.Trackers;
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:37:        public void Transfer_Tracker_Ignores_Stale_Transfer_Removal()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:39:            var tracker = new TransferTracker();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:41:            var ex = Record.Exception(() => tracker.TryRemove(TransferDirection.Download, "missing", "missing-id"));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:47:        public void Transfer_Tracker_Disposes_Cancellation_Token_Source_When_Removing_Transfer()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:49:            var tracker = new TransferTracker();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:53:            tracker.AddOrUpdate(transfer, cancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:54:            tracker.TryRemove(TransferDirection.Download, "user", WebAPI.DTO.Transfer.FromSoulseekTransfer(transfer).Id);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:60:        public void Transfer_Tracker_Disposes_Cancellation_Token_Sources_When_Removing_User()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:62:            var tracker = new TransferTracker();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:66:            tracker.AddOrUpdate(transfer, cancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:67:            tracker.TryRemove(TransferDirection.Download, "user");
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:73:        public void Transfer_Tracker_Disposes_Replaced_Cancellation_Token_Source()
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:75:            var tracker = new TransferTracker();
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:80:            tracker.AddOrUpdate(transfer, oldCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:81:            tracker.AddOrUpdate(transfer, newCancellationTokenSource);
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:84:            Assert.True(tracker.TryGet(TransferDirection.Download, "user", WebAPI.DTO.Transfer.FromSoulseekTransfer(transfer).Id, out var record));
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:93:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:103:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:113:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:131:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), Mock.Of<ISoulseekClient>(), new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:167:                var controller = new TransfersController(CreateConfiguration(root), client.Object, new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTransferTests.cs:209:            var controller = new TransfersController(CreateConfiguration(Path.GetTempPath()), client.Object, new TransferTracker());
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:1:// <copyright file="WebApiTrackerTests.cs" company="slskdN Team">
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:23:    using WebAPI.Trackers;
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:26:    public class WebApiTrackerTests
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:28:        [Theory(DisplayName = "RoomTracker rejects invalid message limit")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:31:        public void RoomTracker_Rejects_Invalid_Message_Limit(int messageLimit)
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:32:            => Assert.Throws<ArgumentOutOfRangeException>(() => new RoomTracker(messageLimit));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:34:        [Fact(DisplayName = "RoomTracker normalizes missing message list")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:35:        public void RoomTracker_Normalizes_Missing_Message_List()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:37:            var tracker = new RoomTracker(messageLimit: 1);
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:38:            tracker.TryAdd("room", new Room { Messages = null });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:40:            tracker.AddOrUpdateMessage("room", new RoomMessage { RoomName = "room", Username = "user", Message = "message" });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:42:            Assert.True(tracker.TryGet("room", out var room));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:43:            Assert.Single(room.Messages);
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:46:        [Fact(DisplayName = "RoomTracker normalizes missing user list")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:47:        public void RoomTracker_Normalizes_Missing_User_List()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:49:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:50:            tracker.TryAdd("room", new Room { Users = null });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:52:            tracker.TryAddUser("room", new UserData("user", UserPresence.Online, 0, 0, 0, 0, "US"));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:54:            Assert.True(tracker.TryGet("room", out var room));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:55:            Assert.Single(room.Users);
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:58:        [Fact(DisplayName = "RoomTracker tolerates missing user list when removing users")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:59:        public void RoomTracker_Tolerates_Missing_User_List_When_Removing_Users()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:61:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:62:            tracker.TryAdd("room", new Room { Users = null });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:64:            tracker.TryRemoveUser("room", "user");
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:66:            Assert.True(tracker.TryGet("room", out var room));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:67:            Assert.Null(room.Users);
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:70:        [Fact(DisplayName = "RoomTracker rejects null payloads")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:71:        public void RoomTracker_Rejects_Null_Payloads()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:73:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:75:            Assert.Throws<ArgumentNullException>(() => tracker.TryAdd("room", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:76:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdateMessage("room", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:77:            Assert.Throws<ArgumentNullException>(() => tracker.TryAddUser("room", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:80:        [Fact(DisplayName = "ConversationTracker rejects null messages")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:81:        public void ConversationTracker_Rejects_Null_Messages()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:83:            var tracker = new ConversationTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:85:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:88:        [Fact(DisplayName = "ConversationTracker normalizes null message lists")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:89:        public void ConversationTracker_Normalizes_Null_Message_Lists()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:91:            var tracker = new ConversationTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:92:            tracker.Conversations.TryAdd("user", null);
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:94:            tracker.AddOrUpdate("user", new PrivateMessage { Username = "user", Message = "message" });
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:96:            Assert.True(tracker.TryGet("user", out var messages));
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:100:        [Fact(DisplayName = "BrowseTracker rejects null progress")]
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:101:        public void BrowseTracker_Rejects_Null_Progress()
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:103:            var tracker = new BrowseTracker();
tests/Soulseek.Tests.Unit/WebApiTrackerTests.cs:105:            Assert.Throws<ArgumentNullException>(() => tracker.AddOrUpdate("user", null));
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:34:    using WebAPI.Trackers;
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:42:            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:56:            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:66:            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:68:            var response = await controller.PostUsers(new SearchRequest { SearchText = "music" }, " ");
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:116:            var controller = new SearchesController(Mock.Of<ISoulseekClient>(), new SearchTracker());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:138:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:139:            tracker.TryAdd("room", new WebAPI.Room());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:153:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:154:            tracker.TryAdd("room", new WebAPI.Room());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:168:            var tracker = new RoomTracker();
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:169:            tracker.TryAdd("room", new WebAPI.Room());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:183:            var controller = new ConversationsController(Mock.Of<ISoulseekClient>(), new ConversationTracker());
tests/Soulseek.Tests.Unit/WebApiRequestTests.cs:193:            var controller = new ConversationsController(Mock.Of<ISoulseekClient>(), new ConversationTracker());
examples/Web/api/Trackers/BrowseTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/BrowseTracker.cs:10:    public class BrowseTracker : IBrowseTracker
examples/Web/api/Trackers/BrowseTracker.cs:15:        public ConcurrentDictionary<string, BrowseProgressUpdatedEventArgs> Browses { get; } = new ConcurrentDictionary<string, BrowseProgressUpdatedEventArgs>();
examples/Web/api/Trackers/BrowseTracker.cs:22:        public void AddOrUpdate(string username, BrowseProgressUpdatedEventArgs progress)
examples/Web/api/Trackers/BrowseTracker.cs:29:            Browses.AddOrUpdate(username, progress, (user, oldprogress) => progress);
examples/Web/api/Trackers/BrowseTracker.cs:36:        public void TryRemove(string username)
examples/Web/api/Trackers/BrowseTracker.cs:37:            => Browses.TryRemove(username, out _);
examples/Web/api/Trackers/BrowseTracker.cs:45:        public bool TryGet(string username, out BrowseProgressUpdatedEventArgs progress)
examples/Web/api/Trackers/BrowseTracker.cs:46:            => Browses.TryGetValue(username, out progress);
examples/Web/api/Trackers/IRoomTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/IRoomTracker.cs:10:    public interface IRoomTracker
examples/Web/api/Trackers/IRoomTracker.cs:15:        ConcurrentDictionary<string, Room> Rooms { get; }
examples/Web/api/Trackers/IRoomTracker.cs:22:        void AddOrUpdateMessage(string roomName, RoomMessage message);
examples/Web/api/Trackers/IRoomTracker.cs:29:        void TryAdd(string roomName, Room room);
examples/Web/api/Trackers/IRoomTracker.cs:36:        void TryAddUser(string roomName, UserData userData);
examples/Web/api/Trackers/IRoomTracker.cs:44:        bool TryGet(string roomName, out Room room);
examples/Web/api/Trackers/IRoomTracker.cs:50:        void TryRemove(string roomName);
examples/Web/api/Trackers/IRoomTracker.cs:57:        void TryRemoveUser(string roomName, string username);
examples/Web/api/Trackers/TransferTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/TransferTracker.cs:11:    public static class TransferTrackerExtensions
examples/Web/api/Trackers/TransferTracker.cs:16:        /// <param name="allTransfers"></param>
examples/Web/api/Trackers/TransferTracker.cs:19:        public static ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> WithDirection(
examples/Web/api/Trackers/TransferTracker.cs:20:            this ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> allTransfers,
examples/Web/api/Trackers/TransferTracker.cs:23:            allTransfers.TryGetValue(direction, out var transfers);
examples/Web/api/Trackers/TransferTracker.cs:24:            return transfers ?? new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>();
examples/Web/api/Trackers/TransferTracker.cs:30:        /// <param name="directedTransfers"></param>
examples/Web/api/Trackers/TransferTracker.cs:33:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers)
examples/Web/api/Trackers/TransferTracker.cs:35:            return directedTransfers.Select(u => new
examples/Web/api/Trackers/TransferTracker.cs:47:        /// <param name="directedTransfers"></param>
examples/Web/api/Trackers/TransferTracker.cs:50:        public static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> FromUser(
examples/Web/api/Trackers/TransferTracker.cs:51:            this ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers,
examples/Web/api/Trackers/TransferTracker.cs:54:            directedTransfers.TryGetValue(username, out var transfers);
examples/Web/api/Trackers/TransferTracker.cs:55:            return transfers ?? new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:61:        /// <param name="userTransfers"></param>
examples/Web/api/Trackers/TransferTracker.cs:63:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers)
examples/Web/api/Trackers/TransferTracker.cs:65:            return userTransfers.Values
examples/Web/api/Trackers/TransferTracker.cs:73:        /// <param name="userTransfers"></param>
examples/Web/api/Trackers/TransferTracker.cs:77:            this ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers,
examples/Web/api/Trackers/TransferTracker.cs:80:            userTransfers.TryGetValue(id, out var transfer);
examples/Web/api/Trackers/TransferTracker.cs:88:    public class TransferTracker : ITransferTracker
examples/Web/api/Trackers/TransferTracker.cs:93:        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; private set; } =
examples/Web/api/Trackers/TransferTracker.cs:94:            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer, CancellationTokenSource)>>>();
examples/Web/api/Trackers/TransferTracker.cs:97:        ///     Initializes a new instance of the <see cref="TransferTracker"/> class.
examples/Web/api/Trackers/TransferTracker.cs:99:        public TransferTracker()
examples/Web/api/Trackers/TransferTracker.cs:101:            Transfers.TryAdd(TransferDirection.Download, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:102:            Transfers.TryAdd(TransferDirection.Upload, new ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
examples/Web/api/Trackers/TransferTracker.cs:110:        public void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:112:            Transfers.TryGetValue(transfer.Direction, out var direction);
examples/Web/api/Trackers/TransferTracker.cs:114:            direction.AddOrUpdate(transfer.Username, GetNewDictionaryForUser(transfer, cancellationTokenSource), (user, dict) =>
examples/Web/api/Trackers/TransferTracker.cs:117:                dict.AddOrUpdate(tx.Id, (tx, cancellationTokenSource), (id, record) =>
examples/Web/api/Trackers/TransferTracker.cs:135:        public void TryRemove(TransferDirection direction, string username, string id = null)
examples/Web/api/Trackers/TransferTracker.cs:137:            if (!Transfers.TryGetValue(direction, out var directionDict))
examples/Web/api/Trackers/TransferTracker.cs:144:                if (directionDict.TryRemove(username, out var removedTransfers))
examples/Web/api/Trackers/TransferTracker.cs:146:                    foreach (var transfer in removedTransfers.Values)
examples/Web/api/Trackers/TransferTracker.cs:154:                if (!directionDict.TryGetValue(username, out var userDict))
examples/Web/api/Trackers/TransferTracker.cs:159:                if (userDict.TryRemove(id, out var removedTransfer))
examples/Web/api/Trackers/TransferTracker.cs:166:                    directionDict.TryRemove(username, out _);
examples/Web/api/Trackers/TransferTracker.cs:179:        public bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer)
examples/Web/api/Trackers/TransferTracker.cs:183:            if (Transfers.TryGetValue(direction, out var transfers))
examples/Web/api/Trackers/TransferTracker.cs:185:                if (transfers.TryGetValue(username, out var user))
examples/Web/api/Trackers/TransferTracker.cs:187:                    if (user.TryGetValue(id, out transfer))
examples/Web/api/Trackers/TransferTracker.cs:197:        private static ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)> GetNewDictionaryForUser(Transfer transfer, CancellationTokenSource cancellationTokenSource)
examples/Web/api/Trackers/TransferTracker.cs:199:            var r = new ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
examples/Web/api/Trackers/TransferTracker.cs:201:            r.AddOrUpdate(tx.Id, (tx, cancellationTokenSource), (id, record) => (tx, record.CancellationTokenSource));
examples/Web/api/Trackers/IConversationTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/IConversationTracker.cs:10:    public interface IConversationTracker
examples/Web/api/Trackers/IConversationTracker.cs:15:        ConcurrentDictionary<string, IList<PrivateMessage>> Conversations { get; }
examples/Web/api/Trackers/IConversationTracker.cs:23:        void AddOrUpdate(string username, PrivateMessage message);
examples/Web/api/Trackers/IConversationTracker.cs:31:        bool TryGet(string username, out IList<PrivateMessage> messages);
examples/Web/api/Trackers/IConversationTracker.cs:37:        void TryRemove(string username);
examples/Web/api/Trackers/SearchTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/SearchTracker.cs:10:    public class SearchTracker : ISearchTracker
examples/Web/api/Trackers/SearchTracker.cs:15:        public ConcurrentDictionary<Guid, Search> Searches { get; private set; } =
examples/Web/api/Trackers/SearchTracker.cs:16:            new ConcurrentDictionary<Guid, Search>();
examples/Web/api/Trackers/SearchTracker.cs:23:        public void AddOrUpdate(Guid id, Search search)
examples/Web/api/Trackers/SearchTracker.cs:25:            Searches.AddOrUpdate(id, search, (token, search) => search);
examples/Web/api/Trackers/SearchTracker.cs:31:        public void Clear()
examples/Web/api/Trackers/SearchTracker.cs:33:            Searches.Clear();
examples/Web/api/Trackers/SearchTracker.cs:40:        public void TryRemove(Guid id)
examples/Web/api/Trackers/SearchTracker.cs:42:            Searches.TryRemove(id, out _);
examples/Web/api/Trackers/IBrowseTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/IBrowseTracker.cs:9:    public interface IBrowseTracker
examples/Web/api/Trackers/IBrowseTracker.cs:14:        ConcurrentDictionary<string, BrowseProgressUpdatedEventArgs> Browses { get; }
examples/Web/api/Trackers/IBrowseTracker.cs:21:        void AddOrUpdate(string username, BrowseProgressUpdatedEventArgs progress);
examples/Web/api/Trackers/IBrowseTracker.cs:27:        void TryRemove(string username);
examples/Web/api/Trackers/IBrowseTracker.cs:35:        bool TryGet(string username, out BrowseProgressUpdatedEventArgs progress);
examples/Web/api/Trackers/ConversationTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/ConversationTracker.cs:11:    public class ConversationTracker : IConversationTracker
examples/Web/api/Trackers/ConversationTracker.cs:16:        public ConcurrentDictionary<string, IList<PrivateMessage>> Conversations { get; } = new ConcurrentDictionary<string, IList<PrivateMessage>>();
examples/Web/api/Trackers/ConversationTracker.cs:24:        public void AddOrUpdate(string username, PrivateMessage message)
examples/Web/api/Trackers/ConversationTracker.cs:31:            Conversations.AddOrUpdate(username, new List<PrivateMessage>() { message }, (_, messageList) =>
examples/Web/api/Trackers/ConversationTracker.cs:45:        public bool TryGet(string username, out IList<PrivateMessage> messages) => Conversations.TryGetValue(username, out messages);
examples/Web/api/Trackers/ConversationTracker.cs:51:        public void TryRemove(string username) => Conversations.TryRemove(username, out _);
examples/Web/api/Trackers/RoomTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/RoomTracker.cs:13:    public class RoomTracker : IRoomTracker
examples/Web/api/Trackers/RoomTracker.cs:16:        ///     Initializes a new instance of the <see cref="RoomTracker"/> class.
examples/Web/api/Trackers/RoomTracker.cs:19:        public RoomTracker(int messageLimit = 25)
examples/Web/api/Trackers/RoomTracker.cs:32:        public ConcurrentDictionary<string, Room> Rooms { get; } = new ConcurrentDictionary<string, Room>();
examples/Web/api/Trackers/RoomTracker.cs:41:        public void AddOrUpdateMessage(string roomName, RoomMessage message)
examples/Web/api/Trackers/RoomTracker.cs:48:            Rooms.AddOrUpdate(roomName, new Room() { Messages = new List<RoomMessage>() { message } }, (_, room) =>
examples/Web/api/Trackers/RoomTracker.cs:50:                room.Messages ??= new List<RoomMessage>();
examples/Web/api/Trackers/RoomTracker.cs:52:                if (room.Messages.Count >= MessageLimit)
examples/Web/api/Trackers/RoomTracker.cs:54:                    room.Messages = room.Messages.TakeLast(MessageLimit - 1).ToList();
examples/Web/api/Trackers/RoomTracker.cs:57:                room.Messages.Add(message);
examples/Web/api/Trackers/RoomTracker.cs:67:        public void TryAdd(string roomName, Room room)
examples/Web/api/Trackers/RoomTracker.cs:74:            Rooms.TryAdd(roomName, room);
examples/Web/api/Trackers/RoomTracker.cs:82:        public void TryAddUser(string roomName, UserData userData)
examples/Web/api/Trackers/RoomTracker.cs:89:            if (Rooms.TryGetValue(roomName, out var room))
examples/Web/api/Trackers/RoomTracker.cs:91:                room.Users ??= new List<UserData>();
examples/Web/api/Trackers/RoomTracker.cs:92:                room.Users.Add(userData);
examples/Web/api/Trackers/RoomTracker.cs:101:        public bool TryGet(string roomName, out Room room) => Rooms.TryGetValue(roomName, out room);
examples/Web/api/Trackers/RoomTracker.cs:108:        public void TryRemove(string roomName) => Rooms.TryRemove(roomName, out _);
examples/Web/api/Trackers/RoomTracker.cs:115:        public void TryRemoveUser(string roomName, string username)
examples/Web/api/Trackers/RoomTracker.cs:117:            if (Rooms.TryGetValue(roomName, out var room))
examples/Web/api/Trackers/RoomTracker.cs:119:                if (room.Users == null)
examples/Web/api/Trackers/RoomTracker.cs:124:                room.Users = room.Users.Where(u => u.Username != username).ToList();
examples/Web/api/Trackers/ISearchTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/ISearchTracker.cs:10:    public interface ISearchTracker
examples/Web/api/Trackers/ISearchTracker.cs:15:        ConcurrentDictionary<Guid, Search> Searches { get; }
examples/Web/api/Trackers/ISearchTracker.cs:22:        void AddOrUpdate(Guid id, Search search);
examples/Web/api/Trackers/ISearchTracker.cs:27:        void Clear();
examples/Web/api/Trackers/ISearchTracker.cs:33:        void TryRemove(Guid id);
examples/Web/api/Trackers/ITransferTracker.cs:1:namespace WebAPI.Trackers
examples/Web/api/Trackers/ITransferTracker.cs:10:    public interface ITransferTracker
examples/Web/api/Trackers/ITransferTracker.cs:15:        ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; }
examples/Web/api/Trackers/ITransferTracker.cs:22:        void AddOrUpdate(Transfer transfer, CancellationTokenSource cancellationTokenSource);
examples/Web/api/Trackers/ITransferTracker.cs:28:        void TryRemove(TransferDirection direction, string username, string id = null);
examples/Web/api/Trackers/ITransferTracker.cs:38:        bool TryGet(TransferDirection direction, string username, string id, out (DTO.Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer);

## Security-sensitive material candidates
./scripts/check-remediation-baseline.sh:603:secret_pattern='-----BEGIN (RSA |DSA |EC |OPENSSH |PGP )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{36,}|xox[baprs]-[A-Za-z0-9-]{20,}|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)["'\'']?\s*[:=]\s*["'\''][A-Za-z0-9_./+=-]{24,}["'\'']'
./scripts/scan-bug-council-candidates.sh:158:  'PRIVATE KEY|gh[pousr]_|xox[baprs]-|AKIA[0-9A-Z]{16}|(?i)(api[_-]?key|access[_-]?token|client[_-]?secret)' \

# End of candidate scan. Every hit must be ledgered as Fixed, Existing guard, False positive, or Out of scope before a council sweep is closed.
