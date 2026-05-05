// <copyright file="SoulseekCapabilityBridgeService.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Capabilities;

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using slskd.Mesh;
using slskd.Mesh.Overlay;
using Soulseek;

/// <summary>
/// Bridges runtime peer capability handshakes into slskdN capability state.
/// </summary>
public sealed class SoulseekCapabilityBridgeService : IHostedService
{
    private readonly ISoulseekClient soulseekClient;
    private readonly ICapabilityService capabilityService;
    private readonly IOptionsMonitor<MeshOptions> meshOptions;
    private readonly IOptions<OverlayOptions> overlayOptions;
    private readonly IKeyStore? keyStore;
    private readonly ILogger<SoulseekCapabilityBridgeService> logger;

    public SoulseekCapabilityBridgeService(
        ISoulseekClient soulseekClient,
        ICapabilityService capabilityService,
        IOptionsMonitor<MeshOptions> meshOptions,
        IOptions<OverlayOptions> overlayOptions,
        IServiceProvider serviceProvider,
        ILogger<SoulseekCapabilityBridgeService> logger)
    {
        this.soulseekClient = soulseekClient;
        this.capabilityService = capabilityService;
        this.meshOptions = meshOptions;
        this.overlayOptions = overlayOptions;
        keyStore = serviceProvider.GetService<IKeyStore>();
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        soulseekClient.PeerCapabilityReceived += HandlePeerCapabilityReceived;
        ConfigureLocalDescriptor();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        soulseekClient.PeerCapabilityReceived -= HandlePeerCapabilityReceived;
        return Task.CompletedTask;
    }

    private void ConfigureLocalDescriptor()
    {
        if (!meshOptions.CurrentValue.EnableSoulseekCapabilityHandshake)
        {
            logger.LogInformation("Soulseek runtime capability handshake disabled by mesh options");
            return;
        }

        try
        {
            var descriptor = BuildLocalDescriptor();
            soulseekClient.SetPeerCapabilityDescriptor(descriptor);
            logger.LogInformation(
                "Configured Soulseek runtime capability descriptor with {FeatureCount} features",
                descriptor.Features.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to configure Soulseek runtime capability descriptor");
        }
    }

    private PeerCapabilityDescriptor BuildLocalDescriptor()
    {
        var content = JsonSerializer.Deserialize<CapabilityFileContent>(
            capabilityService.GetCapabilityFileContent(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var features = content?.Features?.Length > 0
            ? content.Features
            : GetFeatures(content?.Capabilities ?? PeerCapabilityFlags.None);

        var descriptor = new PeerCapabilityDescriptor(
            peerId: keyStore?.Current.PublicKey is { Length: 32 } publicKey
                ? Ed25519PeerDescriptorSigner.DerivePeerId(publicKey)
                : null,
            features: features,
            overlayPort: overlayOptions.Value.QuicListenPort,
            maxPayloadLength: PeerCapabilityEnvelope.DefaultMaxPayloadLength);

        if (keyStore?.Current is { PrivateKey.Length: 32, PublicKey.Length: 32 } keyPair)
        {
            using var signer = new Ed25519PeerDescriptorSigner();
            return signer.Sign(descriptor, keyPair.PrivateKey, keyPair.PublicKey);
        }

        return descriptor;
    }

    private void HandlePeerCapabilityReceived(object? sender, PeerCapabilityReceivedEventArgs e)
    {
        var record = e.Record;
        var capabilities = new PeerCapabilities
        {
            Username = record.Username,
            Flags = ParseFeatureFlags(record.Descriptor.Features),
            ClientVersion = "slskdn/runtime-capability-v1",
            ProtocolVersion = 1,
            LastCapCheck = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
        };

        capabilityService.SetPeerCapabilities(record.Username, capabilities);
    }

    private static string[] GetFeatures(PeerCapabilityFlags flags)
    {
        var features = new List<string>();

        if (flags.HasFlag(PeerCapabilityFlags.SupportsDHT))
            features.Add("dht");
        if (flags.HasFlag(PeerCapabilityFlags.SupportsHashExchange))
            features.Add("hash_exchange");
        if (flags.HasFlag(PeerCapabilityFlags.SupportsPartialDownload))
            features.Add("partial_download");
        if (flags.HasFlag(PeerCapabilityFlags.SupportsMeshSync))
            features.Add("mesh_sync");
        if (flags.HasFlag(PeerCapabilityFlags.SupportsFlacHashDb))
            features.Add("flac_hash_db");
        if (flags.HasFlag(PeerCapabilityFlags.SupportsSwarm))
            features.Add("swarm_download");

        return features.ToArray();
    }

    private static PeerCapabilityFlags ParseFeatureFlags(IEnumerable<string> features)
    {
        var flags = PeerCapabilityFlags.None;

        foreach (var feature in features)
        {
            flags |= feature.Trim().ToLowerInvariant() switch
            {
                "dht" => PeerCapabilityFlags.SupportsDHT,
                "hash_exchange" or "hashx" => PeerCapabilityFlags.SupportsHashExchange,
                "partial_download" or "partial" => PeerCapabilityFlags.SupportsPartialDownload,
                "mesh_sync" or "mesh" => PeerCapabilityFlags.SupportsMeshSync,
                "flac_hash_db" or "flacdb" => PeerCapabilityFlags.SupportsFlacHashDb,
                "swarm_download" or "swarm" => PeerCapabilityFlags.SupportsSwarm,
                _ => PeerCapabilityFlags.None,
            };
        }

        return flags;
    }
}
