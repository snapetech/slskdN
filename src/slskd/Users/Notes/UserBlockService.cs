// <copyright file="UserBlockService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Users.Notes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
///     Entity Framework implementation of <see cref="IUserBlockService"/>.
/// </summary>
public sealed class UserBlockService : IUserBlockService
{
    private readonly IDbContextFactory<UserNotesDbContext> contextFactory;
    private readonly ILogger<UserBlockService> logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserBlockService"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="logger">The logger.</param>
    public UserBlockService(
        IDbContextFactory<UserNotesDbContext> contextFactory,
        ILogger<UserBlockService> logger)
    {
        this.contextFactory = contextFactory;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserBlock>> GetAllBlocksAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserBlocks
            .AsNoTracking()
            .OrderBy(block => block.Username)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlySet<string>> GetBlockedUsernamesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var usernames = await context.UserBlocks
            .AsNoTracking()
            .Select(block => block.Username)
            .ToListAsync(cancellationToken);
        return usernames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<UserBlock> BlockAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.UserBlocks.FindAsync([username], cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var block = new UserBlock { Username = username, CreatedAt = DateTime.UtcNow };
        context.UserBlocks.Add(block);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Blocked user {Username}", username);
        return block;
    }

    /// <inheritdoc/>
    public async Task UnblockAsync(string username, CancellationToken cancellationToken = default)
    {
        username = NormalizeUsername(username);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.UserBlocks.FindAsync([username], cancellationToken);
        if (existing == null)
        {
            return;
        }

        context.UserBlocks.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Unblocked user {Username}", username);
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = username?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        return normalized;
    }
}
