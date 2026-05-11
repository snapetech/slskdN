# Analyzer Suppression Audit

This document explains warning suppressions used by the slskdN app project. Suppressions should be narrow, temporary where possible, and tied to concrete reasons.

## Policy

- Do not add broad `NoWarn` entries without a row in this document.
- Prefer targeted `#pragma` or `.editorconfig` scope over project-wide suppression.
- Prefer fixing warnings over suppressing them.
- Build/tooling warnings should not be suppressed in the runtime project when they belong in a separate build-tasks project.
- Security analyzers require explicit justification and an issue/PR reference.

## Current suppressions to audit

| Warning | Current status | Risk | Required action |
|---|---|---|---|
| `1591` | Suppressed | Missing XML docs can hide public API churn. | Keep only if public API docs are intentionally generated elsewhere. |
| `S1133` | Suppressed | Obsolete code can linger indefinitely. | Replace with targeted suppressions or remove obsolete code. |
| `S2094` | Suppressed | Empty classes may indicate placeholder/hallucinated types. | Audit all empty classes. |
| `S1135` | Suppressed | TODO suppression can hide incomplete features. | Do not use TODOs as feature proof; link to inventory rows. |
| `S3925` | Suppressed | Fragile equality/operator patterns. | Audit before promoting related code to stable. |
| `S125` | Suppressed | Commented-out code can hide dead/generated code. | Remove commented-out implementations. |
| `CA2201` | Suppressed | Generic reserved exceptions reduce diagnosability. | Replace broad exceptions in touched code. |
| `CA2252` | Suppressed | Preview APIs can create runtime/version fragility. | Scope to .NET preview-specific files only. |
| `SA1633` | Suppressed | Header requirement only. | Acceptable if project policy does not require file headers. |
| `CA3003` | Suppressed | SQL-injection analyzer suppression is security-sensitive. | Audit all SQL/direct DB paths before keeping. |
| `CA2208` | Suppressed | Incorrect argument exception names reduce debugging quality. | Fix in touched code. |
| `CS8981` | Suppressed | Lowercase type naming or generated code smell. | Audit source of warning. |
| `1701` / `1702` | Debug-only suppression | Assembly binding/version mismatch. | Confirm still required after dependency cleanup. |

## Required follow-up

1. Run a build with suppressions temporarily reduced where practical.
2. Record the actual files/members triggering each warning.
3. Replace broad project suppressions with targeted suppressions.
4. Remove suppressions that only exist because of dead, placeholder, or generated-looking code.
5. Keep this document in sync with `src/slskd/slskd.csproj`.

## Release rule

A feature cannot be promoted to `stable` while its core implementation depends on unaudited broad suppressions for security, correctness, or placeholder-code warnings.
