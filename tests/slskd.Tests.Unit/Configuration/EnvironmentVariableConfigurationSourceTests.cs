// <copyright file="EnvironmentVariableConfigurationSourceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Configuration;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using slskd.Configuration;
using Utility.EnvironmentVariables;
using Xunit;

public sealed class EnvironmentVariableConfigurationSourceTests
{
    [Fact]
    public void AddEnvironmentVariables_IgnoresEmptyOptionalOverrides()
    {
        const string variableName = "SLSKDN_TEST_EMPTY_BOOLEAN";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, string.Empty);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["slskd:emptyvalue"] = "true",
                })
                .AddEnvironmentVariables(typeof(EnvironmentVariableTestOptions), "SLSKDN_TEST_")
                .Build();

            var options = new EnvironmentVariableTestOptions();
            configuration.GetSection("slskd").Bind(options);

            Assert.True(options.EmptyValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    [Fact]
    public void AddEnvironmentVariables_PreservesNonEmptyOverrides()
    {
        const string variableName = "SLSKDN_TEST_BOOLEAN";
        var previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, "true");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["slskd:value"] = "false",
                })
                .AddEnvironmentVariables(typeof(EnvironmentVariableTestOptions), "SLSKDN_TEST_")
                .Build();

            var options = new EnvironmentVariableTestOptions();
            configuration.GetSection("slskd").Bind(options);

            Assert.True(options.Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }
    }

    private sealed class EnvironmentVariableTestOptions
    {
        [EnvironmentVariable("EMPTY_BOOLEAN")]
        public bool EmptyValue { get; init; }

        [EnvironmentVariable("BOOLEAN")]
        public bool Value { get; init; }
    }
}
