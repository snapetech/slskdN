// <copyright file="StartupCommandMode.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using Serilog;
using slskd.Cryptography;

public sealed record StartupCommandModeOptions(
    bool ShowVersion,
    bool ShowHelp,
    bool ShowEnvironmentVariables,
    bool NoLogo,
    bool GenerateCertificate,
    int GenerateSecret);

public static class StartupCommandMode
{
    public static bool TryRun(
        StartupCommandModeOptions options,
        string fullVersion,
        string environmentVariablePrefix,
        string appName,
        string baseDirectory,
        Type optionsType,
        bool isDevelopment,
        bool isCanary,
        ILogger log,
        Action<int> exit)
    {
        if (options.ShowVersion)
        {
            log.Information(fullVersion);
            return true;
        }

        if (options.ShowHelp || options.ShowEnvironmentVariables)
        {
            if (!options.NoLogo)
            {
                StartupConsoleOutput.PrintLogo(fullVersion, isDevelopment, isCanary);
            }

            if (options.ShowHelp)
            {
                StartupConsoleOutput.PrintCommandLineArguments(optionsType, log);
            }

            if (options.ShowEnvironmentVariables)
            {
                StartupConsoleOutput.PrintEnvironmentVariables(optionsType, environmentVariablePrefix, log);
            }

            return true;
        }

        if (options.GenerateCertificate)
        {
            var (filename, password) = StartupFileSystem.GenerateX509Certificate(
                appName,
                baseDirectory,
                Cryptography.Random.GetBytes(16).ToBase62(),
                $"{appName}.pfx",
                log);

            log.Information("Certificate exported to {Filename}", filename);
            Console.WriteLine($"Password: {password}");
            return true;
        }

        if (options.GenerateSecret > 0)
        {
            if (options.GenerateSecret < 16 || options.GenerateSecret > 255)
            {
                log.Error("Invalid command line input: secret length must be between 16 and 255, inclusive");
                exit(1);
                return true;
            }

            log.Information(Cryptography.Random.GetBytes(options.GenerateSecret).ToBase62());
            return true;
        }

        return false;
    }
}
