// <copyright file="TransferDiscoveryServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using slskd.Backfill;
using slskd.Bootstrap;
using slskd.Mesh;
using slskd.Transfers.MultiSource;
using slskd.Transfers.MultiSource.Discovery;
using slskd.Transfers.MultiSource.Scheduling;
using slskd.Transfers.Rescue;
using Xunit;

public sealed class TransferDiscoveryServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void AddSlskdTransferDiscoveryServices_RegistersDiscoveryOwnersAndGatesBackgroundRescue(
        bool multiSourceDownloadsEnabled,
        int expectedRescueHostedServiceCount)
    {
        var services = new ServiceCollection();
        var options = new slskd.Options
        {
            Feature = new() { MultiSourceDownloads = multiSourceDownloadsEnabled },
        };

        services.AddSlskdTransferDiscoveryServices(options);

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IBackfillSchedulerService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMeshSyncService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ISourceDiscoveryService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMultiSourceDownloadService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IContentVerificationService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IChunkScheduler));
        Assert.Equal(expectedRescueHostedServiceCount, services.Count(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(UnderperformanceDetectorHostedService)));
    }

    [Fact]
    public void DefaultOptions_MatchDocumentedNetworkFeatureDefaults()
    {
        var options = new slskd.Options();

        Assert.True(options.Feature.Mesh);
        Assert.True(options.Feature.Dht);
        Assert.True(options.DhtRendezvous.Enabled);
    }
}
