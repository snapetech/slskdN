// <copyright file="DownloadFilter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Transfers.Downloads;

using System;
using System.Collections.Generic;

/// <summary>
///     Applies the daemon-wide outbound download exclusion policy.
/// </summary>
public static class DownloadFilter
{
    /// <summary>
    ///     Finds the first configured exclusion contained in a remote filename or path.
    /// </summary>
    /// <param name="remoteFilename">The remote filename or path.</param>
    /// <param name="exclusions">The configured literal exclusion terms.</param>
    /// <returns>The matching configured term, or <see langword="null"/> when allowed.</returns>
    public static string? GetMatchingExclusion(string? remoteFilename, IEnumerable<string>? exclusions)
    {
        if (string.IsNullOrWhiteSpace(remoteFilename) || exclusions is null)
        {
            return null;
        }

        var normalizedFilename = Normalize(remoteFilename);

        foreach (var configuredExclusion in exclusions)
        {
            var exclusion = configuredExclusion?.Trim();
            if (string.IsNullOrWhiteSpace(exclusion))
            {
                continue;
            }

            if (normalizedFilename.Contains(Normalize(exclusion), StringComparison.OrdinalIgnoreCase))
            {
                return exclusion;
            }
        }

        return null;
    }

    /// <summary>
    ///     Determines whether a remote filename or path is excluded by policy.
    /// </summary>
    /// <param name="remoteFilename">The remote filename or path.</param>
    /// <param name="exclusions">The configured literal exclusion terms.</param>
    /// <returns><see langword="true"/> when the filename is blocked.</returns>
    public static bool IsExcluded(string? remoteFilename, IEnumerable<string>? exclusions)
        => GetMatchingExclusion(remoteFilename, exclusions) is not null;

    private static string Normalize(string value)
        => value.Trim().Replace('\\', '/');
}

/// <summary>
///     Indicates that a download was rejected by the configured global policy.
/// </summary>
public sealed class DownloadBlockedByPolicyException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DownloadBlockedByPolicyException"/> class.
    /// </summary>
    /// <param name="filename">The blocked remote filename.</param>
    /// <param name="exclusion">The matching configured exclusion.</param>
    public DownloadBlockedByPolicyException(string filename, string exclusion)
        : base($"Download blocked by global exclusion '{exclusion}': {filename}")
    {
        Filename = filename;
        Exclusion = exclusion;
    }

    /// <summary>
    ///     Gets the blocked remote filename.
    /// </summary>
    public string Filename { get; }

    /// <summary>
    ///     Gets the matching configured exclusion.
    /// </summary>
    public string Exclusion { get; }
}
