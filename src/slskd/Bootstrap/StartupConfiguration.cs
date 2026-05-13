// <copyright file="StartupConfiguration.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Bootstrap;

using Microsoft.Extensions.Configuration;
using Serilog;
using slskd.Configuration;
using System;
using slskd.Validation;

public static class StartupConfiguration
{
    public static IConfigurationRoot LoadAndValidate(
        string environmentVariablePrefix,
        string configurationFile,
        VolatileOverlayConfigurationSource<OptionsOverlay> volatileOverlayConfigurationSource,
        OptionsAtStartup optionsAtStartup,
        string appName,
        ILogger log,
        Action<int> exit)
    {
        var configuration = new ConfigurationBuilder()
            .AddSlskdConfigurationProviders(
                environmentVariablePrefix,
                configurationFile,
                reloadOnChange: !optionsAtStartup.Flags.NoConfigWatch,
                volatileOverlayConfigurationSource,
                log)
            .Build();

        configuration.GetSection(appName)
            .Bind(optionsAtStartup, o => { o.BindNonPublicProperties = true; });

        log.Debug("[Config] After binding OptionsAtStartup.Security.Enabled = {Enabled}, Profile = {Profile}",
            optionsAtStartup.Security?.Enabled ?? false,
            optionsAtStartup.Security?.Profile.ToString() ?? "null");

        var securitySection = configuration.GetSection("security");
        var slskdSecuritySection = configuration.GetSection("slskd:security");
        log.Debug("[Config] Raw config sections - security.Exists={SecurityExists}, slskd:security.Exists={SlskdSecurityExists}",
            securitySection.Exists(),
            slskdSecuritySection.Exists());
        if (securitySection.Exists())
        {
            log.Debug("[Config] Raw security section enabled value: {Enabled}", securitySection["enabled"]);
        }

        if (slskdSecuritySection.Exists())
        {
            log.Debug("[Config] Raw slskd:security section enabled value: {Enabled}", slskdSecuritySection["enabled"]);
        }

        if (!optionsAtStartup.TryValidate(out var result))
        {
            log.Information(result.GetResultView());
            exit(1);
        }

        return configuration;
    }
}
