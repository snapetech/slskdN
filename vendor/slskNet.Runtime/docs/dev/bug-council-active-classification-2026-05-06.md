# Bug Council Active Classification - 2026-05-06

Source report: `.council/active-bughunt.md`

This register is the automated burn-down handoff for the current active piles.
A green all-phases run is not proof of no bugs; this register records which
active pile rows were classified, which were accepted as bugs, and which
follow-up gate owns the accepted class.

## Classification Rules

- `Accepted` rows must have a ledger entry, code fix, regression test, and
  remediation-baseline gate before closure.
- `Existing guard` rows are allowed only when a current regression gate already
  protects the exact behavior.
- `Open subgroup` rows stay in the active backlog and must be split by a future
  council cycle.
- `False positive` rows must name why the scanner shape is intentionally noisy.

## Accepted Rows

| ID | Active pile | Evidence | Severity | Confidence | Status |
| --- | --- | --- | --- | --- | --- |
| RT-129 | Callback/event invocation boundaries | `DiagnosticFactory` invoked diagnostic event callbacks directly; components that pass `DiagnosticGenerated?.Invoke(...)` could let a throwing diagnostic subscriber interrupt ordinary diagnostic logging and the runtime path that produced it. | Medium | Proven | Fixed |
| RT-130 | Remote/user text in diagnostics or HTTP errors | Transfer diagnostics mostly used basenames but several warning/debug paths logged raw transfer filename values, and `Path.GetFileName` does not strip Soulseek backslashes on every host platform. | Medium | Proven | Fixed |
| RT-131 | Public mutable ownership surfaces | Peer and distributed connection manager inspection properties cloned the collection wrapper but returned live `IPEndPoint` instances for message, child, and parent connections. | Medium | Proven | Fixed |
| RT-132 | Silent catch or lossy exception boundaries | `DistributedConnectionManager.ParentConnection_Disconnected` swallowed parent reconnect failures after clearing parent state, hiding reconnect-loop failures from diagnostics. | Low | Proven | Fixed |
| RT-133 | Remote/user text in diagnostics or HTTP errors | Runtime search diagnostics and wrapped search failures logged raw search text even though token/count metadata is enough to correlate search lifecycle events. | Medium | Proven | Fixed |

## Silent Catch Or Lossy Exception Boundaries

Classification marker: `Silent catch or lossy exception boundaries: 2/2 classified`.

| Candidate | Classification | Rationale | Follow-up |
| --- | --- | --- | --- |
| `src/Network/DistributedConnectionManager.cs:1214` | Accepted | Parent reconnect failures are intentionally non-fatal after a parent disconnect, but they must still be visible in diagnostics after parent state is cleared. | Fixed as RT-132. |
| `src/Network/ListenerHandler.cs:248` | Existing guard | Best-effort disconnect during failed listener initialization; the failure path already emits `Failed to initialize direct connection...` before cleanup. | Covered by listener handler diagnostic-boundary tests. |
| `src/Network/ListenerHandler.cs:257` | Existing guard | Best-effort dispose during failed listener initialization; throwing from cleanup would mask the real initialization failure. | Covered by listener handler cleanup failure regression tests. |

## Event-Style Async Boundaries

Classification marker: `Event-style async boundaries: 10/10 classified`.

| Candidate | Classification | Rationale | Follow-up |
| --- | --- | --- | --- |
| `DistributedConnectionManager.RemoveAndDisposeAll` | Open subgroup | Public fire-and-forget cleanup remains shaped as `async void`; current per-connection cleanup catches awaited connection acquisition failures. | Split public cleanup API shape from handler callbacks. |
| `DistributedConnectionManager.ParentConnection_Disconnected` | Open subgroup | Event handler contains a non-fatal reconnect attempt; accepted only if future review finds missing diagnostics or state corruption. | Review with reconnect-background diagnostics. |
| `DistributedConnectionManager.StatusDebounceTimer_Elapsed` | Existing guard | Timer callback catches and diagnoses update failures. | Covered by distributed status debounce failure tests. |
| `ServerMessageHandler.HandleMessageRead` | Existing guard | Message handler wraps message dispatch in diagnostics and direct public events have been isolated by RT-118/RT-121. | Covered by server handler event-boundary tests. |
| `PeerMessageHandler.HandleMessageRead` | Existing guard | Peer handler catches message-resolution failures and accepted resolver-output bugs are already behavior-pinned. | Covered by resolver-output and peer handler tests. |
| `DistributedMessageHandler.HandleChildMessageRead` | Existing guard | Distributed child handler catches and diagnoses message processing failures. | Covered by distributed handler diagnostics tests. |
| `DistributedMessageHandler.HandleMessageRead` | Existing guard | Distributed handler catches and diagnoses message processing failures. | Covered by distributed handler diagnostics tests. |
| `DistributedMessageHandler.HandleEmbeddedMessage` | Existing guard | Embedded distributed handler catches and diagnoses message processing failures. | Covered by distributed handler diagnostics tests. |
| `PeerConnectionManager.RemoveAndDisposeAll` | Open subgroup | Public fire-and-forget cleanup remains shaped as `async void`; current per-connection cleanup catches awaited connection acquisition failures. | Split public cleanup API shape from handler callbacks. |
| `ListenerHandler.HandleConnection` | Existing guard | Adapter delegates into `HandleConnectionAsync`, whose body catches initialization failures and stays inside a diagnostic boundary. | Covered by listener handler diagnostic-boundary tests. |

## Callback/Event Invocation Boundaries

Classification marker: `Callback/event invocation boundaries: accepted diagnostic callback subgroup`.

The accepted subgroup is `DiagnosticFactory` callback isolation. Remaining
callback rows stay open in `docs/dev/bug-council-active-backlog.md` because the
pile still mixes caller-owned option delegates, diagnostic forwarding, helper
wrappers, and lower-level transport callbacks.

## Remote/User Text In Diagnostics Or HTTP Errors

Classification marker: `Remote/user text in diagnostics or HTTP errors: accepted transfer filename and search text subgroups`.

The accepted subgroups were transfer filename/path diagnostics and raw search
text diagnostics. Follow-up RT-134 corrected that policy: Runtime diagnostics must preserve operator-visible values such as usernames, filenames, paths, and search text so
the operator can troubleshoot their own instance. Only credentials, private
keys, API tokens, and equivalent secret material should be withheld from logs;
ordinary Soulseek activity text should stay visible, with control characters
escaped to prevent forged log lines. Remaining rows stay open in
`docs/dev/bug-council-active-backlog.md` because the pile also includes
protocol tokens, exception messages, and example Web API response text that need
separate policy decisions.

## Public Mutable Ownership Surfaces

Classification marker: `Public mutable ownership surfaces: accepted manager endpoint subgroup`.

The accepted subgroup is connection-manager endpoint snapshots. The broader
pile remains open because it mixes public immutable collections, intentional
raw stream ownership, mutable example DTOs, and already-guarded byte-array/IP
snapshots.
