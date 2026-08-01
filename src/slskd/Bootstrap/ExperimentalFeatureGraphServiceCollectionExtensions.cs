// <copyright file="ExperimentalFeatureGraphServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ExperimentalFeatureGraphServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdExperimentalFeatureGraph(
        this IServiceCollection services,
        IConfiguration configuration,
        slskd.Options optionsAtStartup)
    {
        services.AddSlskdMultiSourceFeatureServices(optionsAtStartup);
        services.AddSlskdVirtualSoulfindServices(optionsAtStartup);

        services.AddSlskdMediaCorePodServices(optionsAtStartup);

        services.AddSlskdExperimentalMeshServices(configuration, optionsAtStartup);

        services.AddSlskdCapabilitiesAndRendezvousServices(optionsAtStartup);

        services.AddSlskdTransferDiscoveryServices(optionsAtStartup);

        services.AddSlskdIntegrationAndMediaServices();

        return services;
    }
}
