# Bug Council Active Backlog

This backlog is the durable handoff for `scripts/run-council-active-bughunt.sh`.
A green all-phases council run is not proof that no bugs exist; this file
records the active discovery piles that still need review, splitting, or
burn-down.

Every active-bughunt section must have a row below with the current candidate
count. `scripts/check-council-active-backlog.sh` fails when a section is missing,
left `Untriaged`, or has a stale count.

Status meanings:

- `Open` - broad queue still needs classification or narrower subgroup probes.
- `Guarded` - narrow probe is empty and protected by remediation checks.
- `Accepted` - confirmed bug class exists and is being fixed.
- `Existing guard` - candidates are covered by existing behavior and gates.
- `False positive` - scanner shape is not a bug for the listed rationale.
- `Out of scope` - candidate belongs outside this runtime council.

## Commit Wording

Fix commits must describe the runtime change, bug class, or user-visible
hardening. Do not mention council, bughunt, scanners, agents, or other discovery tooling in commit messages. The ledger and process docs can record
how a bug was found; commit history should read as normal maintenance and fix
history.

| Section | Candidate count | Status | Current classification | Next action |
| --- | ---: | --- | --- | --- |
| `Event-style async boundaries` | 10 | Open | Event-handler and timer callbacks remain a broad lifecycle queue. Several known handler paths are already diagnostic-wrapped, but this pile needs a whole-section pass rather than one callback at a time. | Split into event-handler, timer, and disposal subgroups; accept only paths where exceptions can escape without diagnostics or leave state half-updated. |
| `Silent catch or lossy exception boundaries` | 6 | Open | Remaining empty catches are listener initialization cleanup paths that intentionally avoid masking the already-diagnosed initialization failure. The distributed parent reconnect swallow was accepted and fixed as RT-132. | Keep a narrow cleanup-catch gate; accept only future empty catches that hide non-cleanup runtime failures. |
| `Callback/event invocation boundaries` | 137 | Open | Broad callback inventory. Dedicated unisolated event probes below are guarded; remaining hits include option delegates, diagnostics, helper wrappers, and lower-level callback boundaries. | Split into caller-owned option delegates, diagnostic forwarding, low-level listener callbacks, and remaining public events; burn down accepted groups in batches. |
| `Unisolated server handler event invocations` | 0 | Guarded | Dedicated probe is empty after server handler event-boundary hardening. | Keep remediation baseline gate. |
| `Unisolated message connection event invocations` | 0 | Guarded | Dedicated probe is empty after message connection callback isolation. | Keep remediation baseline gate. |
| `Unisolated TCP connection event invocations` | 0 | Guarded | Dedicated probe is empty after TCP connection callback isolation. | Keep remediation baseline gate. |
| `Unisolated client lifecycle event invocations` | 0 | Guarded | Dedicated probe is empty after client lifecycle callback isolation. | Keep remediation baseline gate. |
| `Unisolated client search event invocations` | 0 | Guarded | Dedicated probe is empty after client search callback isolation. | Keep remediation baseline gate. |
| `Unisolated client transfer/browse event invocations` | 0 | Guarded | Dedicated probe is empty after client transfer and browse callback isolation. | Keep remediation baseline gate. |
| `Unisolated SoulseekClient bridge event invocations` | 0 | Guarded | Dedicated probe is empty after client constructor bridge isolation. | Keep remediation baseline gate. |
| `Remote/user text in diagnostics or HTTP errors` | 182 | Open | Broad privacy/logging queue. Some runtime diagnostics intentionally include remote usernames or local filenames; example Web API console output has different risk and should be reviewed separately. Runtime transfer filename and search text subgroups have been accepted and fixed. | Split runtime diagnostics from example Web API output; accept only high-confidence sensitive token, full path, raw query, or protocol-secret leaks. |
| `Public mutable ownership surfaces` | 93 | Open | Broad ownership queue. Many candidates are immutable snapshots or test data; compression internals and public collection shapes need separate treatment. | Split public array snapshots, public collection properties, `params`/constructor captures, and test-only rows; burn down mutable production ownership leaks. |
