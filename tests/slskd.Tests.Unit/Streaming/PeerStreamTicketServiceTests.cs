// <copyright file="PeerStreamTicketServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using System;
using slskd.Streaming;
using Xunit;

public class PeerStreamTicketServiceTests
{
    [Fact]
    public void Create_AudioFile_ReturnsShortLivedTicket()
    {
        var service = new PeerStreamTicketService();

        var ticket = service.Create(new PeerStreamTicketRequest("user", @"dir\track.flac", 123), "user:alice", TimeSpan.FromMinutes(2));

        Assert.False(string.IsNullOrWhiteSpace(ticket.Ticket));
        Assert.Equal("user", ticket.Username);
        Assert.Equal(@"dir\track.flac", ticket.Filename);
        Assert.Equal("audio/flac", ticket.ContentType);
        Assert.Same(ticket, service.Validate(ticket.Ticket));
    }

    [Fact]
    public void Create_NonAudioFile_RejectsPreviewStream()
    {
        var service = new PeerStreamTicketService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new PeerStreamTicketRequest("user", "archive.zip", 123), "user:alice", TimeSpan.FromMinutes(2)));
    }

    [Theory]
    [InlineData(@"..\secret.flac")]
    [InlineData(@"Music\..\secret.flac")]
    [InlineData("%2e%2e/secret.flac")]
    [InlineData("/tmp/secret.flac")]
    [InlineData("C:\\tmp\\secret.flac")]
    public void Create_TraversalOrRootedFilename_RejectsPreviewStream(string filename)
    {
        var service = new PeerStreamTicketService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new PeerStreamTicketRequest("user", filename, 123), "user:alice", TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Validate_ExpiredTicket_ReturnsNull()
    {
        var service = new PeerStreamTicketService();
        var ticket = service.Create(new PeerStreamTicketRequest("user", "track.mp3", 123), "user:alice", TimeSpan.FromMilliseconds(-1));

        Assert.Null(service.Validate(ticket.Ticket));
    }
}
