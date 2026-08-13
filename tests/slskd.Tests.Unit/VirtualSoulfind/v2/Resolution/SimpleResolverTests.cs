// <copyright file="SimpleResolverTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.VirtualSoulfind.v2.Resolution;

using System.IO;
using System.Reflection;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Tests.Unit;
using slskd.VirtualSoulfind.Core;
using slskd.VirtualSoulfind.v2.Backends;
using slskd.VirtualSoulfind.v2.Execution;
using slskd.VirtualSoulfind.v2.Planning;
using slskd.VirtualSoulfind.v2.Resolution;
using slskd.VirtualSoulfind.v2.Sources;
using Xunit;

[Collection("ProgramAppDirectory")]
public class SimpleResolverTests
{
    [Fact]
    public async Task ExecutePlanAsync_WhenBackendThrows_ReturnsSanitizedExecutionError()
    {
        var backend = new Mock<IContentBackend>();
        backend.SetupGet(b => b.Type).Returns(ContentBackendType.Http);
        backend
            .Setup(b => b.ValidateCandidateAsync(It.IsAny<SourceCandidate>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var resolver = new SimpleResolver(
            CreateOptionsMonitor(),
            new[] { backend.Object },
            CreateDownloadOptionsMonitor());

        var plan = new TrackAcquisitionPlan
        {
            TrackId = Guid.NewGuid().ToString(),
            Steps = new[]
            {
                new PlanStep
                {
                    Backend = ContentBackendType.Http,
                    Candidates = new[]
                    {
                        new SourceCandidate
                        {
                            Id = Guid.NewGuid().ToString(),
                            ItemId = ContentItemId.NewId(),
                            Backend = ContentBackendType.Http,
                            BackendRef = "https://allowed.com/file.flac",
                        },
                    },
                },
            },
        };

        var result = await resolver.ExecutePlanAsync(plan, CancellationToken.None);

        Assert.Equal(PlanExecutionStatus.Failed, result.Status);
        Assert.DoesNotContain("sensitive detail", result.ErrorMessage ?? string.Empty);
        Assert.Equal("All candidates in step failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecutePlanAsync_NormalizesRelativeDownloadDirectory_AndCreatesIt()
    {
        var originalAppDirectory = Program.AppDirectory;
        var tempAppDirectory = Path.Combine(Path.GetTempPath(), "slskdn-simple-resolver-tests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempAppDirectory);

        try
        {
            SetAppDirectory(tempAppDirectory);

            var backend = new RecordingFetchBackend();
            var resolver = new SimpleResolver(
                CreateOptionsMonitor(new ResolverOptions { DownloadDirectory = "resolver-downloads" }),
                new[] { backend },
                CreateDownloadOptionsMonitor());

            var plan = new TrackAcquisitionPlan
            {
                TrackId = Guid.NewGuid().ToString(),
                Steps = new[]
                {
                    new PlanStep
                    {
                        Backend = ContentBackendType.Http,
                        Candidates = new[]
                        {
                            new SourceCandidate
                            {
                                Id = Guid.NewGuid().ToString(),
                                ItemId = ContentItemId.NewId(),
                                Backend = ContentBackendType.Http,
                                BackendRef = "https://allowed.com/file.flac",
                            },
                        },
                    },
                },
            };

            var result = await resolver.ExecutePlanAsync(plan, CancellationToken.None);
            var expectedDirectory = Path.Combine(tempAppDirectory, "resolver-downloads");

            Assert.Equal(PlanExecutionStatus.Succeeded, result.Status);
            Assert.NotNull(result.FetchedFilePath);
            Assert.StartsWith(expectedDirectory, result.FetchedFilePath!, StringComparison.Ordinal);
            Assert.True(Directory.Exists(expectedDirectory));
            Assert.True(backend.WasFetchInvoked);
        }
        finally
        {
            SetAppDirectory(originalAppDirectory);

            if (Directory.Exists(tempAppDirectory))
            {
                Directory.Delete(tempAppDirectory, true);
            }
        }
    }

    [Fact]
    public async Task ExecutePlanAsync_BlockedFilenameBearingCandidate_DoesNotValidateOrFetch()
    {
        var backend = new Mock<IContentBackend>();
        backend.SetupGet(value => value.Type).Returns(ContentBackendType.Http);

        var resolver = new SimpleResolver(
            CreateOptionsMonitor(),
            new[] { backend.Object },
            CreateDownloadOptionsMonitor(new slskd.Options.FiltersOptions.DownloadFilterOptions
            {
                Exclude = new[] { "instrumental" },
            }));

        var result = await resolver.ExecutePlanAsync(new TrackAcquisitionPlan
        {
            TrackId = Guid.NewGuid().ToString(),
            Steps = new[]
            {
                new PlanStep
                {
                    Backend = ContentBackendType.Http,
                    Candidates = new[]
                    {
                        new SourceCandidate
                        {
                            Id = Guid.NewGuid().ToString(),
                            ItemId = ContentItemId.NewId(),
                            Backend = ContentBackendType.Http,
                            BackendRef = "https://allowed.example/music/instrumental.flac",
                        },
                    },
                },
            },
        });

        Assert.Equal(PlanExecutionStatus.Failed, result.Status);
        backend.Verify(value => value.ValidateCandidateAsync(It.IsAny<SourceCandidate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IOptionsMonitor<ResolverOptions> CreateOptionsMonitor(ResolverOptions? value = null)
    {
        return Mock.Of<IOptionsMonitor<ResolverOptions>>(options =>
            options.CurrentValue == (value ?? new ResolverOptions()));
    }

    private static IOptionsMonitor<slskd.Options> CreateDownloadOptionsMonitor(
        slskd.Options.FiltersOptions.DownloadFilterOptions? filterOptions = null)
    {
        return Mock.Of<IOptionsMonitor<slskd.Options>>(options =>
            options.CurrentValue == new slskd.Options
            {
                Filters = new slskd.Options.FiltersOptions
                {
                    Download = filterOptions ?? new slskd.Options.FiltersOptions.DownloadFilterOptions(),
                },
            });
    }

    private static void SetAppDirectory(string? value)
    {
        var property = typeof(Program).GetProperty(nameof(Program.AppDirectory), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(property);
        var setter = property!.GetSetMethod(nonPublic: true);
        Assert.NotNull(setter);
        setter!.Invoke(null, new object[] { value ?? string.Empty });
    }

    private sealed class RecordingFetchBackend : IContentBackend, IContentFetchBackend
    {
        public ContentBackendType Type => ContentBackendType.Http;
        public ContentDomain? SupportedDomain => null;

        public bool WasFetchInvoked { get; private set; }

        public Task<IReadOnlyList<SourceCandidate>> FindCandidatesAsync(ContentItemId itemId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SourceCandidate>>(Array.Empty<SourceCandidate>());
        }

        public Task<SourceCandidateValidationResult> ValidateCandidateAsync(SourceCandidate candidate, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SourceCandidateValidationResult.Valid(candidate.TrustScore, candidate.ExpectedQuality));
        }

        public async Task FetchToStreamAsync(SourceCandidate candidate, Stream destination, CancellationToken cancellationToken = default)
        {
            WasFetchInvoked = true;
            await destination.WriteAsync(new byte[] { 1, 2, 3 }, cancellationToken);
        }
    }
}
