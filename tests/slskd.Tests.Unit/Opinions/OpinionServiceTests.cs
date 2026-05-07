// <copyright file="OpinionServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Opinions;

using Microsoft.Extensions.Logging.Abstractions;
using slskd.Opinions;
using Soulseek;
using Xunit;

public sealed class OpinionServiceTests
{
    [Fact]
    public async Task SubmitAsync_Persists_EvidenceBound_Opinion()
    {
        var path = CreateStorePath();
        var service = new OpinionService(NullLogger<OpinionService>.Instance, path);

        var opinion = await service.SubmitAsync(new OpinionRecord
        {
            Issuer = "local:test",
            SubjectType = OpinionSubjectType.User,
            SubjectId = "Jarvis1984",
            Kind = OpinionKind.Trust,
            Strength = 0.75,
            Confidence = 0.9,
            Evidence =
            {
                new OpinionEvidence { Type = "transfer", Value = "completed" },
            },
        });

        Assert.NotEmpty(opinion.Id);
        Assert.NotEmpty(opinion.PayloadHash);

        var reloaded = new OpinionService(NullLogger<OpinionService>.Instance, path);
        var summary = await reloaded.SummarizeAsync(OpinionSubjectType.User, "Jarvis1984");

        Assert.Equal(1, summary.Total);
        Assert.Equal(1, summary.Positive);
        Assert.True(summary.WeightedScore > 0);
        Assert.Equal(opinion.PayloadHash, summary.Opinions.Single().PayloadHash);
    }

    [Fact]
    public async Task ImportSoulseekInterestsAsync_Replaces_Weak_Public_User_Signals()
    {
        var path = CreateStorePath();
        var service = new OpinionService(NullLogger<OpinionService>.Instance, path);

        await service.ImportSoulseekInterestsAsync(
            "alice",
            new UserInterests("alice", new[] { "artist:one" }, new[] { "artist:bad" }));

        await service.ImportSoulseekInterestsAsync(
            "alice",
            new UserInterests("alice", new[] { "artist:two" }, Array.Empty<string>()));

        var records = await service.ListAsync(new OpinionQuery
        {
            Issuer = "soulseek:alice",
            Source = "soulseek-interest",
            Limit = 10,
        });

        Assert.Single(records);
        Assert.Equal(OpinionSubjectType.Artist, records[0].SubjectType);
        Assert.Equal("two", records[0].SubjectId);
        Assert.Equal(OpinionKind.Like, records[0].Kind);
        Assert.Equal(0.25, records[0].Confidence);
    }

    private static string CreateStorePath()
        => Path.Combine(Path.GetTempPath(), $"slskdn-opinions-{Guid.NewGuid():N}.json");
}
