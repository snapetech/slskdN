// <copyright file="LidarrImportHistoryRecord.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Integrations.Lidarr;

using System;
using System.Text.Json.Serialization;

public static class LidarrImportStatus
{
    public const string Queued = "Queued";

    public const string Running = "Running";

    public const string Successful = "Successful";

    public const string Failed = "Failed";

    public const string Skipped = "Skipped";
}

public sealed class LidarrImportHistoryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonIgnore]
    public string SourceDirectory { get; set; } = string.Empty;

    public string Directory { get; set; } = string.Empty;

    public string Status { get; set; } = LidarrImportStatus.Running;

    public string ErrorMessage { get; set; } = string.Empty;

    public string SkippedReason { get; set; } = string.Empty;

    public int CandidateCount { get; set; }

    public int SafeCandidateCount { get; set; }

    public int RejectedCandidateCount { get; set; }

    public int? CommandId { get; set; }

    public string ImportMode { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public Guid? RetryOfId { get; set; }
}
