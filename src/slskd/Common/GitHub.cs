// <copyright file="GitHub.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

// <copyright file="GitHub.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.RegularExpressions;
    using System.Text.Json;
    using System.Threading.Tasks;

    public static class GitHub
    {
        private static readonly Regex SlskdnReleaseVersionPattern = new(
            @"^(?:(?<base>\d+\.\d+\.\d+)|(?<date>\d{8,10}))-slskdn\.(?<sequence>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public sealed record ReleaseInfo(string TagName, string Version, string HtmlUrl);

        public sealed record VersionCheckResult(
            string Current,
            string Full,
            string Latest,
            string LatestTag,
            string LatestUrl,
            bool IsUpdateAvailable,
            DateTimeOffset CheckedAt);

        public static async Task<Version> GetLatestReleaseVersion(string organization, string repository, string userAgent)
        {
            var release = await GetLatestReleaseInfo(organization, repository, userAgent).ConfigureAwait(false);
            if (!Version.TryParse(release.Version, out var version))
            {
                throw new GitHubException($"GitHub returned an unparsable tag '{release.TagName}'");
            }

            return version;
        }

        public static async Task<ReleaseInfo> GetLatestReleaseInfo(string organization, string repository, string userAgent)
        {
            var url = $"https://api.github.com/repos/{organization}/{repository}/releases/latest";

            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using var http = new HttpClient(handler, disposeHandler: true);
                http.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

                var response = await http.GetFromJsonAsync<JsonDocument>(url)
                    ?? throw new GitHubException("GitHub returned an empty response");
                var tagName = response.RootElement.GetProperty("tag_name").GetString();

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    throw new GitHubException("GitHub returned a release without a tag_name");
                }

                var htmlUrl = response.RootElement.TryGetProperty("html_url", out var htmlUrlElement)
                    ? htmlUrlElement.GetString() ?? string.Empty
                    : string.Empty;

                return new ReleaseInfo(tagName, NormalizeReleaseVersion(tagName), htmlUrl);
            }
            catch (Exception ex)
            {
                throw new GitHubException($"Failed to retrieve latest release version from GitHub: {ex.Message}", ex);
            }
        }

        public static VersionCheckResult CreateVersionCheckResult(ReleaseInfo latestRelease, DateTimeOffset checkedAt)
        {
            var current = NormalizeReleaseVersion(Program.SemanticVersion);
            var latest = NormalizeReleaseVersion(latestRelease.Version);
            var isUpdateAvailable = IsNewerVersionAvailable(current, latest);

            return new VersionCheckResult(
                Current: Program.SemanticVersion,
                Full: Program.FullVersion,
                Latest: latest,
                LatestTag: latestRelease.TagName,
                LatestUrl: latestRelease.HtmlUrl,
                IsUpdateAvailable: isUpdateAvailable,
                CheckedAt: checkedAt);
        }

        public static bool IsNewerVersionAvailable(string currentVersion, string latestVersion)
        {
            var current = NormalizeReleaseVersion(currentVersion);
            var latest = NormalizeReleaseVersion(latestVersion);

            if (string.IsNullOrWhiteSpace(latest))
            {
                return false;
            }

            if (string.Equals(current, latest, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var currentSlskdn = TryParseSlskdnRelease(current);
            var latestSlskdn = TryParseSlskdnRelease(latest);
            if (currentSlskdn.HasValue && latestSlskdn.HasValue)
            {
                return latestSlskdn.Value.CompareTo(currentSlskdn.Value) > 0;
            }

            if (Version.TryParse(current, out var currentVersionValue) &&
                Version.TryParse(latest, out var latestVersionValue))
            {
                return latestVersionValue > currentVersionValue;
            }

            // Manual and development builds do not map cleanly to release tags. If GitHub has
            // a different latest release, surface it so operators know a packaged build exists.
            return !string.IsNullOrWhiteSpace(current);
        }

        public static string NormalizeReleaseVersion(string versionOrTag)
        {
            var normalized = (versionOrTag ?? string.Empty).Trim();
            if (normalized.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized["refs/tags/".Length..];
            }

            if (normalized.StartsWith("build-main-", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized["build-main-".Length..];
            }
            else if (normalized.StartsWith("build-dev-", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized["build-dev-".Length..];
            }
            else if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[1..];
            }

            var buildMetadataIndex = normalized.IndexOf('+');
            if (buildMetadataIndex >= 0)
            {
                normalized = normalized[..buildMetadataIndex];
            }

            return normalized;
        }

        private static (long ReleaseLine, int Sequence)? TryParseSlskdnRelease(string version)
        {
            var match = SlskdnReleaseVersionPattern.Match(version);
            if (!match.Success)
            {
                return null;
            }

            var releaseLineText = match.Groups["date"].Success
                ? match.Groups["date"].Value
                : match.Groups["base"].Value.Replace(".", string.Empty, StringComparison.Ordinal);

            if (!long.TryParse(releaseLineText, out var releaseLine) ||
                !int.TryParse(match.Groups["sequence"].Value, out var sequence))
            {
                return null;
            }

            return (releaseLine, sequence);
        }
    }
}
