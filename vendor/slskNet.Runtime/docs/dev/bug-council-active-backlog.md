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
| `Event-style async boundaries` | 10 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Silent catch or lossy exception boundaries` | 6 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Callback/event invocation boundaries` | 137 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated server handler event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated message connection event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated TCP connection event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated client lifecycle event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated client search event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated client transfer/browse event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Unisolated SoulseekClient bridge event invocations` | 0 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Remote/user text in diagnostics or HTTP errors` | 184 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Public mutable ownership surfaces` | 93 | Open | Added by council sweep; classify this active-bughunt section for this repo. | Split into narrower subgroups, reject with rationale, or promote accepted bug classes into behavior tests and remediation anchors. |
| `Red-team abuse lens` | 5441 | Open | Required recurring attacker-view review across secrets, identity, redirects, paths, process launch, and downgrade risks. | Turn accepted hypotheses into behavior tests plus remediation anchors; add preservation tests for normal functionality. |
