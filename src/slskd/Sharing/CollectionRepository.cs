// <copyright file="CollectionRepository.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of ICollectionRepository.</summary>
public sealed class CollectionRepository : ICollectionRepository
{
    private const int ReorderBatchSize = 400;

    private readonly IDbContextFactory<CollectionsDbContext> _factory;

    public CollectionRepository(IDbContextFactory<CollectionsDbContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Collections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Collection>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Collections.AsNoTracking().Where(x => x.OwnerUserId == ownerUserId).OrderBy(x => x.Title).ToListAsync(cancellationToken);
    }

    public async Task<Collection> AddAsync(Collection entity, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        entity.CreatedAt = entity.UpdatedAt = DateTime.UtcNow;
        db.Collections.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Collection entity, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        entity.UpdatedAt = DateTime.UtcNow;
        db.Collections.Update(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Collections
            .Where(collection => collection.Id == id)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<CollectionItem>> GetItemsAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.CollectionItems.AsNoTracking().Where(x => x.CollectionId == collectionId).OrderBy(x => x.Ordinal).ToListAsync(cancellationToken);
    }

    public async Task<CollectionItem?> GetItemAsync(Guid collectionId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.CollectionItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CollectionId == collectionId && item.Id == itemId, cancellationToken);
    }

    public async Task<CollectionItem> AddItemAsync(CollectionItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var parameters = new object[]
        {
            new SqliteParameter("@id", item.Id),
            new SqliteParameter("@collection_id", item.CollectionId),
            new SqliteParameter("@content_id", item.ContentId),
            new SqliteParameter("@media_kind", item.MediaKind ?? (object)DBNull.Value),
            new SqliteParameter("@file_name", item.FileName ?? (object)DBNull.Value),
            new SqliteParameter("@title", item.Title ?? (object)DBNull.Value),
            new SqliteParameter("@artist", item.Artist ?? (object)DBNull.Value),
            new SqliteParameter("@album", item.Album ?? (object)DBNull.Value),
            new SqliteParameter("@content_hash", item.ContentHash ?? (object)DBNull.Value),
        };

        try
        {
            var ordinals = await db.Database.SqlQueryRaw<int>("""
                INSERT INTO "CollectionItems" (
                    "Id", "CollectionId", "Ordinal", "ContentId", "MediaKind",
                    "FileName", "Title", "Artist", "Album", "ContentHash")
                SELECT
                    @id,
                    @collection_id,
                    COALESCE(MAX("Ordinal"), -1) + 1,
                    @content_id,
                    @media_kind,
                    @file_name,
                    @title,
                    @artist,
                    @album,
                    @content_hash
                FROM "CollectionItems"
                WHERE "CollectionId" = @collection_id
                RETURNING "Ordinal" AS "Value"
                """, parameters)
                .ToListAsync(cancellationToken);
            item.Ordinal = ordinals.Single();
        }
        catch (DbException ex)
        {
            throw new DbUpdateException("An error occurred while saving the entity changes.", ex);
        }

        return item;
    }

    public async Task UpdateItemAsync(CollectionItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.CollectionItems.Update(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.CollectionItems
            .Where(item => item.Id == itemId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task ReorderItemsAsync(Guid collectionId, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default)
    {
        var orderedItems = itemIdsInOrder
            .Select((id, ordinal) => (Id: id, Ordinal: ordinal))
            .GroupBy(item => item.Id)
            .Select(group => group.Last())
            .OrderBy(item => item.Ordinal)
            .ToArray();
        if (orderedItems.Length == 0)
        {
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var batch in orderedItems.Chunk(ReorderBatchSize))
        {
            var cases = batch.Select((_, index) => $"WHEN @id{index} THEN {batch[index].Ordinal}");
            var ids = batch.Select((_, index) => $"@id{index}");
            var commandText = $"""
                UPDATE "CollectionItems"
                SET "Ordinal" = CASE "Id" {string.Join(" ", cases)} ELSE "Ordinal" END
                WHERE "CollectionId" = @collection_id
                  AND "Id" IN ({string.Join(", ", ids)})
                """;
            var parameters = new List<object>(batch.Length + 1)
            {
                new SqliteParameter("@collection_id", collectionId),
            };
            parameters.AddRange(batch.Select((item, index) => new SqliteParameter($"@id{index}", item.Id)));

            await db.Database.ExecuteSqlRawAsync(commandText, parameters, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
