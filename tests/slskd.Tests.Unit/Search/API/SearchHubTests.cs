// <copyright file="SearchHubTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Search.API;

using Microsoft.AspNetCore.SignalR;
using Moq;
using slskd.Search;
using slskd.Search.API;
using Xunit;

public class SearchHubTests
{
    [Fact]
    public async Task OnConnectedAsync_SendsOneBoundedInitialSnapshot()
    {
        var searches = new List<Search>();
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(service => service.ListAsync(null, 500, 0, null))
            .ReturnsAsync(searches);
        var caller = new Mock<ISingleClientProxy>();
        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(value => value.Caller).Returns(caller.Object);
        var hub = new SearchHub(searchService.Object)
        {
            Clients = clients.Object,
        };

        await hub.OnConnectedAsync();

        searchService.Verify(service => service.ListAsync(null, 500, 0, null), Times.Once);
        searchService.VerifyNoOtherCalls();
        caller.Verify(
            client => client.SendCoreAsync(
                SearchHubMethods.List,
                It.Is<object?[]>(arguments => arguments.Length == 1 && ReferenceEquals(arguments[0], searches)),
                default),
            Times.Once);
    }
}
