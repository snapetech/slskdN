// <copyright file="OpinionServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Opinions;

using Microsoft.Extensions.Logging.Abstractions;
using slskd.Opinions;
using Soulseek;
using Xunit;

[Collection(AllocationTestCollection.Name)]
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

    [Fact]
    public void BuildOpinionList_PreservesFiltersNewestOrderStableTiesAndClones()
    {
        const long now = 1_000;
        var tiedFirst = CreateOpinion("tied-first", 200);
        var tiedSecond = CreateOpinion("tied-second", 200);
        var newest = CreateOpinion("newest", 300);
        var expired = CreateOpinion("expired", 400);
        expired.ExpiresUnixMs = now;
        var otherIssuer = CreateOpinion("other-issuer", 500);
        otherIssuer.Issuer = "other";
        var records = new List<OpinionRecord>
        {
            tiedFirst,
            tiedSecond,
            newest,
            expired,
            otherIssuer,
        };

        var result = OpinionService.BuildOpinionList(records, new OpinionQuery
        {
            Issuer = " issuer ",
            SubjectType = OpinionSubjectType.User,
            SubjectId = " subject ",
            Kind = OpinionKind.Trust,
            Scope = " scope ",
            Source = " source ",
            Limit = 2,
        }, now);

        Assert.Equal(new[] { "newest", "tied-first" }, result.Select(opinion => opinion.Id));
        Assert.NotSame(newest, result[0]);
        Assert.NotSame(tiedFirst, result[1]);
    }

    [Fact]
    public void BuildOpinionList_WideFilteredInputHasBoundedAllocation()
    {
        const int opinionCount = 10_000;
        const int limit = 50;
        var records = Enumerable.Range(0, opinionCount)
            .Select(index => CreateOpinion($"opinion-{index}", index))
            .ToList();
        var query = new OpinionQuery
        {
            Issuer = " issuer ",
            SubjectType = OpinionSubjectType.User,
            SubjectId = " subject ",
            Kind = OpinionKind.Trust,
            Scope = " scope ",
            Source = " source ",
            IncludeExpired = true,
            Limit = limit,
        };
        _ = OpinionService.BuildOpinionList(records.Take(100), query, opinionCount);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = OpinionService.BuildOpinionList(records, query, opinionCount);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(limit, result.Count);
        Assert.Equal("opinion-9999", result[0].Id);
        Assert.Equal("opinion-9950", result[^1].Id);
        Assert.True(
            allocatedBytes < 150_000,
            $"Expected opinion list construction below 150 KB allocated, got {allocatedBytes:N0} bytes.");
    }

    private static OpinionRecord CreateOpinion(string id, long updatedUnixMs)
    {
        return new OpinionRecord
        {
            Id = id,
            Issuer = "issuer",
            SubjectType = OpinionSubjectType.User,
            SubjectId = "subject",
            Kind = OpinionKind.Trust,
            Scope = "scope",
            Source = "source",
            Strength = 0.5,
            Confidence = 0.75,
            UpdatedUnixMs = updatedUnixMs,
        };
    }

    private static string CreateStorePath()
        => Path.Combine(Path.GetTempPath(), $"slskdn-opinions-{Guid.NewGuid():N}.json");
}
