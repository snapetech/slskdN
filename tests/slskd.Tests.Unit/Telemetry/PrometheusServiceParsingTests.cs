// <copyright file="PrometheusServiceParsingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Telemetry;

using System.Threading.Tasks;
using slskd.Telemetry;
using Xunit;

public class PrometheusServiceParsingTests
{
    private sealed class StubPrometheusService : PrometheusService
    {
        private readonly string _text;

        public StubPrometheusService(string text) => _text = text;

        public override Task<string> GetMetricsAsString() => Task.FromResult(_text);
    }

    [Fact]
    public async Task GetMetricsAsObject_ParsesWellFormedMetric()
    {
        var text = "# HELP my_metric Some help text\n# TYPE my_metric gauge\nmy_metric 42\n";
        var service = new StubPrometheusService(text);

        var metrics = await service.GetMetricsAsObject();

        Assert.True(metrics.ContainsKey("my_metric"));
        Assert.Equal("gauge", metrics["my_metric"].Type);
        Assert.Equal("Some help text", metrics["my_metric"].Help);
    }

    [Fact]
    public async Task GetMetricsAsObject_HelpNotFollowedByType_DoesNotThrow()
    {
        // A HELP line followed directly by a short sample line (no TYPE) previously
        // threw ArgumentOutOfRangeException when Substring(7) ran past the line length.
        var text = "# HELP up The scrape target is up\nup 1\n";
        var service = new StubPrometheusService(text);

        var metrics = await service.GetMetricsAsObject();

        Assert.False(metrics.ContainsKey("up"));
    }

    [Theory]
    [InlineData("# HELP\n# TYPE\n")]
    [InlineData("# HELP \n# TYPE \n")]
    [InlineData("# HELP x\nfoo\n")]
    [InlineData("# HELP name help\n# TYP\n")]
    public async Task GetMetricsAsObject_MalformedInput_DoesNotThrow(string text)
    {
        var service = new StubPrometheusService(text);

        var metrics = await service.GetMetricsAsObject();

        Assert.Empty(metrics);
    }
}
