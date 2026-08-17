// <copyright file="WishlistDbContext.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Wishlist
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using slskd.Integrations.Lidarr;

    public class WishlistDbContext : DbContext
    {
        public WishlistDbContext(DbContextOptions<WishlistDbContext> options)
            : base(options)
        {
        }

        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<WishlistIgnoredResult> WishlistIgnoredResults { get; set; }
        public DbSet<LidarrImportHistoryRecord> LidarrImportHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<WishlistItem>()
                .Property(e => e.CreatedAt)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder
                .Entity<WishlistItem>()
                .Property(e => e.LastSearchedAt)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

            modelBuilder
                .Entity<WishlistItem>()
                .Property(e => e.LastViewedAt)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

            modelBuilder
                .Entity<WishlistItem>()
                .HasIndex(e => e.SearchText);

            modelBuilder.Entity<WishlistIgnoredResult>(entity =>
            {
                entity.HasIndex(e => new { e.WishlistItemId, e.Username, e.Directory }).IsUnique();
                entity.Property(e => e.Username).UseCollation("NOCASE");
                entity.Property(e => e.Directory).UseCollation("NOCASE");
                entity.HasOne(e => e.WishlistItem)
                    .WithMany()
                    .HasForeignKey(e => e.WishlistItemId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.CreatedAt)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            });

            modelBuilder.Entity<LidarrImportHistoryRecord>(entity =>
            {
                entity.ToTable("LidarrImportHistory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartedAt)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
                entity.Property(e => e.CompletedAt)
                    .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
                entity.HasIndex(e => e.StartedAt);
                entity.HasIndex(e => new { e.Status, e.CommandId });
            });
        }
    }
}
