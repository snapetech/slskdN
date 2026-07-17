// <copyright file="InMemoryIntentQueue.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.v2.Intents
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.VirtualSoulfind.Core;

    /// <summary>
    ///     In-memory implementation of <see cref="IIntentQueue"/>.
    /// </summary>
    public sealed class InMemoryIntentQueue : IIntentQueue
    {
        private readonly ConcurrentDictionary<string, DesiredRelease> _releases = new();
        private readonly ConcurrentDictionary<string, DesiredTrack> _tracks = new();

        public Task<DesiredRelease> EnqueueReleaseAsync(
            string releaseId,
            IntentPriority priority = IntentPriority.Normal,
            IntentMode mode = IntentMode.Wanted,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            var desiredRelease = new DesiredRelease
            {
                DesiredReleaseId = Guid.NewGuid().ToString(),
                ReleaseId = releaseId,
                Priority = priority,
                Mode = mode,
                Status = IntentStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Notes = notes,
            };

            _releases[desiredRelease.DesiredReleaseId] = desiredRelease;
            return Task.FromResult(desiredRelease);
        }

        public Task<DesiredTrack> EnqueueTrackAsync(
            ContentDomain domain,
            string trackId,
            IntentPriority priority = IntentPriority.Normal,
            string? parentDesiredReleaseId = null,
            CancellationToken cancellationToken = default)
        {
            var desiredTrack = new DesiredTrack
            {
                Domain = domain,
                DesiredTrackId = Guid.NewGuid().ToString(),
                TrackId = trackId,
                ParentDesiredReleaseId = parentDesiredReleaseId,
                Priority = priority,
                Status = IntentStatus.Pending,
                PlannedSources = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _tracks[desiredTrack.DesiredTrackId] = desiredTrack;
            return Task.FromResult(desiredTrack);
        }

        public Task<IReadOnlyList<DesiredTrack>> GetPendingTracksAsync(
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return Task.FromResult<IReadOnlyList<DesiredTrack>>(Array.Empty<DesiredTrack>());
            }

            var pending = new PriorityQueue<DesiredTrack, PendingTrackPriority>(PendingTrackWorstFirstComparer.Instance);
            var sequence = 0;
            foreach (var pair in _tracks)
            {
                var track = pair.Value;
                if (track.Status != IntentStatus.Pending)
                {
                    continue;
                }

                var priority = new PendingTrackPriority(track.Priority, track.CreatedAt, sequence++);
                if (pending.Count < limit)
                {
                    pending.Enqueue(track, priority);
                }
                else
                {
                    pending.TryPeek(out _, out var worstPriority);
                    if (PendingTrackWorstFirstComparer.Instance.Compare(priority, worstPriority) > 0)
                    {
                        pending.Dequeue();
                        pending.Enqueue(track, priority);
                    }
                }
            }

            var result = new DesiredTrack[pending.Count];
            for (var index = result.Length - 1; index >= 0; index--)
            {
                result[index] = pending.Dequeue();
            }

            return Task.FromResult<IReadOnlyList<DesiredTrack>>(result);
        }

        public Task UpdateTrackStatusAsync(
            string desiredTrackId,
            IntentStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            if (_tracks.TryGetValue(desiredTrackId, out var track))
            {
                var updated = new DesiredTrack
                {
                    Domain = track.Domain,
                    DesiredTrackId = track.DesiredTrackId,
                    TrackId = track.TrackId,
                    ParentDesiredReleaseId = track.ParentDesiredReleaseId,
                    Priority = track.Priority,
                    Status = newStatus,
                    PlannedSources = track.PlannedSources,
                    CreatedAt = track.CreatedAt,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                _tracks[desiredTrackId] = updated;
            }

            return Task.CompletedTask;
        }

        public Task<bool> TryUpdateTrackStatusAsync(
            string desiredTrackId,
            IntentStatus expectedStatus,
            IntentStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (_tracks.TryGetValue(desiredTrackId, out var track))
            {
                if (track.Status != expectedStatus)
                {
                    return Task.FromResult(false);
                }

                var updated = new DesiredTrack
                {
                    Domain = track.Domain,
                    DesiredTrackId = track.DesiredTrackId,
                    TrackId = track.TrackId,
                    ParentDesiredReleaseId = track.ParentDesiredReleaseId,
                    Priority = track.Priority,
                    Status = newStatus,
                    PlannedSources = track.PlannedSources,
                    CreatedAt = track.CreatedAt,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                if (_tracks.TryUpdate(desiredTrackId, updated, track))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<DesiredTrack?> GetTrackIntentAsync(
            string desiredTrackId,
            CancellationToken cancellationToken = default)
        {
            _tracks.TryGetValue(desiredTrackId, out var track);
            return Task.FromResult(track);
        }

        public Task<DesiredRelease?> GetReleaseIntentAsync(
            string desiredReleaseId,
            CancellationToken cancellationToken = default)
        {
            _releases.TryGetValue(desiredReleaseId, out var release);
            return Task.FromResult(release);
        }

        public Task<int> CountTracksByStatusAsync(
            IntentStatus status,
            CancellationToken cancellationToken = default)
        {
            var count = 0;
            foreach (var pair in _tracks)
            {
                if (pair.Value.Status == status)
                {
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        private readonly record struct PendingTrackPriority(
            IntentPriority Priority,
            DateTimeOffset CreatedAt,
            int Sequence);

        private sealed class PendingTrackWorstFirstComparer : IComparer<PendingTrackPriority>
        {
            public static PendingTrackWorstFirstComparer Instance { get; } = new();

            public int Compare(PendingTrackPriority left, PendingTrackPriority right)
            {
                var priorityComparison = ((int)left.Priority).CompareTo((int)right.Priority);
                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                var createdComparison = right.CreatedAt.CompareTo(left.CreatedAt);
                return createdComparison != 0
                    ? createdComparison
                    : right.Sequence.CompareTo(left.Sequence);
            }
        }
    }
}
