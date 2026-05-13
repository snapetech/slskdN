// <copyright file="FeatureGateResult.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

/// <summary>
/// Result of evaluating a feature gate.
/// </summary>
/// <param name="Feature">The evaluated feature.</param>
/// <param name="Status">The runtime status.</param>
/// <param name="IsEnabled">Whether callers may execute the feature.</param>
/// <param name="Message">Operator-facing status detail.</param>
public sealed record FeatureGateResult(
    FeatureId Feature,
    FeatureStatus Status,
    bool IsEnabled,
    string Message);
