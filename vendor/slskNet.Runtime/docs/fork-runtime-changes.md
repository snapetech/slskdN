# slskNet.Runtime Fork Changes

This document tracks protocol and runtime behavior added after the fork from Soulseek.NET `10.0.0`.

## Compatibility Position

The fork is intended to remain compatible with legacy Soulseek clients.

Default behavior preserves the upstream wire behavior. New outbound protocol messages are only sent when an application calls the new APIs. Peer-message obfuscation is disabled by default, and enabling it still requires the regular peer-message port to be advertised.

Compatibility rules used by the implementation:

- Do not replace the regular peer-message listener with an obfuscated-only listener.
- Do not obfuscate file-transfer or distributed-network connections.
- Do not attempt outbound obfuscated peer-message connections unless the remote peer has advertised a compatible type-1 endpoint.
- Keep regular direct and indirect peer-message connection attempts available as fallback paths.
- Fail closed on malformed new protocol responses rather than accepting impossible counts or partial repeated data.

## Type-1 Peer-message Obfuscation

Type-1 obfuscation support covers peer-message streams only. It is not encryption and does not change transfer payload transport.

Implementation pieces:

- `PeerObfuscationOptions` controls runtime behavior.
- `SetListenPortCommand` can append obfuscation type and obfuscated port metadata.
- `UserAddressResponse` and `ConnectToPeerResponse` parse advertised obfuscation metadata.
- `SoulseekClient` can start a dedicated obfuscated peer-message listener.
- `ListenerHandler` decodes the initial obfuscated peer-message frame on obfuscated listeners.
- `MessageConnection` reads and writes obfuscated peer-message frames when the connection is marked obfuscated.
- `PeerConnectionManager` can prefer a cached compatible obfuscated endpoint while racing regular direct and indirect paths.

Expected use:

```csharp
var options = new SoulseekClientOptions(
    listenPort: 2234,
    peerObfuscationOptions: new PeerObfuscationOptions(
        enabled: true,
        listenPort: 2235,
        preferOutbound: true));
```

Legacy impact:

- Legacy peers can still connect to the regular peer-message port.
- If a legacy peer ignores obfuscation metadata, no compatibility issue is introduced.
- If an obfuscated outbound attempt fails, regular direct or indirect connection setup can still succeed.

Security and safety notes:

- Obfuscation hides the peer-message frame shape from casual inspection, but it does not authenticate peers, encrypt payloads, hide IP addresses, or protect file-transfer traffic.
- Obfuscated listener input is decoded only after validating the advertised obfuscated frame length.
- Outbound obfuscated connection preference never disables indirect connection fallback.
- The runtime requires regular-port advertisement when obfuscation is enabled; callers cannot use this implementation to create an obfuscated-only Soulseek client.

## Interest and Recommendation APIs

The fork exposes server protocol messages that were defined in Soulseek protocol references but not previously surfaced as client APIs.

Client APIs:

- `AddInterestAsync(string item)`
- `RemoveInterestAsync(string item)`
- `AddHatedInterestAsync(string item)`
- `RemoveHatedInterestAsync(string item)`
- `GetRecommendationsAsync()`
- `GetGlobalRecommendationsAsync()`
- `GetUserInterestsAsync(string username)`
- `GetSimilarUsersAsync()`
- `GetItemRecommendationsAsync(string item)`
- `GetItemSimilarUsersAsync(string item)`

Returned models:

| Model | Contents |
| ----- | -------- |
| `Recommendation` | Raw item string plus server-provided integer score |
| `RecommendationList` | Recommended and unrecommended item collections |
| `UserInterests` | Username plus liked and hated raw interest strings |
| `SimilarUser` | Username plus server-provided similarity rating |
| `ItemRecommendations` | Requested item plus recommendation collection |
| `ItemSimilarUsers` | Requested item plus similar username collection |

Implementation pieces:

- Outgoing request messages encode the corresponding server message codes.
- Incoming response parsers materialize typed results: `RecommendationList`, `UserInterests`, `SimilarUser`, `ItemRecommendations`, and `ItemSimilarUsers`.
- Waiter keys for user and item responses are case-normalized so a server response with different casing can still complete the request.
- Repeated response counts are validated by `ProtocolCountReader` before allocation and parsing.

Application guidance:

- Treat item values as Soulseek discovery/search seeds, not verified artist, release, or recording identifiers.
- Rate-limit UI or automation calls at the application layer. The runtime exposes protocol commands but does not decide user-facing request budgets.
- Cache or lazy-load user-interest lookups when rendering large user lists; each lookup is a server request.

Legacy impact:

- These messages are sent only when called by the application.
- Existing search, browse, transfer, room, and private-message flows are unchanged.

## Multi-user Private Messages

The fork exposes the Soulseek `MessageUsers` command:

```csharp
await client.SendPrivateMessageAsync(new[] { "alice", "bob" }, "hello");
```

Implementation details:

- Null, empty, and whitespace-only usernames are rejected.
- Recipients are deduplicated case-insensitively.
- A call is capped at 100 unique recipients to avoid oversized packets and accidental mass sends.
- The single-user `SendPrivateMessageAsync(string, string, CancellationToken?)` path is unchanged.
- The runtime sends one `MessageUsers` command. It does not create application-level message history rows; callers that keep local history should write one outbound row per recipient after success.

Legacy impact:

- This command is sent only when the multi-user overload is called.
- The regular single-recipient private-message command remains available and unchanged.

## Room Creation Failure Handling

The fork handles server `CannotCreateRoom` responses.

Implementation details:

- The server response is parsed as the failed room name.
- The pending join-room waiter is completed with a `RoomException`.
- Callers get an explicit failure instead of waiting for timeout behavior.

Legacy impact:

- Join-room request encoding is unchanged.
- This is passive handling of a server response already present in the protocol.

## Parser Hardening

New parsers validate repeated collection counts before reading items.

Validation behavior:

- Negative counts throw `MessageException`.
- Counts that cannot physically fit in the remaining payload throw `MessageException`.
- Successful parses return read-only collections without extra list copies.

This protects the runtime from malformed server responses and avoids silent acceptance of partial or contradictory data.

## First Next Feature Decisions

The first next tranche after syncing upstream is implemented in priority order.

1. Versioned slskdN peer capability handshake

`PeerCapabilityEnvelope` reserves custom peer-message code `0x534C534B` and carries a version, message type, nonce, and descriptor. The built-in handler is registered by `SoulseekClient`, so applications do not need to wire the parser manually. The code is reserved from the public custom-message handler API to keep runtime negotiation stable. Sending is opt-in through `SendPeerCapabilityAsync`; inbound `Hello` messages are recorded and acknowledged when a local descriptor has been configured.

2. Runtime peer capability registry

`PeerCapabilityRegistry` stores the latest descriptor seen per Soulseek username with case-insensitive lookup and update events. `SoulseekClient.PeerCapabilities` exposes the registry and `PeerCapabilityReceived` exposes the same update stream from the client. The decision is intentionally username-keyed because the Soulseek protocol identity available during peer-message exchange is username-first; future overlay peer IDs live inside the descriptor rather than replacing the protocol identity.

3. Wishlist scheduler using `WishlistInterval`

`WishlistSearchScheduler` runs explicit wishlist searches for caller-provided terms using `SearchScope.Wishlist`. It uses the server-provided `ServerInfo.WishlistInterval` when available, with a minimum interval guard and optional override for controlled tests or applications that need stricter pacing. The scheduler does not persist wishlist terms and does not auto-start; applications opt in.

4. Signed descriptor primitives

Descriptors are signed over a canonical binary form that excludes the signature itself. `Ed25519PeerDescriptorSigner` uses BouncyCastle's netstandard-compatible Ed25519 implementation and derives a stable peer id from the public key. Verification is a primitive, not an authorization decision; callers can decide whether unsigned descriptors are acceptable for their deployment.

5. Search/rendezvous helper layer

`MeshRendezvousService` wraps the existing interest and similar-user protocol as a discovery helper. It registers the public `slskdn-mesh-v1` interest tag, gets similar users, and can opportunistically probe discovered users for capability descriptors. Probing is disabled by default so the helper remains passive unless the application chooses active discovery.

6. Distributed-network interoperability audit

No new distributed wire format was added. The implementation keeps upstream-compatible distributed message handling and uses the normal peer-message channel for slskdN capability negotiation. This avoids coupling overlay discovery to parent/child distributed search routing and preserves compatibility with clients that only understand the public Soulseek distributed network.

7. Full parser count-hardening pass

The shared `ProtocolCountReader` now supports server and peer message readers. The pass covers high-risk repeated-count parsers used by search responses, browse responses, folder contents, room joins, room lists, private room user lists, and room tickers. Existing missing-data behavior is preserved where tests depended on `MessageReadException`; impossible counts and mismatched parallel room lists fail closed with `MessageException`.

8. Documentation and test coverage

The implementation has focused unit coverage for capability envelope round trips, registry updates, Ed25519 signing and verification, rendezvous probing, wishlist scheduling, and parser hardening. Existing distributed-network tests remain the compatibility guard for the upstream message path.

## License Notes

The upstream merge did not change this fork's declared package license: `GPL-3.0-only` remains in the project metadata and packaged `LICENSE`/`NOTICE` files remain included.

The new Ed25519 implementation adds `BouncyCastle.Cryptography` `2.6.2`. NuGet metadata lists it under the MIT license, which is compatible with this GPL-3.0-only distribution model; it does not require changing the fork license. No credential material or generated secrets are committed by these changes.
