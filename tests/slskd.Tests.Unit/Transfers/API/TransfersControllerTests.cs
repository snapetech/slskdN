// <copyright file="TransfersControllerTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Transfers.API;

using System.Linq;
using System.Linq.Expressions;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using slskd.Transfers;
using slskd.Transfers.API;
using slskd.Transfers.AutoReplace;
using slskd.Transfers.Downloads;
using slskd.Transfers.Uploads;
using Soulseek;
using Xunit;
using SlskdTransfer = slskd.Transfers.Transfer;

public class TransfersControllerTests
{
    [Fact]
    public async Task EnqueueAsync_WithWhitespaceFilename_ReturnsBadRequest()
    {
        var downloads = new Mock<IDownloadService>();
        var controller = CreateController(downloads);

        var result = await controller.EnqueueAsync(
            "alice",
            new[] { new QueueDownloadRequest { Filename = "   ", Size = 10 } });

        Assert.IsType<BadRequestObjectResult>(result);
        downloads.Verify(
            service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnqueueAsync_TrimsUsernameAndFilenameBeforeEnqueue()
    {
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SlskdTransfer>(), new List<string>()));

        var controller = CreateController(downloads);

        var result = await controller.EnqueueAsync(
            " alice ",
            new[] { new QueueDownloadRequest { Filename = " Music/song.flac ", Size = 10 } });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        downloads.Verify(
            service => service.EnqueueAsync(
                "alice",
                It.Is<IEnumerable<DownloadEnqueueRequest>>(files =>
                    files.Single().Filename == "Music/song.flac" && files.Single().BatchId == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_WithMultipleFiles_AssignsSharedBatchId()
    {
        var downloads = new Mock<IDownloadService>();
        List<DownloadEnqueueRequest> queued = new();
        downloads
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<DownloadEnqueueRequest>, CancellationToken>((_, files, _) => queued = files.ToList())
            .ReturnsAsync((new List<SlskdTransfer>(), new List<string>()));

        var controller = CreateController(downloads);

        var result = await controller.EnqueueAsync(
            "alice",
            new[]
            {
                new QueueDownloadRequest { Filename = "Music/one.flac", Size = 10 },
                new QueueDownloadRequest { Filename = "Music/two.flac", Size = 20 },
            });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(2, queued.Count);
        Assert.NotNull(queued[0].BatchId);
        Assert.Equal(queued[0].BatchId, queued[1].BatchId);
    }

    [Fact]
    public async Task EnqueueAsync_WithConfiguredDestination_PassesNormalizedDestination()
    {
        var destination = System.IO.Directory.CreateTempSubdirectory("slskdn-transfer-destination-");
        try
        {
            var downloads = new Mock<IDownloadService>();
            List<DownloadEnqueueRequest> queued = new();
            downloads
                .Setup(service => service.EnqueueAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, IEnumerable<DownloadEnqueueRequest>, CancellationToken>((_, files, _) => queued = files.ToList())
                .ReturnsAsync((new List<SlskdTransfer>(), new List<string>()));

            var options = new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions { Downloads = destination.FullName },
            };
            var controller = CreateController(downloads, options: options);

            var result = await controller.EnqueueAsync(
                "alice",
                new[] { new QueueDownloadRequest { Filename = "Music/song.flac", Size = 10 } },
                destination.FullName);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(Path.GetFullPath(destination.FullName), queued.Single().DestinationDirectory);
        }
        finally
        {
            destination.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task EnqueueAsync_WithDestinationOutsideAllowedRoots_ReturnsBadRequest()
    {
        var allowed = System.IO.Directory.CreateTempSubdirectory("slskdn-transfer-allowed-");
        var outside = System.IO.Directory.CreateTempSubdirectory("slskdn-transfer-outside-");
        try
        {
            var downloads = new Mock<IDownloadService>();
            var options = new slskd.Options
            {
                Directories = new slskd.Options.DirectoriesOptions { Downloads = allowed.FullName },
            };
            var controller = CreateController(downloads, options: options);

            var result = await controller.EnqueueAsync(
                "alice",
                new[] { new QueueDownloadRequest { Filename = "Music/song.flac", Size = 10 } },
                outside.FullName);

            Assert.IsType<BadRequestObjectResult>(result);
            downloads.Verify(
                service => service.EnqueueAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            allowed.Delete(recursive: true);
            outside.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetDownload_WithMismatchedRouteUsername_ReturnsNotFound()
    {
        var downloads = new Mock<IDownloadService>();
        var transferId = Guid.NewGuid();
        downloads
            .Setup(service => service.Find(It.IsAny<Expression<Func<SlskdTransfer, bool>>>()))
            .Returns(new SlskdTransfer { Id = transferId, Username = "bob" });

        var controller = CreateController(downloads);

        var result = controller.GetDownload("alice", transferId.ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPlaceInQueueAsync_WithMismatchedRouteUsername_ReturnsNotFound()
    {
        var downloads = new Mock<IDownloadService>();
        var transferId = Guid.NewGuid();
        downloads
            .Setup(service => service.Find(It.IsAny<Expression<Func<SlskdTransfer, bool>>>()))
            .Returns(new SlskdTransfer { Id = transferId, Username = "bob" });

        var controller = CreateController(downloads);

        var result = await controller.GetPlaceInQueueAsync("alice", transferId.ToString());

        Assert.IsType<NotFoundResult>(result);
        downloads.Verify(service => service.GetPlaceInQueueAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueAsync_WhenEnqueueThrows_DoesNotLeakExceptionMessage()
    {
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<DownloadEnqueueRequest>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = CreateController(downloads);

        var result = await controller.EnqueueAsync(
            "alice",
            new[] { new QueueDownloadRequest { Filename = "Music/song.flac", Size = 10 } });

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.DoesNotContain("sensitive detail", error.Value?.ToString() ?? string.Empty);
        Assert.Equal("Failed to enqueue downloads", error.Value);
    }

    [Fact]
    public async Task GetPlaceInQueueAsync_WhenQueueLookupThrows_ReturnsNoContent()
    {
        var downloads = new Mock<IDownloadService>();
        var transferId = Guid.NewGuid();
        downloads
            .Setup(service => service.Find(It.IsAny<Expression<Func<SlskdTransfer, bool>>>()))
            .Returns(new SlskdTransfer { Id = transferId, Username = "alice" });
        downloads
            .Setup(service => service.GetPlaceInQueueAsync(transferId))
            .ThrowsAsync(new InvalidOperationException("sensitive detail"));

        var controller = CreateController(downloads);

        var result = await controller.GetPlaceInQueueAsync("alice", transferId.ToString());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetPlaceInQueueAsync_WhenQueueLookupTimesOut_ReturnsNoContent()
    {
        var downloads = new Mock<IDownloadService>();
        var transferId = Guid.NewGuid();
        downloads
            .Setup(service => service.Find(It.IsAny<Expression<Func<SlskdTransfer, bool>>>()))
            .Returns(new SlskdTransfer { Id = transferId, Username = "alice" });
        downloads
            .Setup(service => service.GetPlaceInQueueAsync(transferId))
            .ThrowsAsync(new TimeoutException("sensitive remote detail"));

        var controller = CreateController(downloads);

        var result = await controller.GetPlaceInQueueAsync("alice", transferId.ToString());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void GetDownloadBatch_WithInvalidId_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.GetDownloadBatch("not-a-guid");

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public void GetDownloadBatch_WithExistingBatch_ReturnsSummary()
    {
        var batchId = Guid.NewGuid();
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) =>
                new[]
                {
                    new SlskdTransfer
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batchId,
                        Direction = TransferDirection.Download,
                        Filename = "Album\\done.flac",
                        State = TransferStates.Completed | TransferStates.Succeeded,
                    },
                    new SlskdTransfer
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batchId,
                        Direction = TransferDirection.Download,
                        Filename = "Album\\failed.flac",
                        State = TransferStates.Completed | TransferStates.Errored,
                    },
                }.Where(expression!.Compile()).ToList());
        var controller = CreateController(downloads);

        var result = controller.GetDownloadBatch(batchId.ToString());

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DownloadBatchResponse>(ok.Value);
        Assert.Equal(batchId, response.Id);
        Assert.Equal(2, response.TransferCount);
        Assert.Equal(2, response.CompletedCount);
        Assert.Equal(1, response.SucceededCount);
        Assert.Equal(1, response.FailedCount);
    }

    [Fact]
    public void GetDownloadsAsync_WhenCompletedHidden_KeepsFailedTerminalDownloadsVisible()
    {
        var completed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\done.flac",
            Direction = TransferDirection.Download,
            State = TransferStates.Completed | TransferStates.Succeeded,
        };
        var failed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\retry.flac",
            Direction = TransferDirection.Download,
            State = TransferStates.Completed | TransferStates.Errored,
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) =>
                new[] { completed, failed }.Where(expression!.Compile()).ToList());

        var controller = CreateController(downloads);

        var result = controller.GetDownloadsAsync(includeCompleted: false);

        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.Single(Assert.IsAssignableFrom<IEnumerable<UserResponse>>(ok.Value));
        var file = Assert.Single(user.Directories.Single().Files);
        Assert.Equal("Album\\retry.flac", file.Filename);
    }

    [Fact]
    public void GetDownloadsAsync_WhenCompletedHidden_UsesTranslatablePredicate()
    {
        Expression<Func<SlskdTransfer, bool>>? capturedExpression = null;
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Callback<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) => capturedExpression = expression)
            .Returns(new List<SlskdTransfer>());

        var controller = CreateController(downloads);

        controller.GetDownloadsAsync(includeCompleted: false);

        Assert.NotNull(capturedExpression);
        Assert.False(ContainsMethodCall(capturedExpression!.Body));
    }

    [Fact]
    public void GetUploads_WhenCompletedHidden_UsesTranslatablePredicate()
    {
        Expression<Func<SlskdTransfer, bool>>? capturedExpression = null;
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Callback<Expression<Func<SlskdTransfer, bool>>, bool>((expression, _) => capturedExpression = expression)
            .Returns(new List<SlskdTransfer>());

        var controller = CreateController(uploads: uploads);

        controller.GetUploads(includeCompleted: false);

        Assert.NotNull(capturedExpression);
        Assert.False(ContainsMethodCall(capturedExpression!.Body));
    }

    [Fact]
    public void ClearCompletedDownloads_RemovesOnlySuccessfulDownloads()
    {
        var completed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\done.flac",
            Direction = TransferDirection.Download,
            State = TransferStates.Completed | TransferStates.Succeeded,
        };
        var failed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\retry.flac",
            Direction = TransferDirection.Download,
            State = TransferStates.Completed | TransferStates.Errored,
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) =>
                new[] { completed, failed }.Where(expression!.Compile()).ToList());

        var controller = CreateController(downloads);

        var result = controller.ClearCompletedDownloads();

        Assert.IsType<NoContentResult>(result);
        downloads.Verify(service => service.Remove(completed.Id, false), Times.Once);
        downloads.Verify(service => service.Remove(failed.Id, false), Times.Never);
    }

    [Fact]
    public void GetAcceleratedDownloadMode_ReturnsCurrentState()
    {
        var accelerated = new Mock<IAcceleratedDownloadService>();
        accelerated
            .Setup(service => service.GetState())
            .Returns(new AcceleratedDownloadState
            {
                Enabled = true,
                UpdatedAt = DateTime.UtcNow,
                Policy = "policy",
            });

        var controller = CreateController(acceleratedDownloads: accelerated);

        var result = controller.GetAcceleratedDownloadMode();

        var ok = Assert.IsType<OkObjectResult>(result);
        var state = Assert.IsType<AcceleratedDownloadState>(ok.Value);
        Assert.True(state.Enabled);
    }

    [Fact]
    public void SetAcceleratedDownloadMode_UpdatesRuntimeState()
    {
        var accelerated = new Mock<IAcceleratedDownloadService>();
        accelerated
            .Setup(service => service.SetEnabled(true))
            .Returns(new AcceleratedDownloadState
            {
                Enabled = true,
                UpdatedAt = DateTime.UtcNow,
                Policy = "policy",
            });

        var controller = CreateController(acceleratedDownloads: accelerated);

        var result = controller.SetAcceleratedDownloadMode(new AcceleratedDownloadModeRequest { Enabled = true });

        var ok = Assert.IsType<OkObjectResult>(result);
        var state = Assert.IsType<AcceleratedDownloadState>(ok.Value);
        Assert.True(state.Enabled);
        accelerated.Verify(service => service.SetEnabled(true), Times.Once);
    }

    [Fact]
    public async Task GetUploadDiagnostics_WhenListenerAndSharesLookBad_ReturnsActionableWarnings()
    {
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), true))
            .Returns(new List<SlskdTransfer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Username = "remote-user",
                    Filename = "shared\\file.flac",
                    RequestedAt = DateTime.UtcNow,
                    State = TransferStates.Completed | TransferStates.Errored,
                    Exception = "Connection failed",
                },
            });

        var options = new slskd.Options
        {
            Soulseek = new slskd.Options.SoulseekOptions
            {
                ListenIpAddress = "127.0.0.1",
                ListenPort = 1,
            },
        };

        var state = new slskd.State
        {
            Server = new slskd.ServerState
            {
                State = SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn,
            },
            Shares = new slskd.ShareState
            {
                Files = 0,
                Directories = 0,
            },
        };

        var controller = CreateController(uploads: uploads, options: options, state: state);

        var result = await controller.GetUploadDiagnostics();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UploadDiagnosticsResponse>(ok.Value);
        Assert.False(response.LocalListenProbe.Succeeded);
        Assert.Equal("127.0.0.1", response.ListenIpAddress);
        Assert.Equal(1, response.TotalUploadRecords);
        Assert.Contains(response.Warnings, warning => warning.Contains("loopback", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Warnings, warning => warning.Contains("No shared files", StringComparison.OrdinalIgnoreCase));
        Assert.Single(response.RecentUploads);
    }

    [Fact]
    public void GetSpeeds_UsesTransferredBytesWhenAverageSpeedHasNotUpdatedYet()
    {
        var downloadTransfers = new List<SlskdTransfer>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "remote-user",
                Filename = "Music/song.flac",
                BytesTransferred = 20_000,
                StartedAt = DateTime.UtcNow.AddSeconds(-10),
                State = TransferStates.InProgress,
            },
        };
        var uploadTransfers = new List<SlskdTransfer>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "remote-user",
                Filename = "Uploads/song.flac",
                AverageSpeed = 3_000,
                State = TransferStates.InProgress,
            },
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>()))
            .Returns(downloadTransfers);
        downloads
            .Setup(service => service.List(null, true))
            .Returns(downloadTransfers);

        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns(uploadTransfers);
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), true))
            .Returns(uploadTransfers);

        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetSpeeds();

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = GetDoubleProperty(ok.Value!, "total");
        var download = GetDoubleProperty(ok.Value!, "download");
        var upload = GetDoubleProperty(ok.Value!, "upload");
        Assert.InRange(download, 1_500, 2_500);
        Assert.Equal(3_000, upload);
        Assert.InRange(total, 4_500, 5_500);
    }

    private static double GetDoubleProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Convert.ToDouble(property.GetValue(source));
    }

    private static bool ContainsMethodCall(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => ContainsMethodCall(binary.Left) || ContainsMethodCall(binary.Right),
            ConditionalExpression conditional => ContainsMethodCall(conditional.Test) ||
                ContainsMethodCall(conditional.IfTrue) ||
                ContainsMethodCall(conditional.IfFalse),
            LambdaExpression lambda => ContainsMethodCall(lambda.Body),
            MemberExpression member => member.Expression != null && ContainsMethodCall(member.Expression),
            MethodCallExpression => true,
            UnaryExpression unary => ContainsMethodCall(unary.Operand),
            _ => false,
        };
    }

    [Fact]
    public void GetTransfers_WithDownloadDirection_ReturnsOnlyDownloads()
    {
        var download = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\song.flac",
            Direction = TransferDirection.Download,
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Returns(new List<SlskdTransfer> { download });
        var uploads = new Mock<IUploadService>();

        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransfers(direction: "download");

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<SlskdTransfer>>(ok.Value);
        Assert.Equal(new[] { download }, items);
        uploads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void GetTransfers_WithUploadDirection_ReturnsOnlyUploads()
    {
        var upload = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Username = "bob",
            Filename = "Shared\\track.mp3",
            Direction = TransferDirection.Upload,
        };
        var downloads = new Mock<IDownloadService>();
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Returns(new List<SlskdTransfer> { upload });

        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransfers(direction: "upload");

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<SlskdTransfer>>(ok.Value);
        Assert.Equal(new[] { upload }, items);
        downloads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void GetTransfers_WithNoDirection_ReturnsBothDirections()
    {
        var download = new SlskdTransfer { Id = Guid.NewGuid(), Direction = TransferDirection.Download };
        var upload = new SlskdTransfer { Id = Guid.NewGuid(), Direction = TransferDirection.Upload };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Returns(new List<SlskdTransfer> { download });
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Returns(new List<SlskdTransfer> { upload });

        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransfers();

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<SlskdTransfer>>(ok.Value).ToList();
        Assert.Contains(download, items);
        Assert.Contains(upload, items);
    }

    [Fact]
    public void GetTransfers_WithInvalidDirection_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.GetTransfers(direction: "sideways");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetTransfers_WhenCompletedHidden_UsesTranslatablePredicateForBothServices()
    {
        Expression<Func<SlskdTransfer, bool>>? downloadExpression = null;
        Expression<Func<SlskdTransfer, bool>>? uploadExpression = null;

        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Callback<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) => downloadExpression = expression)
            .Returns(new List<SlskdTransfer>());
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()))
            .Callback<Expression<Func<SlskdTransfer, bool>>, bool>((expression, _) => uploadExpression = expression)
            .Returns(new List<SlskdTransfer>());

        var controller = CreateController(downloads: downloads, uploads: uploads);

        controller.GetTransfers(includeCompleted: false);

        Assert.NotNull(downloadExpression);
        Assert.False(ContainsMethodCall(downloadExpression!.Body));
        Assert.NotNull(uploadExpression);
        Assert.False(ContainsMethodCall(uploadExpression!.Body));
    }

    [Fact]
    public void GetTransferChanges_WithCursor_ReturnsOnlyChangedRowsAndIncludesRemoved()
    {
        var cutoff = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        var old = new SlskdTransfer { Id = Guid.NewGuid(), UpdatedAt = cutoff.AddSeconds(-1) };
        var changed = new SlskdTransfer { Id = Guid.NewGuid(), UpdatedAt = cutoff.AddSeconds(1) };
        var removed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            Direction = TransferDirection.Upload,
            Removed = true,
            UpdatedAt = cutoff.AddSeconds(1),
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), true))
            .Returns<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) =>
                new[] { old, changed }.Where(expression!.Compile()).ToList());
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), true))
            .Returns<Expression<Func<SlskdTransfer, bool>>, bool>((expression, _) =>
                new[] { removed }.Where(expression.Compile()).ToList());
        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransferChanges(new DateTimeOffset(cutoff).ToUnixTimeMilliseconds());

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TransferChangesResponse>(ok.Value);
        Assert.Equal(new[] { changed, removed }, response.Transfers.ToList());
        Assert.True(response.Cursor >= new DateTimeOffset(cutoff).ToUnixTimeMilliseconds());
    }

    [Fact]
    public void GetTransferChanges_InitialSnapshotExcludesRemovedRows()
    {
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns(new List<SlskdTransfer>());
        var uploads = new Mock<IUploadService>();
        uploads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns(new List<SlskdTransfer>());
        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransferChanges();

        Assert.IsType<TransferChangesResponse>(Assert.IsType<OkObjectResult>(result).Value);
        downloads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false),
            Times.Once);
        uploads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false),
            Times.Once);
    }

    [Fact]
    public void GetTransferChanges_WithNegativeCursorReturnsBadRequestWithoutQueries()
    {
        var downloads = new Mock<IDownloadService>();
        var uploads = new Mock<IUploadService>();
        var controller = CreateController(downloads: downloads, uploads: uploads);

        var result = controller.GetTransferChanges(-1);

        Assert.IsType<BadRequestObjectResult>(result);
        downloads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()),
            Times.Never);
        uploads.Verify(
            service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void ClearCompletedDownloads_EmitsRemovedActivityForRemovedTransfers()
    {
        var completed = new SlskdTransfer
        {
            Id = Guid.NewGuid(),
            RequestId = Guid.NewGuid(),
            Username = "alice",
            Filename = "Album\\done.flac",
            Direction = TransferDirection.Download,
            State = TransferStates.Completed | TransferStates.Succeeded,
        };
        var downloads = new Mock<IDownloadService>();
        downloads
            .Setup(service => service.List(It.IsAny<Expression<Func<SlskdTransfer, bool>>>(), false))
            .Returns<Expression<Func<SlskdTransfer, bool>>?, bool>((expression, _) =>
                new[] { completed }.Where(expression!.Compile()).ToList());
        var hub = CreateHubMock(out var clientProxy);

        var controller = CreateController(downloads: downloads, transfersHub: hub);

        var result = controller.ClearCompletedDownloads();

        Assert.IsType<NoContentResult>(result);
        clientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                TransferHubMethods.Removed,
                It.Is<object[]>(arguments =>
                    arguments.Length == 1 &&
                    ((TransferRemoved)arguments[0]).RequestId == completed.RequestId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mock<IHubContext<TransfersHub>> CreateHubMock(out Mock<IClientProxy> clientProxy)
    {
        clientProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.SetupGet(c => c.All).Returns(clientProxy.Object);

        var hub = new Mock<IHubContext<TransfersHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        return hub;
    }

    private static TransfersController CreateController(
        Mock<IDownloadService>? downloads = null,
        Mock<IUploadService>? uploads = null,
        Mock<IAcceleratedDownloadService>? acceleratedDownloads = null,
        slskd.Options? options = null,
        slskd.State? state = null,
        Mock<IHubContext<TransfersHub>>? transfersHub = null)
    {
        var transferService = new Mock<ITransferService>();
        transferService.SetupGet(service => service.Downloads).Returns((downloads ?? new Mock<IDownloadService>()).Object);
        transferService.SetupGet(service => service.Uploads).Returns((uploads ?? new Mock<IUploadService>()).Object);

        var optionsSnapshot = new Mock<IOptionsSnapshot<slskd.Options>>();
        optionsSnapshot.SetupGet(snapshot => snapshot.Value).Returns(options ?? new slskd.Options());

        var stateSnapshot = new Mock<IStateSnapshot<slskd.State>>();
        stateSnapshot.SetupGet(snapshot => snapshot.Value).Returns(state ?? new slskd.State());

        using var autoReplaceBackgroundService = new AutoReplaceBackgroundService(
            Mock.Of<IAutoReplaceService>(),
            Mock.Of<ISoulseekClient>(),
            Mock.Of<IOptionsMonitor<slskd.Options>>(),
            new OptionsAtStartup());

        return new TransfersController(
            transferService.Object,
            optionsSnapshot.Object,
            stateSnapshot.Object,
            Mock.Of<IAutoReplaceService>(),
            autoReplaceBackgroundService,
            (acceleratedDownloads ?? new Mock<IAcceleratedDownloadService>()).Object,
            (transfersHub ?? CreateHubMock(out _)).Object);
    }
}
