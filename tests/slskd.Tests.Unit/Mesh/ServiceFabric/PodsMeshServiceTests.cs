// <copyright file="PodsMeshServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.ServiceFabric;

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.Mesh.ServiceFabric;
using slskd.Mesh.ServiceFabric.Services;
using slskd.PodCore;
using Xunit;

public class PodsMeshServiceTests
{
    [Fact]
    public async Task HandleCallAsync_UnknownMethod_ReturnsSanitizedMethodNotFound()
    {
        var service = CreateService();

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "pods",
                Method = "SensitiveCustomPodsMethod",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = Array.Empty<byte>()
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.MethodNotFound, reply.StatusCode);
        Assert.Equal("Unknown method", reply.ErrorMessage);
        Assert.DoesNotContain("SensitiveCustomPodsMethod", reply.ErrorMessage);
    }

    [Fact]
    public async Task HandleCallAsync_PostMessage_TrimsIdsAndIncludesPodIdInMessage()
    {
        var podMessaging = new Mock<IPodMessaging>();
        podMessaging
            .Setup(service => service.SendAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(podMessaging.Object);

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "pods",
                Method = "PostMessage",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    PodId = " pod:00000000000000000000000000000001 ",
                    ChannelId = " general ",
                    Body = "hello",
                    Signature = " sig "
                })
            },
            new MeshServiceContext { RemotePeerId = " peer-1 " },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        podMessaging.Verify(
            svc => svc.SendAsync(
                It.Is<PodMessage>(message =>
                    message.PodId == "pod:00000000000000000000000000000001" &&
                    message.ChannelId == "general" &&
                    message.SenderPeerId == "peer-1" &&
                    message.Signature == "sig"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallAsync_GetMessages_TrimsIdsBeforeDispatch()
    {
        var podMessaging = new Mock<IPodMessaging>();
        podMessaging
            .Setup(service => service.GetMessagesAsync("pod:00000000000000000000000000000001", "general", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PodMessage>());

        var service = CreateService(podMessaging.Object);

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "pods",
                Method = "GetMessages",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    PodId = " pod:00000000000000000000000000000001 ",
                    ChannelId = " general ",
                    SinceTimestamp = 10L
                })
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        podMessaging.Verify(service => service.GetMessagesAsync("pod:00000000000000000000000000000001", "general", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallAsync_PostMessage_RejectsBodyOverFourKilobytesByUtf8Bytes()
    {
        var podMessaging = new Mock<IPodMessaging>();
        var service = CreateService(podMessaging.Object);

        var reply = await service.HandleCallAsync(
            new ServiceCall
            {
                ServiceName = "pods",
                Method = "PostMessage",
                CorrelationId = Guid.NewGuid().ToString(),
                Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    PodId = "pod:00000000000000000000000000000001",
                    ChannelId = "general",
                    Body = new string('\u00e9', 2049),
                    Signature = "sig"
                })
            },
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.Equal(ServiceStatusCodes.PayloadTooLarge, reply.StatusCode);
        podMessaging.Verify(
            svc => svc.SendAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleStreamAsync_GetMessagesRequest_SendsMessagesAndCloses()
    {
        var expectedMessages = new[]
        {
            new PodMessage
            {
                MessageId = "msg-1",
                PodId = "pod:00000000000000000000000000000001",
                ChannelId = "general",
                SenderPeerId = "peer-1",
                Body = "hello",
                TimestampUnixMs = 10
            }
        };

        var podMessaging = new Mock<IPodMessaging>();
        podMessaging
            .Setup(service => service.GetMessagesAsync("pod:00000000000000000000000000000001", "general", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMessages);

        var service = CreateService(podMessaging.Object);

        var stream = new TestMeshServiceStream(JsonSerializer.SerializeToUtf8Bytes(new
        {
            PodId = " pod:00000000000000000000000000000001 ",
            ChannelId = " general ",
            SinceTimestamp = 10L
        }));

        await service.HandleStreamAsync(
            stream,
            new MeshServiceContext { RemotePeerId = "peer-1" },
            CancellationToken.None);

        Assert.True(stream.Closed);
        Assert.Single(stream.SentPayloads);
        var sentMessages = JsonSerializer.Deserialize<PodMessage[]>(stream.SentPayloads[0]);
        Assert.NotNull(sentMessages);
        Assert.Single(sentMessages);
        Assert.Equal("msg-1", sentMessages[0].MessageId);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("mod")]
    [InlineData(" member ")]
    public async Task Join_PublicPod_UsesTransportIdentityAndMemberRole(string requestedRole)
    {
        var pods = CreatePods(isPublic: true);
        pods.Setup(service => service.JoinAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(pods: pods.Object);

        var reply = await service.HandleCallAsync(Call("Join", new { PodId, Role = requestedRole, PeerId = "owner" }),
            new MeshServiceContext { RemotePeerId = " peer-2 ", RemotePublicKey = "transport-key" });

        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        pods.Verify(service => service.JoinAsync(PodId,
            It.Is<PodMember>(member => member.PeerId == "peer-2" && member.Role == "member" && member.PublicKey == "transport-key"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Join_RejectsPrivateApprovalOrBannedAdmission(bool isPublic, bool approval, bool banned)
    {
        var pods = CreatePods(isPublic, approval, banned ? new PodMember { PeerId = "peer-2", IsBanned = true } : null);
        pods.Setup(service => service.JoinAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call("Join", new { PodId, Role = "owner" }),
            new MeshServiceContext { RemotePeerId = "peer-2" });

        Assert.Equal(ServiceStatusCodes.Forbidden, reply.StatusCode);
        pods.Verify(service => service.JoinAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Join_ExistingApprovedMember_DoesNotReplaceRoleOrKey()
    {
        var pods = CreatePods(false, true, new PodMember { PeerId = "peer-2", Role = "mod", PublicKey = "existing-key" });
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call("Join", new { PodId, Role = "owner" }),
            new MeshServiceContext { RemotePeerId = "peer-2", RemotePublicKey = "replacement-key" });

        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        pods.Verify(service => service.JoinAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Get", false)]
    [InlineData("Get", true)]
    [InlineData("Leave", false)]
    [InlineData("Leave", true)]
    [InlineData("GetMessages", false)]
    [InlineData("GetMessages", true)]
    [InlineData("PostMessage", false)]
    [InlineData("PostMessage", true)]
    public async Task PrivateRoom_DeniesUnrelatedAndBannedPeers(string method, bool banned)
    {
        var pods = CreatePods(false, member: banned ? new PodMember { PeerId = "peer-2", IsBanned = true } : null);
        var messaging = new Mock<IPodMessaging>();
        var reply = await CreateService(messaging.Object, pods.Object).HandleCallAsync(
            Call(method, new { PodId, ChannelId = "general", Body = "hello", PeerId = "peer-1" }),
            new MeshServiceContext { RemotePeerId = "peer-2" });

        Assert.Equal(ServiceStatusCodes.Forbidden, reply.StatusCode);
        messaging.Verify(service => service.GetMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()), Times.Never);
        messaging.Verify(service => service.SendAsync(It.IsAny<PodMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("GetMessages")]
    [InlineData("PostMessage")]
    public async Task PublicRoom_HistoryAndPostingStillRequireMembership(string method)
    {
        var messaging = new Mock<IPodMessaging>();
        var reply = await CreateService(messaging.Object, CreatePods(true).Object).HandleCallAsync(
            Call(method, new { PodId, ChannelId = "general", Body = "hello" }),
            new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.Equal(ServiceStatusCodes.Forbidden, reply.StatusCode);
        messaging.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stream_DeniesUnrelatedOrBannedPeerWithoutReadingHistory(bool banned)
    {
        var messaging = new Mock<IPodMessaging>();
        var pods = CreatePods(false, member: banned ? new PodMember { PeerId = "peer-2", IsBanned = true } : null);
        var stream = new TestMeshServiceStream(JsonSerializer.SerializeToUtf8Bytes(new { PodId, ChannelId = "general" }));
        await CreateService(messaging.Object, pods.Object).HandleStreamAsync(stream, new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.True(stream.Closed);
        Assert.Empty(stream.SentPayloads);
        messaging.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("List")]
    [InlineData("Join")]
    [InlineData("Leave")]
    [InlineData("Get")]
    [InlineData("GetMessages")]
    [InlineData("PostMessage")]
    public async Task Calls_RequireNonemptyTransportIdentity(string method)
    {
        var pods = CreatePods(true);
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call(method, new { PodId, ChannelId = "general" }),
            new MeshServiceContext { RemotePeerId = " " });
        Assert.Equal(ServiceStatusCodes.Unauthorized, reply.StatusCode);
        pods.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_HidesPrivateListedPodsFromUnrelatedPeer()
    {
        var pods = CreatePods(false);
        var privatePod = await pods.Object.GetPodAsync(PodId);
        privatePod!.Visibility = PodVisibility.Listed;
        pods.Setup(service => service.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { privatePod });
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call("List", new { }),
            new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        Assert.Empty(JsonSerializer.Deserialize<Pod[]>(reply.Payload)!);
    }

    [Fact]
    public async Task PublicPodMetadata_IsReadableBeforeJoining()
    {
        var reply = await CreateService(pods: CreatePods(true).Object).HandleCallAsync(Call("Get", new { PodId }),
            new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("List")]
    public async Task PrivateMetadata_ApprovedMemberRetainsAccess(string method)
    {
        var pods = CreatePods(false, member: new PodMember { PeerId = "peer-2" });
        pods.Setup(service => service.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Pod { PodId = PodId, Visibility = PodVisibility.Listed } });
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call(method, new { PodId }),
            new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.Equal(ServiceStatusCodes.OK, reply.StatusCode);
        if (method == "List")
        {
            Assert.Single(JsonSerializer.Deserialize<Pod[]>(reply.Payload)!);
        }
        else
        {
            Assert.Equal(PodId, JsonSerializer.Deserialize<Pod>(reply.Payload)!.PodId);
        }
    }

    [Fact]
    public async Task Join_MissingPod_DoesNotAttemptMembershipWrite()
    {
        var pods = new Mock<IPodService>();
        var reply = await CreateService(pods: pods.Object).HandleCallAsync(Call("Join", new { PodId }),
            new MeshServiceContext { RemotePeerId = "peer-2" });
        Assert.Equal(ServiceStatusCodes.ServiceNotFound, reply.StatusCode);
        pods.Verify(service => service.JoinAsync(It.IsAny<string>(), It.IsAny<PodMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Stream_EmptyIdentity_ClosesWithoutReadingHistory()
    {
        var messaging = new Mock<IPodMessaging>();
        var stream = new TestMeshServiceStream(JsonSerializer.SerializeToUtf8Bytes(new { PodId, ChannelId = "general" }));
        await CreateService(messaging.Object).HandleStreamAsync(stream, new MeshServiceContext { RemotePeerId = " " });
        Assert.True(stream.Closed);
        Assert.Empty(stream.SentPayloads);
        messaging.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RealPodService_RemoteJoinCannotGainOwnerOrEraseBan()
    {
        var pods = new PodService();
        await pods.CreateAsync(new Pod { PodId = PodId, Name = "Room", IsPublic = true });
        var service = CreateService(pods: pods);
        var context = new MeshServiceContext { RemotePeerId = "peer-2", RemotePublicKey = "transport-key" };
        var joined = await service.HandleCallAsync(Call("Join", new { PodId, Role = "owner" }), context);
        Assert.Equal(ServiceStatusCodes.OK, joined.StatusCode);
        Assert.Equal("member", Assert.Single(await pods.GetMembersAsync(PodId)).Role);
        Assert.True(await pods.BanAsync(PodId, "peer-2"));
        Assert.Equal(ServiceStatusCodes.Forbidden, (await service.HandleCallAsync(Call("Leave", new { PodId }), context)).StatusCode);
        Assert.Equal(ServiceStatusCodes.Forbidden, (await service.HandleCallAsync(Call("Join", new { PodId }), context)).StatusCode);
        Assert.Empty(await pods.GetMembersAsync(PodId));
    }

    private const string PodId = "pod:00000000000000000000000000000001";

    private static ServiceCall Call(string method, object payload) => new()
    {
        ServiceName = "pods",
        Method = method,
        CorrelationId = Guid.NewGuid().ToString(),
        Payload = JsonSerializer.SerializeToUtf8Bytes(payload),
    };

    private static Mock<IPodService> CreatePods(bool isPublic, bool approval = false, PodMember? member = null)
    {
        var pods = new Mock<IPodService>();
        pods.Setup(service => service.GetPodAsync(PodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pod { PodId = PodId, IsPublic = isPublic, RequireApproval = approval });
        pods.Setup(service => service.GetMembersAsync(PodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member == null ? Array.Empty<PodMember>() : new[] { member });
        return pods;
    }

    private sealed class TestMeshServiceStream : MeshServiceStream
    {
        private readonly byte[] _requestPayload;

        public TestMeshServiceStream(byte[] requestPayload)
        {
            _requestPayload = requestPayload;
        }

        public bool Closed { get; private set; }

        public List<byte[]> SentPayloads { get; } = new();

        public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            SentPayloads.Add(data.ToArray());
            return Task.CompletedTask;
        }

        public Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<byte[]?>(_requestPayload);
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            Closed = true;
            return Task.CompletedTask;
        }
    }

    private static PodsMeshService CreateService(IPodMessaging? podMessaging = null, IPodService? pods = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => podMessaging ?? Mock.Of<IPodMessaging>());
        var provider = services.BuildServiceProvider();

        return new PodsMeshService(
            Mock.Of<ILogger<PodsMeshService>>(),
            pods ?? CreatePods(false, member: new PodMember { PeerId = "peer-1" }).Object,
            provider.GetRequiredService<IServiceScopeFactory>());
    }
}
