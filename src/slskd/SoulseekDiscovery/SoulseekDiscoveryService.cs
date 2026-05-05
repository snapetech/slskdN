// <copyright file="SoulseekDiscoveryService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.SoulseekDiscovery;

using Microsoft.Extensions.Options;
using slskd.Mesh;
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
    public SoulseekDiscoveryService(ISoulseekClient client, IOptionsMonitor<MeshOptions>? meshOptions = null)
    {
        Client = client;
        MeshOptions = meshOptions;
    }

    private ISoulseekClient Client { get; }
    private IOptionsMonitor<MeshOptions>? MeshOptions { get; }

    public Task AddInterestAsync(string item, CancellationToken cancellationToken = default)
        => Client.AddInterestAsync(NormalizeItem(item), cancellationToken);

    public Task RemoveInterestAsync(string item, CancellationToken cancellationToken = default)
        => Client.RemoveInterestAsync(NormalizeItem(item), cancellationToken);

    public Task AddHatedInterestAsync(string item, CancellationToken cancellationToken = default)
        => Client.AddHatedInterestAsync(NormalizeItem(item), cancellationToken);

    public Task RemoveHatedInterestAsync(string item, CancellationToken cancellationToken = default)
        => Client.RemoveHatedInterestAsync(NormalizeItem(item), cancellationToken);

    public Task<RecommendationList> GetRecommendationsAsync(CancellationToken cancellationToken = default)
        => Client.GetRecommendationsAsync(cancellationToken);

    public Task<RecommendationList> GetGlobalRecommendationsAsync(CancellationToken cancellationToken = default)
        => Client.GetGlobalRecommendationsAsync(cancellationToken);

    public Task<UserInterests> GetUserInterestsAsync(string username, CancellationToken cancellationToken = default)
        => Client.GetUserInterestsAsync(NormalizeUsername(username), cancellationToken);

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
