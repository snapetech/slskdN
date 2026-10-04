#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if rg -n --glob '*.cs' "async void" "$repo_root/src/slskd"; then
  cat >&2 <<'MSG'

Async void handlers in src/slskd should be replaced with observed Task-returning callbacks.
Use TaskObservation.Observe(...) from a synchronous handler to prevent unobserved async faults.
MSG
  exit 1
fi

if rg -n --glob '*.cs' --pcre2 '\basync\s+void\s+\w+\s*\(' "$repo_root/tests/slskd.Tests.Unit"; then
  cat >&2 <<'MSG'

Async xUnit test methods must return Task so the test runner waits for their
assertions and captures asynchronous exceptions.
MSG
  exit 1
fi

printf 'No async void handlers remain in src/slskd.\n'
printf 'No async void test methods remain in tests/slskd.Tests.Unit.\n'
