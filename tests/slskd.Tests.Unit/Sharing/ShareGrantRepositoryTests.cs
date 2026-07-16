// <copyright file="ShareGrantRepositoryTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Sharing;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using slskd.Sharing;
using Xunit;

public sealed class ShareGrantRepositoryTests : IDisposable
{
    private readonly string _dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
    private readonly CommandCaptureInterceptor _interceptor = new();
    private readonly ShareGrantMaterializationInterceptor _materialization = new();
    private readonly IDbContextFactory<CollectionsDbContext> _factory;

    public ShareGrantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CollectionsDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .AddInterceptors(_interceptor, _materialization)
            .Options;
        _factory = new TestDbContextFactory(options);

        using var db = new CollectionsDbContext(options);
        db.Database.EnsureCreated();
        _interceptor.Commands.Clear();
    }

    public void Dispose()
    {
        if (System.IO.File.Exists(_dbPath))
            System.IO.File.Delete(_dbPath);
    }

    [Fact]
    public async Task GetAccessibleByIdAsync_DirectGrant_HydratesOnlyTarget()
    {
        var collectionId = Guid.NewGuid();
        var target = CreateGrant(collectionId, AudienceTypes.User, "alice");
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Collections.Add(new Collection { Id = collectionId, OwnerUserId = "owner", Title = "Test" });
            db.ShareGrants.Add(target);
            db.ShareGrants.AddRange(Enumerable.Range(0, 1000)
                .Select(_ => CreateGrant(collectionId, AudienceTypes.User, "alice")));
            await db.SaveChangesAsync();
        }

        _interceptor.Commands.Clear();
        _materialization.Count = 0;
        var result = await new ShareGrantRepository(_factory).GetAccessibleByIdAsync(target.Id, "alice");

        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
        Assert.Equal(1, _materialization.Count);
        var command = Assert.Single(_interceptor.Commands);
        Assert.Contains("\"Id\"", command, StringComparison.Ordinal);
        Assert.Contains("LIMIT 1", command, StringComparison.OrdinalIgnoreCase);

        _interceptor.Commands.Clear();
        _materialization.Count = 0;
        Assert.Null(await new ShareGrantRepository(_factory).GetAccessibleByIdAsync(target.Id, "bob"));
        Assert.Equal(0, _materialization.Count);
        Assert.Single(_interceptor.Commands);
    }

    [Fact]
    public async Task GetAccessibleByIdAsync_GroupGrant_QueriesOnlyTargetMembership()
    {
        var collectionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var grant = CreateGrant(collectionId, AudienceTypes.ShareGroup, groupId.ToString());
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Collections.Add(new Collection { Id = collectionId, OwnerUserId = "owner", Title = "Test" });
            db.ShareGroups.Add(new ShareGroup { Id = groupId, Name = "Group", OwnerUserId = "owner" });
            db.ShareGroupMembers.Add(new ShareGroupMember { ShareGroupId = groupId, UserId = "alice" });
            db.ShareGrants.Add(grant);
            await db.SaveChangesAsync();
        }

        _interceptor.Commands.Clear();
        _materialization.Count = 0;
        var result = await new ShareGrantRepository(_factory).GetAccessibleByIdAsync(grant.Id, "alice");

        Assert.NotNull(result);
        Assert.Equal(grant.Id, result.Id);
        Assert.Equal(1, _materialization.Count);
        Assert.Equal(2, _interceptor.Commands.Count);

        _interceptor.Commands.Clear();
        _materialization.Count = 0;
        Assert.Null(await new ShareGrantRepository(_factory).GetAccessibleByIdAsync(grant.Id, "bob"));
        Assert.Equal(1, _materialization.Count);
        Assert.Equal(2, _interceptor.Commands.Count);
    }

    [Fact]
    public async Task GetAccessibleByUserAsync_BatchesGroupMembershipLookup()
    {
        var collectionId = Guid.NewGuid();
        var memberGroupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var directGrant = CreateGrant(collectionId, AudienceTypes.User, "alice");
        var memberGrant = CreateGrant(collectionId, AudienceTypes.ShareGroup, memberGroupId.ToString());
        var duplicateMemberGrant = CreateGrant(collectionId, AudienceTypes.ShareGroup, memberGroupId.ToString());
        var otherGroupGrant = CreateGrant(collectionId, AudienceTypes.ShareGroup, otherGroupId.ToString());
        var malformedGroupGrant = CreateGrant(collectionId, AudienceTypes.ShareGroup, "not-a-guid");
        var expiredGrant = CreateGrant(collectionId, AudienceTypes.User, "alice", DateTime.UtcNow.AddMinutes(-1));

        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Collections.Add(new Collection { Id = collectionId, OwnerUserId = "owner", Title = "Test" });
            db.ShareGroups.AddRange(
                new ShareGroup { Id = memberGroupId, Name = "Member", OwnerUserId = "owner" },
                new ShareGroup { Id = otherGroupId, Name = "Other", OwnerUserId = "owner" });
            db.ShareGroupMembers.Add(new ShareGroupMember { ShareGroupId = memberGroupId, UserId = "alice" });
            db.ShareGrants.AddRange(
                directGrant,
                memberGrant,
                duplicateMemberGrant,
                otherGroupGrant,
                malformedGroupGrant,
                expiredGrant,
                CreateGrant(collectionId, AudienceTypes.User, "bob"));
            await db.SaveChangesAsync();
        }

        _interceptor.Commands.Clear();
        var result = await new ShareGrantRepository(_factory).GetAccessibleByUserAsync("alice");

        Assert.Equal(new[] { directGrant.Id, memberGrant.Id, duplicateMemberGrant.Id }.OrderBy(id => id), result.Select(grant => grant.Id).OrderBy(id => id));
        Assert.Equal(2, _interceptor.Commands.Count);
    }

    private static ShareGrant CreateGrant(Guid collectionId, string audienceType, string audienceId, DateTime? expiryUtc = null) =>
        new()
        {
            CollectionId = collectionId,
            AudienceType = audienceType,
            AudienceId = audienceId,
            ExpiryUtc = expiryUtc,
        };

    private sealed class TestDbContextFactory(DbContextOptions<CollectionsDbContext> options)
        : IDbContextFactory<CollectionsDbContext>
    {
        public CollectionsDbContext CreateDbContext() => new(options);

        public ValueTask<CollectionsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ShareGrantMaterializationInterceptor : IMaterializationInterceptor
    {
        public int Count { get; set; }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            if (entity is ShareGrant)
            {
                Count++;
            }

            return entity;
        }
    }
}
