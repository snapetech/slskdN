// <copyright file="RegexUsernameMatcherTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Users;

using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using slskd;
using slskd.Users;
using Xunit;

public class RegexUsernameMatcherTests
{
    private static RegexUsernameMatcher Create(params string[] patterns)
        => Create(false, patterns);

    private static RegexUsernameMatcher Create(bool caseSensitive, params string[] patterns)
    {
        var options = CreateOptions(caseSensitive, patterns);

        return new RegexUsernameMatcher(
            new TestOptionsMonitor<Options>(options),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static Options CreateOptions(bool caseSensitive, params string[] patterns) => new()
    {
        Flags = new Options.FlagsOptions
        {
            CaseSensitiveRegEx = caseSensitive,
        },
        Groups = new Options.GroupsOptions
        {
            Blacklisted = new Options.GroupsOptions.BlacklistedOptions
            {
                Patterns = patterns,
            },
        },
    };

    [Fact]
    public void IsMatch_MatchesConfiguredPattern()
    {
        using var matcher = Create("^bad.*");

        Assert.True(matcher.IsMatch("badguy"));
        Assert.True(matcher.IsMatch("BADGUY"));
        Assert.False(matcher.IsMatch("goodguy"));
    }

    [Fact]
    public void IsMatch_CaseSensitiveOptionHonorsCaseAndInvalidatesCachedResults()
    {
        var monitor = new TestOptionsMonitor<Options>(CreateOptions(false, "^bad.*"));
        using var matcher = new RegexUsernameMatcher(
            monitor,
            new MemoryCache(new MemoryCacheOptions()));

        Assert.True(matcher.IsMatch("BADGUY"));

        monitor.Set(CreateOptions(true, "^bad.*"));

        Assert.True(matcher.IsMatch("badguy"));
        Assert.False(matcher.IsMatch("BADGUY"));
    }

    [Fact]
    public void IsMatch_NoPatterns_ReturnsFalse()
    {
        using var matcher = Create();

        Assert.False(matcher.IsMatch("anyone"));
    }

    [Fact]
    public void IsMatch_CatastrophicPattern_TimesOutInsteadOfHanging()
    {
        // Classic catastrophic-backtracking pattern. Applied to a long non-matching
        // username without a match timeout, this would backtrack for many seconds; the
        // configured timeout must bound it so a malicious peer name can't stall the caller.
        using var matcher = Create("^(a+)+$");
        var hostileUsername = new string('a', 40) + "!";

        var sw = Stopwatch.StartNew();
        var result = matcher.IsMatch(hostileUsername);
        sw.Stop();

        Assert.False(result);
        Assert.True(sw.Elapsed.TotalSeconds < 5, $"IsMatch took {sw.Elapsed.TotalSeconds:F1}s; the match timeout should bound it well under this.");
    }
}
