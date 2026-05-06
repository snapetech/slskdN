#!/usr/bin/env bash
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

pass() {
  printf 'PASS %s\n' "$1"
}

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failures=$((failures + 1))
}

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

require_closed_count "Constructors accepting mutable collections or params arrays" 28 "docs/dev/bug-council-sweep-2026-05-05.md" "constructor sweep count matches scanner"
require_closed_count "Protocol count and length allocation candidates" 221 "docs/dev/bug-council-sweep-protocol-length-2026-05-05.md" "protocol broad count/length sweep count matches scanner"
require_closed_count "Protocol counted collection loops" 54 "docs/dev/bug-council-sweep-protocol-length-2026-05-05.md" "protocol counted-loop sweep count matches scanner"
require_closed_count "Protocol length-prefixed reads and payload allocations" 12 "docs/dev/bug-council-sweep-protocol-length-2026-05-05.md" "protocol length/allocation sweep count matches scanner"
require_closed_count "Protocol compression boundary candidates" 16 "docs/dev/bug-council-sweep-protocol-length-2026-05-05.md" "protocol compression sweep count matches scanner"
require_closed_count "Protocol scalar emission candidates" 145 "docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md" "protocol scalar sweep count matches scanner"
require_closed_count "Protocol scalar constructor guard candidates" 66 "docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md" "protocol scalar guard sweep count matches scanner"
require_closed_count "Resolver output and raw stream candidates" 341 "docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md" "resolver/raw stream sweep count matches scanner"
require_closed_count "Resolver delegate surface candidates" 61 "docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md" "resolver delegate sweep count matches scanner"
require_closed_count "Peer resolver dispatch candidates" 20 "docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md" "peer resolver dispatch sweep count matches scanner"
require_closed_count "Transfer stream factory candidates" 49 "docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md" "transfer stream factory sweep count matches scanner"
require_closed_count "Task, cancellation, timer, and semaphore lifecycle candidates" 203 "docs/dev/bug-council-sweep-lifecycle-2026-05-05.md" "lifecycle broad sweep count matches scanner"
require_closed_count "Lifecycle task completion and race candidates" 82 "docs/dev/bug-council-sweep-lifecycle-2026-05-05.md" "lifecycle task/race sweep count matches scanner"
require_closed_count "Lifecycle cancellation registration candidates" 51 "docs/dev/bug-council-sweep-lifecycle-2026-05-05.md" "lifecycle cancellation sweep count matches scanner"
require_closed_count "Lifecycle timer and semaphore candidates" 84 "docs/dev/bug-council-sweep-lifecycle-2026-05-05.md" "lifecycle timer/semaphore sweep count matches scanner"
require_closed_count "Lifecycle fire-and-forget async misuse candidates" 0 "docs/dev/bug-council-sweep-lifecycle-2026-05-05.md" "lifecycle fire-and-forget sweep count matches scanner"
require_closed_count "Example Web API path, request, and lifecycle candidates" 390 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API broad sweep count matches scanner"
require_closed_count "Example Web API path and shared-file candidates" 177 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API path/shared-file sweep count matches scanner"
require_closed_count "Example Web API controller request-validation candidates" 268 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API request-validation sweep count matches scanner"
require_closed_count "Example Web API transfer lifecycle candidates" 158 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API transfer lifecycle sweep count matches scanner"
require_closed_count "Example Web API tracker state candidates" 212 "docs/dev/bug-council-sweep-webapi-2026-05-05.md" "example Web API tracker state sweep count matches scanner"
require_closed_count "Mutable public byte arrays and array properties" 12 "docs/dev/bug-council-sweep-residual-small-2026-05-05.md" "residual mutable byte-array sweep count matches scanner"
require_closed_count "Value equality and hash-code comparisons" 4 "docs/dev/bug-council-sweep-residual-small-2026-05-05.md" "residual value equality sweep count matches scanner"
require_closed_count "Security-sensitive material candidates" 2 "docs/dev/bug-council-sweep-residual-small-2026-05-05.md" "residual secret-pattern sweep count matches scanner"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d council sweep count check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll council sweep count checks passed.\n'
