# Analyzer Suppression Audit

This document explains warning suppressions used by the slskdN app project. Suppressions should be narrow, temporary where possible, and tied to concrete reasons.

## Policy

- Do not add broad `NoWarn` entries without a row in this document.
- Prefer targeted `#pragma` or `.editorconfig` scope over project-wide suppression.
- Prefer fixing warnings over suppressing them.
- Build/tooling warnings should not be suppressed in the runtime project when they belong in `tools/slskd.BuildTasks`.
- Security analyzers require explicit justification and an issue/PR reference.

## Current suppressions

`src/slskd/slskd.csproj` currently suppresses the following warnings project-wide.
Debug also suppresses `1701` and `1702`; Release does not.

| Warning | Scope | Current reason | Risk | Required action |
|---|---|---|---|---|
| `1591` | Debug and Release | XML documentation is generated, but legacy public/internal surfaces are not fully documented. | Missing XML docs can hide public API churn. | Keep temporarily; prefer file/API-specific documentation for stable public APIs. |
| `S1133` | Debug and Release | Legacy obsolete-code warnings predate this audit. | Obsolete code can linger indefinitely. | Replace with targeted suppressions or remove obsolete code as files are touched. |
| `S2094` | Debug and Release | Some marker/DTO-style classes are empty by design, but this has not been proven per type. | Empty classes may indicate placeholder or hallucinated types. | Audit all empty classes before using them as proof of implemented features. |
| `S1135` | Debug and Release | Existing TODO-style comments are tracked outside this analyzer. | TODO suppression can hide incomplete features. | Do not use TODOs as feature proof; link incomplete work to `FEATURE_INVENTORY.md` or `memory-bank/tasks.md`. |
| `S3925` | Debug and Release | Equality/operator warnings exist in legacy surfaces. | Fragile equality/operator patterns can create subtle runtime bugs. | Audit before promoting related code to stable. |
| `S125` | Debug and Release | Commented code exists in legacy/generated-adjacent areas. | Commented-out code can hide dead or generated-looking implementations. | Remove commented-out implementations when touching affected files. |
| `CA2201` | Debug and Release | Existing code throws broad reserved exception types. | Generic exceptions reduce diagnosability and can mask incorrect failure modes. | Replace broad exceptions in touched code. |
| `CA2252` | Debug and Release | The app targets current .NET preview-era APIs during net10 migration. | Preview APIs can create runtime/version fragility. | Scope to .NET migration files or remove when net10 surface stabilizes. |
| `SA1633` | Debug and Release | Upstream files do not consistently carry slskdN-style headers. | Header-only warning; low runtime risk. | Keep for upstream-compatible files; new slskdN-owned files should still include headers. |
| `CA3003` | Debug and Release | Direct SQL/string construction paths exist in data/catalogue code. | Security-sensitive SQL injection analyzer suppression. | Audit all SQL/direct DB paths; do not promote SQL-backed features to stable until reviewed. |
| `CA2208` | Debug and Release | Legacy argument exception names have not been fully corrected. | Incorrect parameter names reduce debugging quality. | Fix in touched code. |
| `CS8981` | Debug and Release | Lowercase type names exist for compatibility/generated-style surfaces. | Lowercase type naming can point to generated or placeholder code. | Audit source of warning and target suppressions where compatibility requires it. |
| `1701` / `1702` | Debug only | Local Debug builds can see assembly binding/version mismatch noise during dependency churn. | Can hide real binding problems if kept after cleanup. | Recheck after build-task relocation and dependency cleanup. |

## Current observed warning posture

`dotnet build src/slskd/slskd.csproj --no-incremental` currently succeeds and
reports zero warnings.

The previous `CA2000` transport warnings in
`src/slskd/Common/Security/HttpTunnelTransport.cs` and
`src/slskd/Common/Security/MeekTransport.cs` are handled with local pragma
scopes around the `HttpClient` construction. The handlers are passed with
`disposeHandler: true`, so `HttpClient` owns the handler lifetime; do not move
these into project-wide `NoWarn`.

## Required follow-up

1. Run a build with suppressions temporarily reduced where practical.
2. Record the actual files/members triggering each warning.
3. Replace broad project suppressions with targeted suppressions.
4. Remove suppressions that only exist because of dead, placeholder, or generated-looking code.
5. Keep this document in sync with `src/slskd/slskd.csproj`.

## Release rule

A feature cannot be promoted to `stable` while its core implementation depends on unaudited broad suppressions for security, correctness, or placeholder-code warnings.
