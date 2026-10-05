// <copyright file="ScenePeerDiscoveryTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.DisasterMode;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.VirtualSoulfind.DisasterMode;
using slskd.VirtualSoulfind.Scenes;
using Xunit;

public sealed class ScenePeerDiscoveryTests
{
    [Fact]
    public async Task DiscoverPeersAsync_WhenSceneMembershipLookupIsCanceled_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sceneService = new Mock<ISceneService>();
        sceneService
            .Setup(service => service.GetJoinedScenesAsync(cancellation.Token))
            .ReturnsAsync(new List<Scene> { new() { SceneId = "scene-a" } });
        var membershipTracker = new Mock<ISceneMembershipTracker>();
        membershipTracker
            .Setup(tracker => tracker.GetMembersAsync("scene-a", cancellation.Token))
            .Returns(Task.FromCanceled<List<SceneMember>>(cancellation.Token));
        var discovery = new ScenePeerDiscovery(
            NullLogger<ScenePeerDiscovery>.Instance,
            sceneService.Object,
            membershipTracker.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            discovery.DiscoverPeersAsync(cancellation.Token));
    }
}
