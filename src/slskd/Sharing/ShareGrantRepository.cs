// <copyright file="ShareGrantRepository.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of IShareGrantRepository.</summary>
public sealed class ShareGrantRepository : IShareGrantRepository
{
    private readonly IDbContextFactory<CollectionsDbContext> _factory;

    public ShareGrantRepository(IDbContextFactory<CollectionsDbContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<ShareGrant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.ShareGrants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ShareGrant>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.ShareGrants.AsNoTracking().Where(x => x.CollectionId == collectionId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShareGrant>> GetAccessibleByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var candidates = await db.ShareGrants.AsNoTracking()
            .Where(g =>
                (g.ExpiryUtc == null || g.ExpiryUtc > now) &&
                ((g.AudienceType == AudienceTypes.User && g.AudienceId == userId) ||
                 g.AudienceType == AudienceTypes.ShareGroup))
            .ToListAsync(cancellationToken);

        var direct = candidates.Where(g => g.AudienceType == AudienceTypes.User).ToList();
        var groupGrants = candidates.Where(g => g.AudienceType == AudienceTypes.ShareGroup).ToList();
        var result = new List<ShareGrant>(direct);
        var groupIds = groupGrants
            .Select(g => Guid.TryParse(g.AudienceId, out var groupId) ? groupId : (Guid?)null)
            .Where(groupId => groupId.HasValue)
            .Select(groupId => groupId!.Value)
            .Distinct()
            .ToList();

        if (groupIds.Count == 0)
            return result;

        var memberGroupIds = await db.ShareGroupMembers.AsNoTracking()
            .Where(member => member.UserId == userId && groupIds.Contains(member.ShareGroupId))
            .Select(member => member.ShareGroupId)
            .ToHashSetAsync(cancellationToken);

        foreach (var grant in groupGrants)
        {
            if (Guid.TryParse(grant.AudienceId, out var groupId) && memberGroupIds.Contains(groupId))
                result.Add(grant);
        }

        return result;
    }

    public async Task<ShareGrant> AddAsync(ShareGrant entity, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        entity.CreatedAt = entity.UpdatedAt = DateTime.UtcNow;
        db.ShareGrants.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(ShareGrant entity, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        entity.UpdatedAt = DateTime.UtcNow;
        db.ShareGrants.Update(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var e = await db.ShareGrants.FindAsync([id], cancellationToken);
        if (e == null) return false;
        db.ShareGrants.Remove(e);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
