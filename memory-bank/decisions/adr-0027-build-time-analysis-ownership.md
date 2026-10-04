# ADR-0027: Keep Roslyn Analysis In Build Tooling

## Status

Accepted — 2026-10-03

## Context

`BuildTimeAnalyzer` is called by the MSBuild task in
`tools/slskd.BuildTasks`, but the application project also compiled the
analyzer source and referenced Roslyn packages directly. As a result, the
runtime application output included compiler assemblies totaling about
10 MB. Unit tests resolved the build-time analyzer through the application
reference, which hid the ownership mismatch. The separate `SlskdnAnalyzer`
was not registered or referenced by any project and did not run during builds.

## Decision

Keep Roslyn-based source inspection in `slskd.BuildTasks`. Exclude its analyzer
source from the runtime application, reference the build-tools project
directly from tests that exercise it, and remove the unused, unregistered
`SlskdnAnalyzer`. Retain the app's `Microsoft.CodeAnalysis.NetAnalyzers`
package only as a private build analyzer; it must not become a runtime
assembly reference.

Add a unit regression that checks the analyzer's assembly ownership and
asserts that the runtime application assembly has no `Microsoft.CodeAnalysis`
references. Verify build and publish output when changing this boundary.

## Consequences

- Application runtime and published output no longer carry Roslyn compiler
  assemblies.
- Build-time source analysis continues through the existing MSBuild task, and
  its tests target the assembly that owns it.
- The unused `SlskdnAnalyzer` implementation is removed rather than implying
  a compile-time analyzer that the build never registered.
