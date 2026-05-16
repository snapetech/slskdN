// <copyright file="ApplicationRuntimeInfoTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Bootstrap;

using slskd.Bootstrap;
using Xunit;

public sealed class ApplicationRuntimeInfoTests
{
    [Theory]
    [InlineData("0.0.0", true)]
    [InlineData("2026051600-slskdn.259", false)]
    [InlineData("0.0.0-slskdn.2026051600.259", false)]
    [InlineData("0.24.1-dev-20260516.010628", false)]
    public void IsDevelopmentBuildOnlyTreatsUnversionedBuildsAsDevelopment(
        string semanticVersion,
        bool expected)
    {
        var result = ApplicationRuntimeInfo.IsDevelopmentBuild(new Version(0, 0, 0, 0), semanticVersion);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsDevelopmentBuildDoesNotTreatVersionedAssemblyAsDevelopment()
    {
        var result = ApplicationRuntimeInfo.IsDevelopmentBuild(new Version(1, 2, 3, 4), "0.0.0");

        Assert.False(result);
    }
}
