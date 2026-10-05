// <copyright file="IntentQueueProcessor.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.VirtualSoulfind.v2.Processing
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using slskd.VirtualSoulfind.Core;
    using slskd.VirtualSoulfind.v2.Catalogue;
    using slskd.VirtualSoulfind.v2.Intents;
    using slskd.VirtualSoulfind.v2.Planning;
    using slskd.VirtualSoulfind.v2.Resolution;
    using slskd.VirtualSoulfind.v2.Execution;
    using LoggingSanitizer = slskd.Common.Security.LoggingSanitizer;

    /// <summary>
    ///     Production implementation of <see cref="IIntentQueueProcessor"/>.
    /// </summary>
    public sealed class IntentQueueProcessor : IIntentQueueProcessor
    {
        private readonly IIntentQueue _intentQueue;
        private readonly ICatalogueStore _catalogueStore;
        private readonly IPlanner _planner;
        private readonly IResolver _resolver;
        private readonly ILogger<IntentQueueProcessor> _logger;

        private int _totalProcessed;
        private int _successCount;
        private int _failureCount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntentQueueProcessor"/> class.
        /// </summary>
        public IntentQueueProcessor(
            IIntentQueue intentQueue,
            ICatalogueStore catalogueStore,
            IPlanner planner,
            IResolver resolver,
            ILogger<IntentQueueProcessor> logger)
        {
            _intentQueue = intentQueue ?? throw new ArgumentNullException(nameof(intentQueue));
            _catalogueStore = catalogueStore ?? throw new ArgumentNullException(nameof(catalogueStore));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> ProcessBatchAsync(int maxIntents = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing batch of up to {MaxIntents} intents", maxIntents);

            var pendingIntents = await _intentQueue.GetPendingTracksAsync(maxIntents, cancellationToken);

            if (!pendingIntents.Any())
            {
                _logger.LogDebug("No pending intents to process");
                return 0;
            }

            _logger.LogInformation("Processing {Count} pending intents", pendingIntents.Count);

            var processed = 0;
            foreach (var intent in pendingIntents)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Batch processing cancelled after {Processed} intents", processed);
                    break;
                }

                try
                {
                    await ProcessIntentAsync(intent, cancellationToken);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to process intent {IntentId}: {Exception}",
                        LoggingSanitizer.SanitizeExternalIdentifier(intent.DesiredTrackId),
                        LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                }
            }

            _logger.LogInformation("Batch processing complete: {Processed}/{Total} intents processed", processed, pendingIntents.Count);
            return processed;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessIntentAsync(string desiredTrackId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Processing intent {IntentId}", LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));

            // Get the intent
            var intent = await _intentQueue.GetTrackIntentAsync(desiredTrackId, cancellationToken);
            if (intent == null)
            {
                _logger.LogWarning("Intent {IntentId} not found", LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));
                return false;
            }

            return await ProcessIntentAsync(intent, cancellationToken);
        }

        private async Task<bool> ProcessIntentAsync(DesiredTrack intent, CancellationToken cancellationToken)
        {
            var desiredTrackId = intent.DesiredTrackId;

            // Skip if already in progress or completed
            if (intent.Status != IntentStatus.Pending)
            {
                _logger.LogDebug("Intent {IntentId} is not pending (status: {Status}), skipping",
                    LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId), intent.Status);
                return false;
            }

            if (!await _intentQueue.TryUpdateTrackStatusAsync(
                    desiredTrackId,
                    IntentStatus.Pending,
                    IntentStatus.InProgress,
                    cancellationToken))
            {
                _logger.LogDebug("Intent {IntentId} is no longer pending, skipping", LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));
                return false;
            }

            try
            {
                // Get track info from catalogue
                if (!ContentItemId.TryParse(intent.TrackId, out var trackId))
                {
                    _logger.LogWarning("Intent {IntentId} has invalid TrackId '{TrackId}', skipping",
                        LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId),
                        LoggingSanitizer.SanitizeExternalIdentifier(intent.TrackId));
                    await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Failed, cancellationToken);
                    Interlocked.Increment(ref _failureCount);
                    Interlocked.Increment(ref _totalProcessed);
                    return false;
                }

                var track = await _catalogueStore.FindTrackByIdAsync(intent.TrackId, cancellationToken);

                if (track == null)
                {
                    _logger.LogWarning("Track {TrackId} not found in catalogue for intent {IntentId}",
                        LoggingSanitizer.SanitizeExternalIdentifier(intent.TrackId),
                        LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));
                    await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Failed, cancellationToken);
                    Interlocked.Increment(ref _failureCount);
                    Interlocked.Increment(ref _totalProcessed);
                    return false;
                }

                _logger.LogInformation(
                    "Processing intent {IntentId} for track: {Title}",
                    LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId),
                    LoggingSanitizer.SanitizeExternalIdentifier(track.Title ?? "Unknown"));

                // Create acquisition plan
                _logger.LogDebug("Creating acquisition plan for intent {IntentId}", LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));
                var plan = await _planner.CreatePlanAsync(intent, null, cancellationToken);

                if (plan == null || !plan.Steps.Any())
                {
                    _logger.LogWarning("No viable plan created for track {TrackId}, intent {IntentId}",
                        LoggingSanitizer.SanitizeExternalIdentifier(trackId.ToString()),
                        LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));
                    await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Failed, cancellationToken);
                    Interlocked.Increment(ref _failureCount);
                    Interlocked.Increment(ref _totalProcessed);
                    return false;
                }

                _logger.LogInformation(
                    "Created plan with {StepCount} steps for track {TrackId}",
                    plan.Steps.Count,
                    LoggingSanitizer.SanitizeExternalIdentifier(trackId.ToString()));

                // Store plan reference in intent
                // (In a full implementation, we'd update DesiredTrack.PlannedSources here)

                // Execute the plan
                _logger.LogDebug("Executing plan for track {TrackId}", LoggingSanitizer.SanitizeExternalIdentifier(trackId.ToString()));
                var executionState = await _resolver.ExecutePlanAsync(plan, cancellationToken);

                // Update intent based on execution result
                if (executionState.Status == PlanExecutionStatus.Succeeded)
                {
                    _logger.LogInformation(
                        "Successfully acquired track {TrackId} for intent {IntentId}",
                        LoggingSanitizer.SanitizeExternalIdentifier(trackId.ToString()),
                        LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));

                    await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Completed, cancellationToken);
                    Interlocked.Increment(ref _successCount);
                    Interlocked.Increment(ref _totalProcessed);
                    return true;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to acquire track {TrackId} for intent {IntentId}: {Status} - {Error}",
                        LoggingSanitizer.SanitizeExternalIdentifier(trackId.ToString()),
                        LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId),
                        executionState.Status,
                        LoggingSanitizer.SanitizeExternalIdentifier(executionState.ErrorMessage ?? "Unknown error"));

                    await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Failed, cancellationToken);
                    Interlocked.Increment(ref _failureCount);
                    Interlocked.Increment(ref _totalProcessed);
                    return false;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Processing of intent {IntentId} was cancelled", LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId));

                // Use CancellationToken.None: the original token is already cancelled so any
                // awaitable using it would throw immediately, preventing the status reset.
                await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Pending, CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing intent {IntentId}: {Exception}",
                    LoggingSanitizer.SanitizeExternalIdentifier(desiredTrackId),
                    LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString()));
                await _intentQueue.UpdateTrackStatusAsync(desiredTrackId, IntentStatus.Failed, cancellationToken);
                Interlocked.Increment(ref _failureCount);
                Interlocked.Increment(ref _totalProcessed);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IntentProcessorStats> GetStatsAsync()
        {
            var pending = await _intentQueue.CountTracksByStatusAsync(IntentStatus.Pending);
            var inProgress = await _intentQueue.CountTracksByStatusAsync(IntentStatus.InProgress);

            return new IntentProcessorStats
            {
                TotalProcessed = _totalProcessed,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                InProgressCount = inProgress,
                PendingCount = pending,
            };
        }
    }
}
