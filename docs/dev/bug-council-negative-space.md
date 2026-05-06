# slskd Bug Council Negative-Space Gate

This document declares slskd's trust boundaries and the validator each one must run, so a missing validator is itself a CI failure. The gate is enforced by `scripts/check-council-negative-space.sh`.

The candidate scanner finds **call sites that exist**. It cannot find **call sites that should exist but don't** — a new public boundary added without a validator. This list inverts the search: each row names a boundary, and CI asserts the validator is in place.

slskd already runs many topic-specific check scripts (`check-controller-csrf.sh`, `check-anonymous-endpoints.sh`, `check-path-containment.sh`, etc). The negative-space gate complements those by recording the **boundaries** themselves as a single declarative list, instead of leaving each guard's existence implicit in a separate script.

## Boundaries

| Boundary | Source | Sink file(s) | Required validator |
| --- | --- | --- | --- |
| Mutating API endpoints | HTTP from clients | `src/slskd/**/API` and `src/slskd/**/*Controller.cs` | `[Authorize]`, explicit write roles, and CSRF token validation |
| Anonymous-friendly endpoints | HTTP from clients | `src/slskd/**/API` and `src/slskd/**/*Controller.cs` | `[AllowAnonymous]` allowlist must match `check-anonymous-endpoints.sh` |
| Shared-file path resolution | Configuration + caller paths | `src/slskd/**/API` and file/path services | path containment helpers per `check-path-containment.sh` |
| Outbound HTTP fetches | Application code | `src/slskd` | guarded factory per `check-outbound-http-guards.sh` |
| Durable state writes | Application code | `src/slskd` | atomic-write helpers per `check-durable-state-atomic-writes.sh` |
| Soulseek runtime crossings | Embedded slskNet.Runtime | `src/slskd/Soulseek` | event/handler observation per `check-async-task-observation.sh` |

## Adding a new boundary

1. Add a row to the table above.
2. Add an `assert_validator_present` line to `scripts/check-council-negative-space.sh`.
3. Add a behavior-pinned test per `docs/dev/bug-council-behavior-pinning.md`.

## Removing a boundary

Removing a row requires a council sweep entry explaining why the boundary no longer exists. The remediation baseline must be updated in the same change.

## Relationship to slskd's existing checks

The existing topic-specific scripts each enforce one boundary's invariants in detail. The negative-space gate is the **inventory** of boundaries — the master list. If a script disappears or stops being run, the negative-space gate still asserts that the validator symbol is in place at the sink. The two layers protect against different failure modes: scripts catch behavior drift, the negative-space gate catches "I deleted the validator and the script that called it in the same change."
