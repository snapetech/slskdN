// <copyright file="HostDiagnosticsServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using slskd.Core.Diagnostics;

public static class HostDiagnosticsServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdHostDiagnostics(this IServiceCollection services)
    {
        if (Environment.GetEnvironmentVariable("SLSKDN_E2E_TRACE_HOSTED") == "1")
        {
            Console.Error.WriteLine("[HostedServiceTracer] Enabled (SLSKDN_E2E_TRACE_HOSTED=1)");

            var hostedDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IEnumerable<IHostedService>>(sp =>
            {
                var list = new List<IHostedService>();
                foreach (var descriptor in hostedDescriptors)
                {
                    var svcName = descriptor.ImplementationType?.FullName
                                  ?? descriptor.ImplementationInstance?.GetType().FullName
                                  ?? "factory";
                    Console.Error.WriteLine($"[HostedServiceTracer] create {svcName} begin");

                    var svc = descriptor.ImplementationInstance as IHostedService
                              ?? (descriptor.ImplementationFactory?.Invoke(sp) as IHostedService)
                              ?? (IHostedService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
                    list.Add(svc);

                    Console.Error.WriteLine($"[HostedServiceTracer] create {svcName} end");
                }

                return list;
            });

            services.AddSingleton<IHostedService, HostedServiceTracer>();
        }

        services.Configure<HostOptions>(options =>
        {
            options.StartupTimeout = TimeSpan.FromSeconds(30);
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SLSKDN_E2E_CONCURRENT_START")))
            {
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = true;
            }
        });

        return services;
    }
}
