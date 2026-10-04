// <copyright file="MonoTorrentBitTorrentBackend.cs" company="slskdN Team">
//     Copyright (c) slskdN Team. All rights reserved.
// </copyright>
namespace slskd.Signals.Swarm;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonoTorrent;
using MonoTorrent.Client;
using slskd.Swarm;
using slskd.VirtualSoulfind.v2.Backends;

/// <summary>
///     MonoTorrent-based <see cref="IBitTorrentBackend"/> for private swarms and fetch-by-infohash (def-3).
///     Respects <see cref="PrivateTorrentModeOptions"/> (DisableDht, DisablePex, InviteList).
/// </summary>
public sealed class MonoTorrentBitTorrentBackend : IBitTorrentBackend, IDisposable
{
    private readonly ILogger<MonoTorrentBitTorrentBackend> _logger;
    private readonly IOptionsMonitor<TorrentBackendOptions> _options;
    private readonly ClientEngine _engine;
    private readonly string _cacheDir;

    public MonoTorrentBitTorrentBackend(
        ILogger<MonoTorrentBitTorrentBackend> logger,
        IOptionsMonitor<TorrentBackendOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cacheDir = Path.Combine(Path.GetTempPath(), "slskd-bt-cache");
        Directory.CreateDirectory(_cacheDir);

        var pm = _options.CurrentValue.PrivateMode;
        _engine = new ClientEngine(BuildEngineSettings(_cacheDir, pm));
    }

    public bool IsSupported()
    {
        return _options.CurrentValue.Enabled;
    }

    public async Task<string?> FetchByInfoHashOrMagnetAsync(string backendRef, string destDirectory, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        MagnetLink? magnet = null;
        if (backendRef.TrimStart().StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            if (!MagnetLink.TryParse(backendRef, out magnet) || magnet == null)
                return null;
        }
        else
        {
            var ih = ParseInfohash(backendRef);
            if (ih == null) return null;
            magnet = new MagnetLink(ih);
        }

        var privateMode = _options.CurrentValue.PrivateMode;
        var enforcePrivateOnly = privateMode?.PrivateOnly == true;
        magnet = ApplyPrivateMagnetPolicy(magnet, enforcePrivateOnly);
        var manualPeers = BuildManualPeers(privateMode, sources: null, includeInviteList: true);
        if (enforcePrivateOnly && manualPeers.Count == 0)
        {
            _logger.LogWarning("Private torrent fetch has no allowed manual peers");
            return null;
        }

        TorrentManager? manager = null;
        try
        {
            Directory.CreateDirectory(destDirectory);
            manager = await _engine.AddAsync(magnet, destDirectory, BuildTorrentSettings(privateMode, forcePrivateOnly: false));
            ct.ThrowIfCancellationRequested();
            if (enforcePrivateOnly && !CanAddManualPeers(manager, "FetchByInfoHashOrMagnetAsync"))
            {
                await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");
                manager = null;
                return null;
            }

            var addedPeers = manualPeers.Count == 0 ? 0 : await manager.AddPeersAsync(manualPeers);
            ct.ThrowIfCancellationRequested();
            if (enforcePrivateOnly && addedPeers == 0)
            {
                _logger.LogWarning("Private torrent fetch could not add an allowed manual peer");
                await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");
                manager = null;
                return null;
            }

            if (addedPeers > 0)
                _logger.LogDebug("Added {PeerCount} allowed invite peers for private torrent fetch", addedPeers);

            await manager.StartAsync();
            ct.ThrowIfCancellationRequested();
            if (!manager.HasMetadata)
                await manager.WaitForMetadataAsync(ct);

            // Wait for completion (with timeout)
            var timeout = TimeSpan.FromMinutes(60);
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            while (manager.Progress < 100.0 && DateTimeOffset.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
            }

            ct.ThrowIfCancellationRequested();

            if (manager.Progress < 100.0)
            {
                _logger.LogWarning("FetchByInfoHashOrMagnetAsync did not complete: {Progress}%", manager.Progress);
                await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");
                manager = null;
                return null;
            }

            string path;
            if (manager.Torrent!.Files.Count == 1)
                path = Path.Combine(manager.SavePath, manager.Torrent.Files[0].Path);
            else
                path = Path.Combine(manager.ContainingDirectory!, manager.Torrent.Files[0].Path);

            await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");
            manager = null;
            return File.Exists(path) ? path : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            if (manager != null)
                await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FetchByInfoHashOrMagnetAsync failed after validating the backend reference");
            if (manager != null)
                await CleanupManagerAsync(manager, "FetchByInfoHashOrMagnetAsync");

            return null;
        }
    }

    internal static EngineSettings BuildEngineSettings(string cacheDirectory, PrivateTorrentModeOptions? privateMode)
    {
        var builder = new EngineSettingsBuilder
        {
            CacheDirectory = cacheDirectory,

            // Local peer discovery is engine-wide, so keep it off for all managers.
            // Private transfers use only explicit overlay/invite peer sources.
            AllowLocalPeerDiscovery = false,
            AutoSaveLoadMagnetLinkMetadata = false,
        };

        if (privateMode?.PrivateOnly == true || privateMode?.DisableDht == true)
            builder.DhtEndPoint = null;

        return builder.ToSettings();
    }

    internal static TorrentSettings BuildTorrentSettings(PrivateTorrentModeOptions? privateMode, bool forcePrivateOnly)
    {
        var privateOnly = forcePrivateOnly || privateMode?.PrivateOnly == true;
        return new TorrentSettingsBuilder
        {
            AllowDht = !privateOnly && privateMode?.DisableDht != true,
            AllowPeerExchange = !privateOnly && privateMode?.DisablePex != true,
        }.ToSettings();
    }

    internal static MagnetLink ApplyPrivateMagnetPolicy(MagnetLink magnet, bool privateOnly)
    {
        if (!privateOnly)
            return magnet;

        return new MagnetLink(
            magnet.InfoHashes,
            magnet.Name,
            Array.Empty<string>(),
            Array.Empty<string>(),
            magnet.Size);
    }

    internal static IReadOnlyList<PeerInfo> BuildManualPeers(
        PrivateTorrentModeOptions? privateMode,
        IReadOnlyList<SwarmSource>? sources,
        bool includeInviteList)
    {
        var peerSources = privateMode?.AllowedPeerSources ?? PrivatePeerSource.Overlay;
        var allowOverlay = peerSources is PrivatePeerSource.Overlay or PrivatePeerSource.Both;
        var allowInviteList = includeInviteList &&
                              privateMode is not null &&
                              (peerSources is PrivatePeerSource.InviteList or PrivatePeerSource.Both);
        var peers = new List<PeerInfo>();
        var endpoints = new HashSet<Uri>();

        if (allowOverlay && sources is not null)
        {
            foreach (var source in sources)
            {
                if (!string.Equals(source.Transport, "mesh", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(source.Transport, "overlay", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var peer = CreatePeerInfo(source.Address, source.Port);
                if (peer is not null && endpoints.Add(peer.ConnectionUri))
                    peers.Add(peer);
            }
        }

        if (allowInviteList)
        {
            foreach (var endpoint in privateMode!.InviteList ?? Array.Empty<string>())
            {
                var peer = ParseInvitePeer(endpoint);
                if (peer is not null && endpoints.Add(peer.ConnectionUri))
                    peers.Add(peer);
            }
        }

        return peers;
    }

    private static PeerInfo? CreatePeerInfo(string? host, int? port)
    {
        if (string.IsNullOrWhiteSpace(host) || !port.HasValue || port.Value is < 1 or > 65535)
            return null;

        var normalizedHost = host.Trim();
        if (normalizedHost.StartsWith('[') && normalizedHost.EndsWith(']'))
            normalizedHost = normalizedHost[1..^1];

        if (Uri.CheckHostName(normalizedHost) == UriHostNameType.Unknown)
            return null;

        try
        {
            return new PeerInfo(new UriBuilder("tcp", normalizedHost, port.Value).Uri);
        }
        catch (UriFormatException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static PeerInfo? ParseInvitePeer(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) ||
            !Uri.TryCreate($"tcp://{endpoint.Trim()}", UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.Port is < 1 or > 65535 ||
            Uri.CheckHostName(uri.Host) == UriHostNameType.Unknown)
        {
            return null;
        }

        return CreatePeerInfo(uri.Host, uri.Port);
    }

    private async Task CleanupManagerAsync(TorrentManager manager, string operation)
    {
        try
        {
            if (manager.State != TorrentState.Stopped)
                await manager.StopAsync();
        }
        catch (Exception cleanupEx)
        {
            _logger.LogDebug(cleanupEx, "Failed to stop manager during {Operation} cleanup", operation);
        }

        if (manager.State == TorrentState.Stopped)
        {
            try
            {
                await _engine.RemoveAsync(manager);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogDebug(cleanupEx, "Failed to remove manager during {Operation} cleanup", operation);
            }
        }
    }

    private bool CanAddManualPeers(TorrentManager manager, string operation)
    {
        if (!manager.HasMetadata || manager.Torrent?.IsPrivate != true)
            return true;

        _logger.LogWarning(
            "{Operation} cannot add configured peers because MonoTorrent loaded private torrent metadata before peer registration",
            operation);
        return false;
    }

    private static InfoHash? ParseInfohash(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim().ToLowerInvariant();
        if (s.Length == 40 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
        {
            try { return InfoHash.FromHex(s); } catch { return null; }
        }

        if (s.Length == 64 && s.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
        {
            try { return InfoHash.FromHex(s); } catch { return null; }
        }

        return null;
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}
