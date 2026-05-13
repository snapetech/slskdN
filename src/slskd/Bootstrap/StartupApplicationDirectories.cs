// <copyright file="StartupApplicationDirectories.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System.IO;
using System.Threading;
using Serilog;

public sealed record StartupApplicationDirectories(
    string AppDirectory,
    string DataDirectory,
    string DataBackupDirectory,
    string LogDirectory,
    string ScriptDirectory,
    string DefaultConfigurationFile,
    string DefaultDownloadsDirectory,
    string DefaultIncompleteDirectory);

public sealed record StartupApplicationDirectoryPreparation(
    StartupApplicationDirectories Directories,
    string ConfigurationFile,
    Mutex Mutex);

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

    public static StartupApplicationDirectoryPreparation? TryPrepare(
        string appDirectory,
        string defaultAppDirectory,
        string appName,
        string configurationFile,
        ILogger log,
        Action<int> exit)
    {
        Mutex? mutex = null;
        try
        {
            var directories = Resolve(appDirectory, defaultAppDirectory, appName);
            mutex = new Mutex(initiallyOwned: true, StartupSingleInstance.GetMutexName(appName, directories.AppDirectory, defaultAppDirectory));

            if (!mutex.WaitOne(millisecondsTimeout: 0, exitContext: false))
            {
                log.Fatal($"An instance of {appName} is already running in app directory: {directories.AppDirectory}");
                return null;
            }

            try
            {
                VerifyDefaults(directories);
            }
            catch (Exception ex)
            {
                log.Information($"Filesystem exception: {ex.Message}");
                exit(1);
                return null;
            }

            var resolvedConfigurationFile = string.IsNullOrWhiteSpace(configurationFile)
                ? directories.DefaultConfigurationFile
                : configurationFile;

            var preparation = new StartupApplicationDirectoryPreparation(directories, resolvedConfigurationFile, mutex);
            mutex = null;
            return preparation;
        }
        finally
        {
            mutex?.Dispose();
        }
    }
}
