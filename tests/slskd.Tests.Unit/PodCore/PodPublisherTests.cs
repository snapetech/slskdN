// <copyright file="PodPublisherTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.PodCore;

using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Mesh.Dht;
using slskd.PodCore;
using Xunit;

[Collection("GoldStarClubEnv")]
public sealed class PodPublisherTests
{
    private sealed class EnvScope : IDisposable
    {
        private readonly string? previous;

        public EnvScope(string? value)
        {
            previous = Environment.GetEnvironmentVariable(GoldStarClubService.AutoJoinEnvironmentVariable);
            Environment.SetEnvironmentVariable(GoldStarClubService.AutoJoinEnvironmentVariable, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(GoldStarClubService.AutoJoinEnvironmentVariable, previous);
    }

    [Fact]
    public async Task PublishPodAsync_DoesNotPublishGoldStarWithoutExplicitOptIn()
    {
        using var _ = new EnvScope(null);
        var dht = new Mock<IMeshDhtClient>();
        var publisher = CreatePublisher(dht.Object);

        await publisher.PublishPodAsync(new Pod
        {
            PodId = GoldStarClubService.GoldStarClubPodId,
            Name = "Gold Star Club ⭐",
            Visibility = PodVisibility.Listed,
        });

        Assert.Empty(dht.Invocations);
    }

    [Fact]
    public async Task RefreshListedPodsAsync_PublishesMetadataAndRefreshesIndexOnce()
    {
        var dht = new Mock<IMeshDhtClient>();
        var existingIndex = JsonSerializer.SerializeToUtf8Bytes(new PodIndex
        {
            PodIds = new List<string> { "pod-1", "pod-2", "pod-3" },
        });
        dht.Setup(client => client.GetRawAsync("pod:index:listed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIndex);
        dht.Setup(client => client.PutAsync(
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisher = CreatePublisher(dht.Object);
        var pods = new[]
        {
            ListedPod("pod-1"),
            ListedPod("pod-2"),
            ListedPod("pod-3"),
        };

        await publisher.RefreshListedPodsAsync(pods);

        dht.Verify(client => client.GetRawAsync("pod:index:listed", It.IsAny<CancellationToken>()), Times.Once);
        dht.Verify(client => client.PutAsync(
            It.Is<string>(key => key.StartsWith("pod:metadata:", StringComparison.Ordinal)),
            It.IsAny<object?>(),
            3600,
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        dht.Verify(client => client.PutAsync(
            "pod:index:listed",
            It.IsAny<object?>(),
            3600,
            It.IsAny<CancellationToken>()), Times.Once);

        var indexPut = Assert.Single(dht.Invocations, invocation =>
            invocation.Method.Name == nameof(IMeshDhtClient.PutAsync) &&
            Equals(invocation.Arguments[0], "pod:index:listed"));
        var updatedIndex = JsonSerializer.Deserialize<PodIndex>(Assert.IsType<byte[]>(indexPut.Arguments[1]));
        Assert.NotNull(updatedIndex);
        Assert.Equal(new[] { "pod-1", "pod-2", "pod-3" }, updatedIndex.PodIds);
        Assert.True(updatedIndex.UpdatedAt > 0);
    }

    [Fact]
    public async Task RefreshListedPodsAsync_PropagatesCancellationBeforeDhtCalls()
    {
        var dht = new Mock<IMeshDhtClient>();
        var publisher = CreatePublisher(dht.Object);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            publisher.RefreshListedPodsAsync(new[] { ListedPod("pod-1") }, cts.Token));

        Assert.Empty(dht.Invocations);
    }

    [Fact]
    public async Task BackgroundRefresh_QueriesAndPublishesOneListedSnapshot()
    {
        IReadOnlyList<Pod> listedPods = new[] { ListedPod("pod-1"), ListedPod("pod-2") };
        var podService = new Mock<IPodService>();
        podService.Setup(service => service.ListListedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(listedPods);
        var publisher = new Mock<IPodPublisher>();
        publisher.Setup(service => service.RefreshListedPodsAsync(
                It.IsAny<IReadOnlyList<Pod>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(podService.Object);
        await using var provider = services.BuildServiceProvider();
        using var backgroundService = new PodPublisherBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            publisher.Object,
            NullLogger<PodPublisherBackgroundService>.Instance);

        await backgroundService.RefreshOnceAsync(CancellationToken.None);

        podService.Verify(service => service.ListListedAsync(It.IsAny<CancellationToken>()), Times.Once);
        podService.Verify(service => service.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
        podService.Verify(service => service.GetPodAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(service => service.RefreshListedPodsAsync(
            It.Is<IReadOnlyList<Pod>>(pods => ReferenceEquals(pods, listedPods)),
            It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(service => service.RefreshPodAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PodPublisher CreatePublisher(IMeshDhtClient dht) => new(
        dht,
        Mock.Of<IServiceScopeFactory>(),
        NullLogger<PodPublisher>.Instance);

    private static Pod ListedPod(string podId) => new()
    {
        PodId = podId,
        Name = podId,
        Visibility = PodVisibility.Listed,
    };
}
