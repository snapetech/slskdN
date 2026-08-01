# Runtime Feature Gates and Network Defaults

This page documents the current runtime boundary of the slskdN-specific
feature settings. The `feature.*` section is not a master switch for all
background services.

## API gates versus service lifecycle

The following settings gate controllers and APIs. Setting them to `false`
makes corresponding gated endpoints unavailable, but does not prevent the
underlying services from being constructed, hosted, registered with the mesh
service router, or performing independently configured startup work:

- `feature.Mesh`
- `feature.Dht`
- `feature.Pods`
- `feature.SocialFederation`
- `feature.VirtualSoulfind`
- `feature.MultiSourceDownloads`

`feature.MeshPublishAvailability` and `feature.MeshParallelSearch` control
their named operations; they do not stop the mesh or DHT hosts. The service
router currently registers DHT, hole-punch, MeshContent, pods, shadow-index,
private-gateway, and mesh-introspection services independently of those API
flags.

`feature.IdentityFriends` is an exception: in addition to gating Identity and
Friends APIs, setting it to `false` prevents application-startup mDNS friend-
code advertising. It does not disable unrelated mesh discovery.

## Independent runtime controls

There is no supported `mesh.enabled` setting. The current mesh options are
independent:

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

dht:
  enabled: false
  lan_only: true

mesh:
  enable_dht: false
  enable_overlay: false
  enable_soulseek_capability_handshake: false
  enable_soulseek_rendezvous: false
  probe_soulseek_rendezvous_capabilities: false

overlay:
  enable: false

overlay_data:
  enable: false
```

This is a reduction profile, not a guarantee that no slskdN-specific service
will be constructed or appear in startup logs. In particular, the DHT
initialization waiter is independently hosted and can currently log a bounded
initialization timeout even when DHT is disabled. A service being registered
also does not prove that its network operation succeeded or is reachable.

## Pods and Gold Star Club

`feature.Pods: false` gates pod APIs; it does not stop `GoldStarClubService`.
The service ensures the reserved pod exists locally and, by default, enrolls
the configured Soulseek username after login. To prevent automatic enrollment,
set this environment variable before the first startup:

```yaml
environment:
  SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN: "false"
```

With auto-join disabled, the reserved pod is still created locally. Leaving an
existing Gold Star membership writes a local revocation marker and prevents a
later automatic rejoin.

## Default network-visible behavior

The current defaults favor participation rather than a quiet VPN-first node:

- `dht.enabled` defaults to `true` and `dht.lan_only` defaults to `false`, so
  the node bootstraps against the configured public BitTorrent DHT routers.
- `mesh.enable_dht`, `mesh.enable_overlay`, mesh STUN, the Soulseek capability
  handshake, `virtualSoulfindV2.enabled`, and
  `signalSystem.btExtensionChannel.enabled` default to `true`.
- `feature.IdentityFriends` defaults to `true`, which starts LAN mDNS
  advertising of the local profile/friend code when the Web port is available.
- Gold Star Club auto-enrollment defaults to on.
- `soulseek.description` defaults to a public string identifying the account
  as a slskdN user.
- `mesh.enable_soulseek_rendezvous` defaults to `false`; this only prevents the
  recognizable `slskdn-mesh-v1` interest tag from being published. It does not
  suppress the default Soulseek description, DHT bootstrap, mDNS, or capability
  handshake.

Operators who need a quiet or privacy-minimized deployment must configure each
surface explicitly. A false value reported under `/system/info` confirms the
API feature state, not whole-process dormancy.
