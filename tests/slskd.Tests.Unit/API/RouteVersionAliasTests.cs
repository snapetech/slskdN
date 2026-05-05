// <copyright file="RouteVersionAliasTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.API;

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using slskd.API.Native;
using slskd.Jobs.API;
using ApiLibraryHealthController = slskd.LibraryHealth.API.LibraryHealthController;
using Xunit;
using AudioAnalyzerMigrationController = slskd.Audio.API.AnalyzerMigrationController;
using AudioCanonicalController = slskd.Audio.API.CanonicalController;
using AudioDedupeController = slskd.Audio.API.DedupeController;
using VirtualCanonicalController = slskd.API.VirtualSoulfind.CanonicalController;
using VirtualDisasterModeController = slskd.API.VirtualSoulfind.DisasterModeController;
using VirtualShadowIndexController = slskd.API.VirtualSoulfind.ShadowIndexController;

public sealed class RouteVersionAliasTests
{
    [Theory]
    [InlineData(typeof(CapabilitiesController), "api/v{version:apiVersion}/slskdn")]
    [InlineData(typeof(slskd.API.Native.LibraryHealthController), "api/v{version:apiVersion}/slskdn/library")]
    [InlineData(typeof(WarmCacheController), "api/v{version:apiVersion}/slskdn/warm-cache")]
    [InlineData(typeof(VirtualCanonicalController), "api/v{version:apiVersion}/virtualsoulfind/canonical")]
    [InlineData(typeof(VirtualDisasterModeController), "api/v{version:apiVersion}/virtualsoulfind/disaster-mode")]
    [InlineData(typeof(VirtualShadowIndexController), "api/v{version:apiVersion}/virtualsoulfind/shadow-index")]
    [InlineData(typeof(AudioAnalyzerMigrationController), "api/v{version:apiVersion}/audio/analyzers/migrate")]
    [InlineData(typeof(AudioCanonicalController), "api/v{version:apiVersion}/audio/canonical")]
    [InlineData(typeof(AudioDedupeController), "api/v{version:apiVersion}/audio/variants/dedupe")]
    [InlineData(typeof(DiscographyJobsController), "api/v{version:apiVersion}/jobs/discography")]
    [InlineData(typeof(LabelCrateJobsController), "api/v{version:apiVersion}/jobs/label-crate")]
    [InlineData(typeof(ApiLibraryHealthController), "api/v{version:apiVersion}/library/health")]
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
