# ADR-0024: Listed Host Capability Renewal and Session Fencing

**Status**: Accepted
**Date**: 2026-09-28

## Context

Listed-party announcements, their DHT index entry, and the advertised stream
ticket expire after 900 seconds. The persistent player only published on
playback changes, so a long uninterrupted broadcast disappeared from the
directory and its capability expired. A second browser tab could also reuse
the room's observed party ID; the older tab could then overwrite or stop the
replacement host.

ADR-0019 intentionally avoided periodic playback-position publications. That
does not remove the need to maintain an explicitly owned host lease and listed
capability.

## Decision

An active host session owns one cancellable five-minute timer. It renews the
server-side host lease for 30 minutes. When the current room is listed, that
same request republishes its announcement, DHT index TTL, and a fresh 900-second
stream ticket. Renewal does not create a room message or broadcast a position
tick. It remains active while the player is paused or the host disables global
listing, so the room session still has a bounded ownership lease.

Each explicit browser host start creates a fresh cryptographically random
session ID and a new server-assigned party ID. The ID is sent in request headers,
not in room messages or directory metadata. Ordinary host updates, renewals,
and Stop must match both the current session ID and party ID. A new explicit
start may replace the current session. Older sessions receive HTTP 409 and
release their timer and room observer. After a lease expires, the old session
cannot update or renew; Stop remains available only when its current party ID
still matches the room snapshot.

Commit the host fence at the same serialized in-memory room-state transition.
This keeps ownership aligned when later DHT or routing work fails. Each active
lease has one one-shot server expiry callback; there is no periodic idle sweep.
See ADR-0029 for how expiry uses the room's Stop path and retries transient
cleanup failures.

## Consequences

Long-running listed broadcasts continue to refresh their directory entry and
stream capability. Each active browser host adds at most one renewal request
every five minutes. Temporary renewal failures surface through the existing
Retry feedback; authorization loss, expiry, or replacement releases local
ownership. A host that resumes after its 30-minute lease expires must start a
new broadcast.

Fencing is local to the `ListeningPartyService` process and is not durable over
restart. The global DHT index still has no cross-node compare-and-swap or
authenticated distributed owner election. Multi-instance ownership and
cross-node index contention remain separate work.

Unit tests advance service time beyond 900 seconds, verify fresh tickets and
index TTLs, and reject stale publish, renewal, and Stop. A real two-node test
verifies the refreshed directory and cross-node stale-write rejection for two
host sessions on the owning server. Browser-hook tests verify the five-minute
timer and cleanup after supersession.
