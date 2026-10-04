// <copyright file="SongIdServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using slskd.Bootstrap;
using slskd.SongID;
using Xunit;

public sealed class SongIdServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlskdSongId_RegistersSongIdServicesOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdSongId();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ISongIdRunStore));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ISongIdCapabilityReporter));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ISongIdService));
    }
}
