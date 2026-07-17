// <copyright file="StartupWebApplicationRunnerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Bootstrap;

using Serilog;
using slskd.Bootstrap;
using Xunit;

public sealed class StartupWebApplicationRunnerTests
{
    [Fact]
    public void HandleUnexpectedTermination_ExitsWithFailureStatus()
    {
        int? exitCode = null;
        using var log = new LoggerConfiguration().CreateLogger();

        StartupWebApplicationRunner.HandleUnexpectedTermination(
            new InvalidOperationException("startup failed"),
            log,
            code => exitCode = code);

        Assert.Equal(1, exitCode);
    }
}
