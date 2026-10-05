// <copyright file="HolePunchMeshServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.ServiceFabric;

using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh.Nat;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using Xunit;

public class HolePunchMeshServiceTests
{
    [Fact]
    public async Task HandleCallAsync_RequestPunch_WhenCallerCancelsForwarding_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-target", It.IsAny<ServiceCall>(), cancellation.Token))
            .Returns(Task.FromCanceled<ServiceReply>(cancellation.Token));
        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            Mock.Of<IUdpHolePuncher>(),
            meshClient.Object);
        var call = new ServiceCall
        {
            ServiceName = "hole-punch",
            Method = "RequestPunch",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new slskd.Mesh.ServiceFabric.Services.HolePunchRequest
            {
                TargetPeerId = "peer-target",
                LocalEndpoints = new[] { "127.0.0.1:5000" },
            }),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            cancellation.Token));
    }

    [Fact]
    public async Task HandleCallAsync_ConfirmPunch_WhenCallerCancelsUdpPunch_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var holePuncher = new Mock<IUdpHolePuncher>();
        holePuncher
            .Setup(puncher => puncher.TryPunchAsync(
                It.IsAny<IPEndPoint>(),
                It.IsAny<IPEndPoint>(),
                cancellation.Token))
            .Returns(Task.FromCanceled<UdpHolePunchResult>(cancellation.Token));
        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            holePuncher.Object,
            Mock.Of<IMeshServiceClient>());
        var call = new ServiceCall
        {
            ServiceName = "hole-punch",
            Method = "ConfirmPunch",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new HolePunchForwardRequest
            {
                SessionId = "session-canceled",
                InitiatorPeerId = "peer-origin",
                InitiatorEndpoints = new[] { "127.0.0.1:5000" },
            }),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            cancellation.Token));
    }

    [Fact]
    public async Task HandleCallAsync_ConfirmPunch_WhenCanceled_RemovesSessionBeforeRetry()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var attemptedLocalEndpoints = new List<IPEndPoint>();
        var holePuncher = new Mock<IUdpHolePuncher>();
        holePuncher
            .Setup(puncher => puncher.TryPunchAsync(
                It.IsAny<IPEndPoint>(),
                It.IsAny<IPEndPoint>(),
                cancellation.Token))
            .Returns(Task.FromCanceled<UdpHolePunchResult>(cancellation.Token));
        holePuncher
            .Setup(puncher => puncher.TryPunchAsync(
                It.IsAny<IPEndPoint>(),
                It.IsAny<IPEndPoint>(),
                CancellationToken.None))
            .Callback<IPEndPoint, IPEndPoint, CancellationToken>((local, _, _) => attemptedLocalEndpoints.Add(local))
            .ReturnsAsync(new UdpHolePunchResult(true, new IPEndPoint(IPAddress.Loopback, 5000), TimeSpan.Zero));
        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            holePuncher.Object,
            Mock.Of<IMeshServiceClient>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.HandleCallAsync(
            ConfirmPunchCall("127.0.0.1:5000"),
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            cancellation.Token));

        var retryReply = await service.HandleCallAsync(
            ConfirmPunchCall("127.0.0.2:6000"),
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.OK, retryReply.StatusCode);
        Assert.NotEmpty(attemptedLocalEndpoints);
        Assert.All(attemptedLocalEndpoints, endpoint => Assert.Equal(IPAddress.Parse("127.0.0.2"), endpoint.Address));
        Assert.All(attemptedLocalEndpoints, endpoint => Assert.Equal(6000, endpoint.Port));
    }

    [Fact]
    public async Task HandleCallAsync_UnknownMethod_ReturnsSanitizedMethodNotFound()
    {
        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            Mock.Of<IUdpHolePuncher>(),
            Mock.Of<IMeshServiceClient>());

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "hole-punch",
                Method = "RequestPunchButActuallySensitive",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = Array.Empty<byte>()
            },
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.MethodNotFound, reply.StatusCode);
        Assert.Equal("Unknown hole punch method", reply.ErrorMessage);
        Assert.DoesNotContain("Sensitive", reply.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleCallAsync_RequestPunch_WhenTargetReplyFails_ReturnsSanitizedError()
    {
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-target", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceReply
            {
                StatusCode = ServiceStatusCodes.UnknownError,
                ErrorMessage = "sensitive detail",
                Payload = Array.Empty<byte>()
            });

        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            Mock.Of<IUdpHolePuncher>(),
            meshClient.Object);

        var call = new ServiceCall
        {
            ServiceName = "hole-punch",
            Method = "RequestPunch",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new slskd.Mesh.ServiceFabric.Services.HolePunchRequest
            {
                TargetPeerId = "peer-target",
                LocalEndpoints = new[] { "127.0.0.1:5000" }
            })
        };

        var reply = await service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.UnknownError, reply.StatusCode);
        Assert.Equal("Failed to contact target peer", reply.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_RequestPunch_WhenMeshClientThrows_ReturnsSanitizedError()
    {
        var meshClient = new Mock<IMeshServiceClient>();
        meshClient
            .Setup(client => client.CallAsync("peer-target", It.IsAny<ServiceCall>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            Mock.Of<IUdpHolePuncher>(),
            meshClient.Object);

        var call = new ServiceCall
        {
            ServiceName = "hole-punch",
            Method = "RequestPunch",
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.SerializeToUtf8Bytes(new slskd.Mesh.ServiceFabric.Services.HolePunchRequest
            {
                TargetPeerId = "peer-target",
                LocalEndpoints = new[] { "127.0.0.1:5000" }
            })
        };

        var reply = await service.HandleCallAsync(
            call,
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.UnknownError, reply.StatusCode);
        Assert.Equal("Failed to contact target peer", reply.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_RequestPunch_WithInvalidPayload_ReturnsSanitizedError()
    {
        var service = new HolePunchMeshService(
            Mock.Of<ILogger<HolePunchMeshService>>(),
            Mock.Of<IUdpHolePuncher>(),
            Mock.Of<IMeshServiceClient>());

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "hole-punch",
                Method = "RequestPunch",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    targetPeerId = "",
                    localEndpoints = Array.Empty<string>()
                })
            },
            new MeshServiceContext { RemotePeerId = "peer-origin" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.InvalidPayload, reply.StatusCode);
        Assert.Equal("Invalid JSON", reply.ErrorMessage);
        Assert.DoesNotContain("targetPeerId", reply.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceCall ConfirmPunchCall(string endpoint) => new()
    {
        ServiceName = "hole-punch",
        Method = "ConfirmPunch",
        CorrelationId = Guid.NewGuid().ToString(),
        Payload = JsonSerializer.SerializeToUtf8Bytes(new HolePunchForwardRequest
        {
            SessionId = "session-canceled",
            InitiatorPeerId = "peer-origin",
            InitiatorEndpoints = new[] { endpoint },
        }),
    };
}
