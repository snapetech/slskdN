# Bug Council Negative-Space Gate

The candidate scanner finds **call sites that exist**. It cannot find **call sites that should exist but don't** — for example, a new public boundary that takes untrusted input but never calls a validator. This document declares the runtime's trust boundaries and the validator each one must run, so a missing validator is itself a CI failure.

The gate is enforced by `scripts/check-council-negative-space.sh`, which is invoked from `scripts/check-remediation-baseline.sh`.

## Boundaries

A boundary is a code seam where data crosses from a less-trusted source into the runtime. For every boundary, this document records:

- **Source** — where the data comes from.
- **Sink file(s)** — the file(s) where the boundary is implemented.
- **Required validator** — a symbol that must appear in the sink file. The symbol's presence does not prove correctness; it proves the developer thought about the boundary. Behavior is pinned separately by `docs/dev/bug-council-behavior-pinning.md`.

| Boundary | Source | Sink file(s) | Required validator |
| --- | --- | --- | --- |
| Server message frames | TCP from server | `src/Network/MessageFrameValidator.cs` | `ValidateMessageLength`, `MaxMessageLength` |
| Init frames | TCP from peer | `src/Network/MessageFrameValidator.cs` | `ValidateInitMessageLength`, `MaxInitMessageLength` |
| Buffered network reads | TCP | `src/Network/Tcp/Connection.cs` | `MaximumBufferedReadLength` |
| Server protocol counts | Server message body | `src/Messaging/Messages/Server/ProtocolCountReader.cs` | `ReadValidatedCount` |
| Peer transfer file sizes | Peer message body | `src/Messaging/Messages/Peer` | `Invalid transfer file size` |
| Server endpoint ports | Server message body | `src/Messaging/Messages/Server` | `ValidatePort` |
| Distributed branch metadata | Distributed message body | `src/Messaging/Messages/Distributed/DistributedBranchLevel.cs` | `branch level` |
| Distributed child depth | Distributed message body | `src/Messaging/Messages/Distributed/DistributedChildDepth.cs` | `child depth` |
| Resolver outputs | Application code | `src/Messaging/Handlers/PeerMessageHandler.cs` | `WriteRaw` (raw response handler) |
| CSL0003 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToStreamPositionAnalyzer.cs` | `CSL0003` |
| CSL0004 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToFilePathAnalyzer.cs` | `CSL0004` |
| CSL0005 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToTimeoutAnalyzer.cs` | `CSL0005` |
| CSL0006 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToEndpointAnalyzer.cs` | `CSL0006` |
| CSL0007 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToEnumAnalyzer.cs` | `CSL0007` |
| CSL0008 analyzer lens | Protocol reader taint | `analyzers/Soulseek.CouncilAnalyzers/TaintToStringSliceAnalyzer.cs` | `CSL0008` |

## Adding a new boundary

When a new public surface accepts untrusted input:

1. Add a row above with the boundary, the file(s) it lives in, and the validator symbol you've placed in those file(s).
2. Add a PAIR of lines to `scripts/check-council-negative-space.sh`:
   - `assert_validator_present "<boundary>" "<sink>" "<symbol>"` — catches a deleted validator.
   - `assert_baseline_anchor "<boundary>" "<symbol>"` — catches a silently-removed remediation gate. Both halves are required.
3. Add a behavior-pinned test per `docs/dev/bug-council-behavior-pinning.md`.

## Why two halves

The earlier single-half version of this gate was itself a council bug: a maintainer could delete the matching `require_pattern` line in `scripts/check-remediation-baseline.sh` while the validator symbol survived, and this gate would still pass — silently weakening the fix gate without anyone noticing. The strengthened gate requires both the symbol AND a baseline anchor referencing the same name, so a half-removal fails CI on the missing half.

## Removing a boundary

Removing a row requires a council sweep entry explaining why the boundary no longer exists (refactored away, code deleted, source moved to trusted). The remediation baseline must be updated in the same change.

## Why this matters

Most of the council's catches in 2026-05 were of the shape "a guard exists for boundary A, was forgotten for boundary B." The negative-space gate inverts the search: instead of sweeping all call sites for missing guards, it lists every boundary by name and asserts the guard symbol is in place. That makes "I added a new boundary and forgot to think about it" the failure mode that's hardest to commit.
