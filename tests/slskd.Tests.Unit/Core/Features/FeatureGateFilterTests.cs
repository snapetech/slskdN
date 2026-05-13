// <copyright file="FeatureGateFilterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Core.Features;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using slskd.Core.Features;
using Xunit;

public class FeatureGateFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WhenFeatureDisabled_ReturnsNotFoundAndSkipsAction()
    {
        var gate = new Mock<IFeatureGate>();
        gate.Setup(instance => instance.Get(FeatureId.SongId)).Returns(new FeatureGateResult(
            FeatureId.SongId,
            FeatureStatus.Disabled,
            false,
            "Experimental feature is disabled."));
        var filter = new FeatureGateFilter(gate.Object, FeatureId.SongId);
        var context = CreateActionExecutingContext();
        var executed = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        });

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.False(executed);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenFeatureEnabled_RunsAction()
    {
        var gate = new Mock<IFeatureGate>();
        gate.Setup(instance => instance.Get(FeatureId.SongId)).Returns(new FeatureGateResult(
            FeatureId.SongId,
            FeatureStatus.Experimental,
            true,
            "Experimental feature is enabled."));
        var filter = new FeatureGateFilter(gate.Object, FeatureId.SongId);
        var context = CreateActionExecutingContext();
        var executed = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(CreateActionExecutedContext(context));
        });

        Assert.True(executed);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateActionExecutingContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: new object());
    }

    private static ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext context) => new(
        context,
        new List<IFilterMetadata>(),
        context.Controller);
}
