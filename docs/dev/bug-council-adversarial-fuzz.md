# Adversarial Fuzz Harness Template

Use this when a repo has parsers, decoders, protocol frames, archive formats, route values, query strings, or other hostile-input surfaces.

## Required Shape

- Run more than one deterministic seed.
- Include an explicit known corpus of boundary inputs: empty, one byte, all `0xFF`, max signed integer, negative signed integer, oversized length prefixes, and any project-specific sentinel values.
- Assert that parsers either succeed or fail with documented exception/error types. Undocumented panics, null dereferences, overflow exceptions, out-of-memory attempts, or access violations are findings.
- Gate the presence of the seed list and known corpus in `scripts/check-remediation-baseline.sh`.

## Calibration

Before trusting the fuzz harness, inject one deliberate parser defect locally or add a quarantined mutation fixture that proves the harness catches the target failure mode. Record the calibration result in the sweep register or phase tracker.
