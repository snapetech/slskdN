// <copyright file="ContentSafety.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Common.Security;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides content safety verification for downloaded files.
/// SECURITY: Detects file type mismatches (e.g., .exe disguised as .mp3).
/// </summary>
public static class ContentSafety
{
    /// <summary>
    /// Minimum header size for reliable detection.
    /// </summary>
    public const int MinHeaderSize = 16;

    /// <summary>
    /// Creates a unique, rooted staging path that preserves the destination extension.
    /// </summary>
    /// <param name="destinationPath">The intended final file path.</param>
    /// <param name="trustedRoot">The trusted root that contains the final and staging paths.</param>
    /// <returns>A path beneath the root's hidden `.partial` directory.</returns>
    public static string CreateStagingPath(string destinationPath, string trustedRoot)
    {
        var safeDestinationPath = PathGuard.NormalizeAbsolutePathWithinRoots(destinationPath, new[] { trustedRoot })
            ?? throw new IOException("Destination path resolves outside its configured directory");
        var stagingPath = Path.Combine(
            Path.GetFullPath(trustedRoot),
            ".partial",
            Guid.NewGuid().ToString("N"),
            Path.GetFileName(safeDestinationPath));
        return PathGuard.NormalizeAbsolutePathWithinRoots(stagingPath, new[] { trustedRoot })
            ?? throw new IOException("Staging path resolves outside its configured directory");
    }

    /// <summary>
    /// Publishes a completed staging file after it has passed content-safety policy.
    /// </summary>
    /// <param name="stagingPath">The closed staging file.</param>
    /// <param name="destinationPath">The intended final file path.</param>
    /// <param name="trustedRoot">The trusted root for both paths.</param>
    /// <param name="overwrite">Whether to replace an existing final file.</param>
    /// <returns>The normalized final file path.</returns>
    public static string PublishStagedFile(string stagingPath, string destinationPath, string trustedRoot, bool overwrite = false)
    {
        var stagingRoot = Path.Combine(Path.GetFullPath(trustedRoot), ".partial");
        var safeStagingPath = PathGuard.NormalizeAbsolutePathWithinRoots(stagingPath, new[] { stagingRoot })
            ?? throw new IOException("Staging path resolves outside its private directory");
        var safeDestinationPath = PathGuard.NormalizeAbsolutePathWithinRoots(destinationPath, new[] { trustedRoot })
            ?? throw new IOException("Destination path resolves outside its configured directory");

        var destinationDirectory = Path.GetDirectoryName(safeDestinationPath)
            ?? throw new IOException("Destination path has no parent directory");
        var safeDestinationDirectory = PathGuard.NormalizeAbsolutePathWithinRoots(destinationDirectory, new[] { trustedRoot })
            ?? throw new IOException("Destination directory resolves outside its configured directory");
        Directory.CreateDirectory(safeDestinationDirectory);
        safeDestinationPath = PathGuard.NormalizeAbsolutePathWithinRoots(destinationPath, new[] { trustedRoot })
            ?? throw new IOException("Destination path resolves outside its configured directory");

        File.Move(safeStagingPath, safeDestinationPath, overwrite);
        CleanupEmptyStagingDirectories(safeStagingPath, trustedRoot);
        return safeDestinationPath;
    }

    /// <summary>
    /// Removes a partial staging file and its empty private staging directories.
    /// </summary>
    /// <param name="stagingPath">The staging file to remove.</param>
    /// <param name="trustedRoot">The trusted root for the staging path.</param>
    public static void DeleteStagedFile(string stagingPath, string trustedRoot)
    {
        var stagingRoot = Path.Combine(Path.GetFullPath(trustedRoot), ".partial");
        var safeStagingPath = PathGuard.NormalizeAbsolutePathWithinRoots(stagingPath, new[] { stagingRoot })
            ?? throw new IOException("Staging path resolves outside its private directory");
        if (File.Exists(safeStagingPath))
        {
            File.Delete(safeStagingPath);
        }

        CleanupEmptyStagingDirectories(safeStagingPath, trustedRoot);
    }

    /// <summary>
    /// Inspects a completed receive and applies the configured rejection and quarantine policy.
    /// </summary>
    /// <param name="filePath">The completed file path.</param>
    /// <param name="fileRoot">The trusted root that contains the completed file.</param>
    /// <param name="downloadsRoot">The configured downloads root used to resolve relative quarantine paths.</param>
    /// <param name="securityOptions">The active security options.</param>
    /// <param name="cancellationToken">The token to monitor while inspecting the file.</param>
    /// <param name="logger">Optional logger for verification diagnostics.</param>
    /// <param name="quarantineFilename">Optional final filename to use when quarantining a staged receive.</param>
    /// <returns>The verification and local disposition applied to the file.</returns>
    public static async Task<ContentSafetyDisposition> InspectAndApplyPolicyAsync(
        string filePath,
        string fileRoot,
        string downloadsRoot,
        SecurityOptions securityOptions,
        CancellationToken cancellationToken = default,
        ILogger? logger = null,
        string? quarantineFilename = null)
    {
        var options = securityOptions.ContentSafety;
        if (!securityOptions.Enabled || !options.Enabled || (!options.VerifyMagicBytes && !options.BlockExecutables))
        {
            return ContentSafetyDisposition.Skipped;
        }

        var safeFilePath = PathGuard.NormalizeAbsolutePathWithinRoots(filePath, new[] { fileRoot })
            ?? throw new IOException("Completed download path resolves outside its configured directory");
        var verification = await VerifyFileAsync(safeFilePath, cancellationToken, logger).ConfigureAwait(false);
        var executableBlocked = verification.ThreatLevel == ContentThreatLevel.Dangerous
            || (verification.ThreatLevel == ContentThreatLevel.Executable && options.BlockExecutables);
        var magicBytesRejected = options.VerifyMagicBytes && !verification.IsValid;
        var warningQuarantined = options.VerifyMagicBytes && verification.IsWarning && options.QuarantineSuspicious;
        var rejected = executableBlocked || magicBytesRejected || warningQuarantined;

        if (verification.IsWarning || !verification.IsValid)
        {
            logger?.LogWarning(
                "Content verification for {Filename} reported {ThreatLevel}: {Message}",
                Path.GetFileName(safeFilePath),
                verification.ThreatLevel,
                verification.Message);
        }

        if (!rejected)
        {
            return new ContentSafetyDisposition(true, false, verification, null);
        }

        if (options.QuarantineSuspicious)
        {
            try
            {
                var configuredDirectory = options.QuarantineDirectory;
                var quarantineDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
                    ? Path.Combine(downloadsRoot, ".quarantine")
                    : Path.IsPathRooted(configuredDirectory)
                        ? configuredDirectory
                        : Path.Combine(downloadsRoot, configuredDirectory);
                quarantineDirectory = Path.GetFullPath(quarantineDirectory);

                if (PathGuard.NormalizeAbsolutePathWithinRoots(quarantineDirectory, new[] { quarantineDirectory }) == null)
                {
                    throw new IOException("Configured quarantine directory is invalid");
                }

                Directory.CreateDirectory(quarantineDirectory);
                var safeQuarantineDirectory = PathGuard.NormalizeAbsolutePathWithinRoots(
                    quarantineDirectory,
                    new[] { quarantineDirectory })
                    ?? throw new IOException("Configured quarantine directory is invalid");
                var quarantinedPath = MoveToQuarantine(
                    safeFilePath,
                    safeQuarantineDirectory,
                    quarantineFilename ?? Path.GetFileName(safeFilePath));
                CleanupEmptyStagingDirectories(safeFilePath, fileRoot);
                return new ContentSafetyDisposition(true, true, verification, quarantinedPath);
            }
            catch (Exception quarantineException)
            {
                try
                {
                    File.Delete(safeFilePath);
                    CleanupEmptyStagingDirectories(safeFilePath, fileRoot);
                }
                catch (Exception removalException)
                {
                    throw new AggregateException(
                        "Content-safety rejected output could not be quarantined or removed",
                        quarantineException,
                        removalException);
                }

                throw new ContentSafetyRejectedException(
                    $"Content-safety rejected output was removed because quarantine failed: {verification.Message}",
                    quarantineException);
            }
        }

        File.Delete(safeFilePath);
        CleanupEmptyStagingDirectories(safeFilePath, fileRoot);
        return new ContentSafetyDisposition(true, true, verification, null);
    }

    private static string MoveToQuarantine(string sourcePath, string quarantineDirectory, string filename)
    {
        filename = PathGuard.SanitizeFilename(Path.GetFileName(filename));
        var destinationPath = PathGuard.NormalizeAbsolutePathWithinRoots(
            Path.Combine(quarantineDirectory, filename),
            new[] { quarantineDirectory })
            ?? throw new IOException("Quarantine file path is invalid");

        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
        {
            var extension = Path.GetExtension(filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            destinationPath = PathGuard.NormalizeAbsolutePathWithinRoots(
                Path.Combine(quarantineDirectory, $"{name}_{Guid.NewGuid():N}{extension}"),
                new[] { quarantineDirectory })
                ?? throw new IOException("Quarantine file path is invalid");
        }

        File.Move(sourcePath, destinationPath, overwrite: false);
        return destinationPath;
    }

    private static void CleanupEmptyStagingDirectories(string stagingPath, string trustedRoot)
    {
        var stagingRoot = Path.Combine(Path.GetFullPath(trustedRoot), ".partial");
        var stagingDirectory = Path.GetDirectoryName(stagingPath);
        if (stagingDirectory == null ||
            PathGuard.NormalizeAbsolutePathWithinRoots(stagingDirectory, new[] { stagingRoot }) == null)
        {
            return;
        }

        try
        {
            if (Directory.Exists(stagingDirectory) && !Directory.EnumerateFileSystemEntries(stagingDirectory).Any())
            {
                Directory.Delete(stagingDirectory);
            }

            if (Directory.Exists(stagingRoot) && !Directory.EnumerateFileSystemEntries(stagingRoot).Any())
            {
                Directory.Delete(stagingRoot);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// File signature (magic bytes) definitions.
    /// </summary>
    private static readonly Dictionary<string, FileSignature[]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        // Audio formats
        [".flac"] = new[] { new FileSignature(new byte[] { 0x66, 0x4C, 0x61, 0x43 }, 0, "FLAC audio") },
        [".mp3"] = new[]
        {
            new FileSignature(new byte[] { 0xFF, 0xFB }, 0, "MP3 MPEG frame"),
            new FileSignature(new byte[] { 0xFF, 0xFA }, 0, "MP3 MPEG frame"),
            new FileSignature(new byte[] { 0xFF, 0xF3 }, 0, "MP3 MPEG frame"),
            new FileSignature(new byte[] { 0xFF, 0xF2 }, 0, "MP3 MPEG frame"),
            new FileSignature(new byte[] { 0x49, 0x44, 0x33 }, 0, "MP3 ID3v2 tag"),
        },
        [".ogg"] = new[] { new FileSignature(new byte[] { 0x4F, 0x67, 0x67, 0x53 }, 0, "Ogg container") },
        [".opus"] = new[] { new FileSignature(new byte[] { 0x4F, 0x67, 0x67, 0x53 }, 0, "Opus in Ogg") },
        [".wav"] = new[] { new FileSignature(new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0, "WAV RIFF") },
        [".m4a"] = new[]
        {
            new FileSignature(new byte[] { 0x66, 0x74, 0x79, 0x70 }, 4, "M4A ftyp"),
        },
        [".aac"] = new[]
        {
            new FileSignature(new byte[] { 0xFF, 0xF1 }, 0, "AAC ADTS"),
            new FileSignature(new byte[] { 0xFF, 0xF9 }, 0, "AAC ADTS"),
        },
        [".wma"] = new[] { new FileSignature(new byte[] { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11 }, 0, "WMA ASF") },
        [".ape"] = new[] { new FileSignature(new byte[] { 0x4D, 0x41, 0x43, 0x20 }, 0, "Monkey's Audio") },
        [".wv"] = new[] { new FileSignature(new byte[] { 0x77, 0x76, 0x70, 0x6B }, 0, "WavPack") },
        [".aiff"] = new[] { new FileSignature(new byte[] { 0x46, 0x4F, 0x52, 0x4D }, 0, "AIFF FORM") },
        [".aif"] = new[] { new FileSignature(new byte[] { 0x46, 0x4F, 0x52, 0x4D }, 0, "AIFF FORM") },

        // Image formats (sometimes shared on Soulseek)
        [".jpg"] = new[] { new FileSignature(new byte[] { 0xFF, 0xD8, 0xFF }, 0, "JPEG") },
        [".jpeg"] = new[] { new FileSignature(new byte[] { 0xFF, 0xD8, 0xFF }, 0, "JPEG") },
        [".png"] = new[] { new FileSignature(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, "PNG") },
        [".gif"] = new[] { new FileSignature(new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0, "GIF") },

        // Archive formats
        [".zip"] = new[]
        {
            new FileSignature(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0, "ZIP"),
            new FileSignature(new byte[] { 0x50, 0x4B, 0x05, 0x06 }, 0, "ZIP empty"),
        },
        [".rar"] = new[] { new FileSignature(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, 0, "RAR") },
        [".7z"] = new[] { new FileSignature(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, 0, "7-Zip") },

        // PDF
        [".pdf"] = new[] { new FileSignature(new byte[] { 0x25, 0x50, 0x44, 0x46 }, 0, "PDF") },
    };

    /// <summary>
    /// Magic bytes that indicate dangerous/executable content regardless of extension.
    /// </summary>
    private static readonly FileSignature[] DangerousSignatures = new[]
    {
        new FileSignature(new byte[] { 0x4D, 0x5A }, 0, "PE/DOS executable"),
        new FileSignature(new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, 0, "ELF executable"),
        new FileSignature(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, 0, "Java class/Mach-O fat"),
        new FileSignature(new byte[] { 0xCE, 0xFA, 0xED, 0xFE }, 0, "Mach-O 32-bit"),
        new FileSignature(new byte[] { 0xCF, 0xFA, 0xED, 0xFE }, 0, "Mach-O 64-bit"),
        new FileSignature(new byte[] { 0xFE, 0xED, 0xFA, 0xCE }, 0, "Mach-O 32-bit (BE)"),
        new FileSignature(new byte[] { 0xFE, 0xED, 0xFA, 0xCF }, 0, "Mach-O 64-bit (BE)"),
        new FileSignature(new byte[] { 0x23, 0x21 }, 0, "Shell script (#!)"),
        new FileSignature(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x08, 0x08, 0x08, 0x00 }, 0, "JAR/APK archive"),
    };

    /// <summary>
    /// Verifies that a file's content matches its extension.
    /// </summary>
    /// <param name="filePath">Path to the file to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="logger">Optional logger for failure diagnostics.</param>
    /// <returns>Verification result.</returns>
    public static async Task<ContentVerificationResult> VerifyFileAsync(
        string filePath,
        CancellationToken cancellationToken = default,
        ILogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            return ContentVerificationResult.Fail("File not found", ContentThreatLevel.Unknown);
        }

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return ContentVerificationResult.Fail("No file extension", ContentThreatLevel.Suspicious);
        }

        // Read header bytes
        byte[] header;
        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            header = new byte[Math.Min(stream.Length, MinHeaderSize)];
            await stream.ReadExactlyAsync(header, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Failed to read file for content verification: {FilePath}",
                filePath);
            return ContentVerificationResult.Fail("Could not read file", ContentThreatLevel.Unknown);
        }

        return VerifyHeader(header, extension);
    }

    /// <summary>
    /// Verifies content from a byte array (for streaming verification).
    /// </summary>
    /// <param name="header">First bytes of the file (at least 16 bytes recommended).</param>
    /// <param name="expectedExtension">Expected file extension (including dot).</param>
    /// <returns>Verification result.</returns>
    public static ContentVerificationResult VerifyHeader(byte[] header, string expectedExtension)
    {
        if (header == null || header.Length < 2)
        {
            return ContentVerificationResult.Fail("Header too short for analysis", ContentThreatLevel.Unknown);
        }

        var extension = expectedExtension?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return ContentVerificationResult.Fail("No expected extension provided", ContentThreatLevel.Unknown);
        }

        // Check for dangerous content first (regardless of claimed extension)
        foreach (var sig in DangerousSignatures)
        {
            if (MatchesSignature(header, sig))
            {
                // Is this expected for the extension?
                if (extension is ".exe" or ".dll" or ".com" or ".scr" or ".pif")
                {
                    return ContentVerificationResult.Warn(
                        $"Executable file detected: {sig.Description}",
                        ContentThreatLevel.Executable,
                        sig.Description);
                }

                // Extension mismatch with executable content!
                return ContentVerificationResult.Fail(
                    $"DANGEROUS: File claims to be {extension} but contains {sig.Description}",
                    ContentThreatLevel.Dangerous,
                    sig.Description);
            }
        }

        // Check if file matches expected signature for its extension
        if (Signatures.TryGetValue(extension, out var expectedSigs))
        {
            foreach (var sig in expectedSigs)
            {
                if (MatchesSignature(header, sig))
                {
                    return ContentVerificationResult.Success(extension, sig.Description);
                }
            }

            // Has known extension but doesn't match any expected signature
            return ContentVerificationResult.Warn(
                $"Content does not match expected {extension} signature",
                ContentThreatLevel.Mismatch,
                "Unknown");
        }

        // Unknown extension - can't verify
        return ContentVerificationResult.Success(extension, "Unknown format");
    }

    /// <summary>
    /// Quick check if header bytes indicate an executable.
    /// </summary>
    /// <param name="header">File header bytes.</param>
    /// <returns>True if executable signatures detected.</returns>
    public static bool IsExecutable(byte[] header)
    {
        if (header == null || header.Length < 2)
        {
            return false;
        }

        foreach (var sig in DangerousSignatures)
        {
            if (MatchesSignature(header, sig))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detect the actual file type from header bytes.
    /// </summary>
    /// <param name="header">File header bytes.</param>
    /// <returns>Detected file type description, or null if unknown.</returns>
    public static string? DetectFileType(byte[] header)
    {
        if (header == null || header.Length < 2)
        {
            return null;
        }

        // Check dangerous first
        foreach (var sig in DangerousSignatures)
        {
            if (MatchesSignature(header, sig))
            {
                return sig.Description;
            }
        }

        // Check known formats
        foreach (var kvp in Signatures)
        {
            foreach (var sig in kvp.Value)
            {
                if (MatchesSignature(header, sig))
                {
                    return sig.Description;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Check if header matches a signature.
    /// </summary>
    private static bool MatchesSignature(byte[] data, FileSignature signature)
    {
        if (data.Length < signature.Offset + signature.Magic.Length)
        {
            return false;
        }

        for (int i = 0; i < signature.Magic.Length; i++)
        {
            if (data[signature.Offset + i] != signature.Magic[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// File signature definition.
    /// </summary>
    private readonly struct FileSignature
    {
        public byte[] Magic { get; }
        public int Offset { get; }
        public string Description { get; }

        public FileSignature(byte[] magic, int offset, string description)
        {
            Magic = magic is null ? throw new ArgumentNullException(nameof(magic)) : magic.ToArray();
            Offset = offset;
            Description = description;
        }
    }
}

/// <summary>
/// Result of applying content-safety policy to a completed receive.
/// </summary>
public readonly record struct ContentSafetyDisposition(
    bool Inspected,
    bool Rejected,
    ContentVerificationResult? Verification,
    string? QuarantinedPath)
{
    /// <summary>
    /// Gets a disposition for a path that policy did not inspect.
    /// </summary>
    public static ContentSafetyDisposition Skipped => new(false, false, null, null);
}

/// <summary>
/// Indicates that a completed receive was rejected by content-safety policy.
/// </summary>
public sealed class ContentSafetyRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSafetyRejectedException"/> class.
    /// </summary>
    /// <param name="message">The rejection reason.</param>
    /// <param name="innerException">Optional exception that prevented quarantine.</param>
    public ContentSafetyRejectedException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Result of content verification.
/// </summary>
public readonly struct ContentVerificationResult
{
    /// <summary>
    /// Gets a value indicating whether the content is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a warning (valid but suspicious).
    /// </summary>
    public bool IsWarning { get; init; }

    /// <summary>
    /// Gets the message describing the result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the detected content type.
    /// </summary>
    public string? DetectedType { get; init; }

    /// <summary>
    /// Gets the threat level of the content.
    /// </summary>
    public ContentThreatLevel ThreatLevel { get; init; }

    /// <summary>
    /// Creates a successful verification result.
    /// </summary>
    public static ContentVerificationResult Success(string extension, string detectedType) => new()
    {
        IsValid = true,
        IsWarning = false,
        DetectedType = detectedType,
        ThreatLevel = ContentThreatLevel.Safe,
        Message = $"Content verified as {detectedType}",
    };

    /// <summary>
    /// Creates a warning result (valid but suspicious).
    /// </summary>
    public static ContentVerificationResult Warn(string message, ContentThreatLevel level, string? detectedType = null) => new()
    {
        IsValid = true,
        IsWarning = true,
        Message = message,
        DetectedType = detectedType,
        ThreatLevel = level,
    };

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static ContentVerificationResult Fail(string message, ContentThreatLevel level, string? detectedType = null) => new()
    {
        IsValid = false,
        IsWarning = false,
        Message = message,
        DetectedType = detectedType,
        ThreatLevel = level,
    };
}

/// <summary>
/// Threat level of detected content.
/// </summary>
public enum ContentThreatLevel
{
    /// <summary>Content is safe and matches expected type.</summary>
    Safe,

    /// <summary>Content type is unknown.</summary>
    Unknown,

    /// <summary>Content is suspicious but not necessarily dangerous.</summary>
    Suspicious,

    /// <summary>Content type doesn't match extension.</summary>
    Mismatch,

    /// <summary>Content is an executable (may be intentional).</summary>
    Executable,

    /// <summary>Content is dangerous (executable disguised as media).</summary>
    Dangerous,
}
