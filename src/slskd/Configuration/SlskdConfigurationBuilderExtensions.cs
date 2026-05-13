// <copyright file="SlskdConfigurationBuilderExtensions.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Configuration;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Serilog;
using Utility.CommandLine;

public static class SlskdConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddSlskdConfigurationProviders(
        this IConfigurationBuilder builder,
        string environmentVariablePrefix,
        string configurationFile,
        bool reloadOnChange,
        VolatileOverlayConfigurationSource<OptionsOverlay> volatileOverlayConfigurationSource,
        ILogger log)
    {
        configurationFile = Path.GetFullPath(configurationFile);
        log.Information("[Config] Loading configuration from {ConfigFile}", configurationFile);

        var multiValuedArguments = typeof(Options)
            .GetPropertiesRecursively()
            .Where(p => p.PropertyType.IsArray)
            .SelectMany(p =>
                p.CustomAttributes
                    .Where(a => a.AttributeType == typeof(ArgumentAttribute))
                    .Select(a => new[] { a.ConstructorArguments[0].Value, a.ConstructorArguments[1].Value })
                    .SelectMany(v => v))
            .Select(v => v?.ToString())
            .Where(v => v != "\u0000")
            .OfType<string>()
            .ToArray();

        var configurationDirectory = Path.GetDirectoryName(configurationFile);
        if (string.IsNullOrWhiteSpace(configurationDirectory))
        {
            throw new InvalidOperationException($"Configuration file path '{configurationFile}' does not have a directory component.");
        }

        var result = builder
            .AddDefaultValues(
                targetType: typeof(Options))
            .AddEnvironmentVariables(
                targetType: typeof(Options),
                prefix: environmentVariablePrefix)
#pragma warning disable CA2000 // Framework configuration infrastructure owns the file provider lifecycle.
            .AddYamlFile(
                path: Path.GetFileName(configurationFile),
                targetType: typeof(Options),
                optional: true,
                reloadOnChange: reloadOnChange,
                provider: CreateOwnedPhysicalFileProvider(configurationDirectory, ExclusionFilters.None)) // required for locations outside of the app directory
#pragma warning restore CA2000
            .AddCommandLine(
                targetType: typeof(Options),
                multiValuedArguments,
                commandLine: Environment.CommandLine)
            .Add(volatileOverlayConfigurationSource); // this must come last in order to supersede all other sources

        log.Information("[Config] Configuration providers added, YAML file: {ConfigFile}", configurationFile);
        return result;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The assigned framework options/configuration source owns the file provider lifecycle.")]
    private static PhysicalFileProvider CreateOwnedPhysicalFileProvider(string root, ExclusionFilters exclusionFilters = ExclusionFilters.Sensitive)
        => new(root, exclusionFilters);
}
