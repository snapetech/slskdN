// <copyright file="LibraryReconciliationService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.v2.Reconciliation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.VirtualSoulfind.v2.Catalogue;

    /// <summary>
    ///     Production implementation of <see cref="ILibraryReconciliationService"/>.
    /// </summary>
    public sealed class LibraryReconciliationService : ILibraryReconciliationService
    {
        private const int PageSize = 250;
        private readonly ICatalogueStore _catalogue;

        public LibraryReconciliationService(ICatalogueStore catalogue)
        {
            _catalogue = catalogue;
        }

        public async Task<IReadOnlyList<string>> FindMissingTracksForReleaseAsync(string releaseId, CancellationToken ct = default)
        {
            // Get all tracks for the release
            var allTracks = await _catalogue.ListTracksForReleaseAsync(releaseId, ct);

            // For each track, check if it has a local copy or verified copy
            var missingTrackIds = new List<string>();
            var copyStates = await _catalogue.GetTrackCopyStatesAsync(
                allTracks.Select(track => track.TrackId).ToList(),
                ct);

            foreach (var track in allTracks)
            {
                var copyState = copyStates.GetValueOrDefault(track.TrackId);

                // Missing if no local files AND no verified copy
                if (!copyState.HasLocalFile && !copyState.HasVerifiedCopy)
                {
                    missingTrackIds.Add(track.TrackId);
                }
            }

            return missingTrackIds;
        }

        public async Task<IReadOnlyList<ReleaseGapAnalysis>> AnalyzeAllReleasesAsync(CancellationToken ct = default)
        {
            var results = new List<ReleaseGapAnalysis>();

            var releaseCount = await _catalogue.CountReleasesAsync(ct);

            if (releaseCount == 0)
            {
                return results;
            }

            for (var offset = 0; offset < releaseCount; offset += PageSize)
            {
                var releases = await _catalogue.ListReleasesAsync(offset, PageSize, ct);
                var tracksByRelease = await _catalogue.GetTracksByReleaseIdsAsync(
                    releases.Select(release => release.ReleaseId).ToList(),
                    ct);
                var releasesWithTracks = releases
                    .Where(release => tracksByRelease.ContainsKey(release.ReleaseId))
                    .ToList();
                var releaseGroups = await _catalogue.GetReleaseGroupsByIdsAsync(
                    releasesWithTracks.Select(release => release.ReleaseGroupId).ToList(),
                    ct);
                var artists = await _catalogue.GetArtistsByIdsAsync(
                    releaseGroups.Values.Select(releaseGroup => releaseGroup.ArtistId).ToList(),
                    ct);
                var copyStates = await _catalogue.GetTrackCopyStatesAsync(
                    tracksByRelease.Values.SelectMany(tracks => tracks).Select(track => track.TrackId).ToList(),
                    ct);

                foreach (var release in releases)
                {
                    var analysis = AnalyzeRelease(
                        release,
                        tracksByRelease.GetValueOrDefault(release.ReleaseId) ?? [],
                        releaseGroups,
                        artists,
                        copyStates);
                    if (analysis != null)
                    {
                        results.Add(analysis);
                    }
                }
            }

            return results;
        }

        public async Task<IReadOnlyList<UpgradeSuggestion>> FindUpgradeOpportunitiesAsync(
            float minQualityImprovement = 0.2f,
            CancellationToken ct = default)
        {
            var suggestions = new List<UpgradeSuggestion>();
            var fileCount = await _catalogue.CountLocalFilesAsync(ct);

            if (fileCount == 0)
            {
                return suggestions;
            }

            for (var offset = 0; offset < fileCount; offset += PageSize)
            {
                var localFiles = await _catalogue.ListLocalFilesAsync(offset, PageSize, ct);
                var unresolvedLocalFileIds = localFiles
                    .Where(localFile => string.IsNullOrWhiteSpace(localFile.InferredTrackId))
                    .Where(localFile => 1.0f - localFile.QualityRating >= minQualityImprovement)
                    .Select(localFile => localFile.LocalFileId)
                    .ToList();
                var verifiedByLocalFileId = unresolvedLocalFileIds.Count == 0
                    ? new Dictionary<string, VerifiedCopy>()
                    : await _catalogue.GetLatestVerifiedCopiesByLocalFileIdsAsync(unresolvedLocalFileIds, ct);
                var candidates = new List<(LocalFile LocalFile, string TrackId, float QualityImprovement)>();
                foreach (var localFile in localFiles)
                {
                    var trackId = localFile.InferredTrackId;
                    if (string.IsNullOrWhiteSpace(trackId) &&
                        verifiedByLocalFileId.TryGetValue(localFile.LocalFileId, out var verifiedCopy))
                    {
                        trackId = verifiedCopy.TrackId;
                    }

                    if (string.IsNullOrWhiteSpace(trackId))
                    {
                        continue;
                    }

                    var qualityImprovement = 1.0f - localFile.QualityRating;
                    if (qualityImprovement < minQualityImprovement)
                    {
                        continue;
                    }

                    candidates.Add((localFile, trackId, qualityImprovement));
                }

                if (candidates.Count == 0)
                {
                    continue;
                }

                var tracks = await _catalogue.GetTracksByIdsAsync(
                    candidates.Select(candidate => candidate.TrackId).ToList(),
                    ct);
                foreach (var candidate in candidates)
                {
                    suggestions.Add(new UpgradeSuggestion
                    {
                        TrackId = candidate.TrackId,
                        TrackTitle = tracks.GetValueOrDefault(candidate.TrackId)?.Title ?? candidate.TrackId,
                        LocalFileId = candidate.LocalFile.LocalFileId,
                        CurrentQuality = candidate.LocalFile.QualityRating,
                        TargetQuality = "FLAC",
                        QualityImprovement = candidate.QualityImprovement,
                        CurrentCodec = candidate.LocalFile.Codec,
                        CurrentBitrate = candidate.LocalFile.Bitrate,
                    });
                }
            }

            return suggestions;
        }

        public async Task<IReadOnlyList<string>> FindTracksWithoutLocalCopiesAsync(int limit = 100, CancellationToken ct = default)
        {
            var tracksWithoutCopies = new List<string>();
            var trackCount = await _catalogue.CountTracksAsync(ct);

            if (trackCount == 0)
            {
                return tracksWithoutCopies;
            }

            for (var offset = 0; offset < trackCount && tracksWithoutCopies.Count < limit; offset += PageSize)
            {
                var tracks = await _catalogue.ListTracksAsync(offset, PageSize, ct);
                var copyStates = await _catalogue.GetTrackCopyStatesAsync(
                    tracks.Select(track => track.TrackId).ToList(),
                    ct);
                foreach (var track in tracks)
                {
                    if (!copyStates.GetValueOrDefault(track.TrackId).HasLocalFile)
                    {
                        tracksWithoutCopies.Add(track.TrackId);
                        if (tracksWithoutCopies.Count >= limit)
                        {
                            break;
                        }
                    }
                }
            }

            return tracksWithoutCopies;
        }

        public async Task<IReadOnlyList<string>> FindOrphanedLocalFilesAsync(CancellationToken ct = default)
        {
            var orphanedFiles = new List<string>();
            var fileCount = await _catalogue.CountLocalFilesAsync(ct);

            if (fileCount == 0)
            {
                return orphanedFiles;
            }

            for (var offset = 0; offset < fileCount; offset += PageSize)
            {
                var localFiles = await _catalogue.ListLocalFilesAsync(offset, PageSize, ct);
                var unresolvedLocalFileIds = localFiles
                    .Where(localFile => string.IsNullOrWhiteSpace(localFile.InferredTrackId))
                    .Select(localFile => localFile.LocalFileId)
                    .ToList();
                var verifiedByLocalFileId = unresolvedLocalFileIds.Count == 0
                    ? new Dictionary<string, VerifiedCopy>()
                    : await _catalogue.GetLatestVerifiedCopiesByLocalFileIdsAsync(unresolvedLocalFileIds, ct);
                foreach (var localFile in localFiles)
                {
                    if (string.IsNullOrWhiteSpace(localFile.InferredTrackId) &&
                        !verifiedByLocalFileId.ContainsKey(localFile.LocalFileId))
                    {
                        orphanedFiles.Add(localFile.LocalFileId);
                    }
                }
            }

            return orphanedFiles;
        }

        private static ReleaseGapAnalysis? AnalyzeRelease(
            Release release,
            IReadOnlyList<Track> tracks,
            IReadOnlyDictionary<string, ReleaseGroup> releaseGroups,
            IReadOnlyDictionary<string, Artist> artists,
            IReadOnlyDictionary<string, TrackCopyState> copyStates)
        {
            if (tracks.Count == 0)
            {
                return null;
            }

            releaseGroups.TryGetValue(release.ReleaseGroupId, out var releaseGroup);
            var artist = releaseGroup == null
                ? null
                : artists.GetValueOrDefault(releaseGroup.ArtistId);

            var localCopyCount = 0;
            var verifiedCopyCount = 0;
            var missingTrackIds = new List<string>();

            foreach (var track in tracks)
            {
                var copyState = copyStates.GetValueOrDefault(track.TrackId);

                if (copyState.HasLocalFile)
                {
                    localCopyCount++;
                }

                if (copyState.HasVerifiedCopy)
                {
                    verifiedCopyCount++;
                }

                if (!copyState.HasLocalFile && !copyState.HasVerifiedCopy)
                {
                    missingTrackIds.Add(track.TrackId);
                }
            }

            if (localCopyCount == 0 || missingTrackIds.Count == 0)
            {
                return null;
            }

            return new ReleaseGapAnalysis
            {
                ReleaseId = release.ReleaseId,
                ReleaseTitle = release.Title,
                ArtistName = artist?.Name ?? "Unknown Artist",
                TotalTracks = tracks.Count,
                TracksWithLocalCopies = localCopyCount,
                TracksWithVerifiedCopies = verifiedCopyCount,
                MissingTrackIds = missingTrackIds,
            };
        }
    }
}
