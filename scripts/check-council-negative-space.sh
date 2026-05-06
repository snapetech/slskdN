#!/usr/bin/env bash
#
# slskd Bug Council negative-space gate. Asserts that every declared trust
# boundary in docs/dev/bug-council-negative-space.md still has its required
# validator symbol present in the expected sink file.
#
# slskd already runs many topic-specific check scripts. This gate is the
# inventory of boundaries — a single declarative list independent of the
# per-topic scripts. Both layers are required: the scripts catch behavior
# drift, this gate catches "I deleted the validator and the script in the
# same change."

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

failures=0

pass() { printf 'PASS %s\n' "$1"; }
fail() { printf 'FAIL %s\n' "$1" >&2; failures=$((failures + 1)); }

assert_file_present() {
  local boundary="$1"
  local file="$2"

  if [[ -f "$file" ]]; then
    pass "negative-space: [$boundary] $file present"
  else
    fail "negative-space: [$boundary] required file missing: $file"
  fi
}

assert_baseline_runs() {
  local boundary="$1"
  local script="$2"

  assert_file_present "$boundary" "$script"

  if rg -n --fixed-strings -- "\$repo_root/${script}" "scripts/check-remediation-baseline.sh" >/dev/null; then
    pass "negative-space: [$boundary] $script is registered in check-remediation-baseline.sh"
  else
    fail "negative-space: [$boundary] $script is not registered in check-remediation-baseline.sh"
  fi
}

assert_validator_present() {
  local boundary="$1"
  local sink="$2"
  local symbol="$3"

  if [[ ! -e "$sink" ]]; then
    fail "negative-space: sink missing for boundary [$boundary]: $sink"
    return
  fi

  if rg -n --fixed-strings -- "$symbol" "$sink" >/dev/null; then
    pass "negative-space: [$boundary] $symbol present in $sink"
  else
    fail "negative-space: [$boundary] $symbol missing from $sink"
  fi
}

# Mutating API endpoint validators.
assert_validator_present \
  "mutating-api-endpoints" \
  "src/slskd/Core/Security/ValidateCsrfForCookiesOnlyAttribute.cs" \
  "ValidateCsrfForCookiesOnlyAttribute"

assert_baseline_runs \
  "mutating-api-endpoints" \
  "scripts/check-controller-csrf.sh"

assert_baseline_runs \
  "mutating-api-endpoints" \
  "scripts/check-mutating-role-requirements.sh"

# Anonymous endpoint allowlist and scanner.
assert_file_present \
  "anonymous-endpoints" \
  "docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md"

assert_baseline_runs \
  "anonymous-endpoints" \
  "scripts/check-anonymous-endpoints.sh"

# Path containment for caller-supplied paths.
assert_validator_present \
  "path-containment" \
  "src/slskd/Common/Security/PathGuard.cs" \
  "NormalizeAndValidate"

assert_validator_present \
  "path-containment" \
  "src/slskd/Common/Security/PathGuard.cs" \
  "NormalizeAbsolutePathWithinRoots"

assert_baseline_runs \
  "path-containment" \
  "scripts/check-path-containment.sh"

# Outbound HTTP URI guard.
assert_validator_present \
  "outbound-http" \
  "src/slskd/Common/Security/OutboundUriGuard.cs" \
  "OutboundUriGuard"

assert_baseline_runs \
  "outbound-http" \
  "scripts/check-outbound-http-guards.sh"

# Durable state write validators.
assert_validator_present \
  "durable-state-writes" \
  "src/slskd/Common/IO/AtomicFileWriter.cs" \
  "AtomicFileWriter"

assert_baseline_runs \
  "durable-state-writes" \
  "scripts/check-durable-state-atomic-writes.sh"

# App-side runtime crossing / background task observation validators.
assert_validator_present \
  "soulseek-runtime-crossings" \
  "src/slskd/Common/CodeQuality/TaskObservation.cs" \
  "TaskObservation"

assert_baseline_runs \
  "soulseek-runtime-crossings" \
  "scripts/check-async-task-observation.sh"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d negative-space gate check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll negative-space gate checks passed.\n'
