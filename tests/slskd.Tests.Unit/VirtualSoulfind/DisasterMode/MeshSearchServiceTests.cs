// <copyright file="MeshSearchServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.DisasterMode;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using slskd.VirtualSoulfind.DisasterMode;
using Xunit;

public class MeshSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_EscapesLogBreakingQueryAndRetainsOriginalQuery()
    {
        var query = "alpha\r\nforged";
        var logger = new Mock<ILogger<MeshSearchService>>();
        logger.Setup(entry => entry.IsEnabled(LogLevel.Information)).Returns(true);
        var service = new MeshSearchService(logger.Object);

        var result = await service.SearchAsync(query);

        Assert.Equal(query, result.Query);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("alpha\\r\\nforged") &&
                    !state.ToString()!.Contains(query)),
                It.IsAny<System.Exception?>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WhenPeerLookupIsCanceled_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var directory = new Mock<IMeshDirectory>();
        directory
            .Setup(mesh => mesh.FindContentByPeerAsync("peer-a", cancellation.Token))
            .Returns(Task.FromCanceled<IReadOnlyList<MeshContentDescriptor>>(cancellation.Token));
        var sync = new Mock<IMeshSyncService>();
        sync.Setup(mesh => mesh.GetMeshPeers()).Returns(new[] { new MeshPeerInfo { Username = "peer-a" } });
        var service = new MeshSearchService(
            Mock.Of<ILogger<MeshSearchService>>(),
            directory.Object,
            sync.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchAsync("search", cancellation.Token));
    }

    [Fact]
    public async Task SearchByMbidAsync_WhenDirectoryLookupIsCanceled_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var directory = new Mock<IMeshDirectory>();
        directory
            .Setup(mesh => mesh.FindPeersByContentAsync("mbid:recording:recording-a", cancellation.Token))
            .Returns(Task.FromCanceled<IReadOnlyList<MeshPeerDescriptor>>(cancellation.Token));
        var service = new MeshSearchService(Mock.Of<ILogger<MeshSearchService>>(), directory.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SearchByMbidAsync("recording-a", cancellation.Token));
    }
}
