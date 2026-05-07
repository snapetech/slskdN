#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

report="${COUNCIL_OUT_DIR:-.council}/active-bughunt.md"
backlog="docs/dev/bug-council-active-backlog.md"
failed=0

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failed=1
}

pass() {
  printf 'PASS %s\n' "$1"
}

if [[ ! -f "$report" ]]; then
  bash scripts/run-council-active-bughunt.sh >/dev/null
fi

if [[ ! -f "$backlog" ]]; then
  fail "active backlog is missing: $backlog"
  exit 1
fi

if rg -n '\| `[^`]+` \| [0-9]+ \| Untriaged \|' "$backlog" >/tmp/slsknet-active-backlog-untriaged.$$ 2>/dev/null; then
  fail "active backlog contains untriaged sections"
  sed 's/^/  /' /tmp/slsknet-active-backlog-untriaged.$$ >&2
else
  pass "active backlog has no untriaged sections"
fi
rm -f /tmp/slsknet-active-backlog-untriaged.$$

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
' "$report" >/tmp/slsknet-active-backlog-counts.$$

while IFS=$'\t' read -r section count; do
  if rg -n --fixed-strings "| \`$section\` | $count |" "$backlog" >/dev/null; then
    pass "active backlog tracks '$section' count $count"
  else
    fail "active backlog missing or stale for '$section' count $count"
  fi
done </tmp/slsknet-active-backlog-counts.$$

rm -f /tmp/slsknet-active-backlog-counts.$$

if [[ "$failed" -ne 0 ]]; then
  printf '\nActive backlog check failed. Run scripts/run-council-active-bughunt.sh, then update %s.\n' "$backlog" >&2
  exit 1
fi

printf '\nAll active backlog checks passed.\n'
