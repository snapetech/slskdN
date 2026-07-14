// <copyright file="WishlistIgnoredResult.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Wishlist;

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class WishlistIgnoredResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WishlistItemId { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Directory { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public WishlistItem WishlistItem { get; set; } = null!;
}
