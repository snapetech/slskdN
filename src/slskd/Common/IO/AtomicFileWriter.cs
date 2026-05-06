// <copyright file="AtomicFileWriter.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.IO;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Writes durable state files through flushed sibling temp files and atomic replacement.
/// </summary>
public static class AtomicFileWriter
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static void WriteAllText(string path, string contents, UnixFileMode? unixFileMode = null)
    {
        WriteAllBytes(path, Utf8NoBom.GetBytes(contents), unixFileMode);
    }

    public static void WriteAllBytes(string path, byte[] contents, UnixFileMode? unixFileMode = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(contents);

        var tempPath = CreateTempPath(path);

        try
        {
            EnsureDirectory(path);

            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(contents, 0, contents.Length);
                stream.Flush(flushToDisk: true);
            }

            SetUnixFileModeIfNeeded(tempPath, unixFileMode);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    public static Task WriteAllTextAsync(
        string path,
        string contents,
        CancellationToken cancellationToken = default,
        UnixFileMode? unixFileMode = null)
    {
        return WriteAllBytesAsync(path, Utf8NoBom.GetBytes(contents), cancellationToken, unixFileMode);
    }

    public static async Task WriteAllBytesAsync(
        string path,
        byte[] contents,
        CancellationToken cancellationToken = default,
        UnixFileMode? unixFileMode = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(contents);

        var tempPath = CreateTempPath(path);

        try
        {
            EnsureDirectory(path);

            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(contents, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            SetUnixFileModeIfNeeded(tempPath, unixFileMode);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private static string CreateTempPath(string path)
    {
        return $"{path}.{Guid.NewGuid():N}.tmp";
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void SetUnixFileModeIfNeeded(string tempPath, UnixFileMode? unixFileMode)
    {
        if (unixFileMode.HasValue && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(tempPath, unixFileMode.Value);
        }
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch
        {
            // best-effort cleanup; preserve the original failure.
        }
    }
}
