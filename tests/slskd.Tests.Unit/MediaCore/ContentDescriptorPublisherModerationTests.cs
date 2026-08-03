// <copyright file="ContentDescriptorPublisherModerationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.MediaCore
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using slskd.MediaCore;
    using Xunit;

    /// <summary>
    ///     Tests for T-MCP03: Moderation filtering in ContentDescriptorPublisher.
    /// </summary>
    [Collection(AllocationTestCollection.Name)]
    public class ContentDescriptorPublisherModerationTests
    {
        private readonly Mock<IDescriptorPublisher> _basePublisherMock = new();
        private readonly Mock<ILogger<ContentDescriptorPublisher>> _loggerMock = new();
        private readonly Mock<IContentIdRegistry> _registryMock = new();
        private readonly IOptions<MediaCoreOptions> _options = Options.Create(new MediaCoreOptions { MaxTtlMinutes = 60 });

        private ContentDescriptorPublisher CreatePublisher()
        {
            return new ContentDescriptorPublisher(
                _loggerMock.Object,
                _basePublisherMock.Object,
                _registryMock.Object,
                _options);
        }

        [Fact]
        public async Task PublishAsync_WithAdvertisableDescriptor_PublishesSuccessfully()
        {
            // Arrange
            var publisher = CreatePublisher();
            var descriptor = new ContentDescriptor
            {
                ContentId = "test-content-id",
                IsAdvertisable = true,
                SizeBytes = 1024,
                Signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };

            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await publisher.PublishAsync(descriptor);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("test-content-id", result.ContentId);
            _basePublisherMock.Verify(x => x.PublishAsync(descriptor, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishAsync_WithNonAdvertisableDescriptor_FailsWithError()
        {
            // Arrange
            var publisher = CreatePublisher();
            var descriptor = new ContentDescriptor
            {
                ContentId = "test-content-id",
                IsAdvertisable = false,
                SizeBytes = 1024
            };

            // Act
            var result = await publisher.PublishAsync(descriptor);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("test-content-id", result.ContentId);
            Assert.Contains("not advertisable", result.ErrorMessage ?? string.Empty);
            _basePublisherMock.Verify(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PublishAsync_WithAdvertisableDescriptor_UpdatesTracking()
        {
            // Arrange
            var publisher = CreatePublisher();
            var descriptor = new ContentDescriptor
            {
                ContentId = "test-content-id",
                IsAdvertisable = true,
                SizeBytes = 1024,
                Signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };

            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await publisher.PublishAsync(descriptor);

            // Assert
            _basePublisherMock.Verify(x => x.PublishAsync(descriptor, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishAsync_BackendPublishFails_ReturnsFailure()
        {
            // Arrange
            var publisher = CreatePublisher();
            var descriptor = new ContentDescriptor
            {
                ContentId = "test-content-id",
                IsAdvertisable = true,
                SizeBytes = 1024,
                Signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };

            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await publisher.PublishAsync(descriptor);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("test-content-id", result.ContentId);
        }

        [Fact]
        public async Task PublishAsync_WhenBasePublisherThrows_ReturnsSanitizedErrorMessage()
        {
            var publisher = CreatePublisher();
            var descriptor = new ContentDescriptor
            {
                ContentId = "test-content-id",
                IsAdvertisable = true,
                SizeBytes = 1024,
                Signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };

            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("sensitive publish detail"));

            var result = await publisher.PublishAsync(descriptor);

            Assert.False(result.Success);
            Assert.Equal("Failed to publish descriptor", result.ErrorMessage);
            Assert.DoesNotContain("sensitive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PublishBatchAsync_LargeBatchAvoidsPerItemWaitingTasks()
        {
            const int descriptorCount = 10_000;
            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Yield();
                    return true;
                });
            var signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var descriptors = Enumerable.Range(0, descriptorCount)
                .Select(index => new ContentDescriptor
                {
                    ContentId = $"content:audio:track:{index}",
                    Signature = signature,
                })
                .ToArray();
            _ = await CreatePublisher().PublishBatchAsync(descriptors.AsSpan(0, 1).ToArray());

            var publisher = CreatePublisher();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var result = await publisher.PublishBatchAsync(descriptors);
            var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

            Assert.Equal(descriptorCount, result.TotalRequested);
            Assert.Equal(descriptorCount, result.SuccessfullyPublished);
            Assert.Equal(0, result.FailedToPublish);
            Assert.Equal(0, result.Skipped);
            Assert.Equal(descriptorCount, result.Results.Count);
            Assert.True(
                allocatedBytes < 30 * 1024 * 1024,
                $"Expected fixed-worker batch publishing below 30 MiB allocated, got {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public async Task PublishBatchAsync_UsesExactlyBoundedWorkerConcurrency()
        {
            var active = 0;
            var maximumActive = 0;
            _basePublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<ContentDescriptor>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    var currentActive = Interlocked.Increment(ref active);
                    var observedMaximum = Volatile.Read(ref maximumActive);
                    while (currentActive > observedMaximum)
                    {
                        var priorMaximum = Interlocked.CompareExchange(
                            ref maximumActive,
                            currentActive,
                            observedMaximum);
                        if (priorMaximum == observedMaximum)
                        {
                            break;
                        }

                        observedMaximum = priorMaximum;
                    }

                    await Task.Delay(5);
                    Interlocked.Decrement(ref active);
                    return true;
                });
            var publisher = CreatePublisher();
            var signature = new DescriptorSignature("pk", "ABCD", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var descriptors = Enumerable.Range(0, 100)
                .Select(index => new ContentDescriptor
                {
                    ContentId = $"content:audio:track:{index}",
                    Signature = signature,
                })
                .ToArray();

            var result = await publisher.PublishBatchAsync(descriptors);

            Assert.Equal(100, result.SuccessfullyPublished);
            Assert.Equal(5, maximumActive);
            Assert.Equal(0, active);
        }

        [Fact]
        public void GenerateVersion_LargeContentIdAvoidsIntermediatePayloads()
        {
            var descriptor = new ContentDescriptor
            {
                ContentId = "content:audio:track:" + new string('x', 100_000),
                Codec = null,
                SizeBytes = 123456789,
            };
            _ = ContentDescriptorPublisher.GenerateVersion(new ContentDescriptor { ContentId = "warm" });

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var version = ContentDescriptorPublisher.GenerateVersion(descriptor);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.EndsWith("-7b8f1451", version, StringComparison.Ordinal);
            Assert.True(
                allocatedBytes < 2 * 1024,
                $"Expected bounded version generation below 2 KiB allocated, got {allocatedBytes:N0} bytes.");
        }

        [Fact]
        public void GenerateVersion_PreservesLegacyPayloadAcrossNullCultureAndChunkBoundary()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
                var descriptors = new[]
                {
                    new ContentDescriptor { ContentId = "content:audio:track:nulls" },
                    new ContentDescriptor
                    {
                        ContentId = "content:audio:track:culture",
                        Codec = "flac",
                        SizeBytes = -123456789,
                    },
                    new ContentDescriptor
                    {
                        ContentId = "content:audio:track:" + new string('x', 1003) + "🎵tail",
                        Codec = "opus",
                        SizeBytes = long.MaxValue,
                    },
                };

                foreach (var descriptor in descriptors)
                {
                    var version = ContentDescriptorPublisher.GenerateVersion(descriptor);
                    Assert.EndsWith(
                        $"-{ComputeLegacyVersionHash(descriptor)}",
                        version,
                        StringComparison.Ordinal);
                }

                var customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                customCulture.NumberFormat.NegativeSign = new string('~', 40);
                CultureInfo.CurrentCulture = customCulture;
                var expandedSignDescriptor = new ContentDescriptor
                {
                    ContentId = "content:audio:track:expanded-sign",
                    Codec = "flac",
                    SizeBytes = -1,
                };
                var expandedSignVersion = ContentDescriptorPublisher.GenerateVersion(expandedSignDescriptor);
                Assert.EndsWith(
                    $"-{ComputeLegacyVersionHash(expandedSignDescriptor)}",
                    expandedSignVersion,
                    StringComparison.Ordinal);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Fact]
        public void GenerateVersion_IncludesPopulatedBitrateWithoutChangingNullLegacyShape()
        {
            var legacy = ContentDescriptorPublisher.GenerateVersion(new ContentDescriptor
            {
                ContentId = "content:audio:track:bitrate",
                Codec = "mp3",
                SizeBytes = 123,
            });
            var withBitrate = ContentDescriptorPublisher.GenerateVersion(new ContentDescriptor
            {
                ContentId = "content:audio:track:bitrate",
                Codec = "mp3",
                SizeBytes = 123,
                BitrateKbps = 320,
            });

            Assert.NotEqual(
                legacy[(legacy.LastIndexOf('-') + 1)..],
                withBitrate[(withBitrate.LastIndexOf('-') + 1)..]);
        }

        private static string ComputeLegacyVersionHash(ContentDescriptor descriptor)
        {
            var content = $"{descriptor.ContentId}:{descriptor.Codec}:{descriptor.SizeBytes}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 8).ToLower();
        }

        [Fact]
        public async Task UnpublishAsync_WhenTrackingThrows_ReturnsSanitizedErrorMessage()
        {
            var publisher = CreatePublisher();
            var publishedDescriptorsField = typeof(ContentDescriptorPublisher).GetField("_publishedDescriptors", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(publishedDescriptorsField);
            publishedDescriptorsField!.SetValue(publisher, null);

            var result = await publisher.UnpublishAsync("test-content-id");

            Assert.False(result.Success);
            Assert.Equal("Failed to unpublish descriptor", result.ErrorMessage);
            Assert.DoesNotContain("Object reference", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}
