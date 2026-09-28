# ADR-0025: Authenticated Cross-Node Listen Along State

**Status**: Accepted
**Date**: 2026-09-28

## Context

The Listen Along experience could follow a room hosted by the same server, but
generic pod-message routing did not provide a reliable authenticated delivery
path between mesh nodes. A remote state event must be attributable to the
connected peer, checked against current pod membership, delivered to listeners
on the receiving server, and prevented from being routed back into the mesh.
Radio directory availability must not gate room-state delivery.

## Decision

Use the authenticated mesh service client for a dedicated `pods.ApplyListenAlong`
call. The sender targets active, unbanned pod members other than itself. Routing
is limited to eight concurrent calls, one second per peer, and a two-second
total fan-out budget. A timeout or unavailable peer is reported in the routing
result and does not roll back the host's committed room state.

The receiver verifies that the event sender matches the authenticated transport
peer, rechecks active membership and the channel, validates the bounded metadata
and event ordering, then stores and applies the snapshot. Applied remote state is
kept in a bounded process-local cache and sent to authorized local SignalR
subscribers. It is never re-routed. Local subscribers are notified after the
host state commit; authenticated room delivery runs before optional radio
directory work.

## Consequences

Play, Pause, Seek and Stop now reach active followers across mesh nodes without
including audio bytes in room messages. The receiver remains authoritative for
membership revocation. Slow or offline members cannot hold local room updates
behind unbounded fan-out, and a radio directory failure no longer blocks room
delivery.

Remote snapshots are ephemeral process state, capped at 256 rooms; up to 256
room buckets retain at most 16 retired party IDs each to reject stale updates.
They are not reconstructed from stored messages after restart. There is no
offline delivery queue or automatic replay when a mesh member reconnects.
Current-node recovery remains the existing SignalR snapshot/rejoin path.

The two-node Playwright regression uses identical SHA-256 media identities on
both nodes and verifies real playback, Pause, Seek, Stop, and denial after a
member is banned. Unit tests cover sender authentication, membership, ordering,
local-before-remote-before-directory sequencing, and the aggregate fan-out
budget.
