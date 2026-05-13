// <copyright file="FeatureStatus.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

/// <summary>
/// Runtime maturity status for a gated feature.
/// </summary>
public enum FeatureStatus
{
    /// <summary>The feature is enabled and treated as stable.</summary>
    Stable,

    /// <summary>The feature is enabled but experimental.</summary>
    Experimental,

    /// <summary>The feature exists in design or roadmap docs only.</summary>
    DesignOnly,

    /// <summary>The feature is known unavailable or broken.</summary>
    Broken,

    /// <summary>The feature has moved to slskr.</summary>
    MovedToSlskr,

    /// <summary>The feature is disabled by configuration.</summary>
    Disabled,
}
