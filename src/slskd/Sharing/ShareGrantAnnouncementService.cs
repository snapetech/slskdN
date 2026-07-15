// <copyright file="ShareGrantAnnouncementService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soulseek;

/// <summary>
/// Receives share-grant announcements via private messages and ingests them into the local sharing DB
/// so "Shared with Me" can function cross-node.
/// </summary>
public sealed class ShareGrantAnnouncementService : IDisposable
{
    private const string Prefix = "SHAREGRANT:";

    private readonly IDbContextFactory<CollectionsDbContext> _factory;
    private readonly ILogger<ShareGrantAnnouncementService> _log;
    private readonly IOptionsMonitor<slskd.Options> _options;
    private readonly ISoulseekClient? _soulseekClient;
    private bool _disposed;

    public ShareGrantAnnouncementService(
        IDbContextFactory<CollectionsDbContext> factory,
        ILogger<ShareGrantAnnouncementService> log,
        IOptionsMonitor<slskd.Options> options,
        ISoulseekClient? soulseekClient = null)
    {
        _factory = factory;
        _log = log;
        _options = options;
        _soulseekClient = soulseekClient;

        if (_soulseekClient != null)
        {
            _soulseekClient.PrivateMessageReceived += OnPrivateMessageReceived;
        }
    }

    private void OnPrivateMessageReceived(object? sender, PrivateMessageReceivedEventArgs e)
    {
        if (!e.Message.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _log.LogInformation("[ShareGrantInbox] Received SHAREGRANT message from {User}", e.Username);

        _ = ObserveBackgroundTaskAsync(
            Task.Run(() => HandleAnnouncementAsync(e.Message.Substring(Prefix.Length), e.Username, CancellationToken.None), CancellationToken.None),
            e.Username);
    }

    private async Task HandleAnnouncementAsync(string payload, string senderUsername, CancellationToken ct)
    {
        if (!_options.CurrentValue.Feature.CollectionsSharing)
        {
            return;
        }

        ShareGrantAnnouncement? msg;
        try
        {
            msg = JsonSerializer.Deserialize<ShareGrantAnnouncement>(payload);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[ShareGrantInbox] Invalid announcement JSON");
            return;
        }

        if (msg == null)
        {
            return;
        }

        await IngestAsync(msg, senderUsername, ct).ConfigureAwait(false);
    }

    public Task IngestAsync(ShareGrantAnnouncement msg, CancellationToken ct)
    {
        return IngestAsync(msg, senderUsername: null, webAudienceId: null, ct);
    }

    public Task IngestAsync(ShareGrantAnnouncement msg, string? senderUsername, CancellationToken ct)
    {
        return IngestAsync(msg, senderUsername, webAudienceId: null, ct);
    }

    public Task IngestForWebAccountAsync(ShareGrantAnnouncement msg, string webAudienceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webAudienceId))
        {
            throw new ArgumentException("Web audience ID is required.", nameof(webAudienceId));
        }

        return IngestAsync(msg, senderUsername: null, webAudienceId.Trim(), ct);
    }

    private async Task IngestAsync(ShareGrantAnnouncement msg, string? senderUsername, string? webAudienceId, CancellationToken ct)
    {
        if (!_options.CurrentValue.Feature.CollectionsSharing)
        {
            return;
        }

        if (msg.CollectionId == Guid.Empty || msg.ShareGrantId == Guid.Empty)
        {
            return;
        }

        var localNetworkUserId = _options.CurrentValue.Soulseek.Username ?? string.Empty;

        // E2E-only network routing fallback; this value never establishes local web-resource ownership.
        if (string.IsNullOrWhiteSpace(localNetworkUserId) && Environment.GetEnvironmentVariable("SLSKDN_E2E_SHARE_ANNOUNCE") == "1" && !string.IsNullOrWhiteSpace(msg.RecipientUserId))
        {
            localNetworkUserId = msg.RecipientUserId;
        }

        if (string.IsNullOrWhiteSpace(localNetworkUserId))
        {
            return;
        }

        // Only ingest if this message is intended for the current user.
        if (!string.Equals(msg.RecipientUserId, localNetworkUserId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var ownerUserId = msg.OwnerUserId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(senderUsername) &&
            !string.Equals(ownerUserId, senderUsername, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("[ShareGrantInbox] Rejected announcement from {Sender}: claimed owner {OwnerUserId} did not match sender", senderUsername, ownerUserId);
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Upsert collection (owner is remote user)
        var c = await db.Collections.FirstOrDefaultAsync(x => x.Id == msg.CollectionId, ct).ConfigureAwait(false);
        if (c == null)
        {
            c = new Collection
            {
                Id = msg.CollectionId,
                Title = msg.CollectionTitle ?? "Untitled",
                Description = msg.CollectionDescription,
                Type = string.IsNullOrWhiteSpace(msg.CollectionType) ? CollectionType.ShareList : msg.CollectionType!,
                OwnerUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Collections.Add(c);
        }
        else
        {
            c.Title = msg.CollectionTitle ?? c.Title;
            c.Description = msg.CollectionDescription;
            c.Type = string.IsNullOrWhiteSpace(msg.CollectionType) ? c.Type : msg.CollectionType!;
            c.OwnerUserId = string.IsNullOrWhiteSpace(ownerUserId) ? c.OwnerUserId : ownerUserId;
            c.UpdatedAt = DateTime.UtcNow;
        }

        // Replace items (small lists; simplest and deterministic)
        var existingItems = await db.CollectionItems.Where(x => x.CollectionId == msg.CollectionId).ToListAsync(ct).ConfigureAwait(false);
        if (existingItems.Count > 0)
        {
            db.CollectionItems.RemoveRange(existingItems);
        }

        var items = msg.Items ?? new List<ShareGrantAnnouncementItem>();
        foreach (var (item, index) in items.Select((value, i) => (value, i)))
        {
            if (string.IsNullOrWhiteSpace(item.ContentId)) continue;
            db.CollectionItems.Add(new CollectionItem
            {
                Id = Guid.NewGuid(),
                CollectionId = msg.CollectionId,
                Ordinal = item.Ordinal ?? index,
                ContentId = item.ContentId!,
                MediaKind = item.MediaKind,
            });
        }

        // Imported network grants retain their Soulseek recipient separately and are never
        // assigned to an authenticated web account implicitly.
        var audienceId = webAudienceId ?? $"network:{localNetworkUserId}";
        var g = await db.ShareGrants.FirstOrDefaultAsync(x => x.Id == msg.ShareGrantId, ct).ConfigureAwait(false);
        if (g == null)
        {
            g = new ShareGrant
            {
                Id = msg.ShareGrantId,
                CollectionId = msg.CollectionId,
                AudienceType = AudienceTypes.User,
                AudienceId = audienceId,
                AudiencePeerId = localNetworkUserId,
                AllowStream = msg.AllowStream,
                AllowDownload = msg.AllowDownload,
                AllowReshare = msg.AllowReshare,
                ExpiryUtc = msg.ExpiryUtc,
                MaxConcurrentStreams = msg.MaxConcurrentStreams <= 0 ? 1 : msg.MaxConcurrentStreams,
                MaxBitrateKbps = msg.MaxBitrateKbps,
                OwnerEndpoint = msg.OwnerEndpoint,
                ShareToken = msg.Token,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.ShareGrants.Add(g);
        }
        else
        {
            g.CollectionId = msg.CollectionId;
            g.AudienceType = AudienceTypes.User;
            g.AudienceId = audienceId;
            g.AudiencePeerId = localNetworkUserId;
            g.AllowStream = msg.AllowStream;
            g.AllowDownload = msg.AllowDownload;
            g.AllowReshare = msg.AllowReshare;
            g.ExpiryUtc = msg.ExpiryUtc;
            g.MaxConcurrentStreams = msg.MaxConcurrentStreams <= 0 ? 1 : msg.MaxConcurrentStreams;
            g.MaxBitrateKbps = msg.MaxBitrateKbps;
            g.OwnerEndpoint = msg.OwnerEndpoint;
            g.ShareToken = msg.Token;
            g.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        _log.LogInformation("[ShareGrantInbox] Ingested incoming share {ShareId} for collection {CollectionId}", msg.ShareGrantId, msg.CollectionId);
    }

    private async Task ObserveBackgroundTaskAsync(Task task, string username)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[ShareGrantInbox] Failed to handle announcement from {User}", username);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_soulseekClient != null)
        {
            _soulseekClient.PrivateMessageReceived -= OnPrivateMessageReceived;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public sealed class ShareGrantAnnouncement
{
    public Guid ShareGrantId { get; set; }
    public Guid CollectionId { get; set; }
    public string? CollectionTitle { get; set; }
    public string? CollectionDescription { get; set; }
    public string? CollectionType { get; set; }
    public string? OwnerUserId { get; set; }
    public string? OwnerEndpoint { get; set; }
    public string? Token { get; set; }
    public string? RecipientUserId { get; set; }

    public bool AllowStream { get; set; } = true;
    public bool AllowDownload { get; set; } = true;
    public bool AllowReshare { get; set; }
    public DateTime? ExpiryUtc { get; set; }
    public int MaxConcurrentStreams { get; set; } = 1;
    public int? MaxBitrateKbps { get; set; }

    public List<ShareGrantAnnouncementItem>? Items { get; set; }
}

public sealed class ShareGrantAnnouncementItem
{
    public int? Ordinal { get; set; }
    public string? ContentId { get; set; }
    public string? MediaKind { get; set; }
}
