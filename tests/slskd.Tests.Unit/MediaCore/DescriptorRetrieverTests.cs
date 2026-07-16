// <copyright file="DescriptorRetrieverTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore;

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using slskd.MediaCore;
using slskd.Mesh.Dht;
using Xunit;

public class DescriptorRetrieverTests
{
    [Fact]
    public async Task QueryByDomainAsync_WithMbDomainAndNoType_DoesNotThrow()
    {
        var retriever = CreateRetriever();

        var result = await retriever.QueryByDomainAsync("mb");

        Assert.NotNull(result);
        Assert.Empty(result.Descriptors);
        Assert.Equal("audio", result.Domain);
        Assert.Null(result.Type);
    }

    [Fact]
    public async Task QueryByDomainAsync_WithWhitespaceType_TreatsTypeAsMissing()
    {
        var retriever = CreateRetriever();

        var result = await retriever.QueryByDomainAsync("audio", "   ");

        Assert.NotNull(result);
        Assert.Empty(result.Descriptors);
        Assert.Equal("audio", result.Domain);
        Assert.Null(result.Type);
    }

    [Fact]
    public async Task RetrieveBatchAsync_TrimsAndDeduplicatesContentIds()
    {
        var dht = new Mock<IMeshDhtClient>();
        var retriever = CreateRetriever(dht.Object);

        await retriever.RetrieveBatchAsync(new[] { " content:mb:recording:1 ", "content:mb:recording:1", "", "   " });

        dht.Verify(client => client.GetAsync<ContentDescriptor>("mesh:content:content:mb:recording:1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveAsync_TrimsContentIdBeforeLookup()
    {
        var dht = new Mock<IMeshDhtClient>();
        var retriever = CreateRetriever(dht.Object);

        await retriever.RetrieveAsync(" content:mb:recording:2 ");

        dht.Verify(client => client.GetAsync<ContentDescriptor>("mesh:content:content:mb:recording:2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveAsync_WhenDhtLookupThrows_ReturnsSanitizedErrorMessage()
    {
        var dht = new Mock<IMeshDhtClient>();
        dht.Setup(client => client.GetAsync<ContentDescriptor>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive DHT detail"));

        var retriever = CreateRetriever(dht.Object);

        var result = await retriever.RetrieveAsync("content:mb:recording:3");

        Assert.False(result.Found);
        Assert.Equal("Failed to retrieve descriptor from DHT", result.ErrorMessage);
        Assert.DoesNotContain("sensitive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyAsync_WhenValidatorThrows_ReturnsSanitizedValidationError()
    {
        var validator = new Mock<IDescriptorValidator>();
        validator.Setup(v => v.Validate(It.IsAny<ContentDescriptor>(), out It.Ref<string?>.IsAny!))
            .Throws(new InvalidOperationException("sensitive validation detail"));

        var retriever = new DescriptorRetriever(
            NullLogger<DescriptorRetriever>.Instance,
            Mock.Of<IMeshDhtClient>(),
            validator.Object,
            Options.Create(new MediaCoreOptions()));

        var result = await retriever.VerifyAsync(new ContentDescriptor { ContentId = "content:a" }, DateTimeOffset.UtcNow);

        Assert.False(result.IsValid);
        Assert.Equal("Descriptor verification failed", result.ValidationError);
        Assert.DoesNotContain("sensitive", result.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatsAsync_LargeMixedCacheAvoidsSnapshotsAndCleanupKeyBuffer()
    {
        const int entryCount = 10_000;
        var retriever = CreateRetriever();
        var cacheField = typeof(DescriptorRetriever).GetField(
            "_cache",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cache = (ConcurrentDictionary<string, CachedDescriptor>)cacheField.GetValue(retriever)!;
        var now = DateTimeOffset.UtcNow;
        var descriptor = new ContentDescriptor
        {
            ContentId = "content:audio:track:cached",
            Hashes = new List<ContentHash> { new("sha256", "abcd") },
            Codec = "flac",
        };

        for (var index = 0; index < entryCount; index++)
        {
            cache[$"content:audio:track:{index}"] = new CachedDescriptor(
                descriptor,
                now,
                index % 2 == 0 ? now.AddMinutes(5) : now.AddMinutes(-5));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stats = await retriever.GetStatsAsync();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(entryCount / 2, stats.ActiveCacheEntries);
        Assert.Equal(entryCount / 2, cache.Count);
        Assert.Equal(900_000, stats.CacheSizeBytes);
        Assert.True(
            allocatedBytes < 4 * 1024,
            $"Expected direct cache cleanup/statistics below 4 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    [Fact]
    public async Task ClearCacheAsync_LargeCacheAvoidsValuesSnapshot()
    {
        const int entryCount = 10_000;
        var retriever = CreateRetriever();
        var cacheField = typeof(DescriptorRetriever).GetField(
            "_cache",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cache = (ConcurrentDictionary<string, CachedDescriptor>)cacheField.GetValue(retriever)!;
        var now = DateTimeOffset.UtcNow;
        var descriptor = new ContentDescriptor
        {
            ContentId = "content:audio:track:cached",
            Hashes = new List<ContentHash> { new("sha256", "abcd") },
            Codec = "flac",
        };

        for (var index = 0; index < entryCount; index++)
        {
            cache[$"content:audio:track:{index}"] = new CachedDescriptor(
                descriptor,
                now,
                now.AddMinutes(5));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var result = await retriever.ClearCacheAsync();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.True(result.Success);
        Assert.Equal(entryCount, result.EntriesCleared);
        Assert.Equal(1_800_000, result.BytesFreed);
        Assert.Empty(cache);
        Assert.True(
            allocatedBytes < 8 * 1024,
            $"Expected direct cache-clear accounting below 8 KiB allocated, got {allocatedBytes:N0} bytes.");
    }

    private static DescriptorRetriever CreateRetriever(IMeshDhtClient? dht = null)
    {
        return new DescriptorRetriever(
            NullLogger<DescriptorRetriever>.Instance,
            dht ?? Mock.Of<IMeshDhtClient>(),
            Mock.Of<IDescriptorValidator>(),
            Options.Create(new MediaCoreOptions()));
    }
}
