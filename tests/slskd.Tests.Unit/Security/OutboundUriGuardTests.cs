// <copyright file="OutboundUriGuardTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Security;

using System;
using slskd.Common.Security;
using Xunit;

public class OutboundUriGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.0.0.1/")]
    [InlineData("http://169.254.169.254/")]
    public async Task CheckAsync_BlocksNonPublicIpLiterals(string uri)
    {
        var (safe, reason) = await OutboundUriGuard.CheckAsync(new Uri(uri));

        Assert.False(safe);
        Assert.Contains("non-public", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckAsync_AllowsPublicIpLiteral()
    {
        var (safe, reason) = await OutboundUriGuard.CheckAsync(new Uri("https://8.8.8.8/"));

        Assert.True(safe, reason);
    }

    [Fact]
    public void CreateNoRedirectHandler_DisablesRedirectsAndUsesGuardedConnect()
    {
        using var handler = OutboundUriGuard.CreateNoRedirectHandler();

        Assert.False(handler.AllowAutoRedirect);
        Assert.NotNull(handler.ConnectCallback);
    }

    [Fact]
    public void CreateNoRedirectOnlyHandler_DisablesRedirectsWithoutGuardedConnect()
    {
        using var handler = OutboundUriGuard.CreateNoRedirectOnlyHandler();

        Assert.False(handler.AllowAutoRedirect);
        Assert.Null(handler.ConnectCallback);
    }
}
