# Soulseek Type-1 Obfuscation

slskdN treats Soulseek type-1 peer-message, distributed-message, and file-transfer obfuscation as a first-class feature option. The option defaults on in `compatibility` mode, so regular Soulseek paths remain available and obfuscated reachability is added for compatible clients. The option is intentionally conservative: it is configurable, validated, visible in the Network tab, and keeps legacy-client fallback enabled.

## What We Know

The research shows enough to design and expose the feature:

- The public server accepts and returns obfuscation type and obfuscated-port metadata.
- Type-1 obfuscated peer-message streams can be accepted by an obfuscated listener.
- Direct obfuscated peer-message connections can succeed across separate public endpoints.
- Indirect peer-message connection flow can carry enough metadata for the target to choose the obfuscated port.
- Distributed-network message streams use the same listener bootstrap shapes (`PeerInit` and `PierceFirewall`) and one-byte message-code framing, so they can use type-1 obfuscation when wrapped in an obfuscated distributed connection.
- Obfuscated-only reachability can work between compatible implementations, but slskdN does not enable that posture while broad legacy compatibility is the default.

This is stronger than a local-only prototype. It is enough to justify product support for compatibility and prefer modes while reserving only mode for a later explicit compatibility break.

## Current Runtime Status

slskdN’s vendored runtime exposes the wire path:

- SetListenPort obfuscation type and obfuscated-port advertisement.
- Type-1 obfuscated peer/distributed/transfer listener support.
- Type-1 obfuscated outbound peer/distributed/transfer dialing.
- Type-1 obfuscated distributed-message listener support for direct child adoption and solicited `PierceFirewall` handoff.
- Type-1 obfuscated outbound distributed-message dialing for compatible direct parent candidates and compatible indirect child connections.
- Obfuscation fields on peer-address and indirect-connect responses.

When enabled, slskdN reports type-1 obfuscation as `active`. By default (`obfuscation.listen_port: 0`), obfuscated connections share the same TCP socket as `soulseek.listen_port` rather than requiring a second bound port: on each accepted connection, the listener reads the first bytes and tests whether they form a plausible plain init-frame length; if not, it treats them as the start of an obfuscated frame instead. This mirrors how DHT, mesh overlay control, and QUIC already share one public UDP port (see `docs/DHT_RENDEZVOUS_DESIGN.md`). Setting `obfuscation.listen_port` to an explicit nonzero value instead binds a second, dedicated TCP listener for obfuscated connections, as before.

## Modes

`compatibility` mode is the broad-client default. It advertises regular and obfuscated reachability together. This mode must not block or replace normal peer-message or distributed-message paths.

`prefer` mode is the enhanced posture. It prefers type-1 obfuscated outbound peer-message, distributed-message, and file-transfer dials when the peer advertises compatible metadata and keeps regular fallback for other clients.

`only` mode is reserved. The current runtime rejects obfuscated-only advertising because slskdN preserves regular paths for legacy clients.

## Configuration

```yaml
soulseek:
  listen_port: 50300
  obfuscation:
    enabled: true
    mode: compatibility
    listen_port: 0
    advertise_regular_port: true
    prefer_outbound: true  # only changes outbound priority when mode is prefer
```

CLI and environment equivalents are documented in `docs/config.md`.

## Network Health Rules

Type-1 obfuscation is enabled for Soulseek peer-message (`P`), distributed-message (`D`), and file-transfer (`F`) streams. Implementations must preserve regular fallback in `compatibility` and `prefer` modes and rate-limit connection retries. Compatibility mode keeps regular outbound dials first. Prefer mode can add an obfuscated direct candidate when compatible metadata is known. If an obfuscated distributed or transfer candidate connects first but fails setup negotiation, the runtime keeps the regular candidates alive and falls back before failing the operation.

File transfer (`F`) streams can use type-1 obfuscated framing when compatible metadata is available. Regular transfer paths remain advertised and available, so legacy clients that do not support obfuscation continue to use normal Soulseek transfers.

The feature is not encryption. It should not be described as anonymous, secure, or confidential transport. The correct description is obfuscated peer/distributed/transfer connectivity for compatible peers with regular fallback.

## Adjacent Mesh Privacy Work

slskdN also has mesh-DHT and overlay transport privacy controls outside native Soulseek type-1 obfuscation. Mesh routing can now consult the configured anonymity/obfuscated transport selector without opening a throwaway connection, prefer overlay routing when Tor, I2P, WebSocket tunnel, HTTP tunnel, obfs4, or meek is selected, and fall back to normal mesh routing when none of those transports are available.

This is not the same as Soulseek type-1 obfuscation. It applies to slskdN mesh-DHT and overlay paths, not to the official Soulseek server socket. The public BitTorrent DHT remains a separate endpoint-rendezvous path.

Metadata minimization is part of this posture: bridge searches, bridge downloads, DHT store logs, and remote metadata-search logs should use sanitized fingerprints, short identifiers, or basename-only values rather than raw searches, full filenames, full paths, or full peer identifiers.

## Deferred Server Transport Wrapping

The official Soulseek server connection remains direct unless an operator supplies an external network wrapper such as a VPN. A future slskdN-managed SOCKS or pluggable-transport wrapper could be useful for local-network DPI resistance, but it would not hide activity from the Soulseek server and it must be designed separately from native `P`, `D`, and `F` type-1 support.

## Validation Work

Runtime support is active. `ObfuscatedConnectionMatrixTests` uses loopback TCP sockets to prove obfuscated peer-message (`P`), distributed-message (`D`), and file-transfer (`F`) paths, plus regular peer-message, distributed-message, and transfer fallback. Manager-level tests also cover obfuscated inbound transfer handoff, inbound indirect transfer fallback, outbound transfer preference/fallback, distributed parent preference/fallback, mesh private/anti-DPI transport selection, and sanitized metadata logging. Ongoing validation should still include public-server advertisement tests, direct compatible-peer tests, indirect compatible-peer tests, and negative tests proving plain traffic is rejected by the obfuscated listener.
