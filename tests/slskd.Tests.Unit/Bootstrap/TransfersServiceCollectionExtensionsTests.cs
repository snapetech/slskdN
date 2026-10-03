// <copyright file="TransfersServiceCollectionExtensionsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Bootstrap;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using slskd.Bootstrap;
using slskd.Transfers;
using slskd.Transfers.AutoReplace;
using slskd.Transfers.Downloads;
using slskd.Transfers.Uploads;
using Xunit;

public class TransfersServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSlskdTransfers_RegistersTransferServicesOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdTransfers();
        services.AddSlskdTransferHostedServices();

        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IDownloadService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IUploadService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(ITransferService)));
        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(IAutoReplaceService)));
    }

    [Fact]
    public void AddSlskdTransferHostedServices_RegistersHostedTransferServicesOnce()
    {
        var services = new ServiceCollection();

        services.AddSlskdTransferHostedServices();

        Assert.Single(services.Where(descriptor => descriptor.ServiceType == typeof(AutoReplaceBackgroundService)));
        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)));
    }
}
