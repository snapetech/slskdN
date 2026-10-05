// <copyright file="PeerDescriptorRefreshServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.Dht;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public class PeerDescriptorRefreshServiceTests
{
    [Fact]
    public async Task StartAsync_DoesNotImmediatelyDuplicateBootstrapPublish()
    {
        var publisher = new CountingPeerDescriptorPublisher();
        var options = Options.Create(new MeshOptions
        {
            EnableDht = true,
            PeerDescriptorRefresh = new PeerDescriptorRefreshOptions
            {
                EnableIpChangeDetection = false,
                RefreshInterval = TimeSpan.FromMinutes(30)
            }
        });
        var service = new PeerDescriptorRefreshService(
            NullLogger<PeerDescriptorRefreshService>.Instance,
            publisher,
            options);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, publisher.PublishCount);
    }

    [Fact]
    public async Task StopAsync_DuringRefresh_DoesNotLogCancellationAsFailure()
    {
        var publisher = new BlockingPeerDescriptorPublisher();
        var logger = new CapturingLogger<PeerDescriptorRefreshService>();
        var options = Options.Create(new MeshOptions
        {
            EnableDht = true,
            PeerDescriptorRefresh = new PeerDescriptorRefreshOptions
            {
                EnableIpChangeDetection = false,
                RefreshInterval = TimeSpan.Zero,
            }
        });
        var service = new PeerDescriptorRefreshService(logger, publisher, options);

        await service.StartAsync(CancellationToken.None);
        await publisher.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.DoesNotContain(logger.Entries, entry =>
            entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            entry.Message.Contains("Peer descriptor refresh failed", StringComparison.Ordinal));
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

    private sealed class BlockingPeerDescriptorPublisher : IPeerDescriptorPublisher
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task PublishSelfAsync(CancellationToken ct = default)
        {
            Started.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, ct);
        }

        public Task MarkPeerRequiresRelayAsync(string peerId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
