# ADR-0022: SQLite Membership Event Order

**Status**: Accepted

**Date**: 2026-09-28

## Context

Membership history uses pod, peer and Unix milliseconds as its existing key.
Rapid leave/rejoin or join/ban can collide; clock rollback can reorder history.
A full gate and two deterministic frozen/backward-clock regressions reproduced
rejected rejoin and a unique-constraint failure during ban.

## Decision

Allocate each event timestamp as the maximum of current UTC milliseconds and
the prior persisted timestamp for that pod/peer plus one. Read the prior value
inside the write transaction that commits membership and its history together.
Use the same transaction boundary for join, leave and ban. Keep the existing
schema and HTTP API. Accept an optional TimeProvider, following the listening-party
service pattern, for deterministic clock regressions.

## Consequences

Rapid actions and clock rollback keep unique ordered history across service
instances. A burst can temporarily advance event timestamps beyond wall time.
Different peers have independent sequences; this does not impose cross-peer or
distributed ordering. No database migration is required. Existing membership
signing behavior and moderation policy are unchanged. Persisted history remains
the ordering source; an in-memory sequence would lose it on restart.
