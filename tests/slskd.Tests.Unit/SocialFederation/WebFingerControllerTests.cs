// <copyright file="WebFingerControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SocialFederation;

using System.Reflection;
using Microsoft.Extensions.Logging;
using slskd.SocialFederation.API;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class WebFingerControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WebFingerResourceLogs_RedactUrlSecretsAndEscapeMalformedResources(bool served)
    {
        var logger = new CapturingLogger<WebFingerController>();
        const string resource = "https://peer.example/actors/alice?token=secret\r\nforged";

        if (served)
        {
            WebFingerController.LogServedResource(logger, resource);
        }
        else
        {
            WebFingerController.LogRejectedResource(logger, resource);
        }

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("https://peer.example", entry.Message);
        Assert.DoesNotContain("/actors/alice", entry.Message);
        Assert.DoesNotContain("token=secret", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
        Assert.Null(entry.Exception);
    }

    [Theory]
    [InlineData("https://example.com/actors/music/extra")]
    [InlineData("https://example.com/@music/extra")]
    public void TryParseResource_RejectsHttpsActorPathsWithExtraSegments(string resource)
    {
        var (parsed, username, domain) = Parse(resource);

        Assert.False(parsed);
        Assert.Equal(string.Empty, username);
        Assert.Equal("example.com", domain);
    }

    [Fact]
    public void TryParseResource_TrimsWhitespaceAroundAcctResource()
    {
        var (parsed, username, domain) = Parse("  acct:music@example.com  ");

        Assert.True(parsed);
        Assert.Equal("music", username);
        Assert.Equal("example.com", domain);
    }

    private static (bool Parsed, string Username, string Domain) Parse(string resource)
    {
        var method = typeof(WebFingerController).GetMethod("TryParseResource", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var args = new object[] { resource, string.Empty, string.Empty };
        var parsed = (bool)method!.Invoke(null, args)!;

        return (parsed, (string)args[1], (string)args[2]);
    }
}
