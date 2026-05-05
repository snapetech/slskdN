// <copyright file="WishlistSearchSchedulerTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// </copyright>

namespace Soulseek.Tests.Unit
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    public class WishlistSearchSchedulerTests
    {
        [Fact(DisplayName = "Wishlist scheduler searches each configured term using wishlist scope")]
        public async Task Wishlist_Scheduler_Searches_Each_Configured_Term_Using_Wishlist_Scope()
        {
            var client = new Mock<ISoulseekClient>();
            var callCount = 0;
            client.SetupGet(m => m.ServerInfo).Returns(new ServerInfo(wishlistInterval: 1));
            client.Setup(m => m.SearchAsync(
                    It.IsAny<SearchQuery>(),
                    It.IsAny<SearchScope>(),
                    It.IsAny<int?>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken?>()))
                .Callback(() => callCount++)
                .Returns(Task.FromResult((new Search(SearchQuery.FromText("x"), SearchScope.Wishlist, 1, SearchStates.Completed, 0, 0, 0), (IReadOnlyCollection<SearchResponse>)new List<SearchResponse>())));

            var finished = new TaskCompletionSource<bool>();
            using (var cts = new CancellationTokenSource())
            using (var scheduler = new WishlistSearchScheduler(
                client.Object,
                new[] { "alpha", "beta" },
                new WishlistSearchSchedulerOptions(System.TimeSpan.FromMilliseconds(250), System.TimeSpan.Zero)))
            {
                scheduler.SearchCompleted += (_, __) =>
                {
                    if (callCount >= 2)
                    {
                        finished.TrySetResult(true);
                        cts.Cancel();
                    }
                };

                scheduler.Start(cts.Token);
                await finished.Task;
            }

            client.Verify(m => m.SearchAsync(
                    It.IsAny<SearchQuery>(),
                    It.Is<SearchScope>(s => s.Type == SearchScopeType.Wishlist),
                    It.IsAny<int?>(),
                    It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken?>()),
                Times.Exactly(2));
        }
    }
}
