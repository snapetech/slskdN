// <copyright file="VirtualSoulfindServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using slskd.Bootstrap;
using slskd.Core;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

public sealed class VirtualSoulfindServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddSlskdVirtualSoulfindServices_RegistersOneConfiguredDhtRateLimiter()
    {
        var options = new slskd.Options
        {
            VirtualSoulfind = new VirtualSoulfindOptions
            {
                ShadowIndex = new ShadowIndexOptions
                {
                    MaxDhtOperationsPerMinute = 1,
                },
            },
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptionsMonitor<slskd.Options>>(new TestOptionsMonitor<slskd.Options>(options));
        services.AddSlskdVirtualSoulfindServices();
        await using var provider = services.BuildServiceProvider();

        var registrations = provider.GetServices<IDhtRateLimiter>().ToList();
        var limiter = Assert.Single(registrations);

        Assert.True(await limiter.TryAcquireAsync(CancellationToken.None));
        Assert.False(await limiter.TryAcquireAsync(CancellationToken.None));
    }
}
