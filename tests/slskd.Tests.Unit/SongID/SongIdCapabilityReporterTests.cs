// <copyright file="SongIdCapabilityReporterTests.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Tests.Unit.SongID;

using slskd.SongID;
using Xunit;

public sealed class SongIdCapabilityReporterTests
{
    [Fact]
    public async Task GetCapabilities_ReportsConfiguredAndToolBackedCapabilities()
    {
        var options = new slskd.Options
        {
            Integration = new slskd.Options.IntegrationOptions
            {
                Chromaprint = new slskd.Options.IntegrationOptions.ChromaprintOptions
                {
                    Enabled = true,
                },
                AcoustId = new slskd.Options.IntegrationOptions.AcoustIdOptions
                {
                    Enabled = true,
                    ClientId = "client-id",
                },
            },
        };
        var availableCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ffmpeg",
            "yt-dlp",
            "songrec",
            "whisper",
        };
        var existingFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "/usr/share/java/panako.jar",
        };
        var reporter = new SongIdCapabilityReporter(
            new TestOptionsMonitor<slskd.Options>(options),
            (command, _) => Task.FromResult(availableCommands.Contains(command)),
            existingFiles.Contains);

        var capabilities = await reporter.GetCapabilitiesAsync(CancellationToken.None);

        AssertCapability(capabilities, "musicbrainz_lookup", available: true, status: "experimental");
        AssertCapability(capabilities, "chromaprint_fingerprint", available: true, status: "experimental");
        AssertCapability(capabilities, "acoustid_lookup", available: true, status: "experimental");
        AssertCapability(capabilities, "youtube_audio", available: true, status: "experimental");
        AssertCapability(capabilities, "songrec", available: true, status: "experimental");
        AssertCapability(capabilities, "panako", available: true, status: "experimental");
        AssertCapability(capabilities, "ocr_frames", available: false, status: "experimental");
        AssertCapability(capabilities, "hash_from_audio_file_flag", available: false, status: "broken");
    }

    [Fact]
    public async Task GetCapabilities_DoesNotAdvertiseUnconfiguredFingerprintProvidersAsAvailable()
    {
        var reporter = new SongIdCapabilityReporter(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            (_, _) => Task.FromResult(false),
            _ => false);

        var capabilities = await reporter.GetCapabilitiesAsync(CancellationToken.None);

        AssertCapability(capabilities, "text_query", available: true, status: "stable");
        AssertCapability(capabilities, "chromaprint_fingerprint", available: false, status: "experimental");
        AssertCapability(capabilities, "acoustid_lookup", available: false, status: "experimental");
        AssertCapability(capabilities, "youtube_metadata", available: false, status: "experimental");
        AssertCapability(capabilities, "demucs", available: false, status: "experimental");
        AssertCapability(capabilities, "whisper_transcripts", available: false, status: "experimental");
    }

    [Fact]
    public async Task GetCapabilities_ReportsDockerGuidanceForMissingExperimentalMediaTools()
    {
        var reporter = new SongIdCapabilityReporter(
            new TestOptionsMonitor<slskd.Options>(new slskd.Options()),
            (_, _) => Task.FromResult(false),
            _ => false);

        var capabilities = await reporter.GetCapabilitiesAsync(CancellationToken.None);

        AssertReasonContains(capabilities, "songrec", "experimental media image");
        AssertReasonContains(capabilities, "panako", "derived experimental media image");
        AssertReasonContains(capabilities, "audfprint", "derived experimental media image");
        AssertReasonContains(capabilities, "demucs", "experimental media image");
        AssertReasonContains(capabilities, "whisper_transcripts", "experimental media image");
        AssertReasonContains(capabilities, "ocr_frames", "experimental media image");
        AssertReasonContains(capabilities, "c2pa_provenance", "experimental media image");
    }

    private static void AssertCapability(
        IReadOnlyList<SongIdCapability> capabilities,
        string id,
        bool available,
        string status)
    {
        var capability = Assert.Single(capabilities, item => item.Id == id);
        Assert.Equal(available, capability.Available);
        Assert.Equal(status, capability.Status);
        Assert.False(string.IsNullOrWhiteSpace(capability.Reason));
    }

    private static void AssertReasonContains(
        IReadOnlyList<SongIdCapability> capabilities,
        string id,
        string expected)
    {
        var capability = Assert.Single(capabilities, item => item.Id == id);
        Assert.Contains(expected, capability.Reason, StringComparison.Ordinal);
    }
}
