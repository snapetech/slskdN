#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

if rg -n 'raw\.githubusercontent\.com/snapetech/slskdn/master|github\.com/snapetech/slskdn/blob/master|github\.com/snapetech/slskdn/tree/master|github\.com/snapetech/slskdn/raw/master' \
  "$repo_root/packaging" "$repo_root/docs" "$repo_root/config" \
  -g '!docs/archive/**' \
  -g '!docs/dev/bug-burndown-ledger.md' >&2; then
  printf 'Active fork package/docs links must target main, not master.\n' >&2
  failed=1
fi

if rg -n 'branches:\s*\[master\]' "$repo_root/docs" \
  -g '!docs/archive/**' \
  -g '!docs/dev/bug-burndown-ledger.md' >&2; then
  printf 'Active GitHub Actions examples must target main, not master.\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Active fork branch links target main.\n'
