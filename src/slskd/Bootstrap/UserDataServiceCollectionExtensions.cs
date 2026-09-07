// <copyright file="UserDataServiceCollectionExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Bootstrap;

using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class UserDataServiceCollectionExtensions
{
    public static IServiceCollection AddSlskdUserData(this IServiceCollection services)
    {
        // User Notes services
        var userNotesDbPath = Path.Combine(Program.AppDirectory, "user_notes.db");
        services.AddDbContextFactory<Users.Notes.UserNotesDbContext>(options =>
        {
            options.UseSqlite($"Data Source={userNotesDbPath}");
        });

        // Ensure user notes database is created
        using (var userNotesContext = new Users.Notes.UserNotesDbContext(
            new DbContextOptionsBuilder<Users.Notes.UserNotesDbContext>()
                .UseSqlite($"Data Source={userNotesDbPath}")
                .Options))
        {
            userNotesContext.Database.EnsureCreated();
            userNotesContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS UserBlocks (Username TEXT COLLATE NOCASE NOT NULL PRIMARY KEY, CreatedAt TEXT NOT NULL)");
        }

        services.AddSingleton<Users.Notes.IUserNoteService, Users.Notes.UserNoteService>();
        services.AddSingleton<Users.Notes.IUserBlockService, Users.Notes.UserBlockService>();

        // Collections / sharing (ShareGroup, Collection, ShareGrant) — behind Feature.CollectionsSharing
        var collectionsDbPath = Path.Combine(Program.AppDirectory, "collections.db");
        services.AddDbContextFactory<Sharing.CollectionsDbContext>(options =>
        {
            options.UseSqlite($"Data Source={collectionsDbPath}");
        });
        using (var collectionsContext = new Sharing.CollectionsDbContext(
            new DbContextOptionsBuilder<Sharing.CollectionsDbContext>()
                .UseSqlite($"Data Source={collectionsDbPath}")
                .Options))
        {
            collectionsContext.Database.EnsureCreated();
        }

        services.AddSingleton<Sharing.IShareGroupRepository, Sharing.ShareGroupRepository>();
        services.AddSingleton<Sharing.ICollectionRepository, Sharing.CollectionRepository>();
        services.AddSingleton<Sharing.IShareGrantRepository, Sharing.ShareGrantRepository>();
        services.AddSingleton<Sharing.ISharingService, Sharing.SharingService>();
        services.AddSingleton<Sharing.ShareGrantAnnouncementService>();

        // Best-effort schema upgrade for sharing db (EnsureCreated does not apply schema changes)
        try
        {
            using (var collectionsContext = new Sharing.CollectionsDbContext(
                new DbContextOptionsBuilder<Sharing.CollectionsDbContext>()
                    .UseSqlite($"Data Source={collectionsDbPath}")
                    .Options))
            {
                collectionsContext.Database.ExecuteSqlRaw("ALTER TABLE ShareGrants ADD COLUMN OwnerEndpoint TEXT");
            }
        }
        catch
        {
            // Column already exists or DB is read-only; ignore.
        }

        try
        {
            using (var collectionsContext = new Sharing.CollectionsDbContext(
                new DbContextOptionsBuilder<Sharing.CollectionsDbContext>()
                    .UseSqlite($"Data Source={collectionsDbPath}")
                    .Options))
            {
                collectionsContext.Database.ExecuteSqlRaw("ALTER TABLE ShareGrants ADD COLUMN ShareToken TEXT");
            }
        }
        catch
        {
            // Column already exists or DB is read-only; ignore.
        }

        foreach (var sql in new[]
        {
            "ALTER TABLE CollectionItems ADD COLUMN FileName TEXT",
            "ALTER TABLE CollectionItems ADD COLUMN Title TEXT",
            "ALTER TABLE CollectionItems ADD COLUMN Artist TEXT",
            "ALTER TABLE CollectionItems ADD COLUMN Album TEXT",
            Sharing.CollectionsDbContext.ContentLookupIndexSql,
        })
        {
            try
            {
                using (var collectionsContext = new Sharing.CollectionsDbContext(
                    new DbContextOptionsBuilder<Sharing.CollectionsDbContext>()
                        .UseSqlite($"Data Source={collectionsDbPath}")
                        .Options))
                {
                    collectionsContext.Database.ExecuteSqlRaw(sql);
                }
            }
            catch
            {
                // Column already exists or DB is read-only; ignore.
            }
        }

        // Identity / friends (PeerProfile, Contact) — behind Feature.IdentityFriends
        var identityDbPath = Path.Combine(Program.AppDirectory, "identity.db");
        services.AddDbContextFactory<Identity.IdentityDbContext>(options =>
        {
            options.UseSqlite($"Data Source={identityDbPath}");
        });
        using (var identityContext = new Identity.IdentityDbContext(
            new DbContextOptionsBuilder<Identity.IdentityDbContext>()
                .UseSqlite($"Data Source={identityDbPath}")
                .Options))
        {
            identityContext.Database.EnsureCreated();
        }

        services.AddSingleton<Identity.IContactRepository, Identity.ContactRepository>();
        services.AddSingleton<Identity.IContactService, Identity.ContactService>();
        services.AddSingleton<Identity.IProfileService, Identity.ProfileService>();
        services.AddSingleton<Identity.ILanDiscoveryService, Identity.LanDiscoveryService>();

        // Solid / WebID / Solid-OIDC (optional; gated per-request by Feature.Solid)
        services.AddSingleton<slskd.Solid.ISolidClientIdDocumentService, slskd.Solid.SolidClientIdDocumentService>();
        services.AddSingleton<slskd.Solid.ISolidWebIdResolver, slskd.Solid.SolidWebIdResolver>();
        services.AddSingleton<slskd.Solid.ISolidFetchPolicy, slskd.Solid.SolidFetchPolicy>();

        return services;
    }
}
