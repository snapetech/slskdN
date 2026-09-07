// <copyright file="IUserBlockService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Users.Notes;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Stores and reads durable Soulseek user blocks.
/// </summary>
public interface IUserBlockService
{
    /// <summary>
    ///     Gets every blocked user.
    /// </summary>
    Task<IReadOnlyList<UserBlock>> GetAllBlocksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets blocked usernames for result filtering.
    /// </summary>
    Task<IReadOnlySet<string>> GetBlockedUsernamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a block, returning the existing block when it is already present.
    /// </summary>
    Task<UserBlock> BlockAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a block when it exists.
    /// </summary>
    Task UnblockAsync(string username, CancellationToken cancellationToken = default);
}
