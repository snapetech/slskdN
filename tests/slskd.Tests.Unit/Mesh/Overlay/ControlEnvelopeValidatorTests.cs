// <copyright file="ControlEnvelopeValidatorTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Mesh.Overlay;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using slskd.Mesh.Dht;
using slskd.Mesh.Overlay;
using slskd.Mesh.Transport;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class ControlEnvelopeValidatorTests
{
    [Fact]
    public void ValidateEnvelope_WhenDescriptorHasNoSigningKeys_EscapesPeerIdOnlyInLogs()
    {
        const string peerId = "peer-1\r\nforged warning";
        var logger = new CapturingLogger<ControlEnvelopeValidator>();
        var rateLimiter = new RateLimiter(NullLogger<RateLimiter>.Instance);
        var throttler = new ConnectionThrottler(rateLimiter, NullLogger<ConnectionThrottler>.Instance);
        var validator = new ControlEnvelopeValidator(
            new DescriptorSigningService(NullLogger<DescriptorSigningService>.Instance),
            throttler,
            logger);
        var envelope = new ControlEnvelope();
        var descriptor = new MeshPeerDescriptor { PeerId = peerId };

        var result = validator.ValidateEnvelope(envelope, descriptor, peerId);

        Assert.False(result.IsValid);
        var warning = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Null(warning.Exception);
        Assert.Contains("peer-1\\r\\nforged warning", warning.Message);
        Assert.DoesNotContain('\r', warning.Message);
        Assert.DoesNotContain('\n', warning.Message);
    }
}
