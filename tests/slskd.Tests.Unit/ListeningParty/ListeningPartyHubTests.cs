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
        var parties = new Mock<IListeningPartyService>();
        parties.Setup(service => service.Subscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>())).Returns(true);
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("connection-a");
        context.SetupGet(caller => caller.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "test")));
        var hub = new ListeningPartyHub(pods.Object, parties.Object) { Context = context.Object };

        if (allowed)
        {
            await hub.JoinParty(" pod-a ", " channel-a ");
            parties.Verify(service => service.Subscribe("connection-a", "pod-a", "channel-a", context.Object.User!), Times.Once);
        }
        else
        {
            await Assert.ThrowsAsync<HubException>(() => hub.JoinParty("pod-a", "channel-a"));
            parties.Verify(service => service.Subscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>()), Times.Never);
        }
    }

    [Fact]
    public async Task JoinParty_AdministratorRetainsAccessWithoutMembershipLookup()
    {
        var parties = new Mock<IListeningPartyService>();
        parties.Setup(service => service.Subscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>())).Returns(true);
        var context = new Mock<HubCallerContext>();
        context.SetupGet(caller => caller.ConnectionId).Returns("admin-connection");
        context.SetupGet(caller => caller.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Administrator"),
        }, "test")));
        var pods = new Mock<IPodService>(MockBehavior.Strict);
        var hub = new ListeningPartyHub(pods.Object, parties.Object) { Context = context.Object };

        await hub.JoinParty("pod-a", "channel-a");

        parties.Verify(service => service.Subscribe("admin-connection", "pod-a", "channel-a", context.Object.User!), Times.Once);
    }
    [Fact]
    public async Task JoinParty_RejectsCapacityAndDoesNotResurrectCancelledConnections()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator") }, "test"));
        var context = new Mock<HubCallerContext>();
        context.SetupGet(instance => instance.ConnectionId).Returns("connection");
        context.SetupGet(instance => instance.User).Returns(user);
        var parties = new Mock<IListeningPartyService>();
        var hub = new ListeningPartyHub(Mock.Of<IPodService>(), parties.Object) { Context = context.Object };
        await Assert.ThrowsAsync<HubException>(() => hub.JoinParty("pod", "room"));
        parties.Verify(instance => instance.Subscribe("connection", "pod", "room", user), Times.Once);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        context.SetupGet(instance => instance.ConnectionAborted).Returns(cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => hub.JoinParty("pod", "room"));
        parties.Verify(instance => instance.Subscribe("connection", "pod", "room", user), Times.Once);
        await hub.LeaveParty(" pod ", " room ");
        await hub.OnDisconnectedAsync(null);
        parties.Verify(instance => instance.Unsubscribe("connection", "pod", "room"), Times.Once);
        parties.Verify(instance => instance.Disconnect("connection"), Times.Once);
    }

}
