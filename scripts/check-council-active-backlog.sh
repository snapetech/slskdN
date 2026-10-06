#!/usr/bin/env bash
#
# Bug Council active backlog gate — TEMPLATE.
#
# Asserts that every section emitted by run-council-active-bughunt.sh has a
# current durable row in docs/dev/bug-council-active-backlog.md. This is the
# guard that turns "scan a pile of things" into a resumable burn-down queue.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

report="${COUNCIL_OUT_DIR:-.council}/active-bughunt.md"
backlog="docs/dev/bug-council-active-backlog.md"
untriaged_output="$(mktemp)"
counts_output="$(mktemp)"
trap 'rm -f "$untriaged_output" "$counts_output"' EXIT
failed=0

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failed=1
}

pass() {
  printf 'PASS %s\n' "$1"
}

bash scripts/run-council-active-bughunt.sh >/dev/null

if [[ ! -f "$backlog" ]]; then
  fail "active backlog is missing: $backlog"
  exit 1
fi

if rg -n '\| `[^`]+` \| [0-9]+ \| Untriaged \|' "$backlog" >"$untriaged_output" 2>/dev/null; then
  fail "active backlog contains untriaged sections"
  sed 's/^/  /' "$untriaged_output" >&2
else
  pass "active backlog has no untriaged sections"
fi

awk '
  /^## / {
    if (section != "") {
      print section "\t" count
    }

    section = substr($0, 4)
    count = 0
    next
  }

  section != "" && NF {
    count++
  }

  END {
    if (section != "") {
      print section "\t" count
    }
  }
' "$report" >"$counts_output"

while IFS=$'\t' read -r section count; do
  if rg -n --fixed-strings "| \`$section\` | $count |" "$backlog" >/dev/null; then
    pass "active backlog tracks '$section' count $count"
  else
    fail "active backlog missing or stale for '$section' count $count"
  fi
done <"$counts_output"

if [[ "$failed" -ne 0 ]]; then
  printf '\nActive backlog check failed. Run scripts/run-council-active-bughunt.sh, then update %s.\n' "$backlog" >&2
  exit 1
fi

printf '\nAll active backlog checks passed.\n'
