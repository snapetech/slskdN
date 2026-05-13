// <copyright file="StartupApplicationDirectories.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System.IO;

public sealed record StartupApplicationDirectories(
    string AppDirectory,
    string DataDirectory,
    string DataBackupDirectory,
    string LogDirectory,
    string ScriptDirectory,
    string DefaultConfigurationFile,
    string DefaultDownloadsDirectory,
    string DefaultIncompleteDirectory);

public static class StartupApplicationDirectoryResolver
{
    public static StartupApplicationDirectories Resolve(string appDirectory, string defaultAppDirectory, string appName)
    {
        var resolvedAppDirectory = string.IsNullOrWhiteSpace(appDirectory)
            ? defaultAppDirectory
            : appDirectory;

        var dataDirectory = Path.Combine(resolvedAppDirectory, "data");

        return new StartupApplicationDirectories(
            resolvedAppDirectory,
            dataDirectory,
            Path.Combine(dataDirectory, "backups"),
            Path.Combine(resolvedAppDirectory, "logs"),
            Path.Combine(resolvedAppDirectory, "scripts"),
            Path.Combine(resolvedAppDirectory, $"{appName}.yml"),
            Path.Combine(resolvedAppDirectory, "downloads"),
            Path.Combine(resolvedAppDirectory, "incomplete"));
    }

    public static void VerifyDefaults(StartupApplicationDirectories directories)
    {
        StartupFileSystem.VerifyDirectory(directories.AppDirectory, createIfMissing: true, verifyWriteable: true);
        StartupFileSystem.VerifyDirectory(directories.DataDirectory, createIfMissing: true, verifyWriteable: true);
        StartupFileSystem.VerifyDirectory(directories.DataBackupDirectory, createIfMissing: true, verifyWriteable: true);
        StartupFileSystem.VerifyDirectory(directories.ScriptDirectory, createIfMissing: true, verifyWriteable: false);
        StartupFileSystem.VerifyDirectory(directories.DefaultDownloadsDirectory, createIfMissing: true, verifyWriteable: true);
        StartupFileSystem.VerifyDirectory(directories.DefaultIncompleteDirectory, createIfMissing: true, verifyWriteable: true);
    }
}
