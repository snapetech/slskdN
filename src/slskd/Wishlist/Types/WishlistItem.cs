// <copyright file="WishlistItem.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Wishlist
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     A wishlist item representing a saved search.
    /// </summary>
    public class WishlistItem
    {
        /// <summary>
        ///     Gets or sets the unique identifier.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Gets or sets the search text.
        /// </summary>
        [Required]
        public string SearchText { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the filter expression (optional).
        /// </summary>
        public string Filter { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a value indicating whether the wishlist item is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether to auto-download matches.
        /// </summary>
        public bool AutoDownload { get; set; } = false;

        /// <summary>
        ///     Gets or sets the maximum number of results to keep per search.
        /// </summary>
        public int MaxResults { get; set; } = 100;

        /// <summary>
        ///     Gets or sets the date/time the item was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        ///     Gets or sets the date/time of the last search execution.
        /// </summary>
        public DateTime? LastSearchedAt { get; set; }

        /// <summary>
        ///     Gets or sets the number of matches found in the last search.
        /// </summary>
        public int LastMatchCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the number of visible file hits found in the last search.
        /// </summary>
        public int LastVisibleHitCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the number of locked file hits hidden from the last search.
        /// </summary>
        public int LastHiddenLockedHitCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the number of file hits removed by the wishlist filter from the last search.
        /// </summary>
        public int LastFilteredOutHitCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the number of file hits hidden by persistent ignored-result rules.
        /// </summary>
        public int LastIgnoredResultHitCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the raw response count for the last search.
        /// </summary>
        public int LastResponseCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the total number of searches performed.
        /// </summary>
        public int TotalSearchCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the total number of files downloaded from this wishlist.
        /// </summary>
        public int TotalDownloadCount { get; set; } = 0;

        /// <summary>
        ///     Gets or sets the maximum number of successful downloads before auto-disabling.
        ///     When null, the item is auto-disabled after the first successful auto-download.
        /// </summary>
        public int? MaxDownloads { get; set; }

        /// <summary>
        ///     Gets or sets the Lidarr album ID that created this item, if any.
        /// </summary>
        public int? LidarrAlbumId { get; set; }

        /// <summary>
        ///     Gets or sets the Lidarr track ID that created this item, if any.
        /// </summary>
        public int? LidarrTrackId { get; set; }

        /// <summary>
        ///     Gets or sets the track count Lidarr expects for the monitored release, if known.
        ///     Only meaningful for album-level items; used to reject Soulseek candidates whose
        ///     track count doesn't match the wanted edition (e.g. a "Sessions" release).
        /// </summary>
        public int? LidarrTrackCount { get; set; }

        /// <summary>
        ///     Gets or sets the duration, in seconds, Lidarr expects for the monitored release
        ///     (album-level items) or the specific track (track-level items), if known.
        /// </summary>
        public int? LidarrDurationSeconds { get; set; }

        /// <summary>
        ///     Gets or sets the disambiguation/title text of the Lidarr release this item targets,
        ///     if known. Used so a candidate whose folder name matches Lidarr's own release title
        ///     (e.g. Lidarr genuinely wants the "Live" edition) is not penalized as a mismatch.
        /// </summary>
        public string? LidarrReleaseDisambiguation { get; set; }

        /// <summary>
        ///     Gets or sets the GUID of the most recent search.
        /// </summary>
        public Guid? LastSearchId { get; set; }

        /// <summary>
        ///     Gets or sets the date/time the user last viewed search results for this item.
        /// </summary>
        public DateTime? LastViewedAt { get; set; }
    }
}
