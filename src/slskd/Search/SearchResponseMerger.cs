// <copyright file="SearchResponseMerger.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Search;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Merges and deduplicates Soulseek and mesh search responses using normalized filename and size for deduplication.
/// </summary>
public static class SearchResponseMerger
{
    /// <summary>
    /// Deduplicates Soulseek and mesh responses using (Username, normalized filename, size) as the deduplication key.
    /// Keeps first occurrence of each unique file.
    /// </summary>
    public static List<Response> Deduplicate(IEnumerable<Response> soulseekResponses, IReadOnlyList<Response> meshResponses)
    {
        var seenByAsciiFilename = new HashSet<(string Username, string Filename, long Size)>(AsciiFilenameKeyComparer.Instance);
        HashSet<(string Username, string NormalizedFilename, long Size)>? seenByUnicodeFilename = null;
        var merged = new List<Response>();

        bool AddFilename(string username, File file)
        {
            var filename = file.Filename ?? string.Empty;
            if (IsAscii(filename))
            {
                return seenByAsciiFilename.Add((username, filename, file.Size));
            }

            var normalized = NormalizeFilename(filename);
            if (IsAscii(normalized))
            {
                return seenByAsciiFilename.Add((username, normalized, file.Size));
            }

            seenByUnicodeFilename ??= new HashSet<(string Username, string NormalizedFilename, long Size)>();
            return seenByUnicodeFilename.Add((username, normalized, file.Size));
        }

        foreach (var r in soulseekResponses.Concat(meshResponses))
        {
            var username = r.Username ?? string.Empty;
            var keptFiles = new List<File>();
            var keptLocked = new List<File>();

            // Process regular files
            if (r.Files != null)
            {
                foreach (var f in r.Files)
                {
                    if (AddFilename(username, f))
                    {
                        keptFiles.Add(f);
                    }
                }
            }

            // Process locked files
            if (r.LockedFiles != null)
            {
                foreach (var f in r.LockedFiles)
                {
                    if (AddFilename(username, f))
                    {
                        keptLocked.Add(f);
                    }
                }
            }

            if (keptFiles.Count > 0 || keptLocked.Count > 0)
            {
                merged.Add(new Response
                {
                    Username = r.Username ?? string.Empty,
                    Token = r.Token,
                    HasFreeUploadSlot = r.HasFreeUploadSlot,
                    UploadSpeed = r.UploadSpeed,
                    QueueLength = r.QueueLength,
                    FileCount = keptFiles.Count,
                    Files = keptFiles,
                    LockedFileCount = keptLocked.Count,
                    LockedFiles = keptLocked,
                    SourceProviders = r.SourceProviders,
                    PrimarySource = r.PrimarySource,
                    PodContentRef = r.PodContentRef,
                    SceneContentRef = r.SceneContentRef,
                });
            }
        }

        return merged;
    }

    private static string NormalizeFilename(string filename)
    {
        return filename.ToLowerInvariant()
            .Replace('\\', '/')
            .Trim();
    }

    private static bool IsAscii(string value)
    {
        foreach (var character in value)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class AsciiFilenameKeyComparer : IEqualityComparer<(string Username, string Filename, long Size)>
    {
        public static AsciiFilenameKeyComparer Instance { get; } = new();

        public bool Equals(
            (string Username, string Filename, long Size) x,
            (string Username, string Filename, long Size) y)
        {
            if (x.Size != y.Size || !string.Equals(x.Username, y.Username, StringComparison.Ordinal))
            {
                return false;
            }

            var xFilename = TrimAscii(x.Filename);
            var yFilename = TrimAscii(y.Filename);
            if (xFilename.Length != yFilename.Length)
            {
                return false;
            }

            for (var index = 0; index < xFilename.Length; index++)
            {
                if (NormalizeAsciiCharacter(xFilename[index]) != NormalizeAsciiCharacter(yFilename[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode((string Username, string Filename, long Size) obj)
        {
            HashCode hashCode = default;
            hashCode.Add(obj.Username, StringComparer.Ordinal);
            hashCode.Add(obj.Size);
            foreach (var character in TrimAscii(obj.Filename))
            {
                hashCode.Add(NormalizeAsciiCharacter(character));
            }

            return hashCode.ToHashCode();
        }

        private static ReadOnlySpan<char> TrimAscii(string value)
        {
            var start = 0;
            while (start < value.Length && char.IsWhiteSpace(value[start]))
            {
                start++;
            }

            var end = value.Length;
            while (end > start && char.IsWhiteSpace(value[end - 1]))
            {
                end--;
            }

            return value.AsSpan(start, end - start);
        }

        private static char NormalizeAsciiCharacter(char character)
        {
            if (character == '\\')
            {
                return '/';
            }

            return character is >= 'A' and <= 'Z'
                ? (char)(character + ('a' - 'A'))
                : character;
        }
    }
}
