// <copyright file="GoldStarClubService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.PodCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soulseek;

/// <summary>
/// Manages the Gold Star Club pod - auto-join for the first 250 explicitly opted-in network members.
/// Once the pod reaches 250 members, no new members can be added, even if people leave.
/// </summary>
public interface IGoldStarClubService
{
    /// <summary>
    /// Gets the Gold Star Club pod ID.
    /// </summary>
    string GoldStarClubPodId { get; }

    /// <summary>
    /// Gets the maximum membership limit (250).
    /// </summary>
    int MaxMembership { get; }

    /// <summary>
    /// Gets whether the Gold Star Club is still accepting new members.
    /// </summary>
    Task<bool> IsAcceptingMembersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current membership count.
    /// </summary>
    Task<int> GetMembershipCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Attempts to auto-join the current user to the Gold Star Club if eligible.
    /// </summary>
    Task<bool> TryAutoJoinAsync(string peerId, CancellationToken ct = default);

    /// <summary>
    /// Records that the local user intentionally revoked Gold Star Club membership.
    /// </summary>
    Task RecordRevocationAsync(string peerId, CancellationToken ct = default);

    /// <summary>
    /// Ensures the Gold Star Club pod exists (creates it if it doesn't) after
    /// explicit opt-in; otherwise this is a no-op.
    /// </summary>
    Task EnsurePodExistsAsync(CancellationToken ct = default);
}

/// <summary>
/// Implements Gold Star Club auto-join logic.
/// </summary>
public sealed class GoldStarClubService : BackgroundService, IGoldStarClubService
{
    public const string GoldStarClubPodId = "pod:901d57a2c1bb4e5d90d57a2c1bb4e5d0";

    private const string GoldStarClubGeneralChannelId = "gold-star-club-general";
    public const int MaxMembership = 250;

    // Gold Star Club is deliberately opt-in. Only the exact value "true" enables
    // reserved-pod creation, publication, or automatic enrollment.
    // Leaving the pod later records a local revocation marker so an explicitly
    // enabled node does not silently rejoin that node.
    /// <summary>
    /// Gets the environment variable used for explicit Gold Star Club opt-in.
    /// </summary>
    public const string AutoJoinEnvironmentVariable = "SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN";
    private const string RevocationFileName = "gold-star-club.revoked";

    private readonly IPodService podService;
    private readonly ISoulseekClient soulseekClient;
    private readonly ILogger<GoldStarClubService> logger;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool podInitialized;
    private bool? isAcceptingMembers; // null = not checked yet, true/false = cached result
    private DateTime lastConnectionWaitLogUtc = DateTime.MinValue;

    /// <summary>
    /// Gets a value indicating whether Gold Star Club has an exact positive opt-in.
    /// </summary>
    public static bool IsOptedIn()
    {
        var value = Environment.GetEnvironmentVariable(AutoJoinEnvironmentVariable);
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string RevocationFilePath =>
        System.IO.Path.Combine(
            string.IsNullOrWhiteSpace(global::slskd.Program.AppDirectory) ? global::slskd.Program.DefaultAppDirectory : global::slskd.Program.AppDirectory,
            RevocationFileName);

    private static bool IsRevokedLocally() => System.IO.File.Exists(RevocationFilePath);

    public GoldStarClubService(
        IPodService podService,
        ISoulseekClient soulseekClient,
        ILogger<GoldStarClubService> logger)
    {
        this.podService = podService;
        this.soulseekClient = soulseekClient;
        this.logger = logger;
    }

    string IGoldStarClubService.GoldStarClubPodId => GoldStarClubPodId;
    int IGoldStarClubService.MaxMembership => MaxMembership;

    public async Task<bool> IsAcceptingMembersAsync(CancellationToken ct = default)
    {
        if (!IsOptedIn() || IsRevokedLocally())
        {
            return false;
        }

        // If we've already checked and cached the result, return it
        if (isAcceptingMembers.HasValue)
        {
            return isAcceptingMembers.Value;
        }

        // Ensure pod exists first
        await EnsurePodExistsAsync(ct);

        // Check current membership count
        var count = await GetMembershipCountAsync(ct);
        var accepting = count < MaxMembership;

        // Cache the result
        isAcceptingMembers = accepting;

        if (!accepting)
        {
            logger.LogInformation("[GoldStarClub] Membership limit reached ({Count}/{Max}). No new members will be accepted.", count, MaxMembership);
        }

        return accepting;
    }

    public async Task<int> GetMembershipCountAsync(CancellationToken ct = default)
    {
        if (!IsOptedIn() || IsRevokedLocally())
        {
            return 0;
        }

        await EnsurePodExistsAsync(ct);
        var members = await podService.GetMembersAsync(GoldStarClubPodId, ct);
        return members.Count;
    }

    public async Task<bool> TryAutoJoinAsync(string peerId, CancellationToken ct = default)
    {
        if (!IsOptedIn())
        {
            logger.LogDebug(
                "[GoldStarClub] Auto-join disabled because {EnvVar} is not exactly 'true'",
                AutoJoinEnvironmentVariable);
            return false;
        }

        if (IsRevokedLocally())
        {
            logger.LogInformation("[GoldStarClub] Auto-join skipped because this node has revoked Gold Star Club membership");
            return false;
        }

        if (string.IsNullOrWhiteSpace(peerId))
        {
            logger.LogWarning("[GoldStarClub] Cannot auto-join: peerId is null or empty");
            return false;
        }

        // Ensure pod exists
        await EnsurePodExistsAsync(ct);

        // Check if already a member
        var members = await podService.GetMembersAsync(GoldStarClubPodId, ct);
        if (members.Any(m => m.PeerId == peerId))
        {
            logger.LogDebug("[GoldStarClub] Peer {PeerId} is already a member", peerId);
            return true; // Already a member
        }

        // Check if we're still accepting members
        var accepting = await IsAcceptingMembersAsync(ct);
        if (!accepting)
        {
            logger.LogInformation("[GoldStarClub] Cannot auto-join {PeerId}: membership limit reached", peerId);
            return false;
        }

        // Check current count before joining (race condition protection)
        var currentCount = members.Count;
        if (currentCount >= MaxMembership)
        {
            logger.LogInformation("[GoldStarClub] Cannot auto-join {PeerId}: membership limit reached (current: {Count})", peerId, currentCount);
            isAcceptingMembers = false; // Update cache
            return false;
        }

        // Attempt to join
        var member = new PodMember
        {
            PeerId = peerId,
            Role = "member"
        };

        var joined = await podService.JoinAsync(GoldStarClubPodId, member, ct);

        if (joined)
        {
            var newCount = await GetMembershipCountAsync(ct);
            logger.LogInformation("[GoldStarClub] ✓ Auto-joined {PeerId} to Gold Star Club ({Count}/{Max})", peerId, newCount, MaxMembership);

            // If we just hit the limit, update cache
            if (newCount >= MaxMembership)
            {
                isAcceptingMembers = false;
                logger.LogInformation("[GoldStarClub] 🎉 Gold Star Club is now full! No new members will be accepted.");
            }
        }
        else
        {
            logger.LogWarning("[GoldStarClub] Failed to auto-join {PeerId}", peerId);
        }

        return joined;
    }

    public async Task EnsurePodExistsAsync(CancellationToken ct = default)
    {
        if (!IsOptedIn())
        {
            logger.LogDebug(
                "[GoldStarClub] Reserved pod creation disabled because {EnvVar} is not exactly 'true'",
                AutoJoinEnvironmentVariable);
            return;
        }

        if (IsRevokedLocally())
        {
            logger.LogDebug("[GoldStarClub] Reserved pod creation skipped because this node revoked membership");
            return;
        }

        if (podInitialized)
        {
            return; // Already checked
        }

        await initializationLock.WaitAsync(ct);
        try
        {
            if (podInitialized)
            {
                return; // Double-check after acquiring lock
            }

            // Check if pod already exists
            var existingPod = await podService.GetPodAsync(GoldStarClubPodId, ct);
            if (existingPod != null)
            {
                logger.LogDebug("[GoldStarClub] Pod already exists");
                podInitialized = true;
                return;
            }

            // Create the Gold Star Club pod
            var pod = new Pod
            {
                PodId = GoldStarClubPodId,
                Name = "Gold Star Club ⭐",
                Visibility = PodVisibility.Listed,
                Tags = new List<string> { "gold-star", "first-250", "realm-governance", "testing" },
                Channels = new List<PodChannel>
                {
                    new PodChannel
                    {
                        ChannelId = GoldStarClubGeneralChannelId,
                        Kind = PodChannelKind.General,
                        Name = "General"
                    }
                }
            };

            await podService.CreateAsync(pod, ct);
            logger.LogInformation("[GoldStarClub] Created Gold Star Club pod (max {Max} members)", MaxMembership);

            podInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public Task RecordRevocationAsync(string peerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(peerId))
        {
            return Task.CompletedTask;
        }

        var path = RevocationFilePath;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        return System.IO.File.WriteAllTextAsync(
            path,
            $"revoked_by={peerId.Trim()}{Environment.NewLine}revoked_at={DateTimeOffset.UtcNow:O}{Environment.NewLine}",
            ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Critical: never block host startup (BackgroundService.StartAsync runs until first await)
        await Task.Yield();

        // Best-effort auto-join must never take down the host. With the default
        // BackgroundServiceExceptionBehavior.StopHost, any exception escaping ExecuteAsync
        // (e.g. a transient pod-store/DB error while ensuring the pod) would stop the whole
        // application, so contain everything except genuine shutdown cancellation.
        try
        {
            if (!IsOptedIn())
            {
                logger.LogInformation(
                    "[GoldStarClub] Opt-in is absent: {EnvVar} is not exactly 'true'. No pod will be created, published, or joined.",
                    AutoJoinEnvironmentVariable);
                return;
            }

            if (IsRevokedLocally())
            {
                logger.LogInformation("[GoldStarClub] No pod will be created, published, or joined because this node revoked membership.");
                return;
            }

            await EnsurePodExistsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var username = await WaitForConnectionAsync(stoppingToken);
                if (string.IsNullOrWhiteSpace(username))
                {
                    return;
                }

                await TryAutoJoinAsync(username, stoppingToken);
                return;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GoldStarClub] Auto-join background service failed; the host will continue running.");
        }
    }

    private async Task<string?> WaitForConnectionAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var username = soulseekClient.Username;
            if (!string.IsNullOrWhiteSpace(username))
            {
                logger.LogDebug("[GoldStarClub] Soulseek client connected");
                return username;
            }

            var now = DateTime.UtcNow;
            if (now - lastConnectionWaitLogUtc >= TimeSpan.FromSeconds(30))
            {
                logger.LogInformation("[GoldStarClub] Waiting for Soulseek login before auto-join");
                lastConnectionWaitLogUtc = now;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        return null;
    }
}
