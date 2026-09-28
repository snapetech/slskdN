// <copyright file="ListeningPartyHubTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.ListeningParty;

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Moq;
using slskd.ListeningParty;
using slskd.PodCore;

public sealed class ListeningPartyHubTests
{
    [Theory]
    [InlineData("member", false, true)]
    [InlineData("member", true, false)]
    [InlineData("outsider", false, false)]
    public async Task JoinParty_RequiresMatchingUnbannedMembership(string username, bool banned, bool allowed)
    {
        var pods = new Mock<IPodService>();
        pods.Setup(service => service.GetMembersAsync("pod-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new PodMember { PeerId = "member", IsBanned = banned } });
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("connection-a");
        context.SetupGet(caller => caller.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "test")));
        var hub = new ListeningPartyHub(pods.Object) { Context = context.Object, Groups = groups.Object };

        if (allowed)
        {
            await hub.JoinParty(" pod-a ", " channel-a ");
            groups.Verify(manager => manager.AddToGroupAsync("connection-a", "party:pod-a:channel-a", It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            await Assert.ThrowsAsync<HubException>(() => hub.JoinParty("pod-a", "channel-a"));
            groups.Verify(manager => manager.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task JoinParty_AdministratorRetainsAccessWithoutMembershipLookup()
    {
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("admin-connection");
        context.SetupGet(caller => caller.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Administrator"),
        }, "test")));
        var pods = new Mock<IPodService>(MockBehavior.Strict);
        var hub = new ListeningPartyHub(pods.Object) { Context = context.Object, Groups = groups.Object };

        await hub.JoinParty("pod-a", "channel-a");

        groups.Verify(manager => manager.AddToGroupAsync("admin-connection", "party:pod-a:channel-a", It.IsAny<CancellationToken>()), Times.Once);
    }
}
