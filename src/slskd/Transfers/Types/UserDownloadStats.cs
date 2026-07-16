// <copyright file="UserDownloadStats.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Transfers;

/// <summary>
///     Retained download statistics for a user.
/// </summary>
public class UserDownloadStats
{
    public string Username { get; set; } = string.Empty;
    public int TotalDownloads { get; set; }
    public int SuccessfulDownloads { get; set; }
    public int FailedDownloads { get; set; }
    public long TotalBytes { get; set; }
    public DateTime? LastDownloadAt { get; set; }
}
