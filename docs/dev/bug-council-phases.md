# slskd Bug Council Phase Tracker

slskd inherits the council methodology from slskNet.Runtime. The canonical phase tracker for council process upgrades (severity/confidence schema, sibling-search rule, negative-space gate, behavior-pinning, Roslyn analyzer beachhead, fuzz harness) lives in:

- [`vendor/slskNet.Runtime/docs/dev/bug-council-phases.md`](../../vendor/slskNet.Runtime/docs/dev/bug-council-phases.md)

slskd ships its own scoped phase rows below for slskd-specific work that builds on the canonical schema.

## What is mirrored from slskNet.Runtime

- `bug-council-severity-schema.md` — verbatim copy. The severity/confidence tiers apply unchanged to slskd findings.
- `bug-council-sibling-search.md` — verbatim copy.
- `bug-council-behavior-pinning.md` — verbatim copy.
- `bug-council-negative-space.md` — slskd-adapted, declares slskd-specific boundaries.

## What is intentionally not mirrored

- The slskNet.Runtime candidate scanner (`scan-bug-council-candidates.sh`) is **not** mirrored. slskd already runs ~35 topic-specific check scripts that cover the same territory in finer detail. Adding a catch-all scanner on top would produce noise without new signal.
- The Roslyn analyzers (`Soulseek.CouncilAnalyzers`) ship in slskNet.Runtime and reach slskd through the vendored runtime. slskd does not maintain its own copy. The vendored runtime now includes calibrated `CSL0001` through `CSL0008` semantic lenses for allocation, loop-bound, stream-position, file-path, timeout, endpoint, enum/status, and slice-bound sinks.
- The protocol fuzz harness lives in slskNet.Runtime and now includes multiple deterministic seeds plus explicit hostile corpus inputs. slskd-level fuzz (HTTP / web shell input) is its own follow-up phase.

## slskd phases

| # | Name | Status | Owner | Exit criteria |
| --- | --- | --- | --- | --- |
| 1 | Mirror council process docs | Done | (agent) | Schema, sibling-search, behavior-pinning, slskd-adapted negative-space, this tracker, and a slskd `check-council-negative-space.sh` script all present. |
| 2 | Wire negative-space gate into the meta-runner | Done | (agent) | `scripts/check-remediation-baseline.sh` calls `scripts/check-council-negative-space.sh`. |
| 3 | Web-input adversarial fuzz | Done | (agent) | `tests/slskd.Tests/WebInputAdversarialFuzzTests.cs` sends malformed JSON, deterministic random bytes, and hostile query/path strings through the test host; `scripts/check-web-input-adversarial-fuzz.sh` keeps the harness registered in remediation. |

## How to resume

1. Read this tracker; identify the first non-Done row.
2. Run `bash scripts/check-remediation-baseline.sh` to confirm a green baseline.
3. Run `npm run check:council` so every slskd council phase executes in one command.
4. Pick up any failing or non-Done phase, update its status, and follow its exit criteria.
