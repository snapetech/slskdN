// <copyright file="PodApiAuthorizerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System.Security.Claims;
using Moq;
using slskd.PodCore;
using slskd.PodCore.API;
using Xunit;

public class PodApiAuthorizerTests
{
    [Fact]
    public async Task GetAccess_RequiresAuthenticatedMatchingUnbannedMembership()
    {
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PodMember { PeerId = "alice", Role = "member" },
                new PodMember { PeerId = "banned", Role = "owner", IsBanned = true },
            });

        var member = await PodApiAuthorizer.GetAccessAsync(User("alice"), podService.Object, "pod-1", CancellationToken.None);
        var outsider = await PodApiAuthorizer.GetAccessAsync(User("mallory"), podService.Object, "pod-1", CancellationToken.None);
        var banned = await PodApiAuthorizer.GetAccessAsync(User("banned"), podService.Object, "pod-1", CancellationToken.None);
        var missingIdentity = await PodApiAuthorizer.GetAccessAsync(new ClaimsPrincipal(), podService.Object, "pod-1", CancellationToken.None);

        Assert.True(member.IsMember);
        Assert.False(member.CanModerate);
        Assert.False(outsider.IsMember);
        Assert.False(banned.IsMember);
        Assert.Null(missingIdentity.PeerId);
    }

    [Fact]
    public async Task GetAccess_MapsOwnerAndModeratorRolesToMutationAccess()
    {
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PodMember { PeerId = "owner", Role = "owner" },
                new PodMember { PeerId = "moderator", Role = "mod" },
            });

        var owner = await PodApiAuthorizer.GetAccessAsync(User("owner"), podService.Object, "pod-1", CancellationToken.None);
        var moderator = await PodApiAuthorizer.GetAccessAsync(User("moderator"), podService.Object, "pod-1", CancellationToken.None);

        Assert.True(owner.CanModerate);
        Assert.True(moderator.CanModerate);
    }

    private static ClaimsPrincipal User(string name) => new(
        new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "test"));
}
