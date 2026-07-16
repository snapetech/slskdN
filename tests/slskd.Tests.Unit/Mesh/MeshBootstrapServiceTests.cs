// <copyright file="MeshBootstrapServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.Bootstrap;
using slskd.Mesh.Dht;
using Xunit;

public class MeshBootstrapServiceTests
{
    [Fact]
    public async Task StartAsync_PublishesOnceAndLeavesRecurringRefreshToRefreshService()
    {
        var publisher = new CountingPeerDescriptorPublisher();
        var service = new MeshBootstrapService(
            NullLogger<MeshBootstrapService>.Instance,
            publisher,
            Options.Create(new MeshOptions { EnableDht = true }));

        await service.StartAsync(CancellationToken.None);
        var executeTask = service.ExecuteTask
            ?? throw new InvalidOperationException("Bootstrap execution task was not created.");
        await executeTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(1, publisher.PublishCount);
        await service.StopAsync(CancellationToken.None);
    }

    private sealed class CountingPeerDescriptorPublisher : IPeerDescriptorPublisher
    {
        public int PublishCount { get; private set; }

        public Task PublishSelfAsync(CancellationToken ct = default)
        {
            PublishCount++;
            return Task.CompletedTask;
        }

        public Task MarkPeerRequiresRelayAsync(string peerId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
