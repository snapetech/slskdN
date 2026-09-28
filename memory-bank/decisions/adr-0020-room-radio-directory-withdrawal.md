# ADR-0020: Room-Owned Radio Directory Withdrawal

**Status**: Accepted  
**Date**: 2026-09-28

## Context

Accepted room state could change while its previous radio announcement remained
listed. An older directory refresh could restore that entry after cleanup.
Separate rooms also performed unsynchronized read/modify/write operations on the
same DHT index. Removing arbitrary request IDs could erase another room's entry.

## Decision

After successful room-message storage, withdraw only the previous listed state
owned by that room. Normalize Stop to the current room identity before storing
its message. Keep withdrawal suppression for the announcement's 15-minute
lifetime, with pruning during existing work and no idle timer.

Track pending index cleanup by room separately from suppression. A failed write
retains pending cleanup; a successful index update clears that room's pending
work. Ordinary private playback then needs no directory request. Serialize index
mutations from this service with one owned semaphore and release it through DI
disposal. Every write filters retained local withdrawals. Refresh application
rechecks current ownership after each asynchronous read and preserves current
local announcements.

## Consequences

Unlist, replacement, Stop, retry and relist remain explicit actions. Storage
rejection preserves prior state. Cleanup cannot delete another room's listing
merely because its ID was supplied in a request. Directory metadata still
expires naturally; withdrawing from the index does not require a new DHT delete
operation or a periodic peer scan.

This gate orders one server's writers. The global DHT index still lacks
cross-node compare-and-swap and authenticated distributed conflict resolution.
A server restart also loses in-memory room ownership and withdrawal state;
server leases and durable recovery remain separate player work.
