# Mesh-to-Runtime Migration Plan

This document plans a migration of slskdN mesh features from the slskdn application repository (`../slskdn`) into the slskNet.Runtime fork. The goal is to re-express the mesh's protocol/codec surface as runtime extensions so that slskdN-aware peers can rendezvous and exchange overlay metadata over the existing Soulseek peer-message channel, while leaving the BitTorrent-DHT path in slskdn as a server-independent fallback.

## Compatibility Position

All additions in this plan must preserve wire compatibility with legacy Soulseek clients:

- No changes to the format of existing peer or server messages.
- All new peer-message codes are slskdN-specific. Legacy clients ignore unknown peer-message codes and must not be sent any new code that they would interpret incorrectly.
- New server-message use is restricted to APIs already exposed by the runtime (interests, similar-users, recommendations, multi-user private messages, room failures).
- File-transfer connections are not modified.
- Type-1 peer/distributed/transfer obfuscation remains opt-in and must not displace the regular listener.

If a feature cannot be moved without violating these rules, it stays in slskdn.

## Rendezvous Surfaces Available in the Runtime

Three compat-safe rendezvous paths exist in the current runtime and are sufficient for slskdN peers to discover each other without DHT:

1. **Capability handshake over P-type peer-message connections.** After the standard Soulseek peer handshake, slskdN clients exchange a capability message at a slskdN-only message code. Legacy peers ignore the code.
2. **Interest-tag rendezvous.** slskdN clients add a magic interest string (for example `slskdn-mesh-v1`) using `AddInterestAsync`. `GetSimilarUsersAsync` then returns the slskdN cohort. The server treats the tag as ordinary interest text.
3. **Search-based rendezvous.** A magic search query yields announcements from slskdN clients via the existing distributed-network search path. Useful as a third channel and during cohort growth phases.

The migration plan below assumes paths 1 and 2 as primary, with path 3 reserved for sparse-cohort cases.

## Runtime API Additions Required

The following surface area must be added to the runtime before any slskdn code can move:

| API | Purpose | Compat impact |
| --- | ------- | ------------- |
| `PeerMessageCodec` extension hooks | Allow application code to register handlers for slskdN-only peer-message codes on existing `MessageConnection` instances. | Additive. Legacy peers never see these codes. |
| `SendPeerMessageAsync(username, code, payload, ...)` | Send a raw slskdN-only peer message to a specific user, reusing existing peer-connection management and indirect-fallback paths. | Additive. |
| `PeerCapabilityExchange` (typed wrapper) | Strongly-typed Hello/HelloAck pair carrying overlay port, feature list, signed descriptor, and nonce. | Additive. |
| `IPeerDescriptorSigner` and `IPeerDescriptorVerifier` | Ed25519 signing and verification of peer descriptors. Implementation lives in runtime; key storage stays in the application. | Additive. |
| `MeshRendezvousInterestTag` constant and helper APIs | Standardized magic interest tag and helpers for adding/removing/listing it via the existing interest APIs. | Additive. |
| `IPeerMessageRouter` for envelope dispatch | Generic envelope/dispatch pattern for slskdN-only RPCs over peer-message. | Additive. |

These additions do not change any default behavior. No new outbound bytes are emitted unless the consuming application registers handlers and calls the new APIs.

## Migration Phases

The migration is split into phases that each leave both repositories in a buildable, releasable state.

### Phase 0 — Runtime Bootstrap (runtime only)

Add the API surface listed above. Provide tests that exercise the round-trip of a slskdN-only peer message over an existing Soulseek peer connection between two runtime instances.

Exit criteria:

- New APIs ship in the runtime with unit tests.
- Default wire behavior is unchanged.
- A reference example in `examples/` demonstrates a slskdN-only ping over a normal peer connection.

### Phase 1 — Capability Handshake and Peer Registry

Move the mesh capability handshake from the overlay channel into the runtime's peer-message channel.

Source files in slskdn that move or shrink:

- `DhtRendezvous/Messages/OverlayMessages.cs` — handshake, ping, disconnect message shapes become runtime-defined records.
- `DhtRendezvous/MeshNeighborPeerSyncService.cs` — neighbor exchange becomes a runtime peer-message exchange.
- `DhtRendezvous/MeshNeighborRegistry.cs` — runtime exposes a slskdN-aware peer registry layer over `IUserEndPointCache`.
- `Mesh/MeshPeer.cs`, `Mesh/MeshPeerManager.cs` — peer descriptor type and lifecycle move into runtime; slskdn keeps an application-level wrapper for telemetry and policy.

Result: slskdN clients that share a normal Soulseek peer connection automatically learn each other's overlay capability without involving the DHT.

### Phase 2 — Signed Peer Descriptors

Move descriptor signing and verification into the runtime.

Source files:

- `Mesh/Transport/Ed25519Signer.cs`
- `Mesh/Transport/CanonicalSerialization.cs`
- `Mesh/Transport/DescriptorSigningService.cs`
- `Mesh/MeshMessageSigner.cs`
- `Mesh/ProofOfPossessionService.cs`

Key storage (`Mesh/Overlay/KeyStore.cs`) and trust roots stay in slskdn. The runtime accepts a signer and a verifier as injected interfaces. This keeps key custody an application concern and the runtime an algorithm concern.

### Phase 3 — Service-Fabric RPC Codec

Move the overlay control envelope and dispatcher into the runtime so that slskdN RPCs ride on peer-message connections rather than on overlay sockets.

Source files:

- `Mesh/Overlay/ControlDispatcher.cs`
- `Mesh/Overlay/ControlEnvelope.cs`
- `Mesh/Overlay/ControlEnvelopeValidator.cs`
- `Mesh/Overlay/OverlayControlTypes.cs`
- `Mesh/ServiceFabric/*` framing pieces (the request/response shapes, not the application-level service registrations)

Application services registered in slskdn continue to live in slskdn. The runtime becomes the transport-and-codec for them.

### Phase 4 — Mesh Search as a Peer-Message Extension

Move `mesh_search_req` and `mesh_search_resp` into the runtime as slskdN-only peer messages. Legacy peers do not see the codes. slskdN peers gain an alternative search path that does not depend on the distributed-network parent topology.

Source files:

- `DhtRendezvous/Search/*` (the request/response codecs and matching logic at the protocol layer; result-ranking and policy stay in slskdn)

### Phase 5 — Swarm Download Orchestration

`Swarm/SwarmDownloadOrchestrator.cs` and `Swarm/SwarmJobModels.cs` are already client-side coordination over multiple Soulseek peer connections. Move them into the runtime as a higher-level transfer orchestrator that consumes the existing `Transfer` API. `IVerificationEngine` becomes an injection point so that slskdn keeps responsibility for hash verification policy.

### Phase 6 — Relay Protocol Surface (split)

Move only the relay client framing into the runtime:

- `Relay/Types/*` (message shapes)
- `Relay/IRelayClient.cs`, `RelayClient.cs` framing and request/response codec

Stay in slskdn:

- `Relay/RelayService.cs` (orchestration)
- `Relay/RelayTlsPinValidator.cs`, `RelayTlsWarningService.cs`
- `Relay/NullRelayClient.cs` selection logic

The runtime gains the ability to speak relay protocol; slskdn retains operational trust roots.

### Phase 7 — Discovery Graph Signal Sources

`DiscoveryGraph/DiscoveryGraphService.cs` aggregates signals. Most of its useful inputs are already reachable from the runtime: room presence, distributed-parent topology, similar users, and the new capability-handshake registry from Phase 1. The graph computation itself can live in the runtime as a passive observer; slskdn keeps the policy that consumes it.

## What Stays in slskdn

The following components are infrastructure or aggregate state that does not belong in a transport runtime:

- `Mesh/Bootstrap/MeshBootstrapService.cs` — DHT-first bootstrap is the resilience story.
- `Mesh/Dht/*` — Kademlia routing, content-peer hint storage, descriptor publication via DHT.
- `Mesh/Transport/{Tor,I2p}SocksDialer.cs`, `DnsLeakPreventionVerifier.cs`, `TransportDowngradeProtector.cs`, `TransportPolicy.cs`, `TransportSelector.cs` — transport infrastructure and policy.
- `Mesh/Overlay/Quic*`, `Udp*` data plane.
- `Mesh/Governance/*`, `Mesh/Realm/*`, `QuarantineJury/*` — aggregate-state policy.
- `Mesh/Overlay/KeyStore.cs`, `CertificatePinManager.cs`, `RelayTlsPinValidator.cs` — operational trust roots.
- `Mesh/Health/*`, `MeshStatsCollector.cs`, `MeshHealthCheck.cs` — operational telemetry.

## Deployment Posture After Migration

The intended end state runs two parallel rendezvous paths with clear roles:

- **Primary, runtime-resident:** capability handshake on P-type peer connections, plus the magic-interest cohort lookup. Active whenever the Soulseek server is reachable. Most users get instant rendezvous without ever touching the DHT.
- **Fallback, slskdn-resident:** BitTorrent-DHT bootstrap, Tor/I2P transports, relay system. Active when the central server is unreachable, blocked, or when the user opts into server-independence.

The mesh stops being a parallel-everything system and becomes the resilience layer.

## Risks and Cost

- Pushing slskdN-only message codes through the Soulseek server (rooms, private messages) is fine for tiny rendezvous payloads but a poor place for bulk traffic. Keep data planes out of the protocol fork.
- Soulseek username is not a cryptographic identity. Even after Phase 2, signed peer descriptors require a key-management story that lives in slskdn.
- A slskdN-only peer-message code that a misbehaving peer sends to a legacy client costs nothing on the legacy side because the code is unknown and dropped. Verify that the runtime's peer-message read path treats unknown codes as drop-and-continue rather than fatal.
- Magic interest tags are publicly visible to anyone who queries the same interest. Treat them as cohort markers, not as secrets.

## Estimated Code Movement

Approximate, based on a first-pass file-by-file read:

- Lines that move into the runtime: ~6000 (codec/handshake/RPC framing/swarm orchestration).
- Lines that split (codec to runtime, infrastructure stays): ~1500.
- Lines that stay in slskdn unchanged: ~3000 (DHT, transports, governance, key storage, telemetry).

This represents roughly 50% of mesh-adjacent lines and roughly 70% of the protocol/codec surface area moving into the runtime.

## Tracking

This is a forward-looking plan, not an in-flight implementation. When work begins, each phase should land as a separate runtime release with the corresponding slskdn refactor following in the next slskdn release. A phase is complete when:

- The runtime ships the new API surface with tests.
- slskdn deletes the corresponding mesh code or replaces it with a thin adapter that consumes the runtime API.
- Default wire behavior in both repositories is unchanged for users who do not opt in.
