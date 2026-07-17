// <copyright file="TasteRecommendationService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.SocialFederation;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using slskd.Common.Security;
using slskd.DiscoveryGraph;
using slskd.Integrations.MusicBrainz.Radar;
using slskd.SoulseekDiscovery;
using slskd.Wishlist;

public sealed class TasteRecommendationService : ITasteRecommendationService
{
    private const int MaxInboxActivities = 250;
    private const int MaxRecommendationLimit = 100;
    private const int TextWorkKeyBufferLength = 1024;

    private readonly IActivityPubInboxStore _inboxStore;
    private readonly IActivityPubRelationshipStore _relationshipStore;
    private readonly IDiscoveryGraphService? _discoveryGraphService;
    private readonly IWishlistService? _wishlistService;
    private readonly IArtistReleaseRadarService? _artistRadarService;
    private readonly ISoulseekDiscoveryService? _soulseekDiscoveryService;
    private readonly ISoulseekSafetyLimiter? _soulseekSafetyLimiter;
    private readonly ILogger<TasteRecommendationService> _logger;

    public TasteRecommendationService(
        IActivityPubInboxStore inboxStore,
        IActivityPubRelationshipStore relationshipStore,
        ILogger<TasteRecommendationService> logger)
        : this(inboxStore, relationshipStore, null, null, null, logger)
    {
    }

    public TasteRecommendationService(
        IActivityPubInboxStore inboxStore,
        IActivityPubRelationshipStore relationshipStore,
        IDiscoveryGraphService? discoveryGraphService,
        IWishlistService? wishlistService,
        IArtistReleaseRadarService? artistRadarService,
        ILogger<TasteRecommendationService> logger)
        : this(inboxStore, relationshipStore, discoveryGraphService, wishlistService, artistRadarService, null, null, logger)
    {
    }

    public TasteRecommendationService(
        IActivityPubInboxStore inboxStore,
        IActivityPubRelationshipStore relationshipStore,
        IDiscoveryGraphService? discoveryGraphService,
        IWishlistService? wishlistService,
        IArtistReleaseRadarService? artistRadarService,
        ISoulseekDiscoveryService? soulseekDiscoveryService,
        ISoulseekSafetyLimiter? soulseekSafetyLimiter,
        ILogger<TasteRecommendationService> logger)
    {
        _inboxStore = inboxStore;
        _relationshipStore = relationshipStore;
        _discoveryGraphService = discoveryGraphService;
        _wishlistService = wishlistService;
        _artistRadarService = artistRadarService;
        _soulseekDiscoveryService = soulseekDiscoveryService;
        _soulseekSafetyLimiter = soulseekSafetyLimiter;
        _logger = logger;
    }

    public async Task<TasteRecommendationResult> GetRecommendationsAsync(
        TasteRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit <= 0 ? 20 : request.Limit, 1, MaxRecommendationLimit);
        var minimumTrustedSources = Math.Max(2, request.MinimumTrustedSources);
        var trustedActors = await _relationshipStore
            .GetFollowingAsync("music", MaxInboxActivities, cancellationToken)
            .ConfigureAwait(false);
        var trustedActorSet = trustedActors.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = await _inboxStore
            .GetActivitiesAsync("music", MaxInboxActivities, cancellationToken)
            .ConfigureAwait(false);
        var observations = new List<FederatedWorkRefObservation>();

        foreach (var entry in entries)
        {
            if (!trustedActorSet.Contains(entry.RemoteActor))
            {
                continue;
            }

            observations.AddRange(ExtractObservations(entry));
        }

        if (request.IncludeSoulseekRecommendations)
        {
            observations.AddRange(await GetSoulseekRecommendationObservationsAsync(cancellationToken).ConfigureAwait(false));
        }

        var result = BuildRecommendations(observations, trustedActorSet.Count, minimumTrustedSources, limit, request.IncludeSourceActors);
        if (request.IncludeGraphEvidence && _discoveryGraphService != null)
        {
            await AddGraphEvidenceAsync(result.Recommendations, cancellationToken).ConfigureAwait(false);
            result.Recommendations = result.Recommendations
                .OrderByDescending(recommendation => recommendation.Score)
                .ThenBy(recommendation => recommendation.WorkRef.Creator ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(recommendation => recommendation.WorkRef.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return result;
    }

    private async Task<IReadOnlyCollection<FederatedWorkRefObservation>> GetSoulseekRecommendationObservationsAsync(CancellationToken cancellationToken)
    {
        if (_soulseekDiscoveryService == null || _soulseekSafetyLimiter == null)
        {
            return Array.Empty<FederatedWorkRefObservation>();
        }

        if (!_soulseekSafetyLimiter.TryConsumeSearch("taste-recommendations-soulseek"))
        {
            _logger.LogWarning("[TasteRecommendations] Soulseek native recommendation lookup rejected by safety limiter");
            return Array.Empty<FederatedWorkRefObservation>();
        }

        try
        {
            var recommendations = await _soulseekDiscoveryService.GetRecommendationsAsync(cancellationToken).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;

            return recommendations.Recommendations
                .Where(recommendation => !string.IsNullOrWhiteSpace(recommendation.Item))
                .Select(recommendation => new FederatedWorkRefObservation
                {
                    ActorName = "music",
                    RemoteActor = "soulseek:native-recommendations",
                    WorkRef = new WorkRef
                    {
                        Domain = "music",
                        Title = recommendation.Item.Trim(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["source"] = "soulseek-native-recommendations",
                            ["score"] = recommendation.Score,
                        },
                    },
                    ObservedAt = now,
                })
                .Where(observation => IsRecommendable(observation.WorkRef))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogInformation(ex, "[TasteRecommendations] Soulseek native recommendations unavailable");
            return Array.Empty<FederatedWorkRefObservation>();
        }
    }

    public async Task<TasteRecommendationWishlistPromotionResult> PromoteToWishlistAsync(
        TasteRecommendationWishlistPromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_wishlistService == null)
        {
            return new TasteRecommendationWishlistPromotionResult
            {
                Created = false,
                Message = "Wishlist service is not available.",
            };
        }

        if (!IsRecommendable(request.WorkRef))
        {
            return new TasteRecommendationWishlistPromotionResult
            {
                Created = false,
                Message = "WorkRef is not a safe music recommendation.",
            };
        }

        var searchText = BuildSearchText(request.WorkRef);
        var duplicate = await _wishlistService.FindBySearchTextAsync(searchText).ConfigureAwait(false);
        if (duplicate != null)
        {
            return new TasteRecommendationWishlistPromotionResult
            {
                Created = false,
                WishlistItemId = duplicate.Id.ToString(),
                SearchText = searchText,
                Message = "Wishlist already has this recommendation seed.",
            };
        }

        var created = await _wishlistService.CreateAsync(new WishlistItem
        {
            SearchText = searchText,
            Filter = BuildWishlistFilter(request.WorkRef, request.Note),
            AutoDownload = false,
            Enabled = false,
            MaxResults = 25,
            CreatedAt = DateTime.UtcNow,
        }).ConfigureAwait(false);

        return new TasteRecommendationWishlistPromotionResult
        {
            Created = true,
            WishlistItemId = created.Id.ToString(),
            SearchText = searchText,
            Message = "Created review-only Wishlist seed.",
        };
    }

    public async Task<TasteRecommendationRadarSubscriptionResult> SubscribeArtistRadarAsync(
        TasteRecommendationRadarSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_artistRadarService == null)
        {
            return new TasteRecommendationRadarSubscriptionResult
            {
                Created = false,
                Message = "Artist release radar service is not available.",
            };
        }

        if (!IsRecommendable(request.WorkRef))
        {
            return new TasteRecommendationRadarSubscriptionResult
            {
                Created = false,
                Message = "WorkRef is not a safe music recommendation.",
            };
        }

        var artistId = NormalizeArtistId(request.ArtistId) ?? GetExternalId(request.WorkRef, "musicbrainz_artist");
        if (string.IsNullOrWhiteSpace(artistId))
        {
            return new TasteRecommendationRadarSubscriptionResult
            {
                Created = false,
                Message = "Artist MusicBrainz ID is required for release radar subscription.",
            };
        }

        var subscription = await _artistRadarService.SubscribeAsync(new ArtistRadarSubscription
        {
            ArtistId = artistId,
            ArtistName = request.WorkRef.Creator ?? string.Empty,
            Scope = string.IsNullOrWhiteSpace(request.Scope) ? "trusted" : request.Scope.Trim(),
        }, cancellationToken).ConfigureAwait(false);

        return new TasteRecommendationRadarSubscriptionResult
        {
            Created = true,
            SubscriptionId = subscription.Id,
            ArtistId = subscription.ArtistId,
            Message = "Created artist release radar subscription.",
        };
    }

    public async Task<TasteRecommendationGraphPreviewResult> PreviewDiscoveryGraphAsync(
        TasteRecommendationGraphPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_discoveryGraphService == null)
        {
            return new TasteRecommendationGraphPreviewResult
            {
                Available = false,
                Message = "Discovery Graph service is not available.",
            };
        }

        if (!IsRecommendable(request.WorkRef))
        {
            return new TasteRecommendationGraphPreviewResult
            {
                Available = false,
                Message = "WorkRef is not a safe music recommendation.",
            };
        }

        var graphRequest = BuildGraphRequest(request.WorkRef);
        try
        {
            var graph = await _discoveryGraphService.BuildAsync(graphRequest, cancellationToken).ConfigureAwait(false);
            return new TasteRecommendationGraphPreviewResult
            {
                Available = true,
                SeedNodeId = graph.SeedNodeId,
                NodeCount = graph.Nodes.Count,
                EdgeCount = graph.Edges.Count,
                Reasons = BuildGraphReasons(graph),
            };
        }
        catch (InvalidOperationException ex)
        {
            return new TasteRecommendationGraphPreviewResult
            {
                Available = false,
                Message = ex.Message,
            };
        }
    }

    internal static TasteRecommendationResult BuildRecommendations(
        IEnumerable<FederatedWorkRefObservation> observations,
        int trustedActorCount,
        int minimumTrustedSources,
        int limit,
        bool includeSourceActors)
    {
        var externalIdGroups = new Dictionary<string, RecommendationGroup>(StringComparer.OrdinalIgnoreCase);
        var textGroups = new Dictionary<int, TextRecommendationGroupEntry>();
        var groups = new List<RecommendationGroup>();
        Span<char> textWorkKeyBuffer = stackalloc char[TextWorkKeyBufferLength];

        foreach (var observation in observations)
        {
            if (!string.Equals(observation.ActorName, "music", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(observation.RemoteActor) ||
                !IsRecommendable(observation.WorkRef))
            {
                continue;
            }

            if (TryGetMusicBrainzWorkKey(observation.WorkRef, out var musicBrainzId))
            {
                if (!externalIdGroups.TryGetValue(musicBrainzId, out var externalIdGroup))
                {
                    externalIdGroup = new RecommendationGroup(observation, groups.Count);
                    externalIdGroups.Add(musicBrainzId, externalIdGroup);
                    groups.Add(externalIdGroup);
                }
                else
                {
                    externalIdGroup.Add(observation);
                }

                continue;
            }

            if (TryWriteTextWorkKey(observation.WorkRef, textWorkKeyBuffer, out var textWorkKeyLength))
            {
                AddTextObservation(
                    observation,
                    textWorkKeyBuffer[..textWorkKeyLength],
                    allocatedWorkKey: null,
                    textGroups,
                    groups);
            }
            else
            {
                var allocatedWorkKey = BuildTextWorkKey(observation.WorkRef);
                AddTextObservation(
                    observation,
                    allocatedWorkKey,
                    allocatedWorkKey,
                    textGroups,
                    groups);
            }
        }

        var recommendationLimit = Math.Max(0, Math.Min(limit, groups.Count));
        var selectedGroups = new List<RecommendationGroup>(recommendationLimit);
        if (recommendationLimit > 0)
        {
            foreach (var group in groups)
            {
                if (group.TrustedSourceCount < minimumTrustedSources)
                {
                    continue;
                }

                group.Score = Math.Round(
                    group.TrustedSourceCount * 10 + RecencyScore(group.LastSeenAt),
                    3);
                RetainBestGroup(selectedGroups, group, recommendationLimit);
            }
        }

        var recommendations = new List<TasteRecommendation>(selectedGroups.Count);
        foreach (var group in selectedGroups)
        {
            recommendations.Add(new TasteRecommendation
            {
                WorkRef = group.WorkRef,
                TrustedSourceCount = group.TrustedSourceCount,
                LastSeenAt = group.LastSeenAt,
                Score = group.Score,
                Reasons = BuildReasons(group.WorkRef, group.TrustedSourceCount),
                SourceActors = includeSourceActors ? group.GetSortedSourceActors() : new List<string>(),
            });
        }

        return new TasteRecommendationResult
        {
            MinimumTrustedSources = minimumTrustedSources,
            TrustedActorCount = trustedActorCount,
            CandidateCount = groups.Count,
            Recommendations = recommendations,
        };
    }

    private static void AddTextObservation(
        FederatedWorkRefObservation observation,
        scoped ReadOnlySpan<char> workKey,
        string? allocatedWorkKey,
        Dictionary<int, TextRecommendationGroupEntry> textGroups,
        List<RecommendationGroup> groups)
    {
        var workKeyHash = string.GetHashCode(workKey, StringComparison.OrdinalIgnoreCase);
        textGroups.TryGetValue(workKeyHash, out var entry);
        var matchingEntry = entry;
        while (matchingEntry is not null &&
            !workKey.Equals(matchingEntry.WorkKey, StringComparison.OrdinalIgnoreCase))
        {
            matchingEntry = matchingEntry.Next;
        }

        if (matchingEntry is not null)
        {
            matchingEntry.Group.Add(observation);
            return;
        }

        var group = new RecommendationGroup(observation, groups.Count);
        var retainedWorkKey = allocatedWorkKey ?? workKey.ToString();
        textGroups[workKeyHash] = new TextRecommendationGroupEntry(
            retainedWorkKey,
            group,
            entry);
        groups.Add(group);
    }

    private static void RetainBestGroup(
        List<RecommendationGroup> selectedGroups,
        RecommendationGroup candidate,
        int limit)
    {
        var low = 0;
        var high = selectedGroups.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (CompareRecommendationGroups(candidate, selectedGroups[middle]) < 0)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        if (low == limit)
        {
            return;
        }

        selectedGroups.Insert(low, candidate);
        if (selectedGroups.Count > limit)
        {
            selectedGroups.RemoveAt(limit);
        }
    }

    private static int CompareRecommendationGroups(RecommendationGroup left, RecommendationGroup right)
    {
        var comparison = right.Score.CompareTo(left.Score);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.OrdinalIgnoreCase.Compare(
            left.WorkRef.Creator ?? string.Empty,
            right.WorkRef.Creator ?? string.Empty);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.OrdinalIgnoreCase.Compare(left.WorkRef.Title, right.WorkRef.Title);
        return comparison != 0 ? comparison : left.Sequence.CompareTo(right.Sequence);
    }

    private IEnumerable<FederatedWorkRefObservation> ExtractObservations(ActivityPubInboxEntry entry)
    {
        using var document = JsonDocument.Parse(entry.RawJson);
        foreach (var workRefElement in EnumerateWorkRefElements(document.RootElement))
        {
            WorkRef? workRef;
            try
            {
                workRef = workRefElement.Deserialize<WorkRef>();
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "[TasteRecommendations] Ignored malformed inbound WorkRef from {RemoteActor}", entry.RemoteActor);
                continue;
            }

            if (workRef?.ValidateSecurity() == true)
            {
                yield return new FederatedWorkRefObservation
                {
                    ActorName = entry.ActorName,
                    RemoteActor = entry.RemoteActor,
                    WorkRef = workRef,
                    ObservedAt = entry.ReceivedAt,
                };
            }
        }
    }

    private static IEnumerable<JsonElement> EnumerateWorkRefElements(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (IsWorkRefElement(element))
        {
            yield return element;
        }

        if (element.TryGetProperty("object", out var objectElement))
        {
            foreach (var workRef in EnumerateWorkRefElements(objectElement))
            {
                yield return workRef;
            }
        }

        if (element.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                foreach (var workRef in EnumerateWorkRefElements(item))
                {
                    yield return workRef;
                }
            }
        }

        if (element.TryGetProperty("orderedItems", out var orderedItemsElement) && orderedItemsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in orderedItemsElement.EnumerateArray())
            {
                foreach (var workRef in EnumerateWorkRefElements(item))
                {
                    yield return workRef;
                }
            }
        }
    }

    private static bool IsWorkRefElement(JsonElement element)
    {
        return element.TryGetProperty("type", out var typeElement) &&
            typeElement.ValueKind == JsonValueKind.String &&
            string.Equals(typeElement.GetString(), "WorkRef", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecommendable(WorkRef workRef)
    {
        return string.Equals(workRef.Domain, "music", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(workRef.Title) &&
            workRef.ValidateSecurity();
    }

    private static bool TryGetMusicBrainzWorkKey(WorkRef workRef, out string workKey)
    {
        if (workRef.ExternalIds.TryGetValue("musicbrainz", out var musicBrainzId) &&
            !string.IsNullOrWhiteSpace(musicBrainzId))
        {
            workKey = musicBrainzId.Trim();
            return true;
        }

        workKey = string.Empty;
        return false;
    }

    private static string BuildTextWorkKey(WorkRef workRef)
    {
        var creator = NormalizeKeyPart(workRef.Creator);
        var title = NormalizeKeyPart(workRef.Title);
        var year = workRef.Year?.ToString() ?? string.Empty;
        return $"text:{creator}:{title}:{year}";
    }

    private static bool TryWriteTextWorkKey(
        WorkRef workRef,
        scoped Span<char> destination,
        out int charsWritten)
    {
        charsWritten = 0;
        const string prefix = "text:";
        if (destination.Length < prefix.Length)
        {
            return false;
        }

        prefix.CopyTo(destination);
        charsWritten = prefix.Length;

        if (!TryWriteNormalizedKeyPart(
            workRef.Creator,
            destination[charsWritten..],
            out var creatorLength))
        {
            return false;
        }

        charsWritten += creatorLength;
        if (charsWritten == destination.Length)
        {
            return false;
        }

        destination[charsWritten++] = ':';
        if (!TryWriteNormalizedKeyPart(
            workRef.Title,
            destination[charsWritten..],
            out var titleLength))
        {
            return false;
        }

        charsWritten += titleLength;
        if (charsWritten == destination.Length)
        {
            return false;
        }

        destination[charsWritten++] = ':';
        if (workRef.Year.HasValue)
        {
            if (!workRef.Year.Value.TryFormat(
                destination[charsWritten..],
                out var yearLength,
                default,
                System.Globalization.CultureInfo.CurrentCulture))
            {
                return false;
            }

            charsWritten += yearLength;
        }

        return true;
    }

    private static bool TryWriteNormalizedKeyPart(
        string? value,
        scoped Span<char> destination,
        out int charsWritten)
    {
        var source = (value ?? string.Empty).AsSpan().Trim();
        var loweredLength = source.ToLowerInvariant(destination);
        if (loweredLength < 0)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = 0;
        var previousWasSpace = false;
        for (var index = 0; index < loweredLength; index++)
        {
            var current = destination[index];
            if (current == ' ' && previousWasSpace)
            {
                continue;
            }

            destination[charsWritten++] = current;
            previousWasSpace = current == ' ';
        }

        return true;
    }

    private static string NormalizeKeyPart(string? value)
    {
        return string.Join(' ', (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static double RecencyScore(DateTimeOffset lastSeen)
    {
        var ageDays = Math.Max(0, (DateTimeOffset.UtcNow - lastSeen).TotalDays);
        return Math.Max(0, 5 - ageDays);
    }

    private static List<string> BuildReasons(WorkRef workRef, int sourceCount)
    {
        var reasons = new List<string>
        {
            $"appeared in {sourceCount} trusted federated music libraries",
        };

        if (workRef.Metadata.TryGetValue("source", out var source) &&
            string.Equals(source?.ToString(), "soulseek-native-recommendations", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("suggested by Soulseek native recommendations");
        }

        if (!string.IsNullOrWhiteSpace(workRef.Creator))
        {
            reasons.Add($"near your federated music graph for {workRef.Creator}");
        }

        if (workRef.ExternalIds.ContainsKey("musicbrainz"))
        {
            reasons.Add("has MusicBrainz identity evidence");
        }

        return reasons;
    }

    private sealed class RecommendationGroup
    {
        private readonly string _firstSourceActor;
        private HashSet<string>? _sourceActors;

        public RecommendationGroup(FederatedWorkRefObservation observation, int sequence)
        {
            WorkRef = observation.WorkRef;
            LastSeenAt = observation.ObservedAt;
            Sequence = sequence;
            _firstSourceActor = observation.RemoteActor;
        }

        public WorkRef WorkRef { get; private set; }

        public DateTimeOffset LastSeenAt { get; private set; }

        public int Sequence { get; }

        public int TrustedSourceCount => _sourceActors?.Count ?? 1;

        public double Score { get; set; }

        public void Add(FederatedWorkRefObservation observation)
        {
            if (observation.ObservedAt > LastSeenAt)
            {
                WorkRef = observation.WorkRef;
                LastSeenAt = observation.ObservedAt;
            }

            if (_sourceActors is null)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(_firstSourceActor, observation.RemoteActor))
                {
                    return;
                }

                _sourceActors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    _firstSourceActor,
                    observation.RemoteActor,
                };
                return;
            }

            _sourceActors.Add(observation.RemoteActor);
        }

        public List<string> GetSortedSourceActors()
        {
            if (_sourceActors is null)
            {
                return new List<string> { _firstSourceActor };
            }

            var sourceActors = _sourceActors.ToList();
            sourceActors.Sort(StringComparer.OrdinalIgnoreCase);
            return sourceActors;
        }
    }

    private sealed class TextRecommendationGroupEntry
    {
        public TextRecommendationGroupEntry(
            string workKey,
            RecommendationGroup group,
            TextRecommendationGroupEntry? next)
        {
            WorkKey = workKey;
            Group = group;
            Next = next;
        }

        public string WorkKey { get; }

        public RecommendationGroup Group { get; }

        public TextRecommendationGroupEntry? Next { get; }
    }

    private async Task AddGraphEvidenceAsync(
        IReadOnlyList<TasteRecommendation> recommendations,
        CancellationToken cancellationToken)
    {
        foreach (var recommendation in recommendations)
        {
            try
            {
                var graph = await _discoveryGraphService!.BuildAsync(BuildGraphRequest(recommendation.WorkRef), cancellationToken).ConfigureAwait(false);
                var proximityScore = CalculateGraphProximityScore(graph);
                recommendation.GraphEvidence = new TasteRecommendationGraphEvidence
                {
                    SeedNodeId = graph.SeedNodeId,
                    NodeCount = graph.Nodes.Count,
                    EdgeCount = graph.Edges.Count,
                    ProximityScore = proximityScore,
                    Reasons = BuildGraphReasons(graph),
                };
                recommendation.Score = Math.Round(recommendation.Score + proximityScore, 3);
                recommendation.Reasons.AddRange(recommendation.GraphEvidence.Reasons);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "[TasteRecommendations] Discovery Graph evidence unavailable for {Title}", recommendation.WorkRef.Title);
            }
        }
    }

    private static DiscoveryGraphRequest BuildGraphRequest(WorkRef workRef)
    {
        return new DiscoveryGraphRequest
        {
            Scope = !string.IsNullOrWhiteSpace(GetExternalId(workRef, "musicbrainz_artist")) ||
                !string.IsNullOrWhiteSpace(workRef.Creator)
                    ? "artist"
                    : "track",
            ArtistId = GetExternalId(workRef, "musicbrainz_artist"),
            RecordingId = GetExternalId(workRef, "musicbrainz"),
            Title = workRef.Title,
            Artist = workRef.Creator,
        };
    }

    private static double CalculateGraphProximityScore(DiscoveryGraphResult graph)
    {
        var nodeScore = Math.Min(8, graph.Nodes.Count * 0.75);
        var edgeScore = Math.Min(7, graph.Edges.Count);
        return Math.Round(nodeScore + edgeScore, 3);
    }

    private static List<string> BuildGraphReasons(DiscoveryGraphResult graph)
    {
        var reasons = new List<string>();
        if (graph.Nodes.Count > 0)
        {
            reasons.Add($"near a Discovery Graph neighborhood with {graph.Nodes.Count} nodes");
        }

        if (graph.Edges.Count > 0)
        {
            reasons.Add($"connected by {graph.Edges.Count} graph evidence edges");
        }

        return reasons;
    }

    private static string BuildSearchText(WorkRef workRef)
    {
        return string.Join(' ', new[] { workRef.Creator, workRef.Title }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));
    }

    private static string BuildWishlistFilter(WorkRef workRef, string? note)
    {
        var metadata = new List<string> { "source:taste-recommendation", "review-only" };
        var musicBrainzRecordingId = GetExternalId(workRef, "musicbrainz");
        if (!string.IsNullOrWhiteSpace(musicBrainzRecordingId))
        {
            metadata.Add($"mbid:{musicBrainzRecordingId}");
        }

        var artistId = GetExternalId(workRef, "musicbrainz_artist");
        if (!string.IsNullOrWhiteSpace(artistId))
        {
            metadata.Add($"artist-mbid:{artistId}");
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            metadata.Add($"note:{note.Trim()}");
        }

        return string.Join("; ", metadata);
    }

    private static string? GetExternalId(WorkRef workRef, string key)
    {
        return workRef.ExternalIds.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static string? NormalizeArtistId(string? artistId)
    {
        return string.IsNullOrWhiteSpace(artistId) ? null : artistId.Trim();
    }
}
