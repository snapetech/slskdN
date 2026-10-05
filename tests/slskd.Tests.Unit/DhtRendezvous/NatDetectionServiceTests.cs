// <copyright file="NatDetectionServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.DhtRendezvous;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using slskd.DhtRendezvous;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class NatDetectionServiceTests
{
    [Fact]
    public async Task InitializeAsync_WhenPublicIpServicesThrow_EscapesNetworkExceptions()
    {
        var logger = new CapturingLogger<NatDetectionService>();
        var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("remote HTTP failure\r\nforged"));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(clientFactory => clientFactory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));
        await using var service = new NatDetectionService(
            logger,
            new DhtRendezvousOptions { EnableUpnp = false, EnableStun = false },
            factory.Object);

        await service.InitializeAsync();

        var entries = logger.Entries
            .Where(entry => entry.Message.Contains("HTTP IP detection via", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, entries.Length);
        foreach (var entry in entries)
        {
            Assert.Contains("remote HTTP failure\\r\\nforged", entry.Message);
            Assert.DoesNotContain("\r", entry.Message);
            Assert.DoesNotContain("\n", entry.Message);
            Assert.Null(entry.Exception);
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenCallerCancelsPublicIpProbe_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(clientFactory => clientFactory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new ThrowingHttpMessageHandler(new InvalidOperationException("request should be canceled"))));
        await using var service = new NatDetectionService(
            new CapturingLogger<NatDetectionService>(),
            new DhtRendezvousOptions { EnableUpnp = false, EnableStun = false },
            factory.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.InitializeAsync(cancellation.Token));
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<HttpResponseMessage>(cancellationToken)
                : Task.FromException<HttpResponseMessage>(exception);
    }
}
