// <copyright file="WarmCacheControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.API.Native;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using slskd.API.Native;
using slskd.Core;
using slskd.Transfers.MultiSource.Caching;
using Xunit;
using TestOptionsMonitor = slskd.Tests.Unit.TestOptionsMonitor<slskd.Options>;

public class WarmCacheControllerTests
{
    [Fact]
    public async Task SubmitHints_DeduplicatesIdentifiersCaseInsensitively()
    {
        var popularity = new Mock<IWarmCachePopularityService>();
        var controller = new WarmCacheController(
            popularity.Object,
            new TestOptionsMonitor(new slskd.Options
            {
                WarmCache = new WarmCacheOptions { Enabled = true }
            }),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WarmCacheController>>());

        var result = await controller.SubmitHints(
            new WarmCacheHintsRequest(
                MbReleaseIds: new List<string> { " mbid-1 ", "MBID-1" },
                MbArtistIds: new List<string> { " artist-1 ", "ARTIST-1" },
                MbLabelIds: new List<string> { " label-1 ", "LABEL-1" }),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        popularity.Verify(service => service.RecordAccessesAsync(
            It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[]
            {
                "mb:release:mbid-1",
                "mb:artist:artist-1",
                "mb:label:label-1",
            })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitHints_MoreThanOneHundredItems_ReturnsPayloadTooLargeWithoutWork()
    {
        var popularity = new Mock<IWarmCachePopularityService>();
        var controller = CreateController(popularity);
        var request = new WarmCacheHintsRequest(
            MbReleaseIds: Enumerable.Range(0, 101).Select(index => $"release-{index}").ToList());

        var result = await controller.SubmitHints(request, CancellationToken.None);

        var rejected = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, rejected.StatusCode);
        popularity.Verify(service => service.RecordAccessesAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitHints_SubmitsMaximumRequestAsOneBatch()
    {
        var popularity = new Mock<IWarmCachePopularityService>();
        var controller = CreateController(popularity);

        var result = await controller.SubmitHints(
            new WarmCacheHintsRequest(
                MbReleaseIds: Enumerable.Range(0, 100).Select(index => $"release-{index}").ToList()),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        popularity.Verify(service => service.RecordAccessesAsync(
            It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static WarmCacheController CreateController(Mock<IWarmCachePopularityService> popularity)
    {
        return new WarmCacheController(
            popularity.Object,
            new TestOptionsMonitor(new slskd.Options
            {
                WarmCache = new WarmCacheOptions { Enabled = true }
            }),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WarmCacheController>>());
    }
}
