// <copyright file="FeatureId.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

/// <summary>
/// Identifies feature surfaces that can be gated at runtime.
/// </summary>
public enum FeatureId
{
    /// <summary>SongID analysis and evidence APIs.</summary>
    SongId,

    /// <summary>Mesh overlay, hash gossip, and mesh diagnostics.</summary>
    Mesh,

    /// <summary>DHT rendezvous and overlay peer discovery.</summary>
    Dht,

    /// <summary>Pod community features.</summary>
    Pods,

    /// <summary>Social federation and ActivityPub features.</summary>
    SocialFederation,

    /// <summary>VirtualSoulfind shadow index and disaster-mode surfaces.</summary>
    VirtualSoulfind,

    /// <summary>Multi-source download, swarm, and rescue surfaces.</summary>
    MultiSourceDownloads,
}
