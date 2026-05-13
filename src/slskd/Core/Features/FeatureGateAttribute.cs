// <copyright file="FeatureGateAttribute.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Applies runtime feature-gate checks to an MVC controller or action.
/// </summary>
public sealed class FeatureGateAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureGateAttribute"/> class.
    /// </summary>
    /// <param name="feature">Feature to gate.</param>
    public FeatureGateAttribute(FeatureId feature)
        : base(typeof(FeatureGateFilter))
    {
        Arguments = new object[] { feature };
    }
}
