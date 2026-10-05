# T-908: Private BitTorrent Backend

> **Status:** Private policy and resolver fetch are implemented behind the
> VirtualSoulfind v2 torrent option. Torrent acquisition remains disabled by
> default. Cross-peer swarm fallback is explicitly rejected until sender
> activation and job cleanup are implemented. Fallback also lacks a defined
> seeding/serving path and per-peer authentication for BitTorrent connections.

## Implemented behavior

- `TorrentBackend` discovers candidates from `ISourceRegistry`, rejects
  candidates without private provenance when `PrivateOnly` is enabled, and
  validates infohashes and parsed magnet links.
- `MonoTorrentBitTorrentBackend` performs fetch-by-infohash/magnet. The
  incomplete swarm fallback path is explicitly rejected by its signal handler
  unless the job binds the exact requested variant; it does not start managers
  or report a successful fallback id.
- `PrivateOnly` removes announce URLs and web seeds from magnets; disables
  DHT and peer exchange; and uses only allowed peer sources. It overrides
  contradictory `DisableDht`/`DisablePex` values.
- Engine-wide local peer discovery is disabled. This also affects public-mode
  torrent managers in the same process because MonoTorrent exposes this setting
  at engine scope.
- The standalone resolver fetch has no overlay endpoint list, so private fetch
  currently uses configured invite-list peers only. Cross-peer swarm fallback
  is unavailable until it can use job-provided overlay peers through a complete
  sender and cancellation lifecycle.
- Torrent acquisition is explicitly configured under
  `virtualSoulfindV2.backends.torrent` and defaults off.

## Library and target framework

The application targets `net10.0`. MonoTorrent
`3.9.0-alpha.unstable.rev0000` is the current upstream package, targets
`net8.0`, and is compatible with higher target frameworks. NuGet therefore
selects the .NET 8 asset for the .NET 10 app. The package is a prerelease; the
previous package also was a prerelease, but the newer version changed the DHT
and socket-listener APIs.

The MonoTorrent engine does not load or save magnet metadata in its cache. This
keeps manual peer registration ahead of metadata for private transfers. If a
manager already has BEP 27 private metadata before manual peer registration,
the backend fails closed because MonoTorrent rejects external peer injection
for that torrent.

## Network boundaries

- BitTorrent DHT is separate from slskdN's mesh DHT. Public BitTorrent DHT
  rendezvous discovers mesh endpoints; the torrent transfer engine is governed
  by this backend's own settings.
- Private magnets omit trackers and web seeds, and private managers do not use
  DHT, PEX, or local peer discovery.
- Overlay peer input accepts only `mesh` or `overlay` transports. Soulseek
  addresses are never passed to MonoTorrent.
- Invite endpoints must be valid `host:port` entries. User info, URL paths,
  queries, fragments, and invalid ports are rejected.
- Raw magnet references are not written to logs.

## Remaining work

- Define the protocol roles and network trust boundary before adding a
  production job owner. `MonoTorrentBitTorrentBackend` currently fetches data;
  it does not create or seed a torrent for a shared file. The existing
  BitTorrent connection path also has no peer-authentication mechanism, so a
  private torrent hash alone cannot enforce that only the requesting mesh peer
  can read the content.
- Choose a production transfer owner that controls output, progress,
  cancellation, and disposal. `InMemorySwarmJobStore` currently supports lookup
  only, the application creates no `SwarmJob` instances, and
  `SwarmDownloadOrchestrator` is not registered; that prototype writes into a
  temporary output area and is not the existing download pipeline.
- After those boundaries are designed, bind the exact content and variant to a
  real job, connect acknowledgements to an authenticated sender and an active
  torrent transfer, implement authorized `Swarm.JobCancel`, and release every
  manager and temporary resource when its owning job ends.
- Keep requests fail-closed until two-peer end-to-end tests cover peer
  authorization, exact variant binding, activation, cancellation, rejection,
  and manager cleanup.
- Keep keyed swarms deferred until a concrete key exchange and peer-auth
  protocol is designed.

See [Research Design Scope](9-research-design-scope.md#t-908-private-bittorrent-backend),
[DHT and Mesh Architecture](../DHT_MESH_ARCHITECTURE.md), and
[configuration](../config.md#virtualsoulfind-v2-bittorrent).
