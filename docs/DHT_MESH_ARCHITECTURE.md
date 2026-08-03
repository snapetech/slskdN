# DHT and Mesh Architecture

This page defines the current slskdN terminology for DHT, rendezvous, mesh,
and transfer behavior. The word **DHT** refers to two related but separate
systems in the implementation.

## Short version

The public BitTorrent DHT is a rendezvous mechanism. It helps slskdN nodes
find the IP address and port of other slskdN mesh endpoints. After discovery,
the nodes establish a separate, TLS-protected slskdN overlay connection.

The DHT layers do not carry file bytes. Mesh searches, service calls, metadata
exchange, and mesh file or chunk transfers use the slskdN overlay. The overlay
can connect peers without using the Soulseek server as its transport, while
normal Soulseek searches and origin transfers remain separate paths.

## The four network surfaces

| Surface | Wire/protocol | What it does | What it does not do |
| --- | --- | --- | --- |
| Public BitTorrent DHT rendezvous | BEP 5 KRPC over UDP, implemented through MonoTorrent | Bootstraps a DHT node; beacon nodes announce an overlay endpoint under the well-known `slskdn-mesh-v1` rendezvous infohashes; seekers query those infohashes | Does not identify peers with an HTTP user-agent or transfer file data |
| slskdN mesh DHT | slskdN Kademlia-style RPCs over the mesh overlay | Stores and looks up small signed records such as peer descriptors, content-peer hints, service descriptors, and other feature metadata | Is not the public BitTorrent DHT and is not a file byte transport |
| slskdN mesh overlay | TLS-protected peer connections with the slskdN overlay protocol | Performs the `mesh_hello` handshake, mesh search, service calls, synchronization, and range/chunk transfers | Does not require BitTorrent piece-transfer semantics |
| Native Soulseek mesh rendezvous | Optional Soulseek interest/capability messages | Finds compatible slskdN accounts through the Soulseek interest graph and peer-message path | Is separate from both DHT layers and is opt-in |

The native Soulseek rendezvous path is documented separately in [Soulseek
Native Discovery](soulseek-native-discovery.md). It must not be confused with
the public BitTorrent DHT rendezvous path.

## Public BitTorrent DHT rendezvous flow

The public rendezvous layer uses standard BitTorrent DHT operations:

1. The node bootstraps into the public BitTorrent DHT through configured
   routers or previously saved DHT nodes.
2. A publicly reachable node, called a **beacon**, announces its public mesh
   overlay port with `announce_peer` under the slskdN rendezvous infohashes.
3. A node that needs neighbors, called a **seeker**, queries those infohashes
   with `get_peers`.
4. The seeker receives candidate IP/port endpoints and attempts a separate
   TLS mesh connection.
5. The peers exchange a `mesh_hello` / `mesh_hello_ack` handshake containing
   the slskdN peer identity, Soulseek username, and supported mesh features.

The DHT is not a list of slskdN clients listening for one another's
identification strings. Ordinary BitTorrent DHT nodes route and answer the
KRPC requests; the slskdN-specific rendezvous infohash is what lets slskdN
peers find the relevant endpoint records.

There is no HTTP-style user-agent string in this exchange. The application
identity and capabilities are exchanged after the DHT lookup, on the separate
mesh overlay connection.

## Mesh DHT and overlay data flow

The slskdN mesh DHT and overlay have different responsibilities:

```text
Public BitTorrent DHT (BEP 5 / UDP)
        │  overlay endpoint discovery
        ▼
TLS-protected slskdN mesh overlay
        ├─ mesh handshake and peer connectivity
        ├─ mesh search and service RPCs
        ├─ mesh DHT Kademlia RPCs and small signed metadata
        └─ file/range/chunk transfer bytes
```

The public BitTorrent DHT is therefore not involved in the file-transfer data
path. The slskdN mesh DHT can carry metadata needed to locate or describe
content, but it also does not carry the content itself. A mesh content request
is served by the peer over the overlay connection.

## Relationship to Soulseek

The mesh overlay is an additional slskdN-only network path. Mesh peers can
discover and connect to one another through DHT rendezvous and exchange mesh
data without putting those mesh messages on the Soulseek protocol. This does
not mean that every slskdN feature is independent of Soulseek: the normal
Soulseek search, social, and origin-transfer paths remain available, and the
optional native Soulseek rendezvous path uses Soulseek explicitly.

### Mesh search and Soulseek term filtering

Hybrid search starts the mesh query alongside the normal Soulseek query. The
mesh query is sent with the user's search text, and each receiving peer runs it
against that peer's local share index. Mesh responses are merged without
consulting Soulseek server/operator term suppression, the local
`filters.search.request` rules, or the incoming Soulseek excluded-phrase list.
The merge step only removes an exact duplicate identified by peer, normalized
filename, and size.

This means a term that produces no direct Soulseek results can still produce
mesh results when a connected mesh peer has matching indexed content. The
`feature.MeshParallelSearch` option is enabled by default, but a result still
requires at least one connected outbound peer advertising the `mesh_search`
capability. Local UI choices such as blocked peers, file-format filters, and
quality filters remain user-controlled and can hide or exclude results after
they arrive.

For current configuration and privacy behavior, see [Runtime Feature Gates and
Network Defaults](runtime-feature-gating.md) and [Configuration](config.md).

## Terminology to use in documentation

- Say **public BitTorrent DHT rendezvous** when referring to the MonoTorrent /
  BEP 5 UDP layer used to discover mesh endpoints.
- Say **mesh DHT** when referring to the slskdN Kademlia-style metadata and
  service-discovery layer carried over mesh connections.
- Say **mesh overlay** when referring to the authenticated peer connection and
  its control, metadata, search, and file-transfer traffic.
- Say **DHT discovery information** or **DHT metadata**, not “DHT file
  transfers.”

Related implementation references:

- `src/slskd/DhtRendezvous/DhtRendezvousService.cs`
- `src/slskd/Mesh/Dht/MeshDhtClient.cs`
- `src/slskd/Mesh/Dht/DhtService.cs`
- `src/slskd/Mesh/ServiceFabric/Services/MeshContentMeshService.cs`
- [DHT Rendezvous Design](DHT_RENDEZVOUS_DESIGN.md)
- [T-902 DHT Node and Routing Table Design](research/T-902-dht-node-design.md)
