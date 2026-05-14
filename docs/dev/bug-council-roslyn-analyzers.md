# Council Roslyn Analyzers — TEMPLATE

For .NET projects only. Copy into `docs/dev/bug-council-roslyn-analyzers.md` and adapt the analyzer list to the lenses you ship.

The council ships a small Roslyn analyzer project that runs against the runtime build and adds semantic-aware lenses to the council. Analyzers complement the regex scanner: where the scanner asks "is there a line that looks like X," analyzers ask "does the dataflow into Y satisfy invariant Z."

## Layout

- `analyzers/CouncilAnalyzers/CouncilAnalyzers.csproj` — `netstandard2.0`, references `Microsoft.CodeAnalysis.CSharp`. Not packaged. **Lives outside `src/`** so the runtime project's default `Compile` glob does not pick up its sources.
- `analyzers/CouncilAnalyzers/*Analyzer.cs` — one file per lens.
- `analyzers/CouncilAnalyzers/ProtocolTaintAnalysis.cs` — optional shared intra-procedural taint classifier when multiple lenses share the same source/validator model.
- `analyzers/CouncilAnalyzers.Tests/` — analyzer unit tests using direct Roslyn compilation (lighter than `Microsoft.CodeAnalysis.Testing`).
- `analyzers/CouncilAnalyzers.Calibration/` — optional mutation/calibration test project with intentionally bad and intentionally good snippets. Use this to prove a zero-finding run still means the lens can catch its target shape.
- The runtime `.csproj` references the analyzer with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.

## Lens table

| ID | Name | Council severity | Description |
| --- | --- | --- | --- |
| CSL0001 | TaintToAllocation | High | Network-derived allocation size without a sanctioned validator. Strong implementations cover arrays, `Array.CreateInstance`, stream/string-builder capacities, and common collection capacity constructors. |
| CSL0002 | TaintToLoopBound | High | Network-derived loop bound without a sanctioned validator. This catches hostile counts that drive repeated work or repeated per-iteration allocations. |
| CSL0003 | TaintToStreamPosition | High | Network-derived stream position, parser seek, or skip count without a sanctioned validator. This catches hostile offsets that can desynchronize bounded frame parsing. |
| CSL0004 | TaintToFilePath | High | Network-derived file/directory path without sanctioned containment validation. This catches hostile paths before filesystem sinks trust them. |
| CSL0005 | TaintToTimeout | High | Network-derived timeout, delay, timer interval, or duration without sanctioned range validation. |
| CSL0006 | TaintToEndpoint | High | Network-derived address, endpoint, DNS, or URI component without sanctioned endpoint validation. |
| CSL0007 | TaintToEnum | High | Network-derived enum/status conversion without defined-value validation. |
| CSL0008 | TaintToStringSlice | High | Network-derived slice index or length without sanctioned range validation. |
| CSL0009 | TaintToDiagnostic | High | Network-derived diagnostic/log text without log-line/control-character validation. Preserve operator-visible values while preventing forged-line/control-character injection. |
| CSL0010 | TaintToMessageBuilder | High | Network-derived outbound protocol/message-builder values without outbound argument validation. |
| CSL0011 | TaintToCacheKey | High | Network-derived cache, dictionary, or correlation keys without normalization/bounding. |
| CSL0012 | TaintToCryptoTrust | High | Network-derived key/signature/trust material without explicit size/format verification. |
| CSL0013 | TaintToDynamicExecution | High | Network-derived reflection, assembly loading, type lookup, or process input without allowlist validation. |
| CSL0014 | TaintToParserRuntime | High | Network-derived regex, JSON, XML, or parser-runtime input without parser limits or timeout validation. |
| CSL0015 | TaintToResourceCapacity | High | Network-derived concurrency/resource capacity values without sanctioned bounds. |
| CSL0016 | TaintToBufferOperation | High | Network-derived buffer, stream, pool, or compression operation counts without sanctioned bounds. |

## Adding a new lens

1. Pick an ID in the `CSL00xx` range.
2. Add the analyzer file to `analyzers/CouncilAnalyzers/`.
3. Add positive and negative tests.
4. Add a calibration snippet that must fire, plus a sanctioned-validator snippet that must stay silent.
5. Update the lens table.
6. Add `require_pattern` checks to `scripts/check-remediation-baseline.sh` asserting the diagnostic ID and calibration corpus are in source.
7. Build the runtime and confirm the lens does not fire on existing code. If it does, decide: accept the finding into a sweep register, or refine the lens.

## Design rules

- **Intra-procedural by default.** Inter-procedural taint produces false positives.
- **Sanctioned validators are an enumerated allowlist, not a heuristic.** Adding a name is a council-visible decision.
- **Lenses must be deterministic.** Roslyn calls them on every build.
- **Every lens earns its keep.** A lens that has never fired on a real bug after a full sweep cycle is a candidate for removal.
- **Every lens is calibrated.** A zero-finding analyzer run is only credible when a deliberate mutation still fails in the calibration project.
