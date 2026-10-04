# ADR-0029: Host Lease Expiry Cleanup

**Status**: Accepted
**Date**: 2026-10-04

## Context

ADR-0024 fenced host updates with a bounded lease but only pruned expired
ownership records during later host starts. Dropping that record did not clear
the listening-party room snapshot, tell connected listeners or mesh peers that
the broadcast ended, or withdraw the room's radio announcement and DHT index
entry. Browser document close also has no reliable opportunity to finish a
normal request.

## Decision

Create one one-shot `TimeProvider` timer for each active host lease. When it
expires, enter the same per-room publication queue used by host updates and
publish a Stop for the current matching party. This runs the existing state,
subscriber, mesh, and directory-withdrawal transition. If transient storage or
queue work fails, retry cleanup with a bounded one-shot delay while the same
lease remains expired. Replaced, stopped, renewed, and disposed leases cancel
or reschedule their timer.

The browser also sends a small authenticated `fetch` with `keepalive` on
`pagehide` when it has an acknowledged host snapshot. This is best effort; the
server lease remains authoritative. Do not add a periodic idle sweeper.

## Consequences

Room state and listed directory ownership are withdrawn at host lease expiry
even if the browser disappears. Normal document close usually sends Stop sooner
and releases the lease immediately. A process crash still relies on DHT TTL for
remote directory expiry because the process-local timer cannot run after the
crash. Delivery to offline remote peers remains subject to normal mesh routing
availability.

Tests advance a deterministic `TimeProvider` to lease expiry and verify Stop
storage, state removal, peer delivery and index withdrawal. Browser tests verify
the pagehide request carries the acknowledged room identity and host-session
fence with keepalive enabled.
