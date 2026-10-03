// <copyright file="UserEndPointCacheTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common;

using System.Collections.Generic;
using System.Net;
using Xunit;

public class UserEndPointCacheTests
{
    [Fact]
    public void TryGet_ExplicitEndpointOverrideTakesPrecedenceOverTransientCache()
    {
        var expected = new IPEndPoint(IPAddress.Loopback, 50301);
        var discovered = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 50302);
        var cache = new UserEndPointCache(new[]
        {
            new KeyValuePair<string, IPEndPoint>("mesh-peer", expected),
        });

        cache.AddOrUpdate("MESH-PEER", discovered);

        Assert.True(cache.TryGet("mesh-peer", out var endpoint));
        Assert.Equal(expected, endpoint);
    }
}
