// <copyright file="ByzantineConsensusLoggingTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Common.Security;

using Microsoft.Extensions.Logging;
using slskd.Common.Security;
using slskd.Tests.Unit.TestHelpers;
using Xunit;

public sealed class ByzantineConsensusLoggingTests
{
    [Fact]
    public void StartSession_EscapesFilenameOnlyInLogs()
    {
        const string filename = "shared-track.flac\r\nforged warning";
        var logger = new CapturingLogger<ByzantineConsensus>();
        using var consensus = new ByzantineConsensus(logger);

        var sessionId = consensus.StartSession(filename);

        Assert.NotEmpty(sessionId);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("shared-track.flac\\r\\nforged warning", entry.Message);
        Assert.DoesNotContain('\r', entry.Message);
        Assert.DoesNotContain('\n', entry.Message);
    }
}
