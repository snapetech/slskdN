// <copyright file="TelemetryServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using slskd.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<PrometheusService>();
        services.AddSingleton<ReportsService>();
        services.AddSingleton<TelemetryService>();

        return services;
    }
}
