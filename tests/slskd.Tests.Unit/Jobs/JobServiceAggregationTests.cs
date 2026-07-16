// <copyright file="JobServiceAggregationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Jobs;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.HashDb;
using slskd.Integrations.MusicBrainz;
using slskd.Jobs;
using Xunit;

public class JobServiceAggregationTests
{
    [Fact]
    public async Task DiscographyGetJobAsync_WritesOnlyWhenDerivedAggregateChanges()
    {
        var job = new DiscographyJob
        {
            JobId = "job-1",
            TotalReleases = 2,
            CompletedReleases = 1,
            FailedReleases = 0,
            Status = JobStatus.Running,
        };
        var releases = new List<DiscographyReleaseJobStatus>
        {
            new() { ReleaseId = "release-1", Status = JobStatus.Completed },
            new() { ReleaseId = "release-2", Status = JobStatus.Pending },
        };
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetDiscographyJobAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        hashDb.Setup(service => service.GetDiscographyReleaseJobsAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(releases);
        hashDb.Setup(service => service.UpsertDiscographyJobAsync(It.IsAny<DiscographyJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new DiscographyJobService(
            Mock.Of<IDiscographyProfileService>(),
            Mock.Of<IArtistReleaseGraphService>(),
            hashDb.Object,
            NullLogger<DiscographyJobService>.Instance);

        var unchanged = await service.GetJobAsync("job-1");

        Assert.NotNull(unchanged);
        hashDb.Verify(value => value.UpsertDiscographyJobAsync(
            It.IsAny<DiscographyJob>(),
            It.IsAny<CancellationToken>()), Times.Never);

        releases[1].Status = JobStatus.Completed;
        var changed = await service.GetJobAsync("job-1");

        Assert.NotNull(changed);
        Assert.Equal(JobStatus.Completed, changed!.Status);
        Assert.Equal(2, changed.CompletedReleases);
        hashDb.Verify(value => value.UpsertDiscographyJobAsync(
            It.Is<DiscographyJob>(persisted =>
                persisted.Status == JobStatus.Completed &&
                persisted.CompletedReleases == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LabelCrateGetJobAsync_WritesOnlyWhenDerivedAggregateChanges()
    {
        var job = new LabelCrateJob
        {
            JobId = "job-1",
            TotalReleases = 2,
            CompletedReleases = 1,
            FailedReleases = 0,
            Status = JobStatus.Running,
        };
        var releases = new List<DiscographyReleaseJobStatus>
        {
            new() { ReleaseId = "release-1", Status = JobStatus.Completed },
            new() { ReleaseId = "release-2", Status = JobStatus.Pending },
        };
        var hashDb = new Mock<IHashDbService>();
        hashDb.Setup(service => service.GetLabelCrateJobAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        hashDb.Setup(service => service.GetLabelCrateReleaseJobsAsync("job-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(releases);
        hashDb.Setup(service => service.UpsertLabelCrateJobAsync(It.IsAny<LabelCrateJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new LabelCrateJobService(
            hashDb.Object,
            NullLogger<LabelCrateJobService>.Instance);

        var unchanged = await service.GetJobAsync("job-1");

        Assert.NotNull(unchanged);
        hashDb.Verify(value => value.UpsertLabelCrateJobAsync(
            It.IsAny<LabelCrateJob>(),
            It.IsAny<CancellationToken>()), Times.Never);

        releases[1].Status = JobStatus.Failed;
        var changed = await service.GetJobAsync("job-1");

        Assert.NotNull(changed);
        Assert.Equal(JobStatus.Failed, changed!.Status);
        Assert.Equal(1, changed.FailedReleases);
        hashDb.Verify(value => value.UpsertLabelCrateJobAsync(
            It.Is<LabelCrateJob>(persisted =>
                persisted.Status == JobStatus.Failed &&
                persisted.FailedReleases == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
