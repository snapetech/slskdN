# Bug Council Sweep - Lifecycle And Cancellation - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Task, cancellation, timer, and semaphore lifecycle candidates`
- `Lifecycle task completion and race candidates`
- `Lifecycle cancellation registration candidates`
- `Lifecycle timer and semaphore candidates`

Candidate markers:

- Task, cancellation, timer, and semaphore lifecycle candidates: 202/202 classified
- Lifecycle task completion and race candidates: 81/81 classified
- Lifecycle cancellation registration candidates: 51/51 classified
- Lifecycle timer and semaphore candidates: 84/84 classified
- Unclassified candidates: 0

This sweep closes the broad lifecycle scan by splitting task completion/race points, cancellation registration ownership, and timer/semaphore lifetime into stable subgroups. The broad count increased by one during the sweep because the fix adds a scoped helper method containing the same cancellation registration ownership boundary.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Network/Tcp/Connection.cs:481` | Fixed | RT-076 | `WaitForDisconnect` now scopes cancellation registrations to the wait task and disposes them after completion instead of retaining callbacks for the lifetime of the token source. |

## Existing Guards

Classification: Existing guard.

- Runtime `TaskCompletionSource` completion uses `TrySet*` APIs and the baseline rejects non-idempotent `Set*` source calls.
- Transfer enqueue and disconnect races use `Task.WhenAny` with linked cancellation, and upload/download stream paths observe the linked race token.
- `Waiter` owns and disposes cancellation/timeout registrations through `PendingWait.Dispose`.
- `TokenBucket` races reset waits against cancellation with scoped token registrations and releases reset waiters on disposal.
- Scheduler, connection manager, and client timers/semaphores are disposed by their owning lifecycle objects; prior sweeps cover post-dispose wait creation and token-bucket wait release.
- Remaining `async void` hits are event handler entry points that wrap exceptions into diagnostics or manager cleanup paths; they remain tracked by this sweep and existing focused tests.

## False Positives

Classification: False positive.

- XML documentation and property declaration hits in the broad lifecycle section are not independent lifecycle behavior.
- Timer extension method hits are helper definitions used by already-classified timer owners.
