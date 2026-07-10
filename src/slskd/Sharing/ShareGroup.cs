// <copyright file="ShareGroup.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Sharing;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// A group of authenticated web users that can be granted access to a collection.
/// </summary>
public class ShareGroup
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Stable authenticated web-account ID of the owner.</summary>
    [Required]
    [MaxLength(256)]
    public string OwnerUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
