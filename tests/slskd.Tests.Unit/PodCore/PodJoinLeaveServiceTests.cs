// <copyright file="PodJoinLeaveServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.PodCore;

using System.Collections.Concurrent;
using System.Text;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Mesh.Transport;
using slskd.PodCore;
using Xunit;

public class PodJoinLeaveServiceTests
{
    [Fact]
    public async Task RequestJoinAsync_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var podService = new Mock<IPodService>();
        podService
            .Setup(service => service.GetPodAsync("pod-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = CreateService(podService: podService);

        var result = await service.RequestJoinAsync(
            new PodJoinRequest("pod-1", "peer-1", "member", "pub", 1, "long-signature-value"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Failed to process join request", result.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessJoinAcceptanceAsync_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var membershipVerifier = new Mock<IPodMembershipVerifier>();
        membershipVerifier
            .Setup(service => service.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = CreateService(membershipVerifier: membershipVerifier);

        var result = await service.ProcessJoinAcceptanceAsync(
            new PodJoinAcceptance("pod-1", "peer-1", "member", "owner-1", "pub", 1, "long-signature-value"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Failed to process join acceptance", result.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage);
    }

    [Fact]
    public async Task RequestLeaveAsync_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var podService = new Mock<IPodService>();
        podService
            .Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = CreateService(podService: podService);

        var result = await service.RequestLeaveAsync(
            new PodLeaveRequest("pod-1", "peer-1", "pub", 1, "long-signature-value"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Failed to process leave request", result.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessLeaveAcceptanceAsync_WhenDependencyThrows_ReturnsSanitizedError()
    {
        var membershipVerifier = new Mock<IPodMembershipVerifier>();
        membershipVerifier
            .Setup(service => service.HasRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var service = CreateService(membershipVerifier: membershipVerifier);

        var result = await service.ProcessLeaveAcceptanceAsync(
            new PodLeaveAcceptance("pod-1", "peer-1", "owner-1", "pub", 1, "long-signature-value"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Failed to process leave acceptance", result.ErrorMessage);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPendingJoinRequestsAsync_WhenPodHasNoRequests_DoesNotCreateBucket()
    {
        var service = CreateService();

        var result = await service.GetPendingJoinRequestsAsync("pod-1", CancellationToken.None);

        Assert.Empty(result);

        var field = typeof(PodJoinLeaveService).GetField("_pendingJoinRequests", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var pending = Assert.IsType<ConcurrentDictionary<string, ConcurrentBag<PodJoinRequest>>>(field!.GetValue(service));
        Assert.False(pending.ContainsKey("pod-1"));
    }

    [Fact]
    public async Task CancelJoinRequestAsync_MatchesPeerIdCaseInsensitively()
    {
        var service = CreateService();
        var field = typeof(PodJoinLeaveService).GetField("_pendingJoinRequests", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var pending = Assert.IsType<ConcurrentDictionary<string, ConcurrentBag<PodJoinRequest>>>(field!.GetValue(service));
        pending["pod-1"] = new ConcurrentBag<PodJoinRequest>(new[]
        {
            new PodJoinRequest("pod-1", "Peer-1", "member", "pub", 1, "sig"),
        });

        var cancelled = await service.CancelJoinRequestAsync("pod-1", "peer-1", CancellationToken.None);

        Assert.True(cancelled);
        var remaining = await service.GetPendingJoinRequestsAsync("pod-1", CancellationToken.None);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task RequestJoinAsync_WhenEnforcedSignatureValid_AcceptsRequest()
    {
        using var ed25519 = new Ed25519Signer();
        var (privateKey, publicKey) = ed25519.GenerateKeyPair();
        var publicKeyBase64 = Convert.ToBase64String(publicKey);
        var request = new PodJoinRequest(
            "pod-1",
            "peer-1",
            "member",
            publicKeyBase64,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            string.Empty,
            "please",
            "nonce-1");
        request = request with
        {
            Signature = Sign(ed25519, privateKey, PodJoinLeaveService.BuildJoinRequestPayload(request))
        };

        var podService = new Mock<IPodService>();
        podService
            .Setup(service => service.GetPodAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pod { PodId = "pod-1" });
        podService
            .Setup(service => service.GetMembersAsync("pod-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService(
            podService: podService,
            signatureMode: SignatureMode.Enforce,
            ed25519: ed25519);

        var result = await service.RequestJoinAsync(request, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task RequestJoinAsync_WhenEnforcedSignatureIsLegacyShape_RejectsRequest()
    {
        using var ed25519 = new Ed25519Signer();
        var service = CreateService(signatureMode: SignatureMode.Enforce, ed25519: ed25519);

        var result = await service.RequestJoinAsync(
            new PodJoinRequest("pod-1", "peer-1", "member", "pub", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "long-signature-value", Nonce: "nonce-1"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid join request signature", result.ErrorMessage);
    }

    [Fact]
    public async Task RequestJoinAsync_WhenSignatureTampered_RejectsRequest()
    {
        using var ed25519 = new Ed25519Signer();
        var (privateKey, publicKey) = ed25519.GenerateKeyPair();
        var publicKeyBase64 = Convert.ToBase64String(publicKey);
        var request = new PodJoinRequest(
            "pod-1",
            "peer-1",
            "member",
            publicKeyBase64,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            string.Empty,
            "please",
            "nonce-1");
        request = request with
        {
            Signature = Sign(ed25519, privateKey, PodJoinLeaveService.BuildJoinRequestPayload(request)),
            Message = "tampered"
        };

        var service = CreateService(signatureMode: SignatureMode.Enforce, ed25519: ed25519);

        var result = await service.RequestJoinAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid join request signature", result.ErrorMessage);
    }

    [Fact]
    public async Task RequestJoinAsync_WhenSignatureModeOff_AllowsLegacyShape()
    {
        var service = CreateService(signatureMode: SignatureMode.Off);

        var result = await service.RequestJoinAsync(
            new PodJoinRequest("pod-1", "peer-1", "member", "pub", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "long-signature-value"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Pod not found", result.ErrorMessage);
    }

    private static PodJoinLeaveService CreateService(
        Mock<IPodService>? podService = null,
        Mock<IPodMembershipService>? membershipService = null,
        Mock<IPodMembershipVerifier>? membershipVerifier = null,
        SignatureMode signatureMode = SignatureMode.Off,
        Ed25519Signer? ed25519 = null)
    {
        return new PodJoinLeaveService(
            Mock.Of<ILogger<PodJoinLeaveService>>(),
            (podService ?? new Mock<IPodService>()).Object,
            (membershipService ?? new Mock<IPodMembershipService>()).Object,
            (membershipVerifier ?? new Mock<IPodMembershipVerifier>()).Object,
            Mock.Of<IOptionsMonitor<PodJoinOptions>>(options => options.CurrentValue == new PodJoinOptions { SignatureMode = signatureMode }),
            ed25519 ?? new Ed25519Signer());
    }

    private static string Sign(Ed25519Signer ed25519, byte[] privateKey, string payload)
    {
        var signature = ed25519.Sign(Encoding.UTF8.GetBytes(payload), privateKey);
        return $"ed25519:{Convert.ToBase64String(signature)}";
    }
}
