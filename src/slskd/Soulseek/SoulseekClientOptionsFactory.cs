// <copyright file="SoulseekClientOptionsFactory.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.SoulseekRuntime;

using System.Net;
using slskd;
using Soulseek;

public static class SoulseekClientOptionsFactory
{
    public static SoulseekClientOptions CreateInitial(OptionsAtStartup optionsAtStartup)
    {
        if (!IPAddress.TryParse(optionsAtStartup.Soulseek.ListenIpAddress, out var startupListenAddress))
        {
            startupListenAddress = IPAddress.Any;
        }

        return new SoulseekClientOptions(
            enableListener: true,
            listenIPAddress: startupListenAddress,
            listenPort: optionsAtStartup.Soulseek.ListenPort,
            enableDistributedNetwork: !optionsAtStartup.Soulseek.DistributedNetwork.Disabled,
            acceptDistributedChildren: !optionsAtStartup.Soulseek.DistributedNetwork.DisableChildren,
            distributedChildLimit: optionsAtStartup.Soulseek.DistributedNetwork.ChildLimit,
            maximumUploadSpeed: optionsAtStartup.Global.Upload.SpeedLimit,
            maximumConcurrentUploads: optionsAtStartup.Global.Upload.Slots,
            maximumDownloadSpeed: optionsAtStartup.Global.Download.SpeedLimit,
            maximumConcurrentDownloads: optionsAtStartup.Global.Download.Slots,
            minimumDiagnosticLevel: optionsAtStartup.Soulseek.DiagnosticLevel.ToEnum<Soulseek.Diagnostics.DiagnosticLevel>(),
            maximumConcurrentSearches: 2,
            peerObfuscationOptions: SoulseekObfuscationSupport.BuildRuntimeOptions(optionsAtStartup.Soulseek),
            raiseEventsAsynchronously: true);
    }
}
