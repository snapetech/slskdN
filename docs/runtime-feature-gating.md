# Runtime Feature Gates and Network Defaults

slskdN-specific network features are opt-in. The upstream Soulseek client,
shares, search, transfers, and Gluetun integration continue to operate when all
experimental flags are false.

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
gates Identity/Friends APIs and startup mDNS friend-code advertising.

## Quiet default profile

The shipped defaults are equivalent to the following privacy-sensitive
settings:

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

## Pods and Gold Star Club

`feature.Pods: false` prevents Gold Star Club startup, including reserved-pod
creation, DHT publication, and automatic Soulseek-username enrollment.

When pods are explicitly enabled, Gold Star auto-enrollment can still be
disabled independently before first startup:

```yaml
environment:
  SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN: "false"
```

With pods enabled and auto-join disabled, the reserved pod is created locally
but the Soulseek username is not enrolled. Leaving an existing membership
writes a local revocation marker and prevents later automatic rejoin.

## Explicit public DHT opt-in

To join the public BitTorrent DHT, enable both the feature and rendezvous
service and explicitly turn off LAN-only mode:

```yaml
feature:
  Dht: true

dht:
  enabled: true
  lan_only: false
```

This contacts the configured public bootstrap routers and makes the configured
DHT endpoint discoverable. Keep `lan_only: true` for private/LAN-only use.

## Existing configurations

Explicit values in an existing configuration continue to win over the new
defaults. Operators upgrading from a network-forward release should review
their `feature`, `dht`, `mesh`, `overlay`, `virtualSoulfindV2`, `signalSystem`,
and `soulseek.description` sections rather than assuming prior implicit
enablement remains active.
