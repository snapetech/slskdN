// <copyright file="SoulseekDiscoveryService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.SoulseekDiscovery;

using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Opinions;
using Soulseek;

public interface ISoulseekDiscoveryService
{
    Task AddInterestAsync(string item, CancellationToken cancellationToken = default);

    Task RemoveInterestAsync(string item, CancellationToken cancellationToken = default);

    Task AddHatedInterestAsync(string item, CancellationToken cancellationToken = default);

    Task RemoveHatedInterestAsync(string item, CancellationToken cancellationToken = default);

    Task<RecommendationList> GetRecommendationsAsync(CancellationToken cancellationToken = default);

    Task<RecommendationList> GetGlobalRecommendationsAsync(CancellationToken cancellationToken = default);

    Task<UserInterests> GetUserInterestsAsync(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SimilarUser>> GetSimilarUsersAsync(CancellationToken cancellationToken = default);

    Task AddMeshRendezvousInterestAsync(CancellationToken cancellationToken = default);

    Task RemoveMeshRendezvousInterestAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SimilarUser>> GetMeshRendezvousUsersAsync(CancellationToken cancellationToken = default);

    Task<MeshRendezvousResult> DiscoverMeshRendezvousAsync(CancellationToken cancellationToken = default);

    IReadOnlyCollection<PeerCapabilityRecord> GetPeerCapabilityRecords();

    Task<ItemRecommendations> GetItemRecommendationsAsync(string item, CancellationToken cancellationToken = default);

    Task<ItemSimilarUsers> GetItemSimilarUsersAsync(string item, CancellationToken cancellationToken = default);
}

public sealed class SoulseekDiscoveryService : ISoulseekDiscoveryService
{
    public SoulseekDiscoveryService(
        ISoulseekClient client,
        IOptionsMonitor<MeshOptions>? meshOptions = null,
        IOpinionService? opinionService = null)
    {
        Client = client;
        MeshOptions = meshOptions;
        OpinionService = opinionService;
    }

    private ISoulseekClient Client { get; }
    private IOptionsMonitor<MeshOptions>? MeshOptions { get; }
    private IOpinionService? OpinionService { get; }

    public Task AddInterestAsync(string item, CancellationToken cancellationToken = default)
        => SendAndRecordLocalInterestAsync(NormalizeItem(item), OpinionKind.Like, cancellationToken);

    public Task RemoveInterestAsync(string item, CancellationToken cancellationToken = default)
        => RemoveAndRecordLocalInterestAsync(NormalizeItem(item), OpinionKind.Like, cancellationToken);

    public Task AddHatedInterestAsync(string item, CancellationToken cancellationToken = default)
        => SendAndRecordLocalInterestAsync(NormalizeItem(item), OpinionKind.Hate, cancellationToken);

    public Task RemoveHatedInterestAsync(string item, CancellationToken cancellationToken = default)
        => RemoveAndRecordLocalInterestAsync(NormalizeItem(item), OpinionKind.Hate, cancellationToken);

    public Task<RecommendationList> GetRecommendationsAsync(CancellationToken cancellationToken = default)
        => Client.GetRecommendationsAsync(cancellationToken);

    public Task<RecommendationList> GetGlobalRecommendationsAsync(CancellationToken cancellationToken = default)
        => Client.GetGlobalRecommendationsAsync(cancellationToken);

    public async Task<UserInterests> GetUserInterestsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        var interests = await Client.GetUserInterestsAsync(normalizedUsername, cancellationToken).ConfigureAwait(false);

        if (OpinionService != null)
        {
            await OpinionService.ImportSoulseekInterestsAsync(normalizedUsername, interests, cancellationToken).ConfigureAwait(false);
        }

        return interests;
    }

    public Task<IReadOnlyCollection<SimilarUser>> GetSimilarUsersAsync(CancellationToken cancellationToken = default)
        => Client.GetSimilarUsersAsync(cancellationToken);

    public Task AddMeshRendezvousInterestAsync(CancellationToken cancellationToken = default)
        => CreateMeshRendezvousService(probePeerCapabilities: false).RegisterAsync(cancellationToken);

    public Task RemoveMeshRendezvousInterestAsync(CancellationToken cancellationToken = default)
        => CreateMeshRendezvousService(probePeerCapabilities: false).UnregisterAsync(cancellationToken);

    public Task<IReadOnlyCollection<SimilarUser>> GetMeshRendezvousUsersAsync(CancellationToken cancellationToken = default)
        => Client.GetMeshRendezvousUsersAsync(cancellationToken);

    public Task<MeshRendezvousResult> DiscoverMeshRendezvousAsync(CancellationToken cancellationToken = default)
        => CreateMeshRendezvousService(
            probePeerCapabilities: MeshOptions?.CurrentValue.ProbeSoulseekRendezvousCapabilities ?? true)
        .DiscoverAsync(cancellationToken);

    public IReadOnlyCollection<PeerCapabilityRecord> GetPeerCapabilityRecords()
        => Client.PeerCapabilities.Records;

    public Task<ItemRecommendations> GetItemRecommendationsAsync(string item, CancellationToken cancellationToken = default)
        => Client.GetItemRecommendationsAsync(NormalizeItem(item), cancellationToken);

    public Task<ItemSimilarUsers> GetItemSimilarUsersAsync(string item, CancellationToken cancellationToken = default)
        => Client.GetItemSimilarUsersAsync(NormalizeItem(item), cancellationToken);

    private async Task SendAndRecordLocalInterestAsync(string item, OpinionKind kind, CancellationToken cancellationToken)
    {
        if (kind == OpinionKind.Hate)
        {
            await Client.AddHatedInterestAsync(item, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Client.AddInterestAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (OpinionService == null)
        {
            return;
        }

        var issuer = string.IsNullOrWhiteSpace(Client.Username) ? "local:soulseek" : $"soulseek:{Client.Username.Trim()}";
        var subject = OpinionSubject.FromInterestItem(item);
        await OpinionService.SubmitAsync(new OpinionRecord
        {
            Issuer = issuer,
            SubjectType = subject.Type,
            SubjectId = subject.Id,
            Kind = kind,
            Strength = kind == OpinionKind.Hate ? -0.5 : 0.5,
            Confidence = 0.5,
            Scope = "soulseek-public",
            Source = "soulseek-interest",
            Reason = "Mirrored from the local native Soulseek interest list.",
            Evidence =
            {
                new OpinionEvidence
                {
                    Type = "soulseek-interest",
                    Value = item,
                },
            },
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveAndRecordLocalInterestAsync(string item, OpinionKind kind, CancellationToken cancellationToken)
    {
        if (kind == OpinionKind.Hate)
        {
            await Client.RemoveHatedInterestAsync(item, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Client.RemoveInterestAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (OpinionService == null)
        {
            return;
        }

        var issuer = string.IsNullOrWhiteSpace(Client.Username) ? "local:soulseek" : $"soulseek:{Client.Username.Trim()}";
        var subject = OpinionSubject.FromInterestItem(item);
        var existing = await OpinionService.ListAsync(new OpinionQuery
        {
            Issuer = issuer,
            SubjectType = subject.Type,
            SubjectId = subject.Id,
            Kind = kind,
            Scope = "soulseek-public",
            Source = "soulseek-interest",
            Limit = 10,
        }, cancellationToken).ConfigureAwait(false);

        foreach (var record in existing)
        {
            await OpinionService.RemoveAsync(record.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string NormalizeItem(string item)
    {
        item = item?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(item))
        {
            throw new ArgumentException("item is required", nameof(item));
        }

        return item;
    }

    private static string NormalizeUsername(string username)
    {
        username = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("username is required", nameof(username));
        }

        return username;
    }

    private MeshRendezvousService CreateMeshRendezvousService(bool probePeerCapabilities)
        => new MeshRendezvousService(
            Client,
            new MeshRendezvousOptions(
                SoulseekClient.MeshRendezvousInterestTag,
                probePeerCapabilities));
}
