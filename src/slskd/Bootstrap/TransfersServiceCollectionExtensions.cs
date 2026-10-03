// <copyright file="TransfersServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using slskd.Events;
using slskd.Files;
using slskd.Integrations.FTP;
using slskd.Relay;
using slskd.Shares;
using slskd.Transfers;
using slskd.Transfers.AutoReplace;
using slskd.Transfers.Downloads;
using slskd.Transfers.Uploads;
using slskd.Users;
using Soulseek;

public static class TransfersServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdTransfers(this IServiceCollection services)
    {
        services.AddSingleton<IScheduledRateLimitService, ScheduledRateLimitService>();
        services.AddSingleton<IDownloadService>(sp => new DownloadService(
            sp.GetRequiredService<IOptionsMonitor<slskd.Options>>(),
            sp.GetRequiredService<ISoulseekClient>(),
            sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>(),
            sp.GetRequiredService<FileService>(),
            sp.GetRequiredService<IRelayService>(),
            sp.GetRequiredService<IFTPService>(),
            sp.GetRequiredService<EventBus>(),
            sp.GetService<Transfers.MultiSource.Metrics.IPeerMetricsService>()));
        services.AddSingleton<IUploadService>(sp => new UploadService(
            sp.GetRequiredService<FileService>(),
            sp.GetRequiredService<IUserService>(),
            sp.GetRequiredService<ISoulseekClient>(),
            sp.GetRequiredService<IOptionsMonitor<slskd.Options>>(),
            sp.GetRequiredService<IShareService>(),
            sp.GetRequiredService<IRelayService>(),
            sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>(),
            sp.GetRequiredService<EventBus>(),
            sp.GetRequiredService<Transfers.MultiSource.Metrics.ITrafficAccountingService>(),
            sp.GetService<IScheduledRateLimitService>()));
        services.AddSingleton<ITransferService>(sp => new TransferService(
            sp.GetRequiredService<IUploadService>(),
            sp.GetRequiredService<IDownloadService>(),
            sp.GetRequiredService<IDbContextFactory<TransfersDbContext>>()));
        services.AddSingleton<FileService>();
        services.AddSingleton<IAutoReplaceService, AutoReplaceService>();

        return services;
    }

    public static IServiceCollection AddSlskdTransferHostedServices(this IServiceCollection services)
    {
        services.AddSingleton<AutoReplaceBackgroundService>();
        services.AddHostedService(provider => provider.GetRequiredService<AutoReplaceBackgroundService>());
        services.AddHostedService<DownloadAutoRetryService>();

        return services;
    }
}
