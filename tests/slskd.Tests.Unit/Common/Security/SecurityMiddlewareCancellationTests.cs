// <copyright file="SecurityMiddlewareCancellationTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using slskd.Common.Security;
using Xunit;

public class SecurityMiddlewareCancellationTests
{
    [Fact]
    public async Task InvokeAsync_WhenClientDisconnects_DoesNotReportServerError()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var context = new DefaultHttpContext
        {
            RequestAborted = cancellation.Token,
        };
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        var logger = new Mock<ILogger<SecurityMiddleware>>();
        var eventSink = new Mock<ISecurityEventSink>();
        using var networkGuard = new NetworkGuard(NullLogger<NetworkGuard>.Instance);
        var middleware = new SecurityMiddleware(
            _ => throw new OperationCanceledException(cancellation.Token),
            logger.Object,
            networkGuard,
            eventSink: eventSink.Object);

        await middleware.InvokeAsync(context);

        eventSink.Verify(sink => sink.Report(It.IsAny<SecurityEvent>()), Times.Never);
        logger.Verify(
            entry => entry.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenCancellationIsNotFromClient_Propagates()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        using var networkGuard = new NetworkGuard(NullLogger<NetworkGuard>.Instance);
        var middleware = new SecurityMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<SecurityMiddleware>.Instance,
            networkGuard);

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
    }
}
