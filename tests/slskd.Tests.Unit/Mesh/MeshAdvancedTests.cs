// <copyright file="MeshAdvancedTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh;
using Xunit;

namespace slskd.Tests.Unit.Mesh;

public class MeshAdvancedTests
{
    [Fact]
    public async Task TraceRoutesAsync_WhenDirectoryLookupIsCanceled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var directory = new Mock<IMeshDirectory>();
        directory
            .Setup(value => value.FindPeerByIdAsync("peer-id", It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<MeshPeerDescriptor?>(token);
            });

        var advanced = new MeshAdvanced(
            Mock.Of<ILogger<MeshAdvanced>>(),
            directory.Object,
            statsCollector: null!,
            dhtClient: null!,
            natTraversal: null!);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => advanced.TraceRoutesAsync("peer-id", cancellation.Token));
    }
}
