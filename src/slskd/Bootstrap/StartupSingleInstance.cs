// <copyright file="StartupSingleInstance.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using slskd.Cryptography;

public static class StartupSingleInstance
{
    public static string GetMutexName(string appName, string? appDirectory, string defaultAppDirectory)
    {
        var directory = appDirectory ?? defaultAppDirectory;
        return $"{appName}_{Compute.Sha256Hash(directory)}";
    }
}
