# ADR-0028: Reserve Listening-Party IDs Before Publishing

## Status

Accepted — 2026-10-04

## Context

Listed radio announcements are stored under a DHT key derived only from
`PartyId`, while local room state is keyed by pod and channel. Reusing an
explicit party ID in another active room could overwrite the directory record
and make party-ID lookups select an arbitrary room. Same-server publication
queues serialize each room separately, so they do not prevent two rooms from
claiming one ID at the same time.

The DHT client exposes reads and writes but no compare-and-swap or authenticated
publisher ownership. A read-before-write check can detect an existing foreign
room record, but two nodes can both observe an empty key and race to publish.

## Decision

Reserve each newly listed party ID under the directory-state lock before
storing its room message. Reject an ID already owned by another active listed
room or host peer in local or observed remote state, and release a provisional
reservation if validation, DHT preflight, or storage fails. Keep the
reservation through local state commit so concurrent room publications cannot
both accept it. Private unlisted playback does not claim directory identity.

When a room first lists or relists an ID, read its announcement key before
message storage. Reject a non-expired record owned by another pod, channel, or
host peer. Unlisting an existing room keeps that room's accepted party ID. Do
not add DHT reads to ordinary updates of an already-listed party. If the
directory read fails, reject the new listing with a retryable service error;
private room playback remains independent of the directory.

Return HTTP 409 with `party_id_in_use` for observed ownership conflicts. Keep
the current routes and ID format. Treat incoming remote room snapshots with
duplicate active IDs as ignored. Do not claim global race-free allocation until
the DHT provides atomic conditional writes and verifiable ownership.

## Consequences

- Active listed party IDs are unique across room and host owners observed by
  one process; private unlisted playback remains independent.
- Existing cross-node collisions are rejected when visible during preflight.
- Simultaneous first claims on separate nodes can still race; this is an
  explicit distributed-systems limitation, not a uniqueness guarantee.
- Starting a new listed identity requires a successful DHT ownership read.
  Subsequent updates reuse the accepted local owner without another read.
