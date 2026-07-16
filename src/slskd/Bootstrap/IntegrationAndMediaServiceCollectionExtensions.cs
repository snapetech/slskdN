// <copyright file="IntegrationAndMediaServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.AudioCore;
using slskd.Integrations.AcoustId;
using slskd.Integrations.AutoTagging;
using slskd.Integrations.Chromaprint;
using slskd.Integrations.FTP;
using slskd.Integrations.MetadataFacade;
using slskd.Integrations.MusicBrainz;
using slskd.Integrations.Pushbullet;
using slskd.Relay;
using slskd.Transfers.Downloads;
using slskd.Transfers.Rescue;

public static class IntegrationAndMediaServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdIntegrationAndMediaServices(this IServiceCollection services)
    {
        // Wishlist services
        var wishlistDbPath = Path.Combine(Program.AppDirectory, "wishlist.db");
        services.AddDbContextFactory<Wishlist.WishlistDbContext>(options =>
        {
            options.UseSqlite($"Data Source={wishlistDbPath}");
        });

        // Ensure wishlist database is created
        using (var wishlistContext = new Wishlist.WishlistDbContext(
            new DbContextOptionsBuilder<Wishlist.WishlistDbContext>()
                .UseSqlite($"Data Source={wishlistDbPath}")
                .Options))
        {
            wishlistContext.Database.EnsureCreated();
        }

        // Apply wishlist schema migration for columns added after initial EnsureCreated
        var wishlistMigration = new Migrations.Z05182026_WishlistItemViewingAndDownloadLimitsMigration($"Data Source={wishlistDbPath}");
        wishlistMigration.Apply();
        new Migrations.Z07142026_WishlistIgnoredResultsMigration($"Data Source={wishlistDbPath}").Apply();
        new Migrations.Z07162026_WishlistSearchTextIndexMigration($"Data Source={wishlistDbPath}").Apply();

        services.AddSingleton<Wishlist.IWishlistService, Wishlist.WishlistService>();
        services.AddHostedService(provider => (Wishlist.WishlistService)provider.GetRequiredService<Wishlist.IWishlistService>());
        services.AddSingleton<SourceFeeds.ISpotifyConnectionService, SourceFeeds.SpotifyConnectionService>();
        services.AddSingleton<SourceFeeds.ISourceFeedImportService, SourceFeeds.SourceFeedImportService>();

        // Auto-replace services
        services.AddSingleton<Transfers.AutoReplace.IAutoReplaceService, Transfers.AutoReplace.AutoReplaceService>();
        services.AddSingleton<Transfers.AutoReplace.AutoReplaceBackgroundService>();
        services.AddHostedService(provider => provider.GetRequiredService<Transfers.AutoReplace.AutoReplaceBackgroundService>());

        // Auto-retry: re-enqueue failed downloads automatically
        services.AddHostedService<Transfers.Downloads.DownloadAutoRetryService>();

        services.AddSingleton<IRelayService, RelayService>();

        // HARDENING-2026-04-20 H8: loud, periodic reminder when relay controller TLS validation is reduced.
        services.AddHostedService<Relay.RelayTlsWarningService>();

        // HARDENING-2026-04-20 H12: loud, periodic reminder that public DHT rendezvous publishes this node's IP.
        services.AddHostedService<DhtRendezvous.DhtExposureWarningService>();

        services.AddSingleton<IFTPClientFactory, FTPClientFactory>();
        services.AddSingleton<IFTPService, FTPService>();

        // AudioCore: IChromaprintService, IFingerprintExtractionService in AddAudioCore
        services.AddSingleton<IAcoustIdClient, AcoustIdClient>();
        services.AddSingleton<IAutoTaggingService, AutoTaggingService>();
        services.AddSingleton<IMusicBrainzClient, MusicBrainzClient>();
        services.AddSingleton<Integrations.Brainz.IBrainzClient, Integrations.Brainz.BrainzClient>();
        services.AddAudioCore(Program.AppDirectory);
        services.AddSingleton<IMetadataFacade>(sp => new MetadataFacade(
            sp.GetRequiredService<IMusicBrainzClient>(),
            sp.GetRequiredService<IAcoustIdClient>(),
            sp.GetRequiredService<IFingerprintExtractionService>(),
            sp.GetRequiredService<IOptionsMonitor<slskd.Options>>(),
            sp.GetRequiredService<ILogger<MetadataFacade>>(),
            sp.GetService<IMemoryCache>()));
        services.AddSlskdSongId();
        services.AddSingleton<DiscoveryGraph.IDiscoveryGraphService, DiscoveryGraph.DiscoveryGraphService>();
        services.AddSingleton<IPushbulletService, PushbulletService>();
        services.AddSingleton<Integrations.Notifications.INotificationService, Integrations.Notifications.NotificationService>();

        return services;
    }
}
