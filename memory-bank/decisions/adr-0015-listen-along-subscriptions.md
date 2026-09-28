# ADR-0015: Listen-Along Subscriptions

## Context

SignalR groups authorize no subsequent publication. Checking pod membership at
JoinParty alone lets a connection continue receiving room metadata after a ban
or removal. Membership is mutable, and disconnected/reconnecting clients must
not retain stale access. See ADR-0001 gotchas 0z1099 and 0z1100.

## Decision

Keep connection/room subscriptions in the existing singleton listening-party
service. Store only connection, pod, channel, authenticated peer and admin role.
Cap the registry at 4,096 subscriptions and 16 rooms per connection; repeated
joins are idempotent. Explicit leave and hub disconnect remove subscriptions.

For each publication, fetch current pod members once when non-admin recipients
exist. Send state only to still-subscribed administrators or current unbanned
members. Remove denied subscriptions and send an access-revoked notification
without room metadata. No polling or additional peer discovery is introduced.
The UI latches revocation until an authorized rejoin and ignores stale events.

## Consequences

Membership is checked on delivery, rather than only at join. Recipient checking
adds one membership lookup per live publication, independent of recipient count.
Idle rooms do no subscription work. Revocation takes effect before the next
publication; this is not a promise of atomic ordering with concurrent membership
writes. Network transport reconnect must join again and refresh its snapshot.
Cross-node room state propagation and elapsed radio renewal remain separate
open work; local subscription delivery does not prove them.

## Publication ordering — 2026-09-28

Serialize complete publications per pod/channel through storage, directory,
routing and fanout. Normalize sequence and timestamp after acquiring that
room's gate. Limit active room queues to 256 and reservations per room to 16;
reject excess updates with HTTP 429 and explicit retry feedback. Cancellation
and failure release reservations, and idle gates are removed and disposed.
Unrelated rooms retain independent publication progress. This avoids stale
Play arriving after Stop without global serialization or retained tombstones.

## Publication storage boundary — 2026-09-28

Resolve the current room through IPodService in the publication scope, validate
the message contract and require accepted storage before changing visible
snapshot, directory or now-playing state. Missing rooms return 404; rejected
storage returns a stable retryable 503. Failed Play and Stop retain the prior
broadcast. This guarantees the local storage boundary; it does not guarantee
remote routing success or distributed state application.
