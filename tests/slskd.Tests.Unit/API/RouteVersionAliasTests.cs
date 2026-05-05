// <copyright file="RouteVersionAliasTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.API;

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using slskd.Jobs.API;
using slskd.LibraryHealth.API;
using Xunit;

public sealed class RouteVersionAliasTests
{
    [Theory]
    [InlineData(typeof(DiscographyJobsController), "api/v{version:apiVersion}/jobs/discography")]
    [InlineData(typeof(LabelCrateJobsController), "api/v{version:apiVersion}/jobs/label-crate")]
    [InlineData(typeof(LibraryHealthController), "api/v{version:apiVersion}/library/health")]
    public void ActiveLegacyWebControllers_ExposeVersionedAliases(Type controllerType, string expectedRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(route => route.Template)
            .ToArray();

        Assert.Contains(expectedRoute, routes);
    }
}
