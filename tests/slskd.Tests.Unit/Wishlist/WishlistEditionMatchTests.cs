// <copyright file="WishlistEditionMatchTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.Wishlist;

using slskd.Wishlist;
using Xunit;

public class WishlistEditionMatchTests
{
    [Theory]
    [InlineData("Music/Album/01 - Song Title.flac", "Music/Album/1. Song Title.mp3")]
    [InlineData("Music/Album/A1 Song Title.flac", "01 Song Title.flac")]
    [InlineData("Disc 1/03. Song Title.flac", "03 - Song Title.mp3")]
    public void GetNormalizedTrackKey_ignores_numbering_extension_and_punctuation(string a, string b)
    {
        Assert.Equal(WishlistService.GetNormalizedTrackKey(a), WishlistService.GetNormalizedTrackKey(b));
    }

    [Fact]
    public void GetNormalizedTrackKey_distinguishes_different_titles()
    {
        Assert.NotEqual(
            WishlistService.GetNormalizedTrackKey("Music/Album/01 Song One.flac"),
            WishlistService.GetNormalizedTrackKey("Music/Album/02 Song Two.flac"));
    }

    private static WishlistService.WishlistEditionExpectation AlbumExpectation(
        int? trackCount = 12,
        int? durationSeconds = 2400,
        string? disambiguation = null,
        string searchText = "Artist Album")
        => new(
            IsTrackLevel: false,
            TrackCount: trackCount,
            DurationSeconds: durationSeconds,
            ReleaseDisambiguation: disambiguation,
            SearchText: searchText);

    [Fact]
    public void IsEditionMismatch_false_when_track_count_and_duration_match()
    {
        var expectation = AlbumExpectation();
        Assert.False(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album", matchedTrackCount: 12, totalLengthSeconds: 2395));
    }

    [Fact]
    public void IsEditionMismatch_true_when_track_count_deviates_beyond_tolerance()
    {
        var expectation = AlbumExpectation();
        Assert.True(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album", matchedTrackCount: 20, totalLengthSeconds: 2400));
    }

    [Fact]
    public void IsEditionMismatch_tolerates_a_single_track_of_deviation()
    {
        var expectation = AlbumExpectation();
        Assert.False(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album", matchedTrackCount: 13, totalLengthSeconds: 2400));
    }

    [Fact]
    public void IsEditionMismatch_true_when_duration_deviates_beyond_tolerance()
    {
        var expectation = AlbumExpectation(trackCount: null, durationSeconds: 2400);
        Assert.True(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album", matchedTrackCount: 0, totalLengthSeconds: 4800));
    }

    [Fact]
    public void IsEditionMismatch_ignores_track_count_for_track_level_items()
    {
        var expectation = new WishlistService.WishlistEditionExpectation(
            IsTrackLevel: true,
            TrackCount: null,
            DurationSeconds: 200,
            ReleaseDisambiguation: null,
            SearchText: "Artist Album 03 Song");

        Assert.False(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album", matchedTrackCount: 12, totalLengthSeconds: 198));
    }

    [Fact]
    public void IsEditionMismatch_true_when_directory_carries_an_unexpected_edition_marker()
    {
        var expectation = AlbumExpectation(trackCount: null, durationSeconds: null);
        Assert.True(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album (Sessions)", matchedTrackCount: 0, totalLengthSeconds: null));
    }

    [Fact]
    public void IsEditionMismatch_false_when_edition_marker_matches_lidarrs_own_release()
    {
        var expectation = AlbumExpectation(trackCount: null, durationSeconds: null, disambiguation: "Live Sessions");
        Assert.False(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Album (Live Sessions)", matchedTrackCount: 0, totalLengthSeconds: null));
    }

    [Fact]
    public void IsEditionMismatch_false_when_edition_marker_matches_the_search_text()
    {
        var expectation = AlbumExpectation(trackCount: null, durationSeconds: null, searchText: "Artist Live Album");
        Assert.False(WishlistService.IsEditionMismatch(expectation, "Music/Artist/Live Album", matchedTrackCount: 0, totalLengthSeconds: null));
    }

    private static WishlistService.CompletedTrackKey[] AlreadyDownloaded(string key, int? lengthSeconds)
        => [new WishlistService.CompletedTrackKey(key, lengthSeconds)];

    [Fact]
    public void IsAlreadyDownloadedElsewhere_true_for_matching_title_and_close_duration()
    {
        var already = AlreadyDownloaded(WishlistService.GetNormalizedTrackKey("Song Title"), 200);
        Assert.True(WishlistService.IsAlreadyDownloadedElsewhere(already, "Music/OtherPeer/01 - Song Title.flac", 202));
    }

    [Fact]
    public void IsAlreadyDownloadedElsewhere_false_when_duration_differs_beyond_tolerance()
    {
        var already = AlreadyDownloaded(WishlistService.GetNormalizedTrackKey("Song Title"), 60);
        Assert.False(WishlistService.IsAlreadyDownloadedElsewhere(already, "Music/OtherPeer/01 - Song Title.flac", 200));
    }

    [Fact]
    public void IsAlreadyDownloadedElsewhere_true_when_duration_unknown_on_either_side()
    {
        var already = AlreadyDownloaded(WishlistService.GetNormalizedTrackKey("Song Title"), null);
        Assert.True(WishlistService.IsAlreadyDownloadedElsewhere(already, "Music/OtherPeer/01 - Song Title.flac", 200));
    }

    [Fact]
    public void IsAlreadyDownloadedElsewhere_false_for_a_different_song()
    {
        var already = AlreadyDownloaded(WishlistService.GetNormalizedTrackKey("Song Title"), 200);
        Assert.False(WishlistService.IsAlreadyDownloadedElsewhere(already, "Music/OtherPeer/02 - Other Song.flac", 200));
    }

    [Fact]
    public void IsAlreadyDownloadedElsewhere_false_when_nothing_downloaded_yet()
    {
        Assert.False(WishlistService.IsAlreadyDownloadedElsewhere([], "Music/OtherPeer/01 - Song Title.flac", 200));
    }
}
