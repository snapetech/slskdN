// <copyright file="DownloadDestinationResolver.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Destinations;

using System.Collections.Generic;
using System.Linq;
using slskd.Common.Security;

/// <summary>
///     Resolves the configured default download destination and validates explicit overrides.
/// </summary>
public static class DownloadDestinationResolver
{
    /// <summary>
    ///     Gets the configured default destination, falling back to <c>directories.downloads</c>.
    /// </summary>
    public static string GetDefaultPath(Options options)
    {
        var configuredDefault = options.Destinations?.Folders?
            .FirstOrDefault(destination => destination.Default && !string.IsNullOrWhiteSpace(destination.Path));

        return configuredDefault?.Path ?? options.Directories.Downloads;
    }

    /// <summary>
    ///     Normalizes an explicit destination when it is inside an allowed configured root.
    /// </summary>
    public static string? NormalizeExplicitPath(Options options, string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }

        return PathGuard.NormalizeAbsolutePathWithinRoots(destination, GetAllowedRoots(options));
    }

    /// <summary>
    ///     Enumerates roots that may be selected as download destinations.
    /// </summary>
    public static IEnumerable<string> GetAllowedRoots(Options options)
    {
        yield return options.Directories.Downloads;

        foreach (var destination in options.Destinations?.Folders ?? Enumerable.Empty<Options.DestinationOption>())
        {
            if (!string.IsNullOrWhiteSpace(destination.Path))
            {
                yield return destination.Path;
            }
        }
    }
}
