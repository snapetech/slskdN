// <copyright file="SongIdCapabilities.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.SongID;

using Microsoft.Extensions.Options;

public interface ISongIdCapabilityReporter
{
    Task<IReadOnlyList<SongIdCapability>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}

public sealed class SongIdCapability
{
    public string Id { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Status { get; init; } = "experimental";

    public bool Available { get; init; }

    public string Reason { get; init; } = string.Empty;

    public IReadOnlyList<string> Requirements { get; init; } = Array.Empty<string>();
}

public sealed class SongIdCapabilityReporter : ISongIdCapabilityReporter
{
    private const string DockerExperimentalMediaHint = "For Docker, use the experimental media image or install the tool in a derived image and keep it on PATH.";

    private readonly IOptionsMonitor<slskd.Options> options;
    private readonly Func<string, CancellationToken, Task<bool>> commandExists;
    private readonly Func<string, bool> fileExists;

    public SongIdCapabilityReporter(IOptionsMonitor<slskd.Options> options)
        : this(options, CommandExistsAsync, File.Exists)
    {
    }

    internal SongIdCapabilityReporter(
        IOptionsMonitor<slskd.Options> options,
        Func<string, CancellationToken, Task<bool>> commandExists,
        Func<string, bool> fileExists)
    {
        this.options = options;
        this.commandExists = commandExists;
        this.fileExists = fileExists;
    }

    public async Task<IReadOnlyList<SongIdCapability>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var currentOptions = options.CurrentValue;
        var ytDlp = await commandExists("yt-dlp", cancellationToken).ConfigureAwait(false);
        var ffmpeg = await commandExists("ffmpeg", cancellationToken).ConfigureAwait(false);
        var songrec = await commandExists("songrec", cancellationToken).ConfigureAwait(false);
        var whisper = await commandExists("whisper", cancellationToken).ConfigureAwait(false);
        var tesseract = await commandExists("tesseract", cancellationToken).ConfigureAwait(false);
        var demucs = await commandExists("demucs", cancellationToken).ConfigureAwait(false);
        var c2paTool = await commandExists("c2patool", cancellationToken).ConfigureAwait(false);
        var panakoJar = FindPanakoJar();
        var audfprint = FindAudfprintScript();
        var chromaprintConfigured = currentOptions.Integration.Chromaprint.Enabled;
        var acoustIdConfigured = currentOptions.Integration.AcoustId.Enabled &&
            !string.IsNullOrWhiteSpace(currentOptions.Integration.AcoustId.ClientId);
        var musicBrainzConfigured = Uri.TryCreate(currentOptions.Integration.MusicBrainz.BaseUrl, UriKind.Absolute, out _);

        return new[]
        {
            Capability("text_query", "Text query analysis", "stable", true, "Text searches do not require external tools."),
            Capability("url_parsing", "YouTube and Spotify URL parsing", "experimental", true, "URL classification and metadata fallback are implemented."),
            Capability("musicbrainz_lookup", "MusicBrainz lookup", "experimental", musicBrainzConfigured, musicBrainzConfigured ? "MusicBrainz client is configured." : "MusicBrainz base URL is not valid.", "musicbrainz base URL"),
            Capability("spotify_page_metadata", "Spotify page metadata", "experimental", true, "Spotify page metadata fetch is implemented for public pages."),
            Capability("youtube_metadata", "YouTube metadata", "experimental", ytDlp, ytDlp ? "yt-dlp is available." : "yt-dlp was not found on PATH.", "yt-dlp"),
            Capability("youtube_audio", "YouTube audio extraction", "experimental", ytDlp && ffmpeg, ytDlp && ffmpeg ? "yt-dlp and ffmpeg are available." : "Requires yt-dlp and ffmpeg.", "yt-dlp", "ffmpeg"),
            Capability("local_file_intake", "Local file intake", "experimental", true, "Local files are accepted when they are under configured safe directories."),
            Capability("chromaprint_fingerprint", "Chromaprint fingerprinting", "experimental", chromaprintConfigured && ffmpeg, chromaprintConfigured && ffmpeg ? "Chromaprint is enabled and ffmpeg is available." : "Requires integration.chromaprint.enabled and ffmpeg.", "integration.chromaprint.enabled", "ffmpeg", "libchromaprint"),
            Capability("acoustid_lookup", "AcoustID lookup", "experimental", acoustIdConfigured, acoustIdConfigured ? "AcoustID is enabled with a client id." : "Requires integration.acoustid.enabled and a client id.", "integration.acoustid.enabled", "integration.acoustid.client_id"),
            Capability("songrec", "SongRec recognition", "experimental", songrec, songrec ? "songrec is available." : MissingCommandReason("songrec"), "songrec"),
            Capability("panako", "Panako local corpus matching", "experimental", !string.IsNullOrWhiteSpace(panakoJar), !string.IsNullOrWhiteSpace(panakoJar) ? $"Panako jar found at {panakoJar}." : "panako.jar was not found in known locations or PATH. For Docker, mount panako.jar at /usr/share/java/panako.jar or install it in a derived experimental media image.", "java", "panako.jar"),
            Capability("audfprint", "Audfprint local corpus matching", "experimental", !string.IsNullOrWhiteSpace(audfprint), !string.IsNullOrWhiteSpace(audfprint) ? $"Audfprint script found at {audfprint}." : "audfprint.py was not found in known locations or PATH. For Docker, mount audfprint.py at /slskd/external/audfprint/audfprint.py or install it in a derived experimental media image.", "python", "audfprint.py"),
            Capability("demucs", "Demucs stem extraction", "experimental", demucs && ffmpeg, demucs && ffmpeg ? "demucs and ffmpeg are available." : MissingCommandReason("demucs", commandMissing: !demucs, ffmpegMissing: !ffmpeg), "demucs", "ffmpeg"),
            Capability("whisper_transcripts", "Whisper transcript extraction", "experimental", whisper && ffmpeg, whisper && ffmpeg ? "whisper and ffmpeg are available." : MissingCommandReason("whisper", commandMissing: !whisper, ffmpegMissing: !ffmpeg), "whisper", "ffmpeg"),
            Capability("ocr_frames", "OCR frame scanning", "experimental", tesseract && ffmpeg, tesseract && ffmpeg ? "tesseract and ffmpeg are available." : MissingCommandReason("tesseract", commandMissing: !tesseract, ffmpegMissing: !ffmpeg), "tesseract", "ffmpeg"),
            Capability("comments_and_chapters", "YouTube comments and chapters", "experimental", ytDlp, ytDlp ? "yt-dlp is available." : "Requires yt-dlp.", "yt-dlp"),
            Capability("c2pa_provenance", "C2PA provenance signal detection", "experimental", c2paTool, c2paTool ? "c2patool is available." : MissingCommandReason("c2patool"), "c2patool"),
            Capability("hash_from_audio_file_flag", "HashFromAudioFileEnabled flag", "broken", false, "Startup validation rejects this flag because PCM extraction support for that hardening path is unavailable."),
        };
    }

    private static string MissingCommandReason(string command, bool commandMissing = true, bool ffmpegMissing = false)
    {
        var missing = (commandMissing, ffmpegMissing) switch
        {
            (true, true) => $"{command} and ffmpeg were not found on PATH.",
            (true, false) => $"{command} was not found on PATH.",
            (false, true) => "ffmpeg was not found on PATH.",
            _ => "Required tools were not found on PATH.",
        };

        return $"{missing} {DockerExperimentalMediaHint}";
    }

    private static SongIdCapability Capability(
        string id,
        string label,
        string status,
        bool available,
        string reason,
        params string[] requirements)
        => new()
        {
            Id = id,
            Label = label,
            Status = status,
            Available = available,
            Reason = reason,
            Requirements = requirements,
        };

    private string? FindPanakoJar()
    {
        var candidates = new[]
        {
            "/usr/share/java/panako.jar",
            "/usr/local/share/java/panako.jar",
            "/usr/share/panako/panako.jar",
            "/usr/local/share/panako/panako.jar",
        };

        return candidates.FirstOrDefault(fileExists) ?? FindOnPath("panako.jar");
    }

    private string? FindAudfprintScript()
    {
        var candidates = new[]
        {
            "/usr/share/audfprint/audfprint.py",
            "/usr/local/share/audfprint/audfprint.py",
            "/opt/audfprint/audfprint.py",
            Path.Combine(Program.AppDirectory, "external", "audfprint", "audfprint.py"),
            Path.Combine(Program.AppDirectory, "audfprint", "audfprint.py"),
        };

        return candidates.FirstOrDefault(fileExists) ?? FindOnPath("audfprint.py");
    }

    private string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, fileName);
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<bool> CommandExistsAsync(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
