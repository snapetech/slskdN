# Bug Council Scan Registry — TEMPLATE

Copy into `docs/dev/bug-council-scan-registry.md` in the importing repo and adapt the scan classes to your codebase.

Companion documents (process upgrades):

- `bug-council-severity-schema.md` — severity and confidence tiers used in every new sweep row.
- `bug-council-sibling-search.md` — the rule that no row closes Fixed without a sibling sweep.
- `bug-council-negative-space.md` — declared trust boundaries, enforced by `scripts/check-council-negative-space.sh`.
- `bug-council-behavior-pinning.md` — every text-anchored remediation gate must be paired with a behavior test.

## Workflow

The council workflow is inventory-first:

1. Run `bash scripts/scan-bug-council-candidates.sh`.
2. Group every candidate under a ledger row before fixing.
3. Mark each row `New`, `Accepted`, `Fixed`, `Existing guard`, `False positive`, or `Out of scope`.
4. Batch fixes by ownership area so one verification pass covers related behavior.
5. Add or extend `scripts/check-remediation-baseline.sh` for every fixed bug class.
6. Run `bash scripts/check-council-sweep-counts.sh` to ensure closed sweep counts still match the current scanner output.

The candidate scanner is intentionally noisy. It is not the pass/fail gate; it is the durable discovery queue. The remediation baseline is the pass/fail gate for fixed bug classes and must grow whenever the council burns down a confirmed finding.

## Scan classes

Replace the rows below with the classes that make sense for your codebase. The seed list reflects classes that have proven valuable in network-protocol code; keep what fits, drop what does not.

| Class | Purpose |
| --- | --- |
| Mutable public arrays / mutable state on public surfaces | Find collections that leak mutable state to callers. |
| Constructors accepting mutable collections | Find DTOs that may retain caller-owned collections. |
| Value equality and hash-code coherence | Find equality implementations that violate the equals/hash contract. |
| Non-idempotent task completion | Find race-prone single-shot completion APIs. |
| Task / cancellation / timer / semaphore lifecycle | Find ownership and cancellation race candidates. |
| Protocol count and length allocation | Find parser loops and allocations driven by untrusted payload fields. |
| Protocol scalar emission | Find outbound message scalars that may need constructor guards. |
| Resolver outputs / raw stream handling | Find application-supplied data that crosses a serialization boundary. |
| Untrusted-string-to-path | Find file-system operations on caller-supplied strings without containment. |
| Security-sensitive material | Find high-confidence private keys and token patterns. |
| Red-team abuse lens | Re-check accepted fixes from an attacker viewpoint: spoofed identity, secret disclosure, confused deputy, replay, SSRF/path/process escape, and operational downgrade. |

## Expert roles

Keep these roles in every imported council, adapting names to the stack when useful:

| Expert | Required output |
| --- | --- |
| Runtime maintainer | Prove normal product behavior remains intact after each fix. |
| Red-team reviewer | Turn suspicious shapes into concrete exploit hypotheses, then either reject them with rationale or require a behavior test and remediation anchor. |
| Regression keeper | Ensure every accepted bug class has a focused test, a sibling sweep, and a deploy gate. |

## Sweep closure rules

- A scan is not closed while unclassified candidate hits remain in touched domains.
- A selected scan section is not closed until a dated sweep register records the candidate count and classifies every hit from that section.
- The active sweep register must include a machine-checkable classification marker, and `scripts/check-remediation-baseline.sh` must assert that marker before the council can close the section.
- Closed sweep counts must match the current candidate scanner; intentional scan drift requires updating the sweep register and `scripts/check-council-sweep-counts.sh` in the same change.
- Confirmed runtime bugs get focused regression tests and remediation-baseline patterns.
- False positives stay in the ledger only when they document a recurring scan hit that would otherwise be re-reviewed.
