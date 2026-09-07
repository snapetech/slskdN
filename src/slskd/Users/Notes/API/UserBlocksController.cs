// <copyright file="UserBlocksController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Users.Notes.API;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using slskd.Core.Security;

/// <summary>
///     Controller for managing durable user blocks.
/// </summary>
[ApiController]
[ApiVersion("0")]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/users/blocks")]
[Authorize(Policy = AuthPolicy.Any)]
[ValidateCsrfForCookiesOnly]
public sealed class UserBlocksController : ControllerBase
{
    private readonly IUserBlockService userBlockService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserBlocksController"/> class.
    /// </summary>
    /// <param name="userBlockService">The user block service.</param>
    public UserBlocksController(IUserBlockService userBlockService)
    {
        this.userBlockService = userBlockService;
    }

    /// <summary>
    ///     Gets all blocked users.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The blocked users.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserBlock>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await userBlockService.GetAllBlocksAsync(cancellationToken));
    }

    /// <summary>
    ///     Blocks a user. Repeating the request is idempotent.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user block.</returns>
    [HttpPut("{username}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    public async Task<ActionResult<UserBlock>> Block(string username, CancellationToken cancellationToken)
    {
        username = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required.");
        }

        return Ok(await userBlockService.BlockAsync(username, cancellationToken));
    }

    /// <summary>
    ///     Removes a user block. Repeating the request is idempotent.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{username}")]
    [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
    public async Task<ActionResult> Unblock(string username, CancellationToken cancellationToken)
    {
        username = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required.");
        }

        await userBlockService.UnblockAsync(username, cancellationToken);
        return NoContent();
    }
}
