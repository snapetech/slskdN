// <copyright file="HardeningValidatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests;

using System;
using slskd;
using slskd.Common.Security;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HardeningValidator"/>. When EnforceSecurity is on, dangerous configs must throw
/// <see cref="HardeningValidationException"/> with the expected rule name. When EnforceSecurity is off, no exception.
/// </summary>
public class HardeningValidatorTests
{
    [Fact]
    public void EnforceSecurity_false_does_not_throw_even_with_bad_combo()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = false,
                AllowRemoteNoAuth = false,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = true },
            },
        };

        HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true);
    }

    [Fact]
    public void EnforceSecurity_true_AuthDisabled_NonLoopback_AllowRemoteNoAuth_false_throws_AuthDisabledNonLoopback()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = false,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = true },
                Cors = new Options.WebOptions.CorsOptions { Enabled = true, AllowCredentials = false }, // avoid CORS rule
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true));

        Assert.Equal(HardeningValidator.RuleAuthDisabledNonLoopback, ex.RuleName);
        Assert.Contains("Authentication is disabled", ex.Message);
    }

    [Fact]
    public void EnforceSecurity_true_Cors_Disabled_does_not_throw()
    {
        // PR-04: when Cors.Enabled=false, no CORS middleware; CORS rule is skipped
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = true,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = false },
                Cors = new Options.WebOptions.CorsOptions { Enabled = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };

        HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true);
    }

    [Fact]
    public void EnforceSecurity_true_CorsCredentialsWithWildcard_explicit_throws_CorsCredentialsWithWildcard()
    {
        // Explicit: Cors.Enabled=true, AllowCredentials=true, AllowedOrigins empty (=any)
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = true,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = false },
                Cors = new Options.WebOptions.CorsOptions
                {
                    Enabled = true,
                    AllowCredentials = true,
                    AllowedOrigins = Array.Empty<string>(),
                },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true));

        Assert.Equal(HardeningValidator.RuleCorsCredentialsWithWildcard, ex.RuleName);
    }

    [Fact]
    public void EnforceSecurity_true_MemoryDump_AuthDisabled_throws_MemoryDumpWithAuthDisabled()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = true,
                Authentication = new Options.WebOptions.WebAuthenticationOptions
                {
                    Disabled = true,
                    Passthrough = new Options.WebOptions.WebAuthenticationOptions.PassthroughOptions
                    {
                        AllowedCidrs = "127.0.0.1/32,::1/128",
                    },
                },
                Cors = new Options.WebOptions.CorsOptions { Enabled = true, AllowCredentials = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = true },
        };

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true));

        Assert.Equal(HardeningValidator.RuleMemoryDumpWithAuthDisabled, ex.RuleName);
        Assert.Contains("AllowMemoryDump", ex.Message);
    }

    /// <summary>
    /// §11: Flags.HashFromAudioFileEnabled must fail startup because PCM extraction is unavailable.
    /// </summary>
    [Fact]
    public void EnforceSecurity_true_HashFromAudioFileEnabled_throws_HashFromAudioFileEnabled()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = true,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = false },
                Cors = new Options.WebOptions.CorsOptions { Enabled = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
            Flags = new Options.FlagsOptions { HashFromAudioFileEnabled = true },
        };

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true));

        Assert.Equal(HardeningValidator.RuleHashFromAudioFileEnabled, ex.RuleName);
        Assert.Contains("HashFromAudioFileEnabled", ex.Message);
    }

    [Fact]
    public void EnforceSecurity_false_HashFromAudioFileEnabled_throws_HashFromAudioFileEnabled()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = false,
                AllowRemoteNoAuth = false,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = false },
                Cors = new Options.WebOptions.CorsOptions { Enabled = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
            Flags = new Options.FlagsOptions { HashFromAudioFileEnabled = true },
        };

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(options, "Production", isBindingNonLoopback: false));

        Assert.Equal(HardeningValidator.RuleHashFromAudioFileEnabled, ex.RuleName);
        Assert.Contains("not supported", ex.Message);
    }

    [Fact]
    public void EnforceSecurity_true_valid_config_does_not_throw()
    {
        // Enforce on, auth on, CORS with Cors.Enabled=true and AllowCredentials=false (no cred+any), no memory dump
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = false,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = false },
                Cors = new Options.WebOptions.CorsOptions { Enabled = true, AllowCredentials = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };

        HardeningValidator.Validate(options, "Production", isBindingNonLoopback: true);
    }

    [Fact]
    public void EnforceSecurity_true_loopback_bind_AuthDisabled_does_not_throw()
    {
        var options = new OptionsAtStartup
        {
            Web = new Options.WebOptions
            {
                EnforceSecurity = true,
                AllowRemoteNoAuth = false,
                Authentication = new Options.WebOptions.WebAuthenticationOptions { Disabled = true },
                Cors = new Options.WebOptions.CorsOptions { Enabled = true, AllowCredentials = false },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };

        HardeningValidator.Validate(options, "Production", isBindingNonLoopback: false);
    }

    [Theory]
    [InlineData("127.0.0.1", 5030, null)]
    [InlineData("localhost", 5030, null)]
    [InlineData("127.0.0.1", 0, "/tmp/slskd.sock")]
    public void EnforceSecurity_true_AuthDisabled_LocalOnlyBinding_does_not_throw(
        string address,
        int port,
        string socket)
    {
        var options = CreateNoAuthStartupOptions(
            address,
            port,
            socket,
            allowRemoteNoAuth: false,
            allowedCidrs: null);

        var exposure = BindExposureAnalyzer.AnalyzeWebBinding(options);

        HardeningValidator.Validate(
            options,
            "Production",
            BindExposureAnalyzer.IsRemoteReachable(exposure));
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("192.168.1.20")]
    [InlineData("::")]
    [InlineData("not-a-valid-bind-address")]
    public void EnforceSecurity_true_AuthDisabled_RemoteBindingWithoutAllowRemote_throws_AuthDisabledNonLoopback(
        string address)
    {
        var options = CreateNoAuthStartupOptions(
            address,
            5030,
            null,
            allowRemoteNoAuth: false,
            allowedCidrs: null);

        var exposure = BindExposureAnalyzer.AnalyzeWebBinding(options);

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(
                options,
                "Production",
                BindExposureAnalyzer.IsRemoteReachable(exposure)));

        Assert.Equal(HardeningValidator.RuleAuthDisabledNonLoopback, ex.RuleName);
    }

    [Fact]
    public void EnforceSecurity_true_AuthDisabled_AllowRemoteNoAuthWithoutCidrs_throws_RemoteNoAuthWithoutCidrs()
    {
        var options = CreateNoAuthStartupOptions(
            "0.0.0.0",
            5030,
            null,
            allowRemoteNoAuth: true,
            allowedCidrs: null);

        var exposure = BindExposureAnalyzer.AnalyzeWebBinding(options);

        var ex = Assert.Throws<HardeningValidationException>(
            () => HardeningValidator.Validate(
                options,
                "Production",
                BindExposureAnalyzer.IsRemoteReachable(exposure)));

        Assert.Equal(HardeningValidator.RuleRemoteNoAuthWithoutCidrs, ex.RuleName);
    }

    [Fact]
    public void EnforceSecurity_true_AuthDisabled_AllowRemoteNoAuthWithCidrs_does_not_throw()
    {
        var options = CreateNoAuthStartupOptions(
            "0.0.0.0",
            5030,
            null,
            allowRemoteNoAuth: true,
            allowedCidrs: "192.168.1.0/24");

        var exposure = BindExposureAnalyzer.AnalyzeWebBinding(options);

        HardeningValidator.Validate(
            options,
            "Production",
            BindExposureAnalyzer.IsRemoteReachable(exposure));
    }

    [Fact]
    public void Options_null_does_not_throw()
    {
        HardeningValidator.Validate(null!, "Production", isBindingNonLoopback: true);
    }

    private static OptionsAtStartup CreateNoAuthStartupOptions(
        string address,
        int port,
        string socket,
        bool allowRemoteNoAuth,
        string allowedCidrs) => new()
        {
            Web = new Options.WebOptions
            {
                Address = address,
                Port = port,
                Socket = socket ?? string.Empty,
                EnforceSecurity = true,
                AllowRemoteNoAuth = allowRemoteNoAuth,
                Authentication = new Options.WebOptions.WebAuthenticationOptions
                {
                    Disabled = true,
                    Passthrough = new Options.WebOptions.WebAuthenticationOptions.PassthroughOptions
                    {
                        AllowedCidrs = allowedCidrs,
                    },
                },
                Cors = new Options.WebOptions.CorsOptions { Enabled = true, AllowCredentials = false },
                Https = new Options.WebOptions.HttpsOptions { Disabled = true },
            },
            Diagnostics = new Options.DiagnosticsOptions { AllowMemoryDump = false },
        };
}
