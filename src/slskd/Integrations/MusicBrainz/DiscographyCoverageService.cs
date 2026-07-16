// <copyright file="DiscographyCoverageService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.MusicBrainz;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using slskd.DiscoveryGraph;
using slskd.HashDb;
using slskd.Integrations.MusicBrainz.API.DTO;
using slskd.Integrations.MusicBrainz.Models;
using slskd.Wishlist;

public interface IDiscographyCoverageService
{
    Task<DiscographyCoverageResult?> GetCoverageAsync(
        DiscographyCoverageRequest request,
        CancellationToken cancellationToken = default);

    Task<DiscographyWishlistPromotionResult> PromoteMissingToWishlistAsync(
        DiscographyWishlistPromotionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DiscographyCoverageService : IDiscographyCoverageService
{
    private readonly IArtistReleaseGraphService releaseGraphService;
    private readonly IDiscographyProfileService profileService;
    private readonly IMusicBrainzClient musicBrainzClient;
    private readonly IHashDbService hashDb;
    private readonly IWishlistService wishlistService;
    private readonly ILogger<DiscographyCoverageService> logger;
    private readonly IDiscoveryGraphService? discoveryGraphService;

    public DiscographyCoverageService(
        IArtistReleaseGraphService releaseGraphService,
        IDiscographyProfileService profileService,
        IMusicBrainzClient musicBrainzClient,
        IHashDbService hashDb,
        IWishlistService wishlistService,
        ILogger<DiscographyCoverageService> logger,
        IDiscoveryGraphService? discoveryGraphService = null)
    {
        this.releaseGraphService = releaseGraphService;
        this.profileService = profileService;
        this.musicBrainzClient = musicBrainzClient;
        this.hashDb = hashDb;
        this.wishlistService = wishlistService;
        this.logger = logger;
        this.discoveryGraphService = discoveryGraphService;
    }

    public async Task<DiscographyCoverageResult?> GetCoverageAsync(
        DiscographyCoverageRequest request,
        CancellationToken cancellationToken = default)
    {
        var artistId = request.ArtistId.Trim();
        var graph = await releaseGraphService.GetArtistReleaseGraphAsync(
            artistId,
            request.ForceRefresh,
            cancellationToken).ConfigureAwait(false);

        if (graph == null)
        {
            return null;
        }

        var releaseIds = profileService
            .ApplyProfile(graph, DiscographyProfileFilter.FromProfile(request.Profile))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var wishlistItems = await wishlistService.ListAsync().ConfigureAwait(false);
        var wishlistKeys = wishlistItems
            .Select(item => NormalizeKey(item.SearchText, item.Filter))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var wishlistSearchTexts = wishlistItems
            .Select(item => NormalizeSearchText(item.SearchText))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedReleases = graph.ReleaseGroups
            .SelectMany(group => group.Releases
                .Where(release => releaseIds.Contains(release.ReleaseId))
                .Select(release => (Group: group, Release: release)))
            .ToList();
        var storedAlbums = (await hashDb
                .GetAlbumTargetsAsync(
                    selectedReleases.Select(item => item.Release.ReleaseId),
                    cancellationToken)
                .ConfigureAwait(false))
            .ToDictionary(album => album.ReleaseId, StringComparer.OrdinalIgnoreCase);
        var resolvedReleases = new List<(ReleaseGroup Group, Release Release, AlbumTarget? Album)>(selectedReleases.Count);
        foreach (var item in selectedReleases)
        {
            AlbumTarget? album;
            if (storedAlbums.TryGetValue(item.Release.ReleaseId, out var storedAlbum))
            {
                album = ToAlbumTarget(storedAlbum);
            }
            else
            {
                album = await musicBrainzClient
                    .GetReleaseAsync(item.Release.ReleaseId, cancellationToken)
                    .ConfigureAwait(false);
                if (album == null)
                {
                    logger.LogDebug(
                        "[DiscographyCoverage] Release {ReleaseId} could not be resolved",
                        item.Release.ReleaseId);
                }
                else
                {
                    await hashDb.UpsertAlbumTargetAsync(album, cancellationToken).ConfigureAwait(false);
                }
            }

            resolvedReleases.Add((item.Group, item.Release, album));
        }

        var resolvedReleaseIds = resolvedReleases
            .Where(item => item.Album != null)
            .Select(item => item.Release.ReleaseId)
            .ToArray();
        var tracksByRelease = (await hashDb
                .GetAlbumTracksAsync(resolvedReleaseIds, cancellationToken)
                .ConfigureAwait(false))
            .ToLookup(track => track.ReleaseId, StringComparer.OrdinalIgnoreCase);
        var hashesByRecording = (await hashDb
                .LookupHashesByRecordingIdsAsync(
                    tracksByRelease
                        .SelectMany(group => group)
                        .Where(track => !string.IsNullOrWhiteSpace(track.RecordingId))
                        .Select(track => track.RecordingId),
                    cancellationToken)
                .ConfigureAwait(false))
            .ToLookup(hash => hash.MusicBrainzId, StringComparer.Ordinal);

        var result = new DiscographyCoverageResult
        {
            ArtistId = graph.ArtistId,
            ArtistName = graph.Name,
            Profile = request.Profile,
        };

        foreach (var item in resolvedReleases)
        {
            var group = item.Group;
            var release = item.Release;
            var album = item.Album;
            if (album == null)
            {
                result.Releases.Add(new DiscographyCoverageRelease
                {
                    ReleaseGroupId = group.ReleaseGroupId,
                    ReleaseId = release.ReleaseId,
                    Title = release.Title,
                    ReleaseDate = release.ReleaseDate,
                    Type = group.Type,
                });
                continue;
            }

            var tracks = tracksByRelease[release.ReleaseId]
                .OrderBy(track => track.Position)
                .ToList();
            var coverageRelease = new DiscographyCoverageRelease
            {
                ReleaseGroupId = group.ReleaseGroupId,
                ReleaseId = release.ReleaseId,
                Title = string.IsNullOrWhiteSpace(album.Title) ? release.Title : album.Title,
                ReleaseDate = album.Metadata.ReleaseDate?.ToString("yyyy-MM-dd") ?? release.ReleaseDate,
                Type = group.Type,
                TotalTracks = tracks.Count,
            };

            foreach (var track in tracks)
            {
                var coverageTrack = BuildTrackCoverage(
                    track,
                    wishlistKeys,
                    wishlistSearchTexts,
                    hashesByRecording[track.RecordingId]);
                coverageRelease.Tracks.Add(coverageTrack);

                if (coverageTrack.Status == DiscographyCoverageStatus.MeshAvailable)
                {
                    coverageRelease.CoveredTracks++;
                }
            }

            result.Releases.Add(coverageRelease);
        }

        result.TotalReleases = result.Releases.Count;
        result.CompleteReleases = result.Releases.Count(release => release.Complete);
        result.TotalTracks = result.Releases.Sum(release => release.TotalTracks);
        result.CoveredTracks = result.Releases.Sum(release => release.CoveredTracks);

        if (request.IncludeDiscoveryGraphPriority)
        {
            await ApplyDiscoveryGraphPriorityAsync(result, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<DiscographyWishlistPromotionResult> PromoteMissingToWishlistAsync(
        DiscographyWishlistPromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        var coverage = await GetCoverageAsync(
            new DiscographyCoverageRequest
            {
                ArtistId = request.ArtistId,
                Profile = request.Profile,
            },
            cancellationToken).ConfigureAwait(false);

        if (coverage == null)
        {
            throw new NotFoundException($"Artist {request.ArtistId} not found");
        }

        var result = new DiscographyWishlistPromotionResult
        {
            ArtistId = coverage.ArtistId,
        };
        var createdKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var track in coverage.Releases.SelectMany(release => release.Tracks))
        {
            if (track.Status != DiscographyCoverageStatus.Absent)
            {
                if (track.Status == DiscographyCoverageStatus.WishlistSeeded)
                {
                    result.AlreadySeededCount++;
                }

                continue;
            }

            var searchText = BuildSearchText(track);
            var key = NormalizeKey(searchText, request.Filter);
            if (!createdKeys.Add(key))
            {
                result.AlreadySeededCount++;
                continue;
            }

            var item = await wishlistService.CreateAsync(new WishlistItem
            {
                SearchText = searchText,
                Filter = request.Filter.Trim(),
                Enabled = true,
                AutoDownload = false,
                MaxResults = request.MaxResults,
            }).ConfigureAwait(false);

            result.CreatedCount++;
            result.CreatedItemIds.Add(item.Id);
        }

        return result;
    }

    private static AlbumTarget ToAlbumTarget(HashDb.Models.AlbumTargetEntry existing)
    {
        return new AlbumTarget
        {
            MusicBrainzReleaseId = existing.ReleaseId,
            DiscogsReleaseId = existing.DiscogsReleaseId,
            Title = existing.Title,
            Artist = existing.Artist,
            Metadata = new ReleaseMetadata
            {
                Country = existing.Country,
                Label = existing.Label,
                Status = existing.Status,
            },
        };
    }

    private static DiscographyCoverageTrack BuildTrackCoverage(
        HashDb.Models.AlbumTargetTrackEntry track,
        HashSet<string> wishlistKeys,
        HashSet<string> wishlistSearchTexts,
        IEnumerable<HashDb.Models.HashDbEntry> hashes)
    {
        var result = new DiscographyCoverageTrack
        {
            Position = track.Position,
            Title = track.Title,
            Artist = track.Artist,
            RecordingId = track.RecordingId,
            DurationMs = track.DurationMs,
        };

        if (string.IsNullOrWhiteSpace(track.RecordingId))
        {
            result.Status = DiscographyCoverageStatus.Ambiguous;
            result.Evidence.Add("Missing MusicBrainz recording id");
            return result;
        }

        foreach (var hash in hashes.OrderByDescending(hash => hash.LastUpdatedAt))
        {
            result.Matches.Add(new HashMatch
            {
                FlacKey = hash.FlacKey,
                Size = hash.Size,
                UseCount = hash.UseCount,
                FirstSeenAt = hash.FirstSeenAt,
                LastUpdatedAt = hash.LastUpdatedAt,
            });
        }

        if (result.Matches.Count > 0)
        {
            result.Status = DiscographyCoverageStatus.MeshAvailable;
            result.Evidence.Add("HashDb has verified content evidence for this recording");
            return result;
        }

        var searchText = BuildSearchText(result);
        if (wishlistKeys.Contains(NormalizeKey(searchText, "flac")) ||
            wishlistSearchTexts.Contains(NormalizeSearchText(searchText)))
        {
            result.Status = DiscographyCoverageStatus.WishlistSeeded;
            result.Evidence.Add("Wishlist already has a matching search seed");
            return result;
        }

        result.Status = DiscographyCoverageStatus.Absent;
        return result;
    }

    private async Task ApplyDiscoveryGraphPriorityAsync(
        DiscographyCoverageResult result,
        CancellationToken cancellationToken)
    {
        if (discoveryGraphService == null || result.Releases.Count == 0)
        {
            ApplyFallbackPriority(result);
            return;
        }

        try
        {
            var graph = await discoveryGraphService.BuildAsync(
                new DiscoveryGraphRequest
                {
                    Scope = "artist",
                    ArtistId = result.ArtistId,
                    Artist = result.ArtistName,
                },
                cancellationToken).ConfigureAwait(false);

            ApplyGraphPriority(result, graph);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogDebug(ex, "[DiscographyCoverage] Discovery Graph priority unavailable for artist {ArtistId}", result.ArtistId);
            ApplyFallbackPriority(result);
        }
    }

    private static void ApplyGraphPriority(DiscographyCoverageResult result, DiscoveryGraphResult graph)
    {
        var incidentEdges = graph.Edges
            .GroupBy(edge => edge.SourceNodeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var release in result.Releases)
        {
            var graphNodeId = $"release-group:{release.ReleaseGroupId}";
            var node = graph.Nodes.FirstOrDefault(node => string.Equals(node.NodeId, graphNodeId, StringComparison.OrdinalIgnoreCase));
            var edges = incidentEdges.TryGetValue(graph.SeedNodeId, out var sourceEdges)
                ? sourceEdges.Where(edge => string.Equals(edge.TargetNodeId, graphNodeId, StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<DiscoveryGraphEdge>();
            var densityScore = node == null
                ? 0
                : Clamp((node.Weight + edges.Sum(edge => edge.Weight) + Math.Min(graph.Nodes.Count, 12) / 12.0) / 3.0);

            ApplyReleasePriority(release, densityScore);
        }

        result.GraphPriority = BuildPrioritySummary(result, graph.Nodes.Count, graph.Edges.Count);
        result.GraphPriority.Reasons.Add("Discovery Graph artist neighborhood density included in release priority.");
    }

    private static void ApplyFallbackPriority(DiscographyCoverageResult result)
    {
        var releaseGroupCount = Math.Max(result.Releases.Select(release => release.ReleaseGroupId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), 1);
        foreach (var release in result.Releases)
        {
            ApplyReleasePriority(release, Clamp(1.0 / releaseGroupCount));
        }

        result.GraphPriority = BuildPrioritySummary(result, result.Releases.Count, 0);
        result.GraphPriority.Reasons.Add("Fallback release-group density used because Discovery Graph priority was unavailable.");
    }

    private static void ApplyReleasePriority(DiscographyCoverageRelease release, double graphDensityScore)
    {
        release.GraphDensityScore = graphDensityScore;
        release.GapScore = release.TotalTracks == 0 ? 0 : Clamp((double)(release.TotalTracks - release.CoveredTracks) / release.TotalTracks);
        release.EvidenceScore = CalculateEvidenceScore(release);
        release.PriorityScore = Clamp((release.GapScore * 0.45) + (release.GraphDensityScore * 0.35) + (release.EvidenceScore * 0.20));
        release.PriorityReasons.Clear();

        if (release.GapScore > 0)
        {
            release.PriorityReasons.Add($"{release.TotalTracks - release.CoveredTracks} missing track(s) remain in this release.");
        }

        if (release.GraphDensityScore >= 0.50)
        {
            release.PriorityReasons.Add("Release sits in a dense Discovery Graph artist neighborhood.");
        }

        if (release.EvidenceScore >= 0.50)
        {
            release.PriorityReasons.Add("Existing HashDb or Wishlist evidence makes completion lower risk.");
        }
    }

    private static DiscographyGraphPrioritySummary BuildPrioritySummary(
        DiscographyCoverageResult result,
        int nodeCount,
        int edgeCount)
    {
        var prioritized = result.Releases
            .Where(release => release.PriorityScore > 0 && !release.Complete)
            .OrderByDescending(release => release.PriorityScore)
            .ThenBy(release => release.ReleaseDate, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return new DiscographyGraphPrioritySummary
        {
            NodeCount = nodeCount,
            EdgeCount = edgeCount,
            NeighborhoodDensityScore = result.Releases.Count == 0 ? 0 : Clamp(result.Releases.Average(release => release.GraphDensityScore)),
            EvidenceScore = result.Releases.Count == 0 ? 0 : Clamp(result.Releases.Average(release => release.EvidenceScore)),
            RecommendedReleaseIds = prioritized.Select(release => release.ReleaseId).ToList(),
        };
    }

    private static double CalculateEvidenceScore(DiscographyCoverageRelease release)
    {
        if (release.TotalTracks == 0)
        {
            return 0;
        }

        var covered = release.Tracks.Count(track => track.Status == DiscographyCoverageStatus.MeshAvailable);
        var wishlist = release.Tracks.Count(track => track.Status == DiscographyCoverageStatus.WishlistSeeded);
        var useCount = release.Tracks
            .SelectMany(track => track.Matches)
            .Sum(match => Math.Min(match.UseCount, 5));

        return Clamp(((covered + (wishlist * 0.5)) / release.TotalTracks) + Math.Min(useCount, 10) / 20.0);
    }

    private static string BuildSearchText(DiscographyCoverageTrack track)
    {
        var artist = string.IsNullOrWhiteSpace(track.Artist) ? string.Empty : track.Artist.Trim();
        var title = string.IsNullOrWhiteSpace(track.Title) ? string.Empty : track.Title.Trim();
        return string.IsNullOrWhiteSpace(artist) ? title : $"{artist} {title}";
    }

    private static string NormalizeKey(string searchText, string filter) =>
        NormalizeSearchText(searchText) + "\u001f" + (filter ?? string.Empty).Trim();

    private static string NormalizeSearchText(string searchText) =>
        (searchText ?? string.Empty).Trim();

    private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
}
