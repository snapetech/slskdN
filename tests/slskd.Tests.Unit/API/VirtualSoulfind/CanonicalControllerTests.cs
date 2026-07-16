// <copyright file="CanonicalControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.API.VirtualSoulfind;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.API.VirtualSoulfind;
using slskd.VirtualSoulfind.ShadowIndex;
using Xunit;

public class CanonicalControllerTests
{
    [Fact]
    public async Task GetCanonical_WithBlankMbid_ReturnsBadRequest()
    {
        var controller = new CanonicalController(
            NullLogger<CanonicalController>.Instance,
            Mock.Of<IShadowIndexQuery>());

        var result = await controller.GetCanonical("   ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCanonical_TrimsMbidBeforeDispatch()
    {
        var query = new Mock<IShadowIndexQuery>();
        query
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShadowIndexQueryResult());

        var controller = new CanonicalController(
            NullLogger<CanonicalController>.Instance,
            query.Object);

        await controller.GetCanonical(" mbid-1 ", CancellationToken.None);

        query.Verify(service => service.QueryAsync("mbid-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCanonical_WhenQueryThrows_DoesNotLeakMbid()
    {
        var query = new Mock<IShadowIndexQuery>();
        query
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = new CanonicalController(
            NullLogger<CanonicalController>.Instance,
            query.Object);

        var result = await controller.GetCanonical("mbid-1", CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Failed to select canonical variant", error.Value?.ToString() ?? string.Empty);
        Assert.DoesNotContain("mbid-1", error.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task GetCanonical_WhenNoVariantsFound_DoesNotEchoMbid()
    {
        var query = new Mock<IShadowIndexQuery>();
        query
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShadowIndexQueryResult());

        var controller = new CanonicalController(
            NullLogger<CanonicalController>.Instance,
            query.Object);

        var result = await controller.GetCanonical("mbid-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.DoesNotContain("mbid-1", ok.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("available_variants", ok.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public void SelectCanonicalVariant_UsesStableCodecThenQualityMaximum()
    {
        var expected = new VariantHint { Codec = "flac", QualityScore = 0.9 };
        var tiedLater = new VariantHint { Codec = "FLAC", QualityScore = 0.9 };
        var variants = new[]
        {
            new VariantHint { Codec = "unknown", QualityScore = 100 },
            new VariantHint { Codec = "MP3", QualityScore = 1 },
            new VariantHint { Codec = "ALAC", QualityScore = 1 },
            new VariantHint { Codec = "FLAC", QualityScore = 0.8 },
            expected,
            tiedLater,
        };

        var result = CanonicalController.SelectCanonicalVariant(variants);

        Assert.Same(expected, result);
    }

    [Fact]
    public void SelectCanonicalVariant_LargeInputUsesBoundedWorkingMemory()
    {
        var variants = Enumerable.Range(0, 10_000)
            .Select(index => new VariantHint { Codec = "FLAC", QualityScore = index })
            .ToArray();
        _ = CanonicalController.SelectCanonicalVariant(variants.AsSpan(0, 1).ToArray());

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = CanonicalController.SelectCanonicalVariant(variants);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Same(variants[^1], result);
        Assert.True(
            allocatedBytes < 4 * 1024,
            $"Expected single-pass allocation below 4 KiB, got {allocatedBytes:N0} bytes.");
    }
}
