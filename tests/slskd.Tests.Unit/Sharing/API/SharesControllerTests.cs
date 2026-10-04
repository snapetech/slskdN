// <copyright file="SharesControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Sharing.API;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using slskd;
using slskd.Shares;
using slskd.Sharing;
using slskd.Sharing.API;
using slskd.Transfers.Downloads;
using Xunit;
using TestOptionsMonitor = slskd.Tests.Unit.TestOptionsMonitor<slskd.Options>;

public class SharesControllerTests
{
    private readonly Mock<ISharingService> _sharingMock = new();
    private readonly Mock<IShareTokenService> _tokensMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private IOptionsMonitor<slskd.Options> _options = new TestOptionsMonitor(new slskd.Options
    {
        Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
        Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" }
    });

    public SharesControllerTests()
    {
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));
    }

    private SharesController CreateController(string? identity = "alice", Mock<ILogger<SharesController>>? logger = null)
    {
        var loggerMock = logger ?? new Mock<ILogger<SharesController>>();
        var c = new SharesController(_sharingMock.Object, _tokensMock.Object, loggerMock.Object, _options, _httpClientFactoryMock.Object, soulseekClient: null, shareService: null, downloadService: null);
        c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        c.HttpContext.User = identity is null
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, identity) }, "Test"));
        return c;
    }

    [Fact]
    public void TryDeletePartialBackfillFile_WhenCleanupFails_LogsAndPreservesUnexpectedFailures()
    {
        var downloadsDirectory = Path.Combine(Path.GetTempPath(), "slskdn-backfill-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(downloadsDirectory);
        var logger = new Mock<ILogger<SharesController>>();
        var controller = CreateController(logger: logger);
        var method = typeof(SharesController).GetMethod("TryDeletePartialBackfillFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        try
        {
            method!.Invoke(controller, new object[] { Path.Combine(downloadsDirectory, "outside.flac"), downloadsDirectory });

            logger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Failed to remove staged download", StringComparison.Ordinal)),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

            var unexpected = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(controller, new object[] { "", null! }));
            Assert.IsType<ArgumentNullException>(unexpected.InnerException);
        }
        finally
        {
            Directory.Delete(downloadsDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAll_WithoutAuthenticatedWebIdentity_ReturnsForbidden()
    {
        var result = await CreateController(identity: null).GetAll(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _sharingMock.Verify(service => service.GetShareGrantsAccessibleByUserAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_FeatureDisabled_ReturnsNotFound()
    {
        _options = new TestOptionsMonitor(new slskd.Options
        {
            Feature = new slskd.Options.FeatureOptions { CollectionsSharing = false },
            Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" }
        });
        var c = CreateController();

        var r = await c.GetAll(CancellationToken.None);

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task GetAll_Success_ReturnsList()
    {
        var c = CreateController();
        _sharingMock.Setup(x => x.GetShareGrantsAccessibleByUserAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShareGrant> { new() { Id = Guid.NewGuid() } });

        var r = await c.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        var list = Assert.IsAssignableFrom<List<ShareGrant>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task Get_UsesExactAccessibleGrantLookup()
    {
        var grantId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, AudienceType = AudienceTypes.User, AudienceId = "alice" };
        _sharingMock
            .Setup(service => service.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);

        var result = await CreateController().Get(grantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(grant, ok.Value);
        _sharingMock.Verify(
            service => service.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()),
            Times.Once);
        _sharingMock.Verify(
            service => service.GetShareGrantsAccessibleByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _sharingMock.Verify(
            service => service.GetShareGrantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_EmptyCollectionId_ReturnsBadRequest()
    {
        var c = CreateController();

        var r = await c.Create(new CreateShareGrantRequest { CollectionId = default }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(r);
        var problemDetails = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(badRequest.Value);
        Assert.Equal("CollectionId is required.", problemDetails.Detail);
    }

    [Fact]
    public async Task Create_NullRequest_ReturnsBadRequest()
    {
        var c = CreateController();

        var r = await c.Create(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(r);
    }

    [Fact]
    public async Task Create_CollectionNotFound_ReturnsNotFound()
    {
        var c = CreateController();
        var collectionId = Guid.NewGuid();
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        var r = await c.Create(new CreateShareGrantRequest
        {
            CollectionId = collectionId,
            AudienceType = "User",
            AudienceId = "bob"
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task Create_Success_ReturnsCreated()
    {
        var c = CreateController();
        var collectionId = Guid.NewGuid();
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "alice" });
        var created = new ShareGrant { Id = Guid.NewGuid(), CollectionId = collectionId };
        _sharingMock.Setup(x => x.CreateShareGrantAsync(It.IsAny<ShareGrant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var r = await c.Create(new CreateShareGrantRequest
        {
            CollectionId = collectionId,
            AudienceType = "User",
            AudienceId = "bob"
        }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(r);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task CreateToken_GrantNotFound_ReturnsNotFound()
    {
        var c = CreateController();
        _sharingMock.Setup(x => x.GetShareGrantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShareGrant?)null);

        var r = await c.CreateToken(Guid.NewGuid(), new CreateTokenRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task CreateToken_Success_ReturnsToken()
    {
        var c = CreateController();
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = grantId, CollectionId = collectionId };
        _sharingMock.Setup(x => x.GetShareGrantAsync(grantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "alice" });
        _sharingMock.Setup(x => x.CreateTokenAsync(grantId, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("token123");

        var r = await c.CreateToken(grantId, new CreateTokenRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        var resp = Assert.IsType<TokenResponse>(ok.Value);
        Assert.Equal("token123", resp.Token);
    }

    [Fact]
    public async Task GetManifest_TokenAuth_InvalidToken_ReturnsUnauthorized()
    {
        var c = CreateController();
        c.HttpContext.Request.QueryString = new QueryString("?token=bad");
        _tokensMock.Setup(x => x.ValidateAsync("bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShareTokenClaims?)null);

        var r = await c.GetManifest(Guid.NewGuid(), "bad", CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task GetManifest_TokenAuth_Success()
    {
        var c = CreateController();
        var grantId = Guid.NewGuid();
        var claims = new ShareTokenClaims(grantId.ToString(), Guid.NewGuid().ToString(), null, true, true, 1, DateTimeOffset.UtcNow.AddHours(1));
        _tokensMock.Setup(x => x.ValidateAsync("token123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(claims);
        var manifest = new ShareManifestDto { CollectionId = claims.CollectionId, Title = "Test" };
        _sharingMock.Setup(x => x.GetManifestAsync(grantId, "token123", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var r = await c.GetManifest(grantId, "token123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        Assert.Equal(manifest, ok.Value);
    }

    [Fact]
    public async Task GetManifest_ShareTokenViaHeader_Success()
    {
        // The web UI passes the share token in the X-Share-Token header (not the query) so it never
        // lands in the URL/logs. No ?token= query value is supplied here.
        var c = CreateController(identity: null);
        var grantId = Guid.NewGuid();
        c.HttpContext.Request.Headers["X-Share-Token"] = "token123";
        var claims = new ShareTokenClaims(grantId.ToString(), Guid.NewGuid().ToString(), null, true, true, 1, DateTimeOffset.UtcNow.AddHours(1));
        _tokensMock.Setup(x => x.ValidateAsync("token123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(claims);
        var manifest = new ShareManifestDto { CollectionId = claims.CollectionId, Title = "Test" };
        _sharingMock.Setup(x => x.GetManifestAsync(grantId, "token123", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var r = await c.GetManifest(grantId, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        Assert.Equal(manifest, ok.Value);
        _tokensMock.Verify(x => x.ValidateAsync("token123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetManifest_JwtBearerHeader_NotTreatedAsShareToken_ReturnsUnauthorized()
    {
        // A plain Authorization: Bearer <jwt> (no share: prefix) and no X-Share-Token must NOT be
        // interpreted as a share token; an unauthenticated caller stays unauthorized.
        var c = CreateController(identity: null);
        c.HttpContext.User = new ClaimsPrincipal();
        c.HttpContext.Request.Headers.Authorization = "Bearer some.jwt.value";

        var r = await c.GetManifest(Guid.NewGuid(), null, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(r);
        _tokensMock.Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetManifest_NormalAuth_NotAuthenticated_ReturnsUnauthorized()
    {
        var c = CreateController();
        c.HttpContext.User = new ClaimsPrincipal();

        var r = await c.GetManifest(Guid.NewGuid(), null, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task Create_WithAudiencePeerId_Success()
    {
        var c = CreateController();
        var collectionId = Guid.NewGuid();
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "alice" });
        var created = new ShareGrant { Id = Guid.NewGuid(), CollectionId = collectionId, AudiencePeerId = "peer123" };
        _sharingMock.Setup(x => x.CreateShareGrantAsync(It.Is<ShareGrant>(g => g.AudiencePeerId == "peer123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var r = await c.Create(new CreateShareGrantRequest
        {
            CollectionId = collectionId,
            AudienceType = "User",
            AudienceId = "bob",
            AudiencePeerId = "peer123"
        }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(r);
        var grant = Assert.IsType<ShareGrant>(createdResult.Value);
        Assert.Equal("peer123", grant.AudiencePeerId);
    }

    [Fact]
    public async Task Create_TrimsAudienceFieldsBeforePersisting()
    {
        var c = CreateController();
        var collectionId = Guid.NewGuid();
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "alice" });
        _sharingMock.Setup(x => x.CreateShareGrantAsync(It.IsAny<ShareGrant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareGrant { Id = Guid.NewGuid(), CollectionId = collectionId });

        var r = await c.Create(new CreateShareGrantRequest
        {
            CollectionId = collectionId,
            AudienceType = " User ",
            AudienceId = " bob ",
            AudiencePeerId = " peer123 "
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(r);
        _sharingMock.Verify(x => x.CreateShareGrantAsync(
            It.Is<ShareGrant>(g =>
                g.AudienceType == "User" &&
                g.AudienceId == "bob" &&
                g.AudiencePeerId == "peer123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonPositiveMaxConcurrentStreams_ClampsToOne()
    {
        var c = CreateController();
        var shareId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var grant = new ShareGrant { Id = shareId, CollectionId = collectionId, MaxConcurrentStreams = 4 };

        _sharingMock.Setup(x => x.GetShareGrantAsync(shareId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grant);
        _sharingMock.Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "alice" });
        _sharingMock.Setup(x => x.UpdateShareGrantAsync(It.IsAny<ShareGrant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await c.Update(shareId, new UpdateShareGrantRequest { MaxConcurrentStreams = 0 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<ShareGrant>(ok.Value);
        Assert.Equal(1, updated.MaxConcurrentStreams);
    }

    [Fact]
    public async Task Backfill_WhenDownloadEnqueueThrows_DoesNotLeakExceptionMessage()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var downloadService = new Mock<IDownloadService>();
        downloadService
            .Setup(x => x.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<(string Filename, long Size)>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var shareRepo = new Mock<IShareRepository>();
        shareRepo
            .Setup(x => x.FindContentItem("sha256:test"))
            .Returns(("audio", "work-1", "Music/song.flac", true, string.Empty, 0L));
        shareRepo
            .Setup(x => x.FindFileInfo("Music/song.flac"))
            .Returns(("Music/song.flac", 1234L));

        var shareService = new Mock<slskd.Shares.IShareService>();
        shareService.Setup(x => x.GetLocalRepository()).Returns(shareRepo.Object);

        _sharingMock
            .Setup(x => x.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareGrant
            {
                Id = grantId,
                CollectionId = collectionId,
                AllowDownload = true,
                ShareToken = "token"
            });
        _sharingMock
            .Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "owner" });
        _sharingMock
            .Setup(x => x.GetManifestAsync(grantId, "token", "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareManifestDto
            {
                Items = new List<ShareManifestItemDto>
                {
                    new() { ContentId = "sha256:test", MediaKind = "audio" }
                }
            });

        var loggerMock = new Mock<ILogger<SharesController>>();
        var controller = new SharesController(
            _sharingMock.Object,
            _tokensMock.Object,
            loggerMock.Object,
            _options,
            _httpClientFactoryMock.Object,
            soulseekClient: null,
            shareService: shareService.Object,
            downloadService: downloadService.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "Test"));

        var result = await controller.Backfill(grantId, CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
        Assert.Equal("Failed to enqueue downloads", error.Value);
    }

    [Fact]
    public async Task Backfill_WhenRepositoryResolutionThrows_DoesNotLeakExceptionMessageInErrors()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var shareRepo = new Mock<IShareRepository>();
        shareRepo
            .Setup(x => x.FindContentItem("sha256:test"))
            .Throws(new InvalidOperationException("sensitive detail"));

        var shareService = new Mock<slskd.Shares.IShareService>();
        shareService.Setup(x => x.GetLocalRepository()).Returns(shareRepo.Object);

        _sharingMock
            .Setup(x => x.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareGrant
            {
                Id = grantId,
                CollectionId = collectionId,
                AllowDownload = true,
                ShareToken = "token"
            });
        _sharingMock
            .Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "owner" });
        _sharingMock
            .Setup(x => x.GetManifestAsync(grantId, "token", "alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareManifestDto
            {
                Items = new List<ShareManifestItemDto>
                {
                    new() { ContentId = "sha256:test", MediaKind = "audio" }
                }
            });

        var loggerMock = new Mock<ILogger<SharesController>>();
        var controller = new SharesController(
            _sharingMock.Object,
            _tokensMock.Object,
            loggerMock.Object,
            _options,
            _httpClientFactoryMock.Object,
            soulseekClient: null,
            shareService: shareService.Object,
            downloadService: Mock.Of<IDownloadService>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "Test"));

        var result = await controller.Backfill(grantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BackfillResponse>(ok.Value);
        Assert.Single(response.Errors!);
        Assert.DoesNotContain("sensitive detail", response.Errors[0]);
        Assert.Contains("Error resolving", response.Errors[0]);
    }

    [Fact]
    public async Task Backfill_WithPrivateHttpEndpoint_BlocksBeforeDownloadAndHidesToken()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var temp = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);

        try
        {
            _options = new TestOptionsMonitor(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
                Sharing = new slskd.Options.SharingOptions
                {
                    TrustedPrivateOwnerOrigins = new[] { "http://127.0.0.1:2" }
                },
                Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" },
                Directories = new slskd.Options.DirectoriesOptions { Downloads = temp }
            });

            _sharingMock
                .Setup(x => x.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareGrant
                {
                    Id = grantId,
                    CollectionId = collectionId,
                    AllowDownload = true,
                    ShareToken = "secret-token",
                    OwnerEndpoint = "http://127.0.0.1:1"
                });
            _sharingMock
                .Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "remote" });
            _sharingMock
                .Setup(x => x.GetManifestAsync(grantId, "secret-token", "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareManifestDto
                {
                    Items = new List<ShareManifestItemDto>
                    {
                        new()
                        {
                            ContentId = "sha256:test",
                            MediaKind = "audio",
                            StreamUrl = "http://127.0.0.1:1/api/v0/streams/sha256:test?token=secret-token"
                        }
                    }
                });

            var controller = CreateController();
            var result = await controller.Backfill(grantId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BackfillResponse>(ok.Value);
            Assert.Equal(0, response.Enqueued);
            Assert.Equal(1, response.Failed);
            Assert.Single(response.Errors!);
            Assert.Contains("Blocked unsafe download URL", response.Errors[0]);
            Assert.DoesNotContain("secret-token", response.Errors[0]);
            Assert.DoesNotContain("127.0.0.1", response.Errors[0]);
            Assert.Empty(Directory.GetFiles(temp));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task Backfill_WithExactTrustedPrivateOwnerOrigin_DownloadsWithoutDisablingPublicGuard()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var temp = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-{Guid.NewGuid():N}");
        var content = new byte[] { 0x49, 0x44, 0x33, 0x04 };
        Directory.CreateDirectory(temp);

        try
        {
            _options = new TestOptionsMonitor(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
                Sharing = new slskd.Options.SharingOptions
                {
                    TrustedPrivateOwnerOrigins = new[] { "http://127.0.0.1:5030" }
                },
                Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" },
                Directories = new slskd.Options.DirectoriesOptions { Downloads = temp }
            });

            _sharingMock
                .Setup(x => x.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareGrant
                {
                    Id = grantId,
                    CollectionId = collectionId,
                    AllowDownload = true,
                    ShareToken = "secret-token",
                    OwnerEndpoint = "http://127.0.0.1:5030"
                });
            _sharingMock
                .Setup(x => x.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "remote" });
            _sharingMock
                .Setup(x => x.GetManifestAsync(grantId, "secret-token", "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareManifestDto
                {
                    Items = new List<ShareManifestItemDto>
                    {
                        new()
                        {
                            ContentId = "sha256:trusted",
                            MediaKind = "audio",
                            FileName = "sha256_trusted.mp3",
                            StreamUrl = "http://127.0.0.1:5030/api/v0/streams/sha256:trusted?token=secret-token"
                        }
                    }
                });
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName))
                .Returns(() => new HttpClient(new BackfillResponseHandler(content)));

            var result = await CreateController().Backfill(grantId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BackfillResponse>(ok.Value);
            Assert.Equal(1, response.Enqueued);
            Assert.Equal(0, response.Failed);
            var downloadedFile = Assert.Single(Directory.GetFiles(temp));
            Assert.Equal(content, await File.ReadAllBytesAsync(downloadedFile));
            _httpClientFactoryMock.Verify(
                x => x.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName),
                Times.Once);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task Backfill_WithDanglingDestinationSymlink_UsesSafeUniqueFilename()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var downloadsDirectory = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-root-{Guid.NewGuid():N}");
        var outsideDirectory = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-outside-{Guid.NewGuid():N}");
        var destinationLink = Path.Combine(downloadsDirectory, "sha256_trusted.mp3");
        var outsideFilename = Path.Combine(outsideDirectory, "created.mp3");
        Directory.CreateDirectory(downloadsDirectory);
        Directory.CreateDirectory(outsideDirectory);
        File.CreateSymbolicLink(destinationLink, outsideFilename);

        try
        {
            _options = new TestOptionsMonitor(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
                Sharing = new slskd.Options.SharingOptions
                {
                    TrustedPrivateOwnerOrigins = new[] { "http://127.0.0.1:5030" }
                },
                Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" },
                Directories = new slskd.Options.DirectoriesOptions { Downloads = downloadsDirectory }
            });

            _sharingMock
                .Setup(service => service.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareGrant
                {
                    Id = grantId,
                    CollectionId = collectionId,
                    AllowDownload = true,
                    ShareToken = "secret-token",
                    OwnerEndpoint = "http://127.0.0.1:5030"
                });
            _sharingMock
                .Setup(service => service.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "remote" });
            _sharingMock
                .Setup(service => service.GetManifestAsync(grantId, "secret-token", "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareManifestDto
                {
                    Items = new List<ShareManifestItemDto>
                    {
                        new()
                        {
                            ContentId = "sha256:trusted",
                            MediaKind = "audio",
                            FileName = "sha256_trusted.mp3",
                            StreamUrl = "http://127.0.0.1:5030/api/v0/streams/sha256:trusted?token=secret-token"
                        }
                    }
                });
            _httpClientFactoryMock
                .Setup(factory => factory.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName))
                .Returns(() => new HttpClient(new BackfillResponseHandler(new byte[] { 0x49, 0x44, 0x33, 0x04 })));

            var result = await CreateController().Backfill(grantId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BackfillResponse>(ok.Value);
            Assert.False(File.Exists(outsideFilename));
            Assert.True(File.GetAttributes(destinationLink).HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(1, response.Enqueued);
            Assert.Equal(0, response.Failed);
            var downloadedFile = Assert.Single(Directory.GetFiles(downloadsDirectory), path => path != destinationLink);
            Assert.Equal(new byte[] { 0x49, 0x44, 0x33, 0x04 }, await File.ReadAllBytesAsync(downloadedFile));
        }
        finally
        {
            File.Delete(destinationLink);
            Directory.Delete(downloadsDirectory, recursive: true);
            Directory.Delete(outsideDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Backfill_WithExecutableContent_QuarantinesFileAndReportsFailure()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var downloadsDirectory = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-safety-{Guid.NewGuid():N}");
        var maliciousContent = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        Directory.CreateDirectory(downloadsDirectory);

        try
        {
            _options = new TestOptionsMonitor(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
                Sharing = new slskd.Options.SharingOptions
                {
                    TrustedPrivateOwnerOrigins = new[] { "http://127.0.0.1:5030" }
                },
                Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" },
                Directories = new slskd.Options.DirectoriesOptions { Downloads = downloadsDirectory }
            });

            _sharingMock
                .Setup(service => service.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareGrant
                {
                    Id = grantId,
                    CollectionId = collectionId,
                    AllowDownload = true,
                    ShareToken = "secret-token",
                    OwnerEndpoint = "http://127.0.0.1:5030"
                });
            _sharingMock
                .Setup(service => service.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "remote" });
            _sharingMock
                .Setup(service => service.GetManifestAsync(grantId, "secret-token", "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareManifestDto
                {
                    Items = new List<ShareManifestItemDto>
                    {
                        new()
                        {
                            ContentId = "sha256:trusted",
                            MediaKind = "audio",
                            FileName = "blocked.mp3",
                            StreamUrl = "http://127.0.0.1:5030/api/v0/streams/sha256:trusted?token=secret-token"
                        }
                    }
                });
            _httpClientFactoryMock
                .Setup(factory => factory.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName))
                .Returns(() => new HttpClient(new BackfillResponseHandler(maliciousContent)));

            var result = await CreateController().Backfill(grantId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<BackfillResponse>(ok.Value);
            Assert.Equal(0, response.Enqueued);
            Assert.Equal(1, response.Failed);
            Assert.DoesNotContain(Directory.GetFiles(downloadsDirectory), path => Path.GetFileName(path) == "blocked.mp3");

            var quarantineDirectory = Path.Combine(downloadsDirectory, ".quarantine");
            var quarantinedPath = Assert.Single(Directory.GetFiles(quarantineDirectory));
            Assert.Equal(maliciousContent, await File.ReadAllBytesAsync(quarantinedPath));
        }
        finally
        {
            Directory.Delete(downloadsDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Backfill_WhenCancelledAfterHttpCopy_CleansStagingFileAndPropagatesCancellation()
    {
        var grantId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var downloadsDirectory = Path.Combine(Path.GetTempPath(), $"slskdn-backfill-cancel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(downloadsDirectory);
        using var cancellation = new CancellationTokenSource();

        try
        {
            _options = new TestOptionsMonitor(new slskd.Options
            {
                Feature = new slskd.Options.FeatureOptions { CollectionsSharing = true, Streaming = true },
                Sharing = new slskd.Options.SharingOptions
                {
                    TrustedPrivateOwnerOrigins = new[] { "http://127.0.0.1:5030" }
                },
                Soulseek = new slskd.Options.SoulseekOptions { Username = "daemon-account" },
                Directories = new slskd.Options.DirectoriesOptions { Downloads = downloadsDirectory }
            });
            _sharingMock
                .Setup(service => service.GetAccessibleShareGrantAsync(grantId, "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareGrant
                {
                    Id = grantId,
                    CollectionId = collectionId,
                    AllowDownload = true,
                    ShareToken = "secret-token",
                    OwnerEndpoint = "http://127.0.0.1:5030"
                });
            _sharingMock
                .Setup(service => service.GetCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Collection { Id = collectionId, OwnerUserId = "remote" });
            _sharingMock
                .Setup(service => service.GetManifestAsync(grantId, "secret-token", "alice", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShareManifestDto
                {
                    Items = new List<ShareManifestItemDto>
                    {
                        new()
                        {
                            ContentId = "sha256:trusted",
                            MediaKind = "audio",
                            FileName = "song.mp3",
                            StreamUrl = "http://127.0.0.1:5030/api/v0/streams/sha256:trusted?token=secret-token"
                        }
                    }
                });
            _httpClientFactoryMock
                .Setup(factory => factory.CreateClient(slskd.Common.Security.OutboundUriGuard.LocalNoRedirectHttpClientName))
                .Returns(() => new HttpClient(new BackfillCancellationResponseHandler(
                    new byte[] { 0x49, 0x44, 0x33, 0x04 },
                    cancellation)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => CreateController().Backfill(grantId, cancellation.Token));

            Assert.Empty(Directory.Exists(Path.Combine(downloadsDirectory, ".partial"))
                ? Directory.GetFiles(Path.Combine(downloadsDirectory, ".partial"), "*", SearchOption.AllDirectories)
                : Array.Empty<string>());
            Assert.Empty(Directory.GetFiles(downloadsDirectory, "song.mp3", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            Directory.Delete(downloadsDirectory, recursive: true);
        }
    }

    private sealed class BackfillResponseHandler : HttpMessageHandler
    {
        private readonly byte[] _content;

        public BackfillResponseHandler(byte[] content)
        {
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("http://127.0.0.1:5030/api/v0/streams/sha256:trusted?token=secret-token", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("secret-token", request.Headers.Authorization?.Parameter);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_content),
            });
        }
    }

    private sealed class BackfillCancellationResponseHandler : HttpMessageHandler
    {
        private readonly byte[] _content;
        private readonly CancellationTokenSource _cancellation;

        public BackfillCancellationResponseHandler(byte[] content, CancellationTokenSource cancellation)
        {
            _content = content;
            _cancellation = cancellation;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new CancellationOnEofStream(_content, _cancellation)),
            });
        }
    }

    private sealed class CancellationOnEofStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly CancellationTokenSource _cancellation;

        public CancellationOnEofStream(byte[] content, CancellationTokenSource cancellation)
        {
            _inner = new MemoryStream(content, writable: false);
            _cancellation = cancellation;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            CancelAtEndOfStream(read);
            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => ReadAsyncMemory(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsyncArray(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private async ValueTask<int> ReadAsyncMemory(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            CancelAtEndOfStream(read);
            return read;
        }

        private async Task<int> ReadAsyncArray(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            CancelAtEndOfStream(read);
            return read;
        }

        private void CancelAtEndOfStream(int read)
        {
            if (read == 0)
            {
                _cancellation.Cancel();
            }
        }
    }
}
