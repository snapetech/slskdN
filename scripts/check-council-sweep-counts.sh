#!/usr/bin/env bash
#
# Bug Council sweep-count drift gate — TEMPLATE.
#
# Asserts that the candidate counts reported by scan-bug-council-candidates.sh
# match the counts recorded in the closed sweep registers. If the scan grows
# (new candidate hits) without the register updating, this gate fails and the
# council reopens.
#
# Adapt the require_closed_count rows below to match your sweep registers.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

scan_output="$(mktemp)"
counts_output="$(mktemp)"
trap 'rm -f "$scan_output" "$counts_output"' EXIT

bash scripts/scan-bug-council-candidates.sh >"$scan_output"

awk '
  /^## / {
    section = substr($0, 4)
    count[section] = 0
    next
  }

  section != "" && NF && $0 !~ /^# End/ {
    count[section]++
  }

  END {
    for (section in count) {
      printf "%s\t%d\n", section, count[section]
    }
  }
' "$scan_output" >"$counts_output"

failures=0

pass() { printf 'PASS %s\n' "$1"; }
fail() { printf 'FAIL %s\n' "$1" >&2; failures=$((failures + 1)); }

current_count() {
  local section="$1"
  awk -F '\t' -v section="$section" '$1 == section { print $2 }' "$counts_output"
}

require_closed_count() {
  local section="$1"
  local expected="$2"
  local doc="$3"
  local label="$4"

  local actual
  actual="$(current_count "$section")"

  if [[ -z "$actual" ]]; then
    fail "$label: scan section is missing: $section"
    return
  fi

  if [[ "$actual" != "$expected" ]]; then
    fail "$label: expected $expected current candidates, found $actual"
    return
  fi

  if rg -n --fixed-strings "$section: $expected/$expected classified" "$doc" >/dev/null \
    || rg -n --fixed-strings "Candidate count: $expected" "$doc" >/dev/null; then
    pass "$label"
  else
    fail "$label: $doc does not record the current closed count $expected"
  fi
}

# Add one require_closed_count line per closed sweep section. Example:
#
# require_closed_count "Security-sensitive material candidates" 0 \
#   "docs/dev/bug-council-sweep-2026-05-05.md" \
#   "secret-pattern sweep count matches scanner"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d council sweep count check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll council sweep count checks passed.\n'
