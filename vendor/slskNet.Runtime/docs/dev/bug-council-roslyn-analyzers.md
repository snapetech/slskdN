# Bug Council Roslyn Analyzers

The council ships a small Roslyn analyzer project, `analyzers/Soulseek.CouncilAnalyzers`, that runs against the runtime build and adds semantic-aware lenses to the council. Analyzers complement the regex scanner: where the scanner asks "is there a line that looks like X," analyzers ask "does the dataflow into Y satisfy invariant Z."

## Layout

- `analyzers/Soulseek.CouncilAnalyzers/Soulseek.CouncilAnalyzers.csproj` — `netstandard2.0`, references `Microsoft.CodeAnalysis.CSharp`. Not packaged. Lives outside `src/` so the runtime project's default `Compile` glob does not pick up its sources.
- `analyzers/Soulseek.CouncilAnalyzers/*Analyzer.cs` — one file per lens.
- `analyzers/Soulseek.CouncilAnalyzers/ProtocolTaintAnalysis.cs` — shared intra-procedural protocol taint classifier used by current C# lenses.
- `tests/Soulseek.CouncilAnalyzers.Tests/` — analyzer unit tests. Tests use direct Roslyn compilation rather than the heavier `Microsoft.CodeAnalysis.Testing` framework so the test project stays small.
- `tests/Soulseek.CouncilAnalyzers.Calibration/` — mutation/calibration corpus with intentionally bad and intentionally good snippets. This is the proof that a green run means the lenses can still catch the shapes they claim to catch.
- `src/Soulseek.csproj` references the analyzer with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`. The analyzer runs against the runtime build but its types are never linked into the shipping package.

## Current lenses

| ID | Name | Severity (council) | Lens |
| --- | --- | --- | --- |
| CSL0001 | TaintToAllocation | High | Network-derived allocation size without a sanctioned validator. Tainted sources include `MessageReader<T>.{ReadByte, ReadBytes, ReadCode, ReadInteger, ReadLong, ReadString, ReadStringAndEncoding}` and reader extension methods. Sinks include arrays, `Array.CreateInstance`, `MemoryStream(int)`, `StringBuilder(int)`, and common collection capacity constructors. Sanctioned validators include `ProtocolCountReader.ReadCount`/`ReadValidatedCount`, `ProtocolValueValidator.*`, `MessageFrameValidator.Validate*`, and protocol argument guards. |
| CSL0002 | TaintToLoopBound | High | Network-derived `for` loop bound without a sanctioned validator. This catches the non-allocation denial-of-service shape where a hostile count drives repeated work or repeated per-iteration allocations. |

The `Severity (council)` column is the council's severity-schema tier; the analyzer's reported `DiagnosticSeverity` is `Warning` because errors would block the build for partial-knowledge cases. The council severity is what determines triage priority.

## Adding a new lens

1. Pick a name and ID in the `CSL00xx` range.
2. Add the analyzer file to `analyzers/Soulseek.CouncilAnalyzers/`.
3. Add positive and negative tests to `tests/Soulseek.CouncilAnalyzers.Tests/`.
4. Add at least one intentionally bad and one intentionally good snippet to `tests/Soulseek.CouncilAnalyzers.Calibration/`.
5. Update the table above.
6. Add `require_pattern` checks to `scripts/check-remediation-baseline.sh` for the diagnostic ID and the calibration snippet.
7. Run `dotnet build src/Soulseek.csproj` and confirm the new lens does not fire on existing runtime source. If it does, decide: accept the finding into a sweep register, or refine the lens.

## Design rules

- **Intra-procedural only by default.** Inter-procedural taint is expensive and produces false positives across the public API surface. Lenses that need to follow taint across method boundaries should be opt-in and prove their false-positive rate before being wired into the runtime build.
- **Sanctioned validators are an enumerated allowlist, not a heuristic.** Adding a new validator name to a lens is a council-visible decision and should land in the same change as the validator.
- **Lenses must be deterministic.** No timing, no random sampling, no environment-dependent behavior — Roslyn calls them on every build, and flaky diagnostics erode trust faster than missing ones.
- **Every lens earns its keep.** A lens that has never fired on a real bug after one full sweep cycle is a candidate for removal. The cost of a noisy lens is paid by every developer; the benefit is paid only when it catches something.
- **Every lens is calibrated.** A zero-finding run is only meaningful if the calibration project still proves the lens fires on a deliberate mutation and stays silent on the sanctioned-validator shape.

## Mapping to the rest of the council

- The severity tier comes from `docs/dev/bug-council-severity-schema.md`.
- The fix shape, when a CSL fires, must follow the sibling-search rule from `docs/dev/bug-council-sibling-search.md`.
- A new boundary that creates demand for a new lens is also probably a new entry in `docs/dev/bug-council-negative-space.md`.
- Behavior pinning per `docs/dev/bug-council-behavior-pinning.md` is satisfied by the analyzer's own test suite — that is the behavior gate.
