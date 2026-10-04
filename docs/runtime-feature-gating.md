# Runtime Feature Gates and Network Defaults

slskdN-specific network features are enabled by default. Operators can disable
them individually or use the reduction profile below. The upstream Soulseek
client, shares, search, transfers, and Gluetun integration continue to operate
when all experimental flags are false.

The authenticated native capabilities endpoint (`/api/slskdn/capabilities`,
also available at `/api/v0/slskdn/capabilities`) reports each gated feature's
effective `status`, `enabled` value, and operator-facing message under
`featureGates`. The values come from the same runtime gate used by API
controllers, including dependent service settings such as mesh overlay and
DHT enablement. The Web UI reads this status before opening gated surfaces,
refreshes it when a page returns to the foreground and once per minute, and
keeps older servers compatible when they omit feature-gate metadata. Messaging
and MediaCore hide disabled Pods controls and avoid PodCore requests; Search
does the same for SongID and federated recommendations; System does the same
for Mesh, DHT rendezvous, VirtualSoulfind, and multi-source downloads. The
`/pods` routes remain registered so they can explain the disabled state
without redirecting elsewhere.
Configuration-disabled API routes return 404. A gated route whose runtime
status is `MovedToSlskr` returns 410 Gone with a link to the separate slskr
project. The current moved-to-slskr inventory entry is a documentation handoff
and has no active API route.

## Feature and service lifecycle gates

These settings gate both their controllers/APIs and their corresponding
background or network service activation:

- `feature.Mesh`
- `feature.Dht`
- `feature.Pods`
- `feature.SocialFederation`
- `feature.VirtualSoulfind`
- `feature.MultiSourceDownloads`

When all are false, slskdN does not start mesh bootstrap or peer-descriptor
refresh, does not register mesh RPC services with the service router, does not
start DHT rendezvous, and does not start VirtualSoulfind or pod workers.

`feature.MeshPublishAvailability` and `feature.MeshParallelSearch` separately
gate availability publication and parallel mesh search. `feature.IdentityFriends`
gates Identity/Friends APIs. Startup mDNS advertising is separately controlled
by `lan_discovery.advertise`, which defaults to `false`; set it to `true` to
publish the peer ID, friend code, display name, API port, and capabilities on
the local network. The Nearby peers browse action remains user-triggered and
works while automatic advertising is off.

## Explicit reduction profile

Use the following settings when an operator wants the upstream Soulseek path
without slskdN's DHT, mesh, pod, federation, or VirtualSoulfind services:

```yaml
feature:
  Mesh: false
  Dht: false
  Pods: false
  SocialFederation: false
  VirtualSoulfind: false
  MultiSourceDownloads: false
  MeshPublishAvailability: false
  MeshParallelSearch: false
  IdentityFriends: false

lan_discovery:
  advertise: false

dht:
  enabled: false
  lan_only: true
  enable_stun: false

mesh:
  enable_dht: false
  enable_overlay: false
  enable_stun: false
  enable_soulseek_capability_handshake: false
  enable_soulseek_rendezvous: false
  probe_soulseek_rendezvous_capabilities: false

overlay:
  enable: false

overlay_data:
  enable: false

virtualSoulfindV2:
  enabled: false

signalSystem:
  enabled: false
  meshChannel:
    enabled: false
  btExtensionChannel:
    enabled: false

soulseek:
  description: ""
```

There is no supported `mesh.enabled` key. Enabling a feature gate does not
implicitly enable every transport: for example, DHT needs both `feature.Dht`
and `dht.enabled`, while mesh descriptor publishing needs `feature.Mesh` and
`mesh.enable_dht`.

## Upstream-like reduction

The reduction profile intentionally focuses on slskdN's networked experimental
services. To use slskdN for standard Soulseek search/transfers plus multiple
download destinations, add these independent controls when they are not
needed:

```yaml
feature:
  Streaming: false
  StreamingRelayFallback: false

wishlist:
  enabled: false

transfers:
  download:
    auto_retry:
      enabled: false
```

The Web player and album-candidate panel are browser-local controls under
**System → Experience**; they are not server feature gates. Auto-replace is
already opt-in by default. Backfill is also separate from Mesh and can be
disabled at runtime with `POST /api/v0/backfill/enable?enabled=false`; that
switch is in-memory and must be reapplied after a restart.

## Pods and Gold Star Club

`feature.Pods: false` prevents Gold Star Club startup, including reserved-pod
creation, DHT publication, and automatic Soulseek-username enrollment. The
Gold Star Club is also independently and strictly opt-in: the reserved pod is
not created, published, or used for automatic enrollment unless the daemon
environment contains the exact value `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true`.
Missing, malformed, `false`, `0`, and other values are all disabled.

To opt in, enable the pod feature and export the variable before startup:

```sh
SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true
```

With pods enabled and the variable unset or not exactly `true`, Gold Star has
no startup side effects. Leaving an existing membership writes a local
revocation marker and prevents later automatic rejoin.

## DHT controls

The public BitTorrent DHT is enabled by default. Three controls have distinct
effects:

- `feature.Dht: false` disables DHT APIs and prevents DHT hosted-service and
  service-router activation.
- `dht.enabled: false` prevents DHT initialization, publication, refresh, and
  the initialization wait even if the feature flag remains true.
- `dht.lan_only: true` keeps DHT enabled while suppressing public bootstrap
  routers.

The normal enabled configuration is:

```yaml
feature:
  Dht: true

dht:
  enabled: true
  lan_only: false
```

This contacts the configured public bootstrap routers and makes the configured
DHT endpoint discoverable. To disable DHT cleanly, set both `feature.Dht` and
`dht.enabled` to false. Keep `lan_only: true` when DHT should remain active but
public bootstrap must be suppressed.

## Existing configurations

Explicit values in an existing configuration override defaults. Operators who
want a reduced surface should set the relevant feature and service controls
explicitly rather than relying on implicit defaults.
