// <copyright file="FeatureGate.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

using Microsoft.Extensions.Options;
using slskd.Mesh;

/// <summary>
/// Runtime feature gate backed by the existing options model.
/// </summary>
public sealed class FeatureGate : IFeatureGate
{
    private readonly IOptionsMonitor<global::slskd.Options> options;
    private readonly IOptionsMonitor<MeshOptions> meshOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureGate"/> class.
    /// </summary>
    /// <param name="options">Application options.</param>
    /// <param name="meshOptions">Mesh options.</param>
    public FeatureGate(
        IOptionsMonitor<global::slskd.Options> options,
        IOptionsMonitor<MeshOptions> meshOptions)
    {
        this.options = options;
        this.meshOptions = meshOptions;
    }

    /// <inheritdoc />
    public FeatureGateResult Get(FeatureId feature) => feature switch
    {
        FeatureId.SongId => Experimental(feature, options.CurrentValue.Feature.SongId),
        FeatureId.Mesh => Experimental(feature, options.CurrentValue.Feature.Mesh && meshOptions.CurrentValue.EnableOverlay),
        FeatureId.Dht => Experimental(
            feature,
            options.CurrentValue.Feature.Dht && options.CurrentValue.DhtRendezvous.Enabled),
        FeatureId.Pods => Experimental(feature, options.CurrentValue.Feature.Pods),
        FeatureId.SocialFederation => Experimental(feature, options.CurrentValue.Feature.SocialFederation),
        FeatureId.VirtualSoulfind => Experimental(feature, options.CurrentValue.Feature.VirtualSoulfind),
        FeatureId.MultiSourceDownloads => Experimental(feature, options.CurrentValue.Feature.MultiSourceDownloads),
        _ => new FeatureGateResult(feature, FeatureStatus.Disabled, false, "Feature is not recognized."),
    };

    /// <inheritdoc />
    public bool IsEnabled(FeatureId feature) => Get(feature).IsEnabled;

    private static FeatureGateResult Experimental(FeatureId feature, bool enabled) =>
        enabled
            ? new FeatureGateResult(feature, FeatureStatus.Experimental, true, "Experimental feature is enabled.")
            : new FeatureGateResult(feature, FeatureStatus.Disabled, false, "Experimental feature is disabled.");
}
