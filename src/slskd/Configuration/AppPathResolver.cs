// <copyright file="AppPathResolver.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Configuration;

using System;
using System.IO;

public static class AppPathResolver
{
    public static string GetWriteBaseDirectory(string appDirectory, string defaultAppDirectory)
    {
        return string.IsNullOrWhiteSpace(appDirectory) ? defaultAppDirectory : appDirectory;
    }

    public static string ResolveOptionalAppRelativePath(string? path, string appDirectory, string defaultAppDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(GetWriteBaseDirectory(appDirectory, defaultAppDirectory), path);
    }

    public static string ResolveAppRelativePath(
        string path,
        string fallbackRelativePath,
        string appDirectory,
        string defaultAppDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(path) ? fallbackRelativePath : path;
        return ResolveOptionalAppRelativePath(candidate, appDirectory, defaultAppDirectory);
    }
}
