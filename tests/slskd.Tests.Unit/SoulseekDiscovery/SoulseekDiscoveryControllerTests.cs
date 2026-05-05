// <copyright file="SoulseekDiscoveryControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.SoulseekDiscovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Common.Security;
using slskd.Mesh;
using slskd.SoulseekDiscovery;
using slskd.SoulseekDiscovery.API;
using Soulseek;
using Xunit;

public sealed class SoulseekDiscoveryControllerTests
{
    private readonly Mock<ISoulseekDiscoveryService> discoveryService = new(MockBehavior.Strict);
    private readonly Mock<ISoulseekSafetyLimiter> safetyLimiter = new(MockBehavior.Strict);

    [Fact]
    public void GetMeshRendezvousStatus_ReturnsConfiguredState()
    {
        var controller = CreateController(enableSoulseekRendezvous: true);

        var result = Assert.IsType<OkObjectResult>(controller.GetMeshRendezvousStatus());
        Assert.NotNull(result.Value);
        var value = result.Value;

        Assert.Equal(true, GetPropertyValue(value, "enabled"));
        Assert.Equal(SoulseekClient.MeshRendezvousInterestTag, GetPropertyValue(value, "interestTag"));
        Assert.Contains("publishes", Assert.IsType<string>(GetPropertyValue(value, "privacy")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMeshRendezvousInterest_WhenDisabled_ReturnsForbidAndDoesNotPublish()
    {
        var controller = CreateController(enableSoulseekRendezvous: false);

        var result = await controller.AddMeshRendezvousInterest(CancellationToken.None).ConfigureAwait(false);

        Assert.IsType<ForbidResult>(result);
        discoveryService.Verify(x => x.AddMeshRendezvousInterestAsync(It.IsAny<CancellationToken>()), Times.Never);
        safetyLimiter.Verify(x => x.TryConsumeSearch(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddMeshRendezvousInterest_WhenEnabled_UsesLimiterAndPublishes()
    {
        safetyLimiter.Setup(x => x.TryConsumeSearch("soulseek-mesh-rendezvous")).Returns(true);
        discoveryService
            .Setup(x => x.AddMeshRendezvousInterestAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(enableSoulseekRendezvous: true);

        var result = await controller.AddMeshRendezvousInterest(CancellationToken.None).ConfigureAwait(false);

        Assert.IsType<NoContentResult>(result);
        safetyLimiter.Verify(x => x.TryConsumeSearch("soulseek-mesh-rendezvous"), Times.Once);
        discoveryService.Verify(x => x.AddMeshRendezvousInterestAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMeshRendezvousUsers_WhenEnabled_UsesLimiterAndReturnsUsers()
    {
        var users = new List<SimilarUser> { new(username: "mesh-peer", rating: 10) };
        safetyLimiter.Setup(x => x.TryConsumeSearch("soulseek-mesh-rendezvous")).Returns(true);
        discoveryService
            .Setup(x => x.GetMeshRendezvousUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        var controller = CreateController(enableSoulseekRendezvous: true);

        var result = await controller.GetMeshRendezvousUsers(CancellationToken.None).ConfigureAwait(false);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(users, ok.Value);
        safetyLimiter.Verify(x => x.TryConsumeSearch("soulseek-mesh-rendezvous"), Times.Once);
    }

    [Fact]
    public async Task DiscoverMeshRendezvous_WhenEnabled_ReturnsCapabilityRecords()
    {
        var result = new MeshRendezvousResult(
            SoulseekClient.MeshRendezvousInterestTag,
            new List<SimilarUser> { new(username: "mesh-peer", rating: 10) },
            new[]
            {
                new PeerCapabilityRecord(
                    "mesh-peer",
                    null!,
                    new PeerCapabilityDescriptor("peer-id", new[] { "mesh_sync" }, overlayPort: 50305),
                    PeerCapabilityMessageType.Hello,
                    "nonce",
                    DateTimeOffset.UtcNow),
            });
        safetyLimiter.Setup(x => x.TryConsumeSearch("soulseek-mesh-rendezvous")).Returns(true);
        discoveryService
            .Setup(x => x.DiscoverMeshRendezvousAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        var controller = CreateController(enableSoulseekRendezvous: true);

        var actionResult = await controller.DiscoverMeshRendezvous(CancellationToken.None).ConfigureAwait(false);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<SoulseekMeshRendezvousResponse>(ok.Value);
        Assert.Single(response.CapabilityRecords);
        Assert.Equal("peer-id", response.CapabilityRecords.First().PeerId);
        safetyLimiter.Verify(x => x.TryConsumeSearch("soulseek-mesh-rendezvous"), Times.Once);
    }

    [Fact]
    public void GetPeerCapabilities_ReturnsCapabilityRecords()
    {
        discoveryService
            .Setup(x => x.GetPeerCapabilityRecords())
            .Returns(new[]
            {
                new PeerCapabilityRecord(
                    "mesh-peer",
                    null!,
                    new PeerCapabilityDescriptor("peer-id", new[] { "mesh_sync" }, overlayPort: 50305),
                    PeerCapabilityMessageType.Acknowledgement,
                    "nonce",
                    DateTimeOffset.UtcNow),
            });
        var controller = CreateController(enableSoulseekRendezvous: true);

        var result = controller.GetPeerCapabilities();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SoulseekPeerCapabilityResponse[]>(ok.Value);
        Assert.Single(response);
        Assert.Equal("mesh-peer", response[0].Username);
        Assert.Equal("peer-id", response[0].PeerId);
    }

    private static object? GetPropertyValue(object value, string name)
        => value.GetType().GetProperty(name)?.GetValue(value);

    private SoulseekDiscoveryController CreateController(bool enableSoulseekRendezvous)
        => new(
            discoveryService.Object,
            safetyLimiter.Object,
            NullLogger<SoulseekDiscoveryController>.Instance,
            Options.Create(new MeshOptions { EnableSoulseekRendezvous = enableSoulseekRendezvous }));
}
