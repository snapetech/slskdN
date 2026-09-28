// <copyright file="ListeningPartyService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.ListeningParty;

using System.Collections.Concurrent;
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

    private readonly IHubContext<ListeningPartyHub> _hub;
    private readonly IMeshDhtClient _dht;
    private readonly IPodMessageRouter _messageRouter;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NowPlayingService _nowPlaying;
    private readonly IStreamTicketService _streamTickets;
    private readonly ILogger<ListeningPartyService> _logger;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<Options> _options;
    private readonly ConcurrentDictionary<string, ListeningPartyEvent> _states = new();
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
    private readonly object _subscriptionsLock = new();
    private readonly Dictionary<(string ConnectionId, string PodId, string ChannelId), PartySubscription> _subscriptions = new();
    private const int MaxSubscriptions = 4096;
    private const int MaxSubscriptionsPerConnection = 16;

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
        _states.TryGetValue(StateKey(podId, channelId), out var state);
        return Task.FromResult(state);
    }

    public Task<ListeningPartyEvent?> GetStateByPartyIdAsync(string partyId, CancellationToken cancellationToken = default)
    {
        var normalizedPartyId = partyId?.Trim() ?? string.Empty;
        var state = _states.Values.FirstOrDefault(x => string.Equals(x.PartyId, normalizedPartyId, StringComparison.Ordinal));
        return Task.FromResult(state);
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

    public async Task<ListeningPartyEvent> PublishAsync(ListeningPartyEvent partyEvent, CancellationToken cancellationToken = default)
    {
        var room = ((partyEvent.PodId ?? string.Empty).Trim(), (partyEvent.ChannelId ?? string.Empty).Trim());
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
            return await PublishCoreAsync(partyEvent, cancellationToken).ConfigureAwait(false);
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

    private async Task<ListeningPartyEvent> PublishCoreAsync(ListeningPartyEvent partyEvent, CancellationToken cancellationToken)
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
                _states.TryRemove(key, out _);
            }
            else
            {
                _states[key] = normalized;
            }
        }

        if (normalized.Action == "stop")
        {
            _nowPlaying.Clear();
        }
        else if (normalized.Action == "play" && !string.IsNullOrWhiteSpace(normalized.Artist) && !string.IsNullOrWhiteSpace(normalized.Title))
        {
            _nowPlaying.SetTrack(normalized.Artist, normalized.Title, normalized.Album);
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

        var routing = await _messageRouter.RouteMessageAsync(message, cancellationToken);
        if (!routing.Success)
        {
            _logger.LogWarning("Failed to route listen-along message {MessageId}: {Error}", routing.MessageId, routing.ErrorMessage);
        }

        await SendToSubscribersAsync(normalized, cancellationToken).ConfigureAwait(false);

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
            ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
}
