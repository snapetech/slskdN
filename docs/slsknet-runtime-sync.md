# slskNet.Runtime Sync Notes

This repository vendors `slskNet.Runtime` under `vendor/slskNet.Runtime` and consumes it through the `Soulseek` project reference in `src/slskd/slskd.csproj`.

## Synced Runtime Features

- Peer capability descriptors and envelopes for slskdN-to-slskdN feature discovery over Soulseek peer messages.
- Ed25519 descriptor signing and verification, backed by `BouncyCastle.Cryptography`.
- Mesh rendezvous helpers built on the public Soulseek interest graph and the `slskdn-mesh-v1` interest tag.
- Wishlist scheduling primitives that honor server-provided wishlist interval information.
- Protocol count hardening for variable-count server and peer payloads.
- Type-1 obfuscated peer-message (`P`), distributed-message (`D`), and file-transfer (`F`) transport support with regular fallback retained.

## slskdN Integration Decisions

- Runtime capability exchange is bridged by `SoulseekCapabilityBridgeService`, which publishes local capabilities from the existing capability file model and signs descriptors when the node key store is available.
- Capability file lookup now checks the runtime peer capability registry before falling back to browse/download of `@@slskdn/__caps__.json`.
- Mesh rendezvous API calls use the runtime `MeshRendezvousService` so the app and vendored runtime share one rendezvous implementation.
- Wishlist searches now use the Soulseek wishlist search scope and prefer the server-provided interval when available.
- The mesh UI uses the active rendezvous discovery endpoint and displays runtime capability records alongside similar-user candidates.
- The System -> Network obfuscation surface now reports `P,D` runtime support, and runtime option reload patches `PeerObfuscationOptions` so prefer/fallback behavior stays aligned after configuration changes.

## Capability Descriptor Contract

Runtime capability descriptors are signed node statements, not authorization tokens. Consumers should treat them as discovery hints until the advertised peer proves reachability and satisfies normal policy checks.

Descriptor fields:

- `peerId`: stable slskdN peer identifier derived from the node public key.
- `username`: Soulseek username observed for the publishing peer.
- `features`: lower-case feature strings advertised by the node.
- `endpoints`: protocol-specific contact hints such as shared mesh UDP/QUIC public ports.
- `issuedAt` and `expiresAt`: UTC validity window. Expired descriptors are ignored.
- `publicKey`: Ed25519 public key used to verify the descriptor signature.
- `signature`: Ed25519 signature over the canonical descriptor payload.

Known feature strings:

- `slskdn-capabilities-v1`: peer can exchange signed capability descriptors.
- `slskdn-mesh-v1`: peer can participate in Soulseek-assisted mesh rendezvous.
- `slskdn-shared-udp-v1`: peer may advertise DHT, UDP overlay, and QUIC on the compact shared UDP port.
- `slskdn-wishlist-v1`: peer understands wishlist interval-aware scheduling primitives.

Trust rules:

- A valid signature proves descriptor integrity for the included public key only.
- First-use keys are accepted for discovery and logged; key rotation should be visible to operators.
- Missing, expired, malformed, or unsigned descriptors must fall back to legacy browse/download capability discovery.
- Descriptor publication must not reveal local share paths, exact holdings, private listening history, or raw library fingerprints.

## License Impact

The sync adds `BouncyCastle.Cryptography` 2.6.2 to the vendored runtime. Its NuGet metadata declares `MIT`; its package README also notes a modified Bzip2 component under Apache-2.0. Those are permissive licenses and do not introduce copyleft obligations for slskdN.
