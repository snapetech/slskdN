// <copyright file="ConfigurationCompatibilityWarnings.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ConfigurationCompatibilityWarnings
{
    private const int MinimumRetryMaxDelayMilliseconds = 30_000;

    public static IReadOnlyList<string> GetWarnings(string configurationFile, Options options)
    {
        if (!File.Exists(configurationFile))
        {
            return Array.Empty<string>();
        }

        var warnings = new List<string>();
        var lines = File.ReadAllLines(configurationFile);
        var hasCanonicalIntegrations = HasTopLevelKey(lines, "integrations");
        var hasCanonicalTransferGroups = HasDirectChildKey(lines, "transfers", "groups");
        var hasCanonicalUploadLimits = HasNestedChildKey(lines, new[] { "transfers", "upload" }, "limits");

        if (HasTopLevelKey(lines, "global"))
        {
            warnings.Add("Configuration key 'global' is deprecated; slskdN accepts it for now, but 'transfers' is the canonical transfer-rate and retry section.");
        }

        if (HasTopLevelKey(lines, "groups") && !hasCanonicalTransferGroups)
        {
            warnings.Add("Top-level configuration key 'groups' is accepted for compatibility; new configuration should place groups under 'transfers.groups'.");
        }

        if (HasDirectChildKey(lines, "transfers", "limits") && !hasCanonicalUploadLimits)
        {
            warnings.Add("Configuration key 'transfers.limits' is accepted for compatibility; new configuration should place global upload limits under 'transfers.upload.limits'.");
        }

        if (HasTopLevelKey(lines, "integration") && !hasCanonicalIntegrations)
        {
            warnings.Add("Configuration key 'integration' is deprecated; slskdN accepts it for now, but 'integrations' is the canonical external integration section.");
        }

        if (HasGroupLevelLimits(lines))
        {
            warnings.Add("Group-level 'limits' entries are accepted for compatibility; place them under each group's 'upload' section in new configuration files.");
        }

        if (options.Global.Download.Retry.MaxDelay < MinimumRetryMaxDelayMilliseconds)
        {
            warnings.Add($"Download retry max_delay is below {MinimumRetryMaxDelayMilliseconds}ms; slskdN will clamp retry scheduling to that floor.");
        }

        return warnings.AsReadOnly();
    }

    private static bool HasTopLevelKey(IEnumerable<string> lines, string key)
    {
        var prefix = $"{key}:";
        return lines
            .Select(StripYamlComment)
            .Any(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDirectChildKey(IEnumerable<string> lines, string parentKey, string childKey)
        => HasNestedChildKey(lines, new[] { parentKey }, childKey);

    private static bool HasNestedChildKey(IEnumerable<string> lines, IReadOnlyList<string> parentPath, string childKey)
    {
        var matchedDepth = 0;
        var matchedIndents = new List<int>();
        var childIndent = -1;

        foreach (var rawLine in lines.Select(StripYamlComment))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
            var trimmed = rawLine.TrimStart();

            while (matchedDepth > 0 && indent <= matchedIndents[matchedDepth - 1])
            {
                matchedDepth--;
                matchedIndents.RemoveAt(matchedIndents.Count - 1);
                childIndent = -1;
            }

            if (matchedDepth < parentPath.Count &&
                trimmed.StartsWith($"{parentPath[matchedDepth]}:", StringComparison.OrdinalIgnoreCase))
            {
                matchedDepth++;
                matchedIndents.Add(indent);
                childIndent = -1;
                continue;
            }

            if (matchedDepth != parentPath.Count)
            {
                continue;
            }

            if (childIndent < 0)
            {
                childIndent = indent;
            }

            if (indent == childIndent && trimmed.StartsWith($"{childKey}:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasGroupLevelLimits(IEnumerable<string> lines)
    {
        var inGroups = false;
        var groupsIndent = 0;
        var groupIndent = 0;

        foreach (var rawLine in lines.Select(StripYamlComment))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
            var trimmed = rawLine.TrimStart();

            if (indent == 0)
            {
                inGroups = trimmed.StartsWith("groups:", StringComparison.OrdinalIgnoreCase);
                groupsIndent = 0;
                groupIndent = 0;
                continue;
            }

            if (!inGroups)
            {
                continue;
            }

            if (indent <= groupsIndent)
            {
                inGroups = false;
                continue;
            }

            if (groupIndent == 0 && trimmed.EndsWith(":", StringComparison.Ordinal))
            {
                groupIndent = indent;
                continue;
            }

            if (groupIndent > 0 && indent == groupIndent + 2 && trimmed.StartsWith("limits:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripYamlComment(string line)
    {
        var index = line.IndexOf('#');
        return index >= 0 ? line[..index].TrimEnd() : line.TrimEnd();
    }
}
