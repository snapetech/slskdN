// <copyright file="SearchCompatibilityControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.API.Compatibility;

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.API.Compatibility;
using slskd.Search;
using Soulseek;
using Xunit;
using SlskdSearch = slskd.Search.Search;

public class SearchCompatibilityControllerTests
{
    [Fact]
    public async Task Search_EscapesLogBreakingQueryWithoutChangingSearchInput()
    {
        var query = "alpha\r\nforged";
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync(new SlskdSearch
            {
                Id = Guid.NewGuid(),
                SearchText = query,
                Responses = Array.Empty<slskd.Search.Response>()
            });
        var logger = new Mock<ILogger<SearchCompatibilityController>>();
        logger.Setup(entry => entry.IsEnabled(LogLevel.Information)).Returns(true);
        var controller = new SearchCompatibilityController(searchService.Object, logger.Object);

        await controller.Search(new SearchRequest(query, null, 10), CancellationToken.None);

        searchService.Verify(
            service => service.StartAsync(
                It.IsAny<Guid>(),
                It.Is<SearchQuery>(searchQuery => searchQuery.Terms.SequenceEqual(new[] { query })),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()),
            Times.Once);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("alpha\\r\\nforged") &&
                    !state.ToString()!.Contains(query)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_WithNullRequest_ReturnsBadRequest()
    {
        var controller = new SearchCompatibilityController(
            Mock.Of<ISearchService>(),
            NullLogger<SearchCompatibilityController>.Instance);

        var result = await controller.Search(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Search_TrimsQueryBeforeDispatch()
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync(new SlskdSearch
            {
                Id = Guid.NewGuid(),
                SearchText = "hello world",
                Responses = Array.Empty<slskd.Search.Response>()
            });

        var controller = new SearchCompatibilityController(
            searchService.Object,
            NullLogger<SearchCompatibilityController>.Instance);

        var result = await controller.Search(new SearchRequest("  hello world  ", null, 10), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        searchService.Verify(
            service => service.StartAsync(
                It.IsAny<Guid>(),
                It.Is<SearchQuery>(query => query.Terms.SequenceEqual(new[] { "hello", "world" })),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_WithNonPositiveLimit_ReturnsBadRequest()
    {
        var searchService = new Mock<ISearchService>();
        var controller = new SearchCompatibilityController(
            searchService.Object,
            NullLogger<SearchCompatibilityController>.Instance);

        var result = await controller.Search(new SearchRequest("hello", null, 0), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        searchService.Verify(
            service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Search_WhenSearchServiceThrows_DoesNotLeakExceptionMessage()
    {
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.StartAsync(
                It.IsAny<Guid>(),
                It.IsAny<SearchQuery>(),
                It.IsAny<SearchScope>(),
                It.IsAny<SearchOptions>(),
                It.IsAny<List<string>>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = new SearchCompatibilityController(
            searchService.Object,
            NullLogger<SearchCompatibilityController>.Instance);

        var result = await controller.Search(new SearchRequest("hello", null, 5), CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
    }
}
