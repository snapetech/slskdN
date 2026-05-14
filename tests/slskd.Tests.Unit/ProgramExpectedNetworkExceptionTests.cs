// <copyright file="ProgramExpectedNetworkExceptionTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit;

using System;
using System.IO;
using slskd.SoulseekExceptions;
using Xunit;

[Collection("ProgramAppDirectory")]
public class ProgramExpectedNetworkExceptionTests
{
    [Fact]
    public void IsExpectedSoulseekNetworkException_ReturnsTrue_ForPeerTimeoutFailures()
    {
        var exception = new AggregateException(
            new TimeoutException("The wait timed out after 10000 milliseconds in Soulseek.Network.PeerConnectionManager."));

        Assert.True(SoulseekNetworkExceptionClassifier.IsExpected(exception));
    }

    [Fact]
    public void IsExpectedSoulseekNetworkException_ReturnsTrue_ForDistributedOperationCanceledFailures()
    {
        var exception = new AggregateException(
            new OperationCanceledException("Operation canceled in Soulseek.Network.DistributedConnectionManager."));

        Assert.True(SoulseekNetworkExceptionClassifier.IsExpected(exception));
    }

    [Fact]
    public void IsExpectedSoulseekNetworkException_ReturnsTrue_ForUnexpectedConnectionCloseFailures()
    {
        var exception = new AggregateException(
            new IOException("The connection was closed unexpectedly in Soulseek.Network.Tcp.Connection."));

        Assert.True(SoulseekNetworkExceptionClassifier.IsExpected(exception));
    }
}
