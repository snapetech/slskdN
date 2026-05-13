// <copyright file="RuntimeServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using slskd.Common.Security;

public static class RuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdRuntimeServices(
        this IServiceCollection services,
        IConfiguration configuration,
        OptionsAtStartup optionsAtStartup,
        string dataDirectory,
        int soulseekMinorVersion)
    {
        Log.Debug("[DI] Starting AddSlskdRuntimeServices...");

        services.AddSlskdApplicationHost(configuration, Program.AppName, optionsAtStartup, soulseekMinorVersion);
        services.AddSlskdCoreApplicationServices(optionsAtStartup, dataDirectory);
        services.AddSlskdExperimentalFeatureGraph(configuration, optionsAtStartup);
        services.AddSlskdUserData();

        Log.Debug("[DI] About to call AddSlskdnSecurity...");
        services.AddSlskdnSecurity(configuration);
        Log.Debug("[DI] AddSlskdnSecurity completed");

        return services;
    }
}
