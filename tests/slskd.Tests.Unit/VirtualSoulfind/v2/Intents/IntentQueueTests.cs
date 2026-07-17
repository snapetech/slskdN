// <copyright file="IntentQueueTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

// Intent Queue tests
namespace slskd.Tests.Unit.VirtualSoulfind.v2.Intents
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using slskd.VirtualSoulfind.Core;
    using slskd.VirtualSoulfind.v2.Intents;
    using Xunit;

    public class IntentQueueTests
    {
        [Fact]
        public async Task EnqueueTrack_CreatesIntent()
        {
            var queue = new InMemoryIntentQueue();

            var intent = await queue.EnqueueTrackAsync(ContentDomain.Music, "track123", IntentPriority.High);

            Assert.NotNull(intent);
            Assert.NotEmpty(intent.DesiredTrackId);
            Assert.Equal("track123", intent.TrackId);
            Assert.Equal(IntentPriority.High, intent.Priority);
            Assert.Equal(IntentStatus.Pending, intent.Status);
        }

        [Fact]
        public async Task GetPendingTracks_ReturnsPendingOnly()
        {
            var queue = new InMemoryIntentQueue();

            var intent1 = await queue.EnqueueTrackAsync(ContentDomain.Music, "track1");
            var intent2 = await queue.EnqueueTrackAsync(ContentDomain.Music, "track2");
            await queue.UpdateTrackStatusAsync(intent2.DesiredTrackId, IntentStatus.Completed);

            var pending = await queue.GetPendingTracksAsync();

            Assert.Single(pending);
            Assert.Equal(intent1.DesiredTrackId, pending[0].DesiredTrackId);
        }

        [Fact]
        public async Task GetPendingTracks_OrdersByPriorityThenDate()
        {
            var queue = new InMemoryIntentQueue();

            await queue.EnqueueTrackAsync(ContentDomain.Music, "track1", IntentPriority.Low);
            await queue.EnqueueTrackAsync(ContentDomain.Music, "track2", IntentPriority.High);
            await queue.EnqueueTrackAsync(ContentDomain.Music, "track3", IntentPriority.Normal);

            var pending = await queue.GetPendingTracksAsync();

            Assert.Equal(3, pending.Count);
            Assert.Equal(IntentPriority.High, pending[0].Priority);
            Assert.Equal(IntentPriority.Normal, pending[1].Priority);
            Assert.Equal(IntentPriority.Low, pending[2].Priority);
        }

        [Fact]
        public async Task GetPendingTracks_OrdersSamePriorityByCreationTime()
        {
            var queue = new InMemoryIntentQueue();

            var first = await queue.EnqueueTrackAsync(ContentDomain.Music, "track1", IntentPriority.High);
            Thread.Sleep(1);
            var second = await queue.EnqueueTrackAsync(ContentDomain.Music, "track2", IntentPriority.High);

            var pending = await queue.GetPendingTracksAsync();

            Assert.Equal(new[] { first.DesiredTrackId, second.DesiredTrackId }, pending.Select(track => track.DesiredTrackId));
        }

        [Fact]
        public void GetPendingTracks_WidePopulationSmallLimitBoundsAllocation()
        {
            var queue = CreateWideQueue();
            for (var iteration = 0; iteration < 8; iteration++)
            {
                _ = queue.GetPendingTracksAsync(50).GetAwaiter().GetResult();
            }

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var pending = queue.GetPendingTracksAsync(50).GetAwaiter().GetResult();
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Equal(50, pending.Count);
            Assert.All(pending, track => Assert.Equal(IntentPriority.Urgent, track.Priority));
            Assert.True(allocatedBytes < 16_384, $"Allocated {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public async Task UpdateTrackStatus_ChangesStatus()
        {
            var queue = new InMemoryIntentQueue();

            var intent = await queue.EnqueueTrackAsync(ContentDomain.Music, "track1");
            await queue.UpdateTrackStatusAsync(intent.DesiredTrackId, IntentStatus.InProgress);

            var updated = await queue.GetTrackIntentAsync(intent.DesiredTrackId);

            Assert.NotNull(updated);
            Assert.Equal(IntentStatus.InProgress, updated.Status);
        }

        [Fact]
        public async Task TryUpdateTrackStatus_AllowsOnlyOneConcurrentClaim()
        {
            var queue = new InMemoryIntentQueue();
            var intent = await queue.EnqueueTrackAsync(ContentDomain.Music, "track1");
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var claimTasks = Enumerable.Range(0, 32).Select(async _ =>
            {
                await start.Task;
                return await queue.TryUpdateTrackStatusAsync(
                    intent.DesiredTrackId,
                    IntentStatus.Pending,
                    IntentStatus.InProgress);
            }).ToArray();

            start.SetResult();
            var claims = await Task.WhenAll(claimTasks);
            var updated = await queue.GetTrackIntentAsync(intent.DesiredTrackId);

            Assert.Single(claims, claimed => claimed);
            Assert.NotNull(updated);
            Assert.Equal(IntentStatus.InProgress, updated.Status);
        }

        [Fact]
        public async Task CountTracksByStatus_ReturnsCorrectCount()
        {
            var queue = new InMemoryIntentQueue();

            var intent1 = await queue.EnqueueTrackAsync(ContentDomain.Music, "track1");
            var intent2 = await queue.EnqueueTrackAsync(ContentDomain.Music, "track2");
            await queue.UpdateTrackStatusAsync(intent1.DesiredTrackId, IntentStatus.Completed);

            var pendingCount = await queue.CountTracksByStatusAsync(IntentStatus.Pending);
            var completedCount = await queue.CountTracksByStatusAsync(IntentStatus.Completed);

            Assert.Equal(1, pendingCount);
            Assert.Equal(1, completedCount);
        }

        [Fact]
        public void CountTracksByStatus_WidePopulationBoundsAllocation()
        {
            var queue = CreateWideQueue();
            for (var iteration = 0; iteration < 8; iteration++)
            {
                _ = queue.CountTracksByStatusAsync(IntentStatus.Pending).GetAwaiter().GetResult();
            }

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var count = queue.CountTracksByStatusAsync(IntentStatus.Pending).GetAwaiter().GetResult();
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Equal(10_000, count);
            Assert.True(allocatedBytes < 1_024, $"Allocated {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public async Task EnqueueRelease_CreatesIntent()
        {
            var queue = new InMemoryIntentQueue();

            var intent = await queue.EnqueueReleaseAsync("release123", IntentPriority.Urgent, IntentMode.Wanted, "Test notes");

            Assert.NotNull(intent);
            Assert.NotEmpty(intent.DesiredReleaseId);
            Assert.Equal("release123", intent.ReleaseId);
            Assert.Equal(IntentPriority.Urgent, intent.Priority);
            Assert.Equal(IntentMode.Wanted, intent.Mode);
            Assert.Equal("Test notes", intent.Notes);
        }

        [Fact]
        public async Task GetReleaseIntent_ReturnsReleaseOrNull()
        {
            var queue = new InMemoryIntentQueue();
            var intent = await queue.EnqueueReleaseAsync("release123");

            var found = await queue.GetReleaseIntentAsync(intent.DesiredReleaseId);
            var missing = await queue.GetReleaseIntentAsync("missing-id");

            Assert.NotNull(found);
            Assert.Equal(intent.DesiredReleaseId, found.DesiredReleaseId);
            Assert.Null(missing);
        }

        private static InMemoryIntentQueue CreateWideQueue()
        {
            var queue = new InMemoryIntentQueue();
            for (var index = 0; index < 10_000; index++)
            {
                _ = queue.EnqueueTrackAsync(
                    ContentDomain.Music,
                    $"track-{index:D5}",
                    (IntentPriority)(index % 4)).GetAwaiter().GetResult();
            }

            return queue;
        }
    }
}
