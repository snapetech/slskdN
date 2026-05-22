// <copyright file="DownloadRequestsController.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Transfers.Downloads.API
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Serilog;
    using slskd.Core.Security;

    /// <summary>
    ///     Request-level download endpoints. A DownloadRequest is the stable, user-facing
    ///     unit ("the file I asked for"); the legacy <c>/api/v0/transfers/downloads</c>
    ///     endpoints expose attempt-level Transfer records and are now deprecated.
    /// </summary>
    [Route("api/v{version:apiVersion}/downloads/requests")]
    [ApiVersion("0")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ValidateCsrfForCookiesOnly]
    public class DownloadRequestsController : ControllerBase
    {
        public DownloadRequestsController(
            IDbContextFactory<TransfersDbContext> contextFactory,
            IDownloadService downloadService)
        {
            ContextFactory = contextFactory;
            Downloads = downloadService;
        }

        private IDbContextFactory<TransfersDbContext> ContextFactory { get; }
        private IDownloadService Downloads { get; }
        private ILogger Log { get; } = Serilog.Log.ForContext<DownloadRequestsController>();

        /// <summary>
        ///     Lists download requests, newest first, with summary info about their current attempt.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = AuthPolicy.Any)]
        [ProducesResponseType(typeof(List<DownloadRequestSummary>), 200)]
        public async Task<IActionResult> List([FromQuery] string? state = null)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();

            var query = context.DownloadRequests.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(state) && Enum.TryParse<DownloadRequestState>(state, ignoreCase: true, out var parsed))
            {
                query = query.Where(r => r.State == parsed);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            var ids = requests.Select(r => r.Id).ToList();

            var attempts = await context.Transfers
                .AsNoTracking()
                .Where(t => t.RequestId.HasValue && ids.Contains(t.RequestId!.Value))
                .ToListAsync();

            var byRequest = attempts.ToLookup(t => t.RequestId!.Value);
            var summaries = requests.Select(r => BuildSummary(r, byRequest[r.Id])).ToList();

            return Ok(summaries);
        }

        /// <summary>
        ///     Gets a single download request including every attempt that has been made.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthPolicy.Any)]
        [ProducesResponseType(typeof(DownloadRequestDetail), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var request = await context.DownloadRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                return NotFound();
            }

            var attempts = await context.Transfers
                .AsNoTracking()
                .Where(t => t.RequestId == id)
                .OrderByDescending(t => t.RequestedAt)
                .ToListAsync();

            return Ok(new DownloadRequestDetail
            {
                Request = request,
                Attempts = attempts,
                Current = attempts.FirstOrDefault(t => !t.Removed) ?? attempts.FirstOrDefault(),
            });
        }

        /// <summary>
        ///     Renames a download request. Affects only the user-facing label; the
        ///     underlying file path is set by the path template at completion time
        ///     and is not retroactively changed.
        /// </summary>
        [HttpPatch("{id:guid}/name")]
        [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
        [ProducesResponseType(typeof(DownloadRequest), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Rename([FromRoute] Guid id, [FromBody, Required] RenameRequest body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
            {
                return BadRequest("Name is required");
            }

            var trimmed = body.Name.Trim();
            if (trimmed.Length > 512)
            {
                return BadRequest("Name must be 512 characters or fewer");
            }

            await using var context = await ContextFactory.CreateDbContextAsync();
            var request = await context.DownloadRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                return NotFound();
            }

            request.Name = trimmed;
            await context.SaveChangesAsync();
            return Ok(request);
        }

        /// <summary>
        ///     Cancels the current in-flight attempt for the request (if any) and marks the request Cancelled.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = AuthPolicy.Any, Roles = AuthRole.ReadWriteOrAdministrator)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel([FromRoute] Guid id)
        {
            await using var context = await ContextFactory.CreateDbContextAsync();
            var request = await context.DownloadRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                return NotFound();
            }

            var activeAttempts = await context.Transfers
                .Where(t => t.RequestId == id && !t.Removed)
                .Select(t => t.Id)
                .ToListAsync();

            foreach (var attemptId in activeAttempts)
            {
                Downloads.TryCancel(attemptId);
            }

            request.State = DownloadRequestState.Cancelled;
            request.CompletedAt ??= DateTime.UtcNow;
            await context.SaveChangesAsync();

            return NoContent();
        }

        private static DownloadRequestSummary BuildSummary(DownloadRequest request, IEnumerable<Transfer> attempts)
        {
            var attemptList = attempts.OrderByDescending(t => t.RequestedAt).ToList();
            var current = attemptList.FirstOrDefault(t => !t.Removed) ?? attemptList.FirstOrDefault();

            return new DownloadRequestSummary
            {
                Request = request,
                AttemptCount = attemptList.Count,
                Current = current,
            };
        }
    }

    public class RenameRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class DownloadRequestSummary
    {
        public required DownloadRequest Request { get; init; }
        public int AttemptCount { get; init; }
        public Transfer? Current { get; init; }
    }

    public class DownloadRequestDetail
    {
        public required DownloadRequest Request { get; init; }
        public required List<Transfer> Attempts { get; init; }
        public Transfer? Current { get; init; }
    }
}
