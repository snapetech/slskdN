// <copyright file="ConfigurationCompatibilityWarningsTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd.Tests.Unit.Configuration;

using slskd.Configuration;
using Xunit;

public class ConfigurationCompatibilityWarningsTests
{
    [Fact]
    public void GetWarnings_LegacyKeys_ReturnsMigrationWarnings()
    {
        var file = WriteConfig("""
global:
  download:
    retry:
      max_delay: 1000
groups:
  trusted:
    limits:
      upload:
        slots: 1
integration:
  lidarr:
    enabled: false
transfers:
  limits:
    upload:
      slots: 2
""");

        try
        {
            var options = new slskd.Options
            {
                Global = new slskd.Options.GlobalOptions
                {
                    Download = new slskd.Options.GlobalOptions.GlobalDownloadOptions
                    {
                        Retry = new slskd.Options.GlobalOptions.GlobalDownloadOptions.DownloadRetryOptions
                        {
                            MaxDelay = 1000,
                        },
                    },
                },
            };

            var warnings = ConfigurationCompatibilityWarnings.GetWarnings(file, options);

            Assert.Contains(warnings, warning => warning.Contains("'global'", StringComparison.Ordinal));
            Assert.Contains(warnings, warning => warning.Contains("'groups'", StringComparison.Ordinal));
            Assert.Contains(warnings, warning => warning.Contains("'transfers.limits'", StringComparison.Ordinal));
            Assert.Contains(warnings, warning => warning.Contains("'integration'", StringComparison.Ordinal));
            Assert.Contains(warnings, warning => warning.Contains("Group-level 'limits'", StringComparison.Ordinal));
            Assert.Contains(warnings, warning => warning.Contains("Download retry max_delay", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void GetWarnings_CanonicalKeys_DoNotEmitLegacyWarnings()
    {
        var file = WriteConfig("""
transfers:
  groups:
    trusted:
      upload:
        limits:
          slots: 1
  upload:
    limits:
      slots: 2
integrations:
  lidarr:
    enabled: false
""");

        try
        {
            var options = new slskd.Options();

            var warnings = ConfigurationCompatibilityWarnings.GetWarnings(file, options);

            Assert.DoesNotContain(warnings, warning => warning.Contains("'global'", StringComparison.Ordinal));
            Assert.DoesNotContain(warnings, warning => warning.Contains("'groups'", StringComparison.Ordinal));
            Assert.DoesNotContain(warnings, warning => warning.Contains("'transfers.limits'", StringComparison.Ordinal));
            Assert.DoesNotContain(warnings, warning => warning.Contains("'integration'", StringComparison.Ordinal));
            Assert.DoesNotContain(warnings, warning => warning.Contains("Group-level 'limits'", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static string WriteConfig(string yaml)
    {
        var file = Path.Combine(Path.GetTempPath(), $"slskdn-config-compat-{Guid.NewGuid():N}.yml");
        File.WriteAllText(file, yaml);
        return file;
    }
}
