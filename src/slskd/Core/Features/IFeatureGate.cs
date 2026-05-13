// <copyright file="IFeatureGate.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

/// <summary>
/// Evaluates feature availability for experimental or moved surfaces.
/// </summary>
public interface IFeatureGate
{
    /// <summary>
    /// Gets the gate status for a feature.
    /// </summary>
    /// <param name="feature">Feature to evaluate.</param>
    /// <returns>The feature gate result.</returns>
    FeatureGateResult Get(FeatureId feature);

    /// <summary>
    /// Returns whether a feature is currently enabled.
    /// </summary>
    /// <param name="feature">Feature to evaluate.</param>
    /// <returns>True when the feature can execute.</returns>
    bool IsEnabled(FeatureId feature);
}
