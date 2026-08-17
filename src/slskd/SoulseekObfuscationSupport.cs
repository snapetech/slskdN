// <copyright file="SoulseekObfuscationSupport.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>

namespace slskd;

/// <summary>
///     Builds the first-class runtime plan for Soulseek type-1 peer, distributed, and transfer obfuscation.
/// </summary>
public static class SoulseekObfuscationSupport
{
    /// <summary>
    ///     Soulseek public-server obfuscation metadata type for rotated message streams.
    /// </summary>
    public const int Type1 = 1;

    /// <summary>
    ///     Indicates whether the current Soulseek.NET runtime exposes the type-1 listener and dialer hooks.
    /// </summary>
    public static bool RuntimeSupportsType1PeerDistributedAndTransfers => true;

    /// <summary>
    ///     Gets the Soulseek connection types currently supported by type-1 obfuscation.
    /// </summary>
    public static IReadOnlyList<string> SupportedConnectionTypes { get; } = ["P", "D", "F"];

    /// <summary>
    ///     Build a serializable runtime plan for configuration, diagnostics, and the web UI.
    /// </summary>
    /// <param name="soulseek">Soulseek options.</param>
    /// <returns>A runtime plan describing the requested posture and current activation state.</returns>
    public static SoulseekObfuscationPlan BuildPlan(Options.SoulseekOptions soulseek)
    {
        var options = soulseek.Obfuscation;
        var mode = Enum.TryParse<SoulseekObfuscationMode>(options.Mode, ignoreCase: true, out var parsedMode)
            ? parsedMode
            : SoulseekObfuscationMode.Compatibility;

        // a configured obfuscation listen port of 0 means "share the regular listen port" rather than binding a
        // second, dedicated port: the shared listener determines plain-vs-obfuscated per accepted connection by
        // inspecting the first bytes read from the socket. in that case the port obfuscated peers are actually
        // reachable on is the same as the regular listen port.
        var requestedListenPort = options.ListenPort > 0 ? options.ListenPort : (int?)null;
        var shared = requestedListenPort is null;
        var effectiveListenPort = requestedListenPort ?? soulseek.ListenPort;
        var runtimeState = options.Enabled ? "active" : "disabled";

        var limitations = new List<string>();

        limitations.Add("Type-1 obfuscation is a compatibility/privacy posture, not transport security or meaningful encryption.");
        limitations.Add("Current runtime support covers peer-message (P), distributed-message (D), and file-transfer (F) streams.");
        limitations.Add("Regular transfer paths remain advertised and available for legacy-client compatibility.");

        if (shared)
        {
            limitations.Add("Obfuscated connections share the regular listen port; plain and obfuscated init frames are distinguished per-connection instead of using a dedicated port.");
        }

        if (mode == SoulseekObfuscationMode.Only)
        {
            limitations.Add("Only mode is not supported by the current runtime because slskdN keeps regular Soulseek paths advertised for legacy compatibility.");
        }

        return new SoulseekObfuscationPlan(
            Enabled: options.Enabled,
            Mode: mode.ToString().ToLowerInvariant(),
            Type: Type1,
            RegularListenPort: soulseek.ListenPort,
            RequestedListenPort: requestedListenPort,
            EffectiveListenPort: effectiveListenPort,
            Shared: shared,
            AdvertiseRegularPort: options.AdvertiseRegularPort,
            PreferOutbound: mode == SoulseekObfuscationMode.Prefer && options.PreferOutbound,
            SupportedConnectionTypes: SupportedConnectionTypes,
            RuntimeSupported: RuntimeSupportsType1PeerDistributedAndTransfers,
            RuntimeState: runtimeState,
            Summary: BuildSummary(options.Enabled, mode),
            Limitations: limitations);
    }

    /// <summary>
    ///     Build runtime options for the Soulseek client.
    /// </summary>
    /// <param name="soulseek">Soulseek options.</param>
    /// <returns>Runtime peer obfuscation options.</returns>
    public static Soulseek.PeerObfuscationOptions BuildRuntimeOptions(Options.SoulseekOptions soulseek)
    {
        var plan = BuildPlan(soulseek);

        return new Soulseek.PeerObfuscationOptions(
            enabled: plan.Enabled && plan.RuntimeSupported,
            listenPort: plan.RequestedListenPort ?? 0,
            type: plan.Type,
            advertiseRegularPort: plan.AdvertiseRegularPort,
            preferOutbound: plan.PreferOutbound);
    }

    private static string BuildSummary(bool enabled, SoulseekObfuscationMode mode)
    {
        if (!enabled)
        {
            return "Soulseek type-1 peer/distributed/transfer obfuscation is disabled.";
        }

        var posture = mode switch
        {
            SoulseekObfuscationMode.Compatibility => "Compatibility mode keeps regular outbound peer/distributed/transfer dials first and adds obfuscated reachability.",
            SoulseekObfuscationMode.Prefer => "Prefer mode uses obfuscated peer/distributed/transfer dials when peers advertise type-1 metadata and keeps regular fallback.",
            SoulseekObfuscationMode.Only => "Only mode is not currently supported; the runtime keeps regular Soulseek fallback for legacy compatibility.",
            _ => "Soulseek type-1 peer/distributed/transfer obfuscation is configured.",
        };

        return posture;
    }
}

/// <summary>
///     Serializable Soulseek type-1 obfuscation runtime plan.
/// </summary>
/// <param name="Enabled">Whether the feature option is enabled.</param>
/// <param name="Mode">Configured posture.</param>
/// <param name="Type">Soulseek obfuscation type.</param>
/// <param name="RegularListenPort">Regular peer-message listen port.</param>
/// <param name="RequestedListenPort">Configured obfuscated listen port, if explicitly set.</param>
/// <param name="EffectiveListenPort">
///     The port on which obfuscated peer connections are actually reachable: the explicitly configured dedicated
///     port when one is set, otherwise the regular listen port (see <see cref="Shared"/>).
/// </param>
/// <param name="Shared">
///     Whether obfuscated connections share the regular listener's single bound port (per-connection sniffing)
///     rather than using a dedicated obfuscation listen port.
/// </param>
/// <param name="AdvertiseRegularPort">Whether regular-port metadata is advertised alongside obfuscation metadata.</param>
/// <param name="PreferOutbound">Whether outbound peer/distributed/transfer dials prefer compatible obfuscated metadata.</param>
/// <param name="SupportedConnectionTypes">Soulseek connection types supported by the current type-1 obfuscation implementation.</param>
/// <param name="RuntimeSupported">Whether the current runtime can activate type-1 peer/distributed/transfer obfuscation.</param>
/// <param name="RuntimeState">Current activation state.</param>
/// <param name="Summary">Human-readable status.</param>
/// <param name="Limitations">Known limitations and compatibility warnings.</param>
public sealed record SoulseekObfuscationPlan(
    bool Enabled,
    string Mode,
    int Type,
    int RegularListenPort,
    int? RequestedListenPort,
    int? EffectiveListenPort,
    bool Shared,
    bool AdvertiseRegularPort,
    bool PreferOutbound,
    IReadOnlyList<string> SupportedConnectionTypes,
    bool RuntimeSupported,
    string RuntimeState,
    string Summary,
    IReadOnlyList<string> Limitations);
