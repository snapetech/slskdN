// <copyright file="QuicOverlayFactory.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Mesh.Overlay;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Mesh.Privacy;

public static class QuicOverlayFactory
{
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static QuicOverlayClient CreateOverlayClient(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QuicOverlayClient>>();
        var options = serviceProvider.GetRequiredService<IOptions<OverlayOptions>>();
        var signer = serviceProvider.GetRequiredService<IControlSigner>();
        var privacyLayer = serviceProvider.GetService<IPrivacyLayer>();
        return new QuicOverlayClient(logger, options, signer, privacyLayer);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static QuicDataClient CreateDataClient(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<QuicDataClient>>();
        var options = serviceProvider.GetRequiredService<IOptions<DataOverlayOptions>>();
        return new QuicDataClient(logger, options);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static QuicOverlayServer CreateOverlayServer(IServiceProvider serviceProvider)
    {
        return ActivatorUtilities.CreateInstance<QuicOverlayServer>(serviceProvider);
    }

    public static bool ShouldRunStandaloneUdpOverlayServer(bool overlayEnabled, bool sharedMeshUdpRequested)
    {
        return overlayEnabled && !sharedMeshUdpRequested;
    }
}
