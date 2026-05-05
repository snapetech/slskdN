// <copyright file="FederationDiagnosticsControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.SoulseekDiscovery;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Common;
using slskd.Mesh;
using slskd.PodCore;
using slskd.SocialFederation;
using slskd.SocialFederation.API;
using Xunit;

public sealed class FederationDiagnosticsControllerTests
{
    [Fact]
    public void GetDiagnostics_ReturnsReadOnlyFederationPosture()
    {
        var controller = CreateController(
            new SocialFederationOptions
            {
                Enabled = true,
                Mode = "Public",
                BaseUrl = "https://example.test",
                Domain = "example.test",
                VerifySignatures = true,
            },
            new FederationPublishingOptions
            {
                Enabled = true,
                PublishableDomains = new[] { "music" },
                RequireModerationApproval = true,
            },
            SignatureMode.Enforce,
            SignatureMode.Warn);

        var result = Assert.IsType<OkObjectResult>(controller.GetDiagnostics());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"exposure\":\"Public\"", json);
        Assert.Contains("\"domainConfigured\":true", json);
        Assert.Contains("\"messageSignatureMode\":\"Warn\"", json);
        Assert.DoesNotContain("privateKey", json);
    }

    [Fact]
    public void GetDiagnostics_FlagsUnsafePublicFederationSettings()
    {
        var controller = CreateController(
            new SocialFederationOptions
            {
                Enabled = true,
                Mode = "Public",
                VerifySignatures = false,
            },
            new FederationPublishingOptions
            {
                Enabled = true,
                DefaultVisibility = "public",
                RequireModerationApproval = false,
            },
            SignatureMode.Off,
            SignatureMode.Off);

        var result = Assert.IsType<OkObjectResult>(controller.GetDiagnostics());
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("baseUrl is not configured", json);
        Assert.Contains("signature verification is disabled", json);
        Assert.Contains("without moderation approval", json);
        Assert.Contains("Pod join signatures are not enforced", json);
        Assert.Contains("Pod message signatures are not enforced", json);
    }

    private static FederationDiagnosticsController CreateController(
        SocialFederationOptions federation,
        FederationPublishingOptions publishing,
        SignatureMode joinMode,
        SignatureMode messageMode)
    {
        var federationMonitor = Mock.Of<IOptionsMonitor<SocialFederationOptions>>(x => x.CurrentValue == federation);
        var publishingMonitor = Mock.Of<IOptionsMonitor<FederationPublishingOptions>>(x => x.CurrentValue == publishing);
        var joinMonitor = Mock.Of<IOptionsMonitor<PodJoinOptions>>(x => x.CurrentValue == new PodJoinOptions { SignatureMode = joinMode });
        var signerMonitor = Mock.Of<IOptionsMonitor<PodMessageSignerOptions>>(x => x.CurrentValue == new PodMessageSignerOptions { SignatureMode = messageMode });

        return new FederationDiagnosticsController(
            federationMonitor,
            publishingMonitor,
            joinMonitor,
            signerMonitor,
            Options.Create(new MeshOptions { SelfPeerId = "mesh-self" }));
    }
}
