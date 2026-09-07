// <copyright file="WishlistSearchPolicy.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Search;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Soulseek;

/// <summary>
///     Applies the canonical file contract for a Wishlist search.
/// </summary>
internal static class WishlistSearchPolicy
{
    private static readonly Regex QueryTokenPattern = new("-?\"[^\"]+\"|\\S+", RegexOptions.Compiled);

    /// <summary>
    ///     Creates the filter used by Soulseek and bridged providers. Every
    ///     positive query clause must be present in a filename; exclusions
    ///     must not be present.
    /// </summary>
    public static Func<Soulseek.File, bool> CreateSoulseekFileFilter(
        string searchText,
        Func<Soulseek.File, bool>? additionalFilter = null)
    {
        var filenameFilter = CreateFilenameFilter(searchText);
        return file => filenameFilter(file.Filename) && (additionalFilter?.Invoke(file) ?? true);
    }

    /// <summary>
    ///     Creates the same filter for slskd's persisted search response model.
    /// </summary>
    public static Func<File, bool> CreateResultFileFilter(
        string searchText,
        Func<File, bool>? additionalFilter = null)
    {
        var filenameFilter = CreateFilenameFilter(searchText);
        return file => filenameFilter(file.Filename) && (additionalFilter?.Invoke(file) ?? true);
    }

    /// <summary>
    ///     Returns only responses that still contain files satisfying the
    ///     Wishlist policy. This protects mesh results and any provider that
    ///     did not apply the Soulseek file filter itself.
    /// </summary>
    public static List<Response> FilterResponses(
        IEnumerable<Response> responses,
        string searchText,
        Func<Soulseek.File, bool>? additionalFilter = null)
    {
        Func<File, bool>? resultAdditionalFilter = additionalFilter == null
            ? null
            : file => additionalFilter(ToSoulseekFile(file));
        var fileFilter = CreateResultFileFilter(searchText, resultAdditionalFilter);
        var filtered = new List<Response>();

        foreach (var response in responses)
        {
            var files = response.Files.Where(fileFilter).ToList();
            var lockedFiles = response.LockedFiles.Where(fileFilter).ToList();
            if (files.Count == 0 && lockedFiles.Count == 0)
            {
                continue;
            }

            filtered.Add(new Response
            {
                FileCount = files.Count,
                Files = files,
                HasFreeUploadSlot = response.HasFreeUploadSlot,
                LockedFileCount = lockedFiles.Count,
                LockedFiles = lockedFiles,
                PodContentRef = response.PodContentRef,
                PrimarySource = response.PrimarySource,
                QueueLength = response.QueueLength,
                SceneContentRef = response.SceneContentRef,
                SourceProviders = response.SourceProviders.ToList(),
                Token = response.Token,
                UploadSpeed = response.UploadSpeed,
                Username = response.Username,
            });
        }

        return filtered;
    }

    /// <summary>
    ///     Creates a filename-only predicate for use by provider requests.
    /// </summary>
    public static Func<string, bool> CreateFilenameFilter(string searchText)
    {
        var clauses = ParseQuery(searchText);
        return filename =>
        {
            var normalizedFilename = Normalize(filename);
            if (clauses.Exclusions.Any(normalizedFilename.Contains))
            {
                return false;
            }

            return clauses.PositiveClauses.Any(clause =>
                clause.Count == 0 || clause.All(normalizedFilename.Contains));
        };
    }

    private static ParsedQuery ParseQuery(string searchText)
    {
        var clauses = new List<List<string>>();
        var currentClause = new List<string>();
        var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in QueryTokenPattern.Matches(searchText ?? string.Empty))
        {
            var raw = match.Value;
            if (raw.Equals("OR", StringComparison.OrdinalIgnoreCase))
            {
                AddClause();
                continue;
            }

            var excluded = raw.StartsWith("-", StringComparison.Ordinal) && raw.Length > 1;
            var term = (excluded ? raw[1..] : raw).Trim().Trim('"').Trim();
            if (term.Length == 0)
            {
                continue;
            }

            var normalizedTerm = Normalize(term);
            if (excluded)
            {
                exclusions.Add(normalizedTerm);
            }
            else
            {
                currentClause.Add(normalizedTerm);
            }
        }

        AddClause();
        if (clauses.Count == 0)
        {
            clauses.Add([]);
        }

        return new ParsedQuery(clauses, exclusions);

        void AddClause()
        {
            if (currentClause.Count > 0)
            {
                clauses.Add(currentClause);
                currentClause = new List<string>();
            }
        }
    }

    private static string Normalize(string value) =>
        (value ?? string.Empty).Replace('\\', '/').Trim().ToLowerInvariant();

    private static Soulseek.File ToSoulseekFile(File file)
    {
        var attributes = new List<FileAttribute>();
        if (file.BitRate.HasValue)
        {
            attributes.Add(new FileAttribute(FileAttributeType.BitRate, file.BitRate.Value));
        }

        if (file.Length.HasValue)
        {
            attributes.Add(new FileAttribute(FileAttributeType.Length, file.Length.Value));
        }

        if (file.SampleRate.HasValue)
        {
            attributes.Add(new FileAttribute(FileAttributeType.SampleRate, file.SampleRate.Value));
        }

        if (file.BitDepth.HasValue)
        {
            attributes.Add(new FileAttribute(FileAttributeType.BitDepth, file.BitDepth.Value));
        }

        if (file.IsVariableBitRate.HasValue)
        {
            attributes.Add(new FileAttribute(
                FileAttributeType.VariableBitRate,
                file.IsVariableBitRate.Value ? 1 : 0));
        }

        return new Soulseek.File(file.Code, file.Filename, file.Size, file.Extension, attributes);
    }

    private sealed record ParsedQuery(
        IReadOnlyList<IReadOnlyList<string>> PositiveClauses,
        IReadOnlySet<string> Exclusions);
}
