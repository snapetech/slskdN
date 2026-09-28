// <copyright file="ListeningPartyService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.ListeningParty;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using slskd.Mesh.Dht;
using slskd.NowPlaying;
using slskd.PodCore;
using slskd.PodCore.API;
using slskd.Core.Security;
using slskd.Streaming;

/// <summary>
///     Stores and publishes metadata-only listen-along state for pods.
/// </summary>
public sealed class ListeningPartyService : IListeningPartyService, IDisposable
{
    private const int AnnouncementTtlSeconds = 900;
    private const string DirectoryIndexKey = "slskdn:listening-party:index:v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DirectoryRefreshInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan HostSessionLeaseDuration = TimeSpan.FromMinutes(30);

    private readonly IHubContext<ListeningPartyHub> _hub;
    private readonly IMeshDhtClient _dht;
    private readonly IPodMessageRouter _messageRouter;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NowPlayingService _nowPlaying;
    private readonly IStreamTicketService _streamTickets;
    private readonly ILogger<ListeningPartyService> _logger;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<Options> _options;
    private readonly ConcurrentDictionary<string, ListeningPartyEvent> _states = new();
    private readonly object _remoteStatesLock = new();
    private readonly Dictionary<(string PodId, string ChannelId), ListeningPartyEvent> _remoteStates = new();
    private readonly HashSet<(string PodId, string ChannelId)> _remoteStateReservations = new();
    private readonly Dictionary<(string PodId, string ChannelId), Queue<string>> _retiredRemoteParties = new();
    private readonly ConcurrentDictionary<string, ListeningPartyAnnouncement> _directory = new();

    // Directory ownership, withdrawal lifetime and same-server index ordering: ADR-0020.
    private readonly object _directoryStateLock = new();
    private readonly Dictionary<string, (string RoomKey, long ExpiresAt)> _withdrawnListings = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingDirectoryWithdrawals = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _directoryIndexGate = new(1, 1);
    private readonly object _directoryRefreshLock = new();
    private Task? _directoryRefreshTask;
    private DateTimeOffset _directoryLastRefreshedAt;
    private DateTimeOffset _directoryLastForcedAt;

    // Fences browser host sessions and renews their lease; see ADR-0024.
    private readonly object _hostSessionsLock = new();
    private readonly Dictionary<(string PodId, string ChannelId), HostSessionLease> _hostSessions = new();
    private readonly object _subscriptionsLock = new();
    private readonly Dictionary<(string ConnectionId, string PodId, string ChannelId), PartySubscription> _subscriptions = new();
    private const int MaxSubscriptions = 4096;
    private const int MaxSubscriptionsPerConnection = 16;
    private const int MaxRemoteStates = 256;
    private const int MaxRetiredRemoteRooms = 256;
    private const int MaxRetiredPartiesPerRoom = 16;

    // Per-room ordering and bounded queue ownership: ADR-0015.
    private readonly object _publicationLock = new();
    private readonly Dictionary<(string PodId, string ChannelId), (SemaphoreSlim Gate, int Reservations)> _publications = new();
    private const int MaxPublicationRooms = 256;
    private const int MaxPublicationsPerRoom = 16;
    private long _sequence;
    private readonly TimeProvider _timeProvider;

    public ListeningPartyService(
        IHubContext<ListeningPartyHub> hub,
        IMeshDhtClient dht,
        IPodMessageRouter messageRouter,
        IServiceScopeFactory scopeFactory,
        NowPlayingService nowPlaying,
        IStreamTicketService streamTickets,
        ILogger<ListeningPartyService> logger,
        Microsoft.Extensions.Options.IOptionsMonitor<Options> options,
        TimeProvider? timeProvider = null)
    {
        _hub = hub;
        _dht = dht;
        _messageRouter = messageRouter;
        _scopeFactory = scopeFactory;
        _nowPlaying = nowPlaying;
        _streamTickets = streamTickets;
        _logger = logger;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool Subscribe(string connectionId, string podId, string channelId, ClaimsPrincipal user)
    {
        var peerId = PodApiAuthorizer.GetAuthenticatedPeerId(user);
        if (peerId == null) return false;
        var key = (connectionId, podId, channelId);
        lock (_subscriptionsLock)
        {
            if (!_subscriptions.ContainsKey(key) &&
                (_subscriptions.Count >= MaxSubscriptions ||
                 _subscriptions.Keys.Count(entry => entry.ConnectionId == connectionId) >= MaxSubscriptionsPerConnection))
            {
                return false;
            }

            _subscriptions[key] = new PartySubscription(connectionId, podId, channelId, peerId, user.IsInRole(AuthRole.AdministratorOnly));
            return true;
        }
    }

    public void Unsubscribe(string connectionId, string podId, string channelId)
    {
        lock (_subscriptionsLock)
        {
            _subscriptions.Remove((connectionId, podId, channelId));
        }
    }

    public void Disconnect(string connectionId)
    {
        lock (_subscriptionsLock)
        {
            foreach (var key in _subscriptions.Keys.Where(key => key.ConnectionId == connectionId).ToArray())
            {
                _subscriptions.Remove(key);
            }
        }
    }

    // Recheck mutable membership for live delivery; see ADR-0015.
    private async Task SendToSubscribersAsync(ListeningPartyEvent state, CancellationToken cancellationToken)
    {
        PartySubscription[] subscriptions;
        lock (_subscriptionsLock)
        {
            subscriptions = _subscriptions.Values.Where(entry => entry.PodId == state.PodId && entry.ChannelId == state.ChannelId).ToArray();
        }

        if (subscriptions.Length == 0) return;
        var members = new HashSet<string>(StringComparer.Ordinal);
        if (subscriptions.Any(entry => !entry.IsAdministrator))
        {
            using var scope = _scopeFactory.CreateScope();
            var pods = scope.ServiceProvider.GetRequiredService<IPodService>();
            var current = await pods.GetMembersAsync(state.PodId, cancellationToken).ConfigureAwait(false);
            members.UnionWith(current.Where(member => !member.IsBanned).Select(member => member.PeerId));
        }

        var recipients = new List<string>();
        var denied = new List<string>();
        lock (_subscriptionsLock)
        {
            foreach (var subscription in subscriptions)
            {
                var key = (subscription.ConnectionId, subscription.PodId, subscription.ChannelId);
                if (!_subscriptions.TryGetValue(key, out var current) || !ReferenceEquals(current, subscription)) continue;
                if (subscription.IsAdministrator || members.Contains(subscription.PeerId))
                {
                    recipients.Add(subscription.ConnectionId);
                }
                else
                {
                    _subscriptions.Remove(key);
                    denied.Add(subscription.ConnectionId);
                }
            }
        }

        if (denied.Count > 0)
        {
            await _hub.Clients.Clients(denied).SendAsync("partyAccessRevoked", cancellationToken).ConfigureAwait(false);
        }

        if (recipients.Count > 0)
        {
            await _hub.Clients.Clients(recipients).SendAsync("partyState", state, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed record PartySubscription(string ConnectionId, string PodId, string ChannelId, string PeerId, bool IsAdministrator);

    public Task<ListeningPartyEvent?> GetStateAsync(string podId, string channelId, CancellationToken cancellationToken = default)
    {
        var key = StateKey(podId, channelId);
        if (_states.TryGetValue(key, out var localState))
        {
            return Task.FromResult<ListeningPartyEvent?>(localState);
        }

        lock (_remoteStatesLock)
        {
            if (_states.TryGetValue(key, out localState))
            {
                return Task.FromResult<ListeningPartyEvent?>(localState);
            }

            _remoteStates.TryGetValue((podId?.Trim() ?? string.Empty, channelId?.Trim() ?? string.Empty), out var remoteState);
            return Task.FromResult<ListeningPartyEvent?>(remoteState);
        }
    }

    public Task<ListeningPartyEvent?> GetStateByPartyIdAsync(string partyId, CancellationToken cancellationToken = default)
    {
        var normalizedPartyId = partyId?.Trim() ?? string.Empty;
        var state = _states.Values.FirstOrDefault(x => string.Equals(x.PartyId, normalizedPartyId, StringComparison.Ordinal));
        return Task.FromResult(state);
    }

    public async Task<ListeningPartyRemoteApplyResult> ApplyRemoteMessageAsync(
        PodMessage message,
        string authenticatedPeerId,
        CancellationToken cancellationToken = default)
    {
        var peerId = authenticatedPeerId?.Trim() ?? string.Empty;
        if (message == null || string.IsNullOrWhiteSpace(peerId) ||
            !string.Equals(message.SenderPeerId?.Trim(), peerId, StringComparison.OrdinalIgnoreCase))
        {
            return ListeningPartyRemoteApplyResult.Forbidden;
        }

        var state = ParseRemoteState(message);
        var podId = state.PodId;
        var channelId = state.ChannelId;
        var room = (podId, channelId);

        return await RunInPublicationQueueAsync(podId, channelId, async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var pods = scope.ServiceProvider.GetRequiredService<IPodService>();
            var channel = await pods.GetChannelAsync(podId, channelId, cancellationToken).ConfigureAwait(false);
            if (channel == null)
            {
                throw new ListeningPartyRoomNotFoundException();
            }

            var members = await pods.GetMembersAsync(podId, cancellationToken).ConfigureAwait(false);
            if (!members.Any(member => !member.IsBanned &&
                string.Equals(member.PeerId, peerId, StringComparison.OrdinalIgnoreCase)))
            {
                return ListeningPartyRemoteApplyResult.Forbidden;
            }

            var reserveRemoteRoom = false;
            lock (_remoteStatesLock)
            {
                if (_states.ContainsKey(StateKey(podId, channelId)) || IsRetiredRemotePartyNoLock(room, state.PartyId))
                {
                    return ListeningPartyRemoteApplyResult.Ignored;
                }

                _remoteStates.TryGetValue(room, out var current);
                if (current == null)
                {
                    if (state.Action == "stop")
                    {
                        return ListeningPartyRemoteApplyResult.Ignored;
                    }

                    if (!_remoteStateReservations.Contains(room))
                    {
                        if (_remoteStates.Count + _remoteStateReservations.Count >= MaxRemoteStates)
                        {
                            return ListeningPartyRemoteApplyResult.Ignored;
                        }

                        _remoteStateReservations.Add(room);
                        reserveRemoteRoom = true;
                    }
                }
                else if (string.Equals(current.PartyId, state.PartyId, StringComparison.Ordinal))
                {
                    if (state.Sequence <= current.Sequence)
                    {
                        return ListeningPartyRemoteApplyResult.Ignored;
                    }
                }
                else
                {
                    if (state.Action == "stop")
                    {
                        return ListeningPartyRemoteApplyResult.Ignored;
                    }
                }
            }

            try
            {
                var storage = scope.ServiceProvider.GetRequiredService<IPodMessageStorage>();
                if (!await storage.StoreMessageAsync(podId, channelId, message, cancellationToken).ConfigureAwait(false))
                {
                    return ListeningPartyRemoteApplyResult.Ignored;
                }

                lock (_remoteStatesLock)
                {
                    if (state.Action == "stop")
                    {
                        RetireRemotePartyNoLock(room, state.PartyId);
                        _remoteStates.Remove(room);
                    }
                    else
                    {
                        if (_remoteStates.TryGetValue(room, out var previous) &&
                            !string.Equals(previous.PartyId, state.PartyId, StringComparison.Ordinal))
                        {
                            RetireRemotePartyNoLock(room, previous.PartyId);
                        }

                        _remoteStates[room] = state;
                    }

                    if (reserveRemoteRoom)
                    {
                        _remoteStateReservations.Remove(room);
                        reserveRemoteRoom = false;
                    }
                }

                await SendToSubscribersAsync(state, cancellationToken).ConfigureAwait(false);
                return ListeningPartyRemoteApplyResult.Applied;
            }
            finally
            {
                if (reserveRemoteRoom)
                {
                    lock (_remoteStatesLock)
                    {
                        _remoteStateReservations.Remove(room);
                    }
                }
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private ListeningPartyEvent ParseRemoteState(PodMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId) || !message.MessageId.StartsWith("listen-", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(message.Body) ||
            !PodValidation.IsValidPodId(message.PodId) || !PodValidation.IsValidChannelId(message.ChannelId) ||
            !PodValidation.IsValidPeerId(message.SenderPeerId))
        {
            throw new ArgumentException("Listen-along message identity is invalid.", nameof(message));
        }

        var messageValidation = PodValidation.ValidateMessage(message);
        if (!messageValidation.IsValid)
        {
            throw new ArgumentException(messageValidation.Error, nameof(message));
        }

        ListeningPartyEvent? state;
        try
        {
            state = JsonSerializer.Deserialize<ListeningPartyEvent>(message.Body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Listen-along message body is invalid.", nameof(message), exception);
        }

        if (state == null || state.Kind != ListeningPartyEvent.KindName ||
            !string.Equals(state.PodId, message.PodId, StringComparison.Ordinal) ||
            !string.Equals(state.ChannelId, message.ChannelId, StringComparison.Ordinal) ||
            !string.Equals(state.HostPeerId, message.SenderPeerId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(state.PartyId) || state.PartyId.Length > 128 ||
            state.Action is not ("play" or "pause" or "seek" or "stop") ||
            (state.Action != "stop" && string.IsNullOrWhiteSpace(state.ContentId)) ||
            (state.ContentId?.Length ?? 0) > 512 || (state.Title?.Length ?? 0) > 512 || (state.Artist?.Length ?? 0) > 512 ||
            (state.Album?.Length ?? 0) > 512 || state.Tags == null || state.Tags.Count > 10 ||
            state.Tags.Any(tag => tag == null || tag.Length > 128) ||
            !double.IsFinite(state.PositionSeconds) || state.PositionSeconds < 0 ||
            state.Sequence <= 0 || state.ServerTimeUnixMs <= 0 || message.TimestampUnixMs != state.ServerTimeUnixMs)
        {
            throw new ArgumentException("Listen-along state is invalid.", nameof(message));
        }

        var now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        if (Math.Abs(now - state.ServerTimeUnixMs) > TimeSpan.FromMinutes(2).TotalMilliseconds)
        {
            throw new ArgumentException("Listen-along state is outside the accepted time window.", nameof(message));
        }

        return state;
    }

    private bool IsRetiredRemotePartyNoLock((string PodId, string ChannelId) room, string partyId)
        => _retiredRemoteParties.TryGetValue(room, out var retired) && retired.Contains(partyId, StringComparer.Ordinal);

    private void RetireRemotePartyNoLock((string PodId, string ChannelId) room, string partyId)
    {
        if (!_retiredRemoteParties.TryGetValue(room, out var retired))
        {
            if (_retiredRemoteParties.Count >= MaxRetiredRemoteRooms)
            {
                _retiredRemoteParties.Remove(_retiredRemoteParties.Keys.First());
            }

            _retiredRemoteParties[room] = retired = new Queue<string>();
        }

        if (retired.Contains(partyId, StringComparer.Ordinal))
        {
            return;
        }

        retired.Enqueue(partyId);
        while (retired.Count > MaxRetiredPartiesPerRoom)
        {
            retired.Dequeue();
        }
    }

    public Task<IReadOnlyList<ListeningPartyAnnouncement>> ListDirectoryAsync(CancellationToken cancellationToken = default)
        => ListDirectoryCoreAsync(false, cancellationToken);

    public Task<IReadOnlyList<ListeningPartyAnnouncement>> RefreshDirectoryAsync(CancellationToken cancellationToken = default)
        => ListDirectoryCoreAsync(true, cancellationToken);

    private async Task<IReadOnlyList<ListeningPartyAnnouncement>> ListDirectoryCoreAsync(bool force, CancellationToken cancellationToken)
    {
        Task refreshTask;
        lock (_directoryRefreshLock)
        {
            var canForce = force && (_directoryLastForcedAt == default ||
                _timeProvider.GetUtcNow() - _directoryLastForcedAt >= TimeSpan.FromSeconds(2) ||
                _directoryRefreshTask is { IsFaulted: true });
            var refreshIsCurrent = !canForce && _directoryLastRefreshedAt != default
                && _timeProvider.GetUtcNow() - _directoryLastRefreshedAt < DirectoryRefreshInterval;
            if (_directoryRefreshTask is { IsCompleted: false })
            {
                refreshTask = _directoryRefreshTask;
            }
            else if (refreshIsCurrent)
            {
                refreshTask = Task.CompletedTask;
            }
            else
            {
                if (canForce)
                {
                    _directoryLastForcedAt = _timeProvider.GetUtcNow();
                }

                refreshTask = _directoryRefreshTask = RefreshDirectoryAndMarkAsync();
            }
        }

        await refreshTask.WaitAsync(cancellationToken).ConfigureAwait(false);

        var now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        return _directory.Values
            .Where(x => x.ExpiresAtUnixMs > now)
            .OrderByDescending(x => x.LastSeenUnixMs)
            .ToList();
    }

    private async Task RefreshDirectoryAndMarkAsync()
    {
        await RefreshDirectoryFromDhtAsync(CancellationToken.None).ConfigureAwait(false);

        lock (_directoryRefreshLock)
        {
            _directoryLastRefreshedAt = _timeProvider.GetUtcNow();
        }
    }

    public Task<ListeningPartyEvent> PublishAsync(ListeningPartyEvent partyEvent, CancellationToken cancellationToken = default)
        => PublishHostEventAsync(partyEvent, null, false, cancellationToken);

    public Task<ListeningPartyEvent> PublishHostEventAsync(
        ListeningPartyEvent partyEvent,
        string? hostSessionId,
        bool startHostSession,
        CancellationToken cancellationToken = default)
    {
        var podId = (partyEvent.PodId ?? string.Empty).Trim();
        var channelId = (partyEvent.ChannelId ?? string.Empty).Trim();
        return RunInPublicationQueueAsync(podId, channelId, async () =>
        {
            var normalizedSessionId = NormalizeHostSessionId(hostSessionId);
            ValidateHostSession((podId, channelId), partyEvent, normalizedSessionId, startHostSession);

            return await PublishCoreAsync(
                partyEvent,
                cancellationToken,
                published => CommitHostSession((podId, channelId), published, normalizedSessionId, startHostSession)).ConfigureAwait(false);
        }, cancellationToken);
    }

    public async Task RenewHostSessionAsync(
        string podId,
        string channelId,
        string partyId,
        string hostSessionId,
        CancellationToken cancellationToken = default)
    {
        podId = (podId ?? string.Empty).Trim();
        channelId = (channelId ?? string.Empty).Trim();
        partyId = (partyId ?? string.Empty).Trim();
        var normalizedSessionId = NormalizeHostSessionId(hostSessionId);
        if (string.IsNullOrWhiteSpace(partyId) || normalizedSessionId == null)
        {
            throw new ListeningPartyHostSessionConflictException("host_session_expired");
        }

        await RunInPublicationQueueAsync(podId, channelId, async () =>
        {
            var room = (podId, channelId);
            lock (_hostSessionsLock)
            {
                if (!_hostSessions.TryGetValue(room, out var lease) || lease.ExpiresAt <= _timeProvider.GetUtcNow())
                {
                    _hostSessions.Remove(room);
                    throw new ListeningPartyHostSessionConflictException("host_session_expired");
                }

                if (lease.HostSessionId != normalizedSessionId || lease.PartyId != partyId)
                {
                    throw new ListeningPartyHostSessionConflictException("host_session_replaced");
                }
            }

            if (!_states.TryGetValue(StateKey(podId, channelId), out var state) || state.PartyId != partyId)
            {
                throw new ListeningPartyHostSessionConflictException("host_session_expired");
            }

            if (state.Listed)
            {
                try
                {
                    await PublishAnnouncementAsync(state, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new ListeningPartyStorageException(exception);
                }
            }

            lock (_hostSessionsLock)
            {
                if (!_hostSessions.TryGetValue(room, out var lease) ||
                    lease.HostSessionId != normalizedSessionId || lease.PartyId != partyId)
                {
                    throw new ListeningPartyHostSessionConflictException("host_session_replaced");
                }

                _hostSessions[room] = lease with { ExpiresAt = _timeProvider.GetUtcNow().Add(HostSessionLeaseDuration) };
            }

            return true;
        }, cancellationToken);
    }

    private async Task<T> RunInPublicationQueueAsync<T>(
        string podId,
        string channelId,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var room = (podId, channelId);
        SemaphoreSlim gate;
        lock (_publicationLock)
        {
            if (_publications.TryGetValue(room, out var queue))
            {
                if (queue.Reservations >= MaxPublicationsPerRoom)
                {
                    throw new ListeningPartyCapacityException();
                }

                gate = queue.Gate;
                _publications[room] = (gate, queue.Reservations + 1);
            }
            else
            {
                if (_publications.Count >= MaxPublicationRooms)
                {
                    throw new ListeningPartyCapacityException();
                }

                gate = new SemaphoreSlim(1, 1);
                _publications.Add(room, (gate, 1));
            }
        }

        var acquired = false;
        try
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            if (acquired)
            {
                gate.Release();
            }

            lock (_publicationLock)
            {
                var queue = _publications[room];
                if (queue.Reservations == 1)
                {
                    _publications.Remove(room);
                    gate.Dispose();
                }
                else
                {
                    _publications[room] = (gate, queue.Reservations - 1);
                }
            }
        }
    }

    private void ValidateHostSession(
        (string PodId, string ChannelId) room,
        ListeningPartyEvent partyEvent,
        string? hostSessionId,
        bool startHostSession)
    {
        if (startHostSession && (hostSessionId == null ||
            string.Equals(partyEvent.Action?.Trim(), "stop", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("A new host session requires a session ID and a non-Stop event.", nameof(partyEvent));
        }

        if (startHostSession)
        {
            lock (_hostSessionsLock)
            {
                PruneExpiredHostSessions();
            }

            return;
        }

        lock (_hostSessionsLock)
        {
            if (!_hostSessions.TryGetValue(room, out var lease))
            {
                if (hostSessionId != null)
                {
                    if (IsCurrentPartyStop(room, partyEvent)) return;
                    throw new ListeningPartyHostSessionConflictException("host_session_expired");
                }

                return;
            }

            if (lease.ExpiresAt <= _timeProvider.GetUtcNow())
            {
                var expiredPartyId = (partyEvent.PartyId ?? string.Empty).Trim();
                var expiredSessionStop = IsStop(partyEvent) && lease.PartyId == expiredPartyId &&
                    (hostSessionId == null || lease.HostSessionId == hostSessionId);
                if (expiredSessionStop) return;

                _hostSessions.Remove(room);
                throw new ListeningPartyHostSessionConflictException("host_session_expired");
            }

            var requestedPartyId = (partyEvent.PartyId ?? string.Empty).Trim();
            var ownsSession = hostSessionId != null && lease.HostSessionId == hostSessionId && lease.PartyId == requestedPartyId;
            var legacyStopFromCurrentSnapshot = hostSessionId == null && IsStop(partyEvent) && lease.PartyId == requestedPartyId;
            if (!ownsSession && !legacyStopFromCurrentSnapshot)
            {
                throw new ListeningPartyHostSessionConflictException("host_session_replaced");
            }
        }
    }

    private bool IsCurrentPartyStop((string PodId, string ChannelId) room, ListeningPartyEvent partyEvent)
        => IsStop(partyEvent) && _states.TryGetValue(StateKey(room.PodId, room.ChannelId), out var current) &&
            current.PartyId == (partyEvent.PartyId ?? string.Empty).Trim();

    private static bool IsStop(ListeningPartyEvent partyEvent)
        => string.Equals(partyEvent.Action?.Trim(), "stop", StringComparison.OrdinalIgnoreCase);

    private void CommitHostSession(
        (string PodId, string ChannelId) room,
        ListeningPartyEvent published,
        string? hostSessionId,
        bool startHostSession)
    {
        lock (_hostSessionsLock)
        {
            if (published.Action == "stop")
            {
                if (_hostSessions.TryGetValue(room, out var current) && current.PartyId == published.PartyId &&
                    (hostSessionId == null || current.HostSessionId == hostSessionId))
                {
                    _hostSessions.Remove(room);
                }

                return;
            }

            if (startHostSession && hostSessionId != null)
            {
                _hostSessions[room] = new HostSessionLease(hostSessionId, published.PartyId, _timeProvider.GetUtcNow().Add(HostSessionLeaseDuration));
            }
            else if (hostSessionId != null && _hostSessions.TryGetValue(room, out var current) && current.HostSessionId == hostSessionId)
            {
                _hostSessions[room] = current with { PartyId = published.PartyId, ExpiresAt = _timeProvider.GetUtcNow().Add(HostSessionLeaseDuration) };
            }
        }
    }

    private static string? NormalizeHostSessionId(string? hostSessionId)
    {
        if (string.IsNullOrWhiteSpace(hostSessionId))
        {
            return null;
        }

        if (!Guid.TryParse(hostSessionId.Trim(), out var parsed) || parsed == Guid.Empty)
        {
            throw new ArgumentException("Host session ID must be a non-empty UUID.", nameof(hostSessionId));
        }

        return parsed.ToString("N");
    }

    private void PruneExpiredHostSessions()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var room in _hostSessions.Where(entry => entry.Value.ExpiresAt <= now).Select(entry => entry.Key).ToArray())
        {
            _hostSessions.Remove(room);
        }
    }

    private sealed record HostSessionLease(string HostSessionId, string PartyId, DateTimeOffset ExpiresAt);

    private async Task<ListeningPartyEvent> PublishCoreAsync(
        ListeningPartyEvent partyEvent,
        CancellationToken cancellationToken,
        Action<ListeningPartyEvent>? hostStateCommitted = null)
    {
        var normalized = Normalize(partyEvent);
        var key = StateKey(normalized.PodId, normalized.ChannelId);
        if (normalized.Action == "stop" && _states.TryGetValue(key, out var current))
        {
            normalized = normalized with { PartyId = current.PartyId };
        }

        var message = new PodMessage
        {
            MessageId = $"listen-{Guid.NewGuid():N}",
            PodId = normalized.PodId,
            ChannelId = normalized.ChannelId,
            SenderPeerId = normalized.HostPeerId,
            Body = JsonSerializer.Serialize(normalized, JsonOptions),
            TimestampUnixMs = normalized.ServerTimeUnixMs,
            Signature = string.Empty,
        };

        using var scope = _scopeFactory.CreateScope();
        var messageStorage = scope.ServiceProvider.GetRequiredService<IPodMessageStorage>();
        var pods = scope.ServiceProvider.GetRequiredService<IPodService>();
        if (await pods.GetChannelAsync(normalized.PodId, normalized.ChannelId, cancellationToken).ConfigureAwait(false) == null)
        {
            throw new ListeningPartyRoomNotFoundException();
        }

        if (!PodValidation.ValidateMessage(message).IsValid)
        {
            throw new ArgumentException("Listen-along metadata is invalid.", nameof(partyEvent));
        }

        if (!await messageStorage.StoreMessageAsync(normalized.PodId, normalized.ChannelId, message, cancellationToken).ConfigureAwait(false))
        {
            throw new ListeningPartyStorageException();
        }

        bool updateIndex;
        lock (_directoryStateLock)
        {
            PruneWithdrawnListings();
            if (_states.TryGetValue(key, out var previous) && previous.Listed &&
                (normalized.Action == "stop" || !normalized.Listed || previous.PartyId != normalized.PartyId))
            {
                WithdrawListing(previous.PartyId, key);
            }

            if (normalized.Action != "stop" && normalized.Listed)
            {
                _withdrawnListings.Remove(normalized.PartyId);
            }

            updateIndex = _pendingDirectoryWithdrawals.Contains(key);

            if (normalized.Action == "stop")
            {
                lock (_remoteStatesLock)
                {
                    _states.TryRemove(key, out _);
                    if (_remoteStates.Remove((normalized.PodId, normalized.ChannelId), out var previousRemote))
                    {
                        RetireRemotePartyNoLock((normalized.PodId, normalized.ChannelId), previousRemote.PartyId);
                    }
                }
            }
            else
            {
                lock (_remoteStatesLock)
                {
                    _states[key] = normalized;
                    if (_remoteStates.Remove((normalized.PodId, normalized.ChannelId), out var previousRemote))
                    {
                        RetireRemotePartyNoLock((normalized.PodId, normalized.ChannelId), previousRemote.PartyId);
                    }
                }
            }

            hostStateCommitted?.Invoke(normalized);
        }

        if (normalized.Action == "stop")
        {
            _nowPlaying.Clear();
        }
        else if (normalized.Action == "play" && !string.IsNullOrWhiteSpace(normalized.Artist) && !string.IsNullOrWhiteSpace(normalized.Title))
        {
            _nowPlaying.SetTrack(normalized.Artist, normalized.Title, normalized.Album);
        }

        // ADR-0025: Commit local playback feedback before bounded mesh delivery and directory work.
        await SendToSubscribersAsync(normalized, cancellationToken).ConfigureAwait(false);

        // Room delivery is independent of optional radio-directory availability.
        var routing = await _messageRouter.RouteListenAlongMessageAsync(message, cancellationToken).ConfigureAwait(false);
        if (!routing.Success)
        {
            var error = routing.ErrorMessage ?? "one or more peers rejected or did not acknowledge the state";
            _logger.LogWarning(
                "Failed to route listen-along message {MessageId}: {Error}; routed {SuccessCount}/{TargetCount}, failed peers: {FailedPeerIds}",
                routing.MessageId,
                error,
                routing.SuccessfullyRoutedCount,
                routing.TargetPeerCount,
                string.Join(", ", routing.FailedPeerIds ?? Array.Empty<string>()));
        }

        if (normalized.Action != "stop" && normalized.Listed)
        {
            await PublishAnnouncementAsync(normalized, cancellationToken);
        }
        else if (updateIndex)
        {
            await UpdateDirectoryIndexAsync(normalized.PartyId, add: false, cancellationToken);
        }

        lock (_directoryStateLock)
        {
            _pendingDirectoryWithdrawals.Remove(key);
        }

        return normalized;
    }

    private async Task PublishAnnouncementAsync(ListeningPartyEvent partyEvent, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var streamTicket = partyEvent.AllowMeshStreaming
            ? _streamTickets.Create(
                partyEvent.ContentId,
                $"listening-party:{partyEvent.PartyId}",
                TimeSpan.FromSeconds(AnnouncementTtlSeconds))
            : string.Empty;

        var announcement = new ListeningPartyAnnouncement
        {
            PartyId = partyEvent.PartyId,
            PodId = partyEvent.PodId,
            ChannelId = partyEvent.ChannelId,
            HostPeerId = partyEvent.HostPeerId,
            TransportUsername = _options.CurrentValue.Soulseek.Username ?? string.Empty,
            StreamTicket = streamTicket,
            Title = partyEvent.Title,
            Artist = partyEvent.Artist,
            Album = partyEvent.Album,
            ContentId = partyEvent.ContentId,
            Action = partyEvent.Action,
            PositionSeconds = partyEvent.PositionSeconds,
            Description = partyEvent.Description,
            Tags = partyEvent.Tags.ToList(),
            AllowMeshStreaming = partyEvent.AllowMeshStreaming,
            StreamPath = partyEvent.AllowMeshStreaming
                ? $"/api/v0/listening-party/radio/{Uri.EscapeDataString(partyEvent.PartyId)}/{Uri.EscapeDataString(partyEvent.ContentId)}?ticket={Uri.EscapeDataString(streamTicket)}"
                : string.Empty,
            StartedAtUnixMs = partyEvent.ServerTimeUnixMs,
            LastSeenUnixMs = now.ToUnixTimeMilliseconds(),
            ExpiresAtUnixMs = now.AddSeconds(AnnouncementTtlSeconds).ToUnixTimeMilliseconds(),
        };

        lock (_directoryStateLock)
        {
            _directory[announcement.PartyId] = announcement;
        }

        await _dht.PutAsync(AnnouncementKey(announcement.PartyId), Serialize(announcement), AnnouncementTtlSeconds, cancellationToken);
        await UpdateDirectoryIndexAsync(announcement.PartyId, add: true, cancellationToken);
    }

    private async Task RefreshDirectoryFromDhtAsync(CancellationToken cancellationToken)
    {
        var index = await GetAsync<ListeningPartyIndex>(DirectoryIndexKey, cancellationToken);
        if (index == null)
        {
            return;
        }

        var indexed = index.PartyIds.ToHashSet(StringComparer.Ordinal);
        lock (_directoryStateLock)
        {
            PruneWithdrawnListings();
            var local = _states.Values.Where(state => state.Listed).Select(state => state.PartyId).ToHashSet(StringComparer.Ordinal);
            foreach (var partyId in _directory.Keys.Where(partyId => !indexed.Contains(partyId) && !local.Contains(partyId)))
            {
                _directory.TryRemove(partyId, out _);
            }
        }

        foreach (var partyId in index.PartyIds)
        {
            var announcement = await GetAsync<ListeningPartyAnnouncement>(AnnouncementKey(partyId), cancellationToken);
            if (announcement != null)
            {
                lock (_directoryStateLock)
                {
                    // Recheck after the await: a publication may have withdrawn or renewed this listing.
                    if (!_withdrawnListings.ContainsKey(announcement.PartyId) &&
                        !_states.Values.Any(state => state.Listed && state.PartyId == announcement.PartyId))
                    {
                        _directory[announcement.PartyId] = announcement;
                    }
                }
            }
        }
    }

    private async Task UpdateDirectoryIndexAsync(string partyId, bool add, CancellationToken cancellationToken)
    {
        await _directoryIndexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var index = await GetAsync<ListeningPartyIndex>(DirectoryIndexKey, cancellationToken) ?? new ListeningPartyIndex();
            List<string> partyIds;
            lock (_directoryStateLock)
            {
                PruneWithdrawnListings();
                partyIds = index.PartyIds
                    .Where(id => !string.IsNullOrWhiteSpace(id) && !_withdrawnListings.ContainsKey(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            if (add && !partyIds.Contains(partyId, StringComparer.Ordinal))
            {
                partyIds.Add(partyId);
            }

            await _dht.PutAsync(
                DirectoryIndexKey,
                Serialize(new ListeningPartyIndex
                {
                    PartyIds = partyIds,
                    UpdatedAtUnixMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
                }),
                AnnouncementTtlSeconds,
                cancellationToken);
        }
        finally
        {
            _directoryIndexGate.Release();
        }
    }

    // Called under _directoryStateLock; keep withdrawals for the announcement lifetime
    // so an older in-flight DHT refresh cannot resurrect an explicitly removed listing.
    private void WithdrawListing(string partyId, string roomKey)
    {
        _withdrawnListings[partyId] = (roomKey, _timeProvider.GetUtcNow().AddSeconds(AnnouncementTtlSeconds).ToUnixTimeMilliseconds());
        _pendingDirectoryWithdrawals.Add(roomKey);
        _directory.TryRemove(partyId, out _);
    }

    public void Dispose()
    {
        _directoryIndexGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private void PruneWithdrawnListings()
    {
        var now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        foreach (var partyId in _withdrawnListings.Where(entry => entry.Value.ExpiresAt <= now).Select(entry => entry.Key).ToArray())
        {
            var roomKey = _withdrawnListings[partyId].RoomKey;
            _withdrawnListings.Remove(partyId);
            if (!_withdrawnListings.Values.Any(entry => entry.RoomKey == roomKey))
            {
                _pendingDirectoryWithdrawals.Remove(roomKey);
            }
        }
    }

    private ListeningPartyEvent Normalize(ListeningPartyEvent partyEvent)
    {
        var action = (partyEvent.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (action is not ("play" or "pause" or "seek" or "stop"))
        {
            throw new ArgumentException("Action must be play, pause, seek, or stop.", nameof(partyEvent));
        }

        var contentId = action == "stop" ? string.Empty : (partyEvent.ContentId ?? string.Empty).Trim();
        if (action != "stop" && string.IsNullOrWhiteSpace(contentId))
        {
            throw new ArgumentException("ContentId is required for listen-along playback events.", nameof(partyEvent));
        }

        return partyEvent with
        {
            PartyId = string.IsNullOrWhiteSpace(partyEvent.PartyId)
                ? $"party:{Guid.NewGuid():N}"
                : partyEvent.PartyId.Trim(),
            Kind = ListeningPartyEvent.KindName,
            PodId = (partyEvent.PodId ?? string.Empty).Trim(),
            ChannelId = (partyEvent.ChannelId ?? string.Empty).Trim(),
            HostPeerId = (partyEvent.HostPeerId ?? string.Empty).Trim(),
            Action = action,
            ContentId = contentId,
            Title = (partyEvent.Title ?? string.Empty).Trim(),
            Artist = (partyEvent.Artist ?? string.Empty).Trim(),
            Album = string.IsNullOrWhiteSpace(partyEvent.Album) ? null : partyEvent.Album.Trim(),
            PositionSeconds = double.IsFinite(partyEvent.PositionSeconds)
                ? Math.Max(0, partyEvent.PositionSeconds)
                : 0,
            ServerTimeUnixMs = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            Sequence = Interlocked.Increment(ref _sequence),
            Description = (partyEvent.Description ?? string.Empty).Trim(),
            Tags = partyEvent.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList(),
        };
    }

    private static string StateKey(string podId, string channelId)
    {
        return $"{podId?.Trim()}:{channelId?.Trim()}";
    }

    private static string AnnouncementKey(string partyId)
    {
        return $"slskdn:listening-party:party:{partyId}";
    }

    private async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var raw = await _dht.GetRawAsync(key, cancellationToken).ConfigureAwait(false);
        return raw == null ? default : JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    private static byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
    }
}

public sealed class ListeningPartyCapacityException : Exception
{
    public ListeningPartyCapacityException()
        : base("Too many room updates are pending.")
    {
    }
}

public sealed class ListeningPartyRoomNotFoundException : Exception
{
    public ListeningPartyRoomNotFoundException()
        : base("The listen-along room is unavailable.")
    {
    }
}

public sealed class ListeningPartyStorageException : Exception
{
    public ListeningPartyStorageException()
        : base("The room update could not be saved.")
    {
    }

    public ListeningPartyStorageException(Exception innerException)
        : base("The room update could not be saved.", innerException)
    {
    }
}

public sealed class ListeningPartyHostSessionConflictException : Exception
{
    public ListeningPartyHostSessionConflictException(string code)
        : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}
