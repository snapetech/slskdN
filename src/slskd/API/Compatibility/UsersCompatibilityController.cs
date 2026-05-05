// <copyright file="UsersCompatibilityController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.API.Compatibility;

using slskd.Common.Security;
using slskd.Core.Security;
using Soulseek;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides user browsing compatibility API.
/// </summary>
[ApiController]
[Route("api/compatibility/users")]
[Produces("application/json")]
[ValidateCsrfForCookiesOnly] // CSRF protection for cookie-based auth (exempts JWT/API key)
public class UsersCompatibilityController : ControllerBase
{
    private readonly ILogger<UsersCompatibilityController> logger;
    private readonly ISoulseekSafetyLimiter safetyLimiter;
    private readonly ISoulseekClient soulseekClient;

    public UsersCompatibilityController(
        ILogger<UsersCompatibilityController> logger,
        ISoulseekClient soulseekClient,
        ISoulseekSafetyLimiter safetyLimiter)
    {
        this.logger = logger;
        this.soulseekClient = soulseekClient;
        this.safetyLimiter = safetyLimiter;
    }

    /// <summary>
    /// Browse user files (slskd compatibility).
    /// </summary>
    [HttpGet("{username}/browse")]
    [Authorize]
    public async Task<IActionResult> BrowseUser(
            string username,
            CancellationToken cancellationToken = default)
    {
        username = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { error = "Username is required" });
        }

        logger.LogInformation("Browse user requested: {Username}", username);

        if (!safetyLimiter.TryConsumeBrowse("compatibility"))
        {
            logger.LogWarning("[SAFETY] Compatibility browse rejected for user='{Username}': rate limit exceeded", username);
            return StatusCode(429, new { error = "Browse rate limit exceeded. See Soulseek safety configuration." });
        }

        try
        {
            var browseResult = await soulseekClient.BrowseAsync(username, cancellationToken: cancellationToken);

            // Convert Soulseek browse result to compatibility format
            var directories = browseResult.Directories.Select(dir => new
            {
                name = dir.Name,
                files = dir.Files.Select(file => new
                {
                    filename = file.Filename,
                    size = file.Size,
                    attributes = new[] { file.Extension }
                }).ToList()
            }).ToList();

            return Ok(new
            {
                username = username,
                directories = directories
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to browse user {Username}", username);
            return StatusCode(500, new { error = "Failed to browse user" });
        }
    }
}
