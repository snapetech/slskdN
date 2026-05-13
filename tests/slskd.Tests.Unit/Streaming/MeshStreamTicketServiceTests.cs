// <copyright file="MeshStreamTicketServiceTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Streaming;

using slskd.Streaming;
using Xunit;

public class MeshStreamTicketServiceTests
{
    [Fact]
    public void Create_AudioFile_ReturnsShortLivedTicket()
    {
        var service = new MeshStreamTicketService();

        var expectedHash = new string('A', 64);
        var ticket = service.Create(new MeshStreamTicketRequest("content-1", "track.flac", "peer-1", 123, expectedHash), "user:alice", TimeSpan.FromMinutes(2));

        Assert.False(string.IsNullOrWhiteSpace(ticket.Ticket));
        Assert.Equal("content-1", ticket.ContentId);
        Assert.Equal("peer-1", ticket.PeerId);
        Assert.Equal("audio/flac", ticket.ContentType);
        Assert.Equal(expectedHash.ToLowerInvariant(), ticket.ExpectedHash);
        Assert.Same(ticket, service.Validate(ticket.Ticket));
    }

    [Fact]
    public void Create_NonAudioFile_RejectsPreviewStream()
    {
        var service = new MeshStreamTicketService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new MeshStreamTicketRequest("content-1", "archive.zip", "peer-1", 123, null), "user:alice", TimeSpan.FromMinutes(2)));
    }

    [Theory]
    [InlineData(@"..\secret.flac")]
    [InlineData(@"Music\..\secret.flac")]
    [InlineData("%2e%2e/secret.flac")]
    [InlineData("/tmp/secret.flac")]
    [InlineData("C:\\tmp\\secret.flac")]
    public void Create_TraversalOrRootedFilename_RejectsPreviewStream(string filename)
    {
        var service = new MeshStreamTicketService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new MeshStreamTicketRequest("content-1", filename, "peer-1", 123, null), "user:alice", TimeSpan.FromMinutes(2)));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Create_InvalidExpectedHash_RejectsPreviewStream(string expectedHash)
    {
        var service = new MeshStreamTicketService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new MeshStreamTicketRequest("content-1", "track.flac", "peer-1", 123, expectedHash), "user:alice", TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Validate_ExpiredTicket_ReturnsNull()
    {
        var service = new MeshStreamTicketService();
        var ticket = service.Create(new MeshStreamTicketRequest("content-1", "track.mp3", null, 123, null), "user:alice", TimeSpan.FromMilliseconds(-1));

        Assert.Null(service.Validate(ticket.Ticket));
    }
}
