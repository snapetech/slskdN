# Bug Council Severity And Confidence Schema

Every accepted council finding gets a **severity** and a **confidence**. The two fields together let the council triage a 300-row sweep without re-reading every rationale. They are also what makes the council legible to outside reviewers: severity says how bad the bug is, confidence says how sure we are.

## Severity

| Tier | Meaning |
| --- | --- |
| Critical | Remote unauthenticated attacker can cause RCE, auth bypass, or unbounded resource consumption that takes the process down. |
| High | Remote attacker can cause denial of service, integrity violation, info leak across trust boundaries, or persistent state corruption. Includes most untrusted-input → unbounded-allocation findings. |
| Medium | Local correctness bug with observable user-visible failure: hang, wrong result, dropped event. Not exploitable by remote without prior compromise. |
| Low | Defensive-depth gap: code path is currently unreachable from untrusted input, but the absence of the guard is itself a hazard if a refactor exposes it. |
| Cosmetic | Style, naming, dead code, doc drift. Recorded only when it would otherwise be re-flagged on every sweep. |

Pick the **worst plausible** severity given current code paths. If the same code is reachable from two boundaries with different severities, take the higher.

## Confidence

| Tier | Meaning |
| --- | --- |
| Proven | A failing test exists or could be written from the rationale alone. The bug is reproducible. |
| Likely | The reasoning is sound and the call graph is short, but no test reproduces it yet. |
| Speculative | The pattern is suspicious but the call graph is long, gated, or partially unverified. Used to keep a candidate in the queue without blocking a sweep close. |

Speculative findings cannot be marked Fixed without first being upgraded to Likely or Proven. A speculative finding can be marked False positive only with a written rationale; otherwise it carries forward to the next sweep.

## Use in sweep tables

Sweep registers extend their candidate tables with two columns:

```
| Candidate | Severity | Confidence | Classification | Ledger | Rationale |
| --- | --- | --- | --- | --- | --- |
| `src/Foo.cs:42` | High | Proven | Fixed | RT-101 | Inbound length read flowed into new byte[N] without ProtocolCountReader. |
```

Existing sweep registers do not need to be retroactively annotated. New rows added to existing registers must use the schema. New sweep registers must use the schema for every row.

## Promotion rules

- A row's severity can be raised at any time; lowering severity requires a written justification in the rationale.
- A row's confidence can move both directions and should be updated as evidence accumulates.
- The **highest severity row of any open sweep** is the council's headline number. It belongs in the phase tracker so the team always knows what the worst open finding looks like.

## Why two axes instead of one

A single "priority" tier collapses two independent things: how bad it would be, and how sure we are. The council found that mixing them led to either over-prioritizing speculative-but-scary patterns or under-prioritizing proven-but-medium correctness bugs. The severity-confidence split reflects how the actual decisions get made: a Proven Medium finding is fixed before a Speculative High one because the cost of the Proven fix is bounded.
