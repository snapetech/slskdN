// <copyright file="JobListPage.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Jobs;

public sealed record JobListItem(
    string Id,
    string Type,
    string Status,
    DateTimeOffset CreatedAt,
    int TotalReleases,
    int CompletedReleases,
    int FailedReleases);

public sealed record JobListPage(IReadOnlyList<JobListItem> Items, int Total);
