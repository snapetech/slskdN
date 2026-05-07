# Bug Council Mythos-Level Upgrade — Phase Tracker

Started: 2026-05-06.

This document is the resumable plan for upgrading the bug council from regex-only surface scanning to deeper semantic, adversarial, and process-level scanning. Every agent that picks up this work should update this file as phases progress and check the entry/exit criteria below before claiming a phase complete.

The goal is **higher-severity, deeper-fix findings** without abandoning the inventory-first discipline that the existing council depends on. Existing dated sweep registers, the scan registry, and the remediation baseline all continue to work; this work is additive.

## Phases

| # | Name | Status | Owner | Exit criteria |
| --- | --- | --- | --- | --- |
| 1 | Council process upgrades | Done | (agent) | Severity/confidence schema added to one sweep register, sibling-search rule documented, negative-space gate doc + script, behavior-pinning pattern documented, baseline gates the new rule presence. |
| 2 | Roslyn `TaintToAllocation` analyzer beachhead | Done | (agent) | New `Soulseek.CouncilAnalyzers` analyzer project added to the solution and referenced by `src/Soulseek.csproj` as an analyzer. `CSL0001` taint→allocation diagnostic implemented, unit tests pass, `dotnet build` clean, baseline asserts the analyzer ships. |
| 3 | Protocol fuzz harness | Done | (agent) | Hand-rolled (no FsCheck dep) roundtrip + adversarial-bytes property tests across `Server`, `Peer`, and `Distributed` parsers; tests run under `dotnet test --filter Category=Fuzz`; baseline asserts presence of the fuzz traits. |
| 4 | Generic `council_of_experts` repo | Done | (agent) | New repo at `/home/keith/Documents/code/council_of_experts` containing language-agnostic scanners, ledger/registry templates, schema docs, Roslyn analyzer template, README. Public push confirmed: https://github.com/snapetech/council_of_experts. |
| 5 | Mirror to `../slskdn` and `../slskR` | Done | (agent) | slskdn gains severity/sibling/negative-space/behavior-pinning docs and a top-level negative-space gate wired into the meta-runner (no scanner — slskdn already has 35 topic-specific gates). slskR gains the same docs and a Rust-flavored negative-space gate; existing `run-council-scan.sh` retained. Neither vendored slskNet.Runtime in slskdn nor uncommitted user edits were touched. |
| 7 | Broaden CSL0001 sources/sinks | Done | (agent) | Shared protocol taint classifier covers byte/string/code readers and reader extensions; CSL0001 covers arrays, `Array.CreateInstance`, stream capacities, string builders, and common collection capacity constructors; behavior pinned by analyzer tests and calibration corpus. |
| 8 | Add CSL0002 second analyzer | Done | (agent) | `TaintToLoopBoundAnalyzer` flags tainted `for` loop bounds without sanctioned validators; positive, reversed-condition, validator, and parameter cases are tested. |
| 9 | Mutation/calibration fixture project | Done | (agent) | `tests/Soulseek.CouncilAnalyzers.Calibration` is in the solution and contains known-bad/known-good snippets that prove zero-finding analyzer runs are calibrated. |
| 10 | Multi-seed adversarial fuzz corpora | Done | (agent) | `ProtocolAdversarialFuzz` runs multiple deterministic random seeds plus explicit hostile corpus inputs; baseline gates both. |
| 11 | All-phases council runner | Done | (agent) | `scripts/run-bug-council-all-phases.sh` runs candidate inventory, remediation, sweep-count drift, negative-space, analyzer tests, calibration, protocol fuzz, build, and package vulnerability scan in one command; `scripts/check-bug-council-all-phases.sh` is wired into the remediation baseline so partial council runs regress loudly. |
| 12 | Non-proof verdict and active discovery handoff | Done | (agent) | `scripts/run-bug-council-all-phases.sh` explicitly states that a green council pass is not proof of no bugs, invokes `scripts/run-council-active-bughunt.sh`, and the registration guard verifies both the active-discovery runner and non-proof wording. |
| 13 | Active backlog pile gate | Done | (agent) | `docs/dev/bug-council-active-backlog.md` records every active-discovery pile with a current count/status, `scripts/check-council-active-backlog.sh` fails on stale or untriaged rows, and the all-phases runner plus remediation baseline invoke the gate every cycle. |
| 14 | Add CSL0003 stream-position analyzer | Done | (agent) | `TaintToStreamPositionAnalyzer` flags protocol-tainted `Seek`, `Skip`, and `Position` sinks without sanctioned validation; analyzer unit tests and the calibration corpus prove the new lens fires on known-bad code and stays silent on validated offsets. |

Mark a phase **Done** only when every exit-criteria item is satisfied and the phase artifacts are checked in or staged.

## Phase 1 — Council process upgrades

Why first: cheap, repo-wide, mirror-friendly, and they unblock the higher-leverage phases (a Roslyn analyzer needs a severity tier; a fuzz finding needs the same triage shape).

Deliverables:

1. `docs/dev/bug-council-severity-schema.md` — defines severity (Critical/High/Medium/Low/Cosmetic) and confidence (Proven/Likely/Speculative) and how to use them in sweep tables.
2. `docs/dev/bug-council-sibling-search.md` — rule that every accepted finding requires a sibling sweep across the codebase before the row is closed.
3. `docs/dev/bug-council-negative-space.md` — declares trust boundaries and the validator each must run; a CI script asserts every declared boundary still has its validator wired.
4. `docs/dev/bug-council-behavior-pinning.md` — pattern that converts text-anchored remediation gates into behavior-anchored test pins.
5. `scripts/check-council-negative-space.sh` — guard that asserts every declared boundary's validator symbol is present in the expected file. (This script is intentionally narrow; it sits alongside `check-remediation-baseline.sh`, not inside it.)
6. Update `docs/dev/bug-council-scan-registry.md` to point to the new schema docs.
7. Add `bash scripts/check-council-negative-space.sh` invocation to `scripts/check-remediation-baseline.sh` so existing CI picks it up automatically.

Exit checklist:

- [ ] Schema doc exists and is referenced from the registry.
- [ ] Sibling-search doc exists and is referenced from the registry.
- [ ] Negative-space doc + script exists; script passes locally.
- [ ] Behavior-pinning doc exists and is referenced from the registry.
- [ ] One existing sweep register is annotated with severity/confidence to demonstrate the format.
- [ ] `bash scripts/check-remediation-baseline.sh` and `bash scripts/check-council-sweep-counts.sh` both pass.

## Phase 2 — Roslyn `TaintToAllocation` analyzer

Why this analyzer first: highest severity (network-controlled allocation = denial of service), most demonstrative of the move from text to semantics, and overlaps an existing scan class so we can show the lift directly.

Deliverables:

1. `src/Soulseek.CouncilAnalyzers/Soulseek.CouncilAnalyzers.csproj` — `netstandard2.0`, `Microsoft.CodeAnalysis.CSharp` reference, marked as analyzer.
2. `TaintToAllocationAnalyzer.cs` — diagnostic `CSL0001`. Flags `new T[N]` / `Array.CreateInstance(_, N)` / `new MemoryStream(N)` / list capacities where N derives (transitively, intra-procedural) from a method on a type tagged as a wire reader (e.g. `MessageReader.ReadInteger`, `MessageReader.ReadLong`) without an intervening call to a sanctioned validator (`ProtocolCountReader.ReadValidatedCount`, `MessageFrameValidator.Validate*`, etc.).
3. `tests/Soulseek.CouncilAnalyzers.Tests/` — unit tests with positive and negative cases.
4. `src/Soulseek.csproj` — reference the analyzer project with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` so it runs against the runtime build.
5. `docs/dev/bug-council-roslyn-analyzers.md` — how to add a new analyzer (lens) to the council.
6. Remediation baseline gate: assert the analyzer assembly is referenced and that the rule descriptor `CSL0001` appears in source.

Exit checklist:

- [ ] `dotnet build slskNet.Runtime.sln` passes (warnings allowed, no new errors).
- [ ] `dotnet test tests/Soulseek.CouncilAnalyzers.Tests` is green.
- [ ] Analyzer fires on a known unprotected `new byte[ReadInteger()]` snippet in a test fixture, and stays silent when `ProtocolCountReader.ReadValidatedCount` is in the path.
- [ ] Baseline gates the analyzer presence.
- [ ] Phase tracker updated with current sites the analyzer fires on (zero is the goal — anything it fires on becomes a sweep row).

## Phase 3 — Protocol fuzz harness

Why third: depends on the severity schema (so findings get triaged) and benefits from the analyzer catching obvious unbounded allocations first.

Deliverables:

1. `tests/Soulseek.Tests.Unit/Messaging/Fuzz/ProtocolRoundtripFuzz.cs` — FsCheck-based roundtrip property tests for selected `*ResponseFactory.FromByteArray` parsers.
2. `tests/Soulseek.Tests.Unit/Messaging/Fuzz/ProtocolAdversarialFuzz.cs` — adversarial random-byte tests asserting parsers either succeed or throw a documented exception type, never crash with `OutOfMemory`/`AccessViolation`/`NullRef`.
3. Baseline gate asserts both files exist and reference at least the count `ProtocolCountHardeningTests` already covers.

Exit checklist:

- [ ] FsCheck added as a test dependency.
- [ ] Fuzz suites cover at least the three parser families (Server / Peer / Distributed).
- [ ] `dotnet test --filter Category=Fuzz` (or trait equivalent) passes 1000 iterations on each property.
- [ ] Baseline asserts presence.

## Phase 4 — `council_of_experts` generic repo

Deliverables:

1. New directory `/home/keith/Documents/code/council_of_experts` with:
   - `README.md` — what the council is, how to import.
   - `LICENSE` — MIT.
   - `templates/scripts/scan-bug-council-candidates.sh` — language-agnostic scanner skeleton with examples for C#, Rust, TS/JS, Go, Python.
   - `templates/scripts/check-council-sweep-counts.sh` — count drift gate template.
   - `templates/scripts/check-remediation-baseline.sh` — behavior + presence gate template.
   - `templates/scripts/check-council-negative-space.sh` — negative-space boundary check template.
   - `templates/docs/bug-burndown-ledger.md` — ledger template.
   - `templates/docs/bug-council-scan-registry.md` — registry template.
   - `templates/docs/bug-council-severity-schema.md` — severity/confidence schema (verbatim from Phase 1).
   - `templates/docs/bug-council-sibling-search.md` — sibling rule.
   - `templates/docs/bug-council-negative-space.md` — boundary policy.
   - `templates/docs/bug-council-behavior-pinning.md` — behavior pinning pattern.
   - `templates/docs/bug-council-phases.md` — phase tracker template.
   - `templates/analyzers/csharp/` — minimal Roslyn analyzer template + test project.
   - `examples/dotnet/`, `examples/rust/`, `examples/typescript/` — short worked examples for each ecosystem.
2. `git init`, initial commit, branch `main`.
3. **Confirm with user before push**, then `gh repo create snapetech/council_of_experts --public --source=. --remote=origin --push`.

Exit checklist:

- [ ] Local repo populated and committed.
- [ ] User has explicitly confirmed the public push.
- [ ] `gh repo view snapetech/council_of_experts` returns the new repo.
- [ ] Repo URL recorded in this phase tracker.

## Phase 5 — Mirror to `../slskdn` and `../slskR`

Constraints:

- `../slskdn` vendors `slskNet.Runtime` at `vendor/slskNet.Runtime/`. **Do not** propagate the council changes into that vendored copy — slskNet.Runtime ships its own council; the vendored copy will pick it up via normal vendor sync.
- `../slskdn` and `../slskR` both currently have uncommitted working-tree edits. Add new files only; do not touch existing modified files.
- `../slskR` already has a full council (`scripts/run-council-scan.sh`, `scripts/check-council-loop.sh`, `docs/dev/council-scan-inventory.md`) with its own naming. Adapt the upgrade docs to its terminology rather than overwriting.

Deliverables for slskdn (top level):

- `scripts/scan-bug-council-candidates.sh` — adapted to the slskd code layout (`src/`, `tests/`, `slskd.Tests*/`).
- `scripts/check-council-sweep-counts.sh` and `scripts/check-council-negative-space.sh` — same shape as runtime.
- `docs/dev/bug-council-scan-registry.md`, severity/confidence/sibling/negative-space/behavior-pinning docs.
- `docs/dev/bug-council-phases.md` referencing slskNet.Runtime's tracker for context.

Deliverables for slskR (top level):

- Severity/confidence/sibling/negative-space/behavior-pinning docs in `docs/dev/`, named to coexist with the existing council inventory.
- A short note in the existing `council-scan-inventory.md` (or a new `council-upgrades.md`) pointing at the new schema docs.
- A negative-space script if useful for slskR's Rust crates; otherwise noted as not yet implemented.

Exit checklist:

- [ ] slskdn has a top-level scanner that produces a candidate inventory.
- [ ] slskR has the schema/sibling/negative-space/behavior-pinning docs.
- [ ] No edits to vendored slskNet.Runtime in slskdn.
- [ ] No edits to pre-existing modified files in either sibling repo.
- [ ] Phase tracker references for both repos point back to the canonical schema in `council_of_experts`.

## How to resume

1. Read recent product/fix commits and the ledger to see what has landed. Commit messages must describe the runtime change, not the discovery tool or process.
2. Read this file's phase table to find the first non-Done row.
3. Run `bash scripts/run-bug-council-all-phases.sh`; treat a green result as "registered lenses passed", not as "no bugs exist".
4. Pick up the phase, update its status to In Progress, and follow its exit checklist.

If a phase has been partially completed by another agent, treat the on-disk artifacts as the source of truth and reconcile this tracker against them rather than re-doing work.
