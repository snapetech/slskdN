// <copyright file="IContentLocator.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Streaming;

/// <summary>
/// Resolves a content ID to a local file path and metadata. For indexed shares and configured local playback roots.
/// Never accepts client-supplied paths. Used by streaming and share manifest.
/// </summary>
public interface IContentLocator
{
    /// <summary>
    /// Resolves contentId to a local path and metadata. Returns null if not found, not advertisable, or file missing on disk.
    /// </summary>
    /// <param name="contentId">Indexed content identity or path identity within configured playback roots.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Resolved content or null.</returns>
    ResolvedContent? Resolve(string contentId, CancellationToken cancellationToken = default);

    /// <summary>Registers a server-discovered local file for playback without rescanning its directory.</summary>
    /// <param name="absolutePath">Server-discovered path, never a client-supplied path.</param>
    /// <returns>Path content identity, or null when the file is not allowed or available.</returns>
    string? RegisterLocalFile(string absolutePath);
}

/// <summary>
/// Result of resolving a content ID: absolute path, length, and MIME type.
/// </summary>
public sealed record ResolvedContent(string AbsolutePath, long Length, string ContentType);
