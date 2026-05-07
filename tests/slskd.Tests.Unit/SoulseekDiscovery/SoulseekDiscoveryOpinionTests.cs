// <copyright file="SoulseekDiscoveryOpinionTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SoulseekDiscovery;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Opinions;
using slskd.SoulseekDiscovery;
using Soulseek;
using Xunit;

public sealed class SoulseekDiscoveryOpinionTests
{
    [Fact]
    public async Task AddInterestAsync_Records_Local_Soulseek_Opinion()
    {
        var client = new Mock<ISoulseekClient>();
        client.SetupGet(c => c.Username).Returns("local-user");
        client.Setup(c => c.AddInterestAsync("Aphex Twin", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var opinions = new OpinionService(
            NullLogger<OpinionService>.Instance,
            Path.Combine(Path.GetTempPath(), $"slskdn-opinions-{Guid.NewGuid():N}.json"));

        var service = new SoulseekDiscoveryService(client.Object, opinionService: opinions);

        await service.AddInterestAsync(" Aphex Twin ");

        var records = await opinions.ListAsync(new OpinionQuery
        {
            Issuer = "soulseek:local-user",
            SubjectId = "Aphex Twin",
            Source = "soulseek-interest",
        });

        Assert.Single(records);
        Assert.Equal(OpinionKind.Like, records[0].Kind);
        Assert.Equal("soulseek-public", records[0].Scope);
    }

    [Fact]
    public async Task GetUserInterestsAsync_Imports_Remote_Soulseek_Interests()
    {
        var client = new Mock<ISoulseekClient>();
        client.Setup(c => c.GetUserInterestsAsync("remote-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInterests("remote-user", new[] { "drum and bass" }, new[] { "bad rips" }));

        var opinions = new OpinionService(
            NullLogger<OpinionService>.Instance,
            Path.Combine(Path.GetTempPath(), $"slskdn-opinions-{Guid.NewGuid():N}.json"));

        var service = new SoulseekDiscoveryService(client.Object, opinionService: opinions);

        await service.GetUserInterestsAsync(" remote-user ");

        var records = await opinions.ListAsync(new OpinionQuery
        {
            Issuer = "soulseek:remote-user",
            Source = "soulseek-interest",
            Limit = 10,
        });

        Assert.Equal(2, records.Count);
        Assert.Contains(records, record => record.Kind == OpinionKind.Like && record.SubjectId == "drum and bass");
        Assert.Contains(records, record => record.Kind == OpinionKind.Hate && record.SubjectId == "bad rips");
    }
}
