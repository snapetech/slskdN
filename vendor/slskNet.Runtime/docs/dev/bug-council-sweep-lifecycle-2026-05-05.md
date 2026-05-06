# Bug Council Sweep - Lifecycle And Cancellation - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Task, cancellation, timer, and semaphore lifecycle candidates`
- `Non-idempotent task completion candidates`
- `Lifecycle task completion and race candidates`
- `Lifecycle cancellation registration candidates`
- `Lifecycle timer and semaphore candidates`
- `Lifecycle fire-and-forget async misuse candidates`

Candidate markers:

- Task, cancellation, timer, and semaphore lifecycle candidates: 211/211 classified
- Non-idempotent task completion candidates: 0/0 classified
- Lifecycle task completion and race candidates: 82/82 classified
- Lifecycle cancellation registration candidates: 59/59 classified
- Lifecycle timer and semaphore candidates: 84/84 classified
- Lifecycle fire-and-forget async misuse candidates: 0/0 classified
- Unclassified candidates: 0

This sweep closes the broad lifecycle scan by splitting task completion/race points, cancellation registration ownership, timer/semaphore lifetime, and fire-and-forget async misuse into stable subgroups. The broad count increased during later burn-down passes because distributed/peer pending-indirect cancellation ownership is now explicit and itself tracked by the cancellation subgroup.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Network/Tcp/Connection.cs:481` | Fixed | RT-076 | `WaitForDisconnect` now scopes cancellation registrations to the wait task and disposes them after completion instead of retaining callbacks for the lifetime of the token source. |
| `src/Network/DistributedConnectionManager.cs:75` | Fixed | RT-078 | Distributed status timer, watchdog, and queued broadcast/status callbacks now run through safe background helpers that report failures through diagnostics instead of dropping fire-and-forget failures. |
| `src/Messaging/Handlers/DistributedMessageHandler.cs:186` | Fixed | RT-079 | Distributed search broadcast fan-out now queues through a diagnostic wrapper so background broadcast failures are observed. |
| `src/Network/PeerConnectionManager.cs:785` / `src/Network/DistributedConnectionManager.cs:700` | Fixed | RT-092 | Shutdown now cancels and disposes pending inbound-indirect cancellation sources instead of clearing the dictionaries and leaving pending attempts unowned. |
| `src/Network/PeerConnectionManager.cs:393` / `src/Network/DistributedConnectionManager.cs:644` | Fixed | RT-093 | Pending inbound-indirect connection registration now cancels superseded sources and removes only the current source, preventing older attempts from erasing newer cancellation ownership. |

## Existing Guards

Classification: Existing guard.

- Runtime `TaskCompletionSource` completion uses `TrySet*` APIs and the baseline rejects non-idempotent `Set*` source calls.
- Non-idempotent task completion currently has zero scan hits; this zero queue is now closed in the sweep-count drift gate so future `Set*` regressions reopen the council instead of bypassing the ledger.
- Transfer enqueue and disconnect races use `Task.WhenAny` with linked cancellation, and upload/download stream paths observe the linked race token.
- `Waiter` owns and disposes cancellation/timeout registrations through `PendingWait.Dispose`.
- `TokenBucket` races reset waits against cancellation with scoped token registrations and releases reset waiters on disposal.
- Scheduler, connection manager, and client timers/semaphores are disposed by their owning lifecycle objects; prior sweeps cover post-dispose wait creation and token-bucket wait release.
- Remaining `async void` hits are event handler entry points that wrap exceptions into diagnostics or manager cleanup paths; they remain tracked by this sweep and existing focused tests.
- Fire-and-forget async misuse currently has zero scan hits after replacing unobserved `ConfigureAwait(false)` calls with diagnostic queue helpers; the remediation baseline rejects regressions in that subgroup.

## False Positives

Classification: False positive.

- XML documentation and property declaration hits in the broad lifecycle section are not independent lifecycle behavior.
- Timer extension method hits are helper definitions used by already-classified timer owners.
