// <copyright file="AnalyzerMigrationService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Audio
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using slskd.HashDb;

    public interface IAnalyzerMigrationService
    {
        /// <summary>
        ///     Recompute quality/transcode flags for variants. When force is false, only those with stale analyzer_version.
        /// </summary>
        Task<int> MigrateAsync(string targetAnalyzerVersion, bool force = false, CancellationToken ct = default);
    }

    /// <summary>
    ///     Migration to bring existing variants up to the current analyzer version without re-decoding audio.
    ///     Uses stored metadata and existing heuristics to recompute quality/transcode.
    /// </summary>
    public class AnalyzerMigrationService : IAnalyzerMigrationService
    {
        private const int RecordingPageSize = 500;

        private readonly IHashDbService hashDb;
        private readonly ILogger<AnalyzerMigrationService> log;
        private readonly QualityScorer qualityScorer = new();
        private readonly TranscodeDetector transcodeDetector = new();

        public AnalyzerMigrationService(IHashDbService hashDb, ILogger<AnalyzerMigrationService> log)
        {
            this.hashDb = hashDb;
            this.log = log;
        }

        public async Task<int> MigrateAsync(string targetAnalyzerVersion, bool force = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(targetAnalyzerVersion))
            {
                throw new ArgumentException("Target analyzer version is required", nameof(targetAnalyzerVersion));
            }

            var updated = 0;
            string? afterRecordingId = null;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var recordingIds = await hashDb
                    .GetRecordingIdsWithVariantsPageAsync(afterRecordingId, RecordingPageSize, ct)
                    .ConfigureAwait(false);
                if (recordingIds.Count == 0)
                {
                    break;
                }

                var variants = await hashDb
                    .GetVariantsByRecordingsAsync(recordingIds, ct)
                    .ConfigureAwait(false);
                var stale = force
                    ? variants
                    : variants.Where(v => string.IsNullOrWhiteSpace(v.AnalyzerVersion) || !string.Equals(v.AnalyzerVersion, targetAnalyzerVersion, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var v in stale)
                {
                    v.QualityScore = qualityScorer.ComputeQualityScore(v);
                    var (suspect, reason) = transcodeDetector.DetectTranscode(v);
                    v.TranscodeSuspect = suspect;
                    v.TranscodeReason = reason;
                    v.AnalyzerVersion = targetAnalyzerVersion;
                }

                if (stale.Count > 0)
                {
                    await hashDb.UpdateVariantAnalysisAsync(stale, ct).ConfigureAwait(false);
                    updated += stale.Count;
                }

                afterRecordingId = recordingIds[^1];
            }

            log.LogInformation("[AnalyzerMigration] Updated {Count} variants to analyzer_version {Version}", updated, targetAnalyzerVersion);
            return updated;
        }
    }
}
