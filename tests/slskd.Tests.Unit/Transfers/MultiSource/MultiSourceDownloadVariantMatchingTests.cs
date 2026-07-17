// <copyright file="MultiSourceDownloadVariantMatchingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.MultiSource;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using slskd.MediaCore;
using slskd.Transfers.MultiSource;
using Xunit;

public sealed class MultiSourceDownloadVariantMatchingTests
{
    private static readonly MethodInfo IsContentVariantMatchMethod = typeof(MultiSourceDownloadService)
        .GetMethod("IsContentVariantMatch", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CalculateFilenameSimilarityMethod = typeof(MultiSourceDownloadService)
        .GetMethod("CalculateFilenameSimilarity", BindingFlags.Static | BindingFlags.NonPublic)!;

    [Theory]
    [InlineData("aabb", "abcc", 0.5)]
    [InlineData("A", "a", 0.0)]
    [InlineData("ééx", "éyz", 1.0 / 3.0)]
    [InlineData("😀a", "😀b", 2.0 / 3.0)]
    [InlineData("", "", 0.0)]
    public void CalculateFilenameSimilarity_PreservesDistinctCaseSensitiveCharacterSemantics(
        string first,
        string second,
        double expected)
    {
        var result = Assert.IsType<double>(CalculateFilenameSimilarityMethod.Invoke(null, new object[] { first, second }));

        Assert.Equal(expected, result, 10);
    }

    [Fact]
    public void IsContentVariantMatch_UsesBasenameAndSizeGate()
    {
        var matching = CreateVariants(1, "ABCDEF.flac", size: 100);
        var wrongSize = CreateVariants(1, "ABCDEF.flac", size: 200);

        Assert.True(InvokeMatch("/music/ABCDEF.flac", 100, matching));
        Assert.False(InvokeMatch("/music/ABCDEF.flac", 100, wrongSize));
    }

    [Fact]
    public void IsContentVariantMatch_FiftyVariantMissBoundsAllocation()
    {
        var variants = CreateVariants(50, "ZZZZZYYYYY.mp3", size: 123);

        for (var iteration = 0; iteration < 100; iteration++)
        {
            InvokeMatch("/music/AAAAABBBBB.flac", 123, variants);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++)
        {
            InvokeMatch("/music/AAAAABBBBB.flac", 123, variants);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.True(allocatedBytes < 30_000, $"Allocated {allocatedBytes:N0} bytes.");
    }

    private static bool InvokeMatch(string filename, long size, ContentVariantsResult variants)
    {
        return Assert.IsType<bool>(IsContentVariantMatchMethod.Invoke(null, new object[] { filename, size, variants }));
    }

    private static ContentVariantsResult CreateVariants(int count, string filename, long size)
    {
        var descriptor = new ContentDescriptor { ContentId = "content:audio:track:test", SizeBytes = size };
        return new ContentVariantsResult(
            OriginalFilename: "target",
            FileSize: size,
            Variants: Enumerable.Range(0, count)
                .Select(index => new ContentVariant(
                    ContentId: $"content:audio:track:{index}",
                    Filename: filename,
                    SimilarityScore: 0,
                    Descriptor: descriptor,
                    IsCanonical: false))
                .ToList(),
            SimilarityScores: new Dictionary<string, double>());
    }
}
