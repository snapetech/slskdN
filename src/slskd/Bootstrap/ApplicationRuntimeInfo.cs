// <copyright file="ApplicationRuntimeInfo.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

public static class ApplicationRuntimeInfo
{
    public static Version AssemblyVersion { get; } = NormalizeAssemblyVersion(
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0));

    public static string InformationalVersion { get; } = NormalizeInformationalVersion(
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0");

    public static string SemanticVersion { get; } = InformationalVersion.Split('+').First();

    public static string FullVersion { get; } = $"{SemanticVersion} ({InformationalVersion})";

    public static bool IsCanary { get; } = AssemblyVersion.Revision == 65534;

    public static bool IsDevelopment { get; } = new Version(0, 0, 0, 0) == AssemblyVersion;

    public static string TryGetExecutablePath()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static Version NormalizeAssemblyVersion(Version version)
    {
        return version.Equals(new Version(1, 0, 0, 0))
            ? new Version(0, 0, 0, 0)
            : version;
    }

    private static string NormalizeInformationalVersion(string version)
    {
        return version == "1.0.0" ? "0.0.0" : version;
    }
}
