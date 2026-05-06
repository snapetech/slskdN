# Bug Council Scan Registry

The council workflow is inventory-first:

1. Run `bash scripts/scan-bug-council-candidates.sh`.
2. Group every candidate under a ledger row before fixing.
3. Mark each row `New`, `Accepted`, `Fixed`, `Existing guard`, `False positive`, or `Out of scope`.
4. Batch fixes by ownership area so one verification pass covers related behavior.
5. Add or extend `scripts/check-remediation-baseline.sh` for every fixed bug class.
6. Run `bash scripts/check-council-sweep-counts.sh` to ensure closed sweep counts still match the current scanner output.

The candidate scanner is intentionally noisy. It is not the pass/fail gate; it is the durable discovery queue. The remediation baseline is the pass/fail gate for fixed bug classes and must grow whenever the council burns down a confirmed finding.

Current scan classes:

| Class | Purpose |
| --- | --- |
| Mutable public arrays and array properties | Find byte arrays and arrays that can leak mutable state. |
| Constructors accepting mutable collections or params arrays | Find DTOs that may retain caller-owned collections. |
| Value equality and hash-code comparisons | Find equality implementations that dereference null or use hash equality. |
| Non-idempotent task completion | Find race-prone `TaskCompletionSource.Set*` calls in runtime source. |
| Task, cancellation, timer, and semaphore lifecycle | Find ownership and cancellation race candidates. |
| Lifecycle task completion and race | Find task completion, continuation, `Task.WhenAny`, and event-style async entry points. |
| Lifecycle cancellation registration | Find cancellation source and token registration ownership points. |
| Lifecycle timer and semaphore | Find timer and semaphore ownership/lifetime points. |
| Lifecycle fire-and-forget async misuse | Find background async calls that configure awaits without observing the returned task. |
| Protocol count and length allocation | Find parser loops and allocations driven by untrusted payload fields. |
| Protocol scalar emission | Find outbound message scalars that may need constructor guards. |
| Resolver output and raw stream handling | Find application-supplied data that crosses peer serialization boundaries. |
| Resolver delegate surface | Find public resolver and enqueue delegate configuration surfaces. |
| Peer resolver dispatch | Find peer handler points that turn resolver output into peer messages. |
| Transfer stream factory | Find transfer input/output stream factory ownership and lifecycle paths. |
| Example Web API path/request/lifecycle | Find path containment, request validation, and disposable ownership issues in the example app. |
| Example Web API path/shared files | Find shared-file path advertisement, containment, and resolver output issues in the example app. |
| Example Web API controller request validation | Find controller request-body, route, and response validation boundaries in the example app. |
| Example Web API transfer lifecycle | Find transfer cancellation, stream, and background task ownership in the example app. |
| Example Web API tracker state | Find tracker dictionary/list update and null-shape assumptions in the example app. |
| Security-sensitive material | Find high-confidence private keys and token patterns. |

Sweep closure rules:

- A scan is not closed while unclassified candidate hits remain in touched domains.
- A selected scan section is not closed until a dated sweep register records the candidate count and classifies every hit from that section.
- The active sweep register must include a machine-checkable classification marker, and `scripts/check-remediation-baseline.sh` must assert that marker before the council can close the section.
- Closed sweep counts must match the current candidate scanner; intentional scan drift requires updating the sweep register and `scripts/check-council-sweep-counts.sh` in the same change.
- Confirmed runtime bugs get focused regression tests and remediation-baseline patterns.
- False positives stay in the ledger only when they document a recurring scan hit that would otherwise be re-reviewed.
- Integration-only risks are recorded explicitly when credentials or live Soulseek network access are unavailable.
