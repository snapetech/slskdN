// <copyright file="FeatureGateFilter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Core.Features;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// MVC filter that blocks explicitly disabled feature surfaces.
/// </summary>
public sealed class FeatureGateFilter : IAsyncActionFilter
{
    private readonly IFeatureGate featureGate;
    private readonly FeatureId feature;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureGateFilter"/> class.
    /// </summary>
    /// <param name="featureGate">Feature gate service.</param>
    /// <param name="feature">Feature to evaluate.</param>
    public FeatureGateFilter(IFeatureGate featureGate, FeatureId feature)
    {
        this.featureGate = featureGate;
        this.feature = feature;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var gate = featureGate.Get(feature);
        if (!gate.IsEnabled)
        {
            context.Result = new ObjectResult(new
            {
                feature = gate.Feature.ToString(),
                status = gate.Status.ToString(),
                error = gate.Message,
            })
            {
                StatusCode = StatusCodes.Status404NotFound,
            };
            return;
        }

        await next().ConfigureAwait(false);
    }
}
