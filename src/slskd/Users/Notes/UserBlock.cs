// <copyright file="UserBlock.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Users.Notes;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
///     Represents a durable block applied to a Soulseek user.
/// </summary>
public sealed class UserBlock
{
    /// <summary>
    ///     Gets or sets the blocked username.
    /// </summary>
    [Key]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the time the block was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
