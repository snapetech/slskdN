# slskNet.Runtime Sync Notes

This repository vendors `slskNet.Runtime` under `vendor/slskNet.Runtime` and consumes it through the `Soulseek` project reference in `src/slskd/slskd.csproj`.

## Synced Runtime Features

- Peer capability descriptors and envelopes for slskdN-to-slskdN feature discovery over Soulseek peer messages.
- Ed25519 descriptor signing and verification, backed by `BouncyCastle.Cryptography`.
- Mesh rendezvous helpers built on the public Soulseek interest graph and the `slskdn-mesh-v1` interest tag.
- Wishlist scheduling primitives that honor server-provided wishlist interval information.
- Protocol count hardening for variable-count server and peer payloads.

## slskdN Integration Decisions

- Runtime capability exchange is bridged by `SoulseekCapabilityBridgeService`, which publishes local capabilities from the existing capability file model and signs descriptors when the node key store is available.
- Capability file lookup now checks the runtime peer capability registry before falling back to browse/download of `@@slskdn/__caps__.json`.
- Mesh rendezvous API calls use the runtime `MeshRendezvousService` so the app and vendored runtime share one rendezvous implementation.
- Wishlist searches now use the Soulseek wishlist search scope and prefer the server-provided interval when available.
- The mesh UI uses the active rendezvous discovery endpoint and displays runtime capability records alongside similar-user candidates.

## License Impact

The sync adds `BouncyCastle.Cryptography` 2.6.2 to the vendored runtime. Its NuGet metadata declares `MIT`; its package README also notes a modified Bzip2 component under Apache-2.0. Those are permissive licenses and do not introduce copyleft obligations for slskdN.
